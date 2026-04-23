using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Tournament
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TournamentStatus Status { get; set; } = TournamentStatus.Draft;
    public Game? Game { get; set; }
    public ICollection<TournamentRegistration> Registrations { get; set; } = [];
}
