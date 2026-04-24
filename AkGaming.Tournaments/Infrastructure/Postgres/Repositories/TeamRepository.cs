using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Repositories;

public sealed class TeamRepository(TournamentDbContext dbContext) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        => dbContext.Teams
            .Include(team => team.Memberships)
            .Include(team => team.GuestPlayerProfiles)
            .FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
        => await dbContext.Teams.AddAsync(team, cancellationToken);
}
