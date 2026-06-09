namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed class InvoiceBankDetailsDto
{
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Blz { get; set; }
    public string? AccountHolder { get; set; }
}
