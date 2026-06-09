namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? ServiceDate { get; set; }
    public string IntroText { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public string? PaymentTerms { get; set; }
    public string ClosingText { get; set; } = string.Empty;
    public string SignatureName { get; set; } = string.Empty;
    public string Greeting { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<InvoiceParty> Parties { get; set; } = new List<InvoiceParty>();
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public InvoiceBankDetails? BankDetails { get; set; }
}
