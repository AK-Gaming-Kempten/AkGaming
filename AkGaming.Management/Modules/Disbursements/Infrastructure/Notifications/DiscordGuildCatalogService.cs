using System.Net.Http.Headers;
using System.Net.Http.Json;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure.Notifications;

public sealed class DiscordGuildCatalogService(
    IHttpClientFactory httpClientFactory,
    NotificationAccessTokenProvider tokenProvider,
    IOptions<DisbursementNotificationOptions> options) : IDiscordGuildCatalogService
{
    private readonly DisbursementNotificationOptions _options = options.Value;

    public async Task<Result<DiscordGuildCatalogDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetCatalogEndpoint(out var endpoint))
            return Result<DiscordGuildCatalogDto>.Failure("The Discord catalog endpoint is not configured.");

        try
        {
            var token = await tokenProvider.GetTokenAsync(cancellationToken);
            var client = httpClientFactory.CreateClient("DisbursementNotifications");
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result<DiscordGuildCatalogDto>.Failure(
                    $"Discord catalog request failed with {(int)response.StatusCode}: {body}");
            }

            var catalog = await response.Content.ReadFromJsonAsync<DiscordGuildCatalogDto>(cancellationToken);
            return catalog is null
                ? Result<DiscordGuildCatalogDto>.Failure("Discord returned an empty catalog.")
                : Result<DiscordGuildCatalogDto>.Success(catalog);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            return Result<DiscordGuildCatalogDto>.Failure(exception.Message);
        }
    }

    private bool TryGetCatalogEndpoint(out Uri endpoint)
    {
        endpoint = null!;
        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var notificationEndpoint))
            return false;
        var apiBase = new Uri(notificationEndpoint, ".");
        endpoint = new Uri(apiBase, "discord/catalog");
        return true;
    }
}
