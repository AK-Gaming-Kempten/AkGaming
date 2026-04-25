using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.WebApi.Services;
using SkiaSharp;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class LogoFileStorageTests
{
    [Test]
    [Description("Verifies that cropped logo uploads are saved as square PNG files.")]
    public async Task SaveAsync_CropCenter_SavesSquarePng()
    {
        // Arrange
        using var content = CreateImageStream(256, 128, SKColors.Red);
        var storage = CreateStorage();

        // Act
        var storedFile = await storage.SaveAsync(content, "image/png", "wide-logo.png", LogoFitMode.CropCenter);

        // Assert
        using var savedImage = DecodeSavedImage(storedFile.Content);
        Assert.Multiple(() =>
        {
            Assert.That(storedFile.ContentType, Is.EqualTo("image/png"));
            Assert.That(storedFile.OriginalFileName, Is.EqualTo("wide-logo.png"));
            Assert.That(savedImage.Width, Is.EqualTo(512));
            Assert.That(savedImage.Height, Is.EqualTo(512));
            Assert.That(savedImage.GetPixel(0, 0), Is.EqualTo(SKColors.Red));
        });
    }

    [Test]
    [Description("Verifies that contained logo uploads are centered on a transparent square canvas.")]
    public async Task SaveAsync_ContainFill_SavesSquarePngWithTransparentCanvas()
    {
        // Arrange
        using var content = CreateImageStream(256, 128, SKColors.Red);
        var storage = CreateStorage();

        // Act
        var storedFile = await storage.SaveAsync(content, "image/png", "wide-logo.png", LogoFitMode.ContainFill);

        // Assert
        using var savedImage = DecodeSavedImage(storedFile.Content);
        Assert.Multiple(() =>
        {
            Assert.That(storedFile.ContentType, Is.EqualTo("image/png"));
            Assert.That(savedImage.Width, Is.EqualTo(512));
            Assert.That(savedImage.Height, Is.EqualTo(512));
            Assert.That(savedImage.GetPixel(0, 0).Alpha, Is.EqualTo(0));
            Assert.That(savedImage.GetPixel(256, 256), Is.EqualTo(SKColors.Red));
        });
    }

    [Test]
    [Description("Verifies that SVG logo uploads are rasterized into square PNG files.")]
    public async Task SaveAsync_Svg_SavesSquarePng()
    {
        // Arrange
        using var content = CreateSvgStream();
        var storage = CreateStorage();

        // Act
        var storedFile = await storage.SaveAsync(content, "image/svg+xml", "vector-logo.svg", LogoFitMode.ContainFill);

        // Assert
        using var savedImage = DecodeSavedImage(storedFile.Content);
        Assert.Multiple(() =>
        {
            Assert.That(storedFile.ContentType, Is.EqualTo("image/png"));
            Assert.That(storedFile.OriginalFileName, Is.EqualTo("vector-logo.png"));
            Assert.That(savedImage.Width, Is.EqualTo(512));
            Assert.That(savedImage.Height, Is.EqualTo(512));
            Assert.That(savedImage.GetPixel(256, 256).Alpha, Is.GreaterThan(0));
        });
    }

    private static LogoFileStorage CreateStorage()
        => new();

    private static MemoryStream CreateImageStream(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    private static MemoryStream CreateSvgStream()
    {
        const string svg = """
                           <svg xmlns="http://www.w3.org/2000/svg" width="256" height="128" viewBox="0 0 256 128">
                             <rect x="0" y="0" width="256" height="128" fill="#ff0000" />
                           </svg>
                           """;
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svg));
    }

    private static SKBitmap DecodeSavedImage(byte[] content)
        => SKBitmap.Decode(content)!;
}
