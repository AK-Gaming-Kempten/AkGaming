using AkGaming.GamelyBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.GamelyBot.Migrations.Sqlite;

public sealed class GamelyBotDbContextSqliteFactory : IDesignTimeDbContextFactory<GamelyBotDbContext>
{
    public GamelyBotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Data Source=gamelybot-design.db";
        var options = new DbContextOptionsBuilder<GamelyBotDbContext>()
            .UseSqlite(connectionString, database => database.MigrationsAssembly(typeof(GamelyBotDbContextSqliteFactory).Assembly.FullName))
            .Options;
        return new GamelyBotDbContext(options);
    }
}
