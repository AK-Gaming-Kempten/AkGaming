using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class AppUrlSettingsStub : IAppUrlSettings
{
    public string PublicBaseUrl { get; set; } = "https://identity.akgaming.de";
}
