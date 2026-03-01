using AkGaming.Core.Common.Generics;

namespace AkGaming.Management.Modules.MemberManagement.Domain.Entities;

using AkGaming.Management.Modules.MemberManagement.Domain.Enums;
using AkGaming.Management.Modules.MemberManagement.Domain.ValueObjects;

public class Member {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DiscordUsername { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Address? Address { get; set; }
    public MembershipStatus Status { get; set; }
    public ICollection<MembershipStatusChangeEvent> StatusChanges { get; set; } = new List<MembershipStatusChangeEvent>();

    public Result ChangeStatus(MembershipStatus newStatus) {
        if (newStatus == Status)
            return Result.Failure("Member is already in the given status");

        var evt = new MembershipStatusChangeEvent {
            MemberId = Id,
            OldStatus = Status,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow
        };

        Status = newStatus;
        StatusChanges.Add(evt);
        return Result.Success();
    }
}