using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class PaymentMethodSnapshotDto
{
    public Guid PaymentInformationId { get; set; }
    public PaymentInformationType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PayPalEmail { get; set; }
    public string? AccountHolder { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
}
