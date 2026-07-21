using AkGaming.Management.Modules.BoardManagement.Application.Interfaces;
using AkGaming.Management.Modules.BoardManagement.Application.Services;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using Moq;

namespace AkGaming.Management.Modules.BoardManagement.Tests.Application;

[TestFixture]
public sealed class BoardMeetingServiceTests
{
    private Mock<IBoardMeetingRepository> _repository = null!;
    private Mock<IBoardNotificationOutbox> _notifications = null!;
    private BoardMeetingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IBoardMeetingRepository>();
        _notifications = new Mock<IBoardNotificationOutbox>();
        _service = new BoardMeetingService(_repository.Object, _notifications.Object);
    }

    [Test]
    [Description("Rescheduling a board meeting increments its schedule version and clears every existing availability response.")]
    public async Task RescheduleMeeting_WithAvailability_ResetsForecast()
    {
        // Arrange
        var meeting = new BoardMeeting { Id = Guid.NewGuid(), Title = "Board", ScheduledAtUtc = DateTimeOffset.UtcNow.AddDays(1), DurationMinutes = 60, ScheduleVersion = 3 };
        meeting.Availabilities.Add(new BoardAvailability { MeetingId = meeting.Id, UserId = Guid.NewGuid(), Status = BoardAvailabilityStatus.Available, ScheduleVersion = 3 });
        _repository.Setup(x => x.GetMeetingAsync(meeting.Id, It.IsAny<CancellationToken>())).ReturnsAsync(meeting);
        var request = new RescheduleBoardMeetingRequest(DateTimeOffset.UtcNow.AddDays(2), 90, "Conflict");

        // Act
        var result = await _service.RescheduleMeetingAsync(meeting.Id, request, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(meeting.ScheduleVersion, Is.EqualTo(4));
        Assert.That(meeting.Availabilities, Is.Empty);
        _notifications.Verify(x => x.EnqueueMeetingRescheduled(meeting, "Conflict"), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Description("A Discord availability response for an obsolete schedule version is rejected without changing the forecast.")]
    public async Task SetAvailability_WithStaleVersion_IsRejected()
    {
        // Arrange
        var meeting = new BoardMeeting { Id = Guid.NewGuid(), Title = "Board", ScheduleVersion = 2 };
        _repository.Setup(x => x.GetMeetingAsync(meeting.Id, It.IsAny<CancellationToken>())).ReturnsAsync(meeting);

        // Act
        var result = await _service.SetAvailabilityAsync(meeting.Id, Guid.NewGuid(), "Member", BoardAvailabilityStatusDto.Available, 1, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(meeting.Availabilities, Is.Empty);
        _repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Description("Creates initial agenda items before enqueueing the meeting-created notification.")]
    public async Task CreateMeeting_WithAgenda_CreatesAgendaBeforeNotification()
    {
        // Arrange
        BoardMeeting? notifiedMeeting = null;
        _notifications.Setup(x => x.EnqueueMeetingCreated(It.IsAny<BoardMeeting>()))
            .Callback<BoardMeeting>(meeting => notifiedMeeting = meeting);
        var request = new CreateBoardMeetingRequest(
            "Board meeting",
            DateTimeOffset.UtcNow.AddDays(1),
            90,
            "Club room",
            [new CreateBoardAgendaItemRequest("Budget", "Review the draft"), new CreateBoardAgendaItemRequest("Events", null)]);
        var actorUserId = Guid.NewGuid();

        // Act
        var result = await _service.CreateMeetingAsync(request, actorUserId, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(notifiedMeeting, Is.Not.Null);
        Assert.That(notifiedMeeting!.AgendaItems.Select(x => x.Title), Is.EqualTo(new[] { "Budget", "Events" }));
        Assert.That(notifiedMeeting.AgendaItems.Select(x => x.Order), Is.EqualTo(new[] { 0, 1 }));
        Assert.That(notifiedMeeting.AgendaItems.All(x => x.CreatedByUserId == actorUserId), Is.True);
        _repository.Verify(x => x.Add(notifiedMeeting), Times.Once);
    }
}
