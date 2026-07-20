namespace AkGaming.GamelyBot.Domain;

public sealed class NotificationDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationInboxItemId { get; set; }
    public NotificationInboxItem NotificationInboxItem { get; set; } = null!;
    public string Kind { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = NotificationStatuses.Pending;
    public int AttemptCount { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
}

public static class DeliveryKinds
{
    public const string Channel = "channel";
    public const string DirectMessage = "direct-message";
}
