namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? LogoAssetId { get; set; }
    public Guid? BannerAssetId { get; set; }
    public string? PrimaryColor { get; set; }
    public string? ProfileLink { get; set; }
    public Game? Game { get; set; }
    public MediaAsset? LogoAsset { get; set; }
    public MediaAsset? BannerAsset { get; set; }
    public ICollection<TeamMembership> Memberships { get; set; } = [];
    public ICollection<TeamInviteKey> InviteKeys { get; set; } = [];
    public ICollection<PlayerProfile> GuestPlayerProfiles { get; set; } = [];
    public ICollection<TournamentRegistration> Registrations { get; set; } = [];
}
