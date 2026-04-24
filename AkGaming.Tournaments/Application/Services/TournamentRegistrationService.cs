using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TournamentRegistrationService(
    IPlayerProfileRepository playerProfileRepository,
    ITeamRepository teamRepository,
    ITournamentRegistrationRepository tournamentRegistrationRepository,
    ITournamentRepository tournamentRepository,
    IUnitOfWork unitOfWork) : ITournamentRegistrationService
{
    public async Task<IReadOnlyList<TournamentRegistrationDto>> GetTeamRegistrationsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        _ = await RequireTeamAsync(teamId, cancellationToken);

        var registrations = await tournamentRegistrationRepository.GetByTeamIdAsync(teamId, cancellationToken);
        return await MapRegistrationsAsync(registrations, cancellationToken);
    }

    public async Task<TournamentRegistrationDto?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        var registration = await tournamentRegistrationRepository.GetByIdAsync(registrationId, cancellationToken);
        return registration is null
            ? null
            : await MapRegistrationAsync(registration, cancellationToken);
    }

    public async Task<TournamentRegistrationDto> SubmitRegistrationAsync(
        Guid teamId,
        Guid tournamentId,
        string actingUserId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken = default)
    {
        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);
        var tournament = await RequireTournamentAsync(tournamentId, cancellationToken);
        EnsureTeamCanRegisterForTournament(team, tournament);

        if (await tournamentRegistrationRepository.GetByTeamAndTournamentAsync(teamId, tournamentId, cancellationToken) is not null)
        {
            throw new ConflictException($"Team '{teamId}' is already registered for tournament '{tournamentId}'.");
        }

        var selectedProfiles = await ResolveEligibleProfilesAsync(team, tournament.GameId, playerProfileIds, cancellationToken);
        var roster = CreateRoster(1, selectedProfiles);
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            TournamentId = tournamentId,
            Status = TournamentRegistrationStatus.Pending,
            Rosters = [roster]
        };
        roster.TournamentRegistrationId = registration.Id;

        await tournamentRegistrationRepository.AddAsync(registration, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapRegistrationAsync(registration, cancellationToken);
    }

    public async Task<TournamentRegistrationDto> ReviewRegistrationAsync(Guid registrationId, bool approve, string? reviewNote, CancellationToken cancellationToken = default)
    {
        var registration = await RequireRegistrationAsync(registrationId, cancellationToken);
        if (registration.Status != TournamentRegistrationStatus.Pending)
        {
            throw new ValidationException("Only pending registrations can be reviewed.");
        }

        var pendingRoster = registration.Rosters.SingleOrDefault(roster => roster.Status == RosterStatus.Pending)
                           ?? throw new ValidationException("The registration does not have a pending roster.");

        registration.Status = approve
            ? TournamentRegistrationStatus.Approved
            : TournamentRegistrationStatus.Rejected;
        registration.ReviewedAtUtc = DateTimeOffset.UtcNow;
        registration.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();
        pendingRoster.Status = approve ? RosterStatus.Approved : RosterStatus.Rejected;
        pendingRoster.ReviewedAtUtc = registration.ReviewedAtUtc;
        pendingRoster.ReviewNote = registration.ReviewNote;

        if (approve)
        {
            registration.ActiveRosterId = pendingRoster.Id;
            registration.ActiveRoster = pendingRoster;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapRegistrationAsync(registration, cancellationToken);
    }

    public async Task<TournamentRegistrationDto> SubmitRosterChangeAsync(
        Guid registrationId,
        string actingUserId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken = default)
    {
        var registration = await RequireRegistrationAsync(registrationId, cancellationToken);
        if (registration.Status != TournamentRegistrationStatus.Approved)
        {
            throw new ValidationException("Roster changes can only be submitted for approved registrations.");
        }

        if (registration.Rosters.Any(roster => roster.Status == RosterStatus.Pending))
        {
            throw new ConflictException("The registration already has a pending roster review.");
        }

        var team = registration.Team ?? await RequireTeamAsync(registration.TeamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);
        var tournament = await RequireTournamentAsync(registration.TournamentId, cancellationToken);
        EnsureTeamCanRegisterForTournament(team, tournament);
        var selectedProfiles = await ResolveEligibleProfilesAsync(team, tournament.GameId, playerProfileIds, cancellationToken);

        var nextVersion = registration.Rosters.Count == 0 ? 1 : registration.Rosters.Max(roster => roster.Version) + 1;
        var roster = CreateRoster(nextVersion, selectedProfiles);
        roster.TournamentRegistrationId = registration.Id;
        registration.Rosters.Add(roster);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapRegistrationAsync(registration, cancellationToken);
    }

    public async Task<TournamentRegistrationDto> ReviewRosterAsync(
        Guid registrationId,
        Guid rosterId,
        bool approve,
        string? reviewNote,
        CancellationToken cancellationToken = default)
    {
        var registration = await RequireRegistrationAsync(registrationId, cancellationToken);
        if (registration.Status != TournamentRegistrationStatus.Approved)
        {
            throw new ValidationException("Only approved registrations can receive roster reviews.");
        }

        var roster = registration.Rosters.SingleOrDefault(candidate => candidate.Id == rosterId)
                     ?? throw new NotFoundException($"Roster '{rosterId}' was not found.");
        if (roster.Status != RosterStatus.Pending)
        {
            throw new ValidationException("Only pending rosters can be reviewed.");
        }

        roster.Status = approve ? RosterStatus.Approved : RosterStatus.Rejected;
        roster.ReviewedAtUtc = DateTimeOffset.UtcNow;
        roster.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim();

        if (approve)
        {
            registration.ActiveRosterId = roster.Id;
            registration.ActiveRoster = roster;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapRegistrationAsync(registration, cancellationToken);
    }

    private async Task<Team> RequireTeamAsync(Guid teamId, CancellationToken cancellationToken)
        => await teamRepository.GetByIdAsync(teamId, cancellationToken)
           ?? throw new NotFoundException($"Team '{teamId}' was not found.");

    private async Task<Tournament> RequireTournamentAsync(Guid tournamentId, CancellationToken cancellationToken)
        => await tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
           ?? throw new NotFoundException($"Tournament '{tournamentId}' was not found.");

    private async Task<TournamentRegistration> RequireRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
        => await tournamentRegistrationRepository.GetByIdAsync(registrationId, cancellationToken)
           ?? throw new NotFoundException($"Tournament registration '{registrationId}' was not found.");

    private static void EnsureCanEditTeam(Team team, string actingUserId)
    {
        if (string.IsNullOrWhiteSpace(actingUserId))
        {
            throw new ValidationException("User id is required.");
        }

        if (!team.Memberships.Any(member =>
                string.Equals(member.UserId, actingUserId.Trim(), StringComparison.OrdinalIgnoreCase)
                && (member.Role == TeamRole.Owner || member.Role == TeamRole.Editor)))
        {
            throw new ForbiddenException("Only owners and editors can manage registrations.");
        }
    }

    private static void EnsureTeamCanRegisterForTournament(Team team, Tournament tournament)
    {
        if (!string.Equals(team.GameId, tournament.GameId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Teams can only register for tournaments in their game.");
        }
    }

    private async Task<IReadOnlyList<PlayerProfile>> ResolveEligibleProfilesAsync(
        Team team,
        string tournamentGameId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken)
    {
        if (playerProfileIds.Count == 0)
        {
            throw new ValidationException("A roster must contain at least one player profile.");
        }

        var requestedIds = playerProfileIds.Distinct().ToArray();
        var profiles = await playerProfileRepository.GetByIdsAsync(requestedIds, cancellationToken);
        if (profiles.Count != requestedIds.Length)
        {
            throw new ValidationException("One or more player profiles were not found.");
        }

        var memberUserIds = team.Memberships
            .Select(membership => membership.UserId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in profiles)
        {
            if (!string.Equals(profile.GameId, tournamentGameId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("All selected player profiles must belong to the tournament game.");
            }

            var isAllowed = profile.Type switch
            {
                PlayerProfileType.Guest => profile.TeamId == team.Id,
                PlayerProfileType.User => !string.IsNullOrWhiteSpace(profile.UserId) && memberUserIds.Contains(profile.UserId),
                _ => false
            };

            if (!isAllowed)
            {
                throw new ValidationException("A selected player profile is not available to the registering team.");
            }
        }

        return profiles;
    }

    private static Roster CreateRoster(int version, IReadOnlyList<PlayerProfile> playerProfiles)
    {
        var createdUtc = DateTimeOffset.UtcNow;

        return new Roster
        {
            Id = Guid.NewGuid(),
            Version = version,
            Status = RosterStatus.Pending,
            SubmittedAtUtc = createdUtc,
            PlayerSnapshots = playerProfiles
                .Select(playerProfile => new RosterPlayerSnapshot
                {
                    Id = Guid.NewGuid(),
                    SourcePlayerProfileId = playerProfile.Id,
                    PlayerProfileType = playerProfile.Type,
                    Name = playerProfile.Name,
                    UserId = playerProfile.UserId,
                    SourcePlayerProfileLastRevisionUtc = playerProfile.LastRevisionUtc,
                    SnapshotCreatedUtc = createdUtc
                })
                .ToList()
        };
    }

    private async Task<TournamentRegistrationDto> MapRegistrationAsync(TournamentRegistration registration, CancellationToken cancellationToken)
    {
        var currentProfiles = await LoadCurrentProfilesAsync(registration.Rosters, cancellationToken);
        return registration.ToDto(currentProfiles);
    }

    private async Task<IReadOnlyList<TournamentRegistrationDto>> MapRegistrationsAsync(
        IReadOnlyList<TournamentRegistration> registrations,
        CancellationToken cancellationToken)
    {
        var currentProfiles = await LoadCurrentProfilesAsync(registrations.SelectMany(registration => registration.Rosters), cancellationToken);
        return registrations
            .OrderByDescending(registration => registration.SubmittedAtUtc)
            .Select(registration => registration.ToDto(currentProfiles))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, PlayerProfile>> LoadCurrentProfilesAsync(
        IEnumerable<Roster> rosters,
        CancellationToken cancellationToken)
    {
        var ids = rosters
            .SelectMany(roster => roster.PlayerSnapshots)
            .Where(snapshot => snapshot.SourcePlayerProfileId.HasValue)
            .Select(snapshot => snapshot.SourcePlayerProfileId!.Value)
            .Distinct()
            .ToArray();

        var currentProfiles = await playerProfileRepository.GetByIdsAsync(ids, cancellationToken);
        return currentProfiles.ToDictionary(profile => profile.Id);
    }
}
