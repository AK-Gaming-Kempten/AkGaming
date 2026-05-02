using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TournamentCatalogService(ITournamentRepository tournamentRepository) : ITournamentCatalogService
{
    public async Task<IReadOnlyList<TournamentSummaryDto>> GetTournamentsAsync(CancellationToken cancellationToken = default)
    {
        var tournaments = await tournamentRepository.GetAllAsync(includeHidden: false, cancellationToken);
        return tournaments
            .OrderBy(tournament => tournament.StartUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(tournament => tournament.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tournament => tournament.ToSummaryDto())
            .ToList();
    }

    public async Task<TournamentDto?> GetTournamentBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var tournament = await tournamentRepository.GetBySlugAsync(slug.Trim(), includeHidden: false, cancellationToken);
        return tournament?.ToDto();
    }
}
