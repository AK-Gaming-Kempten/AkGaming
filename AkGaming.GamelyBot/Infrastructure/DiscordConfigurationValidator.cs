using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordConfigurationValidator(
    IHttpClientFactory httpClientFactory,
    IOptions<DiscordOptions> options,
    ILogger<DiscordConfigurationValidator> logger) : IHostedService
{
    private readonly DiscordOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token)
            || string.IsNullOrWhiteSpace(_options.GuildId)
            || string.IsNullOrWhiteSpace(_options.AdministrationChannelId)
            || string.IsNullOrWhiteSpace(_options.TreasurerRoleId)
            || string.IsNullOrWhiteSpace(_options.BoardChannelId)
            || string.IsNullOrWhiteSpace(_options.BoardRoleId)
            || string.IsNullOrWhiteSpace(_options.ExtendedBoardRoleId)
            || string.IsNullOrWhiteSpace(_options.ApplicationPublicKey))
            throw new InvalidOperationException("Discord Token, GuildId, AdministrationChannelId, TreasurerRoleId, BoardChannelId, BoardRoleId, ExtendedBoardRoleId, and ApplicationPublicKey must be configured.");

        var client = httpClientFactory.CreateClient(nameof(DiscordConfigurationValidator));
        client.BaseAddress = new Uri("https://discord.com/api/v10/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);

        var guilds = await GetAsync<List<DiscordGuild>>(client, "users/@me/guilds", cancellationToken);
        if (guilds.Count != 1 || guilds[0].Id != _options.GuildId)
            throw new InvalidOperationException("GamelyBot must be installed in exactly the configured club server.");

        var channel = await GetAsync<DiscordChannel>(client, $"channels/{_options.AdministrationChannelId}", cancellationToken);
        if (channel.GuildId != _options.GuildId)
            throw new InvalidOperationException("The configured administration channel does not belong to the configured club server.");
        var boardChannel = await GetAsync<DiscordChannel>(client, $"channels/{_options.BoardChannelId}", cancellationToken);
        if (boardChannel.GuildId != _options.GuildId)
            throw new InvalidOperationException("The configured board channel does not belong to the configured club server.");

        var roles = await GetAsync<List<DiscordRole>>(client, $"guilds/{_options.GuildId}/roles", cancellationToken);
        if (roles.All(role => role.Id != _options.TreasurerRoleId))
            throw new InvalidOperationException("The configured treasurer role does not belong to the configured club server.");
        if (roles.All(role => role.Id != _options.BoardRoleId))
            throw new InvalidOperationException("The configured board role does not belong to the configured club server.");
        if (roles.All(role => role.Id != _options.ExtendedBoardRoleId))
            throw new InvalidOperationException("The configured extended board role does not belong to the configured club server.");
        ValidateOptionalRole(roles, _options.SecretaryRoleId, "secretary");
        ValidateOptionalRole(roles, _options.HeadOfSportsRoleId, "head-of-sports");
        ValidateOptionalRole(roles, _options.MemberRoleId, "member");

        logger.LogInformation("Validated Discord configuration for guild {GuildId}.", _options.GuildId);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void ValidateOptionalRole(IReadOnlyCollection<DiscordRole> roles, string roleId, string roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleId) && roles.All(role => role.Id != roleId))
            throw new InvalidOperationException($"The configured {roleName} role does not belong to the configured club server.");
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Discord configuration validation failed with {(int)response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Discord returned an empty configuration response.");
    }

    private sealed record DiscordGuild([property: JsonPropertyName("id")] string Id);
    private sealed record DiscordChannel(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("guild_id")] string GuildId);
    private sealed record DiscordRole([property: JsonPropertyName("id")] string Id);
}
