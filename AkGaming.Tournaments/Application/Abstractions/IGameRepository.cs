using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Abstractions;

public interface IGameRepository
{
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken = default);
}
