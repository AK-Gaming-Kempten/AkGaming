using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface IGameRepository
{
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken = default);
    Task<bool> IsGameInUseAsync(string gameId, CancellationToken cancellationToken = default);
    Task<bool> MediaAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task AddAsync(Game game, CancellationToken cancellationToken = default);
    void Delete(Game game);
}
