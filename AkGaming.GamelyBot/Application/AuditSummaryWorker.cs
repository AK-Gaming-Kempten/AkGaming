using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Application;

public sealed class AuditSummaryOptions
{
    public const string SectionName = "AuditSummaries";
    public bool EnableWeeklySummary { get; set; } = true;
    public string TimeZoneId { get; set; } = "Europe/Berlin";
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;
    public int Hour { get; set; } = 9;
    public int PollIntervalMinutes { get; set; } = 15;
}

public sealed class AuditSummaryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AuditSummaryOptions> options,
    ILogger<AuditSummaryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableWeeklySummary)
            return;

        await QueueLatestSummaryAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.Value.PollIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await QueueLatestSummaryAsync(stoppingToken);
    }

    private async Task QueueLatestSummaryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var window = GetLatestCompletedWindow(DateTimeOffset.UtcNow, options.Value);
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<AuditSummaryService>();
            await service.QueueWeeklySummariesAsync(window.FromUtc, window.ToUtc, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not queue the weekly audit summaries.");
        }
    }

    internal static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) GetLatestCompletedWindow(
        DateTimeOffset nowUtc,
        AuditSummaryOptions options)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var daysSinceScheduledDay = (7 + (int)localNow.DayOfWeek - (int)options.DayOfWeek) % 7;
        var localEnd = localNow.Date.AddDays(-daysSinceScheduledDay).AddHours(Math.Clamp(options.Hour, 0, 23));
        if (localEnd > localNow.DateTime)
            localEnd = localEnd.AddDays(-7);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified), timeZone);
        return (new DateTimeOffset(endUtc.AddDays(-7), TimeSpan.Zero), new DateTimeOffset(endUtc, TimeSpan.Zero));
    }
}
