using System.Globalization;
using System.Net;
using System.Text;
using AkGaming.InvoiceGenerator.Core.Models;

namespace AkGaming.InvoiceGenerator.Core.Rendering;

public interface IInvoiceHtmlRenderer
{
    string Render(InvoiceDocument invoice);
}

public sealed class InvoiceHtmlRenderer : IInvoiceHtmlRenderer
{
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly string CoreThemeCss = ResolveThemeCss();
    private static readonly string DefaultLogoDataUri = ResolveDefaultLogoDataUri();

    public string Render(InvoiceDocument invoice)
    {
        var total = invoice.LineItems.Sum(item => item.TotalPrice);
        var html = new StringBuilder();

        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"de\" data-theme=\"light\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine($"  <title>Rechnung {H(invoice.InvoiceNumber)}</title>");
        html.AppendLine("  <style>");
        html.AppendLine(CoreThemeCss);
        html.AppendLine("    * { box-sizing: border-box; }");
        html.AppendLine("    body { margin: 0; font-family: Arial, Helvetica, sans-serif; color: var(--color-text-secondary); background: #f5f8f6; }");
        html.AppendLine("    .page { width: 210mm; min-height: 297mm; margin: 0 auto; padding: 18mm; background: var(--color-background-primary); }");
        html.AppendLine("    .sender { font-size: 12px; color: var(--color-highlight-secondary); border-bottom: 2px solid var(--color-light-green-grey); padding-bottom: 6px; margin-bottom: 24px; }");
        html.AppendLine("    .sender-bar { display: grid; grid-template-columns: auto 1fr; align-items: center; gap: 10px; }");
        html.AppendLine("    .sender-logo { width: 38px; height: 38px; object-fit: contain; }");
        html.AppendLine("    .address-grid { display: grid; grid-template-columns: 1fr auto; gap: 20px; margin-bottom: 20px; }");
        html.AppendLine("    .receiver { line-height: 1.5; font-size: 14px; }");
        html.AppendLine("    .meta { border: 1px solid var(--color-light-green-grey); border-radius: 6px; overflow: hidden; min-width: 260px; }");
        html.AppendLine("    .meta-row { display: grid; grid-template-columns: 1fr auto; padding: 8px 12px; font-size: 13px; }");
        html.AppendLine("    .meta-row:nth-child(odd) { background: #f7fbf8; }");
        html.AppendLine("    h1 { margin: 30px 0 20px; color: var(--color-background-tertiary); font-size: 30px; letter-spacing: 0.04em; }");
        html.AppendLine("    .lead { margin: 0 0 12px; }");
        html.AppendLine("    table { width: 100%; border-collapse: collapse; margin: 18px 0 16px; }");
        html.AppendLine("    thead th { background: var(--color-background-tertiary); color: var(--color-text-special); font-weight: 700; font-size: 13px; text-align: left; padding: 10px; }");
        html.AppendLine("    tbody td { border-bottom: 1px solid var(--color-light-gray); padding: 10px; font-size: 13px; vertical-align: top; }");
        html.AppendLine("    th.num, td.num { text-align: right; white-space: nowrap; }");
        html.AppendLine("    th.center, td.center { text-align: center; }");
        html.AppendLine("    .sum { margin-left: auto; width: 340px; border: 1px solid var(--color-light-green-grey); border-radius: 6px; overflow: hidden; }");
        html.AppendLine("    .sum-row { display: grid; grid-template-columns: 1fr auto; padding: 10px 14px; font-size: 15px; }");
        html.AppendLine("    .sum-total { background: var(--color-highlight-secondary); color: var(--color-text-special); font-weight: 700; font-size: 16px; }");
        html.AppendLine("    .payment { margin-top: 20px; background: #f7fbf8; border-left: 4px solid var(--color-highlight-secondary); padding: 10px 12px; font-size: 13px; line-height: 1.5; }");
        html.AppendLine("    .closing { margin-top: 24px; font-size: 14px; line-height: 1.5; }");
        html.AppendLine("    .signature { margin-top: 34px; font-size: 14px; line-height: 1.5; }");
        html.AppendLine("    @media print { body { background: #fff; } .page { margin: 0; width: 100%; min-height: unset; } }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <article class=\"page\">");
        html.AppendLine("    <div class=\"sender\">");
        html.AppendLine("      <div class=\"sender-bar\">");
        if (!string.IsNullOrWhiteSpace(DefaultLogoDataUri))
            html.AppendLine($"        <img class=\"sender-logo\" alt=\"AK Gaming Logo\" src=\"{DefaultLogoDataUri}\" />");
        html.AppendLine($"        <div>{RenderPartySingleLine(invoice.Seller)}</div>");
        html.AppendLine("      </div>");
        html.AppendLine("    </div>");
        html.AppendLine("    <section class=\"address-grid\">");
        html.AppendLine($"      <div class=\"receiver\">{RenderPartyBlock(invoice.Buyer)}</div>");
        html.AppendLine("      <div class=\"meta\">");
        html.AppendLine($"        <div class=\"meta-row\"><span>Rechnung Nr.</span><strong>{H(invoice.InvoiceNumber)}</strong></div>");
        html.AppendLine($"        <div class=\"meta-row\"><span>Rechnungsdatum</span><strong>{invoice.InvoiceDate.ToString("dd.MM.yyyy", DeCulture)}</strong></div>");

        if (invoice.ServiceDate is not null)
            html.AppendLine($"        <div class=\"meta-row\"><span>Leistungsdatum</span><strong>{invoice.ServiceDate.Value.ToString("dd.MM.yyyy", DeCulture)}</strong></div>");

        html.AppendLine("      </div>");
        html.AppendLine("    </section>");
        html.AppendLine($"    <h1>RECHNUNG NR. {H(invoice.InvoiceNumber)}</h1>");
        html.AppendLine($"    <p class=\"lead\">{H(invoice.IntroText)}</p>");
        html.AppendLine($"    <p class=\"lead\">{H(invoice.BodyText)}</p>");
        html.AppendLine("    <table>");
        html.AppendLine("      <thead>");
        html.AppendLine("        <tr>");
        html.AppendLine("          <th>Pos.</th>");
        html.AppendLine("          <th>Beschreibung</th>");
        html.AppendLine("          <th class=\"num\">Einzelpreis</th>");
        html.AppendLine("          <th class=\"center\">Anzahl</th>");
        html.AppendLine("          <th class=\"num\">Gesamtpreis</th>");
        html.AppendLine("        </tr>");
        html.AppendLine("      </thead>");
        html.AppendLine("      <tbody>");

        for (var index = 0; index < invoice.LineItems.Count; index++)
        {
            var item = invoice.LineItems[index];
            html.AppendLine("        <tr>");
            html.AppendLine($"          <td>{index + 1}.</td>");
            html.AppendLine($"          <td>{H(item.Description)}</td>");
            html.AppendLine($"          <td class=\"num\">{Currency(item.UnitPrice)}</td>");
            html.AppendLine($"          <td class=\"center\">{item.Quantity.ToString("0.##", DeCulture)}</td>");
            html.AppendLine($"          <td class=\"num\">{Currency(item.TotalPrice)}</td>");
            html.AppendLine("        </tr>");
        }

        html.AppendLine("      </tbody>");
        html.AppendLine("    </table>");
        html.AppendLine("    <section class=\"sum\">");
        html.AppendLine($"      <div class=\"sum-row sum-total\"><span>Gesamtsumme</span><span>{Currency(total)}</span></div>");
        html.AppendLine("    </section>");

        if (!string.IsNullOrWhiteSpace(invoice.PaymentTerms) || invoice.BankDetails is not null)
        {
            html.AppendLine("    <section class=\"payment\">");
            if (!string.IsNullOrWhiteSpace(invoice.PaymentTerms))
                html.AppendLine($"      <div>{H(invoice.PaymentTerms)}</div>");

            if (invoice.BankDetails is not null)
            {
                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.Iban))
                    html.AppendLine($"      <div>IBAN: {H(invoice.BankDetails.Iban)}</div>");
                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.Blz))
                    html.AppendLine($"      <div>BLZ: {H(invoice.BankDetails.Blz)}</div>");
                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.Bic))
                    html.AppendLine($"      <div>BIC: {H(invoice.BankDetails.Bic)}</div>");
                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.AccountHolder))
                    html.AppendLine($"      <div>Kontoinhaber: {H(invoice.BankDetails.AccountHolder)}</div>");
            }

            html.AppendLine("    </section>");
        }

        html.AppendLine($"    <p class=\"closing\">{H(invoice.ClosingText)}</p>");
        html.AppendLine("    <section class=\"signature\">");
        html.AppendLine($"      <div>{H(invoice.Greeting)},</div>");
        html.AppendLine($"      <div>{H(invoice.SignatureName)}</div>");
        html.AppendLine("    </section>");
        html.AppendLine("  </article>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string RenderPartySingleLine(InvoiceParty party)
        => string.Join(" - ", new[]
        {
            party.Name,
            party.Street,
            $"{party.PostalCode} {party.City}",
            party.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(H));

    private static string RenderPartyBlock(InvoiceParty party)
        => string.Join("<br/>", new[]
        {
            party.Name,
            party.Street,
            $"{party.PostalCode} {party.City}",
            party.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(H));

    private static string Currency(decimal value)
        => string.Format(DeCulture, "{0:N2} EUR", value);

    private static string H(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string ResolveThemeCss()
    {
        var css = CoreThemeCssLoader.Load();
        if (!string.IsNullOrWhiteSpace(css))
            return css;

        return """
               :root {
                   --color-light-gray: #eeeeee;
                   --color-light-green-grey: #c0e1c7;
                   --color-background-primary: #ffffff;
                   --color-background-tertiary: #0f221e;
                   --color-highlight-secondary: #286c3f;
                   --color-text-secondary: #1a1a1a;
                   --color-text-special: #ffffff;
               }
               """;
    }

    private static string ResolveDefaultLogoDataUri()
    {
        var bytes = CoreThemeAssetLoader.LoadBytesBySuffix("AKG_Logos.Default.png");
        if (bytes.Length == 0)
            return string.Empty;

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
