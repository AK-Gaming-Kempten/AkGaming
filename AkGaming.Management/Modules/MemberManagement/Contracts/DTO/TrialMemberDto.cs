namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

public sealed class TrialMemberDto {
    public required MemberDto Member { get; set; }
    public DateTime? TrialStartedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}
