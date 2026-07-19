using System.Security.Claims;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Frontend.Authorization;

public sealed class GeneralMeetingAccessService(MemberManagementApiClient members)
{
    private static readonly HashSet<MembershipStatus> MeetingAccessStatuses =
    [
        MembershipStatus.InTrial,
        MembershipStatus.Member,
        MembershipStatus.HonoraryMember,
        MembershipStatus.SupportingMember,
        MembershipStatus.Suspended
    ];

    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
            return false;

        var result = await members.GetMemberByUserGuidAsync(userId, cancellationToken);
        return result.IsSuccess && result.Value is not null && MeetingAccessStatuses.Contains(result.Value.Status);
    }
}
