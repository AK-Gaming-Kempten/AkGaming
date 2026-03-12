namespace AkGaming.Management.Modules.MemberManagement.Domain.Entities;

using AkGaming.Management.Modules.MemberManagement.Domain.Enums;

public class MembershipDue {
    public int Id { get; set; }
    public int PaymentPeriodId { get; set; }
    public MembershipPaymentPeriod? PaymentPeriod { get; set; }
    public Guid MemberId { get; set; }
    public MembershipDueStatus Status { get; set; }
    public decimal DueAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public string? SettlementReference { get; set; }
}
