using AkGaming.Tournaments.Domain.Entities;
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
}
