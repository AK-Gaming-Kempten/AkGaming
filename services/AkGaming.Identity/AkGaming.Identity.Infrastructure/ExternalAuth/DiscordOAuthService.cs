using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.ExternalAuth;
using Microsoft.Extensions.Options;

namespace AkGaming.Identity.Infrastructure.ExternalAuth;

public sealed class DiscordOAuthService : IDiscordOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly DiscordOptions _options;

    public DiscordOAuthService(HttpClient httpClient, IOptions<DiscordOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string BuildAuthorizationUrl(string state)
    {
        ValidateOptions();

        var scopes = string.Join(" ", _options.Scopes.Distinct(StringComparer.OrdinalIgnoreCase));

        return
            "https://discord.com/oauth2/authorize" +
            $"?response_type=code&client_id={Uri.EscapeDataString(_options.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<DiscordIdentity> GetIdentityFromAuthorizationCodeAsync(string code, CancellationToken cancellationToken)
    {
        ValidateOptions();

        using var tokenResponse = await _httpClient.PostAsync(
            "https://discord.com/api/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _options.RedirectUri
            }),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var details = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Discord token exchange failed ({(int)tokenResponse.StatusCode}): {details}");
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokenPayload is null || string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
        {
            throw new InvalidOperationException("Discord token response did not include an access token.");
        }

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.AccessToken);

        using var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            var details = await userResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Discord user profile request failed ({(int)userResponse.StatusCode}): {details}");
        }

        var userPayload = await userResponse.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);
        if (userPayload is null || string.IsNullOrWhiteSpace(userPayload.Id))
        {
            throw new InvalidOperationException("Discord user profile response did not include a user id.");
        }

        var username = !string.IsNullOrWhiteSpace(userPayload.GlobalName)
            ? userPayload.GlobalName
            : userPayload.Username ?? userPayload.Id;

        return new DiscordIdentity(userPayload.Id, username, userPayload.Email);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret) ||
            string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            throw new InvalidOperationException("Discord OAuth options are not fully configured.");
        }
    }

    private sealed record TokenResponse([property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);

    private sealed record UserResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")] string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("username")] string? Username,
        [property: System.Text.Json.Serialization.JsonPropertyName("global_name")] string? GlobalName,
        [property: System.Text.Json.Serialization.JsonPropertyName("email")] string? Email);
}
