using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

public sealed class TournamentRepository(TournamentDbContext dbContext) : ITournamentRepository
{
    public Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => dbContext.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);
}
