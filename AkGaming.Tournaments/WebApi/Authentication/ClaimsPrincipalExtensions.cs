using System.Security.Claims;

namespace AkGaming.Tournaments.WebApi.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("The authenticated user does not contain a subject identifier.");

        return userId.Trim();
    }
}
