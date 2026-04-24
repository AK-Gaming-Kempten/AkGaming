using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Repositories;

public sealed class TournamentRepository(TournamentDbContext dbContext) : ITournamentRepository
{
    public Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => dbContext.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);
}
