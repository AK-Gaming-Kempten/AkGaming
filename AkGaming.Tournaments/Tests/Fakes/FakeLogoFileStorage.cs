using AkGaming.Tournaments.Application.Services;

namespace AkGaming.Tournaments.Tests.Fakes;

internal sealed class FakeLogoFileStorage : ILogoFileStorage
{
    public Task<StoredLogoFile> SaveAsync(Stream content, string contentType, string originalFileName, LogoFitMode fitMode, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new StoredLogoFile(
            [1, 2, 3],
            "image/png",
            Path.GetFileNameWithoutExtension(originalFileName) + ".png",
            3));
    }
}
