using AkGaming.Management.Modules.BoardManagement.Application.Interfaces;
using AkGaming.Management.Modules.BoardManagement.Application.Services;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AkGaming.Management.Modules.BoardManagement.Tests.Infrastructure;

[TestFixture]
public sealed class SqliteBoardMeetingServiceTests
{
    private SqliteConnection _connection = null!;
    private BoardManagementDbContext _dbContext = null!;
    private Mock<IBoardNotificationOutbox> _notifications = null!;
    private BoardMeetingService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BoardManagementDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new BoardManagementDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        var repository = new EfBoardMeetingRepository(_dbContext);
        _notifications = new Mock<IBoardNotificationOutbox>();
        _service = new BoardMeetingService(repository, _notifications.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    [Description("Persists a board member's first availability response as a new SQLite row.")]
    public async Task SetAvailabilityAsync_FirstResponse_InsertsAvailability()
    {
        // Arrange
        var meeting = await CreateMeetingAsync();
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.SetAvailabilityAsync(
            meeting.Id,
            userId,
            "Board Member",
            BoardAvailabilityStatusDto.Available,
            null,
            CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var saved = await _dbContext.Availabilities.AsNoTracking().SingleAsync();
        Assert.That(saved.UserId, Is.EqualTo(userId));
        Assert.That(saved.Status, Is.EqualTo(BoardAvailabilityStatus.Available));
        Assert.That(saved.ScheduleVersion, Is.EqualTo(1));
    }

    [Test]
    [Description("Persists a rescheduling proposal as a new SQLite row and queues its notification.")]
    public async Task ProposeRescheduleAsync_ValidProposal_InsertsProposal()
    {
        // Arrange
        var meeting = await CreateMeetingAsync();
        var actorUserId = Guid.NewGuid();
        var proposedAtUtc = DateTimeOffset.UtcNow.AddDays(2);
        var request = new CreateRescheduleProposalRequest(proposedAtUtc, 120, "More board members are available");

        // Act
        var result = await _service.ProposeRescheduleAsync(
            meeting.Id,
            request,
            actorUserId,
            "Board Member",
            null,
            CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var saved = await _dbContext.RescheduleProposals.AsNoTracking().SingleAsync();
        Assert.That(saved.MeetingId, Is.EqualTo(meeting.Id));
        Assert.That(saved.ProposedByUserId, Is.EqualTo(actorUserId));
        Assert.That(saved.ProposedAtUtc, Is.EqualTo(proposedAtUtc));
        _notifications.Verify(
            x => x.EnqueueRescheduleProposed(It.Is<BoardMeeting>(value => value.Id == meeting.Id), It.Is<BoardRescheduleProposal>(value => value.Id == saved.Id)),
            Times.Once);
    }

    [Test]
    [Description("Assigns selected backlog items to a meeting in selection order using one SQLite transaction.")]
    public async Task AssignAgendaItemsAsync_BacklogSelection_AssignsItemsInOrder()
    {
        // Arrange
        var meeting = await CreateMeetingAsync();
        var first = await CreateBacklogItemAsync("First selected");
        var second = await CreateBacklogItemAsync("Second selected");
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _service.AssignAgendaItemsAsync(
            meeting.Id,
            new AssignBoardAgendaItemsRequest([second.Id, first.Id]),
            CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var saved = await _dbContext.AgendaItems.AsNoTracking().OrderBy(x => x.Order).ToListAsync();
        Assert.That(saved.Select(x => x.Title), Is.EqualTo(new[] { "Second selected", "First selected" }));
        Assert.That(saved.All(x => x.MeetingId == meeting.Id), Is.True);
    }

    [Test]
    [Description("Persists the complete agenda order after a drag-and-drop reorder.")]
    public async Task ReorderAgendaItemsAsync_CompleteOrder_UpdatesEveryPosition()
    {
        // Arrange
        var meeting = await CreateMeetingAsync();
        var first = await CreateMeetingAgendaItemAsync(meeting.Id, "First", 0);
        var second = await CreateMeetingAgendaItemAsync(meeting.Id, "Second", 1);
        var third = await CreateMeetingAgendaItemAsync(meeting.Id, "Third", 2);
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _service.ReorderAgendaItemsAsync(
            meeting.Id,
            new ReorderBoardAgendaItemsRequest([third.Id, first.Id, second.Id]),
            CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var saved = await _dbContext.AgendaItems.AsNoTracking().OrderBy(x => x.Order).ToListAsync();
        Assert.That(saved.Select(x => x.Title), Is.EqualTo(new[] { "Third", "First", "Second" }));
        Assert.That(saved.Select(x => x.Order), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    [Description("Deletes an agenda item from SQLite and queues a notification containing the remaining meeting agenda.")]
    public async Task DeleteAgendaItemAsync_ExistingMeetingItem_RemovesItemAndQueuesAgendaSnapshot()
    {
        // Arrange
        var meeting = await CreateMeetingAsync();
        var retained = await CreateMeetingAgendaItemAsync(meeting.Id, "Retained", 0);
        var deleted = await CreateMeetingAgendaItemAsync(meeting.Id, "Deleted", 1);
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _service.DeleteAgendaItemAsync(deleted.Id, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var saved = await _dbContext.AgendaItems.AsNoTracking().ToListAsync();
        Assert.That(saved.Select(x => x.Id), Is.EqualTo(new[] { retained.Id }));
        _notifications.Verify(
            x => x.EnqueueAgendaChanged(
                It.Is<BoardMeeting>(value => value.Id == meeting.Id && value.AgendaItems.Count == 1 && value.AgendaItems.Single().Id == retained.Id),
                It.Is<IReadOnlyCollection<BoardAgendaItem>>(items => items.Count == 1 && items.Single().Id == deleted.Id),
                "deleted"),
            Times.Once);
    }

    private async Task<BoardMeeting> CreateMeetingAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var meeting = new BoardMeeting
        {
            Title = "Board meeting",
            ScheduledAtUtc = now.AddDays(1),
            DurationMinutes = 90,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _dbContext.Meetings.Add(meeting);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return meeting;
    }

    private async Task<BoardAgendaItem> CreateBacklogItemAsync(string title)
    {
        return await CreateAgendaItemAsync(null, title, 0);
    }

    private async Task<BoardAgendaItem> CreateMeetingAgendaItemAsync(Guid meetingId, string title, int order)
    {
        return await CreateAgendaItemAsync(meetingId, title, order);
    }

    private async Task<BoardAgendaItem> CreateAgendaItemAsync(Guid? meetingId, string title, int order)
    {
        var now = DateTimeOffset.UtcNow;
        var item = new BoardAgendaItem
        {
            MeetingId = meetingId,
            Title = title,
            Status = meetingId.HasValue ? BoardAgendaItemStatus.Scheduled : BoardAgendaItemStatus.Backlog,
            Order = order,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _dbContext.AgendaItems.Add(item);
        await _dbContext.SaveChangesAsync();
        return item;
    }
}
