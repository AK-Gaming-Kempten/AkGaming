using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AkGaming.Management.Modules.Disbursements.Infrastructure.Notifications;

public sealed class NotificationAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<DisbursementNotificationOptions> options)
{
    private readonly DisbursementNotificationOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _refreshAtUtc;

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!_options.UseAuthentication)
            return null;
        if (!string.IsNullOrWhiteSpace(_token) && _refreshAtUtc > DateTimeOffset.UtcNow)
            return _token;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_token) && _refreshAtUtc > DateTimeOffset.UtcNow)
                return _token;
            var client = httpClientFactory.CreateClient("DisbursementNotifications");
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = _options.Scope
            });
            using var response = await client.PostAsync(_options.TokenEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Identity returned an empty client-credentials response.");
            _token = token.AccessToken;
            _refreshAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, token.ExpiresIn - 60));
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
