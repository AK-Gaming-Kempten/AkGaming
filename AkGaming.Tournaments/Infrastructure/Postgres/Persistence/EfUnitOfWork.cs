using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Persistence;

public sealed class EfUnitOfWork(TournamentDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The requested data changed while this operation was running. Reload and try again.");
        }
    }
}
