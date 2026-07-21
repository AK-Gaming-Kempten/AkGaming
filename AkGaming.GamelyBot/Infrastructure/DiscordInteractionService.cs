using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Core.Notifications;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordInteractionOptions
{
    public const string SectionName = "DiscordInteractions";
    public bool ValidateSignatures { get; set; } = true;
}

public sealed class DiscordInteractionService(IHttpClientFactory clients, ClientCredentialsTokenProvider tokens,
    IOptions<IdentityClientOptions> identityOptions, IOptions<ManagementClientOptions> managementOptions)
{
    private readonly IdentityClientOptions _identity = identityOptions.Value;
    private readonly ManagementClientOptions _management = managementOptions.Value;

    public async Task<string> SetAvailabilityAsync(string discordUserId, Guid meetingId, int scheduleVersion, string status, CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken);
        var identityClient = clients.CreateClient(nameof(DiscordInteractionService));
        identityClient.BaseAddress = new Uri(_identity.BaseUrl, UriKind.Absolute);
        using var identityRequest = new HttpRequestMessage(HttpMethod.Get, $"internal/discord-links/by-discord/{discordUserId}");
        SetToken(identityRequest, token);
        using var identityResponse = await identityClient.SendAsync(identityRequest, cancellationToken);
        if (identityResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            return "Your Discord account is not linked to an AK Gaming account. Link it in your account settings, then try again.";
        if (!identityResponse.IsSuccessStatusCode)
            return "I could not look up your linked account right now. Please try again later.";
        var link = await identityResponse.Content.ReadFromJsonAsync<DiscordUserLinkResponse>(cancellationToken);
        if (link?.IsLinked != true) return "Your Discord account is not linked to an AK Gaming account.";
        if (!link.CanAccessBoardMeetings) return "Your linked account is not currently authorized as a board member.";

        var managementClient = clients.CreateClient(nameof(DiscordInteractionService));
        managementClient.BaseAddress = new Uri(_management.BaseUrl, UriKind.Absolute);
        using var managementRequest = new HttpRequestMessage(HttpMethod.Put, $"board-meetings/{meetingId}/availability/discord");
        SetToken(managementRequest, token);
        managementRequest.Content = JsonContent.Create(new { userId = link.UserId, displayName = link.DisplayName, status, scheduleVersion });
        using var managementResponse = await managementClient.SendAsync(managementRequest, cancellationToken);
        if (managementResponse.IsSuccessStatusCode)
            return status == "Available" ? "Recorded: you have time for this meeting." : "Recorded: you cannot attend this meeting.";
        var body = await managementResponse.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("rescheduled", StringComparison.OrdinalIgnoreCase)) return "This response belongs to an old meeting date. Please use the buttons on the latest notification.";
        return "I could not save your availability right now. Please use the management tool or try again later.";
    }

    private static void SetToken(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
