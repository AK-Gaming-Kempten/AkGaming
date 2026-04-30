using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Repositories;

public sealed class TournamentRegistrationRepository(TournamentDbContext dbContext) : ITournamentRegistrationRepository
{
    public Task<TournamentRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
        => Query()
            .FirstOrDefaultAsync(registration => registration.Id == registrationId, cancellationToken);

    public async Task<IReadOnlyList<TournamentRegistration>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        => await Query()
            .Where(registration => registration.TeamId == teamId)
            .OrderByDescending(registration => registration.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TournamentRegistration>> GetByTournamentIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => await Query()
            .Where(registration => registration.TournamentId == tournamentId)
            .OrderBy(registration => registration.Team != null ? registration.Team.Name : string.Empty)
            .ThenBy(registration => registration.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<TournamentRegistration?> GetByTeamAndTournamentAsync(Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default)
        => Query()
            .FirstOrDefaultAsync(
                registration => registration.TeamId == teamId && registration.TournamentId == tournamentId,
                cancellationToken);

    public async Task AddAsync(TournamentRegistration registration, CancellationToken cancellationToken = default)
        => await dbContext.TournamentRegistrations.AddAsync(registration, cancellationToken);

    public async Task AddRosterAsync(Roster roster, CancellationToken cancellationToken = default)
        => await dbContext.Rosters.AddAsync(roster, cancellationToken);

    public void Delete(TournamentRegistration registration)
        => dbContext.TournamentRegistrations.Remove(registration);

    private IQueryable<TournamentRegistration> Query()
        => dbContext.TournamentRegistrations
            .Include(registration => registration.Team)
                .ThenInclude(team => team!.Memberships)
            .Include(registration => registration.Team)
                .ThenInclude(team => team!.GuestPlayerProfiles)
            .Include(registration => registration.Tournament)
            .Include(registration => registration.ActiveRoster)
            .Include(registration => registration.Rosters)
                .ThenInclude(roster => roster.PlayerSnapshots);
}
