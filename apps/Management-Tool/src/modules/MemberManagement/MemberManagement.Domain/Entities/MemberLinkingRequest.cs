using MemberManagement.Domain.Enums;

namespace MemberManagement.Domain.Entities;

public class MemberLinkingRequest {
    public Guid Id { get; set; }
    public Guid IssuingUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DiscordUserName { get; set; } = string.Empty;
    public MemberLinkingRequestReason Reason { get; set; }
    public bool PrivacyPolicyAccepted { get; set; }
    public bool IsResolved { get; set; }
}
