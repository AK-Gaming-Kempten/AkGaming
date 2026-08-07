using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Domain;
using AkGaming.GamelyBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.GamelyBot.Application;

public sealed class NotificationDeliveryWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan AgendaDebounceWindow = TimeSpan.FromSeconds(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResetInterruptedNotificationsAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var didWork = await ProcessNextAsync(stoppingToken);
                if (!didWork)
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The Discord notification worker failed while polling.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ResetInterruptedNotificationsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GamelyBotDbContext>();
        var interrupted = await dbContext.Notifications
            .Where(item => item.Status == NotificationStatuses.Processing)
            .ToListAsync(cancellationToken);
        foreach (var notification in interrupted)
        {
            notification.Status = NotificationStatuses.Pending;
            notification.NextAttemptAtUtc = DateTimeOffset.UtcNow;
            notification.LastError = "Delivery was interrupted by a previous process shutdown.";
        }
        if (interrupted.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GamelyBotDbContext>();
        var now = DateTimeOffset.UtcNow;
        var candidates = await dbContext.Notifications
            .Include(item => item.Deliveries)
            .Where(item => item.Status == NotificationStatuses.Pending)
            .ToListAsync(cancellationToken);
        var readyCandidates = candidates
            .Where(item => item.NextAttemptAtUtc == null || item.NextAttemptAtUtc <= now)
            .OrderBy(item => item.ReceivedAtUtc)
            .ToList();
        var notification = readyCandidates
            .FirstOrDefault(item => !IsAgendaWaitingForMoreChanges(item, candidates, now));
        if (notification is null)
            return false;

        if (AgendaNotificationAggregator.TryGetMeetingId(notification, out var meetingId))
        {
            var agendaBatch = candidates
                .Where(item => HasSameAgendaContext(item, meetingId))
                .OrderBy(item => item.ReceivedAtUtc)
                .ToList();
            notification = agendaBatch[^1];
            notification.DataJson = AgendaNotificationAggregator.Serialize(
                AgendaNotificationAggregator.Aggregate(agendaBatch));
            foreach (var coalesced in agendaBatch.Where(item => item.Id != notification.Id))
            {
                coalesced.Status = NotificationStatuses.Skipped;
                coalesced.CompletedAtUtc = now;
                coalesced.NextAttemptAtUtc = null;
                coalesced.LastError = $"Coalesced into agenda notification {notification.EventId}.";
            }
        }

        notification.Status = NotificationStatuses.Processing;
        notification.AttemptCount++;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var renderer = scope.ServiceProvider.GetRequiredService<INotificationRenderer>();
            var transport = scope.ServiceProvider.GetRequiredService<INotificationTransport>();
            var linkResolver = scope.ServiceProvider.GetRequiredService<IDiscordLinkResolver>();
            var rendered = renderer.Render(notification);

            if (rendered.ChannelMessage is not null)
            {
                var delivery = GetOrCreateDelivery(dbContext, notification, DeliveryKinds.Channel,
                    rendered.ChannelMessage.ChannelId ?? "administration", rendered.ChannelMessage);
                if (delivery.Status != NotificationStatuses.Delivered)
                {
                    var previousMessageId = notification.Type switch
                    {
                        NotificationEventTypes.BoardAgendaChanged =>
                            await FindLatestAgendaMessageIdAsync(dbContext, notification, cancellationToken),
                        NotificationEventTypes.BoardMeetingAvailabilityChanged =>
                            await FindLatestMeetingMessageIdAsync(dbContext, notification, cancellationToken),
                        NotificationEventTypes.BoardMeetingRescheduleProposed =>
                            await FindLatestProposalMessageIdAsync(dbContext, notification, cancellationToken),
                        NotificationEventTypes.AllocationClaimChanged =>
                            await FindLatestAllocationClaimMessageIdAsync(dbContext, notification, cancellationToken),
                        _ => null
                    };
                    var result = string.IsNullOrWhiteSpace(previousMessageId)
                        ? await transport.SendChannelAsync(rendered.ChannelMessage, cancellationToken)
                        : await transport.UpdateChannelAsync(previousMessageId, rendered.ChannelMessage, cancellationToken);
                    ApplyResult(delivery, result);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    if (!result.IsSuccess && !result.IsPermanentFailure)
                        throw new TemporaryDeliveryException(result.Error ?? "Channel delivery failed temporarily.");
                }
            }

            if (rendered.DirectMessage is not null && notification.SubjectUserId.HasValue)
            {
                var delivery = GetOrCreateDelivery(dbContext, notification, DeliveryKinds.DirectMessage, notification.SubjectUserId.Value.ToString(), rendered.DirectMessage);
                if (delivery.Status != NotificationStatuses.Delivered && delivery.Status != NotificationStatuses.Skipped)
                {
                    var discordUserId = await linkResolver.ResolveDiscordUserIdAsync(notification.SubjectUserId.Value, cancellationToken);
                    if (string.IsNullOrWhiteSpace(discordUserId))
                    {
                        delivery.Status = NotificationStatuses.Skipped;
                        delivery.LastError = "The AK Gaming user has no linked Discord account.";
                    }
                    else
                    {
                        delivery.Target = discordUserId;
                        var result = await transport.SendDirectMessageAsync(discordUserId, rendered.DirectMessage, cancellationToken);
                        ApplyResult(delivery, result);
                        if (!result.IsSuccess && !result.IsPermanentFailure)
                            throw new TemporaryDeliveryException(result.Error ?? "Direct-message delivery failed temporarily.");
                    }
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            notification.Status = NotificationStatuses.Delivered;
            notification.CompletedAtUtc = DateTimeOffset.UtcNow;
            notification.NextAttemptAtUtc = null;
            notification.LastError = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (TemporaryDeliveryException exception)
        {
            ScheduleRetry(notification, exception.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            ScheduleRetry(notification, exception.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning(exception, "Notification {EventId} could not be delivered on attempt {AttemptCount}.", notification.EventId, notification.AttemptCount);
        }
        return true;
    }

    private static bool IsAgendaWaitingForMoreChanges(
        NotificationInboxItem notification,
        IReadOnlyCollection<NotificationInboxItem> candidates,
        DateTimeOffset now)
    {
        if (!AgendaNotificationAggregator.TryGetMeetingId(notification, out var meetingId))
        {
            return false;
        }

        var latestReceivedAt = candidates
            .Where(item => HasSameAgendaContext(item, meetingId))
            .Max(item => item.ReceivedAtUtc);
        return latestReceivedAt > now.Subtract(AgendaDebounceWindow);
    }

    private static bool HasSameAgendaContext(NotificationInboxItem notification, Guid? meetingId)
    {
        return AgendaNotificationAggregator.TryGetMeetingId(notification, out var candidateMeetingId)
            && candidateMeetingId == meetingId;
    }

    private static async Task<string?> FindLatestAgendaMessageIdAsync(
        GamelyBotDbContext dbContext,
        NotificationInboxItem current,
        CancellationToken cancellationToken)
    {
        AgendaNotificationAggregator.TryGetMeetingId(current, out var meetingId);
        var delivered = await dbContext.Notifications
            .AsNoTracking()
            .Include(item => item.Deliveries)
            .Where(item => item.Type == NotificationEventTypes.BoardAgendaChanged
                && item.Status == NotificationStatuses.Delivered
                && item.Id != current.Id)
            .ToListAsync(cancellationToken);

        return delivered
            .Where(item => HasSameAgendaContext(item, meetingId))
            .OrderByDescending(item => item.CompletedAtUtc)
            .SelectMany(item => item.Deliveries)
            .Where(delivery => delivery.Kind == DeliveryKinds.Channel
                && delivery.Status == NotificationStatuses.Delivered
                && !string.IsNullOrWhiteSpace(delivery.ExternalMessageId))
            .Select(delivery => delivery.ExternalMessageId)
            .FirstOrDefault();
    }

    private static async Task<string?> FindLatestMeetingMessageIdAsync(
        GamelyBotDbContext dbContext,
        NotificationInboxItem current,
        CancellationToken cancellationToken)
    {
        if (!TryGetMeetingId(current, out var meetingId))
            return null;

        var supportedTypes = new[]
        {
            NotificationEventTypes.BoardMeetingCreated,
            NotificationEventTypes.BoardMeetingRescheduled,
            NotificationEventTypes.BoardMeetingAvailabilityChanged
        };
        var delivered = await dbContext.Notifications
            .AsNoTracking()
            .Include(item => item.Deliveries)
            .Where(item => supportedTypes.Contains(item.Type)
                && item.Status == NotificationStatuses.Delivered
                && item.Id != current.Id)
            .ToListAsync(cancellationToken);

        return LatestChannelMessageId(delivered.Where(item =>
            TryGetMeetingId(item, out var candidateMeetingId) && candidateMeetingId == meetingId));
    }

    private static async Task<string?> FindLatestProposalMessageIdAsync(
        GamelyBotDbContext dbContext,
        NotificationInboxItem current,
        CancellationToken cancellationToken)
    {
        if (!TryGetProposalId(current, out var proposalId))
            return null;

        var delivered = await dbContext.Notifications
            .AsNoTracking()
            .Include(item => item.Deliveries)
            .Where(item => item.Type == NotificationEventTypes.BoardMeetingRescheduleProposed
                && item.Status == NotificationStatuses.Delivered
                && item.Id != current.Id)
            .ToListAsync(cancellationToken);

        return LatestChannelMessageId(delivered.Where(item =>
            TryGetProposalId(item, out var candidateProposalId) && candidateProposalId == proposalId));
    }

    private static async Task<string?> FindLatestAllocationClaimMessageIdAsync(
        GamelyBotDbContext dbContext,
        NotificationInboxItem current,
        CancellationToken cancellationToken)
    {
        if (!TryGetAllocationApplicationId(current, out var applicationId))
            return null;

        var delivered = await dbContext.Notifications
            .AsNoTracking()
            .Include(item => item.Deliveries)
            .Where(item => item.Type == NotificationEventTypes.AllocationClaimChanged
                && item.Status == NotificationStatuses.Delivered
                && item.Id != current.Id)
            .ToListAsync(cancellationToken);

        return LatestChannelMessageId(delivered.Where(item =>
            TryGetAllocationApplicationId(item, out var candidateId) && candidateId == applicationId));
    }

    private static string? LatestChannelMessageId(IEnumerable<NotificationInboxItem> notifications)
    {
        return notifications
            .OrderByDescending(item => item.CompletedAtUtc)
            .SelectMany(item => item.Deliveries)
            .Where(delivery => delivery.Kind == DeliveryKinds.Channel
                && delivery.Status == NotificationStatuses.Delivered
                && !string.IsNullOrWhiteSpace(delivery.ExternalMessageId))
            .Select(delivery => delivery.ExternalMessageId)
            .FirstOrDefault();
    }

    private static bool TryGetMeetingId(NotificationInboxItem notification, out Guid meetingId)
    {
        meetingId = Guid.Empty;
        try
        {
            var data = JsonSerializer.Deserialize<BoardMeetingNotification>(
                notification.DataJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (data is null)
                return false;
            meetingId = data.MeetingId;
            return meetingId != Guid.Empty;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetProposalId(NotificationInboxItem notification, out Guid proposalId)
    {
        proposalId = Guid.Empty;
        try
        {
            var data = JsonSerializer.Deserialize<BoardRescheduleProposalNotification>(
                notification.DataJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (data is null)
                return false;
            proposalId = data.ProposalId;
            return proposalId != Guid.Empty;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetAllocationApplicationId(NotificationInboxItem notification, out Guid applicationId)
    {
        applicationId = Guid.Empty;
        try
        {
            var data = JsonSerializer.Deserialize<AllocationClaimChangedNotification>(
                notification.DataJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (data is null)
                return false;
            applicationId = data.ApplicationId;
            return applicationId != Guid.Empty;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static NotificationDelivery GetOrCreateDelivery(GamelyBotDbContext dbContext, NotificationInboxItem notification, string kind, string target, RenderedMessage message)
    {
        var delivery = notification.Deliveries.SingleOrDefault(item => item.Kind == kind);
        if (delivery is not null)
            return delivery;
        delivery = new NotificationDelivery
        {
            NotificationInboxItemId = notification.Id,
            Kind = kind,
            Target = target,
            Title = message.Title,
            Body = message.Body,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        notification.Deliveries.Add(delivery);
        dbContext.Deliveries.Add(delivery);
        return delivery;
    }

    private static void ApplyResult(NotificationDelivery delivery, TransportResult result)
    {
        delivery.AttemptCount++;
        delivery.ExternalMessageId = result.ExternalMessageId;
        delivery.LastError = result.Error;
        delivery.Status = result.IsSuccess ? NotificationStatuses.Delivered : NotificationStatuses.Failed;
        delivery.DeliveredAtUtc = result.IsSuccess ? DateTimeOffset.UtcNow : null;
    }

    private static void ScheduleRetry(NotificationInboxItem notification, string error)
    {
        notification.LastError = error.Length <= 4000 ? error : error[..4000];
        if (notification.AttemptCount >= 8)
        {
            notification.Status = NotificationStatuses.Failed;
            notification.NextAttemptAtUtc = null;
            return;
        }
        notification.Status = NotificationStatuses.Pending;
        notification.NextAttemptAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, notification.AttemptCount)));
    }

    private sealed class TemporaryDeliveryException(string message) : Exception(message);
}
