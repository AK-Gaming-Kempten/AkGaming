using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Domain.Constants;
using AkGaming.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AkGaming.Identity.Infrastructure.Persistence;

public sealed class DevelopmentAdminSeeder
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly DevelopmentAdminSeedOptions _options;

    public DevelopmentAdminSeeder(
        AuthDbContext dbContext,
        IPasswordHasherService passwordHasher,
        IOptions<DevelopmentAdminSeedOptions> options)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var email = _options.Email.Trim().ToLowerInvariant();
        var username = _options.Username.Trim();
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "DevelopmentAdmin:Email, DevelopmentAdmin:Username, and DevelopmentAdmin:Password must be configured to seed the development admin.");
        }

        var adminRole = await _dbContext.Roles
            .SingleOrDefaultAsync(x => x.Name == RoleNames.Admin, cancellationToken);
        if (adminRole is null)
        {
            throw new InvalidOperationException("The Admin role must be seeded before the development admin user.");
        }

        var user = await _dbContext.Users
            .Include(x => x.UserRoles)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                Username = username,
                IsEmailVerified = true,
                PrivacyPolicyAccepted = true,
                PrivacyPolicyAcceptedAtUtc = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, _options.Password);
            _dbContext.Users.Add(user);
        }

        if (!user.UserRoles.Any(x => x.RoleId == adminRole.Id))
        {
            user.UserRoles.Add(new UserRole { User = user, Role = adminRole });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
