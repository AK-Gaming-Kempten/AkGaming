using AkGaming.Identity.Application.ExternalAuth;

namespace AkGaming.Identity.Application.Abstractions;

public interface IDiscordOAuthService
{
    string BuildAuthorizationUrl(string state);
    Task<DiscordIdentity> GetIdentityFromAuthorizationCodeAsync(string code, CancellationToken cancellationToken);
}
