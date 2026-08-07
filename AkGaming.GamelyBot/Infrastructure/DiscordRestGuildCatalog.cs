using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AkGaming.GamelyBot.Application;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordRestGuildCatalog(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordOptions> options) : IDiscordGuildCatalog
{
    private readonly DiscordOptions _options = options.Value;

    public async Task<DiscordGuildCatalog> GetAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(DiscordRestGuildCatalog));
        client.BaseAddress = new Uri("https://discord.com/api/v10/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);

        var channels = await GetAsync<List<DiscordChannelResponse>>(
            client, $"guilds/{_options.GuildId}/channels", cancellationToken);
        var roles = await GetAsync<List<DiscordRoleResponse>>(
            client, $"guilds/{_options.GuildId}/roles", cancellationToken);

        return new DiscordGuildCatalog(
            channels
                .OrderBy(item => item.Position)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(item => new DiscordGuildChannel(item.Id, item.Name ?? item.Id, item.Type, item.Position))
                .ToList(),
            roles
                .OrderByDescending(item => item.Position)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(item => new DiscordGuildRole(item.Id, item.Name ?? item.Id, item.Position))
                .ToList());
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord catalog request failed with {(int)response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Discord returned an empty catalog response.");
    }

    private sealed record DiscordChannelResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("type")] int Type,
        [property: JsonPropertyName("position")] int Position);

    private sealed record DiscordRoleResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("position")] int Position);
}
