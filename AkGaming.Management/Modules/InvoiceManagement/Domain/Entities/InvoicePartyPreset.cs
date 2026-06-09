namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoicePartyPreset
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Country { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
