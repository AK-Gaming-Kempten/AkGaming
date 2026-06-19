using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

public class PaymentInformationDto {
    public Guid Id { get; set; }
    public PaymentInformationType Type { get; set; }
    public string? PayPalEmail { get; set; }
    public string? AccountHolder { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
}
