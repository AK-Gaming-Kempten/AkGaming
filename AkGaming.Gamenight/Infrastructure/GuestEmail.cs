using System.Net;
using System.Net.Mail;
using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkGaming.Gamenight.Infrastructure;

public sealed class GuestEmail(GamenightDbContext db, IDataProtectionProvider protection, IConfiguration config) : IGuestEmail
{
    public void Queue(Registration registration, string email, string token)
    {
        var url = (config["App:PublicBaseUrl"] ?? "https://gamenight.akgaming.de").TrimEnd('/') + $"/registration/{registration.Id}?token={token}";
        var body = $"Danke für deine Anmeldung! Deine Anmeldung kannst du hier ansehen:\n{url}\n\nErstelle ein AK Gaming Konto mit dieser E-Mail-Adresse und bestätige deine E-Mail, um deine Anmeldung zu bearbeiten oder zu stornieren. Mit einem verknüpften Mitgliedsprofil prüfen wir deinen Mitgliedstarif automatisch. Nach dem Check-in sind Änderungen nur noch durch das Personal möglich.";
        db.Emails.Add(new EmailOutbox { Recipient = email, ProtectedBody = protection.CreateProtector("Gamenight.GuestEmail.v1").Protect(body), CreatedAt = DateTime.UtcNow, NextAttemptAt = DateTime.UtcNow });
    }
}
public sealed class EmailDispatcher(IServiceScopeFactory scopes, IConfiguration config, ILogger<EmailDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (config.GetValue<bool>("Smtp:Enabled"))
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<GamenightDbContext>();
                    var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("Gamenight.GuestEmail.v1");
                    var now = DateTime.UtcNow;
                    var messages = await db.Emails.Where(e => e.SentAt == null && e.NextAttemptAt <= now).OrderBy(e => e.CreatedAt).Take(20).ToListAsync(stoppingToken);
                    foreach (var item in messages)
                    {
                        try
                        {
                            using var smtp = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 587))
                            {
                                EnableSsl = config.GetValue("Smtp:UseSsl", true),
                                Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"])
                            };
                            using var message = new MailMessage(config["Smtp:FromEmail"]!, item.Recipient, "Deine Game-Night-Anmeldung", protector.Unprotect(item.ProtectedBody));
                            await smtp.SendMailAsync(message, stoppingToken);
                            item.SentAt = DateTime.UtcNow;
                            item.ProtectedBody = "";
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            item.Attempts++;
                            item.NextAttemptAt = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(item.Attempts, 6))));
                            logger.LogWarning("Guest email {MessageId} delivery failed; retry scheduled.", item.Id);
                        }
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError("Gamenight email dispatcher failed ({ErrorType}); retrying.", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
