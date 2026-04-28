using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface ITournamentRegistrationRepository
{
    Task<TournamentRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TournamentRegistration>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TournamentRegistration>> GetByTournamentIdAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<TournamentRegistration?> GetByTeamAndTournamentAsync(Guid teamId, Guid tournamentId, CancellationToken cancellationToken = default);
    Task AddAsync(TournamentRegistration registration, CancellationToken cancellationToken = default);
}
