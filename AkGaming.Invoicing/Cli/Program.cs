using System.Text.Json;
using AkGaming.Invoicing.Core.Models;
using AkGaming.Invoicing.Core.Rendering;
using AkGaming.Invoicing.Core.Samples;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
};

var argsList = args.ToList();
if (argsList.Count == 0 || argsList.Contains("--help"))
{
    PrintHelp();
    return 1;
}

if (TryGetValue(argsList, "--sample", out var samplePath))
{
    var sample = InvoiceSampleFactory.CreateSponsoringSample();
    await WriteJsonAsync(samplePath, sample, jsonOptions);
    Console.WriteLine($"Sample invoice JSON written: {samplePath}");
    return 0;
}

if (!TryGetValue(argsList, "--input", out var inputPath))
{
    Console.Error.WriteLine("Missing required argument --input <path-to-json>.");
    PrintHelp();
    return 1;
}

var outputPath = TryGetValue(argsList, "--output", out var output) ? output : "invoice.pdf";

InvoiceDocument? invoice;
try
{
    await using var inputStream = File.OpenRead(inputPath);
    invoice = await JsonSerializer.DeserializeAsync<InvoiceDocument>(inputStream, jsonOptions);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to read invoice JSON: {ex.Message}");
    return 1;
}

if (invoice is null)
{
    Console.Error.WriteLine("Invoice JSON was empty or invalid.");
    return 1;
}

var outputDirectory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrWhiteSpace(outputDirectory))
    Directory.CreateDirectory(outputDirectory);

var extension = Path.GetExtension(outputPath).ToLowerInvariant();
if (extension == ".pdf")
{
    var pdfRenderer = new InvoicePdfRenderer();
    var pdfBytes = pdfRenderer.Render(invoice);
    await File.WriteAllBytesAsync(outputPath, pdfBytes);
    Console.WriteLine($"Invoice rendered to PDF: {outputPath}");
    return 0;
}

var htmlRenderer = new InvoiceHtmlRenderer();
var html = htmlRenderer.Render(invoice);
await File.WriteAllTextAsync(outputPath, html);
Console.WriteLine($"Invoice rendered to HTML: {outputPath}");
return 0;

static bool TryGetValue(IReadOnlyList<string> args, string key, out string value)
{
    value = string.Empty;
    var index = -1;
    for (var i = 0; i < args.Count; i++)
    {
        if (!string.Equals(args[i], key, StringComparison.Ordinal))
            continue;

        index = i;
        break;
    }

    if (index < 0 || index + 1 >= args.Count)
        return false;

    value = args[index + 1];
    return !string.IsNullOrWhiteSpace(value);
}

static async Task WriteJsonAsync(string path, InvoiceDocument invoice, JsonSerializerOptions jsonOptions)
{
    var outputDirectory = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(outputDirectory))
        Directory.CreateDirectory(outputDirectory);

    await using var outputStream = File.Create(path);
    await JsonSerializer.SerializeAsync(outputStream, invoice, jsonOptions);
}

static void PrintHelp()
{
    Console.WriteLine("AK Gaming Invoice Renderer CLI");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project AkGaming.Invoicing/Cli -- --sample <sample.json>");
    Console.WriteLine("  dotnet run --project AkGaming.Invoicing/Cli -- --input <invoice.json> [--output invoice.pdf|invoice.html]");
}
