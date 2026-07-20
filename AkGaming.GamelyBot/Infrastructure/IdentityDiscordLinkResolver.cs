using System.Net.Http.Headers;
using System.Net.Http.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class IdentityDiscordLinkResolver(
    IHttpClientFactory httpClientFactory,
    ClientCredentialsTokenProvider tokenProvider,
    IOptions<IdentityClientOptions> options,
    ILogger<IdentityDiscordLinkResolver> logger) : IDiscordLinkResolver
{
    private readonly IdentityClientOptions _options = options.Value;

    public async Task<string?> ResolveDiscordUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(IdentityDiscordLinkResolver));
        client.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/discord-links/{userId}");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Identity Discord link lookup failed with {(int)response.StatusCode}: {error}");
        }

        var link = await response.Content.ReadFromJsonAsync<DiscordLinkResponse>(cancellationToken);
        if (link?.IsLinked != true || string.IsNullOrWhiteSpace(link.DiscordUserId))
        {
            logger.LogInformation("No linked Discord account exists for user {UserId}.", userId);
            return null;
        }
        return link.DiscordUserId;
    }
}
