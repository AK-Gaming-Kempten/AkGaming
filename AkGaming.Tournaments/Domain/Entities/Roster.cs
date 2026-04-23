using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Roster
{
    public Guid Id { get; set; }
    public Guid TournamentRegistrationId { get; set; }
    public int Version { get; set; }
    public RosterStatus Status { get; set; } = RosterStatus.Pending;
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }
    public TournamentRegistration? TournamentRegistration { get; set; }
    public ICollection<RosterPlayerSnapshot> PlayerSnapshots { get; set; } = [];
}
