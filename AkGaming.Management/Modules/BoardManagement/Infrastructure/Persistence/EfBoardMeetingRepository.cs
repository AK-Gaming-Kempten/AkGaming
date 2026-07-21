using AkGaming.Management.Modules.BoardManagement.Application.Interfaces;
using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;

public sealed class EfBoardMeetingRepository(BoardManagementDbContext dbContext) : IBoardMeetingRepository
{
    public async Task<IReadOnlyList<BoardMeeting>> GetMeetingsAsync(CancellationToken cancellationToken)
    {
        var meetings = await Query().AsNoTracking().ToListAsync(cancellationToken);
        return meetings.OrderByDescending(x => x.ScheduledAtUtc).ToList();
    }

    public Task<BoardMeeting?> GetMeetingAsync(Guid id, CancellationToken cancellationToken) => Query().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<BoardAgendaItem>> GetBacklogAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.AgendaItems
            .AsNoTracking()
            .Where(x => x.MeetingId == null)
            .ToListAsync(cancellationToken);
        return items
            .OrderBy(x => x.Order)
            .ThenBy(x => x.CreatedAtUtc)
            .ToList();
    }
    public Task<BoardAgendaItem?> GetAgendaItemAsync(Guid id, CancellationToken cancellationToken) => dbContext.AgendaItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<BoardAgendaItem>> GetAgendaItemsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return await dbContext.AgendaItems.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
    }
    public void Add<TEntity>(TEntity entity) where TEntity : class => dbContext.Set<TEntity>().Add(entity);
    public void Remove<TEntity>(TEntity entity) where TEntity : class => dbContext.Set<TEntity>().Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
    private IQueryable<BoardMeeting> Query() => dbContext.Meetings.Include(x => x.Availabilities).Include(x => x.RescheduleProposals).Include(x => x.AgendaItems).AsSplitQuery();
}
