namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class ReceiptDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
}
