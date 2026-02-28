using MemberManagement.Domain.Enums;

namespace MemberManagement.Domain.Entities;

public class MembershipStatusChangeEvent
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public MembershipStatus OldStatus { get; set; }
    public MembershipStatus NewStatus { get; set; }
    public DateTime Timestamp { get; set; }
}