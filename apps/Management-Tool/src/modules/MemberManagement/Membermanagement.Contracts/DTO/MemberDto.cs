using Membermanagement.Contracts.Enums;

namespace Membermanagement.Contracts.DTO;

/// <summary>
/// DTO for <see cref="MemberManagement.Domain.Entities.Member"/>, used for data transmission
/// </summary>
public class MemberDto {
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DiscordUserName { get; set; }
    public DateTime BirthDate { get; set; }
    public AddressDto Address { get; set; }
    public MembershipStatus Status { get; set; }
    public ICollection<MembershipStatusChangeEventDto> StatusChanges { get; set; }
}