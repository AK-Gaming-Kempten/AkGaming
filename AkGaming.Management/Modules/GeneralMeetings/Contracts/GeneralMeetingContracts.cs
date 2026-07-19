namespace AkGaming.Management.Modules.GeneralMeetings.Contracts;

public enum MeetingStatusDto { Draft, InvitationsSent, CheckInOpen, InProgress, Closed, Finalized }
public enum AgendaItemStatusDto { Pending, Current, Completed, Skipped }
public enum BallotTypeDto { YesNoAbstain, Nomination }
public enum BallotStatusDto { Draft, Open, Closed }

public sealed record SaveMeetingRequest(string Title, DateTimeOffset ScheduledAt, string? Location);
public sealed record SaveAgendaItemRequest(Guid? ParentId, string Heading, string? Description, int Order);
public sealed record UpdateMinutesRequest(string? Minutes);
public sealed record ChangeMeetingStatusRequest(MeetingStatusDto Status);
public sealed record ChangeAgendaStateRequest(AgendaItemStatusDto Status);
public sealed record AttendanceRequest(Guid MemberId, bool? CheckedIn);
public sealed record SaveBallotRequest(string Question, BallotTypeDto Type, IReadOnlyList<string> Options, int MaximumSelections, bool ShowResultsWhileOpen);
public sealed record CastVoteRequest(string Credential, IReadOnlyList<Guid> OptionIds);
public sealed record DispatchInvitationRequest(bool IsReminder, string? AdditionalMessage);
public sealed record IssuedCredentialDto(Guid BallotId, string Credential);
public sealed record ProtocolDto(int Revision, string Markdown, string Sha256, DateTimeOffset FinalizedAt);
public sealed record MeetingAuditEventDto(Guid Id, string Action, string Details, Guid? ActorUserId, DateTimeOffset OccurredAt);
public sealed record BallotResultDto(Guid OptionId, string Text, int Votes);
public sealed record BallotDto(Guid Id, string Question, BallotTypeDto Type, BallotStatusDto Status, int MaximumSelections,
    bool ShowResultsWhileOpen, int EligibleVoters, int CredentialsIssued, int VotesCast,
    IReadOnlyList<BallotOptionDto> Options, IReadOnlyList<BallotResultDto>? Results);
public sealed record BallotOptionDto(Guid Id, string Text, int Order);
public sealed record AttendanceDto(Guid MemberId, Guid? UserId, string DisplayName, string MembershipStatus,
    DateTimeOffset? CheckedInAt, DateTimeOffset? CheckedOutAt, bool IsOnline);
public sealed record AgendaItemDto(Guid Id, Guid? ParentId, string Heading, string? Description, string? Minutes, int Order,
    AgendaItemStatusDto Status, IReadOnlyList<BallotDto> Ballots);
public sealed record GeneralMeetingSummaryDto(Guid Id, string Title, DateTimeOffset ScheduledAt, string? Location, MeetingStatusDto Status);
public sealed record GeneralMeetingDto(Guid Id, string Title, DateTimeOffset ScheduledAt, string? Location, MeetingStatusDto Status,
    Guid? CurrentAgendaItemId, long Version, IReadOnlyList<AgendaItemDto> AgendaItems, IReadOnlyList<AttendanceDto> Attendees,
    ProtocolDto? Protocol);
