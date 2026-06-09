using AkGaming.Management.Modules.InvoiceManagement.Domain.Enums;

namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoiceParty
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public InvoicePartyRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Country { get; set; }
}
