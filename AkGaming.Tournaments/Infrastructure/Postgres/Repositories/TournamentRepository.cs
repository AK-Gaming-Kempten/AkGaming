using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Postgres.Repositories;

public sealed class TournamentRepository(TournamentDbContext dbContext) : ITournamentRepository
{
    public async Task<IReadOnlyList<Tournament>> GetAllAsync(bool includeHidden = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Tournaments
            .Include(tournament => tournament.Game)
            .Include(tournament => tournament.InfoSections)
            .Include(tournament => tournament.RegistrationRules)
            .Include(tournament => tournament.Registrations)
            .AsQueryable();

        if (!includeHidden)
        {
            query = query.Where(tournament => tournament.IsVisible);
        }

        var tournaments = await query.ToListAsync(cancellationToken);

        return tournaments;
    }

    public Task<Tournament?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        => dbContext.Tournaments
            .Include(tournament => tournament.Game)
            .Include(tournament => tournament.InfoSections)
            .Include(tournament => tournament.RegistrationRules)
            .Include(tournament => tournament.Registrations)
            .FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);

    public Task<Tournament?> GetBySlugAsync(string slug, bool includeHidden = false, CancellationToken cancellationToken = default)
        => dbContext.Tournaments
            .Include(tournament => tournament.Game)
            .Include(tournament => tournament.InfoSections)
            .Include(tournament => tournament.RegistrationRules)
            .Include(tournament => tournament.Registrations)
            .FirstOrDefaultAsync(
                tournament => tournament.Slug == slug && (includeHidden || tournament.IsVisible),
                cancellationToken);

    public Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default)
        => dbContext.Tournaments.AddAsync(tournament, cancellationToken).AsTask();

    public async Task ReplaceInfoSectionsAsync(Guid tournamentId, IReadOnlyList<TournamentInfoSection> sections, CancellationToken cancellationToken = default)
    {
        var trackedEntries = dbContext.ChangeTracker
            .Entries<TournamentInfoSection>()
            .Where(entry => entry.Entity.TournamentId == tournamentId)
            .ToList();

        foreach (var entry in trackedEntries)
        {
            entry.State = EntityState.Detached;
        }

        await dbContext.TournamentInfoSections
            .Where(section => section.TournamentId == tournamentId)
            .ExecuteDeleteAsync(cancellationToken);

        if (sections.Count > 0)
        {
            await dbContext.TournamentInfoSections.AddRangeAsync(sections, cancellationToken);
        }
    }

    public async Task ReplaceRegistrationRulesAsync(Guid tournamentId, IReadOnlyList<TournamentRegistrationRule> rules, CancellationToken cancellationToken = default)
    {
        var trackedEntries = dbContext.ChangeTracker
            .Entries<TournamentRegistrationRule>()
            .Where(entry => entry.Entity.TournamentId == tournamentId)
            .ToList();

        foreach (var entry in trackedEntries)
        {
            entry.State = EntityState.Detached;
        }

        await dbContext.TournamentRegistrationRules
            .Where(rule => rule.TournamentId == tournamentId)
            .ExecuteDeleteAsync(cancellationToken);

        if (rules.Count > 0)
        {
            await dbContext.TournamentRegistrationRules.AddRangeAsync(rules, cancellationToken);
        }
    }

    public void Delete(Tournament tournament)
        => dbContext.Tournaments.Remove(tournament);
}
