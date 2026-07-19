namespace AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;

public enum MeetingStatus { Draft, InvitationsSent, CheckInOpen, InProgress, Closed, Finalized }
public enum AgendaItemStatus { Pending, Current, Completed, Skipped }
public enum BallotType { YesNoAbstain, Nomination }
public enum BallotStatus { Draft, Open, Closed }

public sealed class GeneralMeeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public string? Location { get; set; }
    public MeetingStatus Status { get; set; }
    public Guid? CurrentAgendaItemId { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
    public ICollection<Attendance> Attendees { get; set; } = new List<Attendance>();
    public ICollection<MeetingAuditEvent> AuditEvents { get; set; } = new List<MeetingAuditEvent>();
    public ICollection<InvitationDispatch> InvitationDispatches { get; set; } = new List<InvitationDispatch>();
    public ICollection<ProtocolRevision> ProtocolRevisions { get; set; } = new List<ProtocolRevision>();
}

public sealed class AgendaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public GeneralMeeting Meeting { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public AgendaItem? Parent { get; set; }
    public ICollection<AgendaItem> Children { get; set; } = new List<AgendaItem>();
    public string Heading { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Minutes { get; set; }
    public int Order { get; set; }
    public AgendaItemStatus Status { get; set; }
    public ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();
}

public sealed class Attendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public GeneralMeeting Meeting { get; set; } = null!;
    public Guid MemberId { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }
    public Guid? ChangedByUserId { get; set; }
}

public sealed class Ballot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgendaItemId { get; set; }
    public AgendaItem AgendaItem { get; set; } = null!;
    public string Question { get; set; } = string.Empty;
    public BallotType Type { get; set; }
    public BallotStatus Status { get; set; }
    public int MaximumSelections { get; set; } = 1;
    public bool ShowResultsWhileOpen { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public ICollection<BallotOption> Options { get; set; } = new List<BallotOption>();
    public ICollection<BallotEntitlement> Entitlements { get; set; } = new List<BallotEntitlement>();
    public ICollection<AnonymousCredential> Credentials { get; set; } = new List<AnonymousCredential>();
    public ICollection<AnonymousVote> Votes { get; set; } = new List<AnonymousVote>();
}

public sealed class BallotOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BallotId { get; set; }
    public Ballot Ballot { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}

// Eligibility and anonymous ballot-box data deliberately have no navigation or FK between them.
public sealed class BallotEntitlement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BallotId { get; set; }
    public Guid MemberId { get; set; }
    public bool CredentialIssued { get; set; }
}

public sealed class AnonymousCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BallotId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public bool Issued { get; set; }
    public bool Used { get; set; }
}

public sealed class AnonymousVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BallotId { get; set; }
    public string SelectionsJson { get; set; } = "[]";
}

public sealed class MeetingAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class InvitationDispatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset DispatchedAt { get; set; }
}

public sealed class ProtocolRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public int Revision { get; set; }
    public string Markdown { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public Guid FinalizedByUserId { get; set; }
    public DateTimeOffset FinalizedAt { get; set; }
}
