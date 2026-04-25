using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class MediaAssetsControllerTests
{
    private Mock<IMediaAssetRepository> Repository { get; set; } = null!;
    private Mock<IMediaAssetService> Service { get; set; } = null!;
    private MediaAssetsController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Repository = new Mock<IMediaAssetRepository>();
        Service = new Mock<IMediaAssetService>();
        Controller = new MediaAssetsController(Repository.Object, Service.Object);
    }

    [Test]
    [Description("Verifies that media asset files are returned from database content bytes.")]
    public async Task GetMediaAssetFile_ReturnsDatabaseContent()
    {
        // Arrange
        var mediaAssetId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };
        var asset = new MediaAsset
        {
            Id = mediaAssetId,
            ContentType = "image/png",
            OriginalFileName = "logo.png",
            Content = content,
            SizeBytes = content.Length
        };
        Repository
            .Setup(mock => mock.GetByIdAsync(mediaAssetId, CancellationToken.None))
            .ReturnsAsync(asset);

        // Act
        var response = await Controller.GetMediaAssetFile(mediaAssetId, CancellationToken.None);

        // Assert
        var file = response as FileContentResult;
        Assert.Multiple(() =>
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(file!.ContentType, Is.EqualTo("image/png"));
            Assert.That(file.FileContents, Is.EqualTo(content));
        });
        Repository.Verify(mock => mock.GetByIdAsync(mediaAssetId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that missing media asset files return not found.")]
    public async Task GetMediaAssetFile_ReturnsNotFoundForMissingAsset()
    {
        // Arrange
        var mediaAssetId = Guid.NewGuid();
        Repository
            .Setup(mock => mock.GetByIdAsync(mediaAssetId, CancellationToken.None))
            .ReturnsAsync((MediaAsset?)null);

        // Act
        var response = await Controller.GetMediaAssetFile(mediaAssetId, CancellationToken.None);

        // Assert
        Assert.That(response, Is.InstanceOf<NotFoundResult>());
        Repository.Verify(mock => mock.GetByIdAsync(mediaAssetId, CancellationToken.None), Times.Once);
    }
}
