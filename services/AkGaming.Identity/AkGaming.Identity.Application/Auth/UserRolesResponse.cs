namespace AkGaming.Identity.Application.Auth;

public sealed record UserRolesResponse(Guid UserId, string[] Roles);
