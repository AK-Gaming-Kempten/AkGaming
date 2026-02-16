namespace AkGaming.Identity.Application.Auth;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    string[] Roles,
    DiscordLinkInfo? Discord);

public sealed record DiscordLinkInfo(
    string ProviderUserId,
    string? ProviderUsername,
    DateTime LinkedAtUtc);
