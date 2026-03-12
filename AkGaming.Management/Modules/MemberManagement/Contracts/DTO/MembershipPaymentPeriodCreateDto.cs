namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// Data required to create a new membership payment period and its dues.
/// </summary>
public class MembershipPaymentPeriodCreateDto {
    public string Name { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal DefaultDueAmount { get; set; }
}
