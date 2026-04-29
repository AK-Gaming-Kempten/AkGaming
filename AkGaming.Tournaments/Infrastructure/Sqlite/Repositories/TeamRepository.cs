using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

public sealed class TeamRepository(TournamentDbContext dbContext) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        => dbContext.Teams
            .Include(team => team.Memberships)
            .Include(team => team.InviteKeys)
            .Include(team => team.GuestPlayerProfiles)
            .FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task<Team?> GetByInviteKeyAsync(Guid teamId, string key, CancellationToken cancellationToken = default)
        => dbContext.Teams
            .Include(team => team.Memberships)
            .Include(team => team.InviteKeys)
            .Include(team => team.GuestPlayerProfiles)
            .FirstOrDefaultAsync(
                team => team.Id == teamId
                        && team.InviteKeys.Any(invite => invite.Key == key),
                cancellationToken);

    public Task<TeamInviteKey?> GetInviteKeyAsync(Guid teamId, string key, CancellationToken cancellationToken = default)
        => dbContext.TeamInviteKeys.FirstOrDefaultAsync(
            invite => invite.TeamId == teamId && invite.Key == key,
            cancellationToken);

    public async Task<IReadOnlyList<Team>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => await dbContext.Teams
            .Include(team => team.Memberships)
            .Include(team => team.InviteKeys)
            .Include(team => team.GuestPlayerProfiles)
            .Where(team => team.Memberships.Any(member => member.UserId == userId))
            .OrderBy(team => team.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> IsUserMemberAsync(Guid teamId, string userId, CancellationToken cancellationToken = default)
        => dbContext.TeamMemberships.AnyAsync(
            membership => membership.TeamId == teamId && membership.UserId == userId,
            cancellationToken);

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
        => await dbContext.Teams.AddAsync(team, cancellationToken);

    public async Task AddMembershipAsync(TeamMembership membership, CancellationToken cancellationToken = default)
        => await dbContext.TeamMemberships.AddAsync(membership, cancellationToken);

    public async Task AddInviteKeyAsync(TeamInviteKey inviteKey, CancellationToken cancellationToken = default)
        => await dbContext.TeamInviteKeys.AddAsync(inviteKey, cancellationToken);
}
