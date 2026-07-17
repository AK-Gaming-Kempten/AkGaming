using System.Security.Claims;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AkGaming.Identity.Api.Authentication;

internal static class LocalSessionManager
{
    internal static async Task SignInAsync(HttpContext context, CurrentUserResponse user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new("sub", user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("preferred_username", user.Username),
            new("username", user.Username),
            new("email_verified", user.IsEmailVerified ? "true" : "false", ClaimValueTypes.Boolean)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(GetSessionLifetime(context))
            });
    }

    internal static Task SignOutAsync(HttpContext context)
    {
        return context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    internal static bool IsEmailVerified(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("email_verified");
        return bool.TryParse(raw, out var verified) && verified;
    }

    internal static string GetPostSignInRedirect(HttpContext context, CurrentUserResponse user, string? returnUrl, bool requireVerifiedEmailForLogin)
    {
        return !requireVerifiedEmailForLogin || user.IsEmailVerified
            ? NormalizeReturnUrl(context, returnUrl)
            : BuildVerificationRedirect(context, returnUrl);
    }

    internal static string GetAuthenticatedRedirect(HttpContext context, ClaimsPrincipal principal, string? returnUrl, bool requireVerifiedEmailForLogin)
    {
        return !requireVerifiedEmailForLogin || IsEmailVerified(principal)
            ? NormalizeReturnUrl(context, returnUrl)
            : BuildVerificationRedirect(context, returnUrl);
    }

    internal static string BuildVerificationRedirect(HttpContext context, string? returnUrl, string? status = null)
    {
        var redirect = "/account/verify";
        var normalizedReturnUrl = NormalizeReturnUrl(context, returnUrl);
        redirect = QueryHelpers.AddQueryString(redirect, "returnUrl", normalizedReturnUrl);

        if (!string.IsNullOrWhiteSpace(status))
            redirect = QueryHelpers.AddQueryString(redirect, "status", status);

        return redirect;
    }

    internal static string NormalizeReturnUrl(HttpContext context, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/account/manage";
        }

        if (Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) && returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return returnUrl;
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute)
            && string.Equals(absolute.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return absolute.PathAndQuery + absolute.Fragment;
        }

        return "/account/manage";
    }

    private static TimeSpan GetSessionLifetime(HttpContext context)
    {
        var jwtOptions = context.RequestServices.GetService<IOptions<JwtOptions>>()?.Value ?? new JwtOptions();
        return TimeSpan.FromDays(Math.Max(1, jwtOptions.RefreshTokenDays));
    }
}
