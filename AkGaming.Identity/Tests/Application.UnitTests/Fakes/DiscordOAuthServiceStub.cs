using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.ExternalAuth;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class DiscordOAuthServiceStub : IDiscordOAuthService
{
    public DiscordIdentity Identity { get; set; } = new("discord-user", "discord-name", "discord@example.com");

    public string BuildAuthorizationUrl(string state) => $"https://discord.test/authorize?state={state}";

    public Task<DiscordIdentity> GetIdentityFromAuthorizationCodeAsync(string code, CancellationToken cancellationToken)
    {
        return Task.FromResult(Identity);
    }
}
