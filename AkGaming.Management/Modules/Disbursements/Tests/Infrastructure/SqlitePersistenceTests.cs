using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Application.Interfaces;
using AkGaming.Management.Modules.Disbursements.Application.Services;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Notifications;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.Disbursements.Tests.Infrastructure;

[TestFixture]
public sealed class SqlitePersistenceTests
{
    [Test]
    [Description("Persists a reimbursement and its notification outbox event in the same EF Core save operation.")]
    public async Task NotificationOutbox_WhenReimbursementIsSaved_PersistsEventWithAggregate()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var reimbursement = new Reimbursement
        {
            UserId = Guid.NewGuid(),
            ApplicantName = "Applicant",
            Purpose = "Travel",
            Status = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Expenses = [new ExpenseItem { Description = "Train", Amount = 42.50m }]
        };
        var outbox = new DisbursementNotificationOutbox(context, Options.Create(new DisbursementNotificationOptions
        {
            ManagementFrontendBaseUrl = "https://management.test.akgaming.de"
        }));
        context.Reimbursements.Add(reimbursement);
        outbox.EnqueueSubmitted(reimbursement);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.That(await context.Reimbursements.CountAsync(), Is.EqualTo(1));
        var message = await context.NotificationOutbox.SingleAsync();
        Assert.That(message.Type, Is.EqualTo("reimbursement.submitted"));
        Assert.That(message.PayloadJson, Does.Contain(reimbursement.Id.ToString()));
        Assert.That(message.PayloadJson, Does.Contain("https://management.test.akgaming.de/disbursements/reimbursements/my"));
        Assert.That(message.PayloadJson, Does.Not.Contain("management.test.akgaming.de/api/"));
    }

    [Test]
    [Description("Applies the SQLite migration and persists the complete event, allocation, application, and approval graph.")]
    public async Task Database_WhenMigrated_PersistsAllocationGraph()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var applicantUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var disbursementEvent = new DisbursementEvent
        {
            Name = "Summer cup", CreatedAt = DateTimeOffset.UtcNow,
            Allocations = [new Allocation { Name = "First place", Amount = 300, Applications = [new AllocationApplication { ApplicantUserId = applicantUserId, ApplicantName = "Player", Amount = 150, CreatedAt = DateTimeOffset.UtcNow, Approvals = [new AllocationApproval { ApproverUserId = approverUserId, ApproverName = "Teammate", IsApproved = true, CreatedAt = DateTimeOffset.UtcNow }] }] }]
        };
        context.Add(disbursementEvent);

        // Act
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var stored = await context.DisbursementEvents.Include(item => item.Allocations).ThenInclude(item => item.Applications).ThenInclude(item => item.Approvals).SingleAsync();

        // Assert
        Assert.That(stored.Allocations.Single().Applications.Single().Approvals.Single().ApproverUserId, Is.EqualTo(approverUserId));
    }

    [Test]
    [Description("Loads SQLite reimbursements in descending creation order without translating DateTimeOffset ordering to SQL.")]
    public async Task Repository_WhenLoadingReimbursements_OrdersDateTimeOffsetsOnClient()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var userId = Guid.NewGuid();
        context.Reimbursements.AddRange(
            new Reimbursement { UserId = userId, ApplicantName = "User", Purpose = "Older", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new Reimbursement { UserId = userId, ApplicantName = "User", Purpose = "Newer", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();
        var repository = new EfDisbursementRepository(context);

        // Act
        var items = await repository.GetReimbursementsAsync(userId, CancellationToken.None);

        // Assert
        Assert.That(items.Select(item => item.Purpose), Is.EqualTo(new[] { "Newer", "Older" }));
    }

    [Test]
    [Description("Inserts a first-time allocation approval after loading the application from SQLite instead of updating a nonexistent row.")]
    public async Task Service_WhenApprovingLoadedApplication_InsertsApproval()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var applicantUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var allocation = new Allocation
        {
            Name = "Team prize",
            Amount = 200,
            Event = new DisbursementEvent { Name = "Summer cup", CreatedAt = DateTimeOffset.UtcNow },
            Applications =
            [
                new AllocationApplication
                {
                    ApplicantUserId = applicantUserId,
                    ApplicantName = "Applicant",
                    Amount = 200,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };
        context.Add(allocation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new EfDisbursementRepository(context);
        var storage = new Mock<IReceiptFileStorage>(MockBehavior.Strict);
        var payments = new Mock<IPaymentInformationService>(MockBehavior.Strict);
        var notificationOutbox = new Mock<IDisbursementNotificationOutbox>();
        var service = new DisbursementService(repository, storage.Object, payments.Object, notificationOutbox.Object);

        // Act
        var result = await service.DecideAsync(
            allocation.ShareToken,
            allocation.Applications.Single().Id,
            approverUserId,
            "Teammate",
            new DecideAllocationApplicationRequest { IsApproved = true });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        context.ChangeTracker.Clear();
        var storedApproval = await context.AllocationApprovals.SingleAsync();
        Assert.That(storedApproval.ApproverUserId, Is.EqualTo(approverUserId));
        Assert.That(storedApproval.IsApproved, Is.True);
    }

    [Test]
    [Description("Persists an adjusted SQLite claim, removes its approvals, and records that Discord must start a new review.")]
    public async Task Service_WhenAmountChanges_PersistsResetAndNewReviewNotification()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var application = new AllocationApplication
        {
            ApplicantUserId = Guid.NewGuid(),
            ApplicantName = "Applicant",
            Amount = 100m,
            Status = (int)AllocationApplicationStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow,
            Approvals =
            [
                new AllocationApproval
                {
                    ApproverUserId = Guid.NewGuid(),
                    ApproverName = "Teammate",
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };
        var allocation = new Allocation
        {
            Name = "Team prize",
            Amount = 300m,
            DiscordChannelId = "channel-123",
            DiscordRoleId = "role-456",
            Event = new DisbursementEvent { Name = "Summer cup", CreatedAt = DateTimeOffset.UtcNow },
            Applications = [application]
        };
        context.Add(allocation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new EfDisbursementRepository(context);
        var outbox = new DisbursementNotificationOutbox(context,
            Options.Create(new DisbursementNotificationOptions
            {
                ManagementFrontendBaseUrl = "https://management.test.akgaming.de"
            }));
        var service = new DisbursementService(
            repository,
            Mock.Of<IReceiptFileStorage>(),
            Mock.Of<IPaymentInformationService>(),
            outbox);

        // Act
        var result = await service.UpdateAllocationApplicationAsync(
            application.Id,
            new UpdateAllocationApplicationRequest { Amount = 150m, Note = "Updated" });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        context.ChangeTracker.Clear();
        var stored = await context.AllocationApplications
            .Include(item => item.Approvals)
            .SingleAsync(item => item.Id == application.Id);
        var notification = await context.NotificationOutbox.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Amount, Is.EqualTo(150m));
            Assert.That(stored.Status, Is.EqualTo((int)AllocationApplicationStatus.Submitted));
            Assert.That(stored.Approvals, Is.Empty);
            Assert.That(notification.PayloadJson, Does.Contain("\"startsNewReview\":true"));
        });
    }

    [Test]
    [Description("Releases a cancelled SQLite claim so another applicant can reserve the same allocation amount.")]
    public async Task Service_WhenClaimIsCancelled_ReleasesAmountForAnotherClaim()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite(connection, database => database.MigrationsAssembly("AkGaming.Management.Modules.Disbursements.Migrations.Sqlite"))
            .Options;
        await using var context = new DisbursementsDbContext(options);
        await context.Database.MigrateAsync();
        var existingApplication = new AllocationApplication
        {
            ApplicantUserId = Guid.NewGuid(),
            ApplicantName = "First applicant",
            Amount = 100m,
            Status = (int)AllocationApplicationStatus.Submitted,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var allocation = new Allocation
        {
            Name = "Team prize",
            Amount = 100m,
            Event = new DisbursementEvent { Name = "Summer cup", CreatedAt = DateTimeOffset.UtcNow },
            Applications = [existingApplication]
        };
        context.Add(allocation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var newApplicantId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        var payments = new Mock<IPaymentInformationService>(MockBehavior.Strict);
        payments.Setup(service => service.GetForUserAsync(newApplicantId))
            .ReturnsAsync(Result<ICollection<PaymentInformationDto>>.Success(
                [
                    new PaymentInformationDto
                    {
                        Id = paymentMethodId,
                        Type = PaymentInformationType.PayPal,
                        PayPalEmail = "applicant@example.org"
                    }
                ]));
        var repository = new EfDisbursementRepository(context);
        var notifications = new Mock<IDisbursementNotificationOutbox>();
        var service = new DisbursementService(
            repository,
            Mock.Of<IReceiptFileStorage>(),
            payments.Object,
            notifications.Object);

        // Act
        var cancellation = await service.CancelAllocationApplicationAsync(existingApplication.Id);
        var replacement = await service.ApplyAsync(
            allocation.ShareToken,
            newApplicantId,
            "Second applicant",
            new CreateAllocationApplicationRequest
            {
                Amount = 100m,
                PaymentInformationId = paymentMethodId
            });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cancellation.IsSuccess, Is.True);
            Assert.That(replacement.IsSuccess, Is.True);
            Assert.That(replacement.Value!.Amount, Is.EqualTo(100m));
        });
        context.ChangeTracker.Clear();
        var statuses = await context.AllocationApplications
            .Select(item => item.Status)
            .ToListAsync();
        Assert.That(statuses, Is.EquivalentTo(new[]
        {
            (int)AllocationApplicationStatus.Cancelled,
            (int)AllocationApplicationStatus.Submitted
        }));
    }
}
