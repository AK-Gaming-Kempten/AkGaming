using AkGaming.InvoiceGenerator.Core.Models;
using AkGaming.InvoiceGenerator.Core.Rendering;
using AkGaming.InvoiceGenerator.Core.Samples;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IInvoiceHtmlRenderer, InvoiceHtmlRenderer>();
builder.Services.AddSingleton<IInvoicePdfRenderer, InvoicePdfRenderer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

app.MapGet("/api/invoices/sample", () => Results.Ok(InvoiceSampleFactory.CreateSponsoringSample()))
    .WithName("GetInvoiceSample")
    .WithDescription("Returns an invoice payload similar to the current sponsoring invoice.");

app.MapPost("/api/invoices/render-html", (InvoiceDocument invoice, IInvoiceHtmlRenderer renderer) =>
{
    if (invoice.LineItems.Count == 0)
        return Results.BadRequest(new { error = "At least one line item is required." });

    var html = renderer.Render(invoice);
    return Results.Content(html, "text/html; charset=utf-8");
})
.WithName("RenderInvoiceHtml")
.WithDescription("Renders invoice HTML using AK Gaming core theme colors.");

app.MapPost("/api/invoices/render-file", (InvoiceDocument invoice, IInvoiceHtmlRenderer renderer) =>
{
    if (invoice.LineItems.Count == 0)
        return Results.BadRequest(new { error = "At least one line item is required." });

    var html = renderer.Render(invoice);
    var fileName = $"invoice-{invoice.InvoiceNumber}.html";
    return Results.File(
        fileContents: System.Text.Encoding.UTF8.GetBytes(html),
        contentType: "text/html; charset=utf-8",
        fileDownloadName: fileName);
})
.WithName("RenderInvoiceFile")
.WithDescription("Returns a rendered invoice as downloadable HTML file.");

app.MapPost("/api/invoices/render-pdf", (InvoiceDocument invoice, IInvoicePdfRenderer renderer) =>
{
    if (invoice.LineItems.Count == 0)
        return Results.BadRequest(new { error = "At least one line item is required." });

    var pdfBytes = renderer.Render(invoice);
    var fileName = $"invoice-{invoice.InvoiceNumber}.pdf";
    return Results.File(
        fileContents: pdfBytes,
        contentType: "application/pdf",
        fileDownloadName: fileName);
})
.WithName("RenderInvoicePdf")
.WithDescription("Returns a natively rendered PDF invoice.");

app.Run();
