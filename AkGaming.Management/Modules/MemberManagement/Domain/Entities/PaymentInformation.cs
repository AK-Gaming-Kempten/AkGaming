using AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Domain.Entities;

public class PaymentInformation {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }
    public PaymentInformationType Type { get; set; }
    public string? PayPalEmail { get; set; }
    public string? AccountHolder { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
}
