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
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions { BoardChannelId = "board-channel", BoardRoleId = "board-role" }));
        var meetingId = Guid.NewGuid();
        var payload = new BoardMeetingNotification(meetingId, "Board meeting", DateTimeOffset.UtcNow.AddDays(1), 90, "Club room", 4, null, null, ["Budget", "Upcoming events"]);
        var notification = new NotificationInboxItem { Type = NotificationEventTypes.BoardMeetingCreated, DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.ChannelId, Is.EqualTo("board-channel"));
        Assert.That(rendered.ChannelMessage?.RoleId, Is.EqualTo("board-role"));
        Assert.That(rendered.ChannelMessage?.Buttons, Has.Count.EqualTo(2));
        Assert.That(rendered.ChannelMessage!.Buttons![0].CustomId, Does.Contain($"{meetingId}:4:available"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("Agenda"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("1. Budget"));
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
