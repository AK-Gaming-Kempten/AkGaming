namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceLineItemCollectionPresetItem
{
    public Guid Id { get; set; }
    public Guid CollectionPresetId { get; set; }
    public InvoiceLineItemCollectionPreset CollectionPreset { get; set; } = null!;
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
}
