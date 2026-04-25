using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface IGameCatalogService
{
    Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default);
    Task<GameDto> CreateGameAsync(string gameId, string name, Guid? logoAssetId, CancellationToken cancellationToken = default);
    Task<GameDto> UpdateGameLogoAsync(string gameId, Guid? logoAssetId, CancellationToken cancellationToken = default);
    Task DeleteGameAsync(string gameId, CancellationToken cancellationToken = default);
}
