using System.Globalization;
using System.Net;
using System.Text;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

internal static class MembershipDueReminderEmailComposer {
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
        var isRegularDue = due.DueAmount == paymentPeriod.DefaultDueAmount;

        var greetingText = string.IsNullOrWhiteSpace(firstName)
            ? "Hallo!"
            : $"Hi {firstName}!";
        var greetingHtml = string.IsNullOrWhiteSpace(firstName)
            ? "Hallo!"
            : $"Hi {H(firstName)}!";

        var explanationParagraph = isRegularDue
            ? $"Seit dem Beschluss unserer neuen Beitragsordnung zum 21.02.2026 sind von jedem Mitglied {totalAmountText} je Semester als Mitgliedsbeitrag zu entrichten."
            : $"Für diesen Zahlungszeitraum ist für dich aktuell ein Mitgliedsbeitrag von {totalAmountText} hinterlegt.";
        var paymentStatusText = paidAmount > 0m
            ? $"Aktuell sind bereits {paidAmountText} verbucht; offen sind noch {remainingAmountText}."
            : $"Aktuell ist der volle Betrag von {remainingAmountText} offen.";

        var subject = $"{ClubConstants.Organization.LegalName} | Offener Mitgliedsbeitrag {paymentPeriod.Name}";

        var textBody = BuildTextBody(
            greetingText,
            paymentPeriod.Name,
            dueDateText,
            explanationParagraph,
            paymentStatusText,
            remainingAmountText);

