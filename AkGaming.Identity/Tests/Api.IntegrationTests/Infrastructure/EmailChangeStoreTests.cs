using System.ComponentModel;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Domain.Entities;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Moq;

namespace AkGaming.Identity.Api.IntegrationTests.Infrastructure;

public sealed class EmailChangeStoreTests : IClassFixture<TestApiFactory>, IDisposable
{
    private TestApiFactory Factory { get; }
    private IServiceScope Scope { get; }
    private AuthDbContext Db { get; }
    private IEmailChangeStore Store { get; }
    private User Account { get; } = new() { Email = $"old-{Guid.NewGuid():N}@example.com", IsEmailVerified = true };
    private EmailChangeRequest Request { get; }

    public EmailChangeStoreTests(TestApiFactory factory)
    {
        Factory = factory;
        Scope = factory.Services.CreateScope();
        Db = Scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Store = Scope.ServiceProvider.GetRequiredService<IEmailChangeStore>();
        Request = NewRequest();
    }

    [Fact]
    [Description("SQLite confirmation atomically verifies the new email, consumes recovery links, revokes legacy and OIDC refresh credentials, and invalidates local sessions.")]
    public async Task Confirm_UpdatesAccount_AndRevokesCredentials()
    {
        // Arrange
        await SeedAsync();
        Db.EmailVerificationTokens.Add(new EmailVerificationToken { UserId = Account.Id, TokenHash = Guid.NewGuid().ToString(), ExpiresAtUtc = DateTime.UtcNow.AddHours(1) });
        Db.PasswordResetTokens.Add(new PasswordResetToken { UserId = Account.Id, TokenHash = Guid.NewGuid().ToString(), ExpiresAtUtc = DateTime.UtcNow.AddHours(1) });
        Db.RefreshTokens.Add(new RefreshToken { UserId = Account.Id, TokenHash = Guid.NewGuid().ToString(), ExpiresAtUtc = DateTime.UtcNow.AddDays(1) });
        await Db.SaveChangesAsync();
        var manager = Scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var token = await manager.CreateAsync(new OpenIddictTokenDescriptor
        {
            Subject = Account.Id.ToString(), Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypeHints.RefreshToken, ExpirationDate = DateTimeOffset.UtcNow.AddDays(1)
        });
        var tokenId = await manager.GetIdAsync(token!);
        await Store.IssueAsync(Request, null, CancellationToken.None);

        // Act
        await Store.ConfirmAsync(Request.TokenHash, "127.0.0.1", CancellationToken.None);

        // Assert
        var user = await ReadAccountAsync();
        Assert.Equal(Request.NewEmail, user.Email);
        Assert.True(user.IsEmailVerified);
        Assert.NotEqual(Account.SecurityVersion, user.SecurityVersion);
        Assert.NotNull((await Db.EmailVerificationTokens.AsNoTracking().SingleAsync(x => x.UserId == Account.Id)).ConsumedAtUtc);
        Assert.NotNull((await Db.PasswordResetTokens.AsNoTracking().SingleAsync(x => x.UserId == Account.Id)).ConsumedAtUtc);
        Assert.NotNull((await Db.RefreshTokens.AsNoTracking().SingleAsync(x => x.UserId == Account.Id)).RevokedAtUtc);
        Db.ChangeTracker.Clear();
        Assert.Equal(OpenIddictConstants.Statuses.Revoked, await manager.GetStatusAsync((await manager.FindByIdAsync(tokenId!))!));
        Assert.Null(await Store.GetPendingAsync(Account.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("cancelled")]
    [InlineData("consumed")]
    [Description("Expired, cancelled, and previously consumed email-change links cannot update an account through SQLite.")]
    public async Task Confirm_RejectsInactiveRequest(string state)
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        if (state == "expired")
            Request.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        if (state == "cancelled")
            Request.CancelledAtUtc = DateTime.UtcNow;
        if (state == "consumed")
            Request.ConsumedAtUtc = DateTime.UtcNow;
        await Db.SaveChangesAsync();

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));

