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
                    GameId = "lol",
                    Name = "Rift Rumble",
                    Status = TournamentStatus.RegistrationOpen,
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
                    GameId = "valorant",
                    Name = "Campus Clash",
                    Status = TournamentStatus.Draft,
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
                    GameId = "ea-sports-fc",
                    Name = "FC Showdown",
                    Status = TournamentStatus.Draft,
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
