using System.Net.Http.Headers;
using System.Text;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Notifications;

public sealed class MemberNotificationOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<MemberNotificationOptions> options,
    ILogger<MemberNotificationOutboxDispatcher> logger) : BackgroundService
{
    private readonly MemberNotificationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            logger.LogWarning("The member notification outbox dispatcher is disabled because Notifications:Endpoint is not configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var didWork = await DispatchNextAsync(stoppingToken);
                if (!didWork)
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The member notification outbox dispatcher failed while polling.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<bool> DispatchNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MemberManagementDbContext>();
        var now = DateTimeOffset.UtcNow;
        var candidates = await dbContext.NotificationOutbox
            .Where(item => item.ProcessedAtUtc == null)
            .ToListAsync(cancellationToken);
        var message = candidates
            .Where(item => item.NextAttemptAtUtc == null || item.NextAttemptAtUtc <= now)
            .OrderBy(item => item.CreatedAtUtc)
            .FirstOrDefault();
        if (message is null)
            return false;

        message.AttemptCount++;
        try
        {
            var tokenProvider = scope.ServiceProvider.GetRequiredService<MemberNotificationAccessTokenProvider>();
            var token = await tokenProvider.GetTokenAsync(cancellationToken);
            var client = httpClientFactory.CreateClient("MemberNotifications");
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = new StringContent(message.PayloadJson, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"GamelyBot notification API returned {(int)response.StatusCode}: {responseBody}");
            message.ProcessedAtUtc = DateTimeOffset.UtcNow;
            message.NextAttemptAtUtc = null;
            message.LastError = null;
        }
        catch (Exception exception)
        {
            message.LastError = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
            message.NextAttemptAtUtc = DateTimeOffset.UtcNow.AddSeconds(
                Math.Min(300, Math.Pow(2, Math.Min(10, message.AttemptCount))));
            logger.LogWarning(exception, "Member outbox event {EventId} could not be submitted on attempt {AttemptCount}.",
                message.EventId, message.AttemptCount);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
