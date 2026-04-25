using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Tournaments.WebApi.Controllers;

[ApiController]
[Route("api/media-assets")]
[Tags("Media Assets")]
public sealed class MediaAssetsController(
    AkGaming.Tournaments.Application.Persistence.IMediaAssetRepository repository,
    IMediaAssetService service) : ControllerBase
{
    [HttpGet("{mediaAssetId:guid}/file", Name = "GetMediaAssetFile")]
    [EndpointSummary("Get a media asset file.")]
    public async Task<IActionResult> GetMediaAssetFile(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var asset = await repository.GetByIdAsync(mediaAssetId, cancellationToken);
        if (asset is null)
            return NotFound();

        if (asset.Content.Length == 0)
            return NotFound();

        return File(asset.Content, asset.ContentType);
    }

    [HttpPost("logos", Name = "UploadLogoAsset")]
    [EndpointSummary("Upload a cropped logo image.")]
    [ProducesResponseType<MediaAssetDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaAssetDto>> UploadLogoAsset(IFormFile file, [FromForm] string? fitMode, CancellationToken cancellationToken)
    {
        var logoFitMode = ParseFitMode(fitMode);
        await using var stream = file.OpenReadStream();
        var asset = await service.CreateLogoAsync(stream, file.ContentType, file.FileName, logoFitMode, cancellationToken);
        return Ok(asset);
    }

    private static LogoFitMode ParseFitMode(string? fitMode)
        => fitMode?.Trim().ToLowerInvariant() switch
        {
            null or "" or "crop-center" => LogoFitMode.CropCenter,
            "contain-fill" => LogoFitMode.ContainFill,
            _ => throw new ValidationException("Logo fit mode must be crop-center or contain-fill.")
        };
}
