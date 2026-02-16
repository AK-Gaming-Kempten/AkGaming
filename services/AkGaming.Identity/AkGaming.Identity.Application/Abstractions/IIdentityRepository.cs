using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.Abstractions;

public interface IIdentityRepository
{
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken);
    Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken);
    Task<EmailVerificationToken?> GetEmailVerificationTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<List<EmailVerificationToken>> GetActiveEmailVerificationTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken);
    Task AddExternalLoginAsync(ExternalLogin externalLogin, CancellationToken cancellationToken);
    Task AddEmailVerificationTokenAsync(EmailVerificationToken emailVerificationToken, CancellationToken cancellationToken);
    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
