using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Repositories;

public sealed class MediaAssetRepository(TournamentDbContext dbContext) : IMediaAssetRepository
{
    public Task<MediaAsset?> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
        => dbContext.MediaAssets.FirstOrDefaultAsync(mediaAsset => mediaAsset.Id == mediaAssetId, cancellationToken);

    public async Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
        => await dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
}
