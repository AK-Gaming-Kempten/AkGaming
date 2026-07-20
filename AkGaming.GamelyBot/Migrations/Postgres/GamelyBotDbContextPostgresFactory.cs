using AkGaming.GamelyBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.GamelyBot.Migrations.Postgres;

public sealed class GamelyBotDbContextPostgresFactory : IDesignTimeDbContextFactory<GamelyBotDbContext>
{
    public GamelyBotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=gamelybot_design;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<GamelyBotDbContext>()
            .UseNpgsql(connectionString, database => database.MigrationsAssembly(typeof(GamelyBotDbContextPostgresFactory).Assembly.FullName))
            .Options;
        return new GamelyBotDbContext(options);
    }
}
