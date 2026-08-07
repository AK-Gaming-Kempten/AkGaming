using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.Disbursements.Api.Controllers;

[ApiController]
[Route("disbursements/admin")]
[Authorize(Policy = "management.disbursements.read")]
public sealed class DisbursementAdministrationController(IDisbursementService service) : ControllerBase
{
    [HttpGet("reimbursements")]
    public async Task<ActionResult<IReadOnlyList<ReimbursementDto>>> GetReimbursements(CancellationToken cancellationToken)
    {
        var result = await service.GetReimbursementsAsync(null, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("reimbursements/{id:guid}")]
    public async Task<ActionResult<ReimbursementDto>> GetReimbursement(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetReimbursementAsync(id, null, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("reimbursements/{id:guid}/status")]
    [Authorize(Policy = "management.disbursements.manage")]
    public async Task<ActionResult<ReimbursementDto>> UpdateReimbursementStatus(Guid id, [FromBody] UpdateReimbursementStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateReimbursementStatusAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("receipts/{id:guid}")]
    public async Task<IActionResult> DownloadReceipt(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetReceiptAsync(id, null, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<DisbursementEventDto>>> GetEvents(CancellationToken cancellationToken)
    {
        var result = await service.GetEventsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("events/{id:guid}")]
    public async Task<ActionResult<DisbursementEventDto>> GetEvent(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetEventAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("events")]
    [Authorize(Policy = "management.disbursements.manage")]
    public async Task<ActionResult<DisbursementEventDto>> CreateEvent([FromBody] SaveDisbursementEventRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateEventAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetEvent), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("events/{eventId:guid}/allocations")]
    [Authorize(Policy = "management.disbursements.manage")]
    public async Task<ActionResult<AllocationDto>> CreateAllocation(Guid eventId, [FromBody] SaveAllocationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAllocationAsync(eventId, request, cancellationToken);
        return result.IsSuccess ? Created(string.Empty, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("allocations/{allocationId:guid}")]
    [Authorize(Policy = "management.disbursements.manage")]
    public async Task<ActionResult<AllocationDto>> UpdateAllocation(Guid allocationId, [FromBody] SaveAllocationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAllocationAsync(allocationId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("applications/{applicationId:guid}/status")]
    [Authorize(Policy = "management.disbursements.manage")]
    public async Task<ActionResult<AllocationApplicationDto>> UpdateApplicationStatus(Guid applicationId, [FromBody] UpdateAllocationApplicationStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateApplicationStatusAsync(applicationId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
