using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;

namespace AkGaming.Management.Modules.GeneralMeetings.Application.Services;

public interface IGeneralMeetingService
{
    Task<Result<IReadOnlyList<GeneralMeetingSummaryDto>>> GetMeetingsAsync(CancellationToken ct);
    Task<Result<GeneralMeetingDto>> GetMeetingAsync(Guid id, CancellationToken ct);
    Task<Result<IReadOnlyList<MeetingAuditEventDto>>> GetAuditEventsAsync(Guid id, CancellationToken ct);
    Task<Result<GeneralMeetingDto>> CreateMeetingAsync(SaveMeetingRequest request, Guid actor, CancellationToken ct);
    Task<Result<GeneralMeetingDto>> UpdateMeetingAsync(Guid id, SaveMeetingRequest request, Guid actor, CancellationToken ct);
    Task<Result<AgendaItemDto>> SaveAgendaItemAsync(Guid meetingId, Guid? itemId, SaveAgendaItemRequest request, Guid actor, CancellationToken ct);
    Task<Result> DeleteAgendaItemAsync(Guid meetingId, Guid itemId, Guid actor, CancellationToken ct);
    Task<Result<AgendaItemDto>> UpdateMinutesAsync(Guid meetingId, Guid itemId, UpdateMinutesRequest request, Guid actor, CancellationToken ct);
    Task<Result<GeneralMeetingDto>> ChangeStatusAsync(Guid meetingId, MeetingStatusDto status, Guid actor, CancellationToken ct);
    Task<Result<AgendaItemDto>> ChangeAgendaStateAsync(Guid meetingId, Guid itemId, AgendaItemStatusDto status, Guid actor, CancellationToken ct);
    Task<Result<AttendanceDto>> SetAttendanceAsync(Guid meetingId, Guid memberId, bool? checkedIn, Guid actor, CancellationToken ct);
    Task<Result<AttendanceDto>> CheckInSelfAsync(Guid meetingId, Guid userId, CancellationToken ct);
    Task<Result<BallotDto>> SaveBallotAsync(Guid meetingId, Guid agendaItemId, Guid? ballotId, SaveBallotRequest request, Guid actor, CancellationToken ct);
    Task<Result<BallotDto>> OpenBallotAsync(Guid meetingId, Guid ballotId, Guid actor, CancellationToken ct);
    Task<Result<BallotDto>> CloseBallotAsync(Guid meetingId, Guid ballotId, Guid actor, CancellationToken ct);
    Task<Result<IssuedCredentialDto>> IssueCredentialAsync(Guid ballotId, Guid memberId, CancellationToken ct);
    Task<Result<IssuedCredentialDto>> IssueCredentialForUserAsync(Guid ballotId, Guid userId, CancellationToken ct);
    Task<Result<Guid>> CastVoteAsync(Guid ballotId, CastVoteRequest request, CancellationToken ct);
    Task<Result> DispatchInvitationsAsync(Guid meetingId, DispatchInvitationRequest request, Guid actor, CancellationToken ct);
    Task<Result<ProtocolDto>> FinalizeAsync(Guid meetingId, Guid actor, CancellationToken ct);
}
