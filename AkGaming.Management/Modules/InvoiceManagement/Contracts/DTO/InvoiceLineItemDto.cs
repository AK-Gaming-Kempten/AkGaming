namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed class InvoiceLineItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal TotalPrice => UnitPrice * Quantity;
}
