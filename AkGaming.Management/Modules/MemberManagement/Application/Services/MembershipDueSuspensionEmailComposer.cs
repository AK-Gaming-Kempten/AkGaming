using System.Globalization;
using System.Net;
using System.Text;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

internal static class MembershipDueSuspensionEmailComposer {
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");

    public static MembershipDueEmailPreviewDto Compose(
        Member member,
        MembershipPaymentPeriod paymentPeriod,
        MembershipDue due)
    {
        var firstName = string.IsNullOrWhiteSpace(member.FirstName)
            ? string.Empty
            : member.FirstName.Trim();
        var displayName = BuildDisplayName(member);
        var dueDateText = FormatDate(due.DueDate);
        var totalAmountText = FormatCurrency(due.DueAmount);
        var paidAmount = due.PaidAmount ?? 0m;
        var remainingAmount = Math.Max(due.DueAmount - paidAmount, 0m);
        var remainingAmountText = FormatCurrency(remainingAmount);
        var paidAmountText = FormatCurrency(paidAmount);

        var greetingText = string.IsNullOrWhiteSpace(firstName)
            ? "Hallo!"
            : $"Hi {firstName}!";
        var greetingHtml = string.IsNullOrWhiteSpace(firstName)
            ? "Hallo!"
            : $"Hi {H(firstName)}!";

        var subject = $"{ClubConstants.Organization.LegalName} | Suspendierung wegen offenem Mitgliedsbeitrag";

        var textBody = BuildTextBody(
            greetingText,
            paymentPeriod.Name,
            dueDateText,
            totalAmountText,
            paidAmount,
            paidAmountText,
            remainingAmountText);

        var htmlBody = BuildHtmlBody(
            greetingHtml,
            paymentPeriod.Name,
            dueDateText,
            totalAmountText,
            paidAmount,
            paidAmountText,
            remainingAmountText);

        return new MembershipDueEmailPreviewDto {
            RecipientEmail = member.Email?.Trim() ?? string.Empty,
            RecipientDisplayName = displayName,
            Subject = subject,
            TextBody = textBody,
            HtmlBody = htmlBody
        };
    }

    private static string BuildTextBody(
        string greeting,
        string paymentPeriodName,
        string dueDateText,
        string totalAmountText,
        decimal paidAmount,
        string paidAmountText,
        string remainingAmountText)
    {
        var text = new StringBuilder();
        text.AppendLine(greeting);
        text.AppendLine();
        text.AppendLine($"der Vorstand des {ClubConstants.Organization.LegalName} hat beschlossen, deine Mitgliedschaft vorübergehend zu suspendieren.");
        text.AppendLine();
        text.AppendLine("Grund der Suspendierung:");
        text.AppendLine($"Für den Zahlungszeitraum {paymentPeriodName} ist dein Mitgliedsbeitrag mit Fälligkeit zum {dueDateText} weiterhin nicht vollständig eingegangen.");
        text.AppendLine($"Gesamtbeitrag: {totalAmountText}");
        if (paidAmount > 0m)
            text.AppendLine($"Bereits verbucht: {paidAmountText}");
        text.AppendLine($"Aktuell offen: {remainingAmountText}");
        text.AppendLine();
        text.AppendLine("Satzungsgrundlage:");
        text.AppendLine("Nach §4.4 unserer Satzung sind vollständige Mitglieder verpflichtet, den in der Beitragsordnung festgelegten Mitgliedsbeitrag zu zahlen.");
        text.AppendLine("Nach §6.9 kann der Vorstand eine vorübergehende Suspendierung beschließen. Während der Suspendierung bist du nach §6.9 b) von deinen Rechten und Pflichten nach §4 entbunden.");
        text.AppendLine("Deine Rechte im Zusammenhang mit Mitgliederversammlungen bleiben nach §6.9 e) bestehen, insbesondere Einladung, Teilnahme und Stimmrecht.");
        text.AppendLine();
        text.AppendLine("So kann die Suspendierung beendet werden:");
        text.AppendLine($"1. Überweise den offenen Betrag von {remainingAmountText} an das Vereinskonto und sende uns bei Bedarf einen Zahlungsnachweis an {ClubConstants.EmailAddresses.Board}.");
        text.AppendLine($"2. Wenn du bereits gezahlt hast, melde dich bitte mit Zahlungsdatum und Verwendungszweck bei {ClubConstants.EmailAddresses.Board}.");
        text.AppendLine($"3. Wenn du den Beitrag aktuell nicht zahlen kannst, kontaktiere den Vorstand umgehend unter {ClubConstants.EmailAddresses.Board}, damit wir eine Beitragsermäßigung oder -befreiung prüfen können.");
        text.AppendLine();
        text.AppendLine(ClubConstants.Organization.LegalName);
        text.AppendLine($"IBAN: {ClubConstants.BankAccount.Iban}");
        text.AppendLine($"BIC: {ClubConstants.BankAccount.Bic}");
        text.AppendLine("Verwendungszweck: (Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)");
        text.AppendLine();
        text.AppendLine("Was als Nächstes passiert:");
        text.AppendLine("Die nächste Mitgliederversammlung stimmt nach §6.9 a) in Verbindung mit §6.5 über einen möglichen Ausschluss ab.");
        text.AppendLine("Lehnt die Mitgliederversammlung den Ausschluss ab, ist die Suspendierung aufgehoben. Der Vorstand kann die Suspendierung außerdem nach §6.9 f) selbst aufheben, sobald der Grund entfallen ist.");
        text.AppendLine("Nach §6.9 g) informieren wir die Vereinsmitglieder unverzüglich über Suspendierungen und deren Aufhebung unter Angabe der Gründe über die üblichen Kommunikationskanäle.");
        text.AppendLine();
        text.AppendLine("Liebe Grüße");
        text.AppendLine($"Vorstand {ClubConstants.Organization.LegalName}");
        text.AppendLine();
        text.AppendLine("Dieses Schreiben wurde maschinell erstellt und ist ohne Unterschrift gültig.");
        text.AppendLine();
        text.AppendLine("Weitere wichtige Links:");
        text.AppendLine($"Mitgliedsbeitrag: {ClubConstants.Urls.MembershipFees}");
        text.AppendLine($"Vereinssatzung: {ClubConstants.Urls.ArticlesOfAssociation}");
        text.AppendLine($"Beitragsordnung: {ClubConstants.Urls.MembershipFeeRegulations}");
        return text.ToString().TrimEnd();
    }

