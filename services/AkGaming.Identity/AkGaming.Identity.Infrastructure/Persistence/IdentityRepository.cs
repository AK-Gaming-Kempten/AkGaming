using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Identity.Infrastructure.Persistence;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly AuthDbContext _dbContext;

    public IdentityRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.ExternalLogins)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.ExternalLogins)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetUserByIdWithExternalLoginsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.ExternalLogins)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<List<User>> GetUsersPageAsync(int skip, int take, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Email.ToLower().Contains(needle));
        }

        return query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUsersAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Email.ToLower().Contains(needle));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<List<AuditLog>> GetAuditLogsPageAsync(int skip, int take, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.EventType.ToLower().Contains(needle)
                || (x.SubjectEmail != null && x.SubjectEmail.ToLower().Contains(needle))
                || (x.Details != null && x.Details.ToLower().Contains(needle)));
        }

        return query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAuditLogsAsync(string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.EventType.ToLower().Contains(needle)
                || (x.SubjectEmail != null && x.SubjectEmail.ToLower().Contains(needle))
                || (x.Details != null && x.Details.ToLower().Contains(needle)));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Roles
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return _dbContext.Roles.SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);
    }

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken)
    {
        return _dbContext.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken);
    }

    public Task<List<Role>> GetRolesByNamesAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        return _dbContext.Roles
            .Where(x => roleNames.Contains(x.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        return _dbContext.UserRoles
            .Where(x => x.Role.Name == roleName)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public Task<int> CountUsersWithRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return _dbContext.UserRoles
            .Where(x => x.RoleId == roleId)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public Task<ExternalLogin?> GetExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken)
    {
        return _dbContext.ExternalLogins
            .SingleOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == providerUserId, cancellationToken);
    }

    public Task<EmailVerificationToken?> GetEmailVerificationTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.EmailVerificationTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return _dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Include(x => x.User)
            .ThenInclude(x => x.ExternalLogins)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public Task<List<RefreshToken>> GetActiveRefreshTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public Task<List<EmailVerificationToken>> GetActiveEmailVerificationTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.EmailVerificationTokens
            .Where(x => x.UserId == userId && x.ConsumedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddRoleAsync(Role role, CancellationToken cancellationToken)
    {
        await _dbContext.Roles.AddAsync(role, cancellationToken);
    }

    public void RemoveRole(Role role)
    {
        _dbContext.Roles.Remove(role);
    }

    public async Task AddExternalLoginAsync(ExternalLogin externalLogin, CancellationToken cancellationToken)
    {
        await _dbContext.ExternalLogins.AddAsync(externalLogin, cancellationToken);
    }

    public async Task AddEmailVerificationTokenAsync(EmailVerificationToken emailVerificationToken, CancellationToken cancellationToken)
    {
        await _dbContext.EmailVerificationTokens.AddAsync(emailVerificationToken, cancellationToken);
    }

    public async Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
