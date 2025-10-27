using MemberManagement.Contracts.Enums;

namespace MemberManagement.Contracts.DTO;

/// <summary>
/// DTO responsible for <see cref="MemberManagement.Domain.Entities.MembershipStatusChangeEvent"/>, used for data transmission
/// </summary>
public class MembershipStatusChangeEventDto {
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public MembershipStatus OldStatus { get; set; }
    public MembershipStatus NewStatus { get; set; }
    public DateTime Timestamp { get; set; }
}