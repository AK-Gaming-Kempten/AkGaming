namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceLineItemPreset
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
