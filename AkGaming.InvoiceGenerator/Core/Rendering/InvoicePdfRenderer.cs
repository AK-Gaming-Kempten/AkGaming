using System.Globalization;
using AkGaming.InvoiceGenerator.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AkGaming.InvoiceGenerator.Core.Rendering;

public interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceDocument invoice);
}

public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly byte[] DefaultLogoBytes = CoreThemeAssetLoader.LoadBytesBySuffix("AKG_Logos.Default.png");

    static InvoicePdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(InvoiceDocument invoice)
    {
        var total = invoice.LineItems.Sum(item => item.TotalPrice);
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#1a1a1a"));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item()
                        .BorderBottom(1)
                        .BorderColor("#c0e1c7")
                        .PaddingBottom(4)
                        .Row(row =>
                        {
                            if (DefaultLogoBytes.Length > 0)
                                row.ConstantItem(32).Height(32).Image(DefaultLogoBytes).FitArea();

                            row.RelativeItem()
                                .PaddingLeft(DefaultLogoBytes.Length > 0 ? 8 : 0)
                                .AlignMiddle()
                                .Text(RenderPartySingleLine(invoice.Seller))
                                .FontColor("#286c3f")
                                .FontSize(10);
                        });

                    column.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(2);
                            left.Item().Text(invoice.Buyer.Name);
                            left.Item().Text(invoice.Buyer.Street);
                            left.Item().Text($"{invoice.Buyer.PostalCode} {invoice.Buyer.City}");
                            if (!string.IsNullOrWhiteSpace(invoice.Buyer.Country))
                                left.Item().Text(invoice.Buyer.Country);
                        });

                        row.ConstantItem(170).Column(meta =>
                        {
                            meta.Item().Border(1).BorderColor("#c0e1c7").Padding(6).Column(metaRows =>
                            {
                                metaRows.Spacing(4);
                                MetaRow(metaRows, "Rechnung Nr.", invoice.InvoiceNumber);
                                MetaRow(metaRows, "Rechnungsdatum", invoice.InvoiceDate.ToString("dd.MM.yyyy", DeCulture));
                                if (invoice.ServiceDate is not null)
                                    MetaRow(metaRows, "Leistungsdatum", invoice.ServiceDate.Value.ToString("dd.MM.yyyy", DeCulture));
                            });
                        });
                    });

                    column.Item().PaddingTop(8).Text($"RECHNUNG NR. {invoice.InvoiceNumber}")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor("#0f221e");

                    column.Item().Text(invoice.IntroText);
                    column.Item().Text(invoice.BodyText);

                    column.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(44);
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(74);
                            columns.ConstantColumn(52);
                            columns.ConstantColumn(84);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).AlignLeft().Text("Pos.").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(HeaderCellStyle).AlignLeft().Text("Beschreibung").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Einzelpreis").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Anzahl").FontColor(Colors.White).SemiBold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Gesamtpreis").FontColor(Colors.White).SemiBold();
                        });

                        for (var i = 0; i < invoice.LineItems.Count; i++)
                        {
                            var item = invoice.LineItems[i];
                            table.Cell().Element(BodyCellStyle).AlignLeft().Text($"{i + 1}.");
                            table.Cell().Element(BodyCellStyle).AlignLeft().Text(item.Description);
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(Currency(item.UnitPrice));
                            table.Cell().Element(BodyCellStyle).AlignCenter().Text(item.Quantity.ToString("0.##", DeCulture));
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(Currency(item.TotalPrice));
                        }
                    });

                    column.Item()
                        .AlignRight()
                        .Width(240)
                        .Border(1)
                        .BorderColor("#c0e1c7")
                        .Element(container => container
                            .Background("#286c3f")
                            .Padding(8))
                        .Row(row =>
                        {
                            row.RelativeItem().Text("Gesamtsumme").FontColor(Colors.White).SemiBold();
                            row.ConstantItem(110).AlignRight().Text(Currency(total)).FontColor(Colors.White).SemiBold();
                        });

                    if (!string.IsNullOrWhiteSpace(invoice.PaymentTerms) || invoice.BankDetails is not null)
                    {
                        column.Item().PaddingTop(6).BorderLeft(3).BorderColor("#286c3f").Background("#f7fbf8").Padding(8).Column(payment =>
                        {
                            payment.Spacing(2);
                            if (!string.IsNullOrWhiteSpace(invoice.PaymentTerms))
                                payment.Item().Text(invoice.PaymentTerms);

                            if (invoice.BankDetails is not null)
                            {
                                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.Iban))
                                    payment.Item().Text($"IBAN: {invoice.BankDetails.Iban}");
                                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.Blz))
                                    payment.Item().Text($"BLZ: {invoice.BankDetails.Blz}");
                                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.Bic))
                                    payment.Item().Text($"BIC: {invoice.BankDetails.Bic}");
                                if (!string.IsNullOrWhiteSpace(invoice.BankDetails.AccountHolder))
                                    payment.Item().Text($"Kontoinhaber: {invoice.BankDetails.AccountHolder}");
                            }
                        });
                    }

                    column.Item().PaddingTop(6).Text(invoice.ClosingText);
                    column.Item().PaddingTop(12).Text($"{invoice.Greeting},");
                    column.Item().Text(invoice.SignatureName);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Background("#0f221e")
            .Padding(8)
            .AlignMiddle();
    }

    private static IContainer BodyCellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor("#eeeeee")
            .PaddingVertical(6)
            .PaddingHorizontal(8);
    }

    private static void MetaRow(ColumnDescriptor metaRows, string label, string value)
    {
        metaRows.Item().Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(70).AlignRight().Text(value).SemiBold();
        });
    }

    private static string RenderPartySingleLine(InvoiceParty party)
        => string.Join(" - ", new[]
        {
            party.Name,
            party.Street,
            $"{party.PostalCode} {party.City}",
            party.Country
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string Currency(decimal value)
        => string.Format(DeCulture, "{0:N2} EUR", value);
}
