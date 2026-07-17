namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class Receipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseItemId { get; set; }
    public ExpenseItem? ExpenseItem { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string StorageKey { get; set; } = string.Empty;
}
