namespace AkGaming.Tournaments.Frontend.Components.Shared;

public static class PlayerRankFormatter
{
    public static string Format(string gameId, int? rating)
    {
        if (rating is not int rankRating)
            return "No MMR";

        var normalizedRating = Math.Max(rankRating, 0);
        var bands = GetRanks(gameId);
        var rank = bands.FirstOrDefault(candidate =>
                       normalizedRating >= candidate.MinimumRating
                       && (candidate.IsOpenEnded || normalizedRating <= candidate.MaximumRating))
                   ?? bands.Last();
        var points = normalizedRating - rank.MinimumRating;
        return $"{rank.Name} {points} LP";
    }

    public static int GetMinimumRating(string gameId)
        => GetRanks(gameId).Min(rank => rank.MinimumRating);

    public static int GetSliderMaximumRating(string gameId)
        => GetRanks(gameId).Max(rank => rank.MaximumRating);

    private static IReadOnlyList<PlayerRankBand> GetRanks(string gameId)
        => gameId switch
        {
            "lol" => LeagueRanks,
            "valorant" => ValorantRanks,
            "ea-sports-fc" => FcRanks,
            _ => FallbackRanks
        };

    private static readonly IReadOnlyList<PlayerRankBand> LeagueRanks = CreateTieredRanks([
        ("iron", "Iron", ["IV", "III", "II", "I"]),
        ("bronze", "Bronze", ["IV", "III", "II", "I"]),
        ("silver", "Silver", ["IV", "III", "II", "I"]),
        ("gold", "Gold", ["IV", "III", "II", "I"]),
        ("platinum", "Platinum", ["IV", "III", "II", "I"]),
        ("emerald", "Emerald", ["IV", "III", "II", "I"]),
        ("diamond", "Diamond", ["IV", "III", "II", "I"]),
        ("master-plus", "Master+", [null])
    ]);

    private static readonly IReadOnlyList<PlayerRankBand> ValorantRanks = CreateTieredRanks([
        ("iron", "Iron", ["1", "2", "3"]),
        ("bronze", "Bronze", ["1", "2", "3"]),
        ("silver", "Silver", ["1", "2", "3"]),
        ("gold", "Gold", ["1", "2", "3"]),
        ("platinum", "Platinum", ["1", "2", "3"]),
        ("diamond", "Diamond", ["1", "2", "3"]),
        ("ascendant", "Ascendant", ["1", "2", "3"]),
        ("immortal", "Immortal", ["1", "2", "3"]),
        ("radiant", "Radiant", [null])
    ]);

    private static readonly IReadOnlyList<PlayerRankBand> FcRanks = CreateLinearRanks([
        ("division-10", "Division 10"),
        ("division-9", "Division 9"),
        ("division-8", "Division 8"),
        ("division-7", "Division 7"),
        ("division-6", "Division 6"),
        ("division-5", "Division 5"),
        ("division-4", "Division 4"),
        ("division-3", "Division 3"),
        ("division-2", "Division 2"),
        ("division-1", "Division 1"),
        ("elite", "Elite")
    ]);

    private static readonly IReadOnlyList<PlayerRankBand> FallbackRanks =
    [
        new("unranked", "Unranked", null, 0, 99, true)
    ];

    private static IReadOnlyList<PlayerRankBand> CreateLinearRanks(IReadOnlyList<(string Id, string Name)> ranks)
    {
        var bands = ranks.Select((rank, index) => new PlayerRankBand(rank.Id, rank.Name, null, index * 100, index * 100 + 99)).ToList();
        if (bands.Count > 0)
        {
            bands[^1] = bands[^1] with { IsOpenEnded = true };
        }

        return bands;
    }

    private static IReadOnlyList<PlayerRankBand> CreateTieredRanks(IReadOnlyList<(string Id, string Name, IReadOnlyList<string?> Divisions)> ranks)
    {
        var bands = new List<PlayerRankBand>();

        foreach (var rank in ranks)
        {
            foreach (var division in rank.Divisions)
            {
                var minimumRating = bands.Count * 100;
                bands.Add(new PlayerRankBand(
                    division is null ? rank.Id : $"{rank.Id}-{division.ToLowerInvariant()}",
                    rank.Name,
                    division,
                    minimumRating,
                    minimumRating + 99));
            }
        }

        if (bands.Count > 0)
        {
            bands[^1] = bands[^1] with { IsOpenEnded = true };
        }

        return bands;
    }
}

public sealed record PlayerRankBand(string Id, string Rank, string? Division, int MinimumRating, int MaximumRating, bool IsOpenEnded = false)
{
    public string Name => string.IsNullOrWhiteSpace(Division) ? Rank : $"{Rank} {Division}";
}
