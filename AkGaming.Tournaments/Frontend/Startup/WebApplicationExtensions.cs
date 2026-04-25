using System.Globalization;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;

namespace AkGaming.Tournaments.Frontend.Startup;

public static class WebApplicationExtensions
{
    public static void ConfigureCultureAndLocalization(this WebApplication app)
    {
        var defaultCulture = new CultureInfo("en-GB");
        var localizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(defaultCulture),
            SupportedCultures = [defaultCulture],
            SupportedUICultures = [defaultCulture]
        };

        app.UseRequestLocalization(localizationOptions);
        CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

        app.Use(async (_, next) =>
        {
            CultureInfo.CurrentCulture = defaultCulture;
            CultureInfo.CurrentUICulture = defaultCulture;
            await next();
        });
    }

    public static void ConfigureRequestPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapGet("/media-assets/{mediaAssetId:guid}/file", async (
                Guid mediaAssetId,
                IHttpClientFactory httpClientFactory,
                CancellationToken cancellationToken) =>
            {
                var client = httpClientFactory.CreateClient(nameof(MediaAssetsApiClient));
                using var response = await client.GetAsync($"api/media-assets/{mediaAssetId}/file", cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return Results.StatusCode((int)response.StatusCode);

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                return Results.File(bytes, contentType);
            })
            .AllowAnonymous();

        app.MapStaticAssets();
        app.MapRazorComponents<AkGaming.Tournaments.Frontend.Components.App>()
            .AddInteractiveServerRenderMode();
    }

    public static void ConfigureAuthenticationEndpoints(this WebApplication app)
    {
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

    private static AuthenticationProperties BuildLoginProperties(HttpContext context, string? returnUrl)
    {
        return new AuthenticationProperties
        {
            RedirectUri = NormalizeReturnUrl(context, returnUrl)
        };
    }

    private static string NormalizeReturnUrl(HttpContext context, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return "/";

        if (Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) &&
            returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return returnUrl;
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute) &&
            string.Equals(absolute.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            var normalized = absolute.PathAndQuery + absolute.Fragment;
            return string.IsNullOrWhiteSpace(normalized) ? "/" : normalized;
        }

        return "/";
    }
}
