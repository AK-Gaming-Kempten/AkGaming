using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Infrastructure.ExternalAuth;

public sealed class DiscordOptions : IDiscordAuthSettings
{
    public const string SectionName = "Discord";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = ["identify", "email"];

    public bool AutoCreateUser { get; set; } = true;
    public bool RequireManualLinkForExistingEmail { get; set; } = true;
}
