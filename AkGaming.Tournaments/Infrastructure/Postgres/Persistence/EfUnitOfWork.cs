using AkGaming.Tournaments.Application.Abstractions;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Persistence;

public sealed class EfUnitOfWork(TournamentDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
