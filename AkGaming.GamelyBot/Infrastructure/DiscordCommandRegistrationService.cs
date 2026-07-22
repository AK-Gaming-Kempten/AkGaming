using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordCommandRegistrationService(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordOptions> options,
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
        var existing = commands.SingleOrDefault(command => command.Name == "board");
        var definition = BoardCommandDefinition();
        using var response = existing is null
            ? await client.PostAsJsonAsync(path, definition, cancellationToken)
            : await client.PatchAsJsonAsync($"{path}/{existing.Id}", definition, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord command registration failed with {(int)response.StatusCode}: {body}");
        logger.LogInformation("Registered the guild-scoped /board meeting commands for guild {GuildId}.", _options.GuildId);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static object BoardCommandDefinition()
    {
        return new
        {
            name = "board",
            description = "Board management",
            type = 1,
            dm_permission = false,
            options = new object[]
            {
                new
                {
                    type = 2,
                    name = "meeting",
                    description = "Board meeting planning",
                    options = new object[]
                    {
                        Subcommand("help", "Show the board meeting commands and management page"),
                        Subcommand("agenda", "View the next board meeting agenda"),
                        Subcommand("backlog", "View the board meeting backlog"),
                        Subcommand("details", "View details of the next board meeting"),
                        Subcommand("add-agenda", "Add an item to the next meeting agenda"),
                        Subcommand("add-backlog", "Add an item to the board meeting backlog"),
                        new
                        {
                            type = 1,
                            name = "promote",
                            description = "Add a backlog item to the next meeting",
                            options = new object[]
                            {
                                new { type = 3, name = "item", description = "Backlog item", required = true, autocomplete = true }
                            }
                        },
                        new
                        {
                            type = 1,
                            name = "availability",
                            description = "Set your availability for the next meeting",
                            options = new object[]
                            {
                                new
                                {
                                    type = 3,
                                    name = "status",
                                    description = "Whether you have time",
                                    required = true,
                                    choices = new[]
                                    {
                                        new { name = "I have time", value = "available" },
                                        new { name = "I cannot attend", value = "unavailable" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
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
