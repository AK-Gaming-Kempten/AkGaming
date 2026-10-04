using System.ComponentModel;
using AkGaming.Core.Common.Email;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Auth;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Application.ExternalAuth;
using AkGaming.Identity.Application.UnitTests.Fakes;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AkGaming.Identity.Application.UnitTests;

public sealed class EmailChangeServiceTests
{
    private InMemoryIdentityRepository Repository { get; } = new();
    private Mock<IEmailChangeStore> Store { get; } = new();
    private Mock<IAuthService> Auth { get; } = new();
    private Mock<IEmailSender> Sender { get; } = new();
    private Mock<IDiscordOAuthService> Discord { get; } = new();
    private Mock<IDiscordStateService> State { get; } = new();
    private User Account { get; } = new() { Email = "old@example.com", IsEmailVerified = true };
    private EmailChangeService Service { get; }
    private EmailChangeRequest? Issued { get; set; }

    public EmailChangeServiceTests()
    {
        Repository.Users.Add(Account);
        Store.Setup(x => x.IssueAsync(It.IsAny<EmailChangeRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<EmailChangeRequest, string?, CancellationToken>((request, _, _) => Issued = request)
            .Returns(Task.CompletedTask);
        Sender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Service = new EmailChangeService(Repository, Store.Object, Auth.Object, new RefreshTokenServiceStub(),
            Sender.Object, new AppUrlSettingsStub(), Discord.Object, State.Object, NullLogger<EmailChangeService>.Instance);
    }

    [Fact]
    [Description("A password-authorized email change normalizes the pending address, sends a confirmation link, and preserves the verified current address.")]
    public async Task Request_PreservesCurrentEmail_AndSendsConfirmation()
    {
        // Arrange
        const string newEmail = " NEW@Example.com ";

        // Act
        await Service.RequestAsync(Account.Id, newEmail, "Password123", null, CancellationToken.None);

        // Assert
        Auth.Verify(x => x.LoginInteractiveAsync(new LoginRequest(Account.Email, "Password123"), null, CancellationToken.None), Times.Once);
        Assert.Equal("old@example.com", Account.Email);
        Assert.True(Account.IsEmailVerified);
        Assert.Equal("new@example.com", Issued!.NewEmail);
        Assert.Equal(Account.SecurityVersion, Issued.OriginalSecurityVersion);
        Sender.Verify(x => x.SendAsync("new@example.com", It.IsAny<string>(),
            It.Is<string>(body => body.Contains("https://identity.akgaming.de/account/email-change?token=")),
            It.IsAny<string?>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    [Description("Failed password reauthentication does not issue an email-change request or send mail.")]
    public async Task Request_RejectsFailedReauthentication()
    {
        // Arrange
        Auth.Setup(x => x.LoginInteractiveAsync(It.IsAny<LoginRequest>(), null, CancellationToken.None))
            .ThrowsAsync(new AuthException(401, "Invalid credentials."));

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Service.RequestAsync(Account.Id, "new@example.com", "wrong", null, CancellationToken.None));

        // Assert
        Assert.Equal(401, error.StatusCode);
        Assert.Null(Issued);
        Sender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("old@example.com")]
    [InlineData("Display Name <new@example.com>")]
    [InlineData("invalid")]
    [Description("Invalid, display-name, and unchanged email addresses cannot become pending email changes.")]
    public async Task Request_RejectsInvalidOrUnchangedAddress(string address)
    {
        // Arrange
        var originalEmail = Account.Email;

        // Act
        await Assert.ThrowsAsync<AuthException>(() => Service.RequestAsync(Account.Id, address, "Password123", null, CancellationToken.None));

        // Assert
        Assert.Equal(originalEmail, Account.Email);
        Assert.Null(Issued);
    }

    [Fact]
    [Description("SMTP failure during issuance reports a retryable error while preserving the original verified email.")]
    public async Task Request_DeliveryFailure_PreservesAccount()
    {
        // Arrange
        Sender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP failed"));

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Service.RequestAsync(Account.Id, "new@example.com", "Password123", null, CancellationToken.None));

        // Assert
        Assert.Equal(503, error.StatusCode);
        Assert.Equal("old@example.com", Account.Email);
        Assert.True(Account.IsEmailVerified);
        Assert.Contains(Repository.AuditLogs, x => x.Details == "confirmation_delivery_failed");
    }

    [Fact]
    [Description("Successful email-change confirmation notifies the previous mailbox.")]
    public async Task Confirm_NotifiesOldMailbox()
    {
        // Arrange
        var request = new EmailChangeRequest { UserId = Account.Id, OriginalEmail = Account.Email, NewEmail = "new@example.com" };
        Store.Setup(x => x.ConfirmAsync(It.IsAny<string>(), null, CancellationToken.None)).ReturnsAsync(request);

        // Act
        await Service.ConfirmAsync("valid-token", null, CancellationToken.None);

        // Assert
        Sender.Verify(x => x.SendAsync("old@example.com", It.IsAny<string>(),
            It.Is<string>(text => text.Contains("new@example.com")), It.IsAny<string?>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    [Description("Notification delivery failure after commit is audited without reporting the completed change as unsuccessful.")]
    public async Task Confirm_NotificationFailure_DoesNotUndoSuccess()
    {
        // Arrange
        Store.Setup(x => x.ConfirmAsync(It.IsAny<string>(), null, CancellationToken.None)).ReturnsAsync(
            new EmailChangeRequest { UserId = Account.Id, OriginalEmail = Account.Email, NewEmail = "new@example.com" });
        Sender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP failed"));

        // Act
        await Service.ConfirmAsync("valid-token", null, CancellationToken.None);

        // Assert
        Assert.Contains(Repository.AuditLogs, x => x.Details == "notification_delivery_failed");
    }

    [Fact]
    [Description("Discord reauthentication rejects an identity linked to another local account and never issues a change.")]
    public async Task Discord_RejectsWrongLinkedIdentity()
    {
        // Arrange
        State.Setup(x => x.ReadState("state")).Returns(new DiscordOAuthState("email_change", Account.Id,
            DateTime.UtcNow.AddMinutes(5), "nonce", State: "new@example.com"));
        Discord.Setup(x => x.GetIdentityFromAuthorizationCodeAsync("code", CancellationToken.None))
            .ReturnsAsync(new DiscordIdentity("different-id", "Other", "other@example.com", true));
        Repository.ExternalLogins.Add(new ExternalLogin { Provider = "discord", ProviderUserId = "different-id", UserId = Guid.NewGuid() });

        // Act
        var error = await Assert.ThrowsAsync<AuthException>(() => Service.RequestWithDiscordAsync(Account.Id, "code", "state", null, CancellationToken.None));

        // Assert
        Assert.Equal(403, error.StatusCode);
        Assert.Null(Issued);
    }

    [Fact]
    [Description("Discord-only users can request an email change by authenticating as their already-linked Discord ID.")]
    public async Task Discord_AllowsLinkedIdentity_WithoutPassword()
    {
        // Arrange
        State.Setup(x => x.ReadState("state")).Returns(new DiscordOAuthState("email_change", Account.Id,
            DateTime.UtcNow.AddMinutes(5), "nonce", State: "new@example.com"));
        Discord.Setup(x => x.GetIdentityFromAuthorizationCodeAsync("code", CancellationToken.None))
            .ReturnsAsync(new DiscordIdentity("linked-id", "Linked", Account.Email, true));
        Repository.ExternalLogins.Add(new ExternalLogin { Provider = "discord", ProviderUserId = "linked-id", UserId = Account.Id });

        // Act
        await Service.RequestWithDiscordAsync(Account.Id, "code", "state", null, CancellationToken.None);

        // Assert
        Assert.Equal("new@example.com", Issued!.NewEmail);
        Auth.Verify(x => x.LoginInteractiveAsync(It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
