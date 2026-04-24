using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Persistence;

public sealed class PostgresTournamentDbContextFactory : IDesignTimeDbContextFactory<TournamentDbContext>
{
    public TournamentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TournamentDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=akgaming_tournaments;Username=postgres;Password=postgres");
        return new TournamentDbContext(optionsBuilder.Options);
    }
}
