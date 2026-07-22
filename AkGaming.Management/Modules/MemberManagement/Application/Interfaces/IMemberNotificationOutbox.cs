using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMemberNotificationOutbox
{
    void EnqueueMembershipApplicationCreated(MembershipApplicationRequest request);
    void EnqueueMemberLinkingRequestCreated(MemberLinkingRequest request);
}

public sealed class NullMemberNotificationOutbox : IMemberNotificationOutbox
{
    public void EnqueueMembershipApplicationCreated(MembershipApplicationRequest request)
    {
    }

    public void EnqueueMemberLinkingRequestCreated(MemberLinkingRequest request)
    {
    }
}
