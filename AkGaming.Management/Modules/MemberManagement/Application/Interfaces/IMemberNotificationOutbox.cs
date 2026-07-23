using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Domain.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMemberNotificationOutbox
{
    void EnqueueMembershipApplicationCreated(MembershipApplicationRequest request);
    void EnqueueMembershipApplicationStatusChanged(MembershipApplicationRequest request, bool accepted);
    void EnqueueMemberLinkingRequestCreated(MemberLinkingRequest request);
    void EnqueueMemberLinkingRequestStatusChanged(MemberLinkingRequest request, bool accepted);
    void EnqueueMembershipStatusChanged(Member member, MembershipStatus previousStatus);
}

public sealed class NullMemberNotificationOutbox : IMemberNotificationOutbox
{
    public void EnqueueMembershipApplicationCreated(MembershipApplicationRequest request)
    {
    }

    public void EnqueueMembershipApplicationStatusChanged(MembershipApplicationRequest request, bool accepted)
    {
    }

    public void EnqueueMemberLinkingRequestCreated(MemberLinkingRequest request)
    {
    }

    public void EnqueueMemberLinkingRequestStatusChanged(MemberLinkingRequest request, bool accepted)
    {
    }

    public void EnqueueMembershipStatusChanged(Member member, MembershipStatus previousStatus)
    {
    }
}
