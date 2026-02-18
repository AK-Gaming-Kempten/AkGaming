using System.Security.Claims;
using AkGaming.Identity.Application.Auth;

namespace AkGaming.Identity.Api.Endpoints;

internal static class EndpointUtilities
{
    internal static string? GetIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString();
    }

    internal static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(userIdClaim, out userId);
    }

    internal static string BuildDiscordCallbackFragment(
        bool success,
        string message,
        string? accessToken = null,
        string? refreshToken = null,
        bool? linked = null,
        bool? createdUser = null,
        string? errorCode = null)
    {
        var parts = new List<string>
        {
            $"success={(success ? "1" : "0")}",
            $"message={Uri.EscapeDataString(message)}"
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            parts.Add($"accessToken={Uri.EscapeDataString(accessToken)}");
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            parts.Add($"refreshToken={Uri.EscapeDataString(refreshToken)}");
        }

        if (linked.HasValue)
        {
            parts.Add($"linked={(linked.Value ? "1" : "0")}");
        }

        if (createdUser.HasValue)
        {
            parts.Add($"createdUser={(createdUser.Value ? "1" : "0")}");
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            parts.Add($"errorCode={Uri.EscapeDataString(errorCode)}");
        }

        return string.Join("&", parts);
    }

    internal static bool IsAllowedRedirectUri(string redirectUri, IConfiguration configuration)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("https" or "http"))
        {
            return false;
        }

        var allowed = configuration.GetSection("Bridge:AllowedRedirectUris").Get<string[]>() ?? [];
        return allowed.Any(x => string.Equals(x?.Trim(), redirectUri.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    internal static string BuildExternalRedirectUrl(RedirectFinalizeRequest request)
    {
        var pairs = new List<string>
        {
            $"access_token={Uri.EscapeDataString(request.AccessToken)}",
            $"refresh_token={Uri.EscapeDataString(request.RefreshToken)}",
            $"expires_at={Uri.EscapeDataString(request.AccessTokenExpiresAtUtc.ToString("O"))}"
        };

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            pairs.Add($"state={Uri.EscapeDataString(request.State)}");
        }

        var separator = request.RedirectUri.Contains('#') ? "&" : "#";
        return $"{request.RedirectUri}{separator}{string.Join("&", pairs)}";
    }
}
