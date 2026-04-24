namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? LogoAssetId { get; set; }
    public MediaAsset? LogoAsset { get; set; }
    public ICollection<TeamMembership> Memberships { get; set; } = [];
    public ICollection<PlayerProfile> GuestPlayerProfiles { get; set; } = [];
    public ICollection<TournamentRegistration> Registrations { get; set; } = [];
}
