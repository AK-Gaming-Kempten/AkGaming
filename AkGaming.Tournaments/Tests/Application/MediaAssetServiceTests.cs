using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class MediaAssetServiceTests
{
    private InMemoryStore Store { get; set; } = null!;
    private FakeUnitOfWork UnitOfWork { get; set; } = null!;
    private MediaAssetService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new MediaAssetService(new InMemoryMediaAssetRepository(Store), new FakeLogoFileStorage(), UnitOfWork);
    }

    [Test]
    [Description("Verifies that logo uploads create media asset records for supported image types.")]
    public void CreateLogoAsync_CreatesMediaAsset()
    {
        // Arrange
        using var content = new MemoryStream([1, 2, 3]);

        // Act
        var asset = Service.CreateLogoAsync(content, "image/png", "logo.png", LogoFitMode.CropCenter).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(asset.Url, Is.EqualTo($"/api/media-assets/{asset.Id}/file"));
            Assert.That(Store.MediaAssets.Single().Id, Is.EqualTo(asset.Id));
            Assert.That(Store.MediaAssets.Single().Content, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that logo uploads create media asset records for SVG images.")]
    public void CreateLogoAsync_CreatesMediaAssetForSvg()
    {
        // Arrange
        using var content = new MemoryStream([1, 2, 3]);

        // Act
        var asset = Service.CreateLogoAsync(content, "image/svg+xml", "logo.svg", LogoFitMode.CropCenter).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(asset.Url, Is.EqualTo($"/api/media-assets/{asset.Id}/file"));
            Assert.That(Store.MediaAssets.Single().Id, Is.EqualTo(asset.Id));
            Assert.That(Store.MediaAssets.Single().ContentType, Is.EqualTo("image/png"));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that logo uploads reject unsupported content types.")]
    public void CreateLogoAsync_RejectsUnsupportedContentType()
    {
        // Arrange
        using var content = new MemoryStream([1, 2, 3]);

        // Act
        Task Act() => Service.CreateLogoAsync(content, "image/gif", "logo.gif", LogoFitMode.CropCenter);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }
}