    private static string BuildHtmlBody(
        string greetingHtml,
        string paymentPeriodName,
        string dueDateText,
        string totalAmountText,
        decimal paidAmount,
        string paidAmountText,
        string remainingAmountText)
    {
        var html = new StringBuilder();

        html.Append("<div style=\"margin:0;padding:24px 0;background:#f7f5f2;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;line-height:1.6;\">");
        html.Append("<div style=\"max-width:700px;margin:0 auto;padding:0 16px;\">");
        html.Append("<div style=\"overflow:hidden;border-radius:20px;background:linear-gradient(145deg,#2c1613,#61251b);border:1px solid #f97316;box-shadow:0 20px 36px rgba(0,0,0,0.18);\">");
        html.Append("<div style=\"padding:28px 28px 24px;color:#ffffff;\">");
        html.Append("<div style=\"display:flex;align-items:center;gap:14px;\">");
        html.Append($"<img src=\"{ClubConstants.Urls.LogoAsset}\" alt=\"{H(ClubConstants.Organization.ShortName)} Logo\" width=\"56\" height=\"56\" style=\"display:block;width:56px;height:56px;border-radius:14px;background:rgba(255,255,255,0.12);padding:6px;\" />");
        html.Append("<div>");
        html.Append($"<p style=\"margin:0 0 6px;font-size:12px;font-weight:700;letter-spacing:0.14em;text-transform:uppercase;color:#fed7aa;\">{H(ClubConstants.Organization.LegalName)}</p>");
        html.Append("<h1 style=\"margin:0;font-size:30px;line-height:1.1;font-weight:700;\">Mitgliedschaft suspendiert</h1>");
        html.Append("</div>");
        html.Append("</div>");
        html.Append($"<p style=\"margin:20px 0 0;font-size:18px;font-weight:700;line-height:1.45;color:#ffffff;\">{greetingHtml}</p>");
        html.Append($"<p style=\"margin:10px 0 0;font-size:15px;color:#fff7ed;\">Der Vorstand hat beschlossen, deine Mitgliedschaft vorübergehend zu suspendieren.</p>");
        html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:rgba(255,255,255,0.12);border:1px solid rgba(255,255,255,0.18);\">");
        html.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;color:#ffffff;\">");
        AppendMetaRow(html, "Zahlungszeitraum", paymentPeriodName);
        AppendMetaRow(html, "Fällig bis", dueDateText);
        AppendMetaRow(html, "Gesamtbeitrag", totalAmountText);
        if (paidAmount > 0m)
            AppendMetaRow(html, "Bereits verbucht", paidAmountText);
        AppendMetaRow(html, "Aktuell offen", remainingAmountText);
        html.Append("</table>");
        html.Append("</div>");
        html.Append("</div>");
        html.Append("</div>");

        html.Append("<div style=\"margin-top:16px;padding:24px;background:#ffffff;border:1px solid #eadfd6;border-radius:18px;\">");
        html.Append("<p style=\"margin:0 0 14px;\">Grund ist, dass dein Mitgliedsbeitrag für den genannten Zahlungszeitraum trotz Fälligkeit weiterhin nicht vollständig eingegangen ist.</p>");
        html.Append("<p style=\"margin:0 0 14px;\">Nach §4.4 unserer Satzung sind vollständige Mitglieder verpflichtet, den in der Beitragsordnung festgelegten Beitrag zu zahlen. Nach §6.9 kann der Vorstand eine vorübergehende Suspendierung beschließen.</p>");
        html.Append("<p style=\"margin:0 0 18px;\">Während der Suspendierung bist du nach §6.9 b) von deinen Rechten und Pflichten nach §4 entbunden. Deine Rechte im Zusammenhang mit Mitgliederversammlungen bleiben nach §6.9 e) bestehen.</p>");

