using System.ComponentModel;
using AkGaming.Core.Notifications;
using AkGaming.Identity.Api.Controllers;
using AkGaming.Identity.Domain.Entities;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class InternalAuditSummaryControllerTests
{
    [Fact]
    [Description("Aggregates successful and failed Identity audit events within the requested weekly interval.")]
    public async Task Get_WithWeeklyInterval_ReturnsFilteredAggregate()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var dbContext = new AuthDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.AuditLogs.AddRange(
            CreateLog("login", true, new DateTime(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc), null),
            CreateLog("login", false, new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc), null),
            CreateLog("role.updated", true, new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc), null),
            CreateLog("login", true, new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), null));
        await dbContext.SaveChangesAsync();
        var controller = new InternalAuditSummaryController(dbContext);

        // Act
        var result = await controller.Get(DateTimeOffset.Parse("2026-07-13T00:00:00Z"),
            DateTimeOffset.Parse("2026-07-20T00:00:00Z"), CancellationToken.None);

        // Assert
        var response = ((OkObjectResult)result.Result!).Value as AuditSummaryResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.TotalEvents);
        Assert.Equal(2, response.SuccessfulEvents);
        Assert.Equal(1, response.FailedEvents);
        Assert.Equal(new AuditSummaryCategory("login", 2), response.TopCategories[0]);
    }

    private static AuditLog CreateLog(string eventType, bool success, DateTime createdAtUtc, Guid? userId)
    {
        return new AuditLog
        {
            EventType = eventType,
            Success = success,
            CreatedAtUtc = createdAtUtc,
            UserId = userId
        };
    }
}
