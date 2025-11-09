using MemberManagement.Contracts.Enums;

namespace MemberManagement.Contracts.DTO;

public class MemberLinkingRequestDto {
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DiscordUserName { get; set; } = string.Empty;
    public MemberLinkingRequestReason Reason { get; set; }
    public bool IsResolved { get; set; }
}