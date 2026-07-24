using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordCommandRegistrationService(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordOptions> options,
    BotText text,
    ILogger<DiscordCommandRegistrationService> logger) : IHostedService
{
    private readonly DiscordOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(DiscordCommandRegistrationService));
        client.BaseAddress = new Uri("https://discord.com/api/v10/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);

        var botUser = await GetAsync<DiscordBotUser>(client, "users/@me", cancellationToken);
        var path = $"applications/{botUser.Id}/guilds/{_options.GuildId}/commands";
        var commands = await GetAsync<List<DiscordRegisteredCommand>>(client, path, cancellationToken);
        var existing = commands.SingleOrDefault(command => command.Name == "boardmeeting");
        var legacy = commands.SingleOrDefault(command => command.Name == "board");
        await UpsertAsync(client, path, existing ?? legacy, BoardCommandDefinition(), cancellationToken);
        var auditSummary = commands.SingleOrDefault(command => command.Name == "auditsummary");
        await UpsertAsync(client, path, auditSummary, AuditSummaryCommandDefinition(), cancellationToken);
        if (existing is not null && legacy is not null)
        {
            using var deleteResponse = await client.DeleteAsync($"{path}/{legacy.Id}", cancellationToken);
            if (!deleteResponse.IsSuccessStatusCode)
            {
                var deleteBody = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Legacy Discord command removal failed with {(int)deleteResponse.StatusCode}: {deleteBody}");
            }
        }
        logger.LogInformation("Registered the guild-scoped /boardmeeting and /auditsummary commands for guild {GuildId}.", _options.GuildId);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private object BoardCommandDefinition()
    {
        return new
        {
            name = "boardmeeting",
            description = text["CommandBoardDescription"],
            type = 1,
            dm_permission = false,
            options = new object[]
            {
                Subcommand("help", text["CommandHelpDescription"]),
                Subcommand("create", text["CommandCreateDescription"]),
                Subcommand("reminder", text["CommandReminderDescription"]),
                Subcommand("agenda", text["CommandAgendaDescription"]),
                Subcommand("backlog", text["CommandBacklogDescription"]),
                Subcommand("details", text["CommandDetailsDescription"]),
                Subcommand("add-agenda", text["CommandAddAgendaDescription"]),
                Subcommand("add-backlog", text["CommandAddBacklogDescription"]),
                new
                {
                    type = 1,
                    name = "promote",
                    description = text["CommandPromoteDescription"],
                    options = new object[]
                    {
                        new { type = 3, name = "item", description = text["CommandBacklogItemDescription"], required = true, autocomplete = true }
                    }
                },
                new
                {
                    type = 1,
                    name = "availability",
                    description = text["CommandAvailabilityDescription"],
                    options = new object[]
                    {
                        new
                        {
                            type = 3,
                            name = "status",
                            description = text["CommandAvailabilityOptionDescription"],
                            required = true,
                            choices = new[]
                            {
                                new { name = text["AvailabilityAvailable"], value = "available" },
                                new { name = text["AvailabilityUnavailable"], value = "unavailable" }
                            }
                        }
                    }
                }
            }
        };
    }

    private object AuditSummaryCommandDefinition()
    {
        return new
        {
            name = "auditsummary",
            description = text["CommandAuditSummaryDescription"],
            type = 1,
            dm_permission = false,
            options = new object[]
            {
                Subcommand("identity", text["CommandIdentityAuditDescription"]),
                Subcommand("management", text["CommandManagementAuditDescription"])
            }
        };
    }

    private static async Task UpsertAsync(HttpClient client, string path, DiscordRegisteredCommand? existing,
        object definition, CancellationToken cancellationToken)
    {
        using var response = existing is null
            ? await client.PostAsJsonAsync(path, definition, cancellationToken)
            : await client.PatchAsJsonAsync($"{path}/{existing.Id}", definition, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord command registration failed with {(int)response.StatusCode}: {body}");
    }

    private static object Subcommand(string name, string description)
    {
        return new { type = 1, name, description };
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord command lookup failed with {(int)response.StatusCode}: {body}");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException("Discord returned an empty command response.");
    }

    private sealed record DiscordBotUser([property: JsonPropertyName("id")] string Id);
    private sealed record DiscordRegisteredCommand(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);
}
