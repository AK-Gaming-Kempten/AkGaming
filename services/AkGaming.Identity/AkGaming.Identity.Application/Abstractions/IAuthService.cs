using AkGaming.Identity.Application.Auth;

namespace AkGaming.Identity.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserRolesResponse> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserRolesResponse> SetUserRolesAsync(Guid actorUserId, Guid userId, AdminSetUserRolesRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);
    Task<RoleResponse> CreateRoleAsync(Guid actorUserId, AdminCreateRoleRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<RoleResponse> RenameRoleAsync(Guid actorUserId, Guid roleId, AdminRenameRoleRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task DeleteRoleAsync(Guid actorUserId, Guid roleId, string? ipAddress, CancellationToken cancellationToken);
    Task<EmailVerificationResponse> RequestEmailVerificationAsync(EmailVerificationRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<EmailVerificationResponse> RequestEmailVerificationForUserAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken);
    Task VerifyEmailAsync(VerifyEmailRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<DiscordStartResponse> GetDiscordStartUrlAsync(CancellationToken cancellationToken);
    Task<DiscordStartResponse> GetDiscordLinkUrlAsync(Guid userId, CancellationToken cancellationToken);
    Task<DiscordCallbackResponse> HandleDiscordCallbackAsync(string code, string state, string? ipAddress, CancellationToken cancellationToken);
}
