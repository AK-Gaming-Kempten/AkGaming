namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceBankAccountPreset
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Blz { get; set; }
    public string? AccountHolder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
