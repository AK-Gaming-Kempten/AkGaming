using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Repositories;

public sealed class GameRepository(TournamentDbContext dbContext) : IGameRepository
{
    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Games
            .AsNoTracking()
            .OrderBy(game => game.Name)
            .ToListAsync(cancellationToken);

    public Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken = default)
        => dbContext.Games
            .FirstOrDefaultAsync(game => game.Id == gameId, cancellationToken);

    public async Task<bool> IsGameInUseAsync(string gameId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Teams.AnyAsync(team => team.GameId == gameId, cancellationToken)
               || await dbContext.PlayerProfiles.AnyAsync(profile => profile.GameId == gameId, cancellationToken)
               || await dbContext.Tournaments.AnyAsync(tournament => tournament.GameId == gameId, cancellationToken);
    }

    public Task<bool> MediaAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
        => dbContext.MediaAssets.AnyAsync(mediaAsset => mediaAsset.Id == mediaAssetId, cancellationToken);

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
        => await dbContext.Games.AddAsync(game, cancellationToken);

    public void Delete(Game game)
        => dbContext.Games.Remove(game);
}