        AppendOptionCard(
            html,
            "Suspendierung beenden",
            $"Überweise den offenen Betrag von <strong>{H(remainingAmountText)}</strong> an das Vereinskonto oder melde dich mit Zahlungsdatum und Verwendungszweck bei <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#9a3412;\">{ClubConstants.EmailAddresses.Board}</a>, falls du bereits gezahlt hast. Wenn du den Beitrag aktuell nicht zahlen kannst, kontaktiere den Vorstand zur Prüfung einer Beitragsermäßigung oder -befreiung.");

        AppendOptionCard(
            html,
            "Vereinskonto",
            $"<div><strong>{H(ClubConstants.Organization.LegalName)}</strong></div><div>IBAN: <strong>{H(ClubConstants.BankAccount.Iban)}</strong></div><div>BIC: <strong>{H(ClubConstants.BankAccount.Bic)}</strong></div><div style=\"margin-top:8px;\">Verwendungszweck: <code style=\"font-family:'Courier New',Courier,monospace;background:#fff7ed;padding:2px 4px;border-radius:4px;\">(Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)</code></div>");

        html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:#fff7ed;border:1px solid #fed7aa;\">");
        html.Append("<p style=\"margin:0;font-weight:700;color:#9a3412;\">Nächste Schritte</p>");
        html.Append("<p style=\"margin:8px 0 0;color:#7c2d12;\">Die nächste Mitgliederversammlung stimmt nach §6.9 a) in Verbindung mit §6.5 über einen möglichen Ausschluss ab. Lehnt die Mitgliederversammlung den Ausschluss ab, ist die Suspendierung aufgehoben. Der Vorstand kann die Suspendierung nach §6.9 f) außerdem selbst aufheben, sobald der Grund entfallen ist.</p>");
        html.Append("</div>");
        html.Append($"<p style=\"margin:20px 0 0;\">Liebe Grüße<br/><strong>Vorstand {H(ClubConstants.Organization.LegalName)}</strong></p>");
        html.Append("</div>");

        html.Append("<div style=\"margin-top:14px;padding:16px 20px;font-size:12px;color:#61756d;\">");
        html.Append("<p style=\"margin:0 0 8px;\">Dieses Schreiben wurde maschinell erstellt und ist ohne Unterschrift gültig.</p>");
        html.Append("<p style=\"margin:0;\"><strong>Weitere wichtige Links:</strong> ");
        html.Append($"<a href=\"{ClubConstants.Urls.MembershipFees}\" style=\"color:#9a3412;\">Mitgliedsbeitrag</a> · ");
        html.Append($"<a href=\"{ClubConstants.Urls.ArticlesOfAssociation}\" style=\"color:#9a3412;\">Vereinssatzung</a> · ");
        html.Append($"<a href=\"{ClubConstants.Urls.MembershipFeeRegulations}\" style=\"color:#9a3412;\">Beitragsordnung</a>");
        html.Append("</p>");
        html.Append("</div>");
        html.Append("</div>");
        html.Append("</div>");

        return html.ToString();
    }

    private static void AppendMetaRow(StringBuilder html, string label, string value) {
        html.Append("<tr>");
        html.Append($"<td style=\"padding:4px 0;color:#fed7aa;\">{H(label)}</td>");
        html.Append($"<td style=\"padding:4px 0;text-align:right;font-weight:700;\">{H(value)}</td>");
        html.Append("</tr>");
    }

    private static void AppendOptionCard(StringBuilder html, string title, string bodyHtml) {
        html.Append("<div style=\"margin-top:12px;padding:18px 18px;border-radius:14px;background:#fbfdfb;border:1px solid #eadfd6;\">");
        html.Append($"<h2 style=\"margin:0 0 8px;font-size:18px;line-height:1.3;color:#2c1613;\">{H(title)}</h2>");
        html.Append($"<div style=\"margin:0;color:#1a1a1a;\">{bodyHtml}</div>");
        html.Append("</div>");
    }

    private static string BuildDisplayName(Member member) {
        var fullName = string.Join(" ", new[] { member.FirstName?.Trim(), member.LastName?.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return member.Email?.Trim() ?? string.Empty;
    }

    private static string FormatDate(DateOnly value) => value.ToString("dd.MM.yyyy", DeCulture);

    private static string FormatCurrency(decimal value) {
        var format = decimal.Truncate(value) == value ? "0" : "0.00";
        return $"{value.ToString(format, DeCulture)} €";
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
