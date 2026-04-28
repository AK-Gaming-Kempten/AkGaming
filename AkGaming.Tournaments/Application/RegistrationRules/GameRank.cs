namespace AkGaming.Tournaments.Application.RegistrationRules;

public sealed record GameRankBand(
    string Id,
    string Rank,
    string? Division,
    int MinimumRating,
    int MaximumRating,
    bool IsOpenEnded = false)
{
    public string Name => string.IsNullOrWhiteSpace(Division) ? Rank : $"{Rank} {Division}";
}

public sealed record GameRankDescription(
    string Label,
    string Rank,
    string? Division,
    int Points,
    int Rating);
