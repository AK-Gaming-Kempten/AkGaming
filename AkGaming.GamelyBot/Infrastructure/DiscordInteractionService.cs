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
    IOptions<DiscordInteractionOptions> interactionOptions, INotificationInbox notificationInbox)
{
    private readonly IdentityClientOptions _identity = identityOptions.Value;
    private readonly ManagementClientOptions _management = managementOptions.Value;
    private readonly DiscordInteractionOptions _interactions = interactionOptions.Value;

    public string GetBoardMeetingHelp()
    {
        var pageUrl = GetBoardMeetingPageUrl();
        return """
            **Board meeting commands**
            `/boardmeeting agenda` - View the next meeting agenda
            `/boardmeeting backlog` - View the agenda backlog
            `/boardmeeting details` - View the next meeting details and attendance forecast
            `/boardmeeting create` - Open the management tool to create a meeting
            `/boardmeeting reminder` - Send a reminder for the next meeting
            `/boardmeeting availability` - Record whether you can attend
            `/boardmeeting add-agenda` - Add an item to the next meeting
            `/boardmeeting add-backlog` - Add an item to the backlog
            `/boardmeeting promote` - Move a backlog item to the next meeting

            Use the management tool to create, reschedule or cancel meetings and for more complex agenda changes:
            """ + $"\n[Open board meeting management]({pageUrl})";
    }

    public string GetBoardMeetingCreateHelp()
    {
        return $"Meetings are created in the management tool so the date, location, and initial agenda can be reviewed together.\n[Create a board meeting]({GetBoardMeetingPageUrl()})";
    }

    public async Task<string> QueueNextMeetingReminderAsync(string discordUserId, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        var next = await GetNextMeetingResponseAsync(context.ManagementClient!, cancellationToken);
        if (next.Meeting is null) return next.Error;
        await QueueReminderAsync(next.Meeting, Guid.NewGuid(), cancellationToken);
        return $"Queued a reminder for **{next.Meeting.Title}** in the board channel.";
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
        if (!response.IsSuccessStatusCode) return "I could not load the board backlog right now.";
        var items = await response.Content.ReadFromJsonAsync<List<BoardAgendaItemResponse>>(cancellationToken) ?? [];
        if (items.Count == 0) return "The board backlog is empty.";
        return Limit("**Board meeting backlog**\n" + string.Join('\n', items.Select((item, index) => $"{index + 1}. **{item.Title}**{Description(item.Description)}")));
    }

    public async Task<string> GetNextMeetingAsync(string discordUserId, bool includeAgenda, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        var result = await GetNextMeetingResponseAsync(context.ManagementClient!, cancellationToken);
        if (result.Meeting is null) return result.Error;
        var meeting = result.Meeting;
        var timestamp = meeting.ScheduledAtUtc.ToUnixTimeSeconds();
        var location = string.IsNullOrWhiteSpace(meeting.Location) ? "Location pending" : meeting.Location;
        var lines = new List<string>
        {
            $"**{meeting.Title}**",
            $"<t:{timestamp}:F> (<t:{timestamp}:R>)",
            $"{meeting.DurationMinutes} minutes - {location}",
            $"Availability: {meeting.AvailableCount} available, {meeting.UnavailableCount} unavailable"
        };
        if (includeAgenda)
        {
            lines.Add(string.Empty);
            lines.Add("**Agenda**");
            lines.AddRange(meeting.AgendaItems.Count == 0
                ? ["No agenda items yet."]
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
            return addToNextMeeting ? $"Added **{title}** to the next meeting agenda." : $"Added **{title}** to the board backlog.";
        return await ErrorMessageAsync(response, addToNextMeeting ? "I could not add the item to the next meeting agenda." : "I could not add the item to the backlog.", cancellationToken);
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
        if (response.IsSuccessStatusCode) return "Added the backlog item to the next meeting agenda.";
        return await ErrorMessageAsync(response, "I could not add that backlog item to the next meeting.", cancellationToken);
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
            return $"Submitted your proposal for <t:{proposedAtUtc.ToUnixTimeSeconds()}:F>.";
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("rescheduled", StringComparison.OrdinalIgnoreCase))
            return "This announcement belongs to an old meeting date. Please use the latest meeting announcement.";
        return "I could not submit your rescheduling proposal right now. Please use the management tool or try again later.";
    }

    public async Task<string> DecideRescheduleProposalAsync(string discordUserId, Guid meetingId, Guid proposalId,
        bool accept, CancellationToken cancellationToken)
    {
        var context = await CreateContextAsync(discordUserId, cancellationToken);
        if (context.Error is not null) return context.Error;
        if (!context.Link!.CanManageBoardMeetings)
            return "Your linked account is not authorized to accept or reject board meeting proposals.";
        using var response = await context.ManagementClient!.PostAsJsonAsync(
            $"board-meetings/{meetingId}/reschedule-proposals/{proposalId}/decision/discord",
            new { userId = context.Link.UserId, accept },
            cancellationToken);
        if (response.IsSuccessStatusCode)
            return accept ? "The rescheduling proposal was accepted." : "The rescheduling proposal was rejected.";
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("already been decided", StringComparison.OrdinalIgnoreCase))
            return "This rescheduling proposal has already been decided.";
        return "I could not update the rescheduling proposal right now. Please use the management tool or try again later.";
    }

    private async Task<InteractionContext> CreateContextAsync(string discordUserId, CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(cancellationToken);
        var identityClient = clients.CreateClient(nameof(DiscordInteractionService));
        identityClient.BaseAddress = new Uri(_identity.BaseUrl, UriKind.Absolute);
        using var identityRequest = new HttpRequestMessage(HttpMethod.Get, $"internal/discord-links/by-discord/{discordUserId}");
        SetToken(identityRequest, token);
        using var identityResponse = await identityClient.SendAsync(identityRequest, cancellationToken);
        if (identityResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            return InteractionContext.Failure("Your Discord account is not linked to an AK Gaming account. Link it in your account settings, then try again.");
        if (!identityResponse.IsSuccessStatusCode)
            return InteractionContext.Failure("I could not look up your linked account right now. Please try again later.");
        var link = await identityResponse.Content.ReadFromJsonAsync<DiscordUserLinkResponse>(cancellationToken);
        if (link?.IsLinked != true) return InteractionContext.Failure("Your Discord account is not linked to an AK Gaming account.");
        if (!link.CanAccessBoardMeetings) return InteractionContext.Failure("Your linked account is not currently authorized as a board member.");

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

    private static async Task<string> SetAvailabilityWithContextAsync(InteractionContext context, Guid meetingId, int scheduleVersion, string status, CancellationToken cancellationToken)
    {
        var managementClient = context.ManagementClient!;
        using var managementRequest = new HttpRequestMessage(HttpMethod.Put, $"board-meetings/{meetingId}/availability/discord");
        managementRequest.Content = JsonContent.Create(new { userId = context.Link!.UserId, displayName = context.Link.DisplayName, status, scheduleVersion });
        using var managementResponse = await managementClient.SendAsync(managementRequest, cancellationToken);
        if (managementResponse.IsSuccessStatusCode)
            return status == "Available" ? "Recorded: you have time for this meeting." : "Recorded: you cannot attend this meeting.";
        var body = await managementResponse.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("rescheduled", StringComparison.OrdinalIgnoreCase)) return "This response belongs to an old meeting date. Please use the buttons on the latest notification.";
        return "I could not save your availability right now. Please use the management tool or try again later.";
    }

    private static async Task<(BoardMeetingResponse? Meeting, string Error)> GetNextMeetingResponseAsync(HttpClient managementClient, CancellationToken cancellationToken)
    {
        using var response = await managementClient.GetAsync("board-meetings/discord/next", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return (null, "No upcoming board meeting is scheduled.");
        if (!response.IsSuccessStatusCode) return (null, "I could not load the next board meeting right now.");
        var meeting = await response.Content.ReadFromJsonAsync<BoardMeetingResponse>(cancellationToken);
        return meeting is null ? (null, "The management tool returned an invalid meeting.") : (meeting, string.Empty);
    }

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, string fallback, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body) ? fallback : $"{fallback} {body.Trim().Trim('"')}";
    }

    private static string Description(string? description) => string.IsNullOrWhiteSpace(description) ? string.Empty : $" — {description}";
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
