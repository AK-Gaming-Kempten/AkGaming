using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

public sealed class TournamentRepository(TournamentDbContext dbContext) : ITournamentRepository
{
    public async Task<IReadOnlyList<Tournament>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tournaments = await dbContext.Tournaments
            .Include(tournament => tournament.Game)
            .Include(tournament => tournament.InfoSections)
            .Include(tournament => tournament.RegistrationRules)
            .Include(tournament => tournament.Registrations)
            .ToListAsync(cancellationToken);

        return tournaments;
    }

    public Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => dbContext.Tournaments
            .Include(tournament => tournament.Game)
            .Include(tournament => tournament.InfoSections)
            .Include(tournament => tournament.RegistrationRules)
            .Include(tournament => tournament.Registrations)
            .FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);

    public Task<Tournament?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => dbContext.Tournaments
            .Include(tournament => tournament.Game)
            .Include(tournament => tournament.InfoSections)
            .Include(tournament => tournament.RegistrationRules)
            .Include(tournament => tournament.Registrations)
            .FirstOrDefaultAsync(tournament => tournament.Slug == slug, cancellationToken);
}
