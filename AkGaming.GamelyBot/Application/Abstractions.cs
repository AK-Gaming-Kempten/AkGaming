using AkGaming.GamelyBot.Domain;
using AkGaming.Core.Notifications;

namespace AkGaming.GamelyBot.Application;

public interface INotificationRenderer
{
    RenderedNotification Render(NotificationInboxItem notification);
}

public interface INotificationTransport
{
    Task<TransportResult> SendChannelAsync(RenderedMessage message, CancellationToken cancellationToken);
    Task<TransportResult> SendDirectMessageAsync(string discordUserId, RenderedMessage message, CancellationToken cancellationToken);
}

public interface IDiscordLinkResolver
{
    Task<string?> ResolveDiscordUserIdAsync(Guid userId, CancellationToken cancellationToken);
}

public interface INotificationInbox
{
    Task<bool> AcceptAsync(NotificationEnvelope request, CancellationToken cancellationToken);
}
