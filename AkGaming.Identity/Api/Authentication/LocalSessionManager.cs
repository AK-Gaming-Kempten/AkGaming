using System.Security.Claims;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AkGaming.Identity.Api.Authentication;

internal static class LocalSessionManager
{
    internal static async Task SignInAsync(HttpContext context, CurrentUserResponse user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new("sub", user.UserId.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email)
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
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    internal static Task SignOutAsync(HttpContext context)
    {
        return context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
}
