using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;

namespace AkGaming.Management.Modules.GeneralMeetings.Api.Realtime;

[Authorize]
public sealed class GeneralMeetingHub(MeetingPresenceTracker presence, IMemberQueryService members) : Hub
{
    private static readonly HashSet<MembershipStatus> MeetingAccessStatuses = [MembershipStatus.InTrial, MembershipStatus.Member, MembershipStatus.HonoraryMember, MembershipStatus.SupportingMember, MembershipStatus.Suspended];
    public static string Group(Guid meetingId) => $"general-meeting:{meetingId:N}";

    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(Context.GetHttpContext()?.Request.Query["meetingId"], out var meetingId) || !TryGetUserId(out var userId)) { Context.Abort(); return; }
        if (!Context.User!.HasClaim("permission", "management.general-meetings.manage") && !Context.User.HasClaim("permission", "management.general-meetings.minutes.write"))
        {
            var member = await members.GetMemberByUserGuidAsync(userId);
            if (!member.IsSuccess || !MeetingAccessStatuses.Contains(member.Value!.Status)) { Context.Abort(); return; }
        }
        Context.Items["meetingId"] = meetingId; Context.Items["userId"] = userId;
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(meetingId));
        if (presence.Add(meetingId, userId, Context.ConnectionId)) await Clients.Group(Group(meetingId)).SendAsync("PresenceChanged", userId, true);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("meetingId", out var m) && m is Guid meetingId && Context.Items.TryGetValue("userId", out var u) && u is Guid userId && presence.Remove(meetingId, userId, Context.ConnectionId))
            await Clients.Group(Group(meetingId)).SendAsync("PresenceChanged", userId, false);
        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub"), out userId);
}
