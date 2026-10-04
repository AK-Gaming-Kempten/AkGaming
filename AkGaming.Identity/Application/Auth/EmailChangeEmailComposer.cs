using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;

namespace AkGaming.Identity.Application.Auth;

internal static class EmailChangeEmailComposer
{
    internal static ComposedEmailMessage ComposeConfirmation(string email, string link)
    {
        var text = $"Confirm your new AK Gaming Identity email address: {link}\n\n" +
            "Open the link and press Confirm Email Change. The link expires in one hour. " +
            "Your current email remains active until confirmation. If you did not request this, ignore this email.";
        return Compose("Confirm your AK Gaming Identity email change", "Confirm Email Change", email,
            text, [new IdentityEmailAction("Review Email Change", link)]);
    }

    internal static ComposedEmailMessage ComposeNotification(string oldEmail, string newEmail, string text)
    {
        return Compose("Your AK Gaming Identity email changed", "Email Address Changed", oldEmail, text,
            [new IdentityEmailAction("Contact Support", $"mailto:{ClubConstants.EmailAddresses.Identity}")]);
    }

    private static ComposedEmailMessage Compose(string subject, string title, string email, string text, IdentityEmailAction[] actions)
    {
        var html = IdentityEmailTemplateComposer.ComposeHtml("AK Gaming Identity", title,
            "<p style=\"margin:0;\">Keep your account email up to date.</p>",
            [new IdentityEmailSummaryItem("Email", email)], actions,
            $"<p>{IdentityEmailTemplateComposer.H(text)}</p>", "AK Gaming Identity", "");
        return new ComposedEmailMessage(subject, text, html);
    }
}
