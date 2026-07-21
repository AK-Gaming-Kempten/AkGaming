using System.Net.Http.Headers;
using System.Text;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.BoardManagement.Infrastructure.Notifications;

public sealed class BoardNotificationOutboxDispatcher(IServiceScopeFactory scopes, IHttpClientFactory clients, IOptions<BoardNotificationOptions> options, ILogger<BoardNotificationOutboxDispatcher> logger) : BackgroundService
{
    private readonly BoardNotificationOptions _options = options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint)) { logger.LogWarning("Board Discord notifications are disabled because Notifications:Endpoint is not configured."); return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try { if (!await DispatchNextAsync(stoppingToken)) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "The board notification outbox dispatcher failed."); await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
        }
    }

    private async Task<bool> DispatchNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BoardManagementDbContext>(); var now = DateTimeOffset.UtcNow;
        var candidates = await db.NotificationOutbox.Where(x => x.ProcessedAtUtc == null).ToListAsync(cancellationToken);
        var message = candidates.Where(x => x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now).OrderBy(x => x.CreatedAtUtc).FirstOrDefault();
        if (message is null) return false;
        message.Attempts++;
        try
        {
            var token = await scope.ServiceProvider.GetRequiredService<BoardNotificationAccessTokenProvider>().GetTokenAsync(cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint) { Content = new StringContent(message.PayloadJson, Encoding.UTF8, "application/json") };
            if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await clients.CreateClient("BoardNotifications").SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"GamelyBot returned {(int)response.StatusCode}: {body}");
            message.ProcessedAtUtc = DateTimeOffset.UtcNow; message.NextAttemptAtUtc = null; message.LastError = null;
        }
        catch (Exception exception)
        {
            message.LastError = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
            message.NextAttemptAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(10, message.Attempts))));
            logger.LogWarning(exception, "Board notification {EventId} failed on attempt {Attempt}.", message.EventId, message.Attempts);
        }
        await db.SaveChangesAsync(cancellationToken); return true;
    }
}
