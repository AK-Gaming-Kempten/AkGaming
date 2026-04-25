namespace AkGaming.Tournaments.Application.Services;

public interface ILogoFileStorage
{
    Task<StoredLogoFile> SaveAsync(Stream content, string contentType, string originalFileName, LogoFitMode fitMode, CancellationToken cancellationToken = default);
}

public enum LogoFitMode
{
    CropCenter,
    ContainFill
}

public sealed record StoredLogoFile(
    byte[] Content,
    string ContentType,
    string OriginalFileName,
    long SizeBytes);
