using AkGaming.Tournaments.Domain.Entities;
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

        if (await dbContext.Games.AnyAsync())
            return;

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
}
