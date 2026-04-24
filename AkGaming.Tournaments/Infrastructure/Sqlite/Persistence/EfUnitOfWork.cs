using AkGaming.Tournaments.Application.Abstractions;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;

public sealed class EfUnitOfWork(TournamentDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
