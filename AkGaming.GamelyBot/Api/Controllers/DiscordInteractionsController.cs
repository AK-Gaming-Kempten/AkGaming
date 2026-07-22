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
        if (!string.Equals(guildId, discordOptions.Value.GuildId, StringComparison.Ordinal)) return Ok(Ephemeral("This bot only accepts interactions from the configured club server."));
        var discordUserId = root.GetProperty("member").GetProperty("user").GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(discordUserId)) return Ok(Ephemeral("Discord did not identify your account."));
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
            return Ok(Ephemeral("The club services did not respond in time. Please try again or use the management tool."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discord interaction failed for user {DiscordUserId}.", discordUserId);
            return Ok(Ephemeral("I could not complete that action right now. Please try again or use the management tool."));
        }
    }

    private async Task<object> HandleInteractionAsync(JsonElement root, int interactionType, string discordUserId, CancellationToken cancellationToken)
    {
        var data = root.GetProperty("data");
        if (interactionType == 3) return await HandleButtonAsync(data, discordUserId, cancellationToken);
        if (interactionType == 4) return await HandleAutocompleteAsync(data, discordUserId, cancellationToken);
        if (interactionType == 5) return await HandleModalAsync(data, discordUserId, cancellationToken);
        if (interactionType != 2 || data.GetProperty("name").GetString() != "boardmeeting") return Ephemeral("Unsupported interaction.");

        var command = GetSubcommand(data);
        return command switch
        {
            "help" => Ephemeral(interactions.GetBoardMeetingHelp()),
            "create" => Ephemeral(interactions.GetBoardMeetingCreateHelp()),
            "reminder" => Ephemeral(await interactions.QueueNextMeetingReminderAsync(discordUserId, cancellationToken)),
            "backlog" => Ephemeral(await interactions.GetBacklogAsync(discordUserId, cancellationToken)),
            "agenda" => Ephemeral(await interactions.GetNextMeetingAsync(discordUserId, true, cancellationToken)),
            "details" => Ephemeral(await interactions.GetNextMeetingAsync(discordUserId, false, cancellationToken)),
            "add-backlog" => AgendaItemModal("board-meeting-add-backlog", "Add backlog item"),
            "add-agenda" => AgendaItemModal("board-meeting-add-agenda", "Add agenda item"),
            "promote" => await PromoteAsync(data, discordUserId, cancellationToken),
            "availability" => await SetNextAvailabilityAsync(data, discordUserId, cancellationToken),
            _ => Ephemeral("Unsupported board meeting command.")
        };
    }

    private async Task<object> HandleButtonAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var customId = data.GetProperty("custom_id").GetString();
        var parts = customId?.Split(':');
        if (parts is { Length: 3 } && parts[0] == "board-reschedule"
            && Guid.TryParse(parts[1], out var rescheduleMeetingId)
            && int.TryParse(parts[2], out var rescheduleVersion))
        {
            return RescheduleModal(rescheduleMeetingId, rescheduleVersion);
        }
        if (parts is not { Length: 4 } || parts[0] != "board-availability" || !Guid.TryParse(parts[1], out var meetingId) || !int.TryParse(parts[2], out var version))
            return Ephemeral("This meeting action is invalid.");
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
        if (customId is not ("board-meeting-add-backlog" or "board-meeting-add-agenda")) return Ephemeral("Unsupported form submission.");
        var title = FindComponentValue(data, "title");
        var description = FindComponentValue(data, "description");
        if (string.IsNullOrWhiteSpace(title)) return Ephemeral("An agenda item title is required.");
        var message = await interactions.AddAgendaItemAsync(discordUserId, title, description, customId == "board-meeting-add-agenda", cancellationToken);
        return Ephemeral(message);
    }

    private async Task<object> PromoteAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var itemValue = FindOptionValue(data, "item");
        if (!Guid.TryParse(itemValue, out var itemId)) return Ephemeral("Select a valid backlog item.");
        var message = await interactions.PromoteBacklogItemAsync(discordUserId, itemId, cancellationToken);
        return Ephemeral(message);
    }

    private async Task<object> SetNextAvailabilityAsync(JsonElement data, string discordUserId, CancellationToken cancellationToken)
    {
        var value = FindOptionValue(data, "status");
        var status = value == "available" ? "Available" : value == "unavailable" ? "Unavailable" : null;
        if (status is null) return Ephemeral("Select whether you have time for the meeting.");
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
    private static object AgendaItemModal(string customId, string title) => new
    {
        type = 9,
        data = new
        {
            custom_id = customId,
            title,
            components = new object[]
            {
                new { type = 1, components = new[] { new { type = 4, custom_id = "title", label = "Title", style = 1, min_length = 1, max_length = 500, required = true } } },
                new { type = 1, components = new[] { new { type = 4, custom_id = "description", label = "Description", style = 2, min_length = 0, max_length = 2000, required = false } } }
            }
        }
    };

    private static object RescheduleModal(Guid meetingId, int scheduleVersion)
    {
        return new
        {
            type = 9,
            data = new
            {
                custom_id = $"board-reschedule:{meetingId}:{scheduleVersion}",
                title = "Propose another meeting time",
                components = new object[]
                {
                    new { type = 1, components = new[] { new { type = 4, custom_id = "proposed-at", label = "Date and time (DD.MM.YYYY HH:mm)", style = 1, min_length = 10, max_length = 16, required = true } } },
                    new { type = 1, components = new[] { new { type = 4, custom_id = "duration", label = "Duration in minutes", style = 1, min_length = 2, max_length = 4, required = true } } },
                    new { type = 1, components = new[] { new { type = 4, custom_id = "reason", label = "Reason", style = 2, min_length = 0, max_length = 1000, required = false } } }
                }
            }
        };
    }
}
