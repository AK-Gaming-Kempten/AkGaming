using AkGaming.Core.Constants;
using AkGaming.InvoiceGenerator.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AkGaming.InvoiceGenerator.Core.Rendering;

public interface INoticePdfRenderer {
    byte[] Render(NoticeDocument notice);
}

public sealed class NoticePdfRenderer : INoticePdfRenderer {
    private static readonly byte[] DefaultLogoBytes = CoreThemeAssetLoader.LoadBytesBySuffix("AKG_Logos.Default.png");

    static NoticePdfRenderer() {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(NoticeDocument notice) {
        var document = Document.Create(container => {
            container.Page(page => {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(18, Unit.Millimetre);
                page.MarginVertical(14, Unit.Millimetre);
                page.DefaultTextStyle(style => style.FontSize(9.5f).FontColor("#1a1a1a").LineHeight(1.25f));

                page.Header().Element(header => ComposeLetterhead(header, notice));
                page.Content().PaddingTop(10).Column(column => ComposeContent(column, notice));
                page.Footer().Element(footer => ComposeFooter(footer, notice));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeLetterhead(IContainer container, NoticeDocument notice) {
        container.Column(column => {
            column.Item().Row(row => {
                if (DefaultLogoBytes.Length > 0)
                    row.ConstantItem(54).Height(54).Image(DefaultLogoBytes).FitArea();

                row.RelativeItem().PaddingLeft(DefaultLogoBytes.Length > 0 ? 10 : 0).Column(sender => {
                    sender.Item().Text(ClubConstants.Organization.LegalName).FontSize(15).SemiBold().FontColor(notice.AccentColor);
                    sender.Item().Text($"{ClubConstants.Address.Street} · {ClubConstants.Address.PostalCode} {ClubConstants.Address.CityWithRegion}").FontSize(8.5f);
                    sender.Item().Text($"{ClubConstants.EmailAddresses.Board} · {ClubConstants.Urls.Website}").FontSize(8.5f).FontColor(notice.MutedColor);
                    sender.Item().Text($"{ClubConstants.Organization.RegisterNumber} · {ClubConstants.Organization.RegisterCourt}").FontSize(8).FontColor(notice.MutedColor);
                });
            });

            column.Item().PaddingTop(6).BorderBottom(1).BorderColor(notice.BorderColor);
        });
    }

    private static void ComposeContent(ColumnDescriptor column, NoticeDocument notice) {
        column.Spacing(8);

        column.Item().Row(row => {
            row.RelativeItem().MinHeight(82).Column(address => {
                address.Item().Text($"{ClubConstants.Organization.LegalName} · {ClubConstants.Address.Street} · {ClubConstants.Address.PostalCode} {ClubConstants.Address.City}")
                    .FontSize(7).FontColor(notice.MutedColor).Underline();
                address.Item().PaddingTop(5).Text(notice.RecipientName).SemiBold();
                foreach (var line in notice.RecipientAddressLines)
                    address.Item().Text(line);
                if (notice.RecipientAddressLines.Count == 0 && !string.IsNullOrWhiteSpace(notice.RecipientEmail))
                    address.Item().Text(notice.RecipientEmail).FontColor(notice.MutedColor);
            });

            row.ConstantItem(150).AlignBottom().AlignRight().Column(meta => {
                meta.Item().AlignRight().Text($"Kempten, {DateTime.Now:dd.MM.yyyy}");
                meta.Item().PaddingTop(4).AlignRight().Text(notice.DocumentType).FontSize(8.5f).FontColor(notice.MutedColor);
            });
        });

        column.Item().Background(notice.DarkColor).Border(1).BorderColor(notice.AccentColor).Padding(14).Column(hero => {
            hero.Spacing(7);
            hero.Item().Text(ClubConstants.Organization.LegalName.ToUpperInvariant()).FontSize(8).SemiBold().LetterSpacing(0.12f).FontColor(notice.BorderColor);
            hero.Item().Text(notice.Title).FontSize(20).Bold().FontColor(Colors.White);
            hero.Item().Text(notice.Greeting).FontSize(12).SemiBold().FontColor(Colors.White);
            hero.Item().Text(notice.HeroText).FontColor(Colors.White);

            if (notice.SummaryRows.Count > 0) {
                hero.Item().PaddingTop(4).Background(notice.SummaryBackgroundColor).Border(1).BorderColor(notice.SummaryBorderColor).Padding(9).Column(summary => {
                    summary.Spacing(3);
                    foreach (var item in notice.SummaryRows) {
                        summary.Item().Row(summaryRow => {
                            summaryRow.RelativeItem().Text(item.Label).FontColor(notice.BorderColor);
                            summaryRow.ConstantItem(105).AlignRight().Text(item.Value).SemiBold().FontColor(Colors.White);
                        });
                    }
                });
            }
        });

        foreach (var paragraph in notice.IntroParagraphs)
            column.Item().Text(paragraph);

        foreach (var section in notice.Sections) {
            column.Item().Border(1).BorderColor(notice.BorderColor).Background(notice.LightColor).Padding(10).Column(card => {
                card.Spacing(4);
                card.Item().Text(section.Title).FontSize(12).SemiBold().FontColor(notice.DarkColor);
                foreach (var paragraph in section.Paragraphs)
                    card.Item().Text(paragraph);
            });
        }

        if (!string.IsNullOrWhiteSpace(notice.HighlightText)) {
            column.Item().Background("#fff7ed").Border(1).BorderColor("#fed7aa").Padding(10).Column(highlight => {
                highlight.Spacing(4);
                highlight.Item().Text(notice.HighlightTitle).SemiBold().FontColor("#9a3412");
                highlight.Item().Text(notice.HighlightText).FontColor("#7c2d12");
            });
        }

        column.Item().PaddingTop(4).Text(notice.Closing);
    }

    private static void ComposeFooter(IContainer container, NoticeDocument notice) {
        container.BorderTop(1).BorderColor(notice.BorderColor).PaddingTop(5).Row(row => {
            row.RelativeItem().Column(left => {
                left.Item().Text("Dieses Schreiben wurde maschinell erstellt und ist ohne Unterschrift gültig.").FontSize(7.5f).FontColor(notice.MutedColor);
                if (notice.Links.Count > 0)
                    left.Item().Text(string.Join(" · ", notice.Links.Select(link => $"{link.Label}: {link.Url}"))).FontSize(7).FontColor(notice.MutedColor);
            });
            row.ConstantItem(42).AlignRight().Text(text => {
                text.CurrentPageNumber().FontSize(7.5f).FontColor(notice.MutedColor);
                text.Span(" / ").FontSize(7.5f).FontColor(notice.MutedColor);
                text.TotalPages().FontSize(7.5f).FontColor(notice.MutedColor);
            });
        });
    }
}
