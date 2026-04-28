using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;

public static class SqliteDatabaseInitializer
{
    public static async Task InitializeTournamentSqliteDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TournamentDbContext>>();

        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.EnsureMediaAssetContentColumnAsync();
        await dbContext.EnsurePlayerProfileRankColumnAsync();
        await dbContext.EnsureTournamentContentSchemaAsync();
        await dbContext.EnsureTournamentRegistrationRulesTableAsync();

        if (!await dbContext.Games.AnyAsync())
        {
            dbContext.Games.AddRange(
                new Game { Id = "lol", Name = "League of Legends" },
                new Game { Id = "valorant", Name = "VALORANT" },
                new Game { Id = "cs2", Name = "Counter-Strike 2" },
                new Game { Id = "tft", Name = "Teamfight Tactics" },
                new Game { Id = "wild-rift", Name = "League of Legends: Wild Rift" },
                new Game { Id = "overwatch-2", Name = "Overwatch 2" },
                new Game { Id = "rainbow-six-siege", Name = "Rainbow Six Siege" },
                new Game { Id = "2xko", Name = "2XKO" },
                new Game { Id = "legends-of-runeterra", Name = "Legends of Runeterra" },
                new Game { Id = "ea-sports-fc", Name = "EA Sports FC" });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded tournament SQLite database with supported games.");
        }

