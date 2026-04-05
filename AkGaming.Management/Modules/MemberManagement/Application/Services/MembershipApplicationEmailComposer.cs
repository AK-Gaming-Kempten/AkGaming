using System.Globalization;
using System.Text;
using AkGaming.Core.Constants;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

internal static class MembershipApplicationEmailComposer {
    private static readonly CultureInfo DeCulture = CultureInfo.GetCultureInfo("de-DE");

    public static ComposedEmailMessage ComposeDecisionEmail(bool accepted) {
        var decisionText = accepted ? "accepted" : "declined";
        var subject = accepted
            ? $"{ClubConstants.Organization.LegalName} membership application accepted"
            : $"{ClubConstants.Organization.LegalName} membership application declined";
        var title = accepted ? "Membership Application Accepted" : "Membership Application Declined";
        var introHtml =
            "<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">Hello,</p>" +
            $"<p style=\"margin:0;\">Your {AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)} membership application has been <strong>{AkGamingEmailTemplateComposer.H(decisionText)}</strong>.</p>";

        var text = new StringBuilder();
        text.AppendLine("Hello,");
        text.AppendLine();
        text.AppendLine($"your {ClubConstants.Organization.LegalName} membership application has been {decisionText}.");
        text.AppendLine();
        text.AppendLine($"If you have questions, please contact us at {ClubConstants.EmailAddresses.Board}.");
        text.AppendLine();
        text.AppendLine($"Kind regards,");
        text.AppendLine(ClubConstants.Organization.LegalName);

        var bodyHtml = new StringBuilder();
        bodyHtml.Append("<p style=\"margin:0 0 12px;\">Thank you for your interest in joining our club.</p>");
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildHighlightCard(
            accepted ? "Next step" : "Questions",
            accepted
                ? $"If you have questions about the next steps, please contact us at <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a>."
                : $"If you have questions about this decision, please contact us at <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a>."));

        var htmlBody = AkGamingEmailTemplateComposer.ComposeHtml(
            ClubConstants.Organization.LegalName,
            title,
            introHtml,
            [new AkGamingEmailSummaryItem("Decision", accepted ? "Accepted" : "Declined")],
            accepted ? [new AkGamingEmailAction("Contact board", $"mailto:{ClubConstants.EmailAddresses.Board}")] : null,
            bodyHtml.ToString(),
            $"Kind regards,<br/><strong>{AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)}</strong>",
            $"<p style=\"margin:0;\"><strong>Contact:</strong> <a href=\"mailto:{ClubConstants.EmailAddresses.Board}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Board}</a></p>");

        return new ComposedEmailMessage(subject, text.ToString().TrimEnd(), htmlBody);
    }

    public static ComposedEmailMessage ComposeCreatedNotificationEmail(MembershipApplicationRequest request) {
        var subject = $"{ClubConstants.Organization.LegalName} new membership application";
        var introHtml =
            "<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">A new membership application was created.</p>" +
            "<p style=\"margin:0;\">Review the request details below and open the admin panel to process it.</p>";

        var requestName = string.Join(" ", new[] { request.FirstName, request.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var text = new StringBuilder();
        text.AppendLine("A new membership application was created.");
        text.AppendLine();
        text.AppendLine($"Open requests in admin panel: {ClubConstants.Urls.ManagementMemberRequests}");
        text.AppendLine();
        text.AppendLine($"RequestId: {request.Id}");
        text.AppendLine($"UserId: {request.IssuingUserId}");
        text.AppendLine($"Name: {requestName}");
        text.AppendLine($"Email: {request.Email}");
        text.AppendLine($"Phone: {request.Phone}");
        text.AppendLine($"Discord: {request.DiscordUserName}");
        text.AppendLine($"BirthDate: {FormatDate(request.BirthDate)}");
        text.AppendLine($"ApplicationText: {request.ApplicationText}");

        var details = new List<AkGamingEmailSummaryItem> {
            new("RequestId", request.Id.ToString()),
            new("UserId", request.IssuingUserId.ToString()),
            new("Name", requestName),
            new("Email", request.Email ?? string.Empty),
            new("Phone", request.Phone ?? string.Empty),
            new("Discord", request.DiscordUserName ?? string.Empty),
            new("BirthDate", FormatDate(request.BirthDate)),
            new("Application text", request.ApplicationText ?? string.Empty)
        };

        var bodyHtml = new StringBuilder();
        bodyHtml.Append("<p style=\"margin:0 0 16px;\">The request below is ready for review in the member management admin panel.</p>");
        bodyHtml.Append(AkGamingEmailTemplateComposer.BuildSectionCard("Request details", AkGamingEmailTemplateComposer.BuildDefinitionTable(details)));

        var htmlBody = AkGamingEmailTemplateComposer.ComposeHtml(
            ClubConstants.Organization.LegalName,
            "New Membership Application",
            introHtml,
            [
                new AkGamingEmailSummaryItem("Applicant", requestName),
                new AkGamingEmailSummaryItem("Request", request.Id.ToString())
            ],
            [new AkGamingEmailAction("Open member requests", ClubConstants.Urls.ManagementMemberRequests)],
            bodyHtml.ToString(),
            $"Member management<br/><strong>{AkGamingEmailTemplateComposer.H(ClubConstants.Organization.LegalName)}</strong>",
            $"<p style=\"margin:0;\"><strong>Admin:</strong> <a href=\"{ClubConstants.Urls.ManagementMemberRequests}\" style=\"color:#286c3f;\">Member requests</a></p>");

        return new ComposedEmailMessage(subject, text.ToString().TrimEnd(), htmlBody);
    }

    private static string FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", DeCulture) ?? string.Empty;
}
