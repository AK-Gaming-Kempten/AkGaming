using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.MemberManagement.Api.Controllers;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Tests.Infrastructure;

[TestFixture]
public sealed class MemberAuditSummaryControllerTests
{
    [Test]
    [Description("Aggregates only member-management audit events within the requested weekly interval.")]
    public async Task Get_WithWeeklyInterval_ReturnsFilteredAggregate()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MemberManagementDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var dbContext = new MemberManagementDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var actorId = Guid.NewGuid();
        dbContext.MemberAuditLogs.AddRange(
            CreateLog("member.updated", new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc), actorId),
            CreateLog("member.updated", new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc), actorId),
            CreateLog("member.created", new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc), null),
            CreateLog("member.deleted", new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), actorId));
        await dbContext.SaveChangesAsync();
        var controller = new MemberAuditSummaryController(dbContext);

        // Act
        var result = await controller.Get(DateTimeOffset.Parse("2026-07-13T00:00:00Z"),
            DateTimeOffset.Parse("2026-07-20T00:00:00Z"), CancellationToken.None);

        // Assert
        var response = ((OkObjectResult)result.Result!).Value as AuditSummaryResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.TotalEvents, Is.EqualTo(3));
        Assert.That(response.UniqueActors, Is.EqualTo(1));
        Assert.That(response.TopCategories[0], Is.EqualTo(new AuditSummaryCategory("member.updated", 2)));
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
