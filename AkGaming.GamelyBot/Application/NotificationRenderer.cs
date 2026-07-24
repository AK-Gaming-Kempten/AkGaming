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

public sealed class NotificationRenderer(
    IOptions<NotificationRoutingOptions> options,
    BotText text) : INotificationRenderer
{
    private readonly NotificationRoutingOptions _options = options.Value;

    public NotificationRenderer(IOptions<NotificationRoutingOptions> options)
        : this(options, BotText.English)
    {
    }

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
            NotificationEventTypes.BoardMeetingCreated => RenderBoardMeeting(notification, text["BoardMeetingNew"]),
            NotificationEventTypes.BoardMeetingRescheduled => RenderBoardMeeting(notification, text["BoardMeetingRescheduled"]),
            NotificationEventTypes.BoardMeetingCancelled => RenderBoardMeeting(notification, text["BoardMeetingCancelled"], false),
            NotificationEventTypes.BoardMeetingReminder => RenderBoardMeeting(notification, text["BoardMeetingReminder"]),
            NotificationEventTypes.BoardMeetingAvailabilityChanged => RenderBoardMeeting(notification, text["BoardMeeting"]),
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
        var amount = data.TotalAmount.ToString("N2", text.Culture);
        var body = text.Format("ReimbursementSubmittedChannelBody", data.ApplicantName, data.Purpose, amount);
        var channel = new RenderedMessage(text["ReimbursementNewTitle"], body, data.ManagementUrl, _options.TreasurerRoleId);
        var direct = new RenderedMessage(
            text["ReimbursementSubmittedTitle"],
            text.Format("ReimbursementSubmittedDirectBody", data.Purpose, amount),
            data.ManagementUrl);
        return new RenderedNotification(channel, direct);
    }

    private RenderedNotification RenderStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<ReimbursementStatusChangedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The reimbursement status payload is invalid.");
        var amount = data.TotalAmount.ToString("N2", text.Culture);
        var note = string.IsNullOrWhiteSpace(data.AdministrativeNote)
            ? string.Empty
            : text.Format("AdministrativeNote", data.AdministrativeNote);
        var direct = new RenderedMessage(
            text["ReimbursementStatusUpdatedTitle"],
            text.Format("ReimbursementStatusUpdatedBody", data.Purpose, amount,
                FormatStatus(data.PreviousStatus), FormatStatus(data.Status), note),
            data.ManagementUrl);
        return new RenderedNotification(null, direct);
    }

    private RenderedNotification RenderBoardMeeting(NotificationInboxItem notification, string heading, bool includeButtons = true)
    {
        var data = JsonSerializer.Deserialize<BoardMeetingNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The board meeting payload is invalid.");
        var when = $"<t:{data.ScheduledAtUtc.ToUnixTimeSeconds()}:F> (<t:{data.ScheduledAtUtc.ToUnixTimeSeconds()}:R>)";
        var location = string.IsNullOrWhiteSpace(data.Location) ? text["LocationToBeDecided"] : data.Location;
        var reason = string.IsNullOrWhiteSpace(data.Reason) ? string.Empty : text.Format("ReasonLine", data.Reason);
        var agenda = RenderAgenda(data.AgendaItems);
        var attendance = RenderAttendance(data.ConfirmedAttendees, data.DeclinedAttendees);
        var buttons = includeButtons ? new[]
        {
            new RenderedButton(text["AvailabilityAvailable"], $"board-availability:{data.MeetingId}:{data.ScheduleVersion}:available", 3),
            new RenderedButton(text["AvailabilityUnavailable"], $"board-availability:{data.MeetingId}:{data.ScheduleVersion}:unavailable", 4),
            new RenderedButton(text["ProposeAnotherTime"], $"board-reschedule:{data.MeetingId}:{data.ScheduleVersion}", 2)
        } : null;
        var message = new RenderedMessage(heading,
            text.Format("BoardMeetingBody", data.Title, when, data.DurationMinutes, location, reason, agenda, attendance),
            data.ManagementUrl, _options.ExtendedBoardRoleId, _options.BoardChannelId, buttons);
        return new RenderedNotification(message, null);
    }

    private string RenderAttendance(
        IReadOnlyList<string>? confirmedAttendees,
        IReadOnlyList<string>? declinedAttendees)
    {
        var confirmed = confirmedAttendees is { Count: > 0 }
            ? string.Join(", ", confirmedAttendees)
            : text["NoneYet"];
        var declined = declinedAttendees is { Count: > 0 }
            ? string.Join(", ", declinedAttendees)
            : text["NoneYet"];
        return text.Format("AttendanceBlock", confirmed, declined);
    }

    private string RenderAgenda(IReadOnlyList<string>? agendaItems)
    {
        if (agendaItems is not { Count: > 0 }) return string.Empty;
        var visibleItems = agendaItems.Take(10).Select((title, index) => $"{index + 1}. {Truncate(title, 160)}");
        var remainder = agendaItems.Count > 10 ? text.Format("AndMore", agendaItems.Count - 10) : string.Empty;
        return text.Format("AgendaBlock", string.Join('\n', visibleItems), remainder);
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
        var reason = string.IsNullOrWhiteSpace(data.Reason) ? string.Empty : text.Format("ReasonLine", data.Reason);
        var isPending = string.Equals(data.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        IReadOnlyList<RenderedButton>? buttons = isPending
            ?
            [
                new RenderedButton(text["AcceptProposal"], $"bp:{data.MeetingId}:{data.ProposalId}:a", 3),
                new RenderedButton(text["RejectProposal"], $"bp:{data.MeetingId}:{data.ProposalId}:r", 4)
            ]
            : null;
        var decision = isPending ? string.Empty : text.Format("DecisionBlock", FormatStatus(data.Status));
        var message = new RenderedMessage(text["BoardRescheduleProposalTitle"],
            text.Format("BoardRescheduleProposalBody", data.ProposedByDisplayName, data.Title, when,
                data.DurationMinutes, reason, decision),
            data.ManagementUrl, _options.ExtendedBoardRoleId, _options.BoardChannelId, buttons);
        return new RenderedNotification(message, null);
    }

    private RenderedNotification RenderMembershipApplicationCreated(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MembershipApplicationCreatedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The membership application payload is invalid.");
        var email = string.IsNullOrWhiteSpace(data.Email) ? string.Empty : text.Format("EmailLine", data.Email);
        var message = new RenderedMessage(text["MembershipApplicationNewTitle"],
            text.Format("MembershipApplicationNewBody", data.ApplicantName, email),
            data.ManagementUrl, _options.BoardRoleId);
        var direct = new RenderedMessage(
            text["MembershipApplicationReceivedTitle"],
            text["MembershipApplicationReceivedBody"],
            data.ApplicantUrl);
        return new RenderedNotification(message, direct);
    }

    private RenderedNotification RenderMembershipApplicationStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MembershipApplicationStatusChangedNotification>(
            notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The membership application status payload is invalid.");
        var direct = new RenderedMessage(
            text["MembershipApplicationUpdatedTitle"],
            text.Format("MembershipApplicationUpdatedBody", FormatStatus(data.Status)),
            data.ApplicantUrl);
        return new RenderedNotification(null, direct);
    }

    private RenderedNotification RenderMemberLinkingRequestCreated(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MemberLinkingRequestCreatedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The member linking request payload is invalid.");
        var email = string.IsNullOrWhiteSpace(data.Email) ? string.Empty : text.Format("EmailLine", data.Email);
        var message = new RenderedMessage(text["MemberLinkingNewTitle"],
            text.Format("MemberLinkingNewBody", data.ApplicantName, data.Reason, email),
            data.ManagementUrl, _options.BoardRoleId);
        var direct = new RenderedMessage(
            text["MemberLinkingReceivedTitle"],
            text["MemberLinkingReceivedBody"],
            data.ApplicantUrl);
        return new RenderedNotification(message, direct);
    }

    private RenderedNotification RenderMemberLinkingRequestStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MemberLinkingRequestStatusChangedNotification>(
            notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The member linking request status payload is invalid.");
        var direct = new RenderedMessage(
            text["MemberLinkingUpdatedTitle"],
            text.Format("MemberLinkingUpdatedBody", FormatStatus(data.Status)),
            data.ApplicantUrl);
        return new RenderedNotification(null, direct);
    }

    private RenderedNotification RenderMembershipStatusChanged(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<MembershipStatusChangedNotification>(
            notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The membership status payload is invalid.");
        var direct = new RenderedMessage(
            text["MembershipStatusUpdatedTitle"],
            text.Format("MembershipStatusUpdatedBody", FormatMembershipStatus(data.PreviousStatus), FormatMembershipStatus(data.Status)),
            data.ApplicantUrl);
        return new RenderedNotification(null, direct);
    }

    private string FormatMembershipStatus(string status) => FormatStatus(status);

    private string FormatStatus(string status)
    {
        return status switch
        {
            "Accepted" => text["StatusAccepted"],
            "Rejected" => text["StatusRejected"],
            "Approved" => text["StatusApproved"],
            "Cancelled" => text["StatusCancelled"],
            "Submitted" => text["StatusSubmitted"],
            "UnderReview" => text["StatusUnderReview"],
            "Paid" => text["StatusPaid"],
            "Pending" => text["StatusPending"],
            "None" => text["StatusNone"],
            "Expelled" => text["StatusExpelled"],
            "Suspended" => text["StatusSuspended"],
            "Withdrawn" => text["StatusWithdrawn"],
            "Applicant" => text["StatusApplicant"],
            "InTrial" => text["StatusInTrial"],
            "Member" => text["StatusMember"],
            "HonoraryMember" => text["StatusHonoraryMember"],
            "ApplicationRejected" => text["StatusApplicationRejected"],
            "SupportingMember" => text["StatusSupportingMember"],
            _ => status
        };
    }

    private RenderedNotification RenderBoardAgendaChange(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<BoardAgendaChangedNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The board agenda payload is invalid.");
        var context = string.IsNullOrWhiteSpace(data.MeetingTitle) ? text["BoardBacklog"] : data.MeetingTitle;
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
            agendaLines.Add(text["NoRemainingAgendaItems"]);
        }
        var removedItems = changes.Values
            .Where(change => agendaItems.All(item => item.AgendaItemId != change.AgendaItemId))
            .Select(change => $"- ~~{Truncate(change.Title, 150)}~~");
        agendaLines.AddRange(removedItems);
        var body = text.Format("UpdatedAgendaBody", context, string.Join('\n', agendaLines));
        var message = new RenderedMessage(text["BoardAgendaChangedTitle"], body, data.ManagementUrl, null, _options.BoardChannelId);
        return new RenderedNotification(message, null);
    }

    private RenderedNotification RenderAuditSummary(NotificationInboxItem notification)
    {
        var data = JsonSerializer.Deserialize<AuditSummaryNotification>(notification.DataJson, JsonOptions())
            ?? throw new InvalidOperationException("The audit summary payload is invalid.");
        var message = new RenderedMessage(text.Format("WeeklyAuditSummaryTitle", data.Summary.Source),
            AuditSummaryService.Format(data.Summary, text));
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
