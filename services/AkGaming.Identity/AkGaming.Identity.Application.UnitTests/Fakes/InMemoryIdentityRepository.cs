using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class InMemoryIdentityRepository : IIdentityRepository
{
    public List<User> Users { get; } = [];
    public List<Role> Roles { get; } = [];
    public List<ExternalLogin> ExternalLogins { get; } = [];
    public List<RefreshToken> RefreshTokens { get; } = [];
    public List<EmailVerificationToken> EmailVerificationTokens { get; } = [];
    public List<AuditLog> AuditLogs { get; } = [];

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.SingleOrDefault(x => x.Email == email));
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.SingleOrDefault(x => x.Id == userId));
    }

    public Task<User?> GetUserByIdWithExternalLoginsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.SingleOrDefault(x => x.Id == userId));
    }

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Roles.SingleOrDefault(x => x.Name == roleName));
    }

    public Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken)
    {
        return Task.FromResult(ExternalLogins.SingleOrDefault(x => x.Provider == provider && x.ProviderUserId == providerUserId));
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(RefreshTokens.SingleOrDefault(x => x.TokenHash == tokenHash));
    }

    public Task<EmailVerificationToken?> GetEmailVerificationTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(EmailVerificationTokens.SingleOrDefault(x => x.TokenHash == tokenHash));
    }

    public Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).ToList();
        return Task.FromResult(tokens);
    }

    public Task<List<EmailVerificationToken>> GetActiveEmailVerificationTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = EmailVerificationTokens.Where(x => x.UserId == userId && x.ConsumedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow).ToList();
        return Task.FromResult(tokens);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken)
    {
        Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task AddExternalLoginAsync(ExternalLogin externalLogin, CancellationToken cancellationToken)
    {
        var user = Users.Single(x => x.Id == externalLogin.UserId);
        externalLogin.User = user;
        user.ExternalLogins.Add(externalLogin);
        ExternalLogins.Add(externalLogin);
        return Task.CompletedTask;
    }

    public Task AddEmailVerificationTokenAsync(EmailVerificationToken emailVerificationToken, CancellationToken cancellationToken)
    {
        var user = Users.Single(x => x.Id == emailVerificationToken.UserId);
        emailVerificationToken.User = user;
        EmailVerificationTokens.Add(emailVerificationToken);
        return Task.CompletedTask;
    }

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        var user = Users.Single(x => x.Id == refreshToken.UserId);
        refreshToken.User = user;
        RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
