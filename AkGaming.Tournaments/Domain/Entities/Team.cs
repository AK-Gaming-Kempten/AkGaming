namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public Guid? LogoAssetId { get; set; }
    public MediaAsset? LogoAsset { get; set; }
    public ICollection<PlayerProfile> PlayerProfiles { get; set; } = [];
    public ICollection<TournamentRegistration> Registrations { get; set; } = [];
}
