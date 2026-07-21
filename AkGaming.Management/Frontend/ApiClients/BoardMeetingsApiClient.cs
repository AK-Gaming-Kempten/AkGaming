using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.BoardManagement.Contracts;

namespace AkGaming.Management.Frontend.ApiClients;

public sealed class BoardMeetingsApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<Result<IReadOnlyList<BoardMeetingSummaryDto>>> GetMeetingsAsync(CancellationToken ct = default) => GetAsync<IReadOnlyList<BoardMeetingSummaryDto>>("board-meetings", ct);
    public Task<Result<BoardMeetingDto>> GetMeetingAsync(Guid id, CancellationToken ct = default) => GetAsync<BoardMeetingDto>($"board-meetings/{id}", ct);
    public Task<Result<IReadOnlyList<BoardAgendaItemDto>>> GetBacklogAsync(CancellationToken ct = default) => GetAsync<IReadOnlyList<BoardAgendaItemDto>>("board-meetings/agenda/backlog", ct);
    public Task<Result<BoardMeetingDto>> CreateMeetingAsync(CreateBoardMeetingRequest request, CancellationToken ct = default) => PostJsonAsync<CreateBoardMeetingRequest, BoardMeetingDto>("board-meetings", request, ct);
    public Task<Result<BoardMeetingDto>> RescheduleAsync(Guid id, RescheduleBoardMeetingRequest request, CancellationToken ct = default) => PostJsonAsync<RescheduleBoardMeetingRequest, BoardMeetingDto>($"board-meetings/{id}/reschedule", request, ct);
    public Task<Result<BoardMeetingDto>> CancelAsync(Guid id, CancellationToken ct = default) => PostJsonAsync<object, BoardMeetingDto>($"board-meetings/{id}/cancel", new { }, ct);
    public Task<Result<BoardRescheduleProposalDto>> ProposeAsync(Guid id, CreateRescheduleProposalRequest request, CancellationToken ct = default) => PostJsonAsync<CreateRescheduleProposalRequest, BoardRescheduleProposalDto>($"board-meetings/{id}/reschedule-proposals", request, ct);
    public Task<Result<BoardMeetingDto>> DecideProposalAsync(Guid meetingId, Guid proposalId, bool accept, CancellationToken ct = default) => PostJsonAsync<object, BoardMeetingDto>($"board-meetings/{meetingId}/reschedule-proposals/{proposalId}/{(accept ? "accept" : "reject")}", new { }, ct);
    public Task<Result<BoardAvailabilityDto>> SetAvailabilityAsync(Guid id, BoardAvailabilityStatusDto status, CancellationToken ct = default) => PutJsonAsync<SetBoardAvailabilityRequest, BoardAvailabilityDto>($"board-meetings/{id}/availability", new(status), ct);
    public Task<Result<BoardAgendaItemDto>> CreateAgendaItemAsync(SaveBoardAgendaItemRequest request, CancellationToken ct = default) => PostJsonAsync<SaveBoardAgendaItemRequest, BoardAgendaItemDto>("board-meetings/agenda", request, ct);
    public Task<Result<BoardAgendaItemDto>> UpdateAgendaItemAsync(Guid id, SaveBoardAgendaItemRequest request, CancellationToken ct = default) => PutJsonAsync<SaveBoardAgendaItemRequest, BoardAgendaItemDto>($"board-meetings/agenda/{id}", request, ct);
    public Task<Result> DeleteAgendaItemAsync(Guid id, CancellationToken ct = default) => DeleteAsync($"board-meetings/agenda/{id}", ct);
    public Task<Result<BoardAgendaItemDto>> MoveAgendaItemAsync(Guid id, Guid? meetingId, int order, CancellationToken ct = default) => PutJsonAsync<MoveBoardAgendaItemRequest, BoardAgendaItemDto>($"board-meetings/agenda/{id}/move", new(meetingId, order), ct);
    public Task<Result<IReadOnlyList<BoardAgendaItemDto>>> ReorderAgendaItemsAsync(Guid meetingId, IReadOnlyList<Guid> itemIds, CancellationToken ct = default) => PutJsonAsync<ReorderBoardAgendaItemsRequest, IReadOnlyList<BoardAgendaItemDto>>($"board-meetings/{meetingId}/agenda/order", new(itemIds), ct);
    public Task<Result<BoardMeetingDto>> AssignAgendaItemsAsync(Guid meetingId, IReadOnlyList<Guid> itemIds, CancellationToken ct = default) => PutJsonAsync<AssignBoardAgendaItemsRequest, BoardMeetingDto>($"board-meetings/{meetingId}/agenda/from-backlog", new(itemIds), ct);
    public Task<Result<BoardAgendaItemDto>> ChangeAgendaStatusAsync(Guid id, BoardAgendaItemStatusDto status, CancellationToken ct = default) => PutJsonAsync<ChangeBoardAgendaItemStatusRequest, BoardAgendaItemDto>($"board-meetings/agenda/{id}/status", new(status), ct);
}
