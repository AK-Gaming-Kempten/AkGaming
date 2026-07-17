namespace AkGaming.Management.Modules.Disbursements.Application.Interfaces;

public interface IReceiptFileStorage
{
    Task<string> SaveAsync(Guid receiptId, string fileName, Stream content, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
