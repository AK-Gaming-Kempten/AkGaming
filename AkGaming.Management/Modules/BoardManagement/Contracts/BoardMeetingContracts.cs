namespace AkGaming.Management.Modules.BoardManagement.Contracts;

public enum BoardMeetingStatusDto { Scheduled, Cancelled }
public enum BoardAvailabilityStatusDto { Available, Unavailable }
public enum RescheduleProposalStatusDto { Pending, Accepted, Rejected, Withdrawn }
public enum BoardAgendaItemStatusDto { Backlog, Scheduled, Completed }

public sealed record CreateBoardAgendaItemRequest(string Title, string? Description);
public sealed record CreateBoardMeetingRequest(string Title, DateTimeOffset ScheduledAtUtc, int DurationMinutes,
    string? Location, IReadOnlyList<CreateBoardAgendaItemRequest>? AgendaItems = null);
public sealed record RescheduleBoardMeetingRequest(DateTimeOffset ScheduledAtUtc, int DurationMinutes, string? Reason);
public sealed record CreateRescheduleProposalRequest(DateTimeOffset ProposedAtUtc, int DurationMinutes, string? Reason);
public sealed record SetBoardAvailabilityRequest(BoardAvailabilityStatusDto Status);
public sealed record SetDiscordBoardAvailabilityRequest(Guid UserId, string DisplayName, BoardAvailabilityStatusDto Status, int ScheduleVersion);
public sealed record CreateDiscordBoardAgendaItemRequest(Guid UserId, string Title, string? Description);
public sealed record AssignDiscordBoardAgendaItemRequest(Guid UserId);
public sealed record SaveBoardAgendaItemRequest(string Title, string? Description, Guid? MeetingId, int Order);
public sealed record MoveBoardAgendaItemRequest(Guid? MeetingId, int Order);
public sealed record ReorderBoardAgendaItemsRequest(IReadOnlyList<Guid> ItemIds);
public sealed record AssignBoardAgendaItemsRequest(IReadOnlyList<Guid> ItemIds);
public sealed record ChangeBoardAgendaItemStatusRequest(BoardAgendaItemStatusDto Status);

public sealed record BoardAvailabilityDto(Guid UserId, string DisplayName, BoardAvailabilityStatusDto Status, DateTimeOffset UpdatedAtUtc);
public sealed record BoardRescheduleProposalDto(Guid Id, DateTimeOffset ProposedAtUtc, int DurationMinutes, string? Reason,
    RescheduleProposalStatusDto Status, Guid ProposedByUserId, string ProposedByDisplayName, DateTimeOffset CreatedAtUtc);
public sealed record BoardAgendaItemDto(Guid Id, Guid? MeetingId, string Title, string? Description,
    BoardAgendaItemStatusDto Status, int Order, DateTimeOffset UpdatedAtUtc);
public sealed record BoardMeetingSummaryDto(Guid Id, string Title, DateTimeOffset ScheduledAtUtc, int DurationMinutes,
    string? Location, BoardMeetingStatusDto Status, int ScheduleVersion, int AvailableCount, int UnavailableCount);
public sealed record BoardMeetingDto(Guid Id, string Title, DateTimeOffset ScheduledAtUtc, int DurationMinutes,
    string? Location, BoardMeetingStatusDto Status, int ScheduleVersion, IReadOnlyList<BoardAvailabilityDto> Availabilities,
    IReadOnlyList<BoardRescheduleProposalDto> RescheduleProposals, IReadOnlyList<BoardAgendaItemDto> AgendaItems);
