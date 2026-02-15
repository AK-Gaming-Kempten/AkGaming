using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.ExternalAuth;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class DiscordStateServiceStub : IDiscordStateService
{
    public DiscordOAuthState State { get; set; } = new("login", null, DateTime.UtcNow.AddMinutes(5), "nonce");

    public string CreateState(DiscordOAuthState state) => "state-token";

    public DiscordOAuthState? ReadState(string protectedState)
    {
        return State;
    }
}
