using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// DTO for <see cref="AkGaming.Management.Modules.MemberManagement.Domain.Entities.Member"/>, used for data transmission
/// </summary>
public class MemberDto {
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DiscordUserName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public AddressDto Address { get; set; }
    public MembershipStatus Status { get; set; }
    public ICollection<MembershipStatusChangeEventDto> StatusChanges { get; set; }
}