namespace AkGaming.Tournaments.Application.RegistrationRules;

public interface IGameRankSystem
{
    string GameId { get; }
    int MinimumRating { get; }
    int MaximumRating { get; }
    IReadOnlyList<GameRankBand> Ranks { get; }
    GameRankDescription DescribeRating(int rating);
    bool TryDescribeRating(int? rating, out GameRankDescription description);
}
