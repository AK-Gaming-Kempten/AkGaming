using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Repositories;

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
