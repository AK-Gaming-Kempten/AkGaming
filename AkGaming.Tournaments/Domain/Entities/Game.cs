namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Game
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? LogoAssetId { get; set; }
    public MediaAsset? LogoAsset { get; set; }
}
