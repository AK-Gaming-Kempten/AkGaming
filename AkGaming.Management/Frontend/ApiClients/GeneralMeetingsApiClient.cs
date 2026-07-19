using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;

namespace AkGaming.Management.Frontend.ApiClients;

public sealed class GeneralMeetingsApiClient(HttpClient http, HttpClient anonymousHttp) : ApiClientBase(http)
{
    private readonly HttpClient _anonymousHttp = anonymousHttp;
    public Task<Result<IReadOnlyList<GeneralMeetingSummaryDto>>> GetMeetingsAsync(CancellationToken ct = default) => GetAsync<IReadOnlyList<GeneralMeetingSummaryDto>>("general-meetings", ct);
    public Task<Result<GeneralMeetingDto>> GetMeetingAsync(Guid id, CancellationToken ct = default) => GetAsync<GeneralMeetingDto>($"general-meetings/{id}", ct);
    public Task<Result<GeneralMeetingDto>> CreateMeetingAsync(SaveMeetingRequest request, CancellationToken ct = default) => PostJsonAsync<SaveMeetingRequest, GeneralMeetingDto>("general-meetings", request, ct);
    public Task<Result<GeneralMeetingDto>> ChangeStatusAsync(Guid id, MeetingStatusDto status, CancellationToken ct = default) => PutJsonAsync<ChangeMeetingStatusRequest, GeneralMeetingDto>($"general-meetings/{id}/status", new(status), ct);
    public Task<Result<AgendaItemDto>> CreateAgendaItemAsync(Guid meetingId, SaveAgendaItemRequest request, CancellationToken ct = default) => PostJsonAsync<SaveAgendaItemRequest, AgendaItemDto>($"general-meetings/{meetingId}/agenda", request, ct);
    public Task<Result<AgendaItemDto>> UpdateAgendaItemAsync(Guid meetingId, Guid itemId, SaveAgendaItemRequest request, CancellationToken ct = default) => PutJsonAsync<SaveAgendaItemRequest, AgendaItemDto>($"general-meetings/{meetingId}/agenda/{itemId}", request, ct);
    public Task<Result<AgendaItemDto>> ChangeAgendaStateAsync(Guid meetingId, Guid itemId, AgendaItemStatusDto status, CancellationToken ct = default) => PutJsonAsync<ChangeAgendaStateRequest, AgendaItemDto>($"general-meetings/{meetingId}/agenda/{itemId}/state", new(status), ct);
    public Task<Result<AgendaItemDto>> UpdateMinutesAsync(Guid meetingId, Guid itemId, string? minutes, CancellationToken ct = default) => PutJsonAsync<UpdateMinutesRequest, AgendaItemDto>($"general-meetings/{meetingId}/agenda/{itemId}/minutes", new(minutes), ct);
    public Task<Result<AttendanceDto>> CheckInAsync(Guid meetingId, CancellationToken ct = default) => PostJsonAsync<object, AttendanceDto>($"general-meetings/{meetingId}/check-in", new { }, ct);
    public Task<Result<AttendanceDto>> SetAttendanceAsync(Guid meetingId, Guid memberId, bool? checkedIn, CancellationToken ct = default) => PutJsonAsync<AttendanceRequest, AttendanceDto>($"general-meetings/{meetingId}/attendance", new(memberId, checkedIn), ct);
    public Task<Result<BallotDto>> CreateBallotAsync(Guid meetingId, Guid agendaItemId, SaveBallotRequest request, CancellationToken ct = default) => PostJsonAsync<SaveBallotRequest, BallotDto>($"general-meetings/{meetingId}/agenda/{agendaItemId}/ballots", request, ct);
    public Task<Result<BallotDto>> UpdateBallotAsync(Guid meetingId, Guid agendaItemId, Guid ballotId, SaveBallotRequest request, CancellationToken ct = default) => PutJsonAsync<SaveBallotRequest, BallotDto>($"general-meetings/{meetingId}/agenda/{agendaItemId}/ballots/{ballotId}", request, ct);
    public Task<Result<BallotDto>> OpenBallotAsync(Guid meetingId, Guid ballotId, CancellationToken ct = default) => PostJsonAsync<object, BallotDto>($"general-meetings/{meetingId}/ballots/{ballotId}/open", new { }, ct);
    public Task<Result<BallotDto>> CloseBallotAsync(Guid meetingId, Guid ballotId, CancellationToken ct = default) => PostJsonAsync<object, BallotDto>($"general-meetings/{meetingId}/ballots/{ballotId}/close", new { }, ct);
    public Task<Result<IssuedCredentialDto>> IssueOwnCredentialAsync(Guid ballotId, CancellationToken ct = default) => PostJsonAsync<object, IssuedCredentialDto>($"general-meetings/ballots/{ballotId}/credential", new { }, ct);
    public Task<Result<IssuedCredentialDto>> IssueCredentialAsync(Guid ballotId, Guid memberId, CancellationToken ct = default) => PostJsonAsync<object, IssuedCredentialDto>($"general-meetings/ballots/{ballotId}/credential/{memberId}", new { }, ct);
    public async Task<Result> CastVoteAsync(Guid ballotId, CastVoteRequest request, CancellationToken ct = default)
    {
        using var response = await _anonymousHttp.PostAsJsonAsync($"general-meetings/ballots/{ballotId}/votes", request, Json, ct);
        return await ToResult(response, ct);
    }
    public Task<Result> DispatchInvitationsAsync(Guid meetingId, bool reminder, string? message, CancellationToken ct = default) => PostJsonAsync($"general-meetings/{meetingId}/invitations", new DispatchInvitationRequest(reminder, message), ct);
    public Task<Result<ProtocolDto>> FinalizeAsync(Guid meetingId, CancellationToken ct = default) => PostJsonAsync<object, ProtocolDto>($"general-meetings/{meetingId}/finalize", new { }, ct);
}
