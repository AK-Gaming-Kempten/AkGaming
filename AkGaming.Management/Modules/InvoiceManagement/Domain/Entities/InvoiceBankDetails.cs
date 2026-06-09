namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceBankDetails
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Blz { get; set; }
    public string? AccountHolder { get; set; }
}
