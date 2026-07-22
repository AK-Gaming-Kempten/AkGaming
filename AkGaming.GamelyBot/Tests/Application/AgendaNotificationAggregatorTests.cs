using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Domain;

namespace AkGaming.GamelyBot.Tests.Application;

[TestFixture]
public sealed class AgendaNotificationAggregatorTests
{
    [Test]
    [Description("Combines rapid agenda changes into the latest agenda snapshot while retaining all changed items.")]
    public void Aggregate_WhenSeveralItemsChange_ReturnsLatestAgendaAndCombinedMarkers()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var retainedId = Guid.NewGuid();
        var addedId = Guid.NewGuid();
        var removedId = Guid.NewGuid();
        var notifications = new[]
        {
            CreateNotification(meetingId, "added",
                [new BoardAgendaNotificationItem(retainedId, "Existing", 0), new BoardAgendaNotificationItem(addedId, "Added", 1), new BoardAgendaNotificationItem(removedId, "Removed", 2)],
                [new BoardAgendaNotificationItem(addedId, "Added", 1)], DateTimeOffset.UtcNow),
            CreateNotification(meetingId, "deleted",
                [new BoardAgendaNotificationItem(retainedId, "Existing", 0), new BoardAgendaNotificationItem(addedId, "Added", 1)],
                [new BoardAgendaNotificationItem(removedId, "Removed", 2)], DateTimeOffset.UtcNow.AddSeconds(1))
        };

        // Act
        var result = AgendaNotificationAggregator.Aggregate(notifications);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AgendaItems, Has.Count.EqualTo(2));
            Assert.That(result.Changes, Has.Some.Matches<BoardAgendaNotificationChange>(change =>
                change.AgendaItemId == addedId && change.Action == "added"));
            Assert.That(result.Changes, Has.Some.Matches<BoardAgendaNotificationChange>(change =>
                change.AgendaItemId == removedId && change.Action == "removed"));
        });
    }

    [Test]
    [Description("Treats backlog and individual meetings as separate aggregation contexts.")]
    public void TryGetMeetingId_WhenNotificationTargetsBacklog_ReturnsNullContext()
    {
        // Arrange
        var notification = CreateNotification(null, "added", [], [], DateTimeOffset.UtcNow);

        // Act
        var isAgendaChange = AgendaNotificationAggregator.TryGetMeetingId(notification, out var meetingId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(isAgendaChange, Is.True);
            Assert.That(meetingId, Is.Null);
        });
    }

    private static NotificationInboxItem CreateNotification(
        Guid? meetingId,
        string action,
        IReadOnlyList<BoardAgendaNotificationItem> agenda,
        IReadOnlyList<BoardAgendaNotificationItem> changed,
        DateTimeOffset receivedAtUtc)
    {
        var payload = new BoardAgendaChangedNotification(meetingId, "Board meeting", action, null, agenda, changed);
        return new NotificationInboxItem
        {
            Type = NotificationEventTypes.BoardAgendaChanged,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            ReceivedAtUtc = receivedAtUtc
        };
    }
}
