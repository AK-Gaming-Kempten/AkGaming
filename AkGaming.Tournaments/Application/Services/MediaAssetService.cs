using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Services;

public sealed class MediaAssetService(
    IMediaAssetRepository mediaAssetRepository,
    ILogoFileStorage logoFileStorage,
    IUnitOfWork unitOfWork) : IMediaAssetService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/svg+xml",
        "image/webp"
    };

    public async Task<MediaAssetDto> CreateLogoAsync(Stream content, string contentType, string originalFileName, LogoFitMode fitMode, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationException("Logo uploads must be PNG, JPEG, WebP, or SVG images.");
        }

        var storedFile = await logoFileStorage.SaveAsync(content, contentType, originalFileName, fitMode, cancellationToken);
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            ContentType = storedFile.ContentType,
            OriginalFileName = storedFile.OriginalFileName,
            Content = storedFile.Content,
            SizeBytes = storedFile.SizeBytes
        };

        await mediaAssetRepository.AddAsync(asset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MediaAssetDto(asset.Id, $"/api/media-assets/{asset.Id}/file", asset.ContentType, asset.OriginalFileName, asset.SizeBytes);
    }
}
