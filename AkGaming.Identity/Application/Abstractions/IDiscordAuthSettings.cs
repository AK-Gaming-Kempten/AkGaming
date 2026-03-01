namespace AkGaming.Identity.Application.Abstractions;

public interface IDiscordAuthSettings
{
    bool AutoCreateUser { get; }
    bool RequireManualLinkForExistingEmail { get; }
}
