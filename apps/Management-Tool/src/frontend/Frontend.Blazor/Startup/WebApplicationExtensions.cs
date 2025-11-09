using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;

namespace Frontend.Blazor.Startup;

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
        app.MapRazorComponents<Frontend.Blazor.Components.App>()
            .AddInteractiveServerRenderMode();

        app.MapGroup("/api/members").RequireAuthorization();
    }

    public static void ConfigureAuthenticationEndpoints(this WebApplication app) {
        var auth = app.MapGroup("/authentication");

        auth.MapGet("/login", async (HttpContext context) => {
            var props = new AuthenticationProperties { RedirectUri = "/" };
            await context.ChallengeAsync("oidc", props);
        });

        auth.MapGet("/logout", async (HttpContext context) => {
            var props = new AuthenticationProperties { RedirectUri = "/" };
            await context.SignOutAsync("Cookies", props);
            await context.SignOutAsync("oidc", props);
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
}
