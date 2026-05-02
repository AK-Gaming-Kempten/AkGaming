using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Persistence;

public sealed class PostgresTournamentDbContextFactory : IDesignTimeDbContextFactory<TournamentDbContext>
{
    public TournamentDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("Persistence__PostgresConnectionString")
            ?? throw new InvalidOperationException(
                "A Postgres connection string could not be resolved for the tournaments design-time DbContext. " +
                "Set ConnectionStrings__Postgres or Persistence__PostgresConnectionString.");

        var optionsBuilder = new DbContextOptionsBuilder<TournamentDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new TournamentDbContext(optionsBuilder.Options);
    }
}
