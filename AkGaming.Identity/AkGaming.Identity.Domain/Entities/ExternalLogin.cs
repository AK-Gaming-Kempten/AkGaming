namespace AkGaming.Identity.Domain.Entities;

public sealed class ExternalLogin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? ProviderUsername { get; set; }
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
}
