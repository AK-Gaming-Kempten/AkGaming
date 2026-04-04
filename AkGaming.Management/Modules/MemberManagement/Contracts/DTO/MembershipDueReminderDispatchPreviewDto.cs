namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// Preview payload for sending membership due reminder emails.
/// </summary>
public class MembershipDueReminderDispatchPreviewDto {
    public int PaymentPeriodId { get; set; }
    public string PaymentPeriodName { get; set; } = string.Empty;
    public ICollection<MembershipDueReminderRecipientDto> Recipients { get; set; } = [];
    public ICollection<MembershipDueReminderSkipDto> SkippedMembers { get; set; } = [];
}
