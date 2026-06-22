using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;

[ApiController]
[Route("invoice-payment-terms-presets")]
[Authorize(Policy = "management.invoices.manage")]
public sealed class InvoicePaymentTermsPresetsController(IInvoiceManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoicePaymentTermsPresetDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetPaymentTermsPresetsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<InvoicePaymentTermsPresetDto>> Create([FromBody] InvoicePaymentTermsPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreatePaymentTermsPresetAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Created($"invoice-payment-terms-presets/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoicePaymentTermsPresetDto>> Update(Guid id, [FromBody] InvoicePaymentTermsPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdatePaymentTermsPresetAsync(id, request, cancellationToken);
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
        var result = await service.DeletePaymentTermsPresetAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }
}
