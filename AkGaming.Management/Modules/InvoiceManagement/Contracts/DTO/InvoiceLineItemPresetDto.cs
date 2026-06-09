namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed class InvoiceLineItemPresetDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public InvoiceLineItemDto LineItem { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
