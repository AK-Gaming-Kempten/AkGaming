using System.Security.Claims;
using AkGaming.Management.Modules.GeneralMeetings.Api.Realtime;
using AkGaming.Management.Modules.GeneralMeetings.Application.Services;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;

namespace AkGaming.Management.Modules.GeneralMeetings.Api.Controllers;

[ApiController]
[Route("general-meetings")]
[Authorize]
public sealed class GeneralMeetingsController(IGeneralMeetingService service, IMemberQueryService members, IHubContext<GeneralMeetingHub> hub, MeetingPresenceTracker presence) : ControllerBase
{
    private static readonly HashSet<MembershipStatus> MeetingAccessStatuses = [MembershipStatus.InTrial, MembershipStatus.Member, MembershipStatus.HonoraryMember, MembershipStatus.SupportingMember, MembershipStatus.Suspended];
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GeneralMeetingSummaryDto>>> GetMeetings(CancellationToken cancellationToken)
    {
        if (!await HasMeetingAccess()) return Forbid();
        var result = await service.GetMeetingsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GeneralMeetingDto>> GetMeeting(Guid id, CancellationToken cancellationToken)
    {
        if (!await HasMeetingAccess()) return Forbid();
        var result = await service.GetMeetingAsync(id, cancellationToken);
        if (!result.IsSuccess) return NotFound(result.Error);
        var meeting = result.Value!;
        var attendees = meeting.Attendees.Select(x => x with { IsOnline = presence.IsOnline(id, x.UserId) }).ToList();
        return Ok(meeting with { Attendees = attendees });
    }

    [HttpGet("{id:guid}/audit-events")]
    [Authorize(Policy = "management.general-meetings.audit.read")]
    public async Task<ActionResult<IReadOnlyList<MeetingAuditEventDto>>> GetAuditEvents(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetAuditEventsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<GeneralMeetingDto>> CreateMeeting([FromBody] SaveMeetingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.CreateMeetingAsync(request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetMeeting), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<GeneralMeetingDto>> UpdateMeeting(Guid id, [FromBody] SaveMeetingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.UpdateMeetingAsync(id, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(id, "MeetingChanged"); return Ok(result.Value);
    }

    [HttpPost("{meetingId:guid}/agenda")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<AgendaItemDto>> CreateAgendaItem(Guid meetingId, [FromBody] SaveAgendaItemRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.SaveAgendaItemAsync(meetingId, null, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "AgendaChanged"); return Created(string.Empty, result.Value);
    }

    [HttpPut("{meetingId:guid}/agenda/{itemId:guid}")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<AgendaItemDto>> UpdateAgendaItem(Guid meetingId, Guid itemId, [FromBody] SaveAgendaItemRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.SaveAgendaItemAsync(meetingId, itemId, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "AgendaChanged"); return Ok(result.Value);
    }

    [HttpDelete("{meetingId:guid}/agenda/{itemId:guid}")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<IActionResult> DeleteAgendaItem(Guid meetingId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.DeleteAgendaItemAsync(meetingId, itemId, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "AgendaChanged"); return NoContent();
    }

    [HttpPut("{meetingId:guid}/agenda/{itemId:guid}/minutes")]
    [Authorize(Policy = "management.general-meetings.minutes.write")]
    public async Task<ActionResult<AgendaItemDto>> UpdateMinutes(Guid meetingId, Guid itemId, [FromBody] UpdateMinutesRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.UpdateMinutesAsync(meetingId, itemId, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "MinutesChanged"); return Ok(result.Value);
    }

    [HttpPut("{meetingId:guid}/status")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<GeneralMeetingDto>> ChangeStatus(Guid meetingId, [FromBody] ChangeMeetingStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.ChangeStatusAsync(meetingId, request.Status, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "MeetingChanged"); return Ok(result.Value);
    }

    [HttpPut("{meetingId:guid}/agenda/{itemId:guid}/state")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<AgendaItemDto>> ChangeAgendaState(Guid meetingId, Guid itemId, [FromBody] ChangeAgendaStateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.ChangeAgendaStateAsync(meetingId, itemId, request.Status, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "AgendaChanged"); return Ok(result.Value);
    }

    [HttpPost("{meetingId:guid}/check-in")]
    public async Task<ActionResult<AttendanceDto>> CheckIn(Guid meetingId, CancellationToken cancellationToken)
    {
        if (!await HasMeetingAccess()) return Forbid();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CheckInSelfAsync(meetingId, userId, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "AttendanceChanged"); return Ok(result.Value);
    }

    [HttpPut("{meetingId:guid}/attendance")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<AttendanceDto>> SetAttendance(Guid meetingId, [FromBody] AttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.SetAttendanceAsync(meetingId, request.MemberId, request.CheckedIn, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "AttendanceChanged"); return Ok(result.Value);
    }

    [HttpPost("{meetingId:guid}/agenda/{agendaItemId:guid}/ballots")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<BallotDto>> CreateBallot(Guid meetingId, Guid agendaItemId, [FromBody] SaveBallotRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.SaveBallotAsync(meetingId, agendaItemId, null, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "BallotChanged"); return Created(string.Empty, result.Value);
    }

    [HttpPut("{meetingId:guid}/agenda/{agendaItemId:guid}/ballots/{ballotId:guid}")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<BallotDto>> UpdateBallot(Guid meetingId, Guid agendaItemId, Guid ballotId, [FromBody] SaveBallotRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.SaveBallotAsync(meetingId, agendaItemId, ballotId, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "BallotChanged"); return Ok(result.Value);
    }

    [HttpPost("{meetingId:guid}/ballots/{ballotId:guid}/open")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<BallotDto>> OpenBallot(Guid meetingId, Guid ballotId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.OpenBallotAsync(meetingId, ballotId, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "BallotChanged"); return Ok(result.Value);
    }

    [HttpPost("{meetingId:guid}/ballots/{ballotId:guid}/close")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<BallotDto>> CloseBallot(Guid meetingId, Guid ballotId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.CloseBallotAsync(meetingId, ballotId, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "BallotChanged"); return Ok(result.Value);
    }

    [HttpPost("ballots/{ballotId:guid}/credential")]
    public async Task<ActionResult<IssuedCredentialDto>> IssueOwnCredential(Guid ballotId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.IssueCredentialForUserAsync(ballotId, userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("ballots/{ballotId:guid}/credential/{memberId:guid}")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<IssuedCredentialDto>> IssueCredential(Guid ballotId, Guid memberId, CancellationToken cancellationToken)
    {
        var result = await service.IssueCredentialAsync(ballotId, memberId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("ballots/{ballotId:guid}/votes")]
    [AllowAnonymous]
    public async Task<IActionResult> CastVote(Guid ballotId, [FromBody] CastVoteRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CastVoteAsync(ballotId, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(result.Value, "BallotChanged");
        return NoContent();
    }

    [HttpPost("{meetingId:guid}/invitations")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<IActionResult> DispatchInvitations(Guid meetingId, [FromBody] DispatchInvitationRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.DispatchInvitationsAsync(meetingId, request, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "MeetingChanged"); return NoContent();
    }

    [HttpPost("{meetingId:guid}/finalize")]
    [Authorize(Policy = "management.general-meetings.manage")]
    public async Task<ActionResult<ProtocolDto>> FinalizeMeeting(Guid meetingId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actor)) return Unauthorized();
        var result = await service.FinalizeAsync(meetingId, actor, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        await Changed(meetingId, "MeetingChanged"); return Ok(result.Value);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId);
    private async Task<bool> HasMeetingAccess()
    {
        if (User.HasClaim("permission", "management.general-meetings.manage") || User.HasClaim("permission", "management.general-meetings.minutes.write")) return true;
        if (!TryGetUserId(out var userId)) return false;
        var member = await members.GetMemberByUserGuidAsync(userId);
        return member.IsSuccess && MeetingAccessStatuses.Contains(member.Value!.Status);
    }
    private Task Changed(Guid meetingId, string eventName) => hub.Clients.Group(GeneralMeetingHub.Group(meetingId)).SendAsync(eventName, meetingId);
}
