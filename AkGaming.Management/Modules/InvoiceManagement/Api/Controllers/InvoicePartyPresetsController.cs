using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;

[ApiController]
[Route("invoice-party-presets")]
[Authorize(Policy = "AdminOnly")]
public sealed class InvoicePartyPresetsController(IInvoiceManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoicePartyPresetDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetPartyPresetsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<InvoicePartyPresetDto>> Create([FromBody] InvoicePartyPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreatePartyPresetAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Created($"invoice-party-presets/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoicePartyPresetDto>> Update(Guid id, [FromBody] InvoicePartyPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdatePartyPresetAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DeletePartyPresetAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }
}
