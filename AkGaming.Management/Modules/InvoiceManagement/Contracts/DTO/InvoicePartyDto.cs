namespace AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;

public sealed class InvoicePartyDto
{
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Country { get; set; }
}
