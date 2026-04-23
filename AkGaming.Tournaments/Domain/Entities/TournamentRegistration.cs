using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class TournamentRegistration
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public TournamentRegistrationStatus Status { get; set; } = TournamentRegistrationStatus.Pending;
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? ActiveRosterId { get; set; }
    public Tournament? Tournament { get; set; }
    public Team? Team { get; set; }
    public Roster? ActiveRoster { get; set; }
    public ICollection<Roster> Rosters { get; set; } = [];
}
