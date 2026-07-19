using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;
using AkGaming.Management.Modules.GeneralMeetings.Contracts;
using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;

namespace AkGaming.Management.Modules.GeneralMeetings.Application.Services;

public sealed class GeneralMeetingService(
    IGeneralMeetingRepository repository,
    IMemberQueryService members,
    IEmailSender emailSender,
    IBallotCredentialProtector credentialProtector) : IGeneralMeetingService
{
    private static readonly HashSet<MembershipStatus> VotingStatuses =
        [MembershipStatus.Member, MembershipStatus.HonoraryMember, MembershipStatus.SupportingMember, MembershipStatus.Suspended];

    public async Task<Result<IReadOnlyList<GeneralMeetingSummaryDto>>> GetMeetingsAsync(CancellationToken ct)
    {
        var meetings = await repository.GetAllAsync(ct);
        return Result<IReadOnlyList<GeneralMeetingSummaryDto>>.Success(meetings.Select(MapSummary).ToList());
    }

    public async Task<Result<GeneralMeetingDto>> GetMeetingAsync(Guid id, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(id, ct);
        return meeting is null ? Result<GeneralMeetingDto>.Failure("General meeting not found.") : Result<GeneralMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<IReadOnlyList<MeetingAuditEventDto>>> GetAuditEventsAsync(Guid id, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(id, ct);
        if (meeting is null) return Result<IReadOnlyList<MeetingAuditEventDto>>.Failure("General meeting not found.");
        return Result<IReadOnlyList<MeetingAuditEventDto>>.Success(meeting.AuditEvents.OrderByDescending(x => x.OccurredAt).Select(x => new MeetingAuditEventDto(x.Id, x.Action, x.Details, x.ActorUserId, x.OccurredAt)).ToList());
    }

    public async Task<Result<GeneralMeetingDto>> CreateMeetingAsync(SaveMeetingRequest request, Guid actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<GeneralMeetingDto>.Failure("A title is required.");
        var now = DateTimeOffset.UtcNow;
        var meeting = new GeneralMeeting { Title = request.Title.Trim(), ScheduledAt = request.ScheduledAt, Location = Clean(request.Location), CreatedAt = now, UpdatedAt = now };
        Audit(meeting, "meeting.created", meeting.Title, actor);
        repository.Add(meeting);
        await repository.SaveChangesAsync(ct);
        return Result<GeneralMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<GeneralMeetingDto>> UpdateMeetingAsync(Guid id, SaveMeetingRequest request, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(id, ct);
        if (meeting is null) return Result<GeneralMeetingDto>.Failure("General meeting not found.");
        if (meeting.Status == MeetingStatus.Finalized) return Result<GeneralMeetingDto>.Failure("A finalized meeting cannot be edited.");
        if (string.IsNullOrWhiteSpace(request.Title)) return Result<GeneralMeetingDto>.Failure("A title is required.");
        meeting.Title = request.Title.Trim(); meeting.ScheduledAt = request.ScheduledAt; meeting.Location = Clean(request.Location);
        Touch(meeting); Audit(meeting, "meeting.updated", meeting.Title, actor);
        await repository.SaveChangesAsync(ct);
        return Result<GeneralMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<AgendaItemDto>> SaveAgendaItemAsync(Guid meetingId, Guid? itemId, SaveAgendaItemRequest request, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct);
        if (meeting is null) return Result<AgendaItemDto>.Failure("General meeting not found.");
        if (meeting.Status == MeetingStatus.Finalized) return Result<AgendaItemDto>.Failure("A finalized meeting cannot be edited.");
        if (string.IsNullOrWhiteSpace(request.Heading)) return Result<AgendaItemDto>.Failure("An agenda heading is required.");
        if (request.ParentId.HasValue && !meeting.AgendaItems.Any(x => x.Id == request.ParentId)) return Result<AgendaItemDto>.Failure("Parent agenda item not found.");
        var item = itemId.HasValue ? meeting.AgendaItems.SingleOrDefault(x => x.Id == itemId) : null;
        if (itemId.HasValue && item is null) return Result<AgendaItemDto>.Failure("Agenda item not found.");
        item ??= new AgendaItem { MeetingId = meetingId };
        if (!itemId.HasValue) { meeting.AgendaItems.Add(item); repository.Add(item); }
        if (request.ParentId == item.Id) return Result<AgendaItemDto>.Failure("An agenda item cannot be its own parent.");
        if (request.ParentId.HasValue && IsDescendantOf(request.ParentId.Value, item.Id, meeting.AgendaItems)) return Result<AgendaItemDto>.Failure("The selected parent would create an agenda cycle.");
        item.ParentId = request.ParentId; item.Heading = request.Heading.Trim(); item.Description = Clean(request.Description); item.Order = request.Order;
        Touch(meeting); Audit(meeting, itemId.HasValue ? "agenda.updated" : "agenda.created", item.Heading, actor);
        await repository.SaveChangesAsync(ct);
        return Result<AgendaItemDto>.Success(Map(item));
    }

    public async Task<Result> DeleteAgendaItemAsync(Guid meetingId, Guid itemId, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct);
        var item = meeting?.AgendaItems.SingleOrDefault(x => x.Id == itemId);
        if (meeting is null || item is null) return Result.Failure("Agenda item not found.");
        if (meeting.Status == MeetingStatus.Finalized) return Result.Failure("A finalized meeting cannot be edited.");
        if (meeting.AgendaItems.Any(x => x.ParentId == item.Id)) return Result.Failure("Delete or move child agenda items first.");
        if (item.Ballots.Any(x => x.Status != BallotStatus.Draft)) return Result.Failure("Agenda items with opened ballots cannot be deleted.");
        meeting.AgendaItems.Remove(item); Touch(meeting); Audit(meeting, "agenda.deleted", item.Heading, actor);
        await repository.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result<AgendaItemDto>> UpdateMinutesAsync(Guid meetingId, Guid itemId, UpdateMinutesRequest request, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); var item = meeting?.AgendaItems.SingleOrDefault(x => x.Id == itemId);
        if (meeting is null || item is null) return Result<AgendaItemDto>.Failure("Agenda item not found.");
        if (meeting.Status == MeetingStatus.Finalized) return Result<AgendaItemDto>.Failure("A finalized meeting cannot be edited.");
        item.Minutes = Clean(request.Minutes); Touch(meeting); Audit(meeting, "minutes.updated", item.Heading, actor);
        await repository.SaveChangesAsync(ct); return Result<AgendaItemDto>.Success(Map(item));
    }

    public async Task<Result<GeneralMeetingDto>> ChangeStatusAsync(Guid meetingId, MeetingStatusDto status, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct);
        if (meeting is null) return Result<GeneralMeetingDto>.Failure("General meeting not found.");
        if (meeting.Status == MeetingStatus.Finalized) return Result<GeneralMeetingDto>.Failure("A finalized meeting cannot change state.");
        if (!IsValidTransition(meeting.Status, (MeetingStatus)status)) return Result<GeneralMeetingDto>.Failure($"The meeting cannot transition from {meeting.Status} to {status}.");
        meeting.Status = (MeetingStatus)status; Touch(meeting); Audit(meeting, "meeting.status_changed", status.ToString(), actor);
        await repository.SaveChangesAsync(ct); return Result<GeneralMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<AgendaItemDto>> ChangeAgendaStateAsync(Guid meetingId, Guid itemId, AgendaItemStatusDto status, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); var item = meeting?.AgendaItems.SingleOrDefault(x => x.Id == itemId);
        if (meeting is null || item is null) return Result<AgendaItemDto>.Failure("Agenda item not found.");
        if (meeting.Status != MeetingStatus.InProgress) return Result<AgendaItemDto>.Failure("The meeting is not in progress.");
        if (status == AgendaItemStatusDto.Current)
        {
            foreach (var current in meeting.AgendaItems.Where(x => x.Status == AgendaItemStatus.Current)) current.Status = AgendaItemStatus.Completed;
            meeting.CurrentAgendaItemId = item.Id;
        }
        item.Status = (AgendaItemStatus)status; Touch(meeting); Audit(meeting, "agenda.state_changed", $"{item.Heading}: {status}", actor);
        await repository.SaveChangesAsync(ct); return Result<AgendaItemDto>.Success(Map(item));
    }

    public async Task<Result<AttendanceDto>> SetAttendanceAsync(Guid meetingId, Guid memberId, bool? checkedIn, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); if (meeting is null) return Result<AttendanceDto>.Failure("General meeting not found.");
        var memberResult = await members.GetMemberByGuidAsync(memberId); if (!memberResult.IsSuccess) return Result<AttendanceDto>.Failure("Member not found.");
        var member = memberResult.Value!; var attendance = meeting.Attendees.SingleOrDefault(x => x.MemberId == memberId);
        attendance ??= AddAttendance(meeting, member);
        var now = DateTimeOffset.UtcNow;
        if (checkedIn == true) { attendance.CheckedInAt ??= now; attendance.CheckedOutAt = null; }
        else if (checkedIn == false) { attendance.CheckedOutAt = now; }
        attendance.ChangedByUserId = actor; attendance.MembershipStatus = member.Status.ToString();
        Touch(meeting); Audit(meeting, checkedIn switch { true => "attendance.checked_in", false => "attendance.checked_out", null => "attendance.added" }, attendance.DisplayName, actor);
        await repository.SaveChangesAsync(ct); return Result<AttendanceDto>.Success(Map(attendance));
    }

    public async Task<Result<AttendanceDto>> CheckInSelfAsync(Guid meetingId, Guid userId, CancellationToken ct)
    {
        var memberResult = await members.GetMemberByUserGuidAsync(userId); if (!memberResult.IsSuccess) return Result<AttendanceDto>.Failure("Your account is not linked to a member.");
        return await SetAttendanceAsync(meetingId, memberResult.Value!.Id, true, userId, ct);
    }

    public async Task<Result<BallotDto>> SaveBallotAsync(Guid meetingId, Guid agendaItemId, Guid? ballotId, SaveBallotRequest request, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); var item = meeting?.AgendaItems.SingleOrDefault(x => x.Id == agendaItemId);
        if (meeting is null || item is null) return Result<BallotDto>.Failure("Agenda item not found.");
        if (string.IsNullOrWhiteSpace(request.Question)) return Result<BallotDto>.Failure("A ballot question is required.");
        var ballot = ballotId.HasValue ? item.Ballots.SingleOrDefault(x => x.Id == ballotId) : null;
        if (ballotId.HasValue && ballot is null) return Result<BallotDto>.Failure("Ballot not found.");
        if (ballot is not null && ballot.Status != BallotStatus.Draft) return Result<BallotDto>.Failure("Only draft ballots can be edited.");
        var options = request.Type == BallotTypeDto.YesNoAbstain ? new[] { "Yes", "No", "Abstain" } : request.Options.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray();
        if (request.Type == BallotTypeDto.Nomination && options.Length < 1) return Result<BallotDto>.Failure("At least one nomination is required.");
        if (request.MaximumSelections < 1 || request.MaximumSelections > options.Length) return Result<BallotDto>.Failure("Maximum selections is invalid.");
        ballot ??= new Ballot { AgendaItemId = item.Id };
        if (!ballotId.HasValue) { item.Ballots.Add(ballot); repository.Add(ballot); }
        ballot.Question = request.Question.Trim(); ballot.Type = (BallotType)request.Type; ballot.MaximumSelections = request.Type == BallotTypeDto.YesNoAbstain ? 1 : request.MaximumSelections; ballot.ShowResultsWhileOpen = request.ShowResultsWhileOpen;
        ballot.Options.Clear(); var order = 0; foreach (var option in options) { var ballotOption = new BallotOption { BallotId = ballot.Id, Text = option, Order = order++ }; ballot.Options.Add(ballotOption); repository.Add(ballotOption); }
        Touch(meeting); Audit(meeting, ballotId.HasValue ? "ballot.updated" : "ballot.created", ballot.Question, actor);
        await repository.SaveChangesAsync(ct); return Result<BallotDto>.Success(Map(ballot));
    }

    public async Task<Result<BallotDto>> OpenBallotAsync(Guid meetingId, Guid ballotId, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); var ballot = meeting?.AgendaItems.SelectMany(x => x.Ballots).SingleOrDefault(x => x.Id == ballotId);
        if (meeting is null || ballot is null) return Result<BallotDto>.Failure("Ballot not found.");
        if (meeting.Status != MeetingStatus.InProgress || ballot.Status != BallotStatus.Draft) return Result<BallotDto>.Failure("Only a draft ballot in an active meeting can be opened.");
        var allMembers = await members.GetAllMembersAsync(); if (!allMembers.IsSuccess) return Result<BallotDto>.Failure(allMembers.Error!);
        var currentById = allMembers.Value!.ToDictionary(x => x.Id);
        var eligible = meeting.Attendees.Where(x => x.CheckedInAt.HasValue && !x.CheckedOutAt.HasValue)
            .Where(x => currentById.TryGetValue(x.MemberId, out var member) && VotingStatuses.Contains(member.Status)).ToList();
        foreach (var attendee in eligible) { var entitlement = new BallotEntitlement { BallotId = ballot.Id, MemberId = attendee.MemberId }; ballot.Entitlements.Add(entitlement); repository.Add(entitlement); }
        ballot.Status = BallotStatus.Open; ballot.OpenedAt = DateTimeOffset.UtcNow; Touch(meeting); Audit(meeting, "ballot.opened", $"{ballot.Question}; eligible:{eligible.Count}", actor);
        await repository.SaveChangesAsync(ct); return Result<BallotDto>.Success(Map(ballot));
    }

    public async Task<Result<BallotDto>> CloseBallotAsync(Guid meetingId, Guid ballotId, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); var ballot = meeting?.AgendaItems.SelectMany(x => x.Ballots).SingleOrDefault(x => x.Id == ballotId);
        if (meeting is null || ballot is null) return Result<BallotDto>.Failure("Ballot not found.");
        if (ballot.Status != BallotStatus.Open) return Result<BallotDto>.Failure("The ballot is not open.");
        ballot.Status = BallotStatus.Closed; ballot.ClosedAt = DateTimeOffset.UtcNow; Touch(meeting); Audit(meeting, "ballot.closed", $"{ballot.Question}; votes:{ballot.Votes.Count}", actor);
        await repository.SaveChangesAsync(ct); return Result<BallotDto>.Success(Map(ballot));
    }

    public async Task<Result<IssuedCredentialDto>> IssueCredentialAsync(Guid ballotId, Guid memberId, CancellationToken ct)
    {
        var ballot = await repository.GetBallotAsync(ballotId, ct); if (ballot?.Status != BallotStatus.Open) return Result<IssuedCredentialDto>.Failure("Ballot is not open.");
        var entitlement = ballot.Entitlements.SingleOrDefault(x => x.MemberId == memberId);
        if (entitlement is null) return Result<IssuedCredentialDto>.Failure("The member is not eligible for this ballot.");
        if (entitlement.CredentialIssued) return Result<IssuedCredentialDto>.Failure("A voting credential was already issued.");
        var token = credentialProtector.Create(ballot.Id); entitlement.CredentialIssued = true;
        await repository.SaveChangesAsync(ct); return Result<IssuedCredentialDto>.Success(new IssuedCredentialDto(ballotId, token));
    }

    public async Task<Result<IssuedCredentialDto>> IssueCredentialForUserAsync(Guid ballotId, Guid userId, CancellationToken ct)
    {
        var member = await members.GetMemberByUserGuidAsync(userId); return !member.IsSuccess
            ? Result<IssuedCredentialDto>.Failure("Your account is not linked to an eligible member.")
            : await IssueCredentialAsync(ballotId, member.Value!.Id, ct);
    }

    public async Task<Result<Guid>> CastVoteAsync(Guid ballotId, CastVoteRequest request, CancellationToken ct)
    {
        var meetingId = await repository.GetMeetingIdForBallotAsync(ballotId, ct);
        var ballot = await repository.GetBallotAsync(ballotId, ct); if (meetingId is null || ballot?.Status != BallotStatus.Open) return Result<Guid>.Failure("Ballot is not open.");
        if (!credentialProtector.IsValid(ballotId, request.Credential)) return Result<Guid>.Failure("Voting credential is invalid.");
        var supplied = HashToken(request.Credential);
        var credential = ballot.Credentials.SingleOrDefault(x => CryptographicOperations.FixedTimeEquals(x.TokenHash, supplied));
        if (credential is not null) return Result<Guid>.Failure("Voting credential has already been used.");
        var selections = request.OptionIds.Distinct().ToArray();
        if (selections.Length < 1 || selections.Length > ballot.MaximumSelections || selections.Any(id => ballot.Options.All(x => x.Id != id))) return Result<Guid>.Failure("Vote selection is invalid.");
        var consumedCredential = new AnonymousCredential { BallotId = ballot.Id, TokenHash = supplied, Issued = true, Used = true };
        var vote = new AnonymousVote { BallotId = ballot.Id, SelectionsJson = JsonSerializer.Serialize(selections) };
        ballot.Credentials.Add(consumedCredential); ballot.Votes.Add(vote); repository.Add(consumedCredential); repository.Add(vote);
        await repository.SaveChangesAsync(ct); return Result<Guid>.Success(meetingId.Value);
    }

    public async Task<Result> DispatchInvitationsAsync(Guid meetingId, DispatchInvitationRequest request, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); if (meeting is null) return Result.Failure("General meeting not found.");
        var memberResult = await members.GetAllMembersAsync(); if (!memberResult.IsSuccess) return Result.Failure(memberResult.Error!);
        var recipients = memberResult.Value!.Where(x => VotingStatuses.Contains(x.Status) && !string.IsNullOrWhiteSpace(x.Email)).ToList();
        foreach (var member in recipients)
        {
            var name = DisplayName(member); var subject = $"{(request.IsReminder ? "Reminder: " : string.Empty)}{meeting.Title}";
            var body = $"Hello {name},\n\n{(request.IsReminder ? "This is a reminder for" : "You are invited to")} {meeting.Title}.\nDate: {meeting.ScheduledAt:yyyy-MM-dd HH:mm zzz}\nLocation: {meeting.Location ?? "To be announced"}\n\n{request.AdditionalMessage}".TrimEnd();
            var dispatch = new InvitationDispatch { MeetingId = meeting.Id, Kind = request.IsReminder ? "Reminder" : "Invitation", RecipientEmail = member.Email!, RecipientName = name, DispatchedAt = DateTimeOffset.UtcNow };
            try { await emailSender.SendAsync(member.Email!, subject, body, null, ct); dispatch.Succeeded = true; }
            catch (Exception exception) { dispatch.Error = exception.Message; }
            meeting.InvitationDispatches.Add(dispatch);
            repository.Add(dispatch);
        }
        if (!request.IsReminder && meeting.Status == MeetingStatus.Draft) meeting.Status = MeetingStatus.InvitationsSent;
        Touch(meeting); Audit(meeting, request.IsReminder ? "reminder.dispatched" : "invitation.dispatched", $"recipients:{recipients.Count}", actor);
        await repository.SaveChangesAsync(ct); return Result.Success();
    }

    public async Task<Result<ProtocolDto>> FinalizeAsync(Guid meetingId, Guid actor, CancellationToken ct)
    {
        var meeting = await repository.GetAsync(meetingId, ct); if (meeting is null) return Result<ProtocolDto>.Failure("General meeting not found.");
        if (meeting.Status != MeetingStatus.Closed) return Result<ProtocolDto>.Failure("The meeting must be closed before finalization.");
        if (meeting.AgendaItems.SelectMany(x => x.Ballots).Any(x => x.Status == BallotStatus.Open)) return Result<ProtocolDto>.Failure("All ballots must be closed first.");
        var markdown = BuildProtocol(meeting); var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(markdown));
        var revision = new ProtocolRevision { MeetingId = meeting.Id, Revision = meeting.ProtocolRevisions.Count + 1, Markdown = markdown, Sha256 = Convert.ToHexString(bytes).ToLowerInvariant(), FinalizedByUserId = actor, FinalizedAt = DateTimeOffset.UtcNow };
        meeting.ProtocolRevisions.Add(revision); repository.Add(revision); meeting.Status = MeetingStatus.Finalized; Touch(meeting); Audit(meeting, "protocol.finalized", $"revision:{revision.Revision};sha256:{revision.Sha256}", actor);
        await repository.SaveChangesAsync(ct); return Result<ProtocolDto>.Success(Map(revision));
    }

    private static string BuildProtocol(GeneralMeeting meeting)
    {
        var text = new StringBuilder(); text.AppendLine($"# {meeting.Title}").AppendLine(); text.AppendLine("## Meeting details");
        text.AppendLine($"- Date: {meeting.ScheduledAt:yyyy-MM-dd HH:mm zzz}"); text.AppendLine($"- Location: {meeting.Location ?? "Not specified"}");
        var present = meeting.Attendees.Where(x => x.CheckedInAt.HasValue).OrderBy(x => x.DisplayName).ToList(); text.AppendLine($"- Attendees: {present.Count}");
        foreach (var attendee in present) text.AppendLine($"  - {attendee.DisplayName}");
        foreach (var item in OrderedAgenda(meeting))
        {
            var depth = GetDepth(item, meeting.AgendaItems); text.AppendLine().AppendLine($"{new string('#', Math.Min(6, depth + 2))} {item.Heading}");
            if (!string.IsNullOrWhiteSpace(item.Description)) text.AppendLine().AppendLine(item.Description);
            if (!string.IsNullOrWhiteSpace(item.Minutes)) text.AppendLine().AppendLine("### Minutes").AppendLine().AppendLine(item.Minutes);
            foreach (var ballot in item.Ballots.Where(x => x.Status == BallotStatus.Closed))
            {
                text.AppendLine().AppendLine($"### Ballot: {ballot.Question}"); text.AppendLine($"- Eligible voters: {ballot.Entitlements.Count}"); text.AppendLine($"- Credentials issued: {ballot.Entitlements.Count(x => x.CredentialIssued)}"); text.AppendLine($"- Votes cast: {ballot.Votes.Count}");
                foreach (var result in Results(ballot)) text.AppendLine($"- {result.Text}: {result.Votes}");
            }
        }
        return text.ToString().TrimEnd() + Environment.NewLine;
    }

    private static IEnumerable<AgendaItem> OrderedAgenda(GeneralMeeting meeting)
    {
        foreach (var root in meeting.AgendaItems.Where(x => !x.ParentId.HasValue).OrderBy(x => x.Order))
            foreach (var item in Flatten(root, meeting.AgendaItems)) yield return item;
    }
    private static IEnumerable<AgendaItem> Flatten(AgendaItem item, ICollection<AgendaItem> all) { yield return item; foreach (var child in all.Where(x => x.ParentId == item.Id).OrderBy(x => x.Order)) foreach (var descendant in Flatten(child, all)) yield return descendant; }
    private static bool IsDescendantOf(Guid candidateParentId, Guid itemId, ICollection<AgendaItem> all) { var current = all.SingleOrDefault(x => x.Id == candidateParentId); while (current is not null) { if (current.Id == itemId) return true; current = current.ParentId.HasValue ? all.SingleOrDefault(x => x.Id == current.ParentId) : null; } return false; }
    private static bool IsValidTransition(MeetingStatus current, MeetingStatus next) => current == next || (current, next) switch { (MeetingStatus.Draft, MeetingStatus.InvitationsSent or MeetingStatus.CheckInOpen) => true, (MeetingStatus.InvitationsSent, MeetingStatus.CheckInOpen) => true, (MeetingStatus.CheckInOpen, MeetingStatus.InProgress) => true, (MeetingStatus.InProgress, MeetingStatus.Closed) => true, _ => false };
    private static int GetDepth(AgendaItem item, ICollection<AgendaItem> all) { var depth = 0; var parent = item.ParentId; while (parent.HasValue && depth < 4) { depth++; parent = all.SingleOrDefault(x => x.Id == parent)?.ParentId; } return depth; }
    private Attendance AddAttendance(GeneralMeeting meeting, MemberDto member) { var item = new Attendance { MeetingId = meeting.Id, MemberId = member.Id, UserId = member.UserId, DisplayName = DisplayName(member), MembershipStatus = member.Status.ToString() }; meeting.Attendees.Add(item); repository.Add(item); return item; }
    private static string DisplayName(MemberDto member) => string.Join(' ', new[] { member.FirstName, member.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim() is { Length: > 0 } name ? name : member.Email ?? member.Id.ToString();
    private static void Touch(GeneralMeeting meeting) { meeting.Version++; meeting.UpdatedAt = DateTimeOffset.UtcNow; }
    private void Audit(GeneralMeeting meeting, string action, string details, Guid? actor) { var auditEvent = new MeetingAuditEvent { MeetingId = meeting.Id, Action = action, Details = details, ActorUserId = actor, OccurredAt = DateTimeOffset.UtcNow }; meeting.AuditEvents.Add(auditEvent); repository.Add(auditEvent); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static byte[] HashToken(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
    private static GeneralMeetingSummaryDto MapSummary(GeneralMeeting x) => new(x.Id, x.Title, x.ScheduledAt, x.Location, (MeetingStatusDto)x.Status);
    private static AttendanceDto Map(Attendance x) => new(x.MemberId, x.UserId, x.DisplayName, x.MembershipStatus, x.CheckedInAt, x.CheckedOutAt, false);
    private static AgendaItemDto Map(AgendaItem x) => new(x.Id, x.ParentId, x.Heading, x.Description, x.Minutes, x.Order, (AgendaItemStatusDto)x.Status, x.Ballots.OrderBy(b => b.OpenedAt).Select(Map).ToList());
    private static BallotDto Map(Ballot x) => new(x.Id, x.Question, (BallotTypeDto)x.Type, (BallotStatusDto)x.Status, x.MaximumSelections, x.ShowResultsWhileOpen, x.Entitlements.Count, x.Entitlements.Count(c => c.CredentialIssued), x.Votes.Count, x.Options.OrderBy(o => o.Order).Select(o => new BallotOptionDto(o.Id, o.Text, o.Order)).ToList(), x.Status == BallotStatus.Closed || x.ShowResultsWhileOpen ? Results(x) : null);
    private static IReadOnlyList<BallotResultDto> Results(Ballot ballot) { var ids = ballot.Votes.SelectMany(x => JsonSerializer.Deserialize<Guid[]>(x.SelectionsJson) ?? []).ToLookup(x => x); return ballot.Options.OrderBy(x => x.Order).Select(x => new BallotResultDto(x.Id, x.Text, ids[x.Id].Count())).ToList(); }
    private static ProtocolDto Map(ProtocolRevision x) => new(x.Revision, x.Markdown, x.Sha256, x.FinalizedAt);
    private static GeneralMeetingDto Map(GeneralMeeting x) => new(x.Id, x.Title, x.ScheduledAt, x.Location, (MeetingStatusDto)x.Status, x.CurrentAgendaItemId, x.Version, x.AgendaItems.OrderBy(a => a.Order).Select(Map).ToList(), x.Attendees.OrderBy(a => a.DisplayName).Select(Map).ToList(), x.ProtocolRevisions.OrderByDescending(p => p.Revision).Select(Map).FirstOrDefault());
}
