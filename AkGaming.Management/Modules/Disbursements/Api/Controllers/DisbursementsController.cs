using System.Text.Json;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.Disbursements.Api.Controllers;

[ApiController]
[Route("disbursements")]
[Authorize]
public sealed class DisbursementsController(IDisbursementService service) : ControllerBase
{
    [HttpGet("reimbursements/me")]
    public async Task<ActionResult<IReadOnlyList<ReimbursementDto>>> GetMyReimbursements(CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.GetReimbursementsAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("reimbursements/me/{id:guid}")]
    public async Task<ActionResult<ReimbursementDto>> GetMyReimbursement(Guid id, CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.GetReimbursementAsync(id, userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("reimbursements")]
    [RequestSizeLimit(55 * 1024 * 1024)]
    public async Task<ActionResult<ReimbursementDto>> CreateReimbursement([FromForm] string requestJson, [FromForm] List<IFormFile> receipts, CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        CreateReimbursementRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<CreateReimbursementRequest>(requestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return BadRequest("The reimbursement form is invalid.");
        }
        if (request is null)
            return BadRequest("The reimbursement form is required.");

        var streams = new List<Stream>();
        try
        {
            var uploads = new List<ReceiptUpload>();
            foreach (var receipt in receipts)
            {
                var stream = receipt.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new ReceiptUpload(receipt.FileName, receipt.ContentType, receipt.Length, stream));
            }
            var result = await service.CreateReimbursementAsync(userId, ControllerIdentity.GetDisplayName(User), request, uploads, cancellationToken);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return CreatedAtAction(nameof(GetMyReimbursement), new { id = result.Value!.Id }, result.Value);
        }
        finally
        {
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }

    [HttpPost("reimbursements/me/{id:guid}/cancel")]
    public async Task<ActionResult<ReimbursementDto>> CancelMyReimbursement(Guid id, CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.CancelReimbursementAsync(id, userId, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("receipts/{id:guid}")]
    public async Task<IActionResult> DownloadMyReceipt(Guid id, CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.GetReceiptAsync(id, userId, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("allocations/me")]
    public async Task<ActionResult<IReadOnlyList<AllocationDto>>> GetMyAllocations(CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.GetAllocationsForUserAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("allocations/share/{token:guid}")]
    public async Task<ActionResult<AllocationDto>> GetSharedAllocation(Guid token, CancellationToken cancellationToken)
    {
        var result = await service.GetAllocationByTokenAsync(token, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("allocations/share/{token:guid}/applications")]
    public async Task<ActionResult<AllocationApplicationDto>> Apply(Guid token, [FromBody] CreateAllocationApplicationRequest request, CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.ApplyAsync(token, userId, ControllerIdentity.GetDisplayName(User), request, cancellationToken);
        return result.IsSuccess ? Created(string.Empty, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("allocations/share/{token:guid}/applications/{applicationId:guid}/decision")]
    public async Task<ActionResult<AllocationApplicationDto>> Decide(Guid token, Guid applicationId, [FromBody] DecideAllocationApplicationRequest request, CancellationToken cancellationToken)
    {
        if (!ControllerIdentity.TryGetUserId(User, out var userId))
            return Forbid();

        var result = await service.DecideAsync(token, applicationId, userId, ControllerIdentity.GetDisplayName(User), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
