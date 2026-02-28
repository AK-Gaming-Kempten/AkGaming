using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;

namespace Frontend.Blazor.Startup;

public static class WebApplicationExtensions {
    private const string AuthStateCookieName = "akg_auth_state";

    public static void ConfigureCultureAndLocalization(this WebApplication app) {
        var defaultCulture = new CultureInfo("en-GB");
        var localizationOptions = new RequestLocalizationOptions {
            DefaultRequestCulture = new RequestCulture(defaultCulture),
            SupportedCultures = new List<CultureInfo> { defaultCulture },
            SupportedUICultures = new List<CultureInfo> { defaultCulture }
        };
        app.UseRequestLocalization(localizationOptions);
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-GB");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-GB");
        app.Use(async (context, next) => {
            var culture = new CultureInfo("en-GB");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            await next();
        });
    }

    public static void ConfigureRequestPipeline(this WebApplication app) {
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();
        app.UseForwardedHeaders();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<Frontend.Blazor.Components.App>()
            .AddInteractiveServerRenderMode();

        app.MapGroup("/api/members").RequireAuthorization();
    }

    public static void ConfigureAuthenticationEndpoints(this WebApplication app) {
        var auth = app.MapGroup("/authentication");

        auth.MapGet("/login", (HttpContext context, IConfiguration config) => {
            return BuildProviderRedirect(context, config, "Auth:LoginPath");
        });

        auth.MapGet("/register", (HttpContext context, IConfiguration config) => {
            return BuildProviderRedirect(context, config, "Auth:RegisterPath");
        });

        auth.MapGet("/callback", (HttpContext context) => {
            context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";

            var callbackPostUrl = $"{context.Request.Scheme}://{context.Request.Host}/authentication/callback";
            var encodedCallbackPostUrl = WebUtility.HtmlEncode(callbackPostUrl);
            const string htmlTemplate = """
                                        <!doctype html>
                                        <html lang="en">
                                        <head>
                                          <meta charset="utf-8" />
                                          <meta name="viewport" content="width=device-width, initial-scale=1" />
                                          <title>Signing in...</title>
                                        </head>
                                        <body>
                                        <script>
                                        (function () {
                                          const hashParams = new URLSearchParams(window.location.hash.slice(1));
                                          const searchParams = new URLSearchParams(window.location.search);
                                          for (const [key, value] of searchParams.entries()) {
                                            if (!hashParams.has(key)) {
                                              hashParams.set(key, value);
                                            }
                                          }
                                          const form = document.createElement('form');
                                          form.method = 'POST';
                                          form.action = '__CALLBACK_POST_URL__';
                                          for (const [key, value] of hashParams.entries()) {
                                            const input = document.createElement('input');
                                            input.type = 'hidden';
                                            input.name = key;
                                            input.value = value;
                                            form.appendChild(input);
                                          }
                                          document.body.appendChild(form);
                                          form.submit();
                                        })();
                                        </script>
                                        </body>
                                        </html>
                                        """;
            var html = htmlTemplate.Replace("__CALLBACK_POST_URL__", encodedCallbackPostUrl, StringComparison.Ordinal);
            return Results.Content(html, "text/html; charset=utf-8");
        });

        auth.MapPost("/callback", async (HttpContext context, IConfiguration config, IHttpClientFactory factory) => {
            var form = context.Request.HasFormContentType ? await context.Request.ReadFormAsync(context.RequestAborted) : null;
            var accessToken = form?["access_token"].FirstOrDefault()
                ?? form?["accessToken"].FirstOrDefault()
                ?? form?["token"].FirstOrDefault()
                ?? context.Request.Query["access_token"].FirstOrDefault()
                ?? context.Request.Query["accessToken"].FirstOrDefault()
                ?? context.Request.Query["token"].FirstOrDefault();
            var refreshToken = form?["refresh_token"].FirstOrDefault()
                ?? form?["refreshToken"].FirstOrDefault()
                ?? context.Request.Query["refresh_token"].FirstOrDefault()
                ?? context.Request.Query["refreshToken"].FirstOrDefault();
            var returnedState = form?["state"].FirstOrDefault()
                ?? context.Request.Query["state"].FirstOrDefault();
            var expectedState = context.Request.Cookies[AuthStateCookieName];

            context.Response.Cookies.Delete(AuthStateCookieName);
            if (string.IsNullOrWhiteSpace(expectedState) || !string.Equals(expectedState, returnedState, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(accessToken))
                return Results.Unauthorized();

            JwtSecurityToken jwt;
            try {
                jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            } catch {
                return Results.BadRequest("Invalid access token.");
            }

            var claims = jwt.Claims.ToList();
            var subject = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!string.IsNullOrWhiteSpace(subject) && claims.All(c => c.Type != ClaimTypes.NameIdentifier))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, subject));

            if (claims.All(c => c.Type != ClaimTypes.Name) && claims.FirstOrDefault(c => c.Type == "email") is { Value: not null } emailClaim)
                claims.Add(new Claim(ClaimTypes.Name, emailClaim.Value));

            var userInfoClaims = await FetchUserInfoClaimsAsync(config, factory, accessToken, context.RequestAborted);
            foreach (var claim in userInfoClaims.Where(c => claims.All(existing => existing.Type != c.Type)))
                claims.Add(claim);

            var identity = new ClaimsIdentity(claims, "Cookies", ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties {
                IsPersistent = true
            };

            var expiresAt = ParseProviderExpiresAt(form?["expires_at"].FirstOrDefault() ?? context.Request.Query["expires_at"].FirstOrDefault())
                ?? jwt.ValidTo.ToUniversalTime();
            if (expiresAt > DateTime.UnixEpoch)
                properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

            var tokens = new List<AuthenticationToken> {
                new() { Name = "access_token", Value = accessToken }
            };
            if (!string.IsNullOrWhiteSpace(refreshToken))
                tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
            properties.StoreTokens(tokens);

            await context.SignInAsync("Cookies", principal, properties);

            var appReturnUrl = form?["returnUrl"].FirstOrDefault() ?? context.Request.Query["returnUrl"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(appReturnUrl) || !Uri.IsWellFormedUriString(appReturnUrl, UriKind.Relative))
                appReturnUrl = "/";
            return Results.Redirect(appReturnUrl);
        });

        auth.MapGet("/logout", async (HttpContext context, IConfiguration config) => {
            await context.SignOutAsync("Cookies");

            var providerLogoutUrl = BuildProviderEndpoint(config, "Auth:LogoutPath");
            if (string.IsNullOrWhiteSpace(providerLogoutUrl))
                return Results.Redirect("/");

            var returnUrl = $"{GetAppBaseUrl(context, config)}/";
            var providerReturnUrlParam = config["Auth:LogoutReturnUrlParam"] ?? "returnUrl";
            var redirectUrl = QueryHelpers.AddQueryString(providerLogoutUrl, providerReturnUrlParam, returnUrl);
            return Results.Redirect(redirectUrl);
        });
    }

    public static void ConfigureDebugEndpoints(this WebApplication app) {
        app.MapGet("/debug/token", [Authorize] async (HttpContext ctx) => {
            var accessToken = await ctx.GetTokenAsync("access_token");
            var idToken = await ctx.GetTokenAsync("id_token");

            static string Decode(string? token) {
                if (token is null) return "(null)";
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                return JsonSerializer.Serialize(jwt.Payload, new JsonSerializerOptions { WriteIndented = true });
            }

            return new { accessToken = Decode(accessToken), idToken = Decode(idToken) };
        });
    }

    private static string? BuildProviderEndpoint(IConfiguration config, string pathKey) {
        var baseUrl = config["Auth:BaseUrl"]?.TrimEnd('/');
        var path = config[pathKey]?.Trim();
        if (string.IsNullOrWhiteSpace(path) && pathKey == "Auth:RegisterPath")
            path = "/register";
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(path))
            return null;

        if (!path.StartsWith('/'))
            path = "/" + path;
        return $"{baseUrl}{path}";
    }

    private static IResult BuildProviderRedirect(HttpContext context, IConfiguration config, string providerPathKey) {
        var providerUrl = BuildProviderEndpoint(config, providerPathKey);
        if (string.IsNullOrWhiteSpace(providerUrl))
            return Results.Problem($"Auth endpoint not configured ({providerPathKey}).", statusCode: StatusCodes.Status500InternalServerError);

        var callbackUrl = $"{GetAppBaseUrl(context, config)}/authentication/callback";
        var appReturnUrl = context.Request.Query["returnUrl"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(appReturnUrl) && Uri.IsWellFormedUriString(appReturnUrl, UriKind.Relative))
            callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "returnUrl", appReturnUrl);

        var state = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(AuthStateCookieName, state, new CookieOptions {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/"
        });

        var redirectUriParam = config["Auth:LoginRedirectUriParam"] ?? "redirect_uri";
        var stateParam = config["Auth:LoginStateParam"] ?? "state";
        var redirectUrl = QueryHelpers.AddQueryString(providerUrl, redirectUriParam, callbackUrl);
        redirectUrl = QueryHelpers.AddQueryString(redirectUrl, stateParam, state);

        return Results.Redirect(redirectUrl);
    }

    private static string GetAppBaseUrl(HttpContext context, IConfiguration config) {
        var configuredBaseUrl = config["Auth:AppBaseUrl"]?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) && Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out _))
            return configuredBaseUrl;

        return $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static DateTime? ParseProviderExpiresAt(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto.UtcDateTime;

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix)) {
            // Treat large integer values as unix milliseconds, otherwise as unix seconds.
            if (unix > 10_000_000_000)
                return DateTimeOffset.FromUnixTimeMilliseconds(unix).UtcDateTime;
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        }

        return null;
    }

    private static async Task<List<Claim>> FetchUserInfoClaimsAsync(IConfiguration config, IHttpClientFactory factory, string accessToken, CancellationToken ct) {
        var meEndpoint = BuildProviderEndpoint(config, "Auth:MePath");
        if (string.IsNullOrWhiteSpace(meEndpoint))
            return [];

        try {
            using var req = new HttpRequestMessage(HttpMethod.Get, meEndpoint);
            req.Headers.Authorization = new("Bearer", accessToken);

            var client = factory.CreateClient();
            using var resp = await client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return [];

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return [];

            var claims = new List<Claim>();
            foreach (var property in doc.RootElement.EnumerateObject()) {
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null or JsonValueKind.Undefined)
                    continue;

                var value = property.Value.ToString();
                if (string.IsNullOrWhiteSpace(value)) continue;
                claims.Add(new Claim(property.Name, value));
            }

            return claims;
        } catch {
            return [];
        }
    }
}
