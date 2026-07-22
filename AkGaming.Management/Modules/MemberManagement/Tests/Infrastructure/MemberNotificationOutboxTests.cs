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
            ManagementBaseUrl = "https://management.test.akgaming.de"
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
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("applicantName").GetString(),
                Is.EqualTo("Erika Mustermann"));
            Assert.That(payload.RootElement.GetProperty("data").GetProperty("managementUrl").GetString(),
                Is.EqualTo("https://management.test.akgaming.de/member-management/requests"));
        });
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

    private static MemberManagementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MemberManagementDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new MemberManagementDbContext(options);
    }
}
