using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class DiscordAuthSettingsStub : IDiscordAuthSettings
{
    public bool AutoCreateUser { get; set; } = true;
    public bool RequireManualLinkForExistingEmail { get; set; } = true;
}