        var htmlBody = BuildHtmlBody(
            greetingHtml,
            paymentPeriod.Name,
            dueDateText,
            explanationParagraph,
            paymentStatusText,
            totalAmountText,
            paidAmount > 0m ? paidAmountText : null,
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
        string explanationParagraph,
        string paymentStatusText,
        string remainingAmountText)
    {
        var text = new StringBuilder();
        text.AppendLine(greeting);
        text.AppendLine();
        text.AppendLine($"leider konnten wir für den aktuellen Zahlungszeitraum ({paymentPeriodName} - Zahlung bis spätestens {dueDateText}) noch keinen vollständigen Eingang deines Mitgliedsbeitrags verbuchen.");
        text.AppendLine();
        text.AppendLine(explanationParagraph);
        text.AppendLine("Um unseren Vereinszweck zu unterstützen und aus Fairness allen anderen Mitgliedern gegenüber möchten wir auch dich bitten, dieser Pflicht nachzukommen.");
        text.AppendLine(paymentStatusText);
        text.AppendLine();
        text.AppendLine("Das sind jetzt deine Optionen:");
        text.AppendLine();
        text.AppendLine("1. MITGLIEDSBEITRAG BEZAHLEN");
        text.AppendLine($"Überweise schnellstmöglich den offenen Betrag von {remainingAmountText} an das unten genannte Konto.");
        text.AppendLine();
        text.AppendLine(ClubConstants.Organization.LegalName);
        text.AppendLine($"IBAN: {ClubConstants.BankAccount.Iban}");
        text.AppendLine($"BIC: {ClubConstants.BankAccount.Bic}");
        text.AppendLine("Verwendungszweck: (Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)");
        text.AppendLine();
        text.AppendLine("2. FÖRDERMITGLIED WERDEN");
        text.AppendLine($"Stelle einen formlosen Antrag per Mail an {ClubConstants.EmailAddresses.Board}, um Fördermitglied zu werden, und überweise den verringerten Beitrag für Fördermitglieder (frei wählbar zwischen 5 € und 15 €) an das oben genannte Konto.");
        text.AppendLine($"Alle Informationen zur Fördermitgliedschaft findest du unter {ClubConstants.Urls.MembershipFees}.");
        text.AppendLine();
        text.AppendLine("3. BEITRAGSERMÄSSIGUNG BZW. -BEFREIUNG BEANTRAGEN");
        text.AppendLine($"Solltest du dich aktuell in einer finanziell schwierigen Lage befinden, kannst du einen formlosen Antrag per Mail an {ClubConstants.EmailAddresses.Board} stellen, um eine Beitragsermäßigung oder -befreiung zu erhalten.");
        text.AppendLine("Überweise nach Genehmigung des Antrags den für dich festgelegten verringerten Beitrag an das oben genannte Konto.");
        text.AppendLine($"Alle Informationen zur Beitragsermäßigung und -befreiung findest du unter {ClubConstants.Urls.MembershipFees}.");
        text.AppendLine();
        text.AppendLine($"4. AUS DEM {ClubConstants.Organization.LegalName.ToUpperInvariant()} AUSTRETEN");
        text.AppendLine($"Auch wenn wir dich nur ungern als Mitglied verlieren, ist eine formlose Austrittserklärung per Mail an {ClubConstants.EmailAddresses.Board} für alle Beteiligten einfacher als ein Suspendierungsverfahren.");
        text.AppendLine();
        text.AppendLine("Sollten wir in den nächsten Tagen weder die Zahlung deines Beitrags verbuchen noch eine Kontaktaufnahme von dir erhalten, müssen wir nach §6.3 a) unserer Satzung deine Suspendierung beschließen, gefolgt von einer Abstimmung über deinen Ausschluss aus dem Verein in der nächsten Mitgliederversammlung.");
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
        string explanationParagraph,
        string paymentStatusText,
        string totalAmountText,
        string? paidAmountText,
        string remainingAmountText)
    {
        var html = new StringBuilder();

        html.Append("<div style=\"margin:0;padding:24px 0;background:#f5f8f6;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;line-height:1.6;\">");
        html.Append("<div style=\"max-width:700px;margin:0 auto;padding:0 16px;\">");

        html.Append("<div style=\"overflow:hidden;border-radius:20px;background:linear-gradient(145deg,#0f221e,#163328);border:1px solid #48cb4f;box-shadow:0 20px 36px rgba(0,0,0,0.18);\">");
        html.Append("<div style=\"padding:28px 28px 24px;color:#ffffff;\">");
        html.Append("<div style=\"display:flex;align-items:center;gap:14px;\">");
        html.Append($"<img src=\"{ClubConstants.Urls.LogoAsset}\" alt=\"{H(ClubConstants.Organization.ShortName)} Logo\" width=\"56\" height=\"56\" style=\"display:block;width:56px;height:56px;border-radius:14px;background:rgba(255,255,255,0.12);padding:6px;\" />");
        html.Append("<div>");
        html.Append($"<p style=\"margin:0 0 6px;font-size:12px;font-weight:700;letter-spacing:0.14em;text-transform:uppercase;color:#c0e1c7;\">{H(ClubConstants.Organization.LegalName)}</p>");
        html.Append("<h1 style=\"margin:0;font-size:30px;line-height:1.1;font-weight:700;\">Mitgliedsbeitrag offen</h1>");
        html.Append("</div>");
        html.Append("</div>");
        html.Append($"<p style=\"margin:20px 0 0;font-size:18px;font-weight:700;line-height:1.45;color:#ffffff;\">{greetingHtml}</p>");
        html.Append($"<p style=\"margin:10px 0 0;font-size:15px;color:#eef7f0;\">Leider konnten wir für den aktuellen Zahlungszeitraum ({H(paymentPeriodName)} - Zahlung bis spätestens {H(dueDateText)}) noch keinen vollständigen Eingang deines Mitgliedsbeitrags verbuchen.</p>");
        html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:rgba(255,255,255,0.12);border:1px solid rgba(255,255,255,0.18);\">");
        html.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;color:#ffffff;\">");
        AppendMetaRow(html, "Zahlungszeitraum", paymentPeriodName);
        AppendMetaRow(html, "Fällig bis", dueDateText);
        AppendMetaRow(html, "Gesamtbeitrag", totalAmountText);
        if (!string.IsNullOrWhiteSpace(paidAmountText))
            AppendMetaRow(html, "Bereits verbucht", paidAmountText);
        AppendMetaRow(html, "Aktuell offen", remainingAmountText);
        html.Append("</table>");
        html.Append("</div>");
        html.Append($"<div style=\"margin-top:18px;\"><a href=\"{ClubConstants.Urls.MembershipFees}\" style=\"display:inline-block;padding:11px 18px;border-radius:999px;background:#286c3f;color:#ffffff;text-decoration:none;font-weight:700;\">Zur Beitragsseite</a></div>");
        html.Append("</div>");
        html.Append("</div>");

        html.Append("<div style=\"margin-top:16px;padding:24px;background:#ffffff;border:1px solid #d6e8da;border-radius:18px;\">");
        html.Append($"<p style=\"margin:0 0 12px;\">{H(explanationParagraph)}</p>");
        html.Append("<p style=\"margin:0 0 18px;\">Um unseren Vereinszweck zu unterstützen und aus Fairness allen anderen Mitgliedern gegenüber möchten wir auch dich bitten, dieser Pflicht nachzukommen.</p>");
        html.Append($"<p style=\"margin:0 0 18px;font-weight:700;color:#286c3f;\">{H(paymentStatusText)}</p>");
        html.Append("<p style=\"margin:0 0 16px;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;font-size:12px;color:#61756d;\">Deine Optionen</p>");

