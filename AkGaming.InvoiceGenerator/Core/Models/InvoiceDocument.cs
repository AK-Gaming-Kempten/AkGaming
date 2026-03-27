namespace AkGaming.InvoiceGenerator.Core.Models;

public sealed class InvoiceDocument
{
    public required string InvoiceNumber { get; init; }
    public required DateOnly InvoiceDate { get; init; }
    public DateOnly? ServiceDate { get; init; }
    public required InvoiceParty Seller { get; init; }
    public required InvoiceParty Buyer { get; init; }
    public string IntroText { get; init; } = "Sehr geehrte Damen und Herren,";
    public string BodyText { get; init; } = "wie mit Ihnen besprochen stellen wir Ihnen folgende Positionen in Rechnung:";
    public required IReadOnlyList<InvoiceLineItem> LineItems { get; init; }
    public string? PaymentTerms { get; init; }
    public InvoiceBankDetails? BankDetails { get; init; }
    public string ClosingText { get; init; } = "Bei Rückfragen stehen wir selbstverstaendlich jederzeit gerne zur Verfügung.";
    public string SignatureName { get; init; } = "AK Gaming e.V.";
    public string Greeting { get; init; } = "Mit freundlichen Grüßen";
}

public sealed class InvoiceParty
{
    public required string Name { get; init; }
    public required string Street { get; init; }
    public required string PostalCode { get; init; }
    public required string City { get; init; }
    public string? Country { get; init; }
}

public sealed class InvoiceLineItem
{
    public required string Description { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Quantity { get; init; } = 1m;
    public decimal TotalPrice => UnitPrice * Quantity;
}

public sealed class InvoiceBankDetails
{
    public string? Iban { get; init; }
    public string? Bic { get; init; }
    public string? Blz { get; init; }
    public string? AccountHolder { get; init; }
}
