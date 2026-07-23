using System.Text.Json;

namespace AkGaming.Core.Notifications;

public static class NotificationUrlBuilder
{
    public static string? ManagementFrontendBaseUrl(string? configuredFrontendBaseUrl, string? legacyBaseUrl)
    {
        var value = string.IsNullOrWhiteSpace(configuredFrontendBaseUrl)
            ? legacyBaseUrl
            : configuredFrontendBaseUrl;
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().TrimEnd('/');
        if (normalized.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^4].TrimEnd('/');
        return normalized;
    }
}

public static class NotificationEventTypes
{
    public const string ReimbursementSubmitted = "reimbursement.submitted";
    public const string ReimbursementStatusChanged = "reimbursement.status-changed";
    public const string MembershipApplicationCreated = "membership-application.created";
    public const string MembershipApplicationStatusChanged = "membership-application.status-changed";
    public const string MemberLinkingRequestCreated = "member-linking-request.created";
    public const string MemberLinkingRequestStatusChanged = "member-linking-request.status-changed";
    public const string MembershipStatusChanged = "membership.status-changed";
    public const string BoardMeetingCreated = "board-meeting.created";
    public const string BoardMeetingRescheduled = "board-meeting.rescheduled";
    public const string BoardMeetingCancelled = "board-meeting.cancelled";
    public const string BoardMeetingReminder = "board-meeting.reminder";
    public const string BoardMeetingRescheduleProposed = "board-meeting.reschedule-proposed";
    public const string BoardAgendaChanged = "board-meeting.agenda-changed";
    public const string IdentityAuditSummary = "audit-summary.identity";
    public const string ManagementAuditSummary = "audit-summary.management";
}

public sealed record NotificationEnvelope(
    Guid EventId,
    string Type,
    string Source,
    DateTimeOffset OccurredAtUtc,
    Guid? SubjectUserId,
    JsonElement Data);

public sealed record NotificationAcceptedResponse(Guid EventId, bool IsDuplicate);

public sealed record ReimbursementSubmittedNotification(
    Guid ReimbursementId,
    string ApplicantName,
    string Purpose,
    decimal TotalAmount,
    string Status,
    string? ManagementUrl);

public sealed record ReimbursementStatusChangedNotification(
    Guid ReimbursementId,
    string ApplicantName,
    string Purpose,
    decimal TotalAmount,
    string PreviousStatus,
    string Status,
    string? AdministrativeNote,
    string? ManagementUrl);

public sealed record MembershipApplicationCreatedNotification(
    Guid RequestId,
    string ApplicantName,
    string? Email,
    string? ManagementUrl,
    string? ApplicantUrl = null);

public sealed record MembershipApplicationStatusChangedNotification(
    Guid RequestId,
    string Status,
    string? ApplicantUrl);

public sealed record MemberLinkingRequestCreatedNotification(
    Guid RequestId,
    string ApplicantName,
    string? Email,
    string Reason,
    string? ManagementUrl,
    string? ApplicantUrl = null);

public sealed record MemberLinkingRequestStatusChangedNotification(
    Guid RequestId,
    string Status,
    string? ApplicantUrl);

public sealed record MembershipStatusChangedNotification(
    Guid MemberId,
    string PreviousStatus,
    string Status,
    string? ApplicantUrl);

public sealed record DiscordLinkResponse(Guid UserId, string? DiscordUserId, bool IsLinked);
public sealed record DiscordUserLinkResponse(Guid UserId, string DisplayName, bool IsLinked,
    bool CanAccessBoardMeetings, bool CanManageBoardMeetings, bool CanReadIdentityAudit = false,
    bool CanReadManagementAudit = false);

public sealed record AuditSummaryCategory(string Name, int Count);

public sealed record AuditSummarySection(string Name, IReadOnlyList<AuditSummaryCategory> Metrics);

public sealed record AuditSummaryResponse(string Source, DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalEvents, int UniqueActors, int? SuccessfulEvents, int? FailedEvents,
    IReadOnlyList<AuditSummaryCategory> TopCategories,
    IReadOnlyList<AuditSummarySection>? Sections = null);

public sealed record AuditSummaryNotification(AuditSummaryResponse Summary);

public sealed record BoardMeetingNotification(Guid MeetingId, string Title, DateTimeOffset ScheduledAtUtc,
    int DurationMinutes, string? Location, int ScheduleVersion, string? Reason, string? ManagementUrl,
    IReadOnlyList<string>? AgendaItems = null);

public sealed record BoardRescheduleProposalNotification(Guid MeetingId, Guid ProposalId, string Title,
    DateTimeOffset ProposedAtUtc, int DurationMinutes, string? Reason, string ProposedByDisplayName, string? ManagementUrl);

public sealed record BoardAgendaNotificationItem(Guid AgendaItemId, string Title, int Order);

public sealed record BoardAgendaNotificationChange(Guid AgendaItemId, string Title, string Action);

public sealed record BoardAgendaChangedNotification(
    Guid? MeetingId,
    string? MeetingTitle,
    string Action,
    string? ManagementUrl,
    IReadOnlyList<BoardAgendaNotificationItem>? AgendaItems = null,
    IReadOnlyList<BoardAgendaNotificationItem>? ChangedItems = null,
    Guid? AgendaItemId = null,
    string? Title = null,
    IReadOnlyList<BoardAgendaNotificationChange>? Changes = null);