        AppendOptionCard(
            html,
            "Mitgliedsbeitrag bezahlen",
            $"Überweise schnellstmöglich den offenen Betrag von <strong>{H(remainingAmountText)}</strong> an das unten genannte Konto." +
            "<div style=\"margin-top:12px;padding:14px 16px;border-radius:12px;background:#f7fbf8;border:1px solid #c0e1c7;\">" +
            $"<div><strong>{H(ClubConstants.Organization.LegalName)}</strong></div>" +
            $"<div>IBAN: <strong>{H(ClubConstants.BankAccount.Iban)}</strong></div>" +
            $"<div>BIC: <strong>{H(ClubConstants.BankAccount.Bic)}</strong></div>" +
            "<div style=\"margin-top:8px;\">Verwendungszweck: <code style=\"font-family:'Courier New',Courier,monospace;background:#ecf5ef;padding:2px 4px;border-radius:4px;\">(Nachname), (Vorname), Mitgliedsbeitrag WS/SS/WS+SS (Jahr)</code></div>" +
            "</div>");

        AppendOptionCard(
            html,
            "Fördermitglied werden",
            $"Stelle einen formlosen Antrag per Mail an <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a>, um Fördermitglied zu werden, und überweise den verringerten Beitrag für Fördermitglieder (frei wählbar zwischen 5 € und 15 €) an das oben genannte Konto. " +
            $"Alle Informationen findest du unter <a href=\"{ClubConstants.Urls.MembershipFees}\" style=\"color:#286c3f;\">akgaming.de/mitgliedschaft/mitgliedsbeitrag</a>.");

        AppendOptionCard(
            html,
            "Beitragsermäßigung bzw. -befreiung beantragen",
            $"Solltest du dich aktuell in einer finanziell schwierigen Lage befinden, kannst du einen formlosen Antrag per Mail an <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a> stellen, um eine Beitragsermäßigung oder -befreiung zu erhalten. " +
            $"Überweise nach Genehmigung des Antrags den für dich festgelegten verringerten Beitrag an das oben genannte Konto. Weitere Informationen findest du unter <a href=\"{ClubConstants.Urls.MembershipFees}\" style=\"color:#286c3f;\">akgaming.de/mitgliedschaft/mitgliedsbeitrag</a>.");

        AppendOptionCard(
            html,
            $"Aus dem {ClubConstants.Organization.LegalName} austreten",
            $"Auch wenn wir dich nur ungern als Mitglied verlieren, ist eine formlose Austrittserklärung per Mail an <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a> für alle Beteiligten einfacher als ein Suspendierungsverfahren.");

        html.Append("<div style=\"margin-top:18px;padding:16px 18px;border-radius:14px;background:#fff7ed;border:1px solid #fed7aa;\">");
        html.Append("<p style=\"margin:0;font-weight:700;color:#9a3412;\">Wichtiger Hinweis</p>");
        html.Append("<p style=\"margin:8px 0 0;color:#7c2d12;\">Sollten wir in den nächsten Tagen weder die Zahlung deines Beitrags verbuchen noch eine Kontaktaufnahme von dir erhalten, müssen wir nach §6.3 a) unserer Satzung deine Suspendierung beschließen, gefolgt von einer Abstimmung über deinen Ausschluss aus dem Verein in der nächsten Mitgliederversammlung.</p>");
        html.Append("</div>");

        html.Append($"<p style=\"margin:20px 0 0;\">Liebe Grüße<br/><strong>Vorstand {H(ClubConstants.Organization.LegalName)}</strong></p>");
        html.Append("</div>");

        html.Append("<div style=\"margin-top:14px;padding:16px 20px;font-size:12px;color:#61756d;\">");
        html.Append("<p style=\"margin:0 0 8px;\">Dieses Schreiben wurde maschinell erstellt und ist ohne Unterschrift gültig.</p>");
        html.Append("<p style=\"margin:0;\"><strong>Weitere wichtige Links:</strong> ");
        html.Append($"<a href=\"{ClubConstants.Urls.MembershipFees}\" style=\"color:#286c3f;\">Mitgliedsbeitrag</a> · ");
        html.Append($"<a href=\"{ClubConstants.Urls.ArticlesOfAssociation}\" style=\"color:#286c3f;\">Vereinssatzung</a> · ");
        html.Append($"<a href=\"{ClubConstants.Urls.MembershipFeeRegulations}\" style=\"color:#286c3f;\">Beitragsordnung</a>");
        html.Append("</p>");
        html.Append("</div>");

        html.Append("</div>");
        html.Append("</div>");

        return html.ToString();
    }

    private static void AppendMetaRow(StringBuilder html, string label, string value) {
        html.Append("<tr>");
        html.Append($"<td style=\"padding:4px 0;color:#c0e1c7;\">{H(label)}</td>");
        html.Append($"<td style=\"padding:4px 0;text-align:right;font-weight:700;\">{H(value)}</td>");
        html.Append("</tr>");
    }

    private static void AppendOptionCard(StringBuilder html, string title, string bodyHtml) {
        html.Append("<div style=\"margin-top:12px;padding:18px 18px;border-radius:14px;background:#fbfdfb;border:1px solid #d6e8da;\">");
        html.Append($"<h2 style=\"margin:0 0 8px;font-size:18px;line-height:1.3;color:#0f221e;\">{H(title)}</h2>");
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
