using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.BoardManagement.Contracts;

namespace AkGaming.Management.Modules.BoardManagement.Application.Services;

public interface IBoardMeetingService
{
    Task<Result<IReadOnlyList<BoardMeetingSummaryDto>>> GetMeetingsAsync(CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> GetMeetingAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> GetNextMeetingAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoardAgendaItemDto>>> GetBacklogAsync(CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> CreateMeetingAsync(CreateBoardMeetingRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> RescheduleMeetingAsync(Guid id, RescheduleBoardMeetingRequest request, CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> CancelMeetingAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<BoardRescheduleProposalDto>> ProposeRescheduleAsync(Guid id, CreateRescheduleProposalRequest request, Guid actorUserId, string displayName, int? expectedScheduleVersion, CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> DecideProposalAsync(Guid meetingId, Guid proposalId, bool accept, Guid actorUserId, CancellationToken cancellationToken);
    Task<Result<BoardAvailabilityDto>> SetAvailabilityAsync(Guid meetingId, Guid userId, string displayName, BoardAvailabilityStatusDto status, int? expectedScheduleVersion, CancellationToken cancellationToken);
    Task<Result<BoardAgendaItemDto>> CreateAgendaItemAsync(SaveBoardAgendaItemRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<Result<BoardAgendaItemDto>> UpdateAgendaItemAsync(Guid itemId, SaveBoardAgendaItemRequest request, CancellationToken cancellationToken);
    Task<Result<BoardAgendaItemDto>> DeleteAgendaItemAsync(Guid itemId, CancellationToken cancellationToken);
    Task<Result<BoardAgendaItemDto>> MoveAgendaItemAsync(Guid itemId, MoveBoardAgendaItemRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoardAgendaItemDto>>> ReorderAgendaItemsAsync(Guid meetingId, ReorderBoardAgendaItemsRequest request, CancellationToken cancellationToken);
    Task<Result<BoardMeetingDto>> AssignAgendaItemsAsync(Guid meetingId, AssignBoardAgendaItemsRequest request, CancellationToken cancellationToken);
    Task<Result<BoardAgendaItemDto>> ChangeAgendaItemStatusAsync(Guid itemId, BoardAgendaItemStatusDto status, CancellationToken cancellationToken);
}
