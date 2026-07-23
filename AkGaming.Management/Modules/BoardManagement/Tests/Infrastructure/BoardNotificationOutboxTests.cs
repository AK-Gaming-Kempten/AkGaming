using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using System.Text.Json;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Notifications;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.BoardManagement.Tests.Infrastructure;

[TestFixture]
public sealed class BoardNotificationOutboxTests
{
    private BoardManagementDbContext _dbContext = null!;
    private BoardNotificationOutbox _outbox = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<BoardManagementDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _dbContext = new BoardManagementDbContext(options);
        _outbox = new BoardNotificationOutbox(_dbContext, Options.Create(new BoardNotificationOptions()));
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    [Description("Does not queue an agenda notification for a cancelled board meeting.")]
    public void EnqueueAgendaChanged_CancelledMeeting_DoesNotQueueMessage()
    {
        // Arrange
        var meeting = CreateMeeting(DateTimeOffset.UtcNow.AddDays(1), BoardMeetingStatus.Cancelled);
        var item = CreateAgendaItem(meeting);

        // Act
        _outbox.EnqueueAgendaChanged(meeting, [item], "updated");

        // Assert
        Assert.That(_dbContext.NotificationOutbox.Local, Is.Empty);
    }

    [Test]
    [Description("Does not queue an agenda notification after a board meeting has ended.")]
    public void EnqueueAgendaChanged_EndedMeeting_DoesNotQueueMessage()
    {
        // Arrange
        var meeting = CreateMeeting(DateTimeOffset.UtcNow.AddHours(-2), BoardMeetingStatus.Scheduled);
        var item = CreateAgendaItem(meeting);

        // Act
        _outbox.EnqueueAgendaChanged(meeting, [item], "updated");

        // Assert
        Assert.That(_dbContext.NotificationOutbox.Local, Is.Empty);
    }

    [Test]
    [Description("Queues an agenda notification while the associated board meeting is still in progress.")]
    public void EnqueueAgendaChanged_OngoingMeeting_QueuesMessage()
    {
        // Arrange
        var meeting = CreateMeeting(DateTimeOffset.UtcNow.AddMinutes(-30), BoardMeetingStatus.Scheduled);
        var item = CreateAgendaItem(meeting);

        // Act
        _outbox.EnqueueAgendaChanged(meeting, [item], "updated");

        // Assert
        Assert.That(_dbContext.NotificationOutbox.Local, Has.Count.EqualTo(1));
    }

    [Test]
    [Description("Uses the Management frontend base URL for links in board meeting notifications.")]
    public void EnqueueMeetingCreated_WithFrontendBaseUrl_QueuesFrontendLink()
    {
        // Arrange
        var outbox = new BoardNotificationOutbox(_dbContext, Options.Create(new BoardNotificationOptions
        {
            ManagementFrontendBaseUrl = "https://management.test.akgaming.de/"
        }));
        var meeting = CreateMeeting(DateTimeOffset.UtcNow.AddDays(1), BoardMeetingStatus.Scheduled);

        // Act
        outbox.EnqueueMeetingCreated(meeting);

        // Assert
        var message = _dbContext.NotificationOutbox.Local.Single();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.That(payload.RootElement.GetProperty("data").GetProperty("managementUrl").GetString(),
            Is.EqualTo($"https://management.test.akgaming.de/board/meetings/{meeting.Id}"));
    }

    [Test]
    [Description("Queues confirmed and declined attendee names in an availability snapshot.")]
    public void EnqueueAvailabilityChanged_WithResponses_QueuesAttendanceLists()
    {
        // Arrange
        var meeting = CreateMeeting(DateTimeOffset.UtcNow.AddDays(1), BoardMeetingStatus.Scheduled);
        meeting.Availabilities.Add(new BoardAvailability
        {
            DisplayName = "Available Member",
            Status = BoardAvailabilityStatus.Available
        });
        meeting.Availabilities.Add(new BoardAvailability
        {
            DisplayName = "Unavailable Member",
            Status = BoardAvailabilityStatus.Unavailable
        });

        // Act
        _outbox.EnqueueAvailabilityChanged(meeting);

        // Assert
        var message = _dbContext.NotificationOutbox.Local.Single();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        var data = payload.RootElement.GetProperty("data");
        Assert.That(message.Type, Is.EqualTo(NotificationEventTypes.BoardMeetingAvailabilityChanged));
        Assert.That(data.GetProperty("confirmedAttendees")[0].GetString(), Is.EqualTo("Available Member"));
        Assert.That(data.GetProperty("declinedAttendees")[0].GetString(), Is.EqualTo("Unavailable Member"));
    }

    [Test]
    [Description("Queues a decided rescheduling proposal with its final status.")]
    public void EnqueueRescheduleProposalChanged_WithAcceptedProposal_QueuesFinalStatus()
    {
        // Arrange
        var meeting = CreateMeeting(DateTimeOffset.UtcNow.AddDays(1), BoardMeetingStatus.Scheduled);
        var proposal = new BoardRescheduleProposal
        {
            MeetingId = meeting.Id,
            ProposedAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            DurationMinutes = 90,
            ProposedByDisplayName = "Board Member",
            Status = RescheduleProposalStatus.Accepted
        };

        // Act
        _outbox.EnqueueRescheduleProposalChanged(meeting, proposal);

        // Assert
        var message = _dbContext.NotificationOutbox.Local.Single();
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.That(payload.RootElement.GetProperty("data").GetProperty("status").GetString(),
            Is.EqualTo("Accepted"));
    }

    private static BoardMeeting CreateMeeting(DateTimeOffset scheduledAtUtc, BoardMeetingStatus status)
    {
        return new BoardMeeting
        {
            Title = "Board meeting",
            ScheduledAtUtc = scheduledAtUtc,
            DurationMinutes = 90,
            Status = status
        };
    }

    private static BoardAgendaItem CreateAgendaItem(BoardMeeting meeting)
    {
        var item = new BoardAgendaItem
        {
            MeetingId = meeting.Id,
            Meeting = meeting,
            Title = "Budget",
            Status = BoardAgendaItemStatus.Scheduled
        };
        meeting.AgendaItems.Add(item);
        return item;
    }
}
