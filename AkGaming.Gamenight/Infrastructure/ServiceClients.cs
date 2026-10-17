using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using Microsoft.Extensions.Configuration;

namespace AkGaming.Gamenight.Infrastructure;

public sealed class ServiceTokenClient(IHttpClientFactory factory, IConfiguration config)
{
    private readonly Dictionary<string, string> _requestTokens = [];
    public async Task<string> GetAsync(string section, CancellationToken ct)
    {
        if (_requestTokens.TryGetValue(section, out var cached)) return cached;
        using var response = await factory.CreateClient().PostAsync(config[section + ":TokenEndpoint"],
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = config[section + ":ClientId"] ?? "",
                ["client_secret"] = config[section + ":ClientSecret"] ?? "",
                ["scope"] = config[section + ":Scope"] ?? ""
            }), ct);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var token = json.RootElement.GetProperty("access_token").GetString()!;
        _requestTokens[section] = token;
        return token;
    }
}
public sealed class ManagementMembershipClient(IHttpClientFactory factory, ServiceTokenClient tokens, IConfiguration config) : IMembershipClient
{
    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config["Management:BaseUrl"])) throw new HttpRequestException("Management is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(config["Management:BaseUrl"]!), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync("Management", ct));
        using var response = await factory.CreateClient().SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
    public Task<MembershipEligibility> GetAsync(string userId, int periodId, CancellationToken ct) =>
        GetAsync<MembershipEligibility>($"internal/gamenight-membership/{Uri.EscapeDataString(userId)}?periodId={periodId}", ct);
    public Task<List<PaymentPeriodOption>> PeriodsAsync(CancellationToken ct) =>
        GetAsync<List<PaymentPeriodOption>>("internal/gamenight-membership/periods", ct);
}
// Configured gateway for future modules; no notifications are emitted by signup.
public sealed class GamelyBotConnection(ServiceTokenClient tokens, IConfiguration config)
{
    public bool Enabled => config.GetValue<bool>("GamelyBot:Enabled");
    public string? Endpoint => config["GamelyBot:Endpoint"];
    public Task<string> AccessTokenAsync(CancellationToken ct) => tokens.GetAsync("GamelyBot", ct);
}
