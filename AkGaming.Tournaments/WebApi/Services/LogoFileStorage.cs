using AkGaming.Tournaments.Application.Services;
using SkiaSharp;
using Svg.Skia;

namespace AkGaming.Tournaments.WebApi.Services;

public sealed class LogoFileStorage : ILogoFileStorage
{
    private const int LogoSize = 512;

    public async Task<StoredLogoFile> SaveAsync(Stream content, string contentType, string originalFileName, LogoFitMode fitMode, CancellationToken cancellationToken = default)
    {
        var imageBytes = await CreateSquareLogoAsync(content, contentType, originalFileName, fitMode, cancellationToken);
        return new StoredLogoFile(
            imageBytes,
            "image/png",
            GetPngFileName(originalFileName),
            imageBytes.LongLength);
    }

    private static async Task<byte[]> CreateSquareLogoAsync(Stream content, string contentType, string originalFileName, LogoFitMode fitMode, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        if (IsSvg(contentType, originalFileName))
            return CreateSquareSvgLogo(memory, fitMode);

        using var source = SKBitmap.Decode(memory);
        if (source is null)
            throw new InvalidOperationException("The uploaded logo image could not be decoded.");

        using var output = new SKBitmap(LogoSize, LogoSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        var scale = fitMode == LogoFitMode.ContainFill
            ? Math.Min((float)LogoSize / source.Width, (float)LogoSize / source.Height)
            : Math.Max((float)LogoSize / source.Width, (float)LogoSize / source.Height);
        var width = source.Width * scale;
        var height = source.Height * scale;
        var destination = new SKRect(
            (LogoSize - width) / 2,
            (LogoSize - height) / 2,
            (LogoSize + width) / 2,
            (LogoSize + height) / 2);

        canvas.DrawBitmap(source, destination);
        canvas.Flush();

        using var image = SKImage.FromBitmap(output);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] CreateSquareSvgLogo(Stream content, LogoFitMode fitMode)
    {
        using var svg = new SKSvg();
        var picture = svg.Load(content);
        if (picture is null)
            throw new InvalidOperationException("The uploaded SVG logo could not be decoded.");

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("The uploaded SVG logo has no drawable size.");

        using var output = new SKBitmap(LogoSize, LogoSize, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);

        var scale = fitMode == LogoFitMode.ContainFill
            ? Math.Min(LogoSize / bounds.Width, LogoSize / bounds.Height)
            : Math.Max(LogoSize / bounds.Width, LogoSize / bounds.Height);
        var width = bounds.Width * scale;
        var height = bounds.Height * scale;

        canvas.Translate((LogoSize - width) / 2, (LogoSize - height) / 2);
        canvas.Scale(scale);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = SKImage.FromBitmap(output);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static bool IsSvg(string contentType, string originalFileName)
    {
        return string.Equals(contentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(originalFileName), ".svg", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPngFileName(string originalFileName)
        => string.IsNullOrWhiteSpace(originalFileName)
            ? "logo.png"
            : $"{Path.GetFileNameWithoutExtension(originalFileName)}.png";
}
