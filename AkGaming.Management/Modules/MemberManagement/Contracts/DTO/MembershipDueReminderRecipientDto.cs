namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// Member that is eligible to receive a reminder email.
/// </summary>
public class MembershipDueReminderRecipientDto {
    public int DueId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal DueAmount { get; set; }
    public DateOnly DueDate { get; set; }
}
