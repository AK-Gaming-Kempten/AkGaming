using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.BoardManagement.Application.Interfaces;
using AkGaming.Management.Modules.BoardManagement.Contracts;
using AkGaming.Management.Modules.BoardManagement.Domain.Entities;

namespace AkGaming.Management.Modules.BoardManagement.Application.Services;

public sealed class BoardMeetingService(IBoardMeetingRepository repository, IBoardNotificationOutbox notifications) : IBoardMeetingService
{
    public async Task<Result<IReadOnlyList<BoardMeetingSummaryDto>>> GetMeetingsAsync(CancellationToken cancellationToken)
    {
        var meetings = await repository.GetMeetingsAsync(cancellationToken);
        var result = meetings.Select(MapSummary).ToList();
        return Result<IReadOnlyList<BoardMeetingSummaryDto>>.Success(result);
    }

    public async Task<Result<BoardMeetingDto>> GetMeetingAsync(Guid id, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(id, cancellationToken);
        return meeting is null ? Result<BoardMeetingDto>.Failure("Board meeting not found.") : Result<BoardMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<BoardMeetingDto>> GetNextMeetingAsync(CancellationToken cancellationToken)
    {
        var meetings = await repository.GetMeetingsAsync(cancellationToken);
        var nextMeeting = meetings
            .Where(meeting => meeting.Status == BoardMeetingStatus.Scheduled && meeting.ScheduledAtUtc >= DateTimeOffset.UtcNow)
            .OrderBy(meeting => meeting.ScheduledAtUtc)
            .FirstOrDefault();
        return nextMeeting is null
            ? Result<BoardMeetingDto>.Failure("No upcoming board meeting is scheduled.")
            : Result<BoardMeetingDto>.Success(Map(nextMeeting));
    }

    public async Task<Result<IReadOnlyList<BoardAgendaItemDto>>> GetBacklogAsync(CancellationToken cancellationToken)
    {
        var items = await repository.GetBacklogAsync(cancellationToken);
        return Result<IReadOnlyList<BoardAgendaItemDto>>.Success(items.Select(Map).ToList());
    }

    public async Task<Result<BoardMeetingDto>> CreateMeetingAsync(CreateBoardMeetingRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var validation = ValidateSchedule(request.Title, request.ScheduledAtUtc, request.DurationMinutes);
        if (validation is not null) return Result<BoardMeetingDto>.Failure(validation);
        var agendaItems = request.AgendaItems ?? [];
        if (agendaItems.Any(x => string.IsNullOrWhiteSpace(x.Title))) return Result<BoardMeetingDto>.Failure("Every agenda item requires a title.");
        var backlogItemIds = agendaItems.Where(x => x.BacklogItemId.HasValue).Select(x => x.BacklogItemId!.Value).ToList();
        if (backlogItemIds.Count != backlogItemIds.Distinct().Count())
            return Result<BoardMeetingDto>.Failure("A backlog item can only be selected once.");
        var backlogItems = backlogItemIds.Count == 0
            ? []
            : await repository.GetAgendaItemsAsync(backlogItemIds, cancellationToken);
        if (backlogItems.Count != backlogItemIds.Count || backlogItems.Any(x => x.MeetingId.HasValue))
            return Result<BoardMeetingDto>.Failure("One or more selected items are no longer in the backlog.");
        var backlogItemsById = backlogItems.ToDictionary(x => x.Id);
        var now = DateTimeOffset.UtcNow;
        var meeting = new BoardMeeting { Title = request.Title.Trim(), ScheduledAtUtc = request.ScheduledAtUtc, DurationMinutes = request.DurationMinutes, Location = Clean(request.Location), CreatedAtUtc = now, UpdatedAtUtc = now };
        var order = 0;
        foreach (var agendaItem in agendaItems)
        {
            BoardAgendaItem item;
            if (agendaItem.BacklogItemId.HasValue)
            {
                item = backlogItemsById[agendaItem.BacklogItemId.Value];
                item.MeetingId = meeting.Id;
                item.Meeting = meeting;
                item.Title = agendaItem.Title.Trim();
                item.Description = Clean(agendaItem.Description);
                item.Status = BoardAgendaItemStatus.Scheduled;
                item.Order = order++;
                item.UpdatedAtUtc = now;
            }
            else
            {
                item = new BoardAgendaItem
                {
                    MeetingId = meeting.Id,
                    Meeting = meeting,
                    Title = agendaItem.Title.Trim(),
                    Description = Clean(agendaItem.Description),
                    Status = BoardAgendaItemStatus.Scheduled,
                    Order = order++,
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
            }
            meeting.AgendaItems.Add(item);
        }
        repository.Add(meeting);
        notifications.EnqueueMeetingCreated(meeting);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<BoardMeetingDto>> RescheduleMeetingAsync(Guid id, RescheduleBoardMeetingRequest request, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(id, cancellationToken);
        if (meeting is null) return Result<BoardMeetingDto>.Failure("Board meeting not found.");
        var validation = ValidateSchedule(meeting.Title, request.ScheduledAtUtc, request.DurationMinutes);
        if (validation is not null) return Result<BoardMeetingDto>.Failure(validation);
        ApplyReschedule(meeting, request.ScheduledAtUtc, request.DurationMinutes);
        notifications.EnqueueMeetingRescheduled(meeting, Clean(request.Reason));
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<BoardMeetingDto>> CancelMeetingAsync(Guid id, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(id, cancellationToken);
        if (meeting is null) return Result<BoardMeetingDto>.Failure("Board meeting not found.");
        if (meeting.Status == BoardMeetingStatus.Cancelled) return Result<BoardMeetingDto>.Failure("The board meeting is already cancelled.");
        meeting.Status = BoardMeetingStatus.Cancelled;
        meeting.UpdatedAtUtc = DateTimeOffset.UtcNow;
        notifications.EnqueueMeetingCancelled(meeting);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<BoardRescheduleProposalDto>> ProposeRescheduleAsync(Guid id, CreateRescheduleProposalRequest request, Guid actorUserId, string displayName, int? expectedScheduleVersion, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(id, cancellationToken);
        if (meeting is null) return Result<BoardRescheduleProposalDto>.Failure("Board meeting not found.");
        if (meeting.Status == BoardMeetingStatus.Cancelled) return Result<BoardRescheduleProposalDto>.Failure("A cancelled meeting cannot be rescheduled.");
        if (expectedScheduleVersion.HasValue && meeting.ScheduleVersion != expectedScheduleVersion.Value)
            return Result<BoardRescheduleProposalDto>.Failure("The board meeting was rescheduled. Please use the latest meeting announcement.");
        var validation = ValidateSchedule(meeting.Title, request.ProposedAtUtc, request.DurationMinutes);
        if (validation is not null) return Result<BoardRescheduleProposalDto>.Failure(validation);
        var proposal = new BoardRescheduleProposal { MeetingId = meeting.Id, ProposedAtUtc = request.ProposedAtUtc, DurationMinutes = request.DurationMinutes, Reason = Clean(request.Reason), ProposedByUserId = actorUserId, ProposedByDisplayName = displayName, CreatedAtUtc = DateTimeOffset.UtcNow };
        repository.Add(proposal);
        notifications.EnqueueRescheduleProposed(meeting, proposal);
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardRescheduleProposalDto>.Success(Map(proposal));
    }

    public async Task<Result<BoardMeetingDto>> DecideProposalAsync(Guid meetingId, Guid proposalId, bool accept, Guid actorUserId, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(meetingId, cancellationToken);
        var proposal = meeting?.RescheduleProposals.SingleOrDefault(x => x.Id == proposalId);
        if (meeting is null || proposal is null) return Result<BoardMeetingDto>.Failure("Reschedule proposal not found.");
        if (proposal.Status != RescheduleProposalStatus.Pending) return Result<BoardMeetingDto>.Failure("The proposal has already been decided.");
        proposal.Status = accept ? RescheduleProposalStatus.Accepted : RescheduleProposalStatus.Rejected;
        proposal.DecidedByUserId = actorUserId;
        proposal.DecidedAtUtc = DateTimeOffset.UtcNow;
        if (accept)
        {
            ApplyReschedule(meeting, proposal.ProposedAtUtc, proposal.DurationMinutes);
            foreach (var other in meeting.RescheduleProposals.Where(x => x.Id != proposal.Id && x.Status == RescheduleProposalStatus.Pending)) other.Status = RescheduleProposalStatus.Rejected;
            notifications.EnqueueMeetingRescheduled(meeting, proposal.Reason);
        }
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<BoardAvailabilityDto>> SetAvailabilityAsync(Guid meetingId, Guid userId, string displayName, BoardAvailabilityStatusDto status, int? expectedScheduleVersion, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(meetingId, cancellationToken);
        if (meeting is null) return Result<BoardAvailabilityDto>.Failure("Board meeting not found.");
        if (meeting.Status == BoardMeetingStatus.Cancelled) return Result<BoardAvailabilityDto>.Failure("Availability cannot be changed for a cancelled meeting.");
        if (expectedScheduleVersion.HasValue && expectedScheduleVersion != meeting.ScheduleVersion) return Result<BoardAvailabilityDto>.Failure("The meeting was rescheduled. Please respond to the new date.");
        var availability = meeting.Availabilities.SingleOrDefault(x => x.UserId == userId);
        if (availability is null)
        {
            availability = new BoardAvailability { MeetingId = meetingId, UserId = userId };
            repository.Add(availability);
        }
        availability.DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Board member" : displayName.Trim();
        availability.Status = (BoardAvailabilityStatus)status;
        availability.ScheduleVersion = meeting.ScheduleVersion;
        availability.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardAvailabilityDto>.Success(Map(availability));
    }

    public async Task<Result<BoardAgendaItemDto>> CreateAgendaItemAsync(SaveBoardAgendaItemRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var validation = await ValidateAgendaRequest(request, cancellationToken);
        if (validation is not null) return Result<BoardAgendaItemDto>.Failure(validation);
        var now = DateTimeOffset.UtcNow;
        var item = new BoardAgendaItem { Title = request.Title.Trim(), Description = Clean(request.Description), MeetingId = request.MeetingId, Status = request.MeetingId.HasValue ? BoardAgendaItemStatus.Scheduled : BoardAgendaItemStatus.Backlog, Order = request.Order, CreatedByUserId = actorUserId, CreatedAtUtc = now, UpdatedAtUtc = now };
        var meeting = request.MeetingId.HasValue
            ? await repository.GetMeetingAsync(request.MeetingId.Value, cancellationToken)
            : null;
        if (meeting is not null)
        {
            item.Meeting = meeting;
            meeting.AgendaItems.Add(item);
        }
        repository.Add(item);
        notifications.EnqueueAgendaChanged(meeting, [item], "added");
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardAgendaItemDto>.Success(Map(item));
    }

    public async Task<Result<BoardAgendaItemDto>> UpdateAgendaItemAsync(Guid itemId, SaveBoardAgendaItemRequest request, CancellationToken cancellationToken)
    {
        var item = await repository.GetAgendaItemAsync(itemId, cancellationToken);
        if (item is null) return Result<BoardAgendaItemDto>.Failure("Agenda item not found.");
        var validation = await ValidateAgendaRequest(request, cancellationToken);
        if (validation is not null) return Result<BoardAgendaItemDto>.Failure(validation);
        var meeting = item.MeetingId.HasValue
            ? await repository.GetMeetingAsync(item.MeetingId.Value, cancellationToken)
            : null;
        item.Title = request.Title.Trim(); item.Description = Clean(request.Description); item.MeetingId = request.MeetingId; item.Order = request.Order;
        if (!item.MeetingId.HasValue) item.Status = BoardAgendaItemStatus.Backlog;
        else if (item.Status == BoardAgendaItemStatus.Backlog) item.Status = BoardAgendaItemStatus.Scheduled;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        notifications.EnqueueAgendaChanged(meeting, [item], "updated");
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardAgendaItemDto>.Success(Map(item));
    }

    public async Task<Result<BoardAgendaItemDto>> DeleteAgendaItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await repository.GetAgendaItemAsync(itemId, cancellationToken);
        if (item is null) return Result<BoardAgendaItemDto>.Failure("Agenda item not found.");
        var meeting = item.MeetingId.HasValue
            ? await repository.GetMeetingAsync(item.MeetingId.Value, cancellationToken)
            : null;
        var result = Map(item);
        meeting?.AgendaItems.Remove(item);
        repository.Remove(item);
        notifications.EnqueueAgendaChanged(meeting, [item], "deleted");
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardAgendaItemDto>.Success(result);
    }

    public async Task<Result<BoardAgendaItemDto>> MoveAgendaItemAsync(Guid itemId, MoveBoardAgendaItemRequest request, CancellationToken cancellationToken)
    {
        var item = await repository.GetAgendaItemAsync(itemId, cancellationToken);
        if (item is null) return Result<BoardAgendaItemDto>.Failure("Agenda item not found.");
        var sourceMeeting = item.MeetingId.HasValue
            ? await repository.GetMeetingAsync(item.MeetingId.Value, cancellationToken)
            : null;
        var targetMeeting = request.MeetingId.HasValue
            ? await repository.GetMeetingAsync(request.MeetingId.Value, cancellationToken)
            : null;
        if (request.MeetingId.HasValue && targetMeeting is null) return Result<BoardAgendaItemDto>.Failure("Target board meeting not found.");
        if (sourceMeeting is not null && sourceMeeting.Id != targetMeeting?.Id)
        {
            sourceMeeting.AgendaItems.Remove(item);
        }
        item.MeetingId = request.MeetingId; item.Order = request.Order; item.Status = request.MeetingId.HasValue ? BoardAgendaItemStatus.Scheduled : BoardAgendaItemStatus.Backlog; item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        item.Meeting = targetMeeting;
        if (targetMeeting is not null && targetMeeting.AgendaItems.All(x => x.Id != item.Id))
        {
            targetMeeting.AgendaItems.Add(item);
        }
        if (sourceMeeting is not null && sourceMeeting.Id != targetMeeting?.Id)
        {
            notifications.EnqueueAgendaChanged(sourceMeeting, [item], request.MeetingId.HasValue ? "moved" : "moved-to-backlog");
        }
        if (targetMeeting is not null)
        {
            notifications.EnqueueAgendaChanged(targetMeeting, [item], sourceMeeting is null ? "added" : "moved");
        }
        else if (sourceMeeting is null)
        {
            notifications.EnqueueAgendaChanged(null, [item], "updated");
        }
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardAgendaItemDto>.Success(Map(item));
    }

    public async Task<Result<IReadOnlyList<BoardAgendaItemDto>>> ReorderAgendaItemsAsync(Guid meetingId, ReorderBoardAgendaItemsRequest request, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(meetingId, cancellationToken);
        if (meeting is null) return Result<IReadOnlyList<BoardAgendaItemDto>>.Failure("Board meeting not found.");
        var itemIds = request.ItemIds.Distinct().ToList();
        if (itemIds.Count != request.ItemIds.Count || itemIds.Count != meeting.AgendaItems.Count || itemIds.Any(id => meeting.AgendaItems.All(item => item.Id != id)))
        {
            return Result<IReadOnlyList<BoardAgendaItemDto>>.Failure("The agenda order must contain every meeting agenda item exactly once.");
        }
        var itemsById = meeting.AgendaItems.ToDictionary(x => x.Id);
        var previousOrders = meeting.AgendaItems.ToDictionary(x => x.Id, x => x.Order);
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < itemIds.Count; index++)
        {
            var item = itemsById[itemIds[index]];
            item.Order = index;
            item.UpdatedAtUtc = now;
        }
        var changedItems = itemIds
            .Select(id => itemsById[id])
            .Where(item => previousOrders[item.Id] != item.Order)
            .ToList();
        if (changedItems.Count > 0)
        {
            notifications.EnqueueAgendaChanged(meeting, changedItems, "reordered");
        }
        await repository.SaveChangesAsync(cancellationToken);
        var result = itemIds.Select(id => Map(itemsById[id])).ToList();
        return Result<IReadOnlyList<BoardAgendaItemDto>>.Success(result);
    }

    public async Task<Result<BoardMeetingDto>> AssignAgendaItemsAsync(Guid meetingId, AssignBoardAgendaItemsRequest request, CancellationToken cancellationToken)
    {
        var meeting = await repository.GetMeetingAsync(meetingId, cancellationToken);
        if (meeting is null) return Result<BoardMeetingDto>.Failure("Board meeting not found.");
        var itemIds = request.ItemIds.Distinct().ToList();
        if (itemIds.Count == 0) return Result<BoardMeetingDto>.Failure("Select at least one backlog item.");
        if (itemIds.Count != request.ItemIds.Count) return Result<BoardMeetingDto>.Failure("A backlog item can only be selected once.");
        var items = await repository.GetAgendaItemsAsync(itemIds, cancellationToken);
        if (items.Count != itemIds.Count || items.Any(x => x.MeetingId.HasValue)) return Result<BoardMeetingDto>.Failure("One or more selected items are no longer in the backlog.");
        var itemsById = items.ToDictionary(x => x.Id);
        var nextOrder = meeting.AgendaItems.Count == 0 ? 0 : meeting.AgendaItems.Max(x => x.Order) + 1;
        var now = DateTimeOffset.UtcNow;
        var assignedItems = new List<BoardAgendaItem>();
        foreach (var itemId in itemIds)
        {
            var item = itemsById[itemId];
            item.MeetingId = meeting.Id;
            item.Meeting = meeting;
            item.Status = BoardAgendaItemStatus.Scheduled;
            item.Order = nextOrder++;
            item.UpdatedAtUtc = now;
            meeting.AgendaItems.Add(item);
            assignedItems.Add(item);
        }
        notifications.EnqueueAgendaChanged(meeting, assignedItems, "added");
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardMeetingDto>.Success(Map(meeting));
    }

    public async Task<Result<BoardAgendaItemDto>> ChangeAgendaItemStatusAsync(Guid itemId, BoardAgendaItemStatusDto status, CancellationToken cancellationToken)
    {
        var item = await repository.GetAgendaItemAsync(itemId, cancellationToken);
        if (item is null) return Result<BoardAgendaItemDto>.Failure("Agenda item not found.");
        var meeting = item.MeetingId.HasValue
            ? await repository.GetMeetingAsync(item.MeetingId.Value, cancellationToken)
            : null;
        if (status == BoardAgendaItemStatusDto.Backlog) item.MeetingId = null;
        if (status == BoardAgendaItemStatusDto.Scheduled && !item.MeetingId.HasValue) return Result<BoardAgendaItemDto>.Failure("Assign the item to a meeting before scheduling it.");
        item.Status = (BoardAgendaItemStatus)status; item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (status == BoardAgendaItemStatusDto.Backlog)
        {
            meeting?.AgendaItems.Remove(item);
            item.Meeting = null;
        }
        notifications.EnqueueAgendaChanged(meeting, [item], status == BoardAgendaItemStatusDto.Backlog ? "moved-to-backlog" : "updated");
        await repository.SaveChangesAsync(cancellationToken);
        return Result<BoardAgendaItemDto>.Success(Map(item));
    }

    private async Task<string?> ValidateAgendaRequest(SaveBoardAgendaItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return "An agenda item title is required.";
        if (request.MeetingId.HasValue && await repository.GetMeetingAsync(request.MeetingId.Value, cancellationToken) is null) return "Board meeting not found.";
        return null;
    }

    private static string? ValidateSchedule(string title, DateTimeOffset scheduledAt, int durationMinutes)
    {
        if (string.IsNullOrWhiteSpace(title)) return "A title is required.";
        if (durationMinutes is < 15 or > 1440) return "Duration must be between 15 minutes and 24 hours.";
        if (scheduledAt == default) return "A meeting date is required.";
        return null;
    }

    private static void ApplyReschedule(BoardMeeting meeting, DateTimeOffset scheduledAt, int durationMinutes)
    {
        meeting.ScheduledAtUtc = scheduledAt; meeting.DurationMinutes = durationMinutes; meeting.ScheduleVersion++; meeting.UpdatedAtUtc = DateTimeOffset.UtcNow;
        meeting.Availabilities.Clear();
    }

    private static BoardMeetingSummaryDto MapSummary(BoardMeeting meeting) => new(meeting.Id, meeting.Title, meeting.ScheduledAtUtc, meeting.DurationMinutes, meeting.Location, (BoardMeetingStatusDto)meeting.Status, meeting.ScheduleVersion, meeting.Availabilities.Count(x => x.Status == BoardAvailabilityStatus.Available), meeting.Availabilities.Count(x => x.Status == BoardAvailabilityStatus.Unavailable));
    private static BoardMeetingDto Map(BoardMeeting meeting) => new(meeting.Id, meeting.Title, meeting.ScheduledAtUtc, meeting.DurationMinutes, meeting.Location, (BoardMeetingStatusDto)meeting.Status, meeting.ScheduleVersion, meeting.Availabilities.OrderBy(x => x.DisplayName).Select(Map).ToList(), meeting.RescheduleProposals.OrderByDescending(x => x.CreatedAtUtc).Select(Map).ToList(), meeting.AgendaItems.OrderBy(x => x.Order).Select(Map).ToList());
    private static BoardAvailabilityDto Map(BoardAvailability value) => new(value.UserId, value.DisplayName, (BoardAvailabilityStatusDto)value.Status, value.UpdatedAtUtc);
    private static BoardRescheduleProposalDto Map(BoardRescheduleProposal value) => new(value.Id, value.ProposedAtUtc, value.DurationMinutes, value.Reason, (RescheduleProposalStatusDto)value.Status, value.ProposedByUserId, value.ProposedByDisplayName, value.CreatedAtUtc);
    private static BoardAgendaItemDto Map(BoardAgendaItem value) => new(value.Id, value.MeetingId, value.Title, value.Description, (BoardAgendaItemStatusDto)value.Status, value.Order, value.UpdatedAtUtc);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
