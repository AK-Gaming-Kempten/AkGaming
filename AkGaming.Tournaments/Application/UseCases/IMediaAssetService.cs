using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Application.Services;

namespace AkGaming.Tournaments.Application.UseCases;

public interface IMediaAssetService
{
    Task<MediaAssetDto> CreateLogoAsync(Stream content, string contentType, string originalFileName, LogoFitMode fitMode, CancellationToken cancellationToken = default);
}