        // Assert
        Assert.Equal(400, error.StatusCode);
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("A token can complete an email change only once.")]
    public async Task Confirm_RejectsReplay()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        await Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None);

        // Act
        await Assert.ThrowsAsync<AuthException>(() => Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));

        // Assert
        Assert.Equal(Request.NewEmail, (await ReadAccountAsync()).Email);
        Assert.Single(await Db.AuditLogs.Where(x => x.UserId == Account.Id && x.EventType == "email_change.completed").ToListAsync());
    }

    [Fact]
    [Description("Cancellation prevents confirmation and preserves the original email address.")]
    public async Task Cancel_InvalidatesPendingRequest()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);

        // Act
        await Store.CancelAsync(Account.Id, null, CancellationToken.None);

        // Assert
        Assert.Null(await Store.GetPendingAsync(Account.Id, CancellationToken.None));
        await Assert.ThrowsAsync<AuthException>(() => Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("Resending replaces the old token while enforcing one pending request per account.")]
    public async Task Issue_Resend_ReplacesPreviousToken()
    {
        // Arrange
        await SeedAsync();
        Request.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2);
        await Store.IssueAsync(Request, null, CancellationToken.None);
        var replacement = NewRequest();

        // Act
        await Store.IssueAsync(replacement, null, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<AuthException>(() => Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));
        Assert.Equal(replacement.NewEmail, (await Store.GetPendingAsync(Account.Id, CancellationToken.None))!.NewEmail);
        Assert.Single(await Db.EmailChangeRequests.AsNoTracking().Where(x => x.UserId == Account.Id).ToListAsync());
    }

    [Fact]
    [Description("Repeated issuance within one minute is rejected without replacing the existing confirmation token.")]
    public async Task Issue_EnforcesResendCooldown()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Store.IssueAsync(NewRequest(), null, CancellationToken.None));

        // Assert
        Assert.Equal(429, error.StatusCode);
        Assert.Equal(Request.TokenHash, (await Store.GetPendingAsync(Account.Id, CancellationToken.None))!.TokenHash);
    }

    [Fact]
    [Description("A new email claimed by another account after issuance is rejected at confirmation without consuming the request.")]
    public async Task Confirm_RechecksEmailUniqueness()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        Db.Users.Add(new User { Email = Request.NewEmail });
        await Db.SaveChangesAsync();

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));

        // Assert
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
        Assert.Null((await Db.EmailChangeRequests.AsNoTracking().SingleAsync(x => x.UserId == Account.Id)).ConsumedAtUtc);
    }

    [Fact]
    [Description("A password reset or other security-version change invalidates a pending email-change request.")]
    public async Task Confirm_RejectsAccountChangedAfterIssuance()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        Account.SecurityVersion = Guid.NewGuid();
        await Db.SaveChangesAsync();

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));

        // Assert
        Assert.Equal(409, error.StatusCode);
        Assert.Null(await Store.GetPendingAsync(Account.Id, CancellationToken.None));
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("If OIDC revocation fails, the transaction rolls back the email change, request consumption, and recovery-token invalidation.")]
    public async Task Confirm_RevocationFailure_RollsBackChange()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        var tokens = new Mock<IOpenIddictTokenManager>();
        tokens.Setup(x => x.FindBySubjectAsync(Account.Id.ToString(), CancellationToken.None)).Returns(EmptyEntities());
        tokens.Setup(x => x.RevokeBySubjectAsync(Account.Id.ToString(), CancellationToken.None)).ThrowsAsync(new InvalidOperationException("revocation failed"));
        var authorizations = new Mock<IOpenIddictAuthorizationManager>();
        authorizations.Setup(x => x.FindBySubjectAsync(Account.Id.ToString(), CancellationToken.None)).Returns(EmptyEntities());
        var store = new EmailChangeStore(Db, tokens.Object, authorizations.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None));

        // Assert
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
        Assert.NotNull(await Store.GetPendingAsync(Account.Id, CancellationToken.None));
    }

    [Fact]
    [Description("Concurrent confirmations in separate SQLite contexts produce exactly one successful email change.")]
    public async Task Confirm_ConcurrentRequests_CompleteOnce()
    {
        // Arrange
        await SeedAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        async Task<bool> ConfirmInNewScopeAsync()
        {
            using var scope = Factory.Services.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IEmailChangeStore>().ConfirmAsync(Request.TokenHash, null, CancellationToken.None);
                return true;
            }
            catch (AuthException)
            {
                return false;
            }
        }

        // Act
        var results = await Task.WhenAll(Task.Run(ConfirmInNewScopeAsync), Task.Run(ConfirmInNewScopeAsync));

        // Assert
        Assert.Single(results, x => x);
        Assert.Equal(Request.NewEmail, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("A legacy refresh rotation that loaded its token before email-change revocation cannot overwrite revocation or persist its replacement credential.")]
    public async Task Confirm_PreventsInFlightLegacyRefreshRotation()
    {
        // Arrange
        await SeedAsync();
        var original = new RefreshToken { UserId = Account.Id, TokenHash = Guid.NewGuid().ToString(), ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        Db.RefreshTokens.Add(original);
        await Db.SaveChangesAsync();
        await Store.IssueAsync(Request, null, CancellationToken.None);
        using var staleScope = Factory.Services.CreateScope();
        var staleDb = staleScope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var staleToken = await staleDb.RefreshTokens.SingleAsync(x => x.Id == original.Id);
        await Store.ConfirmAsync(Request.TokenHash, null, CancellationToken.None);
        staleToken.RevokedAtUtc = DateTime.UtcNow;
        staleToken.RevocationReason = "Rotated refresh token.";
        staleDb.RefreshTokens.Add(new RefreshToken { UserId = Account.Id, TokenHash = Guid.NewGuid().ToString(), ExpiresAtUtc = DateTime.UtcNow.AddDays(1) });

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => staleScope.ServiceProvider.GetRequiredService<IIdentityRepository>().SaveChangesAsync(CancellationToken.None));

        // Assert
        Assert.Equal(409, error.StatusCode);
        var stored = Assert.Single(await Db.RefreshTokens.AsNoTracking().Where(x => x.UserId == Account.Id).ToListAsync());
        Assert.Equal("Email changed.", stored.RevocationReason);
        Assert.NotNull(stored.RevokedAtUtc);
    }

    private static async IAsyncEnumerable<object> EmptyEntities()
    {
        await Task.CompletedTask;
        yield break;
    }

    private async Task SeedAsync()
    {
        Db.Users.Add(Account);
        await Db.SaveChangesAsync();
    }

    private Task<User> ReadAccountAsync()
    {
        return Db.Users.AsNoTracking().SingleAsync(x => x.Id == Account.Id);
    }

    private EmailChangeRequest NewRequest()
    {
        return new EmailChangeRequest
        {
            UserId = Account.Id, OriginalEmail = Account.Email, OriginalSecurityVersion = Account.SecurityVersion,
            NewEmail = $"new-{Guid.NewGuid():N}@example.com", TokenHash = Guid.NewGuid().ToString("N"), ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };
    }

    public void Dispose()
    {
        Scope.Dispose();
    }
}
