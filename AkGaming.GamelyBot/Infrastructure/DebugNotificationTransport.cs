using AkGaming.GamelyBot.Application;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DebugNotificationTransport(ILogger<DebugNotificationTransport> logger) : INotificationTransport
{
    public Task<TransportResult> SendChannelAsync(RenderedMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("DEBUG Discord channel message: {Title} - {Body} - {Url} - role {RoleId}", message.Title, message.Body, message.Url, message.RoleId);
        return Task.FromResult(TransportResult.Success($"debug-{Guid.NewGuid():N}"));
    }

    public Task<TransportResult> SendDirectMessageAsync(string discordUserId, RenderedMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("DEBUG Discord DM to {DiscordUserId}: {Title} - {Body} - {Url}", discordUserId, message.Title, message.Body, message.Url);
        return Task.FromResult(TransportResult.Success($"debug-{Guid.NewGuid():N}"));
    }
}
