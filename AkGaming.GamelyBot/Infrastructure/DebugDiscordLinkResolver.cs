using AkGaming.GamelyBot.Application;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DebugDiscordLinkResolver(IOptions<IdentityClientOptions> options) : IDiscordLinkResolver
{
    private readonly IdentityClientOptions _options = options.Value;

    public Task<string?> ResolveDiscordUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_options.DebugDiscordUserId);
    }
}
