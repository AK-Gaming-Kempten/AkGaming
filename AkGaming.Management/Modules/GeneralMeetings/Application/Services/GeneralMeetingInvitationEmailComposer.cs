using System.Globalization;
using System.Text;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;

namespace AkGaming.Management.Modules.GeneralMeetings.Application.Services;

internal static class GeneralMeetingInvitationEmailComposer
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static ComposedEmailMessage Compose(GeneralMeeting meeting, bool isReminder, string? additionalMessage)
    {
        var invitationLabel = isReminder ? "Erinnerung zur Vereinssitzung" : "Einladung zur Vereinssitzung";
        var subject = $"{ClubConstants.Organization.LegalName} | {(isReminder ? "Erinnerung" : "Einladung")}: {meeting.Title}";
        var scheduledAt = FormatScheduledAt(meeting.ScheduledAt);
        var location = string.IsNullOrWhiteSpace(meeting.Location) ? "Wird noch bekannt gegeben" : meeting.Location;

        var text = new StringBuilder();
        text.AppendLine("Liebe Mitglieder,");
        text.AppendLine();
        text.AppendLine(isReminder
            ? $"hiermit erinnern wir euch an die bevorstehende Vereinssitzung „{meeting.Title}“ des {ClubConstants.Organization.LegalName}."
            : $"hiermit laden wir euch herzlich zur Vereinssitzung „{meeting.Title}“ des {ClubConstants.Organization.LegalName} ein.");
        text.AppendLine();
        text.AppendLine($"Datum: {scheduledAt}");
        text.AppendLine($"Ort: {location}");
        AppendCustomText(text, additionalMessage);
        text.AppendLine();
        text.AppendLine("Tagesordnung");
        AppendAgendaText(text, meeting);
        text.AppendLine();
        text.AppendLine("Anträge auf Ergänzung der Tagesordnung richtet ihr bitte rechtzeitig an den Vorstand.");
        text.AppendLine();
        text.AppendLine("Wir freuen uns auf euer zahlreiches Erscheinen.");
        text.AppendLine();
        text.AppendLine("Mit freundlichen Grüßen");
        text.AppendLine($"Der Vorstand des {ClubConstants.Organization.LegalName}");

        var introHtml = isReminder
            ? $"<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">Liebe Mitglieder,</p><p style=\"margin:0;\">wir erinnern euch an die bevorstehende Vereinssitzung <strong>„{AkGamingEmailTemplateComposer.H(meeting.Title)}“</strong>.</p>"
            : $"<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">Liebe Mitglieder,</p><p style=\"margin:0;\">wir laden euch herzlich zur Vereinssitzung <strong>„{AkGamingEmailTemplateComposer.H(meeting.Title)}“</strong> ein.</p>";

        var bodyHtml = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(additionalMessage))
        {
            bodyHtml.Append(AkGamingEmailTemplateComposer.BuildHighlightCard(
                "Persönliche Nachricht",
                $"<p style=\"margin:0;\">{ToHtmlLines(additionalMessage)}</p>"));
        }

        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard("Tagesordnung", BuildAgendaHtml(meeting)));
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard(
            "Anmerkungen",
            "<p style=\"margin:0;\">Anträge auf Ergänzung der Tagesordnung richtet ihr bitte rechtzeitig an den Vorstand.</p>"));
        bodyHtml.Append("<p style=\"margin:18px 0 0;\">Wir freuen uns auf euer zahlreiches Erscheinen.</p>");

        var html = AkGamingEmailTemplateComposer.ComposeHtml(
            ClubConstants.Organization.LegalName,
            invitationLabel,
            introHtml,
            [
                new AkGamingEmailSummaryItem("Datum", scheduledAt),
                new AkGamingEmailSummaryItem("Ort", location)
            ],
            null,
            bodyHtml.ToString(),
            $"Mit freundlichen Grüßen<br/><strong>Der Vorstand des {AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)}</strong>",
            $"<p style=\"margin:0;\"><strong>Kontakt:</strong> <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a></p>");

        return new ComposedEmailMessage(subject, text.ToString().TrimEnd(), html);
    }

    private static string FormatScheduledAt(DateTimeOffset value)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Local;
        }

        var local = TimeZoneInfo.ConvertTime(value, timeZone);
        return local.ToString("dddd, dd. MMMM yyyy 'um' HH:mm 'Uhr'", GermanCulture);
    }

    private static void AppendCustomText(StringBuilder text, string? additionalMessage)
    {
        if (string.IsNullOrWhiteSpace(additionalMessage)) return;
        text.AppendLine();
        text.AppendLine(additionalMessage.Trim());
    }

    private static void AppendAgendaText(StringBuilder text, GeneralMeeting meeting)
    {
        var roots = meeting.AgendaItems.Where(item => !item.ParentId.HasValue).OrderBy(item => item.Order).ToList();
        if (roots.Count == 0)
        {
            text.AppendLine("Die Tagesordnung wird noch bekannt gegeben.");
            return;
        }

        for (var index = 0; index < roots.Count; index++)
            AppendAgendaTextItem(text, roots[index], meeting.AgendaItems, (index + 1).ToString(GermanCulture), 0);
    }

    private static void AppendAgendaTextItem(StringBuilder text, AgendaItem item, ICollection<AgendaItem> allItems, string number, int depth)
    {
        text.Append(' ', depth * 2).Append(number).Append(". ").AppendLine(item.Heading);
        var children = allItems.Where(candidate => candidate.ParentId == item.Id).OrderBy(candidate => candidate.Order).ToList();
        for (var index = 0; index < children.Count; index++)
            AppendAgendaTextItem(text, children[index], allItems, $"{number}.{index + 1}", depth + 1);
    }

    private static string BuildAgendaHtml(GeneralMeeting meeting)
    {
        var roots = meeting.AgendaItems.Where(item => !item.ParentId.HasValue).OrderBy(item => item.Order).ToList();
        if (roots.Count == 0) return "<p style=\"margin:0;\">Die Tagesordnung wird noch bekannt gegeben.</p>";

        var html = new StringBuilder();
        AppendAgendaHtmlList(html, roots, meeting.AgendaItems);
        return html.ToString();
    }

    private static void AppendAgendaHtmlList(StringBuilder html, IReadOnlyList<AgendaItem> items, ICollection<AgendaItem> allItems)
    {
        html.Append("<ol style=\"margin:0;padding-left:24px;\">");
        foreach (var item in items)
        {
            html.Append($"<li style=\"margin:7px 0;\"><strong>{AkGamingEmailTemplateComposer.H(item.Heading)}</strong>");
            if (!string.IsNullOrWhiteSpace(item.Description))
                html.Append($"<div style=\"margin-top:3px;color:#61756d;\">{ToHtmlLines(item.Description)}</div>");
            var children = allItems.Where(candidate => candidate.ParentId == item.Id).OrderBy(candidate => candidate.Order).ToList();
            if (children.Count > 0) AppendAgendaHtmlList(html, children, allItems);
            html.Append("</li>");
        }
        html.Append("</ol>");
    }

    private static string ToHtmlLines(string value)
    {
        return AkGamingEmailTemplateComposer.H(value.Trim())
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);
    }
}
