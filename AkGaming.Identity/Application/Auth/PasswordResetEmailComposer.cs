using System.Text;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;

namespace AkGaming.Identity.Application.Auth;

internal static class PasswordResetEmailComposer
{
    public static ComposedEmailMessage Compose(
        string email,
        string resetLink,
        string identityBaseUrl,
        string resetToken,
        int tokenLifetimeHours)
    {
        var identityDisplayName = $"{ClubConstants.Organization.ShortName} Identity";
        var subject = $"Reset your {identityDisplayName} password";

        var text = new StringBuilder();
        text.AppendLine("Hello,");
        text.AppendLine();
        text.AppendLine($"A password reset was requested for your {identityDisplayName} account.");
        text.AppendLine();
        text.AppendLine($"Reset your password: {resetLink}");
        text.AppendLine();
        text.AppendLine("If the button does not work, open the reset page and use this token:");
        text.AppendLine(resetToken);
        text.AppendLine();
        text.AppendLine($"Identity: {identityBaseUrl}");
        text.AppendLine($"This token expires in {tokenLifetimeHours} hour(s).");
        text.AppendLine();
        text.AppendLine("If you did not request this, you can ignore this email.");
        text.AppendLine();
        text.AppendLine($"Support: {ClubConstants.EmailAddresses.Identity}");
        text.AppendLine($"Privacy policy: {ClubConstants.Urls.PrivacyPolicy}");

        var introHtml =
            "<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;color:#ffffff;\">Hello,</p>" +
            $"<p style=\"margin:0;\">a password reset was requested for your <strong>{IdentityEmailTemplateComposer.H(identityDisplayName)}</strong> account.</p>";

        var heroNoteHtml =
            "<p style=\"margin:0;font-size:12px;font-weight:700;letter-spacing:0.14em;text-transform:uppercase;color:#c0e1c7;\">Manual reset token</p>" +
            $"<p style=\"margin:10px 0 0;font-size:26px;font-weight:700;letter-spacing:0.14em;font-family:'Courier New',Courier,monospace;color:#ffffff;\">{IdentityEmailTemplateComposer.H(resetToken)}</p>";

        var bodyHtml = new StringBuilder();
        bodyHtml.Append("<p style=\"margin:0 0 12px;\">Use the button above to set a new password.</p>");
        bodyHtml.Append(IdentityEmailTemplateComposer.BuildSectionCard(
            "If the button does not work",
            "<p style=\"margin:0 0 12px;\">Open this secure password reset link directly:</p>" +
            $"<p style=\"margin:0 0 12px;word-break:break-all;\"><a href=\"{IdentityEmailTemplateComposer.H(resetLink)}\" style=\"color:#286c3f;\">{IdentityEmailTemplateComposer.H(resetLink)}</a></p>" +
            $"<p style=\"margin:0;\">Alternatively, open <a href=\"{IdentityEmailTemplateComposer.H(identityBaseUrl)}/account/password-reset\" style=\"color:#286c3f;\">AK Gaming Identity password reset</a> and paste the token shown above.</p>"));
        bodyHtml.Append(IdentityEmailTemplateComposer.BuildHighlightCard(
            "Security notice",
            $"<p style=\"margin:0;\">This token expires in {tokenLifetimeHours} hour(s). If you did not request this email, you can ignore it.</p>"));

        var htmlBody = IdentityEmailTemplateComposer.ComposeHtml(
            identityDisplayName,
            "Reset Your Password",
            introHtml,
            [
                new IdentityEmailSummaryItem("Email", email),
                new IdentityEmailSummaryItem("Valid for", $"{tokenLifetimeHours} hour(s)")
            ],
            [new IdentityEmailAction("Reset Password", resetLink)],
            bodyHtml.ToString(),
            $"AK Gaming Identity<br/><strong>{IdentityEmailTemplateComposer.H(ClubConstants.Organization.LegalName)}</strong>",
            $"<p style=\"margin:0;\"><strong>Support:</strong> <a href=\"mailto:{ClubConstants.EmailAddresses.Identity}\" style=\"color:#286c3f;\">{ClubConstants.EmailAddresses.Identity}</a></p>" +
            $"<p style=\"margin:8px 0 0;\"><strong>Privacy:</strong> <a href=\"{ClubConstants.Urls.PrivacyPolicy}\" style=\"color:#286c3f;\">Privacy policy</a></p>",
            heroNoteHtml);

        return new ComposedEmailMessage(subject, text.ToString().TrimEnd(), htmlBody);
    }
}
