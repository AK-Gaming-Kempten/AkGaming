namespace AkGaming.Identity.Domain.Entities;

public sealed class EmailVerificationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public bool IsActive => ConsumedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
