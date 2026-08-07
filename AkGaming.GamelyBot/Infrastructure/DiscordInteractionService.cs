using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordInteractionOptions
{
    public const string SectionName = "DiscordInteractions";
    public bool ValidateSignatures { get; set; } = true;
    public string TimeZoneId { get; set; } = "Europe/Berlin";
    public bool EnableAutomaticReminders { get; set; } = true;
    public int ReminderLeadTimeMinutes { get; set; } = 60;
    public int ReminderPollIntervalSeconds { get; set; } = 60;
}

public sealed class DiscordInteractionService(IHttpClientFactory clients, ClientCredentialsTokenProvider tokens,
    IOptions<IdentityClientOptions> identityOptions, IOptions<ManagementClientOptions> managementOptions,
    IOptions<DiscordInteractionOptions> interactionOptions, INotificationInbox notificationInbox,
    BotText text)
{
    private readonly IdentityClientOptions _identity = identityOptions.Value;
    private readonly ManagementClientOptions _management = managementOptions.Value;
    private readonly DiscordInteractionOptions _interactions = interactionOptions.Value;

    public string GetBoardMeetingHelp()
    {
        var pageUrl = GetBoardMeetingPageUrl();
        return text["BoardMeetingHelp"] + $"\n[{text["OpenBoardMeetingManagement"]}]({pageUrl})";
    }

    public string GetBoardMeetingCreateHelp()
    {
        return text.Format("BoardMeetingCreateHelp", GetBoardMeetingPageUrl());
    }

    public async Task<string> QueueNextMeetingReminderAsync(string discordUserId, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        var next = await GetNextMeetingResponseAsync(context.ManagementClient!, cancellationToken);
        if (next.Meeting is null) return next.Error;
        await QueueReminderAsync(next.Meeting, Guid.NewGuid(), cancellationToken);
        return text.Format("ReminderQueued", next.Meeting.Title);
    }

    public async Task QueueAutomaticReminderAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        if (!_interactions.EnableAutomaticReminders) return;
        var managementClient = await CreateManagementClientAsync(cancellationToken);
        var next = await GetNextMeetingResponseAsync(managementClient, cancellationToken);
        if (next.Meeting is null) return;
        var leadTime = TimeSpan.FromMinutes(Math.Max(1, _interactions.ReminderLeadTimeMinutes));
        if (next.Meeting.ScheduledAtUtc <= nowUtc || next.Meeting.ScheduledAtUtc > nowUtc.Add(leadTime)) return;
        var eventId = CreateAutomaticReminderEventId(next.Meeting.Id, next.Meeting.ScheduleVersion,
            _interactions.ReminderLeadTimeMinutes);
        await QueueReminderAsync(next.Meeting, eventId, cancellationToken);
    }

    public async Task<string> GetBacklogAsync(string discordUserId, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        using var response = await context.ManagementClient!.GetAsync("board-meetings/discord/backlog", cancellationToken);
        if (!response.IsSuccessStatusCode) return text["BacklogLoadFailed"];
        var items = await response.Content.ReadFromJsonAsync<List<BoardAgendaItemResponse>>(cancellationToken) ?? [];
        if (items.Count == 0) return text["BacklogEmpty"];
        return Limit(text.Format("BacklogList", string.Join('\n',
            items.Select((item, index) => $"{index + 1}. **{item.Title}**{Description(item.Description)}"))));
    }

    public async Task<string> GetNextMeetingAsync(string discordUserId, bool includeAgenda, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        var result = await GetNextMeetingResponseAsync(context.ManagementClient!, cancellationToken);
        if (result.Meeting is null) return result.Error;
        var meeting = result.Meeting;
        var timestamp = meeting.ScheduledAtUtc.ToUnixTimeSeconds();
        var location = string.IsNullOrWhiteSpace(meeting.Location) ? text["LocationPending"] : meeting.Location;
        var lines = new List<string>
        {
            $"**{meeting.Title}**",
            $"<t:{timestamp}:F> (<t:{timestamp}:R>)",
            text.Format("MeetingDurationLocation", meeting.DurationMinutes, location),
            text.Format("AvailabilityCounts", meeting.AvailableCount, meeting.UnavailableCount)
        };
        if (includeAgenda)
        {
            lines.Add(string.Empty);
            lines.Add(text["AgendaHeading"]);
            lines.AddRange(meeting.AgendaItems.Count == 0
                ? [text["NoAgendaItemsYet"]]
                : meeting.AgendaItems.OrderBy(item => item.Order).Select((item, index) => $"{index + 1}. **{item.Title}**{Description(item.Description)}"));
        }
        return Limit(string.Join('\n', lines));
    }

    public async Task<string> AddAgendaItemAsync(string discordUserId, string title, string? description, bool addToNextMeeting, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        var path = addToNextMeeting ? "board-meetings/discord/next/agenda" : "board-meetings/discord/backlog";
        using var response = await context.ManagementClient!.PostAsJsonAsync(path, new { userId = context.Link!.UserId, title, description }, cancellationToken);
        if (response.IsSuccessStatusCode)
            return addToNextMeeting ? text.Format("AgendaItemAdded", title) : text.Format("BacklogItemAdded", title);
        return await ErrorMessageAsync(response,
            addToNextMeeting ? text["AgendaItemAddFailed"] : text["BacklogItemAddFailed"], cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordCommandChoice>> GetBacklogChoicesAsync(string discordUserId, string query, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return [];
        using var response = await context.ManagementClient!.GetAsync("board-meetings/discord/backlog", cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        var items = await response.Content.ReadFromJsonAsync<List<BoardAgendaItemResponse>>(cancellationToken) ?? [];
        return items
            .Where(item => string.IsNullOrWhiteSpace(query) || item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(item => new DiscordCommandChoice(item.Title.Length > 100 ? item.Title[..100] : item.Title, item.Id.ToString()))
            .ToList();
    }

    public async Task<string> PromoteBacklogItemAsync(string discordUserId, Guid itemId, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        using var response = await context.ManagementClient!.PutAsJsonAsync($"board-meetings/discord/backlog/{itemId}/next", new { userId = context.Link!.UserId }, cancellationToken);
        if (response.IsSuccessStatusCode) return text["BacklogPromoted"];
        return await ErrorMessageAsync(response, text["BacklogPromoteFailed"], cancellationToken);
    }

    public async Task<string> SetNextMeetingAvailabilityAsync(string discordUserId, string status, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        var next = await GetNextMeetingResponseAsync(context.ManagementClient!, cancellationToken);
        if (next.Meeting is null) return next.Error;
        return await SetAvailabilityWithContextAsync(context, next.Meeting.Id, next.Meeting.ScheduleVersion, status, cancellationToken);
    }

    public async Task<string> SetAvailabilityAsync(string discordUserId, Guid meetingId, int scheduleVersion, string status, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        return await SetAvailabilityWithContextAsync(context, meetingId, scheduleVersion, status, cancellationToken);
    }

    public async Task<string> SetAllocationClaimDecisionAsync(
        string discordUserId,
        Guid applicationId,
        bool isApproved,
        CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken, requireBoardAccess: false);
        if (context.Error is not null) return context.Error;

        using var response = await context.ManagementClient!.PutAsJsonAsync(
            $"disbursements/discord/applications/{applicationId}/decision",
            new
            {
                userId = context.Link!.UserId,
                approverName = context.Link.DisplayName,
                isApproved
            },
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return isApproved
                ? text["AllocationClaimDecisionApproved"]
                : text["AllocationClaimDecisionObjected"];
        return await ErrorMessageAsync(response, text["AllocationClaimUpdateFailed"], cancellationToken);
    }

    public async Task<string> ProposeRescheduleAsync(string discordUserId, Guid meetingId, int scheduleVersion,
        DateTimeOffset proposedAtUtc, int durationMinutes, string? reason, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        using var response = await context.ManagementClient!.PostAsJsonAsync(
            $"board-meetings/{meetingId}/reschedule-proposals/discord",
            new
            {
                userId = context.Link!.UserId,
                displayName = context.Link.DisplayName,
                proposedAtUtc,
                durationMinutes,
                reason,
                scheduleVersion
            },
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return text.Format("RescheduleProposalSubmitted", proposedAtUtc.ToUnixTimeSeconds());
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("rescheduled", StringComparison.OrdinalIgnoreCase))
            return text["OldMeetingAnnouncement"];
        return text["RescheduleProposalFailed"];
    }

    public async Task<string> DecideRescheduleProposalAsync(string discordUserId, Guid meetingId, Guid proposalId,
        bool accept, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        if (!context.Link!.CanManageBoardMeetings)
            return text["ProposalDecisionUnauthorized"];
        using var response = await context.ManagementClient!.PostAsJsonAsync(
            $"board-meetings/{meetingId}/reschedule-proposals/{proposalId}/decision/discord",
            new { userId = context.Link.UserId, accept },
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return accept ? text["ProposalAccepted"] : text["ProposalRejected"];
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("already been decided", StringComparison.OrdinalIgnoreCase))
            return text["ProposalAlreadyDecided"];
        return text["ProposalUpdateFailed"];
    }

    private async Task<InteractionContext> CreateContextAsync(
        string discordUserId,
        CancellationToken cancellationToken,
        bool requireBoardAccess = true)
    {
        var token = await tokens.GetTokenAsync(cancellationToken);
        var identityClient = clients.CreateClient(nameof(DiscordInteractionService));
        identityClient.BaseAddress = new Uri(_identity.BaseUrl, UriKind.Absolute);
        using var identityRequest = new HttpRequestMessage(HttpMethod.Get, $"internal/discord-links/by-discord/{discordUserId}");
        SetToken(identityRequest, token);
        using var identityResponse = await identityClient.SendAsync(identityRequest, cancellationToken);
        if (identityResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            return InteractionContext.Failure(text["DiscordAccountNotLinkedWithHelp"]);
        if (!identityResponse.IsSuccessStatusCode)
            return InteractionContext.Failure(text["LinkedAccountLookupFailed"]);
        var link = await identityResponse.Content.ReadFromJsonAsync<DiscordUserLinkResponse>(cancellationToken);
        if (link?.IsLinked != true) return InteractionContext.Failure(text["DiscordAccountNotLinked"]);
        if (requireBoardAccess && !link.CanAccessBoardMeetings)
            return InteractionContext.Failure(text["BoardMemberUnauthorized"]);

        var managementClient = clients.CreateClient(nameof(DiscordInteractionService));
        managementClient.BaseAddress = new Uri(_management.BaseUrl, UriKind.Absolute);
        managementClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return InteractionContext.Success(link, managementClient);
    }

    private async Task<HttpClient> CreateManagementClientAsync(CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken);
        var managementClient = clients.CreateClient(nameof(DiscordInteractionService));
        managementClient.BaseAddress = new Uri(_management.BaseUrl, UriKind.Absolute);
        managementClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return managementClient;
    }

    private async Task QueueReminderAsync(BoardMeetingResponse meeting, Guid eventId, CancellationToken cancellationToken)
    {
        var data = new BoardMeetingNotification(
            meeting.Id,
            meeting.Title,
            meeting.ScheduledAtUtc,
            meeting.DurationMinutes,
            meeting.Location,
            meeting.ScheduleVersion,
            null,
            _management.GetBoardMeetingFrontendUrl(meeting.Id),
            meeting.AgendaItems.OrderBy(item => item.Order).Select(item => item.Title).ToList(),
            meeting.Availabilities
                .Where(item => string.Equals(item.Status, "Available", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayName)
                .Select(item => item.DisplayName)
                .ToList(),
            meeting.Availabilities
                .Where(item => string.Equals(item.Status, "Unavailable", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayName)
                .Select(item => item.DisplayName)
                .ToList());
        var envelope = new NotificationEnvelope(eventId, NotificationEventTypes.BoardMeetingReminder,
            "gamelybot", DateTimeOffset.UtcNow, null, JsonSerializer.SerializeToElement(data));
        await notificationInbox.AcceptAsync(envelope, cancellationToken);
    }

    internal static Guid CreateAutomaticReminderEventId(Guid meetingId, int scheduleVersion, int leadTimeMinutes)
    {
        var value = $"board-meeting-reminder:{meetingId:D}:{scheduleVersion}:{leadTimeMinutes}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private async Task<string> SetAvailabilityWithContextAsync(InteractionContext context, Guid meetingId, int scheduleVersion, string status, CancellationToken cancellationToken)
    {
        var managementClient = context.ManagementClient!;
        using var managementRequest = new HttpRequestMessage(HttpMethod.Put, $"board-meetings/{meetingId}/availability/discord");
        managementRequest.Content = JsonContent.Create(new { userId = context.Link!.UserId, displayName = context.Link.DisplayName, status, scheduleVersion });
        using var managementResponse = await managementClient.SendAsync(managementRequest, cancellationToken);
        if (managementResponse.IsSuccessStatusCode)
            return status == "Available" ? text["AvailabilityRecordedAvailable"] : text["AvailabilityRecordedUnavailable"];
        var body = await managementResponse.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("rescheduled", StringComparison.OrdinalIgnoreCase)) return text["OldAvailabilityResponse"];
        return text["AvailabilitySaveFailed"];
    }

    private async Task<(BoardMeetingResponse? Meeting, string Error)> GetNextMeetingResponseAsync(HttpClient managementClient, CancellationToken cancellationToken)
    {
        using var response = await managementClient.GetAsync("board-meetings/discord/next", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return (null, text["NoUpcomingBoardMeeting"]);
        if (!response.IsSuccessStatusCode) return (null, text["NextBoardMeetingLoadFailed"]);
        var meeting = await response.Content.ReadFromJsonAsync<BoardMeetingResponse>(cancellationToken);
        return meeting is null ? (null, text["InvalidMeetingResponse"]) : (meeting, string.Empty);
    }

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, string fallback, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body) ? fallback : $"{fallback} {body.Trim().Trim('"')}";
    }

    private string Description(string? description) =>
        string.IsNullOrWhiteSpace(description) ? string.Empty : text.Format("DescriptionSuffix", description);
    private static string Limit(string value) => value.Length <= 1900 ? value : value[..1897] + "...";

    private string GetBoardMeetingPageUrl()
    {
        return _management.GetBoardMeetingsFrontendUrl();
    }

    private static void SetToken(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record InteractionContext(DiscordUserLinkResponse? Link, HttpClient? ManagementClient, string? Error)
    {
        public static InteractionContext Success(DiscordUserLinkResponse link, HttpClient managementClient) => new(link, managementClient, null);
        public static InteractionContext Failure(string error) => new(null, null, error);
    }
}

public sealed record DiscordCommandChoice(string Name, string Value);
internal sealed record BoardAgendaItemResponse(Guid Id, string Title, string? Description, int Order);
internal sealed record BoardMeetingResponse(Guid Id, string Title, DateTimeOffset ScheduledAtUtc, int DurationMinutes,
    string? Location, int ScheduleVersion, IReadOnlyList<BoardAvailabilityResponse> Availabilities,
    IReadOnlyList<object> RescheduleProposals, IReadOnlyList<BoardAgendaItemResponse> AgendaItems)
{
    public int AvailableCount => Availabilities.Count(item => string.Equals(item.Status, "Available", StringComparison.OrdinalIgnoreCase));
    public int UnavailableCount => Availabilities.Count(item => string.Equals(item.Status, "Unavailable", StringComparison.OrdinalIgnoreCase));
}
internal sealed record BoardAvailabilityResponse(string DisplayName, string Status);
