using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class AuditSummaryService(
    IHttpClientFactory clients,
    ClientCredentialsTokenProvider tokens,
    IOptions<IdentityClientOptions> identityOptions,
    IOptions<ManagementClientOptions> managementOptions,
    INotificationInbox notificationInbox,
    BotText text)
{
    private readonly IdentityClientOptions _identity = identityOptions.Value;
    private readonly ManagementClientOptions _management = managementOptions.Value;

    public async Task<string> GetForDiscordAsync(
        string discordUserId,
        string source,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken)
            ?? throw new InvalidOperationException("The client-credentials token endpoint returned no access token.");
        var link = await GetLinkAsync(discordUserId, token, cancellationToken);
        if (link is null)
            return text["DiscordAccountNotLinked"];

        var isIdentity = string.Equals(source, "identity", StringComparison.OrdinalIgnoreCase);
        if (isIdentity && !link.CanReadIdentityAudit)
            return text["IdentityAuditUnauthorized"];
        if (!isIdentity && !link.CanReadManagementAudit)
            return text["ManagementAuditUnauthorized"];

        var summary = await GetSummaryAsync(source, fromUtc, toUtc, token, cancellationToken);
        return Format(summary, text);
    }

    public async Task QueueWeeklySummariesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken)
            ?? throw new InvalidOperationException("The client-credentials token endpoint returned no access token.");
        foreach (var source in new[] { "identity", "management" })
        {
            var summary = await GetSummaryAsync(source, fromUtc, toUtc, token, cancellationToken);
            var type = source == "identity"
                ? NotificationEventTypes.IdentityAuditSummary
                : NotificationEventTypes.ManagementAuditSummary;
            var data = new AuditSummaryNotification(summary);
            var envelope = new NotificationEnvelope(CreateEventId(source, fromUtc, toUtc), type,
                "gamelybot", toUtc, null, JsonSerializer.SerializeToElement(data));
            await notificationInbox.AcceptAsync(envelope, cancellationToken);
        }
    }

    internal static string Format(AuditSummaryResponse summary, BotText text)
    {
        var lines = new List<string>
        {
            text.Format("WeeklySummaryHeading", summary.Source),
            text.Format("AuditPeriod", summary.FromUtc.ToUnixTimeSeconds(), summary.ToUtc.ToUnixTimeSeconds())
        };
        if (summary.Sections is { Count: > 0 })
        {
            foreach (var section in summary.Sections)
            {
                lines.Add(string.Empty);
                lines.Add($"**{section.Name}**");
                lines.AddRange(section.Metrics.Select(metric => $"- {metric.Name}: **{metric.Count}**"));
            }
        }
        else
        {
            lines.Add(string.Empty);
            lines.Add(text["MostFrequentActivity"]);
            lines.AddRange(summary.TopCategories.Count == 0
                ? [text["NoAuditedActivity"]]
                : summary.TopCategories.Select(category => $"- {category.Name}: {category.Count}"));
        }
        var result = string.Join('\n', lines);
        return result.Length <= 1900 ? result : result[..1897] + "...";
    }

    private async Task<DiscordUserLinkResponse?> GetLinkAsync(
        string discordUserId,
        string token,
        CancellationToken cancellationToken)
    {
        var client = clients.CreateClient(nameof(AuditSummaryService));
        client.BaseAddress = new Uri(_identity.BaseUrl, UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/discord-links/by-discord/{discordUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiscordUserLinkResponse>(cancellationToken);
    }

    private async Task<AuditSummaryResponse> GetSummaryAsync(
        string source,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string token,
        CancellationToken cancellationToken)
    {
        var isIdentity = string.Equals(source, "identity", StringComparison.OrdinalIgnoreCase);
        var client = clients.CreateClient(nameof(AuditSummaryService));
        client.BaseAddress = new Uri(isIdentity ? _identity.BaseUrl : _management.BaseUrl, UriKind.Absolute);
        var query = $"internal/audit-summary?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}";
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuditSummaryResponse>(cancellationToken)
            ?? throw new InvalidOperationException($"{source} returned an empty audit summary.");
    }

    private static Guid CreateEventId(string source, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        var value = $"weekly-audit-summary:{source}:{fromUtc:O}:{toUtc:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
