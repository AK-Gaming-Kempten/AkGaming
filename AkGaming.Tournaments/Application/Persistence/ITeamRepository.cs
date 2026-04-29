using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Persistence;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<Team?> GetByInviteKeyAsync(Guid teamId, string key, CancellationToken cancellationToken = default);
    Task<TeamInviteKey?> GetInviteKeyAsync(Guid teamId, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Team>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid teamId, string userId, CancellationToken cancellationToken = default);
    Task AddMembershipAsync(TeamMembership membership, CancellationToken cancellationToken = default);
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task AddInviteKeyAsync(TeamInviteKey inviteKey, CancellationToken cancellationToken = default);
}
