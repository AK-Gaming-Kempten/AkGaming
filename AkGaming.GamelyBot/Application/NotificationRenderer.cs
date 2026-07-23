using System.Globalization;
using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Domain;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Application;

public sealed class NotificationRoutingOptions
{
    public const string SectionName = "NotificationRouting";
    public string? TreasurerRoleId { get; set; }
    public string? BoardRoleId { get; set; }
    public string? ExtendedBoardRoleId { get; set; }
    public string? BoardChannelId { get; set; }
}

public sealed class NotificationRenderer(IOptions<NotificationRoutingOptions> options) : INotificationRenderer
{
    private readonly NotificationRoutingOptions _options = options.Value;

    public RenderedNotification Render(NotificationInboxItem notification)
    {
        return notification.Type switch
        {
            NotificationEventTypes.ReimbursementSubmitted => RenderSubmitted(notification),
            NotificationEventTypes.ReimbursementStatusChanged => RenderStatusChanged(notification),
            NotificationEventTypes.MembershipApplicationCreated => RenderMembershipApplicationCreated(notification),
            NotificationEventTypes.MembershipApplicationStatusChanged => RenderMembershipApplicationStatusChanged(notification),
            NotificationEventTypes.MemberLinkingRequestCreated => RenderMemberLinkingRequestCreated(notification),
            NotificationEventTypes.MemberLinkingRequestStatusChanged => RenderMemberLinkingRequestStatusChanged(notification),
            NotificationEventTypes.MembershipStatusChanged => RenderMembershipStatusChanged(notification),
            NotificationEventTypes.BoardMeetingCreated => RenderBoardMeeting(notification, "New board meeting"),
            NotificationEventTypes.BoardMeetingRescheduled => RenderBoardMeeting(notification, "Board meeting rescheduled"),
            NotificationEventTypes.BoardMeetingCancelled => RenderBoardMeeting(notification, "Board meeting cancelled", false),
            NotificationEventTypes.BoardMeetingReminder => RenderBoardMeeting(notification, "Board meeting reminder"),
            NotificationEventTypes.BoardMeetingAvailabilityChanged => RenderBoardMeeting(notification, "Board meeting"),
            NotificationEventTypes.BoardMeetingRescheduleProposed => RenderBoardProposal(notification),
            NotificationEventTypes.BoardAgendaChanged => RenderBoardAgendaChange(notification),
            NotificationEventTypes.IdentityAuditSummary => RenderAuditSummary(notification),
            NotificationEventTypes.ManagementAuditSummary => RenderAuditSummary(notification),
            _ => throw new InvalidOperationException($"Unsupported notification type '{notification.Type}'.")
        };
    }

