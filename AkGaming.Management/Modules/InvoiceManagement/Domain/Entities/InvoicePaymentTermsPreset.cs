namespace AkGaming.Management.Modules.InvoiceManagement.Domain.Entities;

public sealed class InvoicePaymentTermsPreset
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
