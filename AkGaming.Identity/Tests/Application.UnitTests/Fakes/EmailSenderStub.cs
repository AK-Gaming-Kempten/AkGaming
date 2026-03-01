using AkGaming.Core.Common.Email;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class EmailSenderStub : IEmailSender
{
    public List<SentEmail> SentEmails { get; } = [];

    public Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody, CancellationToken cancellationToken)
    {
        SentEmails.Add(new SentEmail(toEmail, subject, textBody, htmlBody));
        return Task.CompletedTask;
    }
}

internal sealed record SentEmail(string ToEmail, string Subject, string TextBody, string? HtmlBody);
