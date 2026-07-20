namespace AkGaming.GamelyBot.Domain;

public sealed class NotificationInboxItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? SubjectUserId { get; set; }
    public string DataJson { get; set; } = "{}";
    public string Status { get; set; } = NotificationStatuses.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? LastError { get; set; }
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}

public static class NotificationStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Delivered = "delivered";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}
