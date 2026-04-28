namespace AkGaming.Tournaments.Application.RegistrationRules;

public sealed class GameRankSystemRegistry : IGameRankSystemRegistry
{
    private readonly IReadOnlyDictionary<string, IGameRankSystem> rankSystems;
    private readonly IGameRankSystem fallbackRankSystem;

    public GameRankSystemRegistry()
    {
        var systems = new IGameRankSystem[]
        {
            CreateLeagueRankSystem("lol"),
            CreateValorantRankSystem("valorant"),
            CreateLinearRankSystem("ea-sports-fc", [
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
            ])
        };

        rankSystems = systems.ToDictionary(system => system.GameId, StringComparer.OrdinalIgnoreCase);
        fallbackRankSystem = CreateLinearRankSystem("default", [("unranked", "Unranked")]);
    }

    public IGameRankSystem GetRankSystem(string gameId)
        => rankSystems.TryGetValue(gameId, out var rankSystem)
            ? rankSystem
            : fallbackRankSystem;

    private static IGameRankSystem CreateLeagueRankSystem(string gameId)
        => CreateTieredRankSystem(gameId, [
            ("iron", "Iron", ["IV", "III", "II", "I"]),
            ("bronze", "Bronze", ["IV", "III", "II", "I"]),
            ("silver", "Silver", ["IV", "III", "II", "I"]),
            ("gold", "Gold", ["IV", "III", "II", "I"]),
            ("platinum", "Platinum", ["IV", "III", "II", "I"]),
            ("emerald", "Emerald", ["IV", "III", "II", "I"]),
            ("diamond", "Diamond", ["IV", "III", "II", "I"]),
            ("master-plus", "Master+", [null])
        ]);

    private static IGameRankSystem CreateValorantRankSystem(string gameId)
        => CreateTieredRankSystem(gameId, [
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

    private static IGameRankSystem CreateLinearRankSystem(string gameId, IReadOnlyList<(string Id, string Name)> ranks)
    {
        var bands = ranks
            .Select((rank, index) => new GameRankBand(rank.Id, rank.Name, null, index * 100, index * 100 + 99))
            .ToList();

        if (bands.Count > 0)
        {
            bands[^1] = bands[^1] with { IsOpenEnded = true };
        }

        return new StaticGameRankSystem(gameId, bands);
    }

    private static IGameRankSystem CreateTieredRankSystem(
        string gameId,
        IReadOnlyList<(string Id, string Name, IReadOnlyList<string?> Divisions)> ranks)
    {
        var bands = new List<GameRankBand>();

        foreach (var rank in ranks)
        {
            foreach (var division in rank.Divisions)
            {
                var minimumRating = bands.Count * 100;
                bands.Add(new GameRankBand(
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

        return new StaticGameRankSystem(gameId, bands);
    }
}
