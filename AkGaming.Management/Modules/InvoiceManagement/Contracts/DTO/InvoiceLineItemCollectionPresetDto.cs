namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed class InvoiceLineItemCollectionPresetDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public List<InvoiceLineItemDto> LineItems { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
