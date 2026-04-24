using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.Services;

public sealed class GameCatalogService(IGameRepository gameRepository) : IGameCatalogService
{
    public async Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var games = await gameRepository.GetAllAsync(cancellationToken);
        return games
            .OrderBy(game => game.Name, StringComparer.OrdinalIgnoreCase)
            .Select(game => game.ToDto())
            .ToList();
    }
}
