using System.Text;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

internal static class MemberLinkingEmailComposer {
    public static ComposedEmailMessage ComposeDecisionEmail(bool accepted) {
        var decisionText = accepted ? "angenommen" : "abgelehnt";
        var subject = accepted
            ? $"{ClubConstants.Organization.LegalName} | Kontoverknüpfung angenommen"
            : $"{ClubConstants.Organization.LegalName} | Kontoverknüpfung abgelehnt";
        var title = accepted ? "Kontoverknüpfung angenommen" : "Kontoverknüpfung abgelehnt";
        var introHtml =
            "<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">Hallo,</p>" +
            $"<p style=\"margin:0;\">deine Anfrage zur Kontoverknüpfung beim {AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)} wurde <strong>{AkGamingEmailTemplateComposer.H(decisionText)}</strong>.</p>";

        var text = new StringBuilder();
        text.AppendLine("Hallo,");
        text.AppendLine();
        text.AppendLine($"deine Anfrage zur Kontoverknüpfung beim {ClubConstants.Organization.LegalName} wurde {decisionText}.");
        text.AppendLine();
        if (accepted) {
            text.AppendLine($"Bitte aktualisiere jetzt deine persönlichen Daten unter {ClubConstants.Urls.ManagementMembership}.");
            text.AppendLine();
        }
        text.AppendLine($"Wenn du Fragen hast, kontaktiere uns gerne unter {ClubConstants.EmailAddresses.Board}.");
        text.AppendLine();
        text.AppendLine("Liebe Grüße");
        text.AppendLine(ClubConstants.Organization.LegalName);

        var bodyHtml = new StringBuilder();
        bodyHtml.Append("<p style=\"margin:0 0 12px;\">Deine Anfrage wurde bearbeitet.</p>");
        if (accepted) {
            bodyHtml.Append(AkGamingEmailTemplateComposer.BuildHighlightCard(
                "Nächster Schritt",
                $"Bitte <a href=\"{ClubConstants.Urls.ManagementMembership}\" style=\"color:#286c3f;\">aktualisiere deine persönlichen Daten</a>, jetzt da dein Account verknüpft wurde."));
        }
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard(
            "Fragen",
            $"<p style=\"margin:0;\">Wenn du Fragen hast, kontaktiere uns gerne unter <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a>.</p>"));

        var actions = accepted
            ? new[] { new AkGamingEmailAction("Zur Mitgliedschaft", ClubConstants.Urls.ManagementMembership) }
            : null;

        var htmlBody = AkGamingEmailTemplateComposer.ComposeHtml(
            ClubConstants.Organization.LegalName,
            title,
            introHtml,
            [new AkGamingEmailSummaryItem("Status", accepted ? "Angenommen" : "Abgelehnt")],
            actions,
            bodyHtml.ToString(),
            $"Liebe Grüße<br/><strong>{AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)}</strong>",
            $"<p style=\"margin:0;\"><strong>Kontakt:</strong> <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a></p>");

        return new ComposedEmailMessage(subject, text.ToString().TrimEnd(), htmlBody);
    }

    public static ComposedEmailMessage ComposeCreatedNotificationEmail(MemberLinkingRequest request) {
        var subject = $"{ClubConstants.Organization.LegalName} new member linking request";
        var introHtml =
            "<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">A new member linking request was created.</p>" +
            "<p style=\"margin:0;\">Review the request details below and open the admin panel to process it.</p>";

        var requestName = string.Join(" ", new[] { request.FirstName, request.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var text = new StringBuilder();
        text.AppendLine("A new member linking request was created.");
        text.AppendLine();
        text.AppendLine($"Open requests in admin panel: {ClubConstants.Urls.ManagementMemberRequests}");
        text.AppendLine();
        text.AppendLine($"RequestId: {request.Id}");
        text.AppendLine($"UserId: {request.IssuingUserId}");
        text.AppendLine($"Name: {requestName}");
        text.AppendLine($"Email: {request.Email}");
        text.AppendLine($"Discord: {request.DiscordUserName}");
        text.AppendLine($"Reason: {request.Reason}");

        var details = new List<AkGamingEmailSummaryItem> {
            new("RequestId", request.Id.ToString()),
            new("UserId", request.IssuingUserId.ToString()),
            new("Name", requestName),
            new("Email", request.Email ?? string.Empty),
            new("Discord", request.DiscordUserName ?? string.Empty),
            new("Reason", request.Reason.ToString())
        };

        var bodyHtml = new StringBuilder();
        bodyHtml.Append("<p style=\"margin:0 0 16px;\">The request below is ready for review in the member management admin panel.</p>");
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard("Request details", AkGamingEmailTemplateComposer.BuildDefinitionTable(details)));

        var htmlBody = AkGamingEmailTemplateComposer.ComposeHtml(
            ClubConstants.Organization.LegalName,
            "New Member Linking Request",
            introHtml,
            [
                new AkGamingEmailSummaryItem("Requester", requestName),
                new AkGamingEmailSummaryItem("Reason", request.Reason.ToString())
            ],
            [new AkGamingEmailAction("Open member requests", ClubConstants.Urls.ManagementMemberRequests)],
            bodyHtml.ToString(),
            $"Member management<br/><strong>{AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)}</strong>",
            $"<p style=\"margin:0;\"><strong>Admin:</strong> <a href=\"{ClubConstants.Urls.ManagementMemberRequests}\" style=\"color:#286c3f;\">Member requests</a></p>",
            includeAutomatedNotice: false);

        return new ComposedEmailMessage(subject, text.ToString().TrimEnd(), htmlBody);
    }
}
