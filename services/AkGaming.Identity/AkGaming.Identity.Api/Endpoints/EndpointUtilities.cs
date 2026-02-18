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

    internal static bool IsAllowedRedirectUri(
        string redirectUri,
        IConfiguration configuration,
        out string reason,
        out IReadOnlyList<string> evaluations)
    {
        var checks = new List<string>();

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var candidate))
        {
            reason = "invalid_uri";
            evaluations = checks;
            return false;
        }

        if (candidate.Scheme is not ("https" or "http"))
        {
            reason = "invalid_scheme";
            evaluations = checks;
            return false;
        }

        var allowed = configuration.GetSection("Bridge:AllowedRedirectUris").Get<string[]>() ?? [];
        foreach (var entry in allowed)
        {
            var matched = MatchesAllowedRedirect(candidate, entry);
            checks.Add($"[{entry}]=>{(matched ? "match" : "no_match")}");
            if (matched)
            {
                reason = "matched_allowlist";
                evaluations = checks;
                return true;
            }
        }

        reason = "not_in_allowlist";
        evaluations = checks;
        return false;
    }

    private static bool MatchesAllowedRedirect(Uri candidate, string? allowedEntryRaw)
    {
        var allowedEntry = allowedEntryRaw?.Trim();
        if (string.IsNullOrWhiteSpace(allowedEntry))
        {
            return false;
        }

        if (!Uri.TryCreate(allowedEntry, UriKind.Absolute, out var allowedUri))
        {
            return false;
        }

        if (!string.Equals(candidate.Scheme, allowedUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HasWildcardHost(allowedUri.Host))
        {
            return MatchesWildcardHost(candidate, allowedUri);
        }

        return string.Equals(
            candidate.GetLeftPart(UriPartial.Path).TrimEnd('/'),
            allowedUri.GetLeftPart(UriPartial.Path).TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasWildcardHost(string host)
    {
        return host.StartsWith("*.", StringComparison.Ordinal) && host.Count(ch => ch == '*') == 1;
    }

    private static bool MatchesWildcardHost(Uri candidate, Uri allowedUri)
    {
        var wildcardBaseHost = allowedUri.Host[2..];

        if (!candidate.Host.EndsWith("." + wildcardBaseHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(candidate.Host, wildcardBaseHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (candidate.Port != allowedUri.Port)
        {
            return false;
        }

        var allowedPath = allowedUri.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(allowedPath) || allowedPath == "/")
        {
            return true;
        }

        return string.Equals(
            candidate.AbsolutePath.TrimEnd('/'),
            allowedPath,
            StringComparison.OrdinalIgnoreCase);
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
