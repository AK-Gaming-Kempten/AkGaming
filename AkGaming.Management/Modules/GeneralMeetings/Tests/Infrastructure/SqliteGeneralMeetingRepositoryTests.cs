using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;
using AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AkGaming.Core.Common.Email;
using AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;
using AkGaming.Management.Modules.GeneralMeetings.Application.Services;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Moq;

namespace AkGaming.Management.Modules.GeneralMeetings.Tests.Infrastructure;

[TestFixture]
public sealed class SqliteGeneralMeetingRepositoryTests
{
    [Test]
    [Description("Loads and orders general meetings by DateTimeOffset when the database provider is SQLite.")]
    public async Task GetAllAsync_WithSqlite_OrdersMeetingsOnClient()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<GeneralMeetingsDbContext>().UseSqlite(connection).Options;
        await using var db = new GeneralMeetingsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var earlier = new GeneralMeeting { Title = "Earlier", ScheduledAt = new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.FromHours(1)) };
        var later = new GeneralMeeting { Title = "Later", ScheduledAt = new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.FromHours(2)) };
        db.Meetings.AddRange(earlier, later);
        await db.SaveChangesAsync();
        var repository = new EfGeneralMeetingRepository(db);

        // Act
        var meetings = await repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.That(meetings.Select(x => x.Title), Is.EqualTo(new[] { "Later", "Earlier" }));
    }

    [Test]
    [Description("Adds an agenda item to a newly persisted meeting without triggering a false optimistic concurrency conflict.")]
    public async Task SaveAgendaItemAsync_WithSqlite_UpdatesMeetingVersion()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<GeneralMeetingsDbContext>().UseSqlite(connection).Options;
        await using var db = new GeneralMeetingsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var repository = new EfGeneralMeetingRepository(db);
        var service = new GeneralMeetingService(repository, Mock.Of<IMemberQueryService>(), Mock.Of<IEmailSender>(), Mock.Of<IBallotCredentialProtector>());
        var created = await service.CreateMeetingAsync(new SaveMeetingRequest("Meeting", DateTimeOffset.UtcNow, null), Guid.NewGuid(), CancellationToken.None);

        // Act
        var result = await service.SaveAgendaItemAsync(created.Value!.Id, null, new SaveAgendaItemRequest(null, "Opening", null, 0), Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await db.AgendaItems.CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Meetings.SingleAsync()).Version, Is.EqualTo(1));
    }
}
