using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface ITournamentCatalogService
{
    Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default);
    Task<TournamentDto?> GetTournamentBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
