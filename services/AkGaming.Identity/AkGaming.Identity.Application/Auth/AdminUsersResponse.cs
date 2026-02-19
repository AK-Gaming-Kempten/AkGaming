namespace AkGaming.Identity.Application.Auth;

public sealed record AdminUsersResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AdminUserListItemResponse> Items);

public sealed record AdminUserListItemResponse(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    string[] Roles,
    DateTime CreatedAtUtc,
    DateTime? LockoutEndUtc);

public sealed record AdminUserDetailsResponse(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    string[] Roles,
    DateTime CreatedAtUtc,
    DateTime? LockoutEndUtc,
    DiscordLinkInfo? Discord);
