using System.Net;
using System.Net.Mail;
using AkGaming.Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AkGaming.Identity.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SMTP is disabled. Skipping email send for {Recipient}.", toEmail);
            return;
        }

        ValidateOptions();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = textBody,
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(toEmail));

            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                Timeout = _options.TimeoutSeconds * 1000
            };

            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

            await client.SendMailAsync(message).WaitAsync(TimeSpan.FromSeconds(_options.TimeoutSeconds), cancellationToken);
        }
        catch (TimeoutException exception)
        {
            _logger.LogError(exception, "SMTP send timed out after {TimeoutSeconds}s. Host={Host}, Port={Port}, Recipient={Recipient}",
                _options.TimeoutSeconds, _options.Host, _options.Port, toEmail);
            throw new SmtpException($"SMTP send timed out after {_options.TimeoutSeconds} seconds.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "SMTP send failed. Host={Host}, Port={Port}, UseSsl={UseSsl}, Recipient={Recipient}, From={FromEmail}",
                _options.Host, _options.Port, _options.UseSsl, toEmail, _options.FromEmail);
            throw;
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("Smtp:Host must be configured when SMTP is enabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("Smtp:FromEmail must be configured when SMTP is enabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.Username))
        {
            throw new InvalidOperationException("Smtp:Username must be configured when SMTP is enabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("Smtp:Password must be configured when SMTP is enabled.");
        }

        if (_options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Smtp:TimeoutSeconds must be greater than zero.");
        }
    }
}
