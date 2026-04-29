using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface ITournamentRepository
{
    Task<IReadOnlyList<Tournament>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<Tournament?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task ReplaceInfoSectionsAsync(Guid tournamentId, IReadOnlyList<TournamentInfoSection> sections, CancellationToken cancellationToken = default);
    Task ReplaceRegistrationRulesAsync(Guid tournamentId, IReadOnlyList<TournamentRegistrationRule> rules, CancellationToken cancellationToken = default);
}
