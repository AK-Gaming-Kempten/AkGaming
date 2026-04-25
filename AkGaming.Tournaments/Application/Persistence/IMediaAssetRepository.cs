using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);
}
