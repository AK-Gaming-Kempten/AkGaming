using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using AkGaming.Management.Modules.InvoiceManagement.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.InvoiceManagement.Api.Controllers;

[ApiController]
[Route("invoices")]
[Authorize(Policy = "management.invoices.manage")]
public sealed class InvoicesController(IInvoiceManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetInvoicesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetInvoiceAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDetailsDto>> Create([FromBody] InvoiceDetailsDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreateInvoiceAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var invoice = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, invoice);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailsDto>> Update(Guid id, [FromBody] InvoiceDetailsDto request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateInvoiceAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            if (IsNotFound(result.Error))
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteInvoiceAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }

    [HttpGet("{id:guid}/html")]
    public async Task<IActionResult> RenderHtml(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.RenderHtmlAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        var content = Content(result.Value!, "text/html; charset=utf-8");
        return content;
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> RenderPdf(Guid id, CancellationToken cancellationToken)
    {
        var invoiceResult = await service.GetInvoiceAsync(id, cancellationToken);
        if (!invoiceResult.IsSuccess)
            return NotFound(invoiceResult.Error);

        var result = service.RenderPdf(invoiceResult.Value!);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var fileName = $"invoice-{invoiceResult.Value!.InvoiceNumber}.pdf";
        return File(result.Value!, "application/pdf", fileName);
    }

    [HttpPost("render-html")]
    public IActionResult RenderDraftHtml([FromBody] InvoiceDetailsDto request)
    {
        var result = service.RenderHtml(request);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var content = Content(result.Value!, "text/html; charset=utf-8");
        return content;
    }

    [HttpPost("render-pdf")]
    public IActionResult RenderDraftPdf([FromBody] InvoiceDetailsDto request)
    {
        var result = service.RenderPdf(request);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        var fileName = $"invoice-{request.InvoiceNumber}.pdf";
        return File(result.Value!, "application/pdf", fileName);
    }

    private static bool IsNotFound(string? error)
    {
        return error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
    }
}
