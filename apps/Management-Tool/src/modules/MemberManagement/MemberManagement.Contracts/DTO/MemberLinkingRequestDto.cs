using MemberManagement.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace MemberManagement.Contracts.DTO;

public class MemberLinkingRequestDto {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssuingUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DiscordUserName { get; set; } = string.Empty;
    public MemberLinkingRequestReason Reason { get; set; }
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the privacy policy.")]
    public bool PrivacyPolicyAccepted { get; set; }
    public bool IsResolved { get; set; }
}
