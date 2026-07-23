using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Domain.Enums;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Notifications;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.MemberManagement.Tests.Infrastructure;

[TestFixture]
public sealed class MemberNotificationOutboxTests
{
    [Test]
    [Description("Persists a membership application notification envelope in the member database outbox.")]
    public async Task EnqueueMembershipApplicationCreated_WhenSaved_PersistsNotificationEnvelope()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var outbox = new MemberNotificationOutbox(dbContext, Options.Create(new MemberNotificationOptions
        {
            ManagementFrontendBaseUrl = "https://management.test.akgaming.de"
        }));
        var request = new MembershipApplicationRequest
        {
            Id = Guid.NewGuid(),
            IssuingUserId = Guid.NewGuid(),
            FirstName = "Erika",
            LastName = "Mustermann",
            Email = "erika@example.com"
        };

        // Act
        outbox.EnqueueMembershipApplicationCreated(request);
        await dbContext.SaveChangesAsync();

        // Assert
        var message = await dbContext.NotificationOutbox.SingleAsync();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Multiple(() =>
        {
            Assert.That(message.Type, Is.EqualTo(NotificationEventTypes.MembershipApplicationCreated));
            Assert.That(payload.RootElement.GetProperty("subjectUserId").GetGuid(),
                Is.EqualTo(request.IssuingUserId));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("applicantName").GetString(),
                Is.EqualTo("Erika Mustermann"));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("managementUrl").GetString(),
                Is.EqualTo("https://management.test.akgaming.de/member-management/requests"));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("applicantUrl").GetString(),
                Is.EqualTo("https://management.test.akgaming.de/membership"));
        });
    }

    [Test]
    [Description("Persists membership application decisions for private delivery to the applicant.")]
    public async Task EnqueueMembershipApplicationStatusChanged_WhenSaved_PersistsApplicantEnvelope()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var outbox = new MemberNotificationOutbox(dbContext, Options.Create(new MemberNotificationOptions()));
        var request = new MembershipApplicationRequest
        {
            Id = Guid.NewGuid(),
            IssuingUserId = Guid.NewGuid()
        };

        // Act
        outbox.EnqueueMembershipApplicationStatusChanged(request, accepted: true);
        await dbContext.SaveChangesAsync();

        // Assert
        var message = await dbContext.NotificationOutbox.SingleAsync();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Multiple(() =>
        {
            Assert.That(message.Type, Is.EqualTo(NotificationEventTypes.MembershipApplicationStatusChanged));
            Assert.That(payload.RootElement.GetProperty("subjectUserId").GetGuid(),
                Is.EqualTo(request.IssuingUserId));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("status").GetString(),
                Is.EqualTo("Accepted"));
        });
    }

    [Test]
    [Description("Removes a trailing API path from the legacy Management notification URL setting.")]
    public void ManagementFrontendBaseUrl_WithLegacyApiBase_ReturnsFrontendBaseUrl()
    {
        // Arrange
        const string legacyApiBaseUrl = "https://management.test.akgaming.de/api/";

        // Act
        var result = NotificationUrlBuilder.ManagementFrontendBaseUrl(null, legacyApiBaseUrl);

        // Assert
        Assert.That(result, Is.EqualTo("https://management.test.akgaming.de"));
    }

    [Test]
    [Description("Persists a member linking request notification including its reason in the member database outbox.")]
    public async Task EnqueueMemberLinkingRequestCreated_WhenSaved_PersistsNotificationEnvelope()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var outbox = new MemberNotificationOutbox(dbContext, Options.Create(new MemberNotificationOptions()));
        var request = new MemberLinkingRequest
        {
            Id = Guid.NewGuid(),
            IssuingUserId = Guid.NewGuid(),
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            Reason = MemberLinkingRequestReason.NewRegistration
        };

        // Act
        outbox.EnqueueMemberLinkingRequestCreated(request);
        await dbContext.SaveChangesAsync();

        // Assert
        var message = await dbContext.NotificationOutbox.SingleAsync();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Multiple(() =>
        {
            Assert.That(message.Type, Is.EqualTo(NotificationEventTypes.MemberLinkingRequestCreated));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("reason").GetString(),
                Is.EqualTo("NewRegistration"));
        });
    }

    [Test]
    [Description("Skips membership status notifications when the member has no linked identity user.")]
    public async Task EnqueueMembershipStatusChanged_WithoutUserId_DoesNotQueueNotification()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var outbox = new MemberNotificationOutbox(dbContext, Options.Create(new MemberNotificationOptions()));
        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserId = null,
            Status = MembershipStatus.Member
        };

        // Act
        outbox.EnqueueMembershipStatusChanged(member, MembershipStatus.InTrial);
        await dbContext.SaveChangesAsync();

        // Assert
        Assert.That(await dbContext.NotificationOutbox.CountAsync(), Is.Zero);
    }

    [Test]
    [Description("Persists membership status changes for private delivery to the linked member.")]
    public async Task EnqueueMembershipStatusChanged_WithUserId_PersistsMemberEnvelope()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var outbox = new MemberNotificationOutbox(dbContext, Options.Create(new MemberNotificationOptions()));
        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = MembershipStatus.Member
        };

        // Act
        outbox.EnqueueMembershipStatusChanged(member, MembershipStatus.InTrial);
        await dbContext.SaveChangesAsync();

        // Assert
        var message = await dbContext.NotificationOutbox.SingleAsync();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Multiple(() =>
        {
            Assert.That(message.Type, Is.EqualTo(NotificationEventTypes.MembershipStatusChanged));
            Assert.That(payload.RootElement.GetProperty("subjectUserId").GetGuid(), Is.EqualTo(member.UserId));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("previousStatus").GetString(),
                Is.EqualTo("InTrial"));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("status").GetString(),
                Is.EqualTo("Member"));
        });
    }

    private static MemberManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MemberManagementDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new MemberManagementDbContext(options);
    }
}
