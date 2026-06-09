namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed class InvoiceDetailsDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ServiceDate { get; set; }
    public InvoicePartyDto Seller { get; set; } = new();
    public InvoicePartyDto Buyer { get; set; } = new();
    public string IntroText { get; set; } = "Sehr geehrte Damen und Herren,";
    public string BodyText { get; set; } = "wie mit Ihnen besprochen stellen wir Ihnen folgende Positionen in Rechnung:";
    public List<InvoiceLineItemDto> LineItems { get; set; } = [];
    public string? PaymentTerms { get; set; }
    public InvoiceBankDetailsDto BankDetails { get; set; } = new();
    public string ClosingText { get; set; } = "Bei Rückfragen stehen wir Ihnen jederzeit gerne zur Verfügung.";
    public string SignatureName { get; set; } = string.Empty;
    public string Greeting { get; set; } = "Mit freundlichen Grüßen";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
