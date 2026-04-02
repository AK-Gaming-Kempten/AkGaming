using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.ExternalAuth;

namespace AkGaming.Identity.Api.IntegrationTests;

internal sealed class DiscordOAuthServiceStub : IDiscordOAuthService
{
    public string BuildAuthorizationUrl(string state) => $"https://discord.test/authorize?state={Uri.EscapeDataString(state)}";

    public Task<DiscordIdentity> GetIdentityFromAuthorizationCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = string.Concat(code.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "discorduser";
        }

        return Task.FromResult(new DiscordIdentity(
            $"discord-{normalized}",
            $"Discord {normalized}",
            $"{normalized.ToLowerInvariant()}@example.com"));
    }
}
