using System.Net;
using System.Text;
using AkGaming.Core.Constants;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

internal sealed record AkGamingEmailAction(string Label, string Url);
internal sealed record AkGamingEmailSummaryItem(string Label, string Value);

internal static class AkGamingEmailTemplateComposer {
    public static string ComposeHtml(
        string eyebrow,
        string title,
        string introHtml,
        IEnumerable<AkGamingEmailSummaryItem>? summaryItems,
        IEnumerable<AkGamingEmailAction>? actions,
        string bodyHtml,
        string signOffHtml,
        string? footerHtml = null,
        string? heroNoteHtml = null,
        bool includeAutomatedNotice = true)
    {
        var html = new StringBuilder();

        html.Append("<div style=\"margin:0;padding:24px 0;background:#f5f8f6;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;line-height:1.6;\">");
        html.Append("<div style=\"max-width:700px;margin:0 auto;padding:0 16px;\">");

        html.Append("<div style=\"overflow:hidden;border-radius:20px;background:linear-gradient(145deg,#0f221e,#163328);border:1px solid #48cb4f;box-shadow:0 20px 36px rgba(0,0,0,0.18);\">");
        html.Append("<div style=\"padding:28px 28px 24px;color:#ffffff;\">");
        html.Append("<div style=\"display:flex;align-items:center;gap:14px;\">");
        html.Append($"<img src=\"{ClubConstants.Urls.LogoAsset}\" alt=\"{H(ClubConstants.Organization.ShortName)} Logo\" width=\"56\" height=\"56\" style=\"display:block;width:56px;height:56px;border-radius:14px;background:rgba(255,255,255,0.12);padding:6px;\" />");
        html.Append("<div>");
        html.Append($"<p style=\"margin:0 0 6px;font-size:12px;font-weight:700;letter-spacing:0.14em;text-transform:uppercase;color:#c0e1c7;\">{H(eyebrow)}</p>");
        html.Append($"<h1 style=\"margin:0;font-size:30px;line-height:1.1;font-weight:700;\">{H(title)}</h1>");
        html.Append("</div>");
        html.Append("</div>");
        html.Append($"<div style=\"margin-top:18px;font-size:15px;color:#eef7f0;\">{introHtml}</div>");

        var summaryList = summaryItems?.ToList() ?? [];
        if (summaryList.Count > 0) {
            html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:rgba(255,255,255,0.12);border:1px solid rgba(255,255,255,0.18);\">");
            html.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;color:#ffffff;\">");
            foreach (var item in summaryList)
                AppendMetaRow(html, item.Label, item.Value);
            html.Append("</table>");
            html.Append("</div>");
        }

        var actionList = actions?.ToList() ?? [];
        if (actionList.Count > 0) {
            html.Append("<div style=\"margin-top:18px;display:flex;flex-wrap:wrap;gap:10px;\">");
            foreach (var action in actionList) {
                html.Append($"<a href=\"{action.Url}\" style=\"display:inline-block;padding:11px 18px;border-radius:999px;background:#286c3f;color:#ffffff;text-decoration:none;font-weight:700;\">{H(action.Label)}</a>");
            }
            html.Append("</div>");
        }

        if (!string.IsNullOrWhiteSpace(heroNoteHtml)) {
            html.Append("<div style=\"margin-top:16px;padding:14px 16px;border-radius:14px;background:rgba(255,255,255,0.10);border:1px solid rgba(255,255,255,0.16);\">");
            html.Append(heroNoteHtml);
            html.Append("</div>");
        }

        html.Append("</div>");
        html.Append("</div>");

        html.Append("<div style=\"margin-top:16px;padding:24px;background:#ffffff;border:1px solid #d6e8da;border-radius:18px;\">");
        html.Append(bodyHtml);
        html.Append($"<p style=\"margin:20px 0 0;\">{signOffHtml}</p>");
        html.Append("</div>");

        html.Append("<div style=\"margin-top:14px;padding:16px 20px;font-size:12px;color:#61756d;\">");
        if (includeAutomatedNotice)
            html.Append("<p style=\"margin:0 0 8px;\">Dieses Schreiben wurde maschinell erstellt und ist ohne Unterschrift gültig.</p>");
        if (!string.IsNullOrWhiteSpace(footerHtml))
            html.Append($"<div style=\"margin:0;\">{footerHtml}</div>");
        html.Append("</div>");

        html.Append("</div>");
        html.Append("</div>");

        return html.ToString();
    }

    public static string BuildSectionCard(string title, string bodyHtml) {
        var html = new StringBuilder();
        html.Append("<div style=\"margin-top:12px;padding:18px 18px;border-radius:14px;background:#fbfdfb;border:1px solid #d6e8da;\">");
        html.Append($"<h2 style=\"margin:0 0 8px;font-size:18px;line-height:1.3;color:#0f221e;\">{H(title)}</h2>");
        html.Append(bodyHtml);
        html.Append("</div>");
        return html.ToString();
    }

    public static string BuildHighlightCard(string title, string bodyHtml) {
        var html = new StringBuilder();
        html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:#f7fbf8;border:1px solid #c0e1c7;\">");
        html.Append($"<p style=\"margin:0;font-weight:700;color:#163328;\">{H(title)}</p>");
        html.Append($"<div style=\"margin-top:8px;color:#1a1a1a;\">{bodyHtml}</div>");
        html.Append("</div>");
        return html.ToString();
    }

    public static string BuildWarningCard(string title, string bodyHtml) {
        var html = new StringBuilder();
        html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:#fff7ed;border:1px solid #fed7aa;\">");
        html.Append($"<p style=\"margin:0;font-weight:700;color:#9a3412;\">{H(title)}</p>");
        html.Append($"<div style=\"margin-top:8px;color:#7c2d12;\">{bodyHtml}</div>");
        html.Append("</div>");
        return html.ToString();
    }

    public static string BuildDefinitionTable(IEnumerable<AkGamingEmailSummaryItem> items) {
        var html = new StringBuilder();
        html.Append("<table style=\"width:100%;border-collapse:collapse;\">");
        foreach (var item in items) {
            html.Append("<tr>");
            html.Append($"<td style=\"padding:6px 10px 6px 0;font-weight:700;vertical-align:top;color:#163328;\">{H(item.Label)}</td>");
            html.Append($"<td style=\"padding:6px 0;vertical-align:top;color:#1a1a1a;\">{H(item.Value)}</td>");
            html.Append("</tr>");
        }
        html.Append("</table>");
        return html.ToString();
    }

    private static void AppendMetaRow(StringBuilder html, string label, string value) {
        html.Append("<tr>");
        html.Append($"<td style=\"padding:4px 0;color:#c0e1c7;\">{H(label)}</td>");
        html.Append($"<td style=\"padding:4px 0;text-align:right;font-weight:700;\">{H(value)}</td>");
        html.Append("</tr>");
    }

    public static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
