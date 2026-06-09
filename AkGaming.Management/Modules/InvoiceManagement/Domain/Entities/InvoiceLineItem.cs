namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
}
