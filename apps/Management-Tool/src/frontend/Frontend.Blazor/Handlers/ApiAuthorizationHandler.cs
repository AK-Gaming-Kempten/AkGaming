using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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

        var accessToken = await context.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
        var refreshToken = await context.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

        if (string.IsNullOrEmpty(accessToken))
            return await base.SendAsync(request, ct);

        // Refresh if token expired or nearly expired
        if (IsExpired(accessToken)) {
            _log.LogInformation("Access token expired, refreshing...");

            var tokenEndpoint = $"{_cfg["Oidc:Authority"]}/protocol/openid-connect/token";
            var client = _factory.CreateClient();

            var form = new Dictionary<string, string> {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _cfg["Oidc:ClientId"]!,
                ["refresh_token"] = refreshToken!
            };

            // include secret if this is a confidential client
            var secret = _cfg["Oidc:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(secret))
                form["client_secret"] = secret;

            HttpResponseMessage resp;
            try {
                resp = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
            } catch (Exception ex) {
                _log.LogError(ex, "Failed to call token endpoint");
                await context.SignOutAsync();
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            if (!resp.IsSuccessStatusCode) {
                _log.LogWarning("Token refresh failed: {Status}", resp.StatusCode);
                await context.SignOutAsync();
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }

            using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = json.RootElement;

            var newAccess = root.GetProperty("access_token").GetString();
            var newRefresh = root.TryGetProperty("refresh_token", out var r) ? r.GetString() : refreshToken;
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            // Save updated tokens in cookie
            var auth = await context.AuthenticateAsync();
            auth.Properties.UpdateTokenValue("access_token", newAccess);
            auth.Properties.UpdateTokenValue("refresh_token", newRefresh);
            auth.Properties.UpdateTokenValue("expires_at",
                DateTime.UtcNow.AddSeconds(expiresIn)
                    .ToString("o", CultureInfo.InvariantCulture));
            await context.SignInAsync(auth.Principal, auth.Properties);

            accessToken = newAccess!;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, ct);
    }

    private static bool IsExpired(string token) {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        // refresh 30 seconds before actual expiry
        return jwt.ValidTo.ToUniversalTime() <= DateTime.UtcNow.AddSeconds(30);
    }
}
