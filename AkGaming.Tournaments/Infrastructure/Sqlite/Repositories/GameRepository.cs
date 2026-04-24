using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

public sealed class GameRepository(TournamentDbContext dbContext) : IGameRepository
{
    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Games
            .AsNoTracking()
            .OrderBy(game => game.Name)
            .ToListAsync(cancellationToken);

    public Task<Game?> GetByIdAsync(string gameId, CancellationToken cancellationToken = default)
        => dbContext.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(game => game.Id == gameId, cancellationToken);
}
