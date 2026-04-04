namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// Member that will not receive a reminder email and the reason why.
/// </summary>
public class MembershipDueReminderSkipDto {
    public Guid MemberId { get; set; }
    public string MemberDisplayName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
