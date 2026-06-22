using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;

[ApiController]
[Route("invoice-line-item-presets")]
[Authorize(Policy = "management.invoices.manage")]
public sealed class InvoiceLineItemPresetsController(IInvoiceManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceLineItemPresetDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetLineItemPresetsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceLineItemPresetDto>> Create([FromBody] InvoiceLineItemPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreateLineItemPresetAsync(request, cancellationToken);
        return result.IsSuccess ? Created($"invoice-line-item-presets/{result.Value!.Id}", result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceLineItemPresetDto>> Update(Guid id, [FromBody] InvoiceLineItemPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateLineItemPresetAsync(id, request, cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        return IsNotFound(result.Error) ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteLineItemPresetAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    private static bool IsNotFound(string? error) => error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
}
