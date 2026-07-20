using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.GamelyBot.Infrastructure.Persistence;

public sealed class EfNotificationInbox(GamelyBotDbContext dbContext) : INotificationInbox
{
    public async Task<bool> AcceptAsync(NotificationEnvelope request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Notifications.AnyAsync(item => item.EventId == request.EventId, cancellationToken);
        if (existing)
            return true;

        dbContext.Notifications.Add(new NotificationInboxItem
        {
            EventId = request.EventId,
            Type = request.Type.Trim(),
            Source = request.Source.Trim(),
            OccurredAtUtc = request.OccurredAtUtc,
            SubjectUserId = request.SubjectUserId,
            DataJson = request.Data.GetRawText(),
            ReceivedAtUtc = DateTimeOffset.UtcNow
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await dbContext.Notifications.AnyAsync(item => item.EventId == request.EventId, cancellationToken))
                return true;
            throw;
        }
    }
}
