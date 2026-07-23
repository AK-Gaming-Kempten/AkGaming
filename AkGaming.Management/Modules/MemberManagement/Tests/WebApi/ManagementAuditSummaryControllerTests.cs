using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Domain.Entities;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using AkGaming.Management.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Tests.Infrastructure;

[TestFixture]
public sealed class ManagementAuditSummaryControllerTests
{
    [Test]
    [Description("Combines weekly membership activity with current member and reimbursement work queues.")]
    public async Task Get_WithWeeklyInterval_ReturnsOperationalMetrics()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MemberManagementDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var dbContext = new MemberManagementDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var disbursementOptions = new DbContextOptionsBuilder<DisbursementsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var disbursementsDbContext = new DisbursementsDbContext(disbursementOptions);
        await disbursementsDbContext.Database.OpenConnectionAsync();
        await disbursementsDbContext.Database.EnsureCreatedAsync();
        var actorId = Guid.NewGuid();
        dbContext.MemberAuditLogs.AddRange(
            CreateLog("MembershipApplicationRequestCreated", new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc), actorId),
            CreateLog("MembershipApplicationRequestAccepted", new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc), actorId),
            CreateLog("MemberLinkingRequestCreated", new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc), null),
            CreateLog("MembershipApplicationRequestCreated", new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), actorId));
        dbContext.MembershipApplicationRequests.Add(new MembershipApplicationRequest
        {
            Id = Guid.NewGuid(),
            IssuingUserId = Guid.NewGuid(),
            IsResolved = false
        });
        disbursementsDbContext.Reimbursements.AddRange(
            new Reimbursement
            {
                CreatedAt = DateTimeOffset.Parse("2026-07-17T10:00:00Z"),
                Status = (int)DisbursementStatus.Submitted
            },
            new Reimbursement
            {
                CreatedAt = DateTimeOffset.Parse("2026-07-01T10:00:00Z"),
                Status = (int)DisbursementStatus.Paid
            });
        await dbContext.SaveChangesAsync();
        await disbursementsDbContext.SaveChangesAsync();
        var controller = new ManagementAuditSummaryController(dbContext, disbursementsDbContext);

        // Act
        var result = await controller.Get(DateTimeOffset.Parse("2026-07-13T00:00:00Z"),
            DateTimeOffset.Parse("2026-07-20T00:00:00Z"), CancellationToken.None);

        // Assert
        var response = ((OkObjectResult)result.Result!).Value as AuditSummaryResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TotalEvents, Is.EqualTo(4));
        Assert.That(response.UniqueActors, Is.EqualTo(1));
        Assert.That(response.Sections![0].Metrics[0],
            Is.EqualTo(new AuditSummaryCategory("Membership applications received", 1)));
        Assert.That(response.Sections[0].Metrics[6],
            Is.EqualTo(new AuditSummaryCategory("Reimbursements submitted", 1)));
        Assert.That(response.Sections[1].Metrics[0],
            Is.EqualTo(new AuditSummaryCategory("Open membership applications", 1)));
        Assert.That(response.Sections[1].Metrics[2],
            Is.EqualTo(new AuditSummaryCategory("Open reimbursements", 1)));
    }

    private static MemberAuditLog CreateLog(string action, DateTime occurredAtUtc, Guid? actorId)
    {
        return new MemberAuditLog
        {
            ActionType = action,
            EntityType = "Member",
            EntityId = Guid.NewGuid(),
            OccurredAtUtc = occurredAtUtc,
            PerformedByUserId = actorId
        };
    }
}
