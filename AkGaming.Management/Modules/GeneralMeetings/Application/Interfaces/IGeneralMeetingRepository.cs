using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;

namespace AkGaming.Management.Modules.GeneralMeetings.Application.Interfaces;

public interface IGeneralMeetingRepository
{
    Task<IReadOnlyList<GeneralMeeting>> GetAllAsync(CancellationToken cancellationToken);
    Task<GeneralMeeting?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Ballot?> GetBallotAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> GetMeetingIdForBallotAsync(Guid id, CancellationToken cancellationToken);
    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
