namespace AkGaming.Identity.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody, CancellationToken cancellationToken);
}
