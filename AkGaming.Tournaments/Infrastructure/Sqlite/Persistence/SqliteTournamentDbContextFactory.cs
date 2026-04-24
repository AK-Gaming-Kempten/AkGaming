using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;

public sealed class SqliteTournamentDbContextFactory : IDesignTimeDbContextFactory<TournamentDbContext>
{
    public TournamentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TournamentDbContext>();
        optionsBuilder.UseSqlite("Data Source=akgaming-tournaments.db");
        return new TournamentDbContext(optionsBuilder.Options);
    }
}
