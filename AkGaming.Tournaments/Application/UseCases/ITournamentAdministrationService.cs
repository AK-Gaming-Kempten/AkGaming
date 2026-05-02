using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface ITournamentAdministrationService
{
    Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default);
    Task<TournamentDto?> GetTournamentBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<TournamentDto> CreateTournamentAsync(string slug, string gameId, string name, bool isVisible, CancellationToken cancellationToken = default);
    Task<TournamentDto> UpdateTournamentVisibilityAsync(Guid tournamentId, bool isVisible, CancellationToken cancellationToken = default);
    Task DeleteTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
}
