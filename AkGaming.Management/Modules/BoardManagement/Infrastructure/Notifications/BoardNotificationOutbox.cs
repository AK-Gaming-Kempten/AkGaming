using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.BoardManagement.Application.Interfaces;
using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.BoardManagement.Infrastructure.Notifications;

public sealed class BoardNotificationOutbox(BoardManagementDbContext dbContext, IOptions<BoardNotificationOptions> options) : IBoardNotificationOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BoardNotificationOptions _options = options.Value;
    public void EnqueueMeetingCreated(BoardMeeting meeting) => Enqueue(NotificationEventTypes.BoardMeetingCreated, MeetingData(meeting, null));
    public void EnqueueMeetingRescheduled(BoardMeeting meeting, string? reason) => Enqueue(NotificationEventTypes.BoardMeetingRescheduled, MeetingData(meeting, reason));
    public void EnqueueMeetingCancelled(BoardMeeting meeting) => Enqueue(NotificationEventTypes.BoardMeetingCancelled, MeetingData(meeting, null));
    public void EnqueueRescheduleProposed(BoardMeeting meeting, BoardRescheduleProposal proposal) => Enqueue(NotificationEventTypes.BoardMeetingRescheduleProposed, new BoardRescheduleProposalNotification(meeting.Id, proposal.Id, meeting.Title, proposal.ProposedAtUtc, proposal.DurationMinutes, proposal.Reason, proposal.ProposedByDisplayName, MeetingUrl(meeting.Id)));
    public void EnqueueAgendaChanged(BoardMeeting? meeting, IReadOnlyCollection<BoardAgendaItem> changedItems, string action)
    {
        var agendaItems = meeting?.AgendaItems
            .OrderBy(x => x.Order)
            .Select(AgendaItemData)
            .ToList() ?? [];
        var changes = changedItems.Select(AgendaItemData).ToList();
        var data = new BoardAgendaChangedNotification(
            meeting?.Id,
            meeting?.Title,
            action,
            meeting is null ? BoardUrl() : MeetingUrl(meeting.Id),
            agendaItems,
            changes);
        Enqueue(NotificationEventTypes.BoardAgendaChanged, data);
    }

    private BoardMeetingNotification MeetingData(BoardMeeting meeting, string? reason) => new(
        meeting.Id,
        meeting.Title,
        meeting.ScheduledAtUtc,
        meeting.DurationMinutes,
        meeting.Location,
        meeting.ScheduleVersion,
        reason,
        MeetingUrl(meeting.Id),
        meeting.AgendaItems.OrderBy(x => x.Order).Select(x => x.Title).ToList());
    private static BoardAgendaNotificationItem AgendaItemData(BoardAgendaItem item) => new(item.Id, item.Title, item.Order);
    private void Enqueue<T>(string type, T data)
    {
        var eventId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var envelope = new NotificationEnvelope(eventId, type, "management", now, null, JsonSerializer.SerializeToElement(data, JsonOptions));
        dbContext.NotificationOutbox.Add(new BoardNotificationOutboxMessage { EventId = eventId, Type = type, PayloadJson = JsonSerializer.Serialize(envelope, JsonOptions), CreatedAtUtc = now });
    }
    private string? MeetingUrl(Guid id) => string.IsNullOrWhiteSpace(_options.ManagementBaseUrl) ? null : $"{_options.ManagementBaseUrl.TrimEnd('/')}/board/meetings/{id}";
    private string? BoardUrl() => string.IsNullOrWhiteSpace(_options.ManagementBaseUrl) ? null : $"{_options.ManagementBaseUrl.TrimEnd('/')}/board/meetings";
}
