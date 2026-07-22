using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Domain;

namespace AkGaming.GamelyBot.Application;

public static class AgendaNotificationAggregator
{
    public static bool TryGetMeetingId(NotificationInboxItem notification, out Guid? meetingId)
    {
        meetingId = null;
        if (notification.Type != NotificationEventTypes.BoardAgendaChanged)
        {
            return false;
        }

        var payload = Deserialize(notification);
        meetingId = payload.MeetingId;
        return true;
    }

    public static BoardAgendaChangedNotification Aggregate(IReadOnlyCollection<NotificationInboxItem> notifications)
    {
        var payloads = notifications
            .OrderBy(notification => notification.ReceivedAtUtc)
            .Select(Deserialize)
            .ToList();
        var latest = payloads[^1];
        var finalAgendaIds = (latest.AgendaItems ?? [])
            .Select(item => item.AgendaItemId)
            .ToHashSet();
        var changes = new Dictionary<Guid, BoardAgendaNotificationChange>();

        foreach (var payload in payloads)
        {
            var changedItems = payload.ChangedItems
                ?? (payload.AgendaItemId.HasValue && !string.IsNullOrWhiteSpace(payload.Title)
                    ? [new BoardAgendaNotificationItem(payload.AgendaItemId.Value, payload.Title, 0)]
                    : []);
            var eventAgendaIds = (payload.AgendaItems ?? [])
                .Select(item => item.AgendaItemId)
                .ToHashSet();

            foreach (var item in changedItems)
            {
                var action = !eventAgendaIds.Contains(item.AgendaItemId)
                    ? "removed"
                    : IsAddition(payload.Action) ? "added" : "updated";
                if (changes.TryGetValue(item.AgendaItemId, out var previous)
                    && previous.Action == "added" && action == "updated")
                {
                    action = "added";
                }
                changes[item.AgendaItemId] = new BoardAgendaNotificationChange(item.AgendaItemId, item.Title, action);
            }
        }

        foreach (var itemId in changes.Keys.ToList())
        {
            var change = changes[itemId];
            if (!finalAgendaIds.Contains(itemId))
            {
                changes[itemId] = change with { Action = "removed" };
            }
        }

        return latest with
        {
            Action = "aggregated",
            ChangedItems = null,
            AgendaItemId = null,
            Title = null,
            Changes = changes.Values.ToList()
        };
    }

    public static string Serialize(BoardAgendaChangedNotification payload)
    {
        return JsonSerializer.Serialize(payload, JsonOptions());
    }

    private static BoardAgendaChangedNotification Deserialize(NotificationInboxItem notification)
    {
        return JsonSerializer.Deserialize<BoardAgendaChangedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The board agenda payload is invalid.");
    }

    private static bool IsAddition(string action)
    {
        return action is "added" or "created" or "added from backlog";
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }
}
