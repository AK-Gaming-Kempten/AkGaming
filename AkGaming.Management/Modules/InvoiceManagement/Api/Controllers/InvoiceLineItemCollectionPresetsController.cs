using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;

[ApiController]
[Route("invoice-line-item-collection-presets")]
[Authorize(Policy = "management.invoices.manage")]
public sealed class InvoiceLineItemCollectionPresetsController(IInvoiceManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceLineItemCollectionPresetDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetLineItemCollectionPresetsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceLineItemCollectionPresetDto>> Create([FromBody] InvoiceLineItemCollectionPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreateLineItemCollectionPresetAsync(request, cancellationToken);
        return result.IsSuccess ? Created($"invoice-line-item-collection-presets/{result.Value!.Id}", result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceLineItemCollectionPresetDto>> Update(Guid id, [FromBody] InvoiceLineItemCollectionPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateLineItemCollectionPresetAsync(id, request, cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        return IsNotFound(result.Error) ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteLineItemCollectionPresetAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    private static bool IsNotFound(string? error) => error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
}
