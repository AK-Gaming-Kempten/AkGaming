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
    Task<TransportResult> UpdateChannelAsync(string externalMessageId, RenderedMessage message, CancellationToken cancellationToken);
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

public interface IDiscordGuildCatalog
{
    Task<DiscordGuildCatalog> GetAsync(CancellationToken cancellationToken);
}

public sealed record DiscordGuildCatalog(
    IReadOnlyList<DiscordGuildChannel> Channels,
    IReadOnlyList<DiscordGuildRole> Roles);

public sealed record DiscordGuildChannel(string Id, string Name, int Type, int Position);
public sealed record DiscordGuildRole(string Id, string Name, int Position);
