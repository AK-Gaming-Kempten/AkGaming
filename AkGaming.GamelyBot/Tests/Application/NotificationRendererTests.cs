using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Domain;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Tests.Application;

[TestFixture]
public sealed class NotificationRendererTests
{
    [Test]
    [Description("Renders a weekly audit summary as an administration-channel message without a role mention.")]
    public void Render_WhenWeeklyAuditSummaryIsQueued_ReturnsAdministrationMessage()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions()));
        var summary = new AuditSummaryResponse("Identity", DateTimeOffset.Parse("2026-07-13T07:00:00Z"),
            DateTimeOffset.Parse("2026-07-20T07:00:00Z"), 12, 4, 10, 2,
            [new AuditSummaryCategory("login.succeeded", 8)]);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.IdentityAuditSummary,
            DataJson = JsonSerializer.Serialize(new AuditSummaryNotification(summary),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage, Is.Not.Null);
        Assert.That(rendered.ChannelMessage!.Title, Is.EqualTo("Identity weekly audit summary"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("login.succeeded: 8"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("Failed: **2**"));
        Assert.That(rendered.ChannelMessage.RoleId, Is.Null);
        Assert.That(rendered.DirectMessage, Is.Null);
    }

    [Test]
    [Description("Renders a reimbursement submission for the treasurer channel and the linked applicant.")]
    public void Render_WhenReimbursementIsSubmitted_ReturnsChannelAndDirectMessages()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions { TreasurerRoleId = "treasurer" }));
        var payload = new ReimbursementSubmittedNotification(Guid.NewGuid(), "Applicant", "Travel", 42.50m, "Submitted", "https://management.test/reimbursement");
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.ReimbursementSubmitted,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage, Is.Not.Null);
        Assert.That(rendered.ChannelMessage!.RoleId, Is.EqualTo("treasurer"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("42,50 EUR"));
        Assert.That(rendered.DirectMessage, Is.Not.Null);
    }

    [Test]
    [Description("Renders reimbursement status changes only as a private applicant notification.")]
    public void Render_WhenReimbursementStatusChanges_ReturnsOnlyDirectMessage()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions()));
        var payload = new ReimbursementStatusChangedNotification(Guid.NewGuid(), "Applicant", "Travel", 42.50m, "Submitted", "Approved", "Looks good", null);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.ReimbursementStatusChanged,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage, Is.Null);
        Assert.That(rendered.DirectMessage!.Body, Does.Contain("Approved"));
        Assert.That(rendered.DirectMessage.Body, Does.Contain("Looks good"));
    }

    [Test]
    [Description("Renders a board meeting in the board channel with schedule-versioned availability buttons.")]
    public void Render_WhenBoardMeetingIsCreated_ReturnsInteractiveBoardMessage()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions { BoardChannelId = "board-channel", ExtendedBoardRoleId = "extended-board-role" }));
        var meetingId = Guid.NewGuid();
        var payload = new BoardMeetingNotification(meetingId, "Board meeting", DateTimeOffset.UtcNow.AddDays(1), 90, "Club room", 4, null, null, ["Budget", "Upcoming events"]);
        var notification = new NotificationInboxItem { Type = NotificationEventTypes.BoardMeetingCreated, DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.ChannelId, Is.EqualTo("board-channel"));
        Assert.That(rendered.ChannelMessage?.RoleId, Is.EqualTo("extended-board-role"));
        Assert.That(rendered.ChannelMessage?.Buttons, Has.Count.EqualTo(3));
        Assert.That(rendered.ChannelMessage!.Buttons![0].CustomId, Does.Contain($"{meetingId}:4:available"));
        Assert.That(rendered.ChannelMessage.Buttons[2].CustomId, Is.EqualTo($"board-reschedule:{meetingId}:4"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("Agenda"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("1. Budget"));
    }

    [Test]
    [Description("Routes new membership applications to the administration channel and pings only the board role.")]
    public void Render_WhenMembershipApplicationIsCreated_ReturnsBoardNotification()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions
        {
            BoardRoleId = "board-role",
            ExtendedBoardRoleId = "extended-board-role"
        }));
        var payload = new MembershipApplicationCreatedNotification(Guid.NewGuid(), "Erika Mustermann",
            "erika@example.com", "https://management.test/member-management/requests");
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.MembershipApplicationCreated,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rendered.ChannelMessage?.RoleId, Is.EqualTo("board-role"));
            Assert.That(rendered.ChannelMessage?.ChannelId, Is.Null);
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("Erika Mustermann"));
        });
    }

    [Test]
    [Description("Routes new member linking requests to the administration channel and pings only the board role.")]
    public void Render_WhenMemberLinkingRequestIsCreated_ReturnsBoardNotification()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions
        {
            BoardRoleId = "board-role",
            ExtendedBoardRoleId = "extended-board-role"
        }));
        var payload = new MemberLinkingRequestCreatedNotification(Guid.NewGuid(), "Max Mustermann",
            "max@example.com", "NewRegistration", "https://management.test/member-management/requests");
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.MemberLinkingRequestCreated,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rendered.ChannelMessage?.RoleId, Is.EqualTo("board-role"));
            Assert.That(rendered.ChannelMessage?.ChannelId, Is.Null);
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("NewRegistration"));
        });
    }

    [Test]
    [Description("Renders an automatic board meeting reminder with the current agenda and response controls.")]
    public void Render_WhenBoardMeetingReminderIsQueued_ReturnsInteractiveReminder()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions
        {
            BoardChannelId = "board-channel",
            ExtendedBoardRoleId = "extended-board-role"
        }));
        var meetingId = Guid.NewGuid();
        var payload = new BoardMeetingNotification(meetingId, "Board meeting", DateTimeOffset.UtcNow.AddHours(1),
            90, "Club room", 2, null, "https://management.test/board/meetings", ["Budget"]);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.BoardMeetingReminder,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.Title, Is.EqualTo("Board meeting reminder"));
        Assert.That(rendered.ChannelMessage?.ChannelId, Is.EqualTo("board-channel"));
        Assert.That(rendered.ChannelMessage?.Buttons, Has.Count.EqualTo(3));
        Assert.That(rendered.ChannelMessage?.Body, Does.Contain("Budget"));
    }

    [Test]
    [Description("Renders a rescheduling proposal with accept and reject controls for the board channel.")]
    public void Render_WhenRescheduleIsProposed_ReturnsDecisionButtons()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions
        {
            BoardChannelId = "board-channel",
            ExtendedBoardRoleId = "extended-board-role"
        }));
        var meetingId = Guid.NewGuid();
        var proposalId = Guid.NewGuid();
        var payload = new BoardRescheduleProposalNotification(meetingId, proposalId, "Board meeting",
            DateTimeOffset.UtcNow.AddDays(2), 90, "Conflict", "Board Member", null);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.BoardMeetingRescheduleProposed,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.Buttons, Has.Count.EqualTo(2));
        Assert.That(rendered.ChannelMessage!.Buttons![0].CustomId, Is.EqualTo($"bp:{meetingId}:{proposalId}:a"));
        Assert.That(rendered.ChannelMessage.Buttons[1].CustomId, Is.EqualTo($"bp:{meetingId}:{proposalId}:r"));
    }

    [Test]
    [Description("Renders the complete updated agenda and highlights newly added entries.")]
    public void Render_WhenAgendaItemIsAdded_ReturnsFullAgendaWithAdditionMarker()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions { BoardChannelId = "board-channel" }));
        var unchanged = new BoardAgendaNotificationItem(Guid.NewGuid(), "Budget", 0);
        var added = new BoardAgendaNotificationItem(Guid.NewGuid(), "Summer event", 1);
        var payload = new BoardAgendaChangedNotification(
            Guid.NewGuid(),
            "Board meeting",
            "added",
            null,
            [unchanged, added],
            [added]);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.BoardAgendaChanged,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.Body, Does.Contain("1. Budget"));
        Assert.That(rendered.ChannelMessage?.Body, Does.Contain("+ **Summer event**"));
    }

    [Test]
    [Description("Renders deleted agenda entries struck through after the remaining agenda.")]
    public void Render_WhenAgendaItemIsDeleted_ReturnsStruckThroughEntry()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions { BoardChannelId = "board-channel" }));
        var remaining = new BoardAgendaNotificationItem(Guid.NewGuid(), "Budget", 0);
        var deleted = new BoardAgendaNotificationItem(Guid.NewGuid(), "Old topic", 1);
        var payload = new BoardAgendaChangedNotification(
            Guid.NewGuid(),
            "Board meeting",
            "deleted",
            null,
            [remaining],
            [deleted]);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.BoardAgendaChanged,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.Body, Does.Contain("1. Budget"));
        Assert.That(rendered.ChannelMessage?.Body, Does.Contain("- ~~Old topic~~"));
    }
}
