using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Application;

public sealed class BoardMeetingReminderWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DiscordInteractionOptions> options,
    ILogger<BoardMeetingReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableAutomaticReminders) return;
        var interval = TimeSpan.FromSeconds(Math.Max(10, options.Value.ReminderPollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        await CheckForReminderAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await CheckForReminderAsync(stoppingToken);
    }

    private async Task CheckForReminderAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var interactionService = scope.ServiceProvider.GetRequiredService<DiscordInteractionService>();
            await interactionService.QueueAutomaticReminderAsync(DateTimeOffset.UtcNow, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not check whether the next board meeting needs a reminder.");
        }
    }
}
