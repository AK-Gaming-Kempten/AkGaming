using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default);
}
