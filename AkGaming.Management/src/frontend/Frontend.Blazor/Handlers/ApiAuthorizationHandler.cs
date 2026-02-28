using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Frontend.Blazor.Handlers;

public class ApiAuthorizationHandler : DelegatingHandler {
    private readonly IHttpContextAccessor _http;
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _cfg;
    private readonly ILogger<ApiAuthorizationHandler> _log;

    public ApiAuthorizationHandler(
        IHttpContextAccessor http,
        IHttpClientFactory factory,
        IConfiguration cfg,
        ILogger<ApiAuthorizationHandler> log) {
        _http = http;
        _factory = factory;
        _cfg = cfg;
        _log = log;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
        var context = _http.HttpContext;
        if (context == null) return await base.SendAsync(request, ct);

        var accessToken = await context.GetTokenAsync("access_token");
        var refreshToken = await context.GetTokenAsync("refresh_token");

        if (string.IsNullOrEmpty(accessToken))
            return await base.SendAsync(request, ct);

        // Refresh if token expired or nearly expired
        if (IsExpired(accessToken)) {
            _log.LogInformation("Access token expired, refreshing...");
            if (context.Response.HasStarted) {
                _log.LogWarning("Cannot refresh token because response has already started and refreshed tokens cannot be persisted.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            if (string.IsNullOrWhiteSpace(refreshToken)) {
                _log.LogWarning("Access token is expired but no refresh token is present. Signing out user.");
                await TrySignOutAsync(context, "Access token expired and refresh token is missing.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            var tokenEndpoint = ResolveTokenEndpoint();
            if (string.IsNullOrWhiteSpace(tokenEndpoint)) {
                _log.LogWarning("No token endpoint configured/discovered. Signing out user.");
                await TrySignOutAsync(context, "No token endpoint configured/discovered.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            var client = _factory.CreateClient();
            var payload = new Dictionary<string, string?> {
                ["refresh_token"] = refreshToken,
                ["refreshToken"] = refreshToken,
                ["access_token"] = accessToken,
                ["accessToken"] = accessToken
            };

            HttpResponseMessage resp;
            try {
                using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) {
                    Content = new StringContent(JsonSerializer.Serialize(payload))
                };
                refreshRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                refreshRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                resp = await client.SendAsync(refreshRequest, ct);
            } catch (Exception ex) {
                _log.LogError(ex, "Failed to call token endpoint");
                await TrySignOutAsync(context, "Failed to call token endpoint.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            var responseContent = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) {
                var bodyPreview = responseContent.Length > 512 ? responseContent[..512] + "..." : responseContent;
                _log.LogWarning("Token refresh failed: {Status}. Body: {Body}", resp.StatusCode, bodyPreview);
                await TrySignOutAsync(context, $"Token refresh failed with status {(int)resp.StatusCode}.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            using var json = JsonDocument.Parse(responseContent);
            var root = json.RootElement;

            var newAccess = GetTokenValue(root, "access_token", "accessToken");
            var newRefresh = GetTokenValue(root, "refresh_token", "refreshToken") ?? refreshToken;
            var expiresIn = GetIntegerValue(root, "expires_in", "expiresIn");

            if (string.IsNullOrWhiteSpace(newAccess)) {
                _log.LogWarning("Token refresh succeeded but response did not contain an access token.");
                await TrySignOutAsync(context, "Token refresh response missing access token.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            var newExpiry = expiresIn.HasValue
                ? DateTime.UtcNow.AddSeconds(expiresIn.Value)
                : ReadExpiry(newAccess);

            // Save updated tokens in cookie
            var auth = await context.AuthenticateAsync("Cookies");
            if (!auth.Succeeded || auth.Principal == null || auth.Properties == null) {
                _log.LogWarning("Cookie authentication state missing during token refresh.");
                await TrySignOutAsync(context, "Cookie authentication state missing during token refresh.");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            auth.Properties.UpdateTokenValue("access_token", newAccess);
            auth.Properties.UpdateTokenValue("refresh_token", newRefresh);
            auth.Properties.UpdateTokenValue("expires_at", newExpiry.ToString("o", CultureInfo.InvariantCulture));
            await TrySignInAsync(context, auth.Principal, auth.Properties);

            accessToken = newAccess!;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, ct);
    }

    private string? ResolveTokenEndpoint() {
        var configuredTokenEndpoint = _cfg["Auth:RefreshEndpoint"];
        if (!string.IsNullOrWhiteSpace(configuredTokenEndpoint))
            return configuredTokenEndpoint;

        var baseUrl = _cfg["Auth:BaseUrl"]?.TrimEnd('/');
        var refreshPath = _cfg["Auth:RefreshPath"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(refreshPath))
            return null;
        if (!refreshPath.StartsWith('/'))
            refreshPath = "/" + refreshPath;
        return $"{baseUrl}{refreshPath}";
    }

    private static bool IsExpired(string token) {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        // refresh 30 seconds before actual expiry
        return jwt.ValidTo.ToUniversalTime() <= DateTime.UtcNow.AddSeconds(30);
    }

    private static DateTime ReadExpiry(string token) {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var validTo = jwt.ValidTo.ToUniversalTime();
        return validTo > DateTime.UnixEpoch ? validTo : DateTime.UtcNow.AddMinutes(10);
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

    private async Task TrySignOutAsync(HttpContext context, string reason) {
        if (context.Response.HasStarted) {
            _log.LogWarning("Skipping cookie sign-out because response has already started. Reason: {Reason}", reason);
            return;
        }

        await context.SignOutAsync("Cookies");
    }

    private async Task TrySignInAsync(HttpContext context, ClaimsPrincipal principal, AuthenticationProperties properties) {
        if (context.Response.HasStarted) {
            _log.LogWarning("Skipping cookie token persistence because response has already started.");
            return;
        }

        await context.SignInAsync("Cookies", principal, properties);
    }
}
