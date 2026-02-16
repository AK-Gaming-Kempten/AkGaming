using AkGaming.Identity.Application.Auth;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Application.UnitTests.Fakes;
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
