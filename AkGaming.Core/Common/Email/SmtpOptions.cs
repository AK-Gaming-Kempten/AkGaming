using AkGaming.Core.Constants;

namespace AkGaming.Core.Common.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = ClubConstants.Organization.ShortName;
    public int TimeoutSeconds { get; set; } = 15;
}
