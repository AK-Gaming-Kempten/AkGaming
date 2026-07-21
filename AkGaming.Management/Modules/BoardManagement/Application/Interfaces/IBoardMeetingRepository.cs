using AkGaming.Management.Modules.BoardManagement.Domain.Entities;

namespace AkGaming.Management.Modules.BoardManagement.Application.Interfaces;

public interface IBoardMeetingRepository
{
    Task<IReadOnlyList<BoardMeeting>> GetMeetingsAsync(CancellationToken cancellationToken);
    Task<BoardMeeting?> GetMeetingAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoardAgendaItem>> GetBacklogAsync(CancellationToken cancellationToken);
    Task<BoardAgendaItem?> GetAgendaItemAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<BoardAgendaItem>> GetAgendaItemsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IBoardNotificationOutbox
{
    void EnqueueMeetingCreated(BoardMeeting meeting);
    void EnqueueMeetingRescheduled(BoardMeeting meeting, string? reason);
    void EnqueueMeetingCancelled(BoardMeeting meeting);
    void EnqueueRescheduleProposed(BoardMeeting meeting, BoardRescheduleProposal proposal);
    void EnqueueAgendaChanged(BoardMeeting? meeting, IReadOnlyCollection<BoardAgendaItem> changedItems, string action);
}
