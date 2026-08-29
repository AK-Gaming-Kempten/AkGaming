using System.Globalization;
using System.Text;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;

namespace AkGaming.Management.Modules.GeneralMeetings.Application.Services;

internal static class GeneralMeetingInvitationEmailComposer
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static ComposedEmailMessage Compose(GeneralMeeting meeting, bool isReminder, string? invitationText)
    {
        invitationText = ResolveInvitationText(meeting, isReminder, invitationText);
        var invitationLabel = isReminder ? "Erinnerung zur Mitgliederversammlung" : "Einladung zur Mitgliederversammlung";
        var subject = $"{ClubConstants.Organization.LegalName} | {(isReminder ? "Erinnerung" : "Einladung")}: {meeting.Title}";
        var scheduledAt = FormatScheduledAt(meeting.ScheduledAt);
        var location = string.IsNullOrWhiteSpace(meeting.Location) ? "Wird noch bekannt gegeben" : meeting.Location;

        var text = new StringBuilder();
        text.AppendLine("Liebe Mitglieder,");
        text.AppendLine();
        text.AppendLine(invitationText);
        text.AppendLine();
        text.AppendLine($"Datum: {scheduledAt}");
        text.AppendLine($"Ort: {location}");
        text.AppendLine();
        text.AppendLine("Wichtiger Hinweis");
        text.AppendLine($"Bitte stellt vor der Mitgliederversammlung sicher, dass ihr ein Konto im AK Gaming Management habt und eure Mitgliedschaft damit verknüpft ist: {ClubConstants.Urls.ManagementMembership}");
        text.AppendLine();
        text.AppendLine("Tagesordnung");
        AppendAgendaText(text, meeting);
        text.AppendLine();
        text.AppendLine("Anträge auf Ergänzung der Tagesordnung sind bis spätestens eine Woche vor der Versammlung einzureichen.");
        text.AppendLine();
        text.AppendLine("Wir freuen uns auf euer zahlreiches Erscheinen.");
        text.AppendLine();
        text.AppendLine("Mit freundlichen Grüßen");
        text.AppendLine($"Der Vorstand des {ClubConstants.Organization.LegalName}");

        var introHtml = $"<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">Liebe Mitglieder,</p><p style=\"margin:0;color:#eef7f0;\">{ToHtmlLines(invitationText)}</p>";

        var bodyHtml = new StringBuilder();
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildWarningCard(
            "Wichtiger Hinweis",
            $"<p style=\"margin:0;\">Bitte stellt vor der Mitgliederversammlung sicher, dass ihr ein Konto im AK Gaming Management habt und eure Mitgliedschaft damit verknüpft ist.</p><p style=\"margin:10px 0 0;\"><a href=\"{ClubConstants.Urls.ManagementMembership}\" style=\"color:#9a3412;font-weight:700;\">Konto und Mitgliedschaft prüfen</a></p>"));
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard("Tagesordnung", BuildAgendaHtml(meeting)));
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard(
            "Anmerkungen",
            "<p style=\"margin:0;\">Anträge auf Ergänzung der Tagesordnung sind bis spätestens eine Woche vor der Versammlung einzureichen.</p>"));
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

    public static string ResolveInvitationText(GeneralMeeting meeting, bool isReminder, string? invitationText)
    {
        if (!string.IsNullOrWhiteSpace(invitationText)) return invitationText.Trim();
        return isReminder
            ? $"hiermit erinnern wir euch an die bevorstehende Mitgliederversammlung „{meeting.Title}“ des {ClubConstants.Organization.LegalName}."
            : $"hiermit laden wir euch herzlich zur Mitgliederversammlung „{meeting.Title}“ des {ClubConstants.Organization.LegalName} ein.";
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
