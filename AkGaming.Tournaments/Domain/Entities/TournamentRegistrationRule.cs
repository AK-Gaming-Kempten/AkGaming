namespace AkGaming.Tournaments.Domain.Entities;

public abstract class TournamentRegistrationRule
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public int SortOrder { get; set; }
    public int Value { get; set; }
    public Tournament? Tournament { get; set; }
}

public sealed class MinPlayersPerTeamRegistrationRule : TournamentRegistrationRule;

public sealed class MaxPlayersPerTeamRegistrationRule : TournamentRegistrationRule;

public sealed class MaxPlayerRankRatingRegistrationRule : TournamentRegistrationRule;

public sealed class MaxTeamAverageRankRatingRegistrationRule : TournamentRegistrationRule;
