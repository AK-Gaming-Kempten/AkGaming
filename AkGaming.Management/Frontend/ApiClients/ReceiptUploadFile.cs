namespace AkGaming.Management.Frontend.ApiClients;

public sealed record ReceiptUploadFile(
    string FileName,
    string ContentType,
    byte[] Content)
{
    public long Size => Content.LongLength;
}
