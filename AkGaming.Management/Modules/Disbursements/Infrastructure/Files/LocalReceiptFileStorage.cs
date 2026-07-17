using AkGaming.Management.Modules.Disbursements.Application.Interfaces;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure.Files;

public sealed class LocalReceiptFileStorage(string rootPath) : IReceiptFileStorage
{
    public async Task<string> SaveAsync(Guid receiptId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(rootPath);
        var extension = Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant();
        var key = $"{receiptId:N}{extension}";
        var fullPath = Resolve(key);
        await using var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(target, cancellationToken);
        return key;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storageKey);
        Stream? stream = File.Exists(fullPath) ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true) : null;
        return Task.FromResult(stream);
    }

    private string Resolve(string storageKey)
    {
        var safeName = Path.GetFileName(storageKey);
        return Path.Combine(rootPath, safeName);
    }
}
