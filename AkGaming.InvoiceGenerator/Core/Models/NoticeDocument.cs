namespace AkGaming.InvoiceGenerator.Core.Models;

public sealed record NoticeDocument {
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public IReadOnlyList<string> RecipientAddressLines { get; set; } = [];
    public string Greeting { get; set; } = string.Empty;
    public string HeroText { get; set; } = string.Empty;
    public IReadOnlyList<NoticeSummaryRow> SummaryRows { get; set; } = [];
    public IReadOnlyList<string> IntroParagraphs { get; set; } = [];
    public IReadOnlyList<NoticeSection> Sections { get; set; } = [];
    public string HighlightTitle { get; set; } = string.Empty;
    public string HighlightText { get; set; } = string.Empty;
    public string Closing { get; set; } = string.Empty;
    public IReadOnlyList<NoticeLink> Links { get; set; } = [];
    public string AccentColor { get; set; } = "#286c3f";
    public string DarkColor { get; set; } = "#0f221e";
    public string MutedColor { get; set; } = "#61756d";
    public string LightColor { get; set; } = "#f7fbf8";
    public string BorderColor { get; set; } = "#d6e8da";
    public string SummaryBackgroundColor { get; set; } = "#28433a";
    public string SummaryBorderColor { get; set; } = "#49645b";
}

public sealed record NoticeSummaryRow(string Label, string Value);

public sealed class NoticeSection {
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<string> Paragraphs { get; set; } = [];
}

public sealed record NoticeLink(string Label, string Url);
