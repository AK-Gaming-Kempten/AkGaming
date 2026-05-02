using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AkGaming.Tournaments.Frontend.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentApiAuthorizationHandler(
    OidcTokenStore tokenStore,
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    ILogger<TournamentApiAuthorizationHandler> logger) : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> RetryAttemptedOptionKey = new("__akg_tournaments_retry_attempted");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await EnsureTokenStoreInitializedAsync();

        var accessToken = tokenStore.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            accessToken = await RefreshAccessTokenAsync(tokenStore.RefreshToken, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return await base.SendAsync(request, cancellationToken);
        }

        if (IsExpired(tokenStore.ExpiresAt))
        {
            accessToken = await RefreshAccessTokenAsync(tokenStore.RefreshToken, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        if (request.Options.TryGetValue(RetryAttemptedOptionKey, out var retryAttempted) && retryAttempted)
        {
            tokenStore.Clear();
            return response;
        }

        response.Dispose();

        accessToken = await RefreshAccessTokenAsync(tokenStore.RefreshToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

        var retryRequest = await CloneRequestAsync(request, cancellationToken);
        retryRequest.Options.Set(RetryAttemptedOptionKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var retryResponse = await base.SendAsync(retryRequest, cancellationToken);
        if (retryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            tokenStore.Clear();

        return retryResponse;
    }

    private async Task EnsureTokenStoreInitializedAsync()
    {
        if (tokenStore.IsInitialized)
            return;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        var accessToken = await httpContext.GetTokenAsync("access_token");
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        var expiresAt = await httpContext.GetTokenAsync("expires_at");
        tokenStore.Initialize(accessToken, refreshToken, expiresAt);
    }

    private async Task<string?> RefreshAccessTokenAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogWarning("No refresh token available for tournaments frontend token refresh.");
            tokenStore.Clear();
            return null;
        }

        var tokenEndpoint = await ResolveTokenEndpointAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenEndpoint))
        {
            logger.LogWarning("OIDC discovery did not return a token endpoint.");
            tokenStore.Clear();
            return null;
        }

        var oidcOptions = oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        if (string.IsNullOrWhiteSpace(oidcOptions.ClientId))
        {
            logger.LogWarning("OIDC client id is not configured for tournaments frontend.");
            tokenStore.Clear();
            return null;
        }

        var requestContent = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = oidcOptions.ClientId
        };

        if (!string.IsNullOrWhiteSpace(oidcOptions.ClientSecret))
            requestContent["client_secret"] = oidcOptions.ClientSecret;

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestContent)
        };

        using var client = httpClientFactory.CreateClient("OidcBackchannel");
        using var response = await client.SendAsync(refreshRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Tournaments token refresh failed with status {StatusCode}.", response.StatusCode);
            tokenStore.Clear();
            return null;
        }

        using var json = JsonDocument.Parse(responseContent);
        var root = json.RootElement;
        var newAccessToken = GetString(root, "access_token");
        var newRefreshToken = GetString(root, "refresh_token") ?? refreshToken;
        var expiresIn = GetInteger(root, "expires_in");
        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            logger.LogWarning("Tournaments token refresh response did not contain an access token.");
            tokenStore.Clear();
            return null;
        }

        var expiresAt = expiresIn.HasValue
            ? DateTime.UtcNow.AddSeconds(expiresIn.Value)
            : DateTime.UtcNow.AddMinutes(10);
        var expiresAtRaw = expiresAt.ToString("o", CultureInfo.InvariantCulture);

        tokenStore.SetTokens(newAccessToken, newRefreshToken, expiresAtRaw);
        await PersistTokensAsync(newAccessToken, newRefreshToken, expiresAtRaw);
        return newAccessToken;
    }

    private async Task<string?> ResolveTokenEndpointAsync(CancellationToken cancellationToken)
    {
        var oidcOptions = oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        if (oidcOptions.ConfigurationManager is not null)
        {
            try
            {
                var configuration = await oidcOptions.ConfigurationManager.GetConfigurationAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
                    return configuration.TokenEndpoint;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve OIDC token endpoint from discovery.");
            }
        }

        if (Uri.TryCreate(oidcOptions.Authority, UriKind.Absolute, out var authority))
            return new Uri(authority, "/connect/token").ToString();

        return null;
    }

    private async Task PersistTokensAsync(string accessToken, string refreshToken, string expiresAt)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        if (httpContext.Response.HasStarted)
        {
            logger.LogDebug("Skipping tournaments auth cookie refresh because the HTTP response has already started.");
            return;
        }

        var authenticationResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null || authenticationResult.Properties is null)
            return;

        var tokens = authenticationResult.Properties.GetTokens().ToList();
        SetToken(tokens, "access_token", accessToken);
        SetToken(tokens, "refresh_token", refreshToken);
        SetToken(tokens, "expires_at", expiresAt);
        authenticationResult.Properties.StoreTokens(tokens);

        try
        {
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                authenticationResult.Principal,
                authenticationResult.Properties);
        }
        catch (InvalidOperationException ex) when (httpContext.Response.HasStarted)
        {
            logger.LogDebug(ex, "Skipping tournaments auth cookie refresh because headers are no longer writable.");
        }
    }

    private static void SetToken(ICollection<AuthenticationToken> tokens, string name, string value)
    {
        var existingToken = tokens.FirstOrDefault(token => string.Equals(token.Name, name, StringComparison.Ordinal));
        if (existingToken is not null)
            tokens.Remove(existingToken);

        tokens.Add(new AuthenticationToken { Name = name, Value = value });
    }

    private static bool IsExpired(string? expiresAtRaw)
    {
        if (string.IsNullOrWhiteSpace(expiresAtRaw))
            return false;

        if (!DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
            return false;

        return expiresAt.UtcDateTime <= DateTime.UtcNow.AddSeconds(30);
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? GetInteger(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
            return integer;

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var option in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentClone = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
                contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            clone.Content = contentClone;
        }

        return clone;
    }
}
