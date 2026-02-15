using AkGaming.Identity.Application.ExternalAuth;

namespace AkGaming.Identity.Application.Abstractions;

public interface IDiscordStateService
{
    string CreateState(DiscordOAuthState state);
    DiscordOAuthState? ReadState(string protectedState);
}
