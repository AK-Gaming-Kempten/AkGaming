namespace AkGaming.Identity.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, bool PrivacyPolicyAccepted, string Username);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);

public sealed record EmailVerificationRequest(string Email);
public sealed record VerifyEmailRequest(string Token);
public sealed record EmailVerificationResponse(string Message, string? VerificationToken = null);

public sealed record DiscordStartResponse(string AuthorizationUrl);
public sealed record DiscordCallbackResponse(
    bool Linked,
    bool CreatedUser,
    string DiscordUserId,
    string? DiscordUsername,
    AuthResponse? Tokens,
    string Message,
    string? RedirectUri = null,
    string? State = null,
    CurrentUserResponse? User = null);

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string Username,
    bool IsEmailVerified,
    string[] Roles,
    DiscordLinkInfo? Discord);

public sealed record DiscordLinkInfo(
    string ProviderUserId,
    string? ProviderUsername,
    DateTime LinkedAtUtc);

public sealed record RedirectFinalizeRequest(
    string RedirectUri,
    string? State,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);

public sealed record RoleResponse(Guid Id, string Name);
public sealed record AdminCreateRoleRequest(string Name);
public sealed record AdminRenameRoleRequest(string Name);
public sealed record AdminSetUserRolesRequest(string[] Roles);
public sealed record UserRolesResponse(Guid UserId, string[] Roles);

public sealed record OidcClientResponse(
    string ClientId,
    string DisplayName,
    string ClientType,
    string ConsentType,
    bool RequirePkce,
    bool AllowAuthorizationCodeFlow,
    bool AllowRefreshTokenFlow,
    bool HasClientSecret,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris,
    string[] Scopes,
    bool IsProtected,
    string? ProtectionReason);

public sealed record AdminCreateOidcClientRequest(
    string ClientId,
    string? ClientSecret,
    string DisplayName,
    string ClientType,
    string ConsentType,
    bool RequirePkce,
    bool AllowAuthorizationCodeFlow,
    bool AllowRefreshTokenFlow,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris,
    string[] Scopes);

public sealed record AdminUpdateOidcClientRequest(
    string DisplayName,
    string ClientType,
    string ConsentType,
    bool RequirePkce,
    bool AllowAuthorizationCodeFlow,
    bool AllowRefreshTokenFlow,
    string? NewClientSecret,
    string[] RedirectUris,
    string[] PostLogoutRedirectUris,
    string[] Scopes);

public sealed record OidcScopeResponse(
    string Name,
    string DisplayName,
    string? Description,
    string[] Resources,
    bool IsProtected,
    string? ProtectionReason);

public sealed record AdminCreateOidcScopeRequest(
    string Name,
    string DisplayName,
    string? Description,
    string[] Resources);

public sealed record AdminUpdateOidcScopeRequest(
    string DisplayName,
    string? Description,
    string[] Resources);

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

public sealed record AdminAuditLogsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AdminAuditLogItemResponse> Items);

public sealed record AdminAuditLogItemResponse(
    Guid Id,
    Guid? UserId,
    string EventType,
    bool Success,
    string? IpAddress,
    string? SubjectEmail,
    string? Details,
    DateTime CreatedAtUtc);
