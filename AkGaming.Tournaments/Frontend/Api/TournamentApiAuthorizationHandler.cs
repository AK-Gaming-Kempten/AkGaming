using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AkGaming.Core.Components.Authentication;
using AkGaming.Tournaments.Frontend.Authentication;
using AkGaming.Tournaments.Frontend.Startup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TournamentApiAuthorizationHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> RetryAttemptedOptionKey = new("__akg_tournaments_retry_attempted");
    internal static readonly HttpRequestOptionsKey<bool> SkipAuthorizationOptionKey = new("__akg_tournaments_skip_authorization");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptionsMonitor;
    private readonly AuthenticationTicketTokenUpdater _ticketTokenUpdater;
    private readonly ILogger<TournamentApiAuthorizationHandler> _logger;

    public TournamentApiAuthorizationHandler(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
        AuthenticationTicketTokenUpdater ticketTokenUpdater,
        ILogger<TournamentApiAuthorizationHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _oidcOptionsMonitor = oidcOptionsMonitor;
        _ticketTokenUpdater = ticketTokenUpdater;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(SkipAuthorizationOptionKey, out var skipAuthorization) && skipAuthorization)
            return await base.SendAsync(request, cancellationToken);

        var applicationServices = ResolveApplicationServices(request);
        if (applicationServices is null)
            return await base.SendAsync(request, cancellationToken);

        var tokenStore = applicationServices.GetRequiredService<OidcTokenStore>();
        var sessionCoordinator = applicationServices.GetRequiredService<FrontendSessionCoordinator>();
        await EnsureTokenStoreInitializedAsync(applicationServices, tokenStore);

        var accessToken = tokenStore.AccessToken;
        var refreshToken = tokenStore.RefreshToken;
        var expiresAtRaw = tokenStore.ExpiresAt;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogInformation("No tournaments access token available in the current circuit scope, attempting refresh.");
                accessToken = await RefreshAccessTokenAsync(tokenStore, sessionCoordinator, refreshToken, accessToken, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                if (tokenStore.IsInitialized)
                {
                    _logger.LogWarning("No tournaments bearer token is available for an initialized session. Re-authentication required.");
                    await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }

        if (IsExpired(expiresAtRaw))
        {
            _logger.LogInformation("Tournaments access token expired, refreshing.");
            accessToken = await RefreshAccessTokenAsync(tokenStore, sessionCoordinator, refreshToken, accessToken, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        if (!tokenStore.IsInitialized)
            return response;

        if (request.Options.TryGetValue(RetryAttemptedOptionKey, out var retryAttempted) && retryAttempted)
        {
            _logger.LogWarning("Tournaments API request still returned 401 after refresh attempt. Re-authentication required.");
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
            return response;
        }

        _logger.LogWarning("Tournaments API request returned 401. Attempting token refresh and retry.");
        response.Dispose();

        accessToken = await RefreshAccessTokenAsync(tokenStore, sessionCoordinator, tokenStore.RefreshToken, tokenStore.AccessToken, cancellationToken, force: true);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

        var retryRequest = await CloneRequestAsync(request, cancellationToken);
        retryRequest.Options.Set(RetryAttemptedOptionKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var retryResponse = await base.SendAsync(retryRequest, cancellationToken);
        if (retryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);

        return retryResponse;
    }

    private IServiceProvider? ResolveApplicationServices(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(ApplicationScopeHttpClientExtensions.ScopeKey, out var services))
            return services;

        _logger.LogWarning("No application scope was attached to the outgoing tournaments request.");
        return null;
    }

    private static async Task EnsureTokenStoreInitializedAsync(IServiceProvider services, OidcTokenStore tokenStore)
    {
        if (tokenStore.IsInitialized)
            return;

        var httpContext = services.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext is null)
            return;

        var accessToken = await httpContext.GetTokenAsync("access_token");
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        var expiresAt = await httpContext.GetTokenAsync("expires_at");
        tokenStore.Initialize(accessToken, refreshToken, expiresAt);
    }

    private async Task<string?> ResolveTokenEndpointAsync(CancellationToken cancellationToken)
    {
        var oidcOptions = _oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        if (oidcOptions.ConfigurationManager is null)
            return null;

        try
        {
            var configuration = await oidcOptions.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            return configuration.TokenEndpoint;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve tournaments token endpoint from OIDC discovery.");
            return null;
        }
    }

    private async Task<string?> RefreshAccessTokenAsync(
        OidcTokenStore tokenStore,
        FrontendSessionCoordinator sessionCoordinator,
        string? refreshToken,
        string? staleAccessToken,
        CancellationToken cancellationToken,
        bool force = false)
    {
        await tokenStore.RefreshLock.WaitAsync(cancellationToken);

        try
        {
            var currentAccessToken = tokenStore.AccessToken;
            var currentExpiresAt = tokenStore.ExpiresAt;
            if (!string.IsNullOrWhiteSpace(currentAccessToken)
                && !IsExpired(currentExpiresAt)
                && (!force || !string.Equals(currentAccessToken, staleAccessToken, StringComparison.Ordinal)))
            {
                return currentAccessToken;
            }

            refreshToken = tokenStore.RefreshToken ?? refreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Tournaments access token refresh requested but no refresh token is present.");
                await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                return null;
            }

            var tokenEndpoint = await ResolveTokenEndpointAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(tokenEndpoint))
            {
                _logger.LogWarning("No tournaments token endpoint configured/discovered.");
                await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                return null;
            }

            var oidcOptions = _oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
            if (string.IsNullOrWhiteSpace(oidcOptions.ClientId))
            {
                _logger.LogWarning("Tournaments OIDC client id is not configured.");
                await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                return null;
            }

            var client = _httpClientFactory.CreateClient("OidcBackchannel");
            var payload = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = oidcOptions.ClientId
            };

            if (!string.IsNullOrWhiteSpace(oidcOptions.ClientSecret))
                payload["client_secret"] = oidcOptions.ClientSecret;

            HttpResponseMessage response;
            try
            {
                using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(payload)
                };
                response = await client.SendAsync(refreshRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call tournaments token endpoint.");
                await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                return null;
            }

            using (response)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var bodyPreview = responseContent.Length > 512 ? responseContent[..512] + "..." : responseContent;
                    _logger.LogWarning("Tournaments token refresh failed: {Status}. Body: {Body}", response.StatusCode, bodyPreview);
                    await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                    return null;
                }

                using var json = JsonDocument.Parse(responseContent);
                var root = json.RootElement;

                var newAccessToken = GetTokenValue(root, "access_token", "accessToken");
                var newRefreshToken = GetTokenValue(root, "refresh_token", "refreshToken") ?? refreshToken;
                var expiresIn = GetIntegerValue(root, "expires_in", "expiresIn");

                if (string.IsNullOrWhiteSpace(newAccessToken))
                {
                    _logger.LogWarning("Tournaments token refresh succeeded but did not return an access token.");
                    await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                    return null;
                }

                var newExpiry = expiresIn.HasValue
                    ? DateTime.UtcNow.AddSeconds(expiresIn.Value)
                    : DateTime.UtcNow.AddMinutes(10);
                var newExpiresAt = newExpiry.ToString("o", CultureInfo.InvariantCulture);

                tokenStore.SetTokens(newAccessToken, newRefreshToken, newExpiresAt);
                await _ticketTokenUpdater.UpdateTokensAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    newAccessToken,
                    newRefreshToken,
                    newExpiresAt,
                    cancellationToken);

                if (force)
                    _logger.LogInformation("Forced tournaments token refresh completed successfully after API 401.");

                return newAccessToken;
            }
        }
        finally
        {
            tokenStore.RefreshLock.Release();
        }
    }

    private async Task HandleExpiredSessionAsync(OidcTokenStore tokenStore, FrontendSessionCoordinator sessionCoordinator)
    {
        tokenStore.Clear();
        await sessionCoordinator.NotifySessionExpiredAsync();
    }

    private static bool IsExpired(string? expiresAtRaw)
    {
        if (string.IsNullOrWhiteSpace(expiresAtRaw))
            return false;

        if (!DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
            return false;

        return expiresAt.UtcDateTime <= DateTime.UtcNow.AddSeconds(30);
    }

    private static string? GetTokenValue(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        if (root.TryGetProperty("tokens", out var tokens) && tokens.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in names)
            {
                if (tokens.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString();
            }
        }

        return null;
    }

    private static int? GetIntegerValue(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var asInt))
                return asInt;

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                return parsed;
        }

        return null;
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
