namespace AkGaming.Tournaments.Domain.Entities;

public sealed class TeamInviteKey
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int RemainingUses { get; set; } = 1;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedUtc { get; set; }
    public Team? Team { get; set; }
}
