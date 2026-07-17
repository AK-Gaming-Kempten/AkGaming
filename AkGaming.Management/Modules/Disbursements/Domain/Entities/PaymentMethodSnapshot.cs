namespace AkGaming.Management.Modules.Disbursements.Domain.Entities;

public sealed class PaymentMethodSnapshot
{
    public Guid PaymentInformationId { get; set; }
    public int Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PayPalEmail { get; set; }
    public string? AccountHolder { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
}
