using System.Security.Claims;
using AkGaming.Management.Modules.BoardManagement.Application.Services;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.BoardManagement.Api.Controllers;

[ApiController]
[Route("board-meetings")]
public sealed class BoardMeetingsController(IBoardMeetingService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "management.board-meetings.read")]
    public async Task<ActionResult<IReadOnlyList<BoardMeetingSummaryDto>>> GetMeetings(CancellationToken cancellationToken)
    {
        var result = await service.GetMeetingsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "management.board-meetings.read")]
    public async Task<ActionResult<BoardMeetingDto>> GetMeeting(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetMeetingAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("agenda/backlog")]
    [Authorize(Policy = "management.board-meetings.read")]
    public async Task<ActionResult<IReadOnlyList<BoardAgendaItemDto>>> GetBacklog(CancellationToken cancellationToken)
    {
        var result = await service.GetBacklogAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardMeetingDto>> CreateMeeting([FromBody] CreateBoardMeetingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CreateMeetingAsync(request, userId, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(GetMeeting), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardMeetingDto>> RescheduleMeeting(Guid id, [FromBody] RescheduleBoardMeetingRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RescheduleMeetingAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardMeetingDto>> CancelMeeting(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.CancelMeetingAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/reschedule-proposals")]
    [Authorize(Policy = "management.board-meetings.read")]
    public async Task<ActionResult<BoardRescheduleProposalDto>> ProposeReschedule(Guid id, [FromBody] CreateRescheduleProposalRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.ProposeRescheduleAsync(id, request, userId, GetDisplayName(), cancellationToken);
        return result.IsSuccess ? Created(string.Empty, result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{meetingId:guid}/reschedule-proposals/{proposalId:guid}/accept")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardMeetingDto>> AcceptProposal(Guid meetingId, Guid proposalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.DecideProposalAsync(meetingId, proposalId, true, userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{meetingId:guid}/reschedule-proposals/{proposalId:guid}/reject")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardMeetingDto>> RejectProposal(Guid meetingId, Guid proposalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.DecideProposalAsync(meetingId, proposalId, false, userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{meetingId:guid}/availability")]
    [Authorize(Policy = "management.board-meetings.read")]
    public async Task<ActionResult<BoardAvailabilityDto>> SetAvailability(Guid meetingId, [FromBody] SetBoardAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.SetAvailabilityAsync(meetingId, userId, GetDisplayName(), request.Status, null, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{meetingId:guid}/availability/discord")]
    [Authorize(Policy = "management.board-meetings.discord-interactions")]
    public async Task<ActionResult<BoardAvailabilityDto>> SetDiscordAvailability(Guid meetingId, [FromBody] SetDiscordBoardAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SetAvailabilityAsync(meetingId, request.UserId, request.DisplayName, request.Status, request.ScheduleVersion, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("agenda")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardAgendaItemDto>> CreateAgendaItem([FromBody] SaveBoardAgendaItemRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.CreateAgendaItemAsync(request, userId, cancellationToken);
        return result.IsSuccess ? Created(string.Empty, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("agenda/{itemId:guid}")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardAgendaItemDto>> UpdateAgendaItem(Guid itemId, [FromBody] SaveBoardAgendaItemRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAgendaItemAsync(itemId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("agenda/{itemId:guid}")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardAgendaItemDto>> DeleteAgendaItem(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAgendaItemAsync(itemId, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPut("agenda/{itemId:guid}/move")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardAgendaItemDto>> MoveAgendaItem(Guid itemId, [FromBody] MoveBoardAgendaItemRequest request, CancellationToken cancellationToken)
    {
        var result = await service.MoveAgendaItemAsync(itemId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{meetingId:guid}/agenda/order")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<IReadOnlyList<BoardAgendaItemDto>>> ReorderAgendaItems(Guid meetingId, [FromBody] ReorderBoardAgendaItemsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReorderAgendaItemsAsync(meetingId, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{meetingId:guid}/agenda/from-backlog")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardMeetingDto>> AssignAgendaItems(Guid meetingId, [FromBody] AssignBoardAgendaItemsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignAgendaItemsAsync(meetingId, request, cancellationToken);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPut("agenda/{itemId:guid}/status")]
    [Authorize(Policy = "management.board-meetings.manage")]
    public async Task<ActionResult<BoardAgendaItemDto>> ChangeAgendaItemStatus(Guid itemId, [FromBody] ChangeBoardAgendaItemStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeAgendaItemStatusAsync(itemId, request.Status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out userId);
    }

    private string GetDisplayName()
    {
        return User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("preferred_username") ?? "Board member";
    }
}
