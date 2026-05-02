using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Tournament
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public Guid? LogoAssetId { get; set; }
    public Guid? BannerAssetId { get; set; }
    public string? PrimaryColor { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.Draft;
    public DateTimeOffset? RegistrationOpenUtc { get; set; }
    public DateTimeOffset? RegistrationClosedUtc { get; set; }
    public DateTimeOffset? StartUtc { get; set; }
    public DateTimeOffset? EndUtc { get; set; }
    public Game? Game { get; set; }
    public MediaAsset? LogoAsset { get; set; }
    public MediaAsset? BannerAsset { get; set; }
    public ICollection<TournamentInfoSection> InfoSections { get; set; } = [];
    public ICollection<TournamentRegistrationRule> RegistrationRules { get; set; } = [];
    public ICollection<TournamentRegistration> Registrations { get; set; } = [];
}
