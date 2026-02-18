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

        if (TryParseWildcardAllowedEntry(allowedEntry, out var wildcard))
        {
            if (!string.Equals(candidate.Scheme, wildcard.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (wildcard.Port.HasValue && candidate.Port != wildcard.Port.Value)
            {
                return false;
            }

            if (!candidate.Host.EndsWith("." + wildcard.BaseHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(candidate.Host, wildcard.BaseHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (wildcard.Path == "/" || string.IsNullOrWhiteSpace(wildcard.Path))
            {
                return true;
            }

            return string.Equals(candidate.AbsolutePath.TrimEnd('/'), wildcard.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        if (!Uri.TryCreate(allowedEntry, UriKind.Absolute, out var allowedUri))
        {
            return false;
        }

        if (!string.Equals(candidate.Scheme, allowedUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(
            candidate.GetLeftPart(UriPartial.Path).TrimEnd('/'),
            allowedUri.GetLeftPart(UriPartial.Path).TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseWildcardAllowedEntry(string allowedEntry, out WildcardAllowedEntry wildcard)
    {
        wildcard = default;

        var schemeSeparator = allowedEntry.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator <= 0)
        {
            return false;
        }

        var scheme = allowedEntry[..schemeSeparator];
        if (!scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            && !scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var authorityAndPath = allowedEntry[(schemeSeparator + 3)..];
        var pathSeparator = authorityAndPath.IndexOf('/');
        var authority = pathSeparator >= 0 ? authorityAndPath[..pathSeparator] : authorityAndPath;
        var path = pathSeparator >= 0 ? authorityAndPath[pathSeparator..] : "/";

        if (!authority.StartsWith("*.", StringComparison.Ordinal))
        {
            return false;
        }

        var hostWithOptionalPort = authority[2..];
        string baseHost;
        int? port = null;

        var lastColon = hostWithOptionalPort.LastIndexOf(':');
        if (lastColon > 0 && int.TryParse(hostWithOptionalPort[(lastColon + 1)..], out var parsedPort))
        {
            baseHost = hostWithOptionalPort[..lastColon];
            port = parsedPort;
        }
        else
        {
            baseHost = hostWithOptionalPort;
        }

        if (string.IsNullOrWhiteSpace(baseHost))
        {
            return false;
        }

        wildcard = new WildcardAllowedEntry(scheme, baseHost, port, string.IsNullOrWhiteSpace(path) ? "/" : path);
        return true;
    }

    private readonly record struct WildcardAllowedEntry(string Scheme, string BaseHost, int? Port, string Path);

    internal static string BuildExternalRedirectUrl(RedirectFinalizeRequest request)
    {
        if (!Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("redirectUri must be absolute.");
        }

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

        var builder = new UriBuilder(baseUri);
        var existingFragment = baseUri.Fragment.TrimStart('#');
        var newFragment = string.IsNullOrWhiteSpace(existingFragment)
            ? string.Join("&", pairs)
            : $"{existingFragment}&{string.Join("&", pairs)}";
        builder.Fragment = newFragment;
        return builder.Uri.AbsoluteUri;
    }
}
