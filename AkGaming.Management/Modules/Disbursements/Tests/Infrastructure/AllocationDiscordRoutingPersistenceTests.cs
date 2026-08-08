using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Notifications;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.Disbursements.Tests.Infrastructure;

[TestFixture]
public sealed class AllocationDiscordRoutingPersistenceTests
{
    [Test]
    [Description("Persists allocation Discord routing and a complete claim snapshot through the SQLite provider.")]
    public async Task AllocationClaim_WhenSaved_PersistsRoutingAndNotificationSnapshot()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var allocation = CreateAllocation();
        var application = allocation.Applications.Single();
        context.Add(allocation);
        var outbox = new DisbursementNotificationOutbox(context, Options.Create(new DisbursementNotificationOptions
        {
            ManagementFrontendBaseUrl = "https://management.test.akgaming.de"
        }));
        outbox.EnqueueAllocationClaimChanged(application);

        // Act
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Assert
        var storedAllocation = await context.Allocations.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(storedAllocation.DiscordChannelId, Is.EqualTo("channel-123"));
            Assert.That(storedAllocation.DiscordRoleId, Is.EqualTo("role-456"));
        });
        var message = await context.NotificationOutbox.SingleAsync();
        var envelope = JsonSerializer.Deserialize<NotificationEnvelope>(message.PayloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var snapshot = envelope!.Data.Deserialize<AllocationClaimChangedNotification>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Multiple(() =>
        {
            Assert.That(snapshot!.ApplicationId, Is.EqualTo(application.Id));
            Assert.That(snapshot.ChannelId, Is.EqualTo("channel-123"));
            Assert.That(snapshot.RoleId, Is.EqualTo("role-456"));
            Assert.That(snapshot.Approvals, Is.EqualTo(new[] { "Anna" }));
            Assert.That(snapshot.Objections, Is.EqualTo(new[] { "Berta" }));
            Assert.That(snapshot.ManagementUrl, Does.EndWith($"/disbursements/claim/{allocation.ShareToken}"));
        });
    }

    [Test]
    [Description("Does not route legacy allocation claims when no per-allocation Discord destination exists.")]
    public async Task AllocationClaim_WhenRoutingIsMissing_DoesNotQueueFallbackNotification()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var allocation = CreateAllocation();
        allocation.DiscordChannelId = string.Empty;
        var outbox = new DisbursementNotificationOutbox(context,
            Options.Create(new DisbursementNotificationOptions()));

        // Act
        outbox.EnqueueAllocationClaimChanged(allocation.Applications.Single());

        // Assert
        Assert.That(context.NotificationOutbox.Local, Is.Empty);
    }

    [Test]
    [Description("Persists a manual allocation announcement with its claim link, channel, and role.")]
    public async Task AllocationAnnouncement_WhenQueued_PersistsDiscordRoutingAndClaimLink()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var allocation = CreateAllocation();
        var outbox = new DisbursementNotificationOutbox(context, Options.Create(new DisbursementNotificationOptions
        {
            ManagementFrontendBaseUrl = "https://management.test.akgaming.de"
        }));
        outbox.EnqueueAllocationAvailable(allocation);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var message = await context.NotificationOutbox.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(message.Type, Is.EqualTo(NotificationEventTypes.AllocationAvailable));
            Assert.That(message.PayloadJson, Does.Contain("channel-123"));
            Assert.That(message.PayloadJson, Does.Contain("role-456"));
            Assert.That(message.PayloadJson, Does.Contain($"/disbursements/claim/{allocation.ShareToken}"));
            Assert.That(message.PayloadJson, Does.Contain("/guides/disbursement-claim-guide-de.png"));
        });
    }

    private static Allocation CreateAllocation()
    {
        var allocation = new Allocation
        {
            Name = "Team prize",
            Amount = 200m,
            DiscordChannelId = "channel-123",
            DiscordChannelName = "team-prizes",
            DiscordRoleId = "role-456",
            DiscordRoleName = "Team Blue",
            Event = new DisbursementEvent
            {
                Name = "Summer cup",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };
        allocation.Applications.Add(new AllocationApplication
        {
            Allocation = allocation,
            AllocationId = allocation.Id,
            ApplicantUserId = Guid.NewGuid(),
            ApplicantName = "Chris",
            Amount = 200m,
            CreatedAt = DateTimeOffset.UtcNow,
            Approvals =
            [
                new AllocationApproval { ApproverUserId = Guid.NewGuid(), ApproverName = "Anna", IsApproved = true },
                new AllocationApproval { ApproverUserId = Guid.NewGuid(), ApproverName = "Berta", IsApproved = false }
            ]
        });
        return allocation;
    }
}
