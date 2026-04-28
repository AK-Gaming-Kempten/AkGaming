using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface IPlayerProfileRepository
{
    Task<PlayerProfile?> GetByIdAsync(Guid playerProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlayerProfile>> GetByIdsAsync(IEnumerable<Guid> playerProfileIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlayerProfile>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<PlayerProfile?> GetByUserAndGameAsync(string userId, string gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlayerProfile>> GetByUsersAndGameAsync(IEnumerable<string> userIds, string gameId, CancellationToken cancellationToken = default);
    Task AddAsync(PlayerProfile playerProfile, CancellationToken cancellationToken = default);
    void Delete(PlayerProfile playerProfile);
}
