using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;

[ApiController]
[Route("invoice-bank-account-presets")]
[Authorize(Policy = "management.invoices.manage")]
public sealed class InvoiceBankAccountPresetsController(IInvoiceManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceBankAccountPresetDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetBankAccountPresetsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceBankAccountPresetDto>> Create([FromBody] InvoiceBankAccountPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreateBankAccountPresetAsync(request, cancellationToken);
        return result.IsSuccess ? Created($"invoice-bank-account-presets/{result.Value!.Id}", result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceBankAccountPresetDto>> Update(Guid id, [FromBody] InvoiceBankAccountPresetDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateBankAccountPresetAsync(id, request, cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        return IsNotFound(result.Error) ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteBankAccountPresetAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    private static bool IsNotFound(string? error) => error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
}
