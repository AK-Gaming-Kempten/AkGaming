using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

public sealed class PlayerProfileRepository(TournamentDbContext dbContext) : IPlayerProfileRepository
{
    public Task<PlayerProfile?> GetByIdAsync(Guid playerProfileId, CancellationToken cancellationToken = default)
        => dbContext.PlayerProfiles
            .FirstOrDefaultAsync(playerProfile => playerProfile.Id == playerProfileId, cancellationToken);

    public async Task<IReadOnlyList<PlayerProfile>> GetByIdsAsync(IEnumerable<Guid> playerProfileIds, CancellationToken cancellationToken = default)
    {
        var ids = playerProfileIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await dbContext.PlayerProfiles
            .Where(playerProfile => ids.Contains(playerProfile.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerProfile>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => await dbContext.PlayerProfiles
            .AsNoTracking()
            .Where(playerProfile => playerProfile.UserId == userId)
            .OrderBy(playerProfile => playerProfile.GameId)
            .ThenBy(playerProfile => playerProfile.Name)
            .ToListAsync(cancellationToken);

    public Task<PlayerProfile?> GetByUserAndGameAsync(string userId, string gameId, CancellationToken cancellationToken = default)
        => dbContext.PlayerProfiles
            .FirstOrDefaultAsync(
                playerProfile => playerProfile.UserId == userId
                                 && playerProfile.GameId == gameId,
                cancellationToken);

    public async Task<IReadOnlyList<PlayerProfile>> GetByUsersAndGameAsync(IEnumerable<string> userIds, string gameId, CancellationToken cancellationToken = default)
    {
        var ids = userIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ids.Length == 0)
        {
            return [];
        }

        return await dbContext.PlayerProfiles
            .AsNoTracking()
            .Where(playerProfile => playerProfile.GameId == gameId
                                    && playerProfile.UserId != null
                                    && ids.Contains(playerProfile.UserId))
            .OrderBy(playerProfile => playerProfile.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlayerProfile playerProfile, CancellationToken cancellationToken = default)
        => await dbContext.PlayerProfiles.AddAsync(playerProfile, cancellationToken);
}