        if (!await dbContext.Tournaments.AnyAsync())
        {
            dbContext.Tournaments.AddRange(
                new Tournament
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Slug = "rift-rumble",
                    GameId = "lol",
                    Name = "Rift Rumble",
                    Status = TournamentStatus.RegistrationOpen,
                    RegistrationOpenUtc = new DateTimeOffset(2026, 4, 1, 16, 0, 0, TimeSpan.Zero),
                    RegistrationClosedUtc = new DateTimeOffset(2026, 5, 10, 21, 0, 0, TimeSpan.Zero),
                    StartUtc = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero),
                    EndUtc = new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.Zero),
                    InfoSections =
                    [
                        new TournamentInfoSection
                        {
                            Id = Guid.NewGuid(),
                            Header = "Format",
                            ContentMarkdown = "5v5 team tournament with a group stage followed by a single-elimination playoff bracket.",
                            SortOrder = 0
                        },
                        new TournamentInfoSection
                        {
                            Id = Guid.NewGuid(),
                            Header = "Roster rules",
                            ContentMarkdown = "- Up to 7 players per team\n- At least 5 players must be registered\n- Eligibility is checked against the configured rank rules",
                            SortOrder = 1
                        }
                    ],
                    RegistrationRules =
                    [
                        new MinPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 0, Value = 5 },
                        new MaxPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 1, Value = 7 },
                        new MaxPlayerRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = 2, Value = 2799 },
                        new MaxTeamAverageRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = 3, Value = 2399 }
                    ]
                },
                new Tournament
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Slug = "campus-clash",
                    GameId = "valorant",
                    Name = "Campus Clash",
                    Status = TournamentStatus.Draft,
                    RegistrationOpenUtc = new DateTimeOffset(2026, 5, 3, 16, 0, 0, TimeSpan.Zero),
                    RegistrationClosedUtc = new DateTimeOffset(2026, 6, 1, 21, 0, 0, TimeSpan.Zero),
                    StartUtc = new DateTimeOffset(2026, 6, 7, 13, 0, 0, TimeSpan.Zero),
                    EndUtc = new DateTimeOffset(2026, 6, 14, 20, 0, 0, TimeSpan.Zero),
                    InfoSections =
                    [
                        new TournamentInfoSection
                        {
                            Id = Guid.NewGuid(),
                            Header = "Overview",
                            ContentMarkdown = "Campus Clash brings together student rosters for a weekend VALORANT event.",
                            SortOrder = 0
                        }
                    ],
                    RegistrationRules =
                    [
                        new MinPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 0, Value = 5 },
                        new MaxPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 1, Value = 7 },
                        new MaxPlayerRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = 2, Value = 2099 },
                        new MaxTeamAverageRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = 3, Value = 1799 }
                    ]
                },
                new Tournament
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Slug = "fc-showdown",
                    GameId = "ea-sports-fc",
                    Name = "FC Showdown",
                    Status = TournamentStatus.Draft,
                    RegistrationOpenUtc = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero),
                    RegistrationClosedUtc = new DateTimeOffset(2026, 7, 5, 21, 0, 0, TimeSpan.Zero),
                    StartUtc = new DateTimeOffset(2026, 7, 12, 14, 0, 0, TimeSpan.Zero),
                    EndUtc = new DateTimeOffset(2026, 7, 12, 20, 0, 0, TimeSpan.Zero),
                    InfoSections =
                    [
                        new TournamentInfoSection
                        {
                            Id = Guid.NewGuid(),
                            Header = "Format",
                            ContentMarkdown = "1v1 open bracket with a same-day final.",
                            SortOrder = 0
                        }
                    ],
                    RegistrationRules =
                    [
                        new MinPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 0, Value = 1 },
                        new MaxPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 1, Value = 1 },
                        new MaxPlayerRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = 2, Value = 999 },
                        new MaxTeamAverageRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = 3, Value = 999 }
                    ]
                });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded tournament SQLite database with public tournaments.");
        }

        await dbContext.BackfillTournamentContentAsync(logger);
    }

    private static async Task EnsureMediaAssetContentColumnAsync(this TournamentDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var hasContentColumn = false;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(media_assets);";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), "Content", StringComparison.OrdinalIgnoreCase))
                {
                    hasContentColumn = true;
                    break;
                }
            }
        }

        if (hasContentColumn)
            return;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "ALTER TABLE media_assets ADD COLUMN Content BLOB NOT NULL DEFAULT X'';";
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task EnsurePlayerProfileRankColumnAsync(this TournamentDbContext dbContext)
    {
        if (!await dbContext.HasColumnAsync("player_profiles", "RankRating"))
        {
            await dbContext.ExecuteSqlAsync("ALTER TABLE player_profiles ADD COLUMN RankRating INTEGER NULL;");
        }
    }

    private static async Task EnsureTournamentRegistrationRulesTableAsync(this TournamentDbContext dbContext)
    {
        await dbContext.ExecuteSqlAsync("""
            CREATE TABLE IF NOT EXISTS tournament_registration_rules (
                Id TEXT NOT NULL CONSTRAINT PK_tournament_registration_rules PRIMARY KEY,
                TournamentId TEXT NOT NULL,
                SortOrder INTEGER NOT NULL,
                Value INTEGER NOT NULL,
                RuleType TEXT NOT NULL,
                CONSTRAINT FK_tournament_registration_rules_tournaments_TournamentId
                    FOREIGN KEY (TournamentId) REFERENCES tournaments (Id) ON DELETE CASCADE
            );
            """);

        var tournamentIdsWithoutRules = await dbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => !dbContext.TournamentRegistrationRules.Any(rule => rule.TournamentId == tournament.Id))
            .Select(tournament => tournament.Id)
            .ToListAsync();

        foreach (var tournamentId in tournamentIdsWithoutRules)
        {
            await dbContext.ExecuteSqlAsync($"""
                INSERT INTO tournament_registration_rules (Id, TournamentId, SortOrder, Value, RuleType)
                VALUES ('{Guid.NewGuid()}', '{tournamentId}', 0, 1, 'MinPlayersPerTeam');
                """);
            await dbContext.ExecuteSqlAsync($"""
                INSERT INTO tournament_registration_rules (Id, TournamentId, SortOrder, Value, RuleType)
                VALUES ('{Guid.NewGuid()}', '{tournamentId}', 1, 99, 'MaxPlayersPerTeam');
                """);
        }
    }

    private static async Task EnsureTournamentContentSchemaAsync(this TournamentDbContext dbContext)
    {
        if (!await dbContext.HasColumnAsync("tournaments", "Slug"))
        {
            await dbContext.ExecuteSqlAsync("ALTER TABLE tournaments ADD COLUMN Slug TEXT NOT NULL DEFAULT '';");
        }

        if (!await dbContext.HasColumnAsync("tournaments", "RegistrationOpenUtc"))
        {
            await dbContext.ExecuteSqlAsync("ALTER TABLE tournaments ADD COLUMN RegistrationOpenUtc TEXT NULL;");
        }

        if (!await dbContext.HasColumnAsync("tournaments", "RegistrationClosedUtc"))
        {
            await dbContext.ExecuteSqlAsync("ALTER TABLE tournaments ADD COLUMN RegistrationClosedUtc TEXT NULL;");
        }

        if (!await dbContext.HasColumnAsync("tournaments", "StartUtc"))
        {
            await dbContext.ExecuteSqlAsync("ALTER TABLE tournaments ADD COLUMN StartUtc TEXT NULL;");
        }

        if (!await dbContext.HasColumnAsync("tournaments", "EndUtc"))
        {
            await dbContext.ExecuteSqlAsync("ALTER TABLE tournaments ADD COLUMN EndUtc TEXT NULL;");
        }

        await dbContext.ExecuteSqlAsync("""
            CREATE TABLE IF NOT EXISTS tournament_info_sections (
                Id TEXT NOT NULL CONSTRAINT PK_tournament_info_sections PRIMARY KEY,
                TournamentId TEXT NOT NULL,
                Header TEXT NOT NULL,
                ContentMarkdown TEXT NOT NULL,
                SortOrder INTEGER NOT NULL,
                CONSTRAINT FK_tournament_info_sections_tournaments_TournamentId
                    FOREIGN KEY (TournamentId) REFERENCES tournaments (Id) ON DELETE CASCADE
            );
            """);

        await dbContext.ExecuteSqlAsync("""
            CREATE INDEX IF NOT EXISTS IX_tournament_info_sections_TournamentId_SortOrder
            ON tournament_info_sections (TournamentId, SortOrder);
            """);
    }

    private static async Task BackfillTournamentContentAsync(this TournamentDbContext dbContext, ILogger logger)
    {
        var tournaments = await dbContext.Tournaments
            .Include(tournament => tournament.InfoSections)
            .ToListAsync();

        foreach (var tournament in tournaments)
        {
            var wasChanged = false;

            if (string.IsNullOrWhiteSpace(tournament.Slug))
            {
                tournament.Slug = tournament.Id switch
                {
                    var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") => "rift-rumble",
                    var id when id == Guid.Parse("22222222-2222-2222-2222-222222222222") => "campus-clash",
                    var id when id == Guid.Parse("33333333-3333-3333-3333-333333333333") => "fc-showdown",
                    _ => tournament.Name.Trim().ToLowerInvariant().Replace(' ', '-')
                };
                wasChanged = true;
            }

            if (!tournament.RegistrationOpenUtc.HasValue)
            {
                tournament.RegistrationOpenUtc = tournament.Id switch
                {
                    var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") => new DateTimeOffset(2026, 4, 1, 16, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("22222222-2222-2222-2222-222222222222") => new DateTimeOffset(2026, 5, 3, 16, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("33333333-3333-3333-3333-333333333333") => new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero),
                    _ => null
                };
                wasChanged = true;
            }

            if (!tournament.RegistrationClosedUtc.HasValue)
            {
                tournament.RegistrationClosedUtc = tournament.Id switch
                {
                    var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") => new DateTimeOffset(2026, 5, 10, 21, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("22222222-2222-2222-2222-222222222222") => new DateTimeOffset(2026, 6, 1, 21, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("33333333-3333-3333-3333-333333333333") => new DateTimeOffset(2026, 7, 5, 21, 0, 0, TimeSpan.Zero),
                    _ => null
                };
                wasChanged = true;
            }

            if (!tournament.StartUtc.HasValue)
            {
                tournament.StartUtc = tournament.Id switch
                {
                    var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") => new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("22222222-2222-2222-2222-222222222222") => new DateTimeOffset(2026, 6, 7, 13, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("33333333-3333-3333-3333-333333333333") => new DateTimeOffset(2026, 7, 12, 14, 0, 0, TimeSpan.Zero),
                    _ => null
                };
                wasChanged = true;
            }

            if (!tournament.EndUtc.HasValue)
            {
                tournament.EndUtc = tournament.Id switch
                {
                    var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") => new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("22222222-2222-2222-2222-222222222222") => new DateTimeOffset(2026, 6, 14, 20, 0, 0, TimeSpan.Zero),
                    var id when id == Guid.Parse("33333333-3333-3333-3333-333333333333") => new DateTimeOffset(2026, 7, 12, 20, 0, 0, TimeSpan.Zero),
                    _ => null
                };
                wasChanged = true;
            }

            if (tournament.InfoSections.Count == 0)
            {
                foreach (var template in GetDefaultTournamentInfoSections(tournament.Id))
                {
                    tournament.InfoSections.Add(template);
                }

                wasChanged = true;
            }

            if (wasChanged)
            {
                logger.LogDebug("Backfilled tournament content for {TournamentId}.", tournament.Id);
            }
        }

        await dbContext.SaveChangesAsync();
        await dbContext.ExecuteSqlAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_tournaments_Slug
            ON tournaments (Slug);
            """);
    }

    private static IReadOnlyList<TournamentInfoSection> GetDefaultTournamentInfoSections(Guid tournamentId)
        => tournamentId switch
        {
            var id when id == Guid.Parse("11111111-1111-1111-1111-111111111111") =>
            [
                new TournamentInfoSection
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournamentId,
                    Header = "Format",
                    ContentMarkdown = "5v5 team tournament with a group stage followed by a single-elimination playoff bracket.",
                    SortOrder = 0
                },
                new TournamentInfoSection
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournamentId,
                    Header = "Roster rules",
                    ContentMarkdown = "- Up to 7 players per team\n- At least 5 players must be registered\n- Eligibility is checked against the configured rank rules",
                    SortOrder = 1
                }
            ],
            var id when id == Guid.Parse("22222222-2222-2222-2222-222222222222") =>
            [
                new TournamentInfoSection
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournamentId,
                    Header = "Overview",
                    ContentMarkdown = "Campus Clash brings together student rosters for a weekend VALORANT event.",
                    SortOrder = 0
                }
            ],
            var id when id == Guid.Parse("33333333-3333-3333-3333-333333333333") =>
            [
                new TournamentInfoSection
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournamentId,
                    Header = "Format",
                    ContentMarkdown = "1v1 open bracket with a same-day final.",
                    SortOrder = 0
                }
            ],
            _ => []
        };

    private static async Task<bool> HasColumnAsync(this TournamentDbContext dbContext, string tableName, string columnName)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task ExecuteSqlAsync(this TournamentDbContext dbContext, string commandText)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }
}