    private RenderedNotification RenderSubmitted(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<ReimbursementSubmittedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The reimbursement submission payload is invalid.");
        var amount = data.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("de-DE"));
        var body = $"{data.ApplicantName} submitted **{data.Purpose}** for **{amount} EUR**.";
        var channel = new RenderedMessage("New expense reimbursement", body, data.ManagementUrl, _options.TreasurerRoleId);
        var direct = new RenderedMessage(
            "Reimbursement submitted",
            $"Your reimbursement **{data.Purpose}** for **{amount} EUR** was submitted successfully. We will notify you when its status changes.",
            data.ManagementUrl);
        return new RenderedNotification(channel, direct);
    }

    private static RenderedNotification RenderStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<ReimbursementStatusChangedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The reimbursement status payload is invalid.");
        var amount = data.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("de-DE"));
        var note = string.IsNullOrWhiteSpace(data.AdministrativeNote) ? string.Empty : $"\n\nNote: {data.AdministrativeNote}";
        var direct = new RenderedMessage(
            "Reimbursement status updated",
            $"Your reimbursement **{data.Purpose}** for **{amount} EUR** changed from **{data.PreviousStatus}** to **{data.Status}**.{note}",
            data.ManagementUrl);
        return new RenderedNotification(null, direct);
    }

    private RenderedNotification RenderBoardMeeting(NotificationInboxItem notification, string heading, bool includeButtons = true)
    {
        var data = JsonSerializer.Deserialize<BoardMeetingNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The board meeting payload is invalid.");
        var when = $"<t:{data.ScheduledAtUtc.ToUnixTimeSeconds()}:F> (<t:{data.ScheduledAtUtc.ToUnixTimeSeconds()}:R>)";
        var location = string.IsNullOrWhiteSpace(data.Location) ? "Location to be decided" : data.Location;
        var reason = string.IsNullOrWhiteSpace(data.Reason) ? string.Empty : $"\nReason: {data.Reason}";
        var agenda = RenderAgenda(data.AgendaItems);
        var attendance = RenderAttendance(data.ConfirmedAttendees, data.DeclinedAttendees);
        var buttons = includeButtons ? new[]
        {
            new RenderedButton("I have time", $"board-availability:{data.MeetingId}:{data.ScheduleVersion}:available", 3),
            new RenderedButton("I cannot attend", $"board-availability:{data.MeetingId}:{data.ScheduleVersion}:unavailable", 4),
            new RenderedButton("Propose another time", $"board-reschedule:{data.MeetingId}:{data.ScheduleVersion}", 2)
        } : null;
        var message = new RenderedMessage(heading, $"**{data.Title}**\n{when}\n{data.DurationMinutes} minutes · {location}{reason}{agenda}{attendance}", data.ManagementUrl, _options.ExtendedBoardRoleId, _options.BoardChannelId, buttons);
        return new RenderedNotification(message, null);
    }

    private static string RenderAttendance(
        IReadOnlyList<string>? confirmedAttendees,
        IReadOnlyList<string>? declinedAttendees)
    {
        var confirmed = confirmedAttendees is { Count: > 0 }
            ? string.Join(", ", confirmedAttendees)
            : "None yet";
        var declined = declinedAttendees is { Count: > 0 }
            ? string.Join(", ", declinedAttendees)
            : "None yet";
        return $"\n\n**Attendance**\nConfirmed: {confirmed}\nDeclined: {declined}";
    }

    private static string RenderAgenda(IReadOnlyList<string>? agendaItems)
    {
        if (agendaItems is not { Count: > 0 }) return string.Empty;
        var visibleItems = agendaItems.Take(10).Select((title, index) => $"{index + 1}. {Truncate(title, 160)}");
        var remainder = agendaItems.Count > 10 ? $"\n…and {agendaItems.Count - 10} more" : string.Empty;
        return $"\n\n**Agenda**\n{string.Join('\n', visibleItems)}{remainder}";
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : $"{value[..(maximumLength - 1)]}…";
    }

    private RenderedNotification RenderBoardProposal(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<BoardRescheduleProposalNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The board reschedule proposal payload is invalid.");
        var when = $"<t:{data.ProposedAtUtc.ToUnixTimeSeconds()}:F> (<t:{data.ProposedAtUtc.ToUnixTimeSeconds()}:R>)";
        var reason = string.IsNullOrWhiteSpace(data.Reason) ? string.Empty : $"\nReason: {data.Reason}";
        var isPending = string.Equals(data.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        IReadOnlyList<RenderedButton>? buttons = isPending
            ?
            [
                new RenderedButton("Accept proposal", $"bp:{data.MeetingId}:{data.ProposalId}:a", 3),
                new RenderedButton("Reject proposal", $"bp:{data.MeetingId}:{data.ProposalId}:r", 4)
            ]
            : null;
        var decision = isPending ? string.Empty : $"\n\n**Decision:** {data.Status}.";
        var message = new RenderedMessage("Board meeting reschedule proposal", $"**{data.ProposedByDisplayName}** proposed a new date for **{data.Title}**:\n{when}\n{data.DurationMinutes} minutes{reason}{decision}", data.ManagementUrl, _options.ExtendedBoardRoleId, _options.BoardChannelId, buttons);
        return new RenderedNotification(message, null);
    }

    private RenderedNotification RenderMembershipApplicationCreated(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MembershipApplicationCreatedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The membership application payload is invalid.");
        var email = string.IsNullOrWhiteSpace(data.Email) ? string.Empty : $"\nEmail: {data.Email}";
        var message = new RenderedMessage("New membership application",
            $"**{data.ApplicantName}** submitted a membership application.{email}",
            data.ManagementUrl, _options.BoardRoleId);
        var direct = new RenderedMessage(
            "Membership application received",
            "Your membership application was submitted successfully. We will notify you when it is reviewed.",
            data.ApplicantUrl);
        return new RenderedNotification(message, direct);
    }

    private static RenderedNotification RenderMembershipApplicationStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MembershipApplicationStatusChangedNotification>(
            notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The membership application status payload is invalid.");
        var direct = new RenderedMessage(
            "Membership application updated",
            $"Your membership application was **{data.Status.ToLowerInvariant()}**.",
            data.ApplicantUrl);
        return new RenderedNotification(null, direct);
    }

    private RenderedNotification RenderMemberLinkingRequestCreated(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MemberLinkingRequestCreatedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The member linking request payload is invalid.");
        var email = string.IsNullOrWhiteSpace(data.Email) ? string.Empty : $"\nEmail: {data.Email}";
        var message = new RenderedMessage("New member linking request",
            $"**{data.ApplicantName}** requested a member-account link.\nReason: {data.Reason}{email}",
            data.ManagementUrl, _options.BoardRoleId);
        var direct = new RenderedMessage(
            "Member linking request received",
            "Your request to link your account to a membership record was submitted successfully. We will notify you when it is reviewed.",
            data.ApplicantUrl);
        return new RenderedNotification(message, direct);
    }

    private static RenderedNotification RenderMemberLinkingRequestStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MemberLinkingRequestStatusChangedNotification>(
            notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The member linking request status payload is invalid.");
        var direct = new RenderedMessage(
            "Member linking request updated",
            $"Your member linking request was **{data.Status.ToLowerInvariant()}**.",
            data.ApplicantUrl);
        return new RenderedNotification(null, direct);
    }

    private static RenderedNotification RenderMembershipStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MembershipStatusChangedNotification>(
            notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The membership status payload is invalid.");
        var direct = new RenderedMessage(
            "Membership status updated",
            $"Your membership status changed from **{FormatMembershipStatus(data.PreviousStatus)}** to **{FormatMembershipStatus(data.Status)}**.",
            data.ApplicantUrl);
        return new RenderedNotification(null, direct);
    }

    private static string FormatMembershipStatus(string status)
    {
        return status switch
        {
            "InTrial" => "In trial",
            "HonoraryMember" => "Honorary member",
            "ApplicationRejected" => "Application rejected",
            "SupportingMember" => "Supporting member",
            _ => status
        };
    }

    private RenderedNotification RenderBoardAgendaChange(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<BoardAgendaChangedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The board agenda payload is invalid.");
        var context = string.IsNullOrWhiteSpace(data.MeetingTitle) ? "Board backlog" : data.MeetingTitle;
        var agendaItems = data.AgendaItems ?? [];
        var changedItems = data.ChangedItems
            ?? (data.AgendaItemId.HasValue && !string.IsNullOrWhiteSpace(data.Title)
                ? [new BoardAgendaNotificationItem(data.AgendaItemId.Value, data.Title, 0)]
                : []);
        var changes = data.Changes?.ToDictionary(change => change.AgendaItemId)
            ?? changedItems.ToDictionary(
                item => item.AgendaItemId,
                item => new BoardAgendaNotificationChange(item.AgendaItemId, item.Title, data.Action));
        var agendaLines = agendaItems
            .OrderBy(x => x.Order)
            .Select((item, index) => RenderAgendaChangeLine(item, index + 1,
                changes.GetValueOrDefault(item.AgendaItemId)?.Action))
            .ToList();
        if (agendaLines.Count == 0)
        {
            agendaLines.Add("_No remaining agenda items._");
        }
        var removedItems = changes.Values
            .Where(change => agendaItems.All(item => item.AgendaItemId != change.AgendaItemId))
            .Select(change => $"- ~~{Truncate(change.Title, 150)}~~");
        agendaLines.AddRange(removedItems);
        var body = $"**{context}**\n\n**Updated agenda**\n{string.Join('\n', agendaLines)}";
        var message = new RenderedMessage("Board agenda changed", body, data.ManagementUrl, null, _options.BoardChannelId);
        return new RenderedNotification(message, null);
    }

    private static RenderedNotification RenderAuditSummary(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<AuditSummaryNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The audit summary payload is invalid.");
        var message = new RenderedMessage($"{data.Summary.Source} weekly audit summary",
            AuditSummaryService.Format(data.Summary));
        return new RenderedNotification(message, null);
    }

    private static string RenderAgendaChangeLine(BoardAgendaNotificationItem item, int position, string? action)
    {
        var title = Truncate(item.Title, 150);
        if (string.IsNullOrWhiteSpace(action))
        {
            return $"{position}. {title}";
        }

        return action switch
        {
            "added" or "created" or "added from backlog" => $"+ **{title}**",
            _ => $"~ **{title}**"
        };
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
