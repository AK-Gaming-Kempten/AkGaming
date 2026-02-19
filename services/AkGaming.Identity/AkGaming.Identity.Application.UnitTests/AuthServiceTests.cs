using AkGaming.Identity.Application.Auth;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Application.UnitTests.Fakes;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Constants;
using Microsoft.Extensions.Logging.Abstractions;

namespace AkGaming.Identity.Application.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserWithDefaultRole_AndReturnsTokens()
    {
        var repository = new InMemoryIdentityRepository();
        var service = BuildService(repository);

        var result = await service.RegisterAsync(new RegisterRequest("user@test.local", "Password123"), "127.0.0.1", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Single(repository.Users);
        Assert.Single(repository.Roles);
        Assert.Equal(RoleNames.User, repository.Roles.Single().Name);
        Assert.Single(repository.RefreshTokens);
        Assert.Equal("user@test.local", repository.Users.Single().Email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorized()
    {
        var repository = new InMemoryIdentityRepository();
        var hasher = new PasswordHasherStub();
        var user = new AkGaming.Identity.Domain.Entities.User
        {
            Email = "user@test.local",
            PasswordHash = hasher.HashPassword(new AkGaming.Identity.Domain.Entities.User(), "Password123")
        };

        repository.Users.Add(user);

        var service = BuildService(repository, hasher);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            service.LoginAsync(new LoginRequest("user@test.local", "WrongPassword"), "127.0.0.1", CancellationToken.None));

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_AfterThreshold_LocksUser()
    {
        var repository = new InMemoryIdentityRepository();
        var hasher = new PasswordHasherStub();
        var user = new AkGaming.Identity.Domain.Entities.User
        {
            Email = "lock@test.local",
            PasswordHash = hasher.HashPassword(new AkGaming.Identity.Domain.Entities.User(), "Password123")
        };

        repository.Users.Add(user);

        var service = BuildService(
            repository,
            hasher,
            hardeningSettings: new AuthHardeningSettingsStub { MaxFailedLoginAttempts = 2, LockoutMinutes = 5 });

        await Assert.ThrowsAsync<AuthException>(() =>
            service.LoginAsync(new LoginRequest("lock@test.local", "wrong"), "127.0.0.1", CancellationToken.None));

        var second = await Assert.ThrowsAsync<AuthException>(() =>
            service.LoginAsync(new LoginRequest("lock@test.local", "wrong"), "127.0.0.1", CancellationToken.None));

        Assert.Equal(401, second.StatusCode);
        Assert.NotNull(user.LockoutEndUtc);

        var locked = await Assert.ThrowsAsync<AuthException>(() =>
            service.LoginAsync(new LoginRequest("lock@test.local", "Password123"), "127.0.0.1", CancellationToken.None));

        Assert.Equal(423, locked.StatusCode);
    }

    [Fact]
    public async Task RefreshAsync_WhenRevokedTokenIsReused_RevokesAllActiveTokens()
    {
        var repository = new InMemoryIdentityRepository();
        var refresh = new RefreshTokenServiceStub();
        var user = new AkGaming.Identity.Domain.Entities.User
        {
            Email = "user@test.local",
            PasswordHash = "hash::Password123"
        };

        repository.Users.Add(user);

        var reusedTokenRaw = "stolen-refresh";
        var reusedToken = new AkGaming.Identity.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = refresh.HashToken(reusedTokenRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        var activeToken = new AkGaming.Identity.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = refresh.HashToken("active-refresh"),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };

        repository.RefreshTokens.Add(reusedToken);
        repository.RefreshTokens.Add(activeToken);

        var service = BuildService(repository, refreshTokenService: refresh);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            service.RefreshAsync(new RefreshRequest(reusedTokenRaw), "127.0.0.1", CancellationToken.None));

        Assert.Equal(401, exception.StatusCode);
        Assert.NotNull(activeToken.RevokedAtUtc);
        Assert.Equal("Refresh token reuse detected.", activeToken.RevocationReason);
    }

    [Fact]
    public async Task EmailVerification_Flow_Works()
    {
        var repository = new InMemoryIdentityRepository();
        var emailSender = new EmailSenderStub();
        var service = BuildService(repository, emailSender: emailSender);

        await service.RegisterAsync(new RegisterRequest("verify@test.local", "Password123"), "127.0.0.1", CancellationToken.None);
        var user = repository.Users.Single();

        var issued = await service.RequestEmailVerificationAsync(
            new EmailVerificationRequest("verify@test.local"),
            "127.0.0.1",
            CancellationToken.None);

        Assert.NotNull(issued.VerificationToken);
        Assert.False(user.IsEmailVerified);

        await service.VerifyEmailAsync(new VerifyEmailRequest(issued.VerificationToken!), "127.0.0.1", CancellationToken.None);

        Assert.True(user.IsEmailVerified);
        Assert.Single(emailSender.SentEmails);
        Assert.Equal("verify@test.local", emailSender.SentEmails[0].ToEmail);
    }

    [Fact]
    public async Task GetDiscordStartUrlAsync_ReturnsAuthorizationUrl()
    {
        var repository = new InMemoryIdentityRepository();
        var discordOAuth = new DiscordOAuthServiceStub();
        var service = BuildService(repository, discordOAuthService: discordOAuth);

        var response = await service.GetDiscordStartUrlAsync(CancellationToken.None);

        Assert.StartsWith("https://discord.test/authorize", response.AuthorizationUrl);
    }

    [Fact]
    public async Task HandleDiscordCallbackAsync_LoginPurpose_AutoCreatesUserAndIssuesTokens()
    {
        var repository = new InMemoryIdentityRepository();
        var discordOAuth = new DiscordOAuthServiceStub
        {
            Identity = new("discord-42", "DiscordUser", "discord-42@example.com")
        };
        var discordState = new DiscordStateServiceStub
        {
            State = new("login", null, DateTime.UtcNow.AddMinutes(5), "nonce")
        };

        var service = BuildService(
            repository,
            discordOAuthService: discordOAuth,
            discordStateService: discordState,
            discordAuthSettings: new DiscordAuthSettingsStub
            {
                AutoCreateUser = true,
                RequireManualLinkForExistingEmail = true
            });

        var response = await service.HandleDiscordCallbackAsync("code", "state", "127.0.0.1", CancellationToken.None);

        Assert.True(response.Linked);
        Assert.True(response.CreatedUser);
        Assert.NotNull(response.Tokens);
        Assert.Single(repository.Users);
        Assert.Single(repository.ExternalLogins);
        Assert.Equal("discord-42", repository.ExternalLogins.Single().ProviderUserId);
    }

    [Fact]
    public async Task SetUserRolesAsync_UpdatesRoles_WhenRolesAreValid()
    {
        var repository = new InMemoryIdentityRepository();
        var userRole = new AkGaming.Identity.Domain.Entities.Role { Name = RoleNames.User };
        var adminRole = new AkGaming.Identity.Domain.Entities.Role { Name = RoleNames.Admin };
        repository.Roles.Add(userRole);
        repository.Roles.Add(adminRole);

        var targetUser = new AkGaming.Identity.Domain.Entities.User { Email = "target@test.local", PasswordHash = "hash" };
        targetUser.UserRoles.Add(new AkGaming.Identity.Domain.Entities.UserRole { UserId = targetUser.Id, User = targetUser, RoleId = userRole.Id, Role = userRole });
        repository.Users.Add(targetUser);

        var service = BuildService(repository);
        var actorUserId = Guid.NewGuid();

        var result = await service.SetUserRolesAsync(
            actorUserId,
            targetUser.Id,
            new AdminSetUserRolesRequest([RoleNames.User, RoleNames.Admin]),
            "127.0.0.1",
            CancellationToken.None);

        Assert.Equal(targetUser.Id, result.UserId);
        Assert.Contains(RoleNames.Admin, result.Roles);
        Assert.Contains(RoleNames.User, result.Roles);
    }

    [Fact]
    public async Task SetUserRolesAsync_RemovingLastAdmin_ThrowsConflict()
    {
        var repository = new InMemoryIdentityRepository();
        var userRole = new AkGaming.Identity.Domain.Entities.Role { Name = RoleNames.User };
        var adminRole = new AkGaming.Identity.Domain.Entities.Role { Name = RoleNames.Admin };
        repository.Roles.Add(userRole);
        repository.Roles.Add(adminRole);

        var onlyAdmin = new AkGaming.Identity.Domain.Entities.User { Email = "admin@test.local", PasswordHash = "hash" };
        onlyAdmin.UserRoles.Add(new AkGaming.Identity.Domain.Entities.UserRole { UserId = onlyAdmin.Id, User = onlyAdmin, RoleId = adminRole.Id, Role = adminRole });
        repository.Users.Add(onlyAdmin);

        var service = BuildService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            service.SetUserRolesAsync(
                onlyAdmin.Id,
                onlyAdmin.Id,
                new AdminSetUserRolesRequest([RoleNames.User]),
                "127.0.0.1",
                CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task CreateRoleAsync_AddsRole()
    {
        var repository = new InMemoryIdentityRepository();
        var service = BuildService(repository);

        var created = await service.CreateRoleAsync(Guid.NewGuid(), new AdminCreateRoleRequest("Moderator"), "127.0.0.1", CancellationToken.None);

        Assert.Equal("Moderator", created.Name);
        Assert.Contains(repository.Roles, x => x.Name == "Moderator");
    }

    [Fact]
    public async Task RenameRoleAsync_UpdatesName()
    {
        var repository = new InMemoryIdentityRepository();
        var role = new AkGaming.Identity.Domain.Entities.Role { Name = "Support" };
        repository.Roles.Add(role);
        var service = BuildService(repository);

        var renamed = await service.RenameRoleAsync(Guid.NewGuid(), role.Id, new AdminRenameRoleRequest("GameMaster"), "127.0.0.1", CancellationToken.None);

        Assert.Equal("GameMaster", renamed.Name);
        Assert.Equal("GameMaster", repository.Roles.Single(x => x.Id == role.Id).Name);
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenAssigned_ThrowsConflict()
    {
        var repository = new InMemoryIdentityRepository();
        var role = new AkGaming.Identity.Domain.Entities.Role { Name = "Member" };
        repository.Roles.Add(role);
        var user = new AkGaming.Identity.Domain.Entities.User { Email = "member@test.local", PasswordHash = "hash" };
        user.UserRoles.Add(new AkGaming.Identity.Domain.Entities.UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role
        });
        repository.Users.Add(user);

        var service = BuildService(repository);

        var exception = await Assert.ThrowsAsync<AuthException>(() =>
            service.DeleteRoleAsync(Guid.NewGuid(), role.Id, "127.0.0.1", CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Contains(repository.Roles, x => x.Id == role.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedUsers()
    {
        var repository = new InMemoryIdentityRepository();
        var userRole = new AkGaming.Identity.Domain.Entities.Role { Name = RoleNames.User };
        repository.Roles.Add(userRole);

        for (var i = 0; i < 3; i++)
        {
            var user = new AkGaming.Identity.Domain.Entities.User { Email = $"user{i}@test.local", PasswordHash = "hash" };
            user.UserRoles.Add(new AkGaming.Identity.Domain.Entities.UserRole
            {
                UserId = user.Id,
                User = user,
                RoleId = userRole.Id,
                Role = userRole
            });
            repository.Users.Add(user);
        }

        var service = BuildService(repository);
        var page = await service.GetUsersAsync(1, 2, null, CancellationToken.None);

        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task GetUserDetailsAsync_ReturnsDiscordInfo_WhenLinked()
    {
        var repository = new InMemoryIdentityRepository();
        var userRole = new AkGaming.Identity.Domain.Entities.Role { Name = RoleNames.User };
        repository.Roles.Add(userRole);

        var user = new AkGaming.Identity.Domain.Entities.User { Email = "detail@test.local", PasswordHash = "hash", IsEmailVerified = true };
        user.UserRoles.Add(new AkGaming.Identity.Domain.Entities.UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = userRole.Id,
            Role = userRole
        });
        user.ExternalLogins.Add(new AkGaming.Identity.Domain.Entities.ExternalLogin
        {
            UserId = user.Id,
            User = user,
            Provider = "discord",
            ProviderUserId = "12345",
            ProviderUsername = "DiscordTester"
        });
        repository.Users.Add(user);

        var service = BuildService(repository);
        var details = await service.GetUserDetailsAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, details.UserId);
        Assert.Equal("detail@test.local", details.Email);
        Assert.True(details.IsEmailVerified);
        Assert.NotNull(details.Discord);
        Assert.Equal("DiscordTester", details.Discord!.ProviderUsername);
    }

    private static AuthService BuildService(
        InMemoryIdentityRepository repository,
        PasswordHasherStub? passwordHasher = null,
        RefreshTokenServiceStub? refreshTokenService = null,
        EmailSenderStub? emailSender = null,
        DiscordOAuthServiceStub? discordOAuthService = null,
        DiscordStateServiceStub? discordStateService = null,
        DiscordAuthSettingsStub? discordAuthSettings = null,
        AuthHardeningSettingsStub? hardeningSettings = null,
        AppUrlSettingsStub? appUrlSettings = null)
    {
        return new AuthService(
            repository,
            passwordHasher ?? new PasswordHasherStub(),
            new JwtTokenServiceStub(),
            refreshTokenService ?? new RefreshTokenServiceStub(),
            emailSender ?? new EmailSenderStub(),
            discordOAuthService ?? new DiscordOAuthServiceStub(),
            discordStateService ?? new DiscordStateServiceStub(),
            discordAuthSettings ?? new DiscordAuthSettingsStub(),
            hardeningSettings ?? new AuthHardeningSettingsStub(),
            appUrlSettings ?? new AppUrlSettingsStub(),
            NullLogger<AuthService>.Instance);
    }
}
