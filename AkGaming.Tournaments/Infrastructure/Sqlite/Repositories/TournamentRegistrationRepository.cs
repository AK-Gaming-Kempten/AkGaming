using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

public sealed class TournamentRegistrationRepository(TournamentDbContext dbContext) : ITournamentRegistrationRepository
{
    public Task<TournamentRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
        => Query()
            .FirstOrDefaultAsync(registration => registration.Id == registrationId, cancellationToken);

    public async Task<IReadOnlyList<TournamentRegistration>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var registrations = await Query()
            .Where(registration => registration.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return registrations
            .OrderByDescending(registration => registration.SubmittedAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<TournamentRegistration>> GetByTournamentIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var registrations = await Query()
            .Where(registration => registration.TournamentId == tournamentId)
            .ToListAsync(cancellationToken);

        return registrations
            .OrderBy(registration => registration.Team?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(registration => registration.SubmittedAtUtc)
            .ToList();
    }

    public Task<TournamentRegistration?> GetByTeamAndTournamentAsync(Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default)
        => Query()
            .FirstOrDefaultAsync(
                registration => registration.TeamId == teamId && registration.TournamentId == tournamentId,
                cancellationToken);

    public async Task AddAsync(TournamentRegistration registration, CancellationToken cancellationToken = default)
        => await dbContext.TournamentRegistrations.AddAsync(registration, cancellationToken);

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
