using System.Security.Claims;

namespace AkGaming.Management.Modules.Disbursements.Api.Controllers;

internal static class ControllerIdentity
{
    public static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }

    public static string GetDisplayName(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("name") ?? principal.FindFirstValue("email") ?? principal.Identity?.Name ?? "User";
    }
}
