using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public string Round { get; set; } = string.Empty;
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public Tournament? Tournament { get; set; }
}
