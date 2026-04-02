using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AkGaming.Management.Frontend.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using AkGaming.Management.Frontend.Startup;

namespace AkGaming.Management.Frontend.Handlers;

public class ApiAuthorizationHandler : DelegatingHandler {
    private static readonly HttpRequestOptionsKey<bool> RetryAttemptedOptionKey = new("__akg_retry_attempted");

    private readonly IHttpClientFactory _factory;
    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptionsMonitor;
    private readonly ILogger<ApiAuthorizationHandler> _log;

    public ApiAuthorizationHandler(
        IHttpClientFactory factory,
        IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
        ILogger<ApiAuthorizationHandler> log) {
        _factory = factory;
        _oidcOptionsMonitor = oidcOptionsMonitor;
        _log = log;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        var applicationServices = ResolveApplicationServices(request);
        if (applicationServices is null)
            return await base.SendAsync(request, ct);

        var tokenStore = applicationServices.GetRequiredService<OidcTokenStore>();
        var sessionCoordinator = applicationServices.GetRequiredService<FrontendSessionCoordinator>();
        await EnsureTokenStoreInitializedAsync(applicationServices, tokenStore);

        var accessToken = tokenStore.AccessToken;
        var refreshToken = tokenStore.RefreshToken;
        var expiresAtRaw = tokenStore.ExpiresAt;

        if (string.IsNullOrWhiteSpace(accessToken)) {
            if (!string.IsNullOrWhiteSpace(refreshToken)) {
                _log.LogInformation("No access token available in the current circuit scope, attempting refresh.");
                accessToken = await RefreshAccessTokenAsync(tokenStore, sessionCoordinator, refreshToken, ct);
            }

            if (string.IsNullOrWhiteSpace(accessToken))
                return await base.SendAsync(request, ct);
        }

        if (IsExpired(expiresAtRaw)) {
            _log.LogInformation("Access token expired, refreshing...");
            accessToken = await RefreshAccessTokenAsync(tokenStore, sessionCoordinator, refreshToken, ct);
            if (string.IsNullOrWhiteSpace(accessToken))
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        if (!tokenStore.IsInitialized)
            return response;

        if (request.Options.TryGetValue(RetryAttemptedOptionKey, out var retryAttempted) && retryAttempted) {
            _log.LogWarning("API request still returned 401 after refresh attempt. Re-authentication required.");
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
            return response;
        }

        _log.LogWarning("API request returned 401. Attempting token refresh and retry.");
        response.Dispose();

        accessToken = await RefreshAccessTokenAsync(tokenStore, sessionCoordinator, tokenStore.RefreshToken, ct, force: true);
        if (string.IsNullOrWhiteSpace(accessToken))
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

        var retryRequest = await CloneRequestAsync(request, ct);
        retryRequest.Options.Set(RetryAttemptedOptionKey, true);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var retryResponse = await base.SendAsync(retryRequest, ct);
        if (retryResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);

        return retryResponse;
    }

    private async Task<string?> ResolveTokenEndpointAsync(CancellationToken ct) {
        var oidcOptions = _oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        if (oidcOptions.ConfigurationManager is null)
            return null;

        try {
            var configuration = await oidcOptions.ConfigurationManager.GetConfigurationAsync(ct);
            return configuration.TokenEndpoint;
        } catch (Exception ex) {
            _log.LogWarning(ex, "Failed to resolve token endpoint from OIDC discovery.");
            return null;
        }
    }

    private static async Task EnsureTokenStoreInitializedAsync(IServiceProvider services, OidcTokenStore tokenStore) {
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

    private static bool IsExpired(string? expiresAtRaw) {
        if (string.IsNullOrWhiteSpace(expiresAtRaw))
            return false;

        if (!DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
            return false;

        // Refresh 30 seconds before actual expiry.
        return expiresAt.UtcDateTime <= DateTime.UtcNow.AddSeconds(30);
    }

    private static string? GetTokenValue(JsonElement root, params string[] names) {
        foreach (var name in names) {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        if (root.TryGetProperty("tokens", out var tokens) && tokens.ValueKind == JsonValueKind.Object) {
            foreach (var name in names) {
                if (tokens.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString();
            }
        }

        return null;
    }

    private static int? GetIntegerValue(JsonElement root, params string[] names) {
        foreach (var name in names) {
            if (!root.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var asInt))
                return asInt;
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                return parsed;
        }

        return null;
    }

    private async Task<string?> RefreshAccessTokenAsync(
        OidcTokenStore tokenStore,
        FrontendSessionCoordinator sessionCoordinator,
        string? refreshToken,
        CancellationToken ct,
        bool force = false) {
        if (string.IsNullOrWhiteSpace(refreshToken)) {
            _log.LogWarning("Access token refresh requested but no refresh token is present.");
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
            return null;
        }

        var tokenEndpoint = await ResolveTokenEndpointAsync(ct);
        if (string.IsNullOrWhiteSpace(tokenEndpoint)) {
            _log.LogWarning("No token endpoint configured/discovered.");
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
            return null;
        }

        var oidcOptions = _oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        if (string.IsNullOrWhiteSpace(oidcOptions.ClientId)) {
            _log.LogWarning("OIDC client id is not configured.");
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
            return null;
        }

        var client = _factory.CreateClient("OidcBackchannel");
        var payload = new Dictionary<string, string> {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = oidcOptions.ClientId
        };
        if (!string.IsNullOrWhiteSpace(oidcOptions.ClientSecret))
            payload["client_secret"] = oidcOptions.ClientSecret;

        HttpResponseMessage resp;
        try {
            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) {
                Content = new FormUrlEncodedContent(payload)
            };
            resp = await client.SendAsync(refreshRequest, ct);
        } catch (Exception ex) {
            _log.LogError(ex, "Failed to call token endpoint");
            await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
            return null;
        }

        using (resp) {
            var responseContent = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) {
                var bodyPreview = responseContent.Length > 512 ? responseContent[..512] + "..." : responseContent;
                _log.LogWarning("Token refresh failed: {Status}. Body: {Body}", resp.StatusCode, bodyPreview);
                await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                return null;
            }

            using var json = JsonDocument.Parse(responseContent);
            var root = json.RootElement;

            var newAccess = GetTokenValue(root, "access_token", "accessToken");
            var newRefresh = GetTokenValue(root, "refresh_token", "refreshToken") ?? refreshToken;
            var expiresIn = GetIntegerValue(root, "expires_in", "expiresIn");

            if (string.IsNullOrWhiteSpace(newAccess)) {
                _log.LogWarning("Token refresh succeeded but response did not contain an access token.");
                await HandleExpiredSessionAsync(tokenStore, sessionCoordinator);
                return null;
            }

            var newExpiry = expiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(expiresIn.Value)
                : DateTime.UtcNow.AddMinutes(10);

            tokenStore.SetTokens(newAccess, newRefresh, newExpiry.ToString("o", CultureInfo.InvariantCulture));

            if (force)
                _log.LogInformation("Forced token refresh completed successfully after API 401.");

            return newAccess!;
        }
    }

    private async Task HandleExpiredSessionAsync(OidcTokenStore tokenStore, FrontendSessionCoordinator sessionCoordinator) {
        tokenStore.Clear();
        await sessionCoordinator.NotifySessionExpiredAsync();
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken ct) {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var option in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null) {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(ct);
            var contentClone = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
                contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            clone.Content = contentClone;
        }

        return clone;
    }

    private IServiceProvider? ResolveApplicationServices(HttpRequestMessage request) {
        if (request.Options.TryGetValue(ApplicationScopeHttpClientExtensions.ScopeKey, out var services))
            return services;

        _log.LogWarning("No application scope was attached to the outgoing request.");
        return null;
    }
}
