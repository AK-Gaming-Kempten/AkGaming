using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;

namespace AkGaming.Management.Frontend.Startup;

public static class WebApplicationExtensions {
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
        app.MapRazorComponents<AkGaming.Management.Frontend.Components.App>()
            .AddInteractiveServerRenderMode();

        app.MapGroup("/api/members").RequireAuthorization();
    }

    public static void ConfigureAuthenticationEndpoints(this WebApplication app) {
        var auth = app.MapGroup("/authentication");

        auth.MapGet("/login", (HttpContext context, string? returnUrl) =>
                Results.Challenge(
                    BuildLoginProperties(context, returnUrl),
                    [OpenIdConnectDefaults.AuthenticationScheme]))
            .AllowAnonymous();

        auth.MapGet("/register", (HttpContext context, string? returnUrl) =>
                Results.Redirect($"/authentication/login?returnUrl={Uri.EscapeDataString(NormalizeReturnUrl(context, returnUrl))}"))
            .AllowAnonymous();

        auth.MapGet("/logout", (HttpContext context, string? returnUrl) =>
                Results.SignOut(
                    new AuthenticationProperties { RedirectUri = NormalizeReturnUrl(context, returnUrl) },
                    [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .AllowAnonymous();
    }

    public static void ConfigureDebugEndpoints(this WebApplication app) {
        app.MapGet("/debug/token", [Authorize] async (HttpContext ctx) => {
            var accessToken = await ctx.GetTokenAsync("access_token");
            var refreshToken = await ctx.GetTokenAsync("refresh_token");
            var idToken = await ctx.GetTokenAsync("id_token");
            var expiresAt = await ctx.GetTokenAsync("expires_at");

            static string DescribeToken(string? token) {
                if (token is null) return "(null)";
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return "(present, non-JWT or encrypted token)";

                try {
                    var jwt = handler.ReadJwtToken(token);
                    return JsonSerializer.Serialize(jwt.Payload, new JsonSerializerOptions { WriteIndented = true });
                } catch {
                    return "(present, unreadable token)";
                }
            }

            return new {
                accessToken = DescribeToken(accessToken),
                refreshToken = string.IsNullOrWhiteSpace(refreshToken) ? "(null)" : "(present)",
                idToken = DescribeToken(idToken),
                expiresAt
            };
        });
    }

    private static AuthenticationProperties BuildLoginProperties(HttpContext context, string? returnUrl) {
        return new AuthenticationProperties {
            RedirectUri = NormalizeReturnUrl(context, returnUrl)
        };
    }

    private static string NormalizeReturnUrl(HttpContext context, string? returnUrl) {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return "/";

        if (Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) && returnUrl.StartsWith("/", StringComparison.Ordinal))
            return returnUrl;

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute)
            && string.Equals(absolute.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase)) {
            var normalized = absolute.PathAndQuery + absolute.Fragment;
            return string.IsNullOrWhiteSpace(normalized) ? "/" : normalized;
        }

        return "/";
    }
}
