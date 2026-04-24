using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.Abstractions;

public interface IGameCatalogService
{
    Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default);
}
