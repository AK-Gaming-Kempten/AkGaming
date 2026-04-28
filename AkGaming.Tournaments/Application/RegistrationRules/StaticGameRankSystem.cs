namespace AkGaming.Tournaments.Application.RegistrationRules;

public sealed class StaticGameRankSystem(string gameId, IReadOnlyList<GameRankBand> ranks) : IGameRankSystem
{
    public string GameId { get; } = gameId;
    public IReadOnlyList<GameRankBand> Ranks { get; } = ranks;
    public int MinimumRating { get; } = ranks.Min(rank => rank.MinimumRating);
    public int MaximumRating { get; } = ranks.Max(rank => rank.MaximumRating);

    public GameRankDescription DescribeRating(int rating)
    {
        var normalizedRating = Math.Max(rating, MinimumRating);
        var rank = Ranks.FirstOrDefault(candidate =>
                       normalizedRating >= candidate.MinimumRating
                       && (candidate.IsOpenEnded || normalizedRating <= candidate.MaximumRating))
                   ?? Ranks.Last();
        var points = normalizedRating - rank.MinimumRating;

        return new GameRankDescription(
            $"{rank.Name} {points} LP",
            rank.Rank,
            rank.Division,
            points,
            normalizedRating);
    }

    public bool TryDescribeRating(int? rating, out GameRankDescription description)
    {
        if (rating is not int rankRating)
        {
            description = null!;
            return false;
        }

        description = DescribeRating(rankRating);
        return true;
    }
}
