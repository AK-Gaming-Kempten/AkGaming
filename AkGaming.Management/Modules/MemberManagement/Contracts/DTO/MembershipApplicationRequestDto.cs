using System.ComponentModel.DataAnnotations;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

public class MembershipApplicationRequestDto {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssuingUserId { get; set; }
    public MemberCreationDto MemberCreationInfo { get; set; } = new();
    public string ApplicationText { get; set; } = string.Empty;
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the privacy policy.")]
    public bool PrivacyPolicyAccepted { get; set; }
    public bool IsResolved { get; set; }
}
