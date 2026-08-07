using System.Text;
using System.Text.Json;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace AkGaming.GamelyBot.Api.Controllers;

[ApiController]
[Route("api/discord/interactions")]
[AllowAnonymous]
public sealed class DiscordInteractionsController(DiscordInteractionService interactions, IOptions<DiscordOptions> discordOptions,
    IOptions<DiscordInteractionOptions> interactionOptions, BoardRescheduleInputParser rescheduleInputParser,
    AuditSummaryService auditSummaries, BotText text,
    ILogger<DiscordInteractionsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        if (interactionOptions.Value.ValidateSignatures && !HasValidSignature(body)) return Unauthorized();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var interactionType = root.GetProperty("type").GetInt32();
        if (interactionType == 1) return Ok(new { type = 1 });
        var guildId = root.TryGetProperty("guild_id", out var guild) ? guild.GetString() : null;
        if (!string.Equals(guildId, discordOptions.Value.GuildId, StringComparison.Ordinal)) return Ok(Ephemeral(text["WrongDiscordServer"]));
        var discordUserId = root.GetProperty("member").GetProperty("user").GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(discordUserId)) return Ok(Ephemeral(text["DiscordAccountUnknown"]));
        using var interactionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        interactionTimeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            var response = await HandleInteractionAsync(root, interactionType, discordUserId, interactionTimeout.Token);
            return Ok(response);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Discord interaction timed out for user {DiscordUserId}.", discordUserId);
            return Ok(Ephemeral(text["InteractionTimedOut"]));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discord interaction failed for user {DiscordUserId}.", discordUserId);
            return Ok(Ephemeral(text["InteractionFailed"]));
        }
    }

    private async Task<object> HandleInteractionAsync(JsonElement root, int interactionType, string discordUserId, CancellationToken cancellationToken)
    {
        var data = root.GetProperty("data");
        if (interactionType == 3) return await HandleButtonAsync(data, discordUserId, cancellationToken);
        if (interactionType == 4) return await HandleAutocompleteAsync(data, discordUserId, cancellationToken);
        if (interactionType == 5) return await HandleModalAsync(data, discordUserId, cancellationToken);
        if (interactionType != 2) return Ephemeral(text["UnsupportedInteraction"]);

        var commandName = data.GetProperty("name").GetString();
        if (commandName == "auditsummary")
            return Ephemeral(await GetAuditSummaryAsync(data, discordUserId, cancellationToken));
        if (commandName != "boardmeeting") return Ephemeral(text["UnsupportedInteraction"]);

        var command = GetSubcommand(data);
        return command switch
        {
            "help" => Ephemeral(interactions.GetBoardMeetingHelp()),
            "create" => Ephemeral(interactions.GetBoardMeetingCreateHelp()),
            "reminder" => Ephemeral(await interactions.QueueNextMeetingReminderAsync(discordUserId, cancellationToken)),
            "backlog" => Ephemeral(await interactions.GetBacklogAsync(discordUserId, cancellationToken)),
            "agenda" => Ephemeral(await interactions.GetNextMeetingAsync(discordUserId, true, cancellationToken)),
            "details" => Ephemeral(await interactions.GetNextMeetingAsync(discordUserId, false, cancellationToken)),
            "add-backlog" => AgendaItemModal("board-meeting-add-backlog", text["AddBacklogItem"]),
            "add-agenda" => AgendaItemModal("board-meeting-add-agenda", text["AddAgendaItem"]),
            "promote" => await PromoteAsync(data, discordUserId, cancellationToken),
            "availability" => await SetNextAvailabilityAsync(data, discordUserId, cancellationToken),
            _ => Ephemeral(text["UnsupportedBoardMeetingCommand"])
        };
    }

    private async Task<string> GetAuditSummaryAsync(JsonElement data, string discordUserId,
        CancellationToken cancellationToken)
    {
        var source = GetSubcommand(data);
        if (source is not ("identity" or "management"))
            return text["SelectAuditSummary"];
        var toUtc = DateTimeOffset.UtcNow;
        var fromUtc = toUtc.AddDays(-7);
        return await auditSummaries.GetForDiscordAsync(discordUserId, source, fromUtc, toUtc, cancellationToken);
    }

    private async Task<object> HandleButtonAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var customId = data.GetProperty("custom_id").GetString();
        var parts = customId?.Split(':');
        if (customId == "bpx") return ReplaceInteractionMessage(text["NoChangesMade"]);
        if (parts is { Length: 3 } && parts[0] == "ac"
            && Guid.TryParse(parts[1], out var allocationApplicationId)
            && parts[2] is "a" or "o")
        {
            var decisionMessage = await interactions.SetAllocationClaimDecisionAsync(
                discordUserId,
                allocationApplicationId,
                parts[2] == "a",
                cancellationToken);
            return Ephemeral(decisionMessage);
        }
        if (parts is { Length: > 0 } && parts[0] == "ac")
            return Ephemeral(text["InvalidAllocationClaimAction"]);
        if (parts is { Length: 4 } && parts[0] == "bp"
            && Guid.TryParse(parts[1], out var proposalMeetingId)
            && Guid.TryParse(parts[2], out var proposalId)
            && parts[3] is "a" or "r")
        {
            var accept = parts[3] == "a";
            return ProposalDecisionConfirmation(proposalMeetingId, proposalId, accept);
        }
        if (parts is { Length: 4 } && parts[0] == "bpc"
            && Guid.TryParse(parts[1], out var confirmedMeetingId)
            && Guid.TryParse(parts[2], out var confirmedProposalId)
            && parts[3] is "a" or "r")
        {
            var accept = parts[3] == "a";
            var decisionMessage = await interactions.DecideRescheduleProposalAsync(discordUserId,
                confirmedMeetingId, confirmedProposalId, accept, cancellationToken);
            return ReplaceInteractionMessage(decisionMessage);
        }
        if (parts is { Length: 3 } && parts[0] == "board-reschedule"
            && Guid.TryParse(parts[1], out var rescheduleMeetingId)
            && int.TryParse(parts[2], out var rescheduleVersion))
        {
            return RescheduleModal(rescheduleMeetingId, rescheduleVersion);
        }
        if (parts is not { Length: 4 } || parts[0] != "board-availability" || !Guid.TryParse(parts[1], out var meetingId) || !int.TryParse(parts[2], out var version))
            return Ephemeral(text["InvalidMeetingAction"]);
        var status = parts[3] == "available" ? "Available" : "Unavailable";
        var message = await interactions.SetAvailabilityAsync(discordUserId, meetingId, version, status, cancellationToken);
        return Ephemeral(message);
    }

    private async Task<object> HandleAutocompleteAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        if (GetSubcommand(data) != "promote") return Autocomplete([]);
        var focused = FindFocusedOption(data);
        var choices = await interactions.GetBacklogChoicesAsync(discordUserId, focused, cancellationToken);
        return Autocomplete(choices);
    }

    private async Task<object> HandleModalAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var customId = data.GetProperty("custom_id").GetString();
        var customIdParts = customId?.Split(':');
        if (customIdParts is { Length: 3 } && customIdParts[0] == "board-reschedule"
            && Guid.TryParse(customIdParts[1], out var meetingId)
            && int.TryParse(customIdParts[2], out var scheduleVersion))
        {
            var input = rescheduleInputParser.Parse(
                FindComponentValue(data, "proposed-at"),
                FindComponentValue(data, "duration"));
            if (!input.IsSuccess) return Ephemeral(input.Error!);
            var reason = FindComponentValue(data, "reason");
            var proposalMessage = await interactions.ProposeRescheduleAsync(discordUserId, meetingId, scheduleVersion,
                input.ProposedAtUtc, input.DurationMinutes, reason, cancellationToken);
            return Ephemeral(proposalMessage);
        }
        if (customId is not ("board-meeting-add-backlog" or "board-meeting-add-agenda")) return Ephemeral(text["UnsupportedFormSubmission"]);
        var title = FindComponentValue(data, "title");
        var description = FindComponentValue(data, "description");
        if (string.IsNullOrWhiteSpace(title)) return Ephemeral(text["AgendaItemTitleRequired"]);
        var message = await interactions.AddAgendaItemAsync(discordUserId, title, description, customId == "board-meeting-add-agenda", cancellationToken);
        return Ephemeral(message);
    }

    private async Task<object> PromoteAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var itemValue = FindOptionValue(data, "item");
        if (!Guid.TryParse(itemValue, out var itemId)) return Ephemeral(text["SelectValidBacklogItem"]);
        var message = await interactions.PromoteBacklogItemAsync(discordUserId, itemId, cancellationToken);
        return Ephemeral(message);
    }

    private async Task<object> SetNextAvailabilityAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var value = FindOptionValue(data, "status");
        var status = value == "available" ? "Available" : value == "unavailable" ? "Unavailable" : null;
        if (status is null) return Ephemeral(text["SelectAvailability"]);
        var message = await interactions.SetNextMeetingAvailabilityAsync(discordUserId, status, cancellationToken);
        return Ephemeral(message);
    }

    private static string? GetSubcommand(JsonElement data)
    {
        if (!data.TryGetProperty("options", out var options)) return null;
        var subcommand = options.EnumerateArray().FirstOrDefault();
        return subcommand.ValueKind == JsonValueKind.Undefined ? null : subcommand.GetProperty("name").GetString();
    }

    private static string FindOptionValue(JsonElement data, string name)
    {
        foreach (var option in DescendantOptions(data))
            if (option.GetProperty("name").GetString() == name && option.TryGetProperty("value", out var value)) return value.GetString() ?? string.Empty;
        return string.Empty;
    }

    private static string FindFocusedOption(JsonElement data)
    {
        foreach (var option in DescendantOptions(data))
            if (option.TryGetProperty("focused", out var focused) && focused.GetBoolean()) return option.GetProperty("value").GetString() ?? string.Empty;
        return string.Empty;
    }

    private static IEnumerable<JsonElement> DescendantOptions(JsonElement element)
    {
        if (!element.TryGetProperty("options", out var options)) yield break;
        foreach (var option in options.EnumerateArray())
        {
            yield return option;
            foreach (var descendant in DescendantOptions(option)) yield return descendant;
        }
    }

    private static string? FindComponentValue(JsonElement data, string customId)
    {
        foreach (var row in data.GetProperty("components").EnumerateArray())
        foreach (var component in row.GetProperty("components").EnumerateArray())
            if (component.GetProperty("custom_id").GetString() == customId) return component.GetProperty("value").GetString();
        return null;
    }

    private bool HasValidSignature(string body)
    {
        var signatureHex = Request.Headers["X-Signature-Ed25519"].ToString();
        var timestamp = Request.Headers["X-Signature-Timestamp"].ToString();
        if (string.IsNullOrWhiteSpace(signatureHex) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(discordOptions.Value.ApplicationPublicKey)) return false;
        try
        {
            if (!long.TryParse(timestamp, out var timestampSeconds)) return false;
            var signedAt = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            if (DateTimeOffset.UtcNow - signedAt > TimeSpan.FromMinutes(5) || signedAt - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(1)) return false;
            var algorithm = SignatureAlgorithm.Ed25519;
            var publicKey = PublicKey.Import(algorithm, Convert.FromHexString(discordOptions.Value.ApplicationPublicKey), KeyBlobFormat.RawPublicKey);
            return algorithm.Verify(publicKey, Encoding.UTF8.GetBytes(timestamp + body), Convert.FromHexString(signatureHex));
        }
        catch (Exception) { return false; }
    }

    private static object Ephemeral(string content) => new { type = 4, data = new { content, flags = 64 } };
    private static object Autocomplete(IReadOnlyList<DiscordCommandChoice> choices) => new { type = 8, data = new { choices = choices.Select(choice => new { name = choice.Name, value = choice.Value }) } };
    private object AgendaItemModal(string customId, string title) => new
    {
        type = 9,
        data = new
        {
            custom_id = customId,
            title,
            components = new object[]
            {
                new { type = 1, components = new[] { new { type = 4, custom_id = "title", label = text["Title"], style = 1, min_length = 1, max_length = 500, required = true } } },
                new { type = 1, components = new[] { new { type = 4, custom_id = "description", label = text["Description"], style = 2, min_length = 0, max_length = 2000, required = false } } }
            }
        }
    };

    private object RescheduleModal(Guid meetingId, int scheduleVersion)
    {
        return new
        {
            type = 9,
            data = new
            {
                custom_id = $"board-reschedule:{meetingId}:{scheduleVersion}",
                title = text["ProposeAnotherMeetingTime"],
                components = new object[]
                {
                    new { type = 1, components = new[] { new { type = 4, custom_id = "proposed-at", label = text["DateAndTimeLabel"], style = 1, min_length = 10, max_length = 16, required = true } } },
                    new { type = 1, components = new[] { new { type = 4, custom_id = "duration", label = text["DurationMinutesLabel"], style = 1, min_length = 2, max_length = 4, required = true } } },
                    new { type = 1, components = new[] { new { type = 4, custom_id = "reason", label = text["Reason"], style = 2, min_length = 0, max_length = 1000, required = false } } }
                }
            }
        };
    }

    private object ProposalDecisionConfirmation(Guid meetingId, Guid proposalId, bool accept)
    {
        var action = accept ? text["AcceptAction"] : text["RejectAction"];
        var confirmLabel = accept ? text["ConfirmAcceptance"] : text["ConfirmRejection"];
        var confirmStyle = accept ? 3 : 4;
        return new
        {
            type = 4,
            data = new
            {
                content = text.Format("ConfirmProposalDecision", action),
                flags = 64,
                components = new object[]
                {
                    new
                    {
                        type = 1,
                        components = new object[]
                        {
                            new { type = 2, style = confirmStyle, label = confirmLabel, custom_id = $"bpc:{meetingId}:{proposalId}:{(accept ? "a" : "r")}" },
                            new { type = 2, style = 2, label = text["Cancel"], custom_id = "bpx" }
                        }
                    }
                }
            }
        };
    }

    private static object ReplaceInteractionMessage(string content)
    {
        return new
        {
            type = 7,
            data = new
            {
                content,
                components = Array.Empty<object>()
            }
        };
    }
}
