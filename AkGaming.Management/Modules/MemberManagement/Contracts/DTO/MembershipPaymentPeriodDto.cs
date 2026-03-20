namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// DTO representing a membership payment period.
/// </summary>
public class MembershipPaymentPeriodDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal DefaultDueAmount { get; set; }
    public decimal ReducedDueAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
