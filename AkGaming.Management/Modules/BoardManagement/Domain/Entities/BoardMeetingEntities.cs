namespace AkGaming.Management.Modules.BoardManagement.Domain.Entities;

public enum BoardMeetingStatus { Scheduled, Cancelled }
public enum BoardAvailabilityStatus { Available, Unavailable }
public enum RescheduleProposalStatus { Pending, Accepted, Rejected, Withdrawn }
public enum BoardAgendaItemStatus { Backlog, Scheduled, Completed }

public sealed class BoardMeeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string? Location { get; set; }
    public BoardMeetingStatus Status { get; set; }
    public int ScheduleVersion { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<BoardAvailability> Availabilities { get; set; } = new List<BoardAvailability>();
    public ICollection<BoardRescheduleProposal> RescheduleProposals { get; set; } = new List<BoardRescheduleProposal>();
    public ICollection<BoardAgendaItem> AgendaItems { get; set; } = new List<BoardAgendaItem>();
}

public sealed class BoardAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public BoardMeeting Meeting { get; set; } = null!;
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public BoardAvailabilityStatus Status { get; set; }
    public int ScheduleVersion { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class BoardRescheduleProposal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public BoardMeeting Meeting { get; set; } = null!;
    public DateTimeOffset ProposedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string? Reason { get; set; }
    public RescheduleProposalStatus Status { get; set; }
    public Guid ProposedByUserId { get; set; }
    public string ProposedByDisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

public sealed class BoardAgendaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? MeetingId { get; set; }
    public BoardMeeting? Meeting { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BoardAgendaItemStatus Status { get; set; }
    public int Order { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class BoardNotificationOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
}
