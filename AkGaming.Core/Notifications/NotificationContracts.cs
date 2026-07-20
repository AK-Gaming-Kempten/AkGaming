using System.Text.Json;

namespace AkGaming.Core.Notifications;

public static class NotificationEventTypes
{
    public const string ReimbursementSubmitted = "reimbursement.submitted";
    public const string ReimbursementStatusChanged = "reimbursement.status-changed";
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

public sealed record DiscordLinkResponse(Guid UserId, string? DiscordUserId, bool IsLinked);
