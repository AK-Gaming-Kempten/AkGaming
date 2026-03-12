using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// DTO for membership due data.
/// </summary>
public class MembershipDueDto {
    public int Id { get; set; }
    public int PaymentPeriodId { get; set; }
    public Guid MemberId { get; set; }
    public MembershipDueStatus Status { get; set; }
    public decimal DueAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public string? SettlementReference { get; set; }

    public bool IsOverdue() => IsOverdueAt(DateOnly.FromDateTime(DateTime.UtcNow));

    public bool IsOverdueAt(DateOnly referenceDate) =>
        Status == MembershipDueStatus.Pending && DueDate < referenceDate;
}
