using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class PlayerProfile
{
    public Guid Id { get; set; }
    public Guid? TeamId { get; set; }
    public string GameId { get; set; } = string.Empty;
    public Guid? LogoAssetId { get; set; }
    public PlayerProfileType Type { get; set; } = PlayerProfileType.Guest;
    public string Name { get; set; } = string.Empty;
    public int? RankRating { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset LastRevisionUtc { get; set; } = DateTimeOffset.UtcNow;
    public Team? Team { get; set; }
    public Game? Game { get; set; }
    public MediaAsset? LogoAsset { get; set; }
}
