namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceLineItemCollectionPreset
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<InvoiceLineItemCollectionPresetItem> LineItems { get; set; } = new List<InvoiceLineItemCollectionPresetItem>();
}
