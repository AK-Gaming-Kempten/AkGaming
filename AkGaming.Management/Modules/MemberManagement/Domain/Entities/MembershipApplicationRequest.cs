using AkGaming.Management.Modules.MemberManagement.Domain.ValueObjects;

namespace AkGaming.Management.Modules.MemberManagement.Domain.Entities;

public class MembershipApplicationRequest {
    public Guid Id { get; set; }
    public Guid IssuingUserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DiscordUserName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Address? Address { get; set; }
    public string ApplicationText { get; set; } = string.Empty;
    public bool PrivacyPolicyAccepted { get; set; }
    public bool IsResolved { get; set; }
}
