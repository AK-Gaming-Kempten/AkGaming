namespace AkGaming.Identity.Domain.Entities;

public sealed class EmailChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid OriginalSecurityVersion { get; set; }
    public string OriginalEmail { get; set; } = string.Empty;
    public string NewEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}
