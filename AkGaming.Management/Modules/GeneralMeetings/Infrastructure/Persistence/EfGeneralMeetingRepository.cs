using AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;
using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;

public sealed class EfGeneralMeetingRepository(GeneralMeetingsDbContext db) : IGeneralMeetingRepository
{
    public async Task<IReadOnlyList<GeneralMeeting>> GetAllAsync(CancellationToken cancellationToken)
    {
        var meetings = await db.Meetings.AsNoTracking().ToListAsync(cancellationToken);
        return meetings.OrderByDescending(x => x.ScheduledAt).ToList();
    }
    public async Task<GeneralMeeting?> GetAsync(Guid id, CancellationToken cancellationToken) => await MeetingQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<Ballot?> GetBallotAsync(Guid id, CancellationToken cancellationToken) => await db.Ballots.Include(x => x.Options).Include(x => x.Entitlements).Include(x => x.Credentials).Include(x => x.Votes).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<Guid?> GetMeetingIdForBallotAsync(Guid id, CancellationToken cancellationToken) => await db.Ballots.Where(x => x.Id == id).Select(x => (Guid?)x.AgendaItem.MeetingId).SingleOrDefaultAsync(cancellationToken);
    public void Add<TEntity>(TEntity entity) where TEntity : class => db.Set<TEntity>().Add(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private IQueryable<GeneralMeeting> MeetingQuery() => db.Meetings
        .Include(x => x.AgendaItems).ThenInclude(x => x.Ballots).ThenInclude(x => x.Options)
        .Include(x => x.AgendaItems).ThenInclude(x => x.Ballots).ThenInclude(x => x.Entitlements)
        .Include(x => x.AgendaItems).ThenInclude(x => x.Ballots).ThenInclude(x => x.Credentials)
        .Include(x => x.AgendaItems).ThenInclude(x => x.Ballots).ThenInclude(x => x.Votes)
        .Include(x => x.Attendees).Include(x => x.AuditEvents).Include(x => x.InvitationDispatches).Include(x => x.ProtocolRevisions).AsSplitQuery();
}
