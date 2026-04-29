using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.RegistrationRules;
using AkGaming.Tournaments.Application.UseCases;
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
    IGameRankSystemRegistry rankSystemRegistry,
    IUnitOfWork unitOfWork) : ITournamentRegistrationService
{
    public async Task<IReadOnlyList<TournamentRegistrationDto>> GetTeamRegistrationsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        _ = await RequireTeamAsync(teamId, cancellationToken);

        var registrations = await tournamentRegistrationRepository.GetByTeamIdAsync(teamId, cancellationToken);
        return await MapRegistrationsAsync(registrations, cancellationToken);
    }

    public async Task<IReadOnlyList<TournamentRegistrationDto>> GetTournamentRegistrationsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        _ = await RequireTournamentAsync(tournamentId, cancellationToken);

        var registrations = await tournamentRegistrationRepository.GetByTournamentIdAsync(tournamentId, cancellationToken);
        return await MapRegistrationsAsync(registrations, cancellationToken);
    }

    public async Task<TournamentRegistrationDto?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        var registration = await tournamentRegistrationRepository.GetByIdAsync(registrationId, cancellationToken);
        return registration is null
            ? null
            : await MapRegistrationAsync(registration, cancellationToken);
    }

    public async Task<TournamentRegistrationEligibilityDto> GetRegistrationEligibilityAsync(
        Guid teamId,
        Guid tournamentId,
        string actingUserId,
        IReadOnlyCollection<Guid> playerProfileIds,
        CancellationToken cancellationToken = default)
    {
        var team = await RequireTeamAsync(teamId, cancellationToken);
        var tournament = await RequireTournamentAsync(tournamentId, cancellationToken);
        var availableProfiles = await LoadAvailableProfilesAsync(team, tournament.GameId, cancellationToken);
        var selectedIds = NormalizeSelectedProfileIds(playerProfileIds, availableProfiles);
        var existingRegistration = await tournamentRegistrationRepository.GetByTeamAndTournamentAsync(teamId, tournamentId, cancellationToken);
        var canEditTeam = CanEditTeam(team, actingUserId);

        return EvaluateRegistrationEligibility(
            team,
            tournament,
            availableProfiles,
            selectedIds,
            canEditTeam,
            existingRegistration);
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
        EnsureRosterQualifies(team, tournament, selectedProfiles);
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

    public async Task DeleteRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        var registration = await RequireRegistrationAsync(registrationId, cancellationToken);
        tournamentRegistrationRepository.Delete(registration);
        await unitOfWork.SaveChangesAsync(cancellationToken);
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
        EnsureRosterQualifies(team, tournament, selectedProfiles);

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

        if (!CanEditTeam(team, actingUserId))
        {
            throw new ForbiddenException("Only owners and editors can manage registrations.");
        }
    }

    private static bool CanEditTeam(Team team, string actingUserId)
        => !string.IsNullOrWhiteSpace(actingUserId)
           && team.Memberships.Any(member =>
               string.Equals(member.UserId, actingUserId.Trim(), StringComparison.OrdinalIgnoreCase)
               && (member.Role == TeamRole.Owner || member.Role == TeamRole.Editor));

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

    private async Task<IReadOnlyList<PlayerProfile>> LoadAvailableProfilesAsync(
        Team team,
        string tournamentGameId,
        CancellationToken cancellationToken)
    {
        var memberUserIds = team.Memberships
            .Select(member => member.UserId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var memberProfiles = await playerProfileRepository.GetByUsersAndGameAsync(memberUserIds, tournamentGameId, cancellationToken);
        var guestProfiles = team.GuestPlayerProfiles
            .Where(profile => string.Equals(profile.GameId, tournamentGameId, StringComparison.OrdinalIgnoreCase));

        return memberProfiles
            .Concat(guestProfiles)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlySet<Guid> NormalizeSelectedProfileIds(
        IReadOnlyCollection<Guid> requestedProfileIds,
        IReadOnlyList<PlayerProfile> availableProfiles)
    {
        if (requestedProfileIds.Count > 0)
            return requestedProfileIds.Distinct().ToHashSet();

        return availableProfiles.Select(profile => profile.Id).ToHashSet();
    }

    private void EnsureRosterQualifies(Team team, Tournament tournament, IReadOnlyList<PlayerProfile> selectedProfiles)
    {
        var eligibility = EvaluateRegistrationEligibility(
            team,
            tournament,
            selectedProfiles,
            selectedProfiles.Select(profile => profile.Id).ToHashSet(),
            canEditTeam: true,
            existingRegistration: null);

        if (eligibility.CanSubmit)
            return;

        var failedChecks = eligibility.Checks
            .Where(check => !check.Passed)
            .Select(check => check.Description)
            .ToArray();

        throw new ValidationException(string.Join(" ", failedChecks));
    }

    private TournamentRegistrationEligibilityDto EvaluateRegistrationEligibility(
        Team team,
        Tournament tournament,
        IReadOnlyList<PlayerProfile> availableProfiles,
        IReadOnlySet<Guid> selectedIds,
        bool canEditTeam,
        TournamentRegistration? existingRegistration)
    {
        var rankSystem = rankSystemRegistry.GetRankSystem(tournament.GameId);
        var selectedProfiles = availableProfiles.Where(profile => selectedIds.Contains(profile.Id)).ToList();
        var activeRules = GetEffectiveRegistrationRules(tournament);
        var checks = new List<TournamentRegistrationRuleCheckDto>();
        var unknownSelectedCount = selectedIds.Count - selectedProfiles.Count;

        checks.Add(new TournamentRegistrationRuleCheckDto(
            "Permission",
            canEditTeam ? "You can submit registrations for this team." : "Only team owners and editors can submit registrations.",
            canEditTeam,
            canEditTeam ? "positive" : "warn"));

        checks.Add(new TournamentRegistrationRuleCheckDto(
            "Tournament",
            string.Equals(team.GameId, tournament.GameId, StringComparison.OrdinalIgnoreCase)
                ? "The team belongs to this tournament's game."
                : "The team belongs to a different game.",
            string.Equals(team.GameId, tournament.GameId, StringComparison.OrdinalIgnoreCase),
            string.Equals(team.GameId, tournament.GameId, StringComparison.OrdinalIgnoreCase) ? "positive" : "warn"));

        checks.Add(new TournamentRegistrationRuleCheckDto(
            "Existing registration",
            existingRegistration is null
                ? "This team has not registered for this tournament yet."
                : $"This team already has a {existingRegistration.Status} registration for this tournament.",
            existingRegistration is null,
            existingRegistration is null ? "positive" : "warn"));

        if (unknownSelectedCount > 0)
        {
            checks.Add(new TournamentRegistrationRuleCheckDto(
                "Roster profiles",
                $"{unknownSelectedCount} selected player profile id(s) are not available to this team.",
                false,
                "warn"));
        }

        var maxPlayerRankRating = activeRules
            .OfType<MaxPlayerRankRatingRegistrationRule>()
            .Select(rule => (int?)rule.Value)
            .Min();
        var playerEligibility = availableProfiles
            .Select(profile => CreatePlayerEligibility(profile, selectedIds.Contains(profile.Id), rankSystem, maxPlayerRankRating))
            .ToList();

        foreach (var rule in activeRules.OrderBy(rule => rule.SortOrder))
        {
            checks.Add(EvaluateRule(rule, selectedProfiles.Count, playerEligibility, rankSystem));
        }

        return new TournamentRegistrationEligibilityDto(
            tournament.Id,
            team.Id,
            checks.All(check => check.Passed),
            canEditTeam,
            existingRegistration?.Status.ToString(),
            activeRules.OrderBy(rule => rule.SortOrder).Select(rule => ToRuleDto(rule, rankSystem)).ToList(),
            playerEligibility,
            checks);
    }

    private static TournamentRegistrationRuleCheckDto EvaluateRule(
        TournamentRegistrationRule rule,
        int selectedPlayerCount,
        IReadOnlyList<TournamentRegistrationPlayerEligibilityDto> playerEligibility,
        IGameRankSystem rankSystem)
        => rule switch
        {
            MinPlayersPerTeamRegistrationRule minRule => EvaluateMinPlayersRule(minRule, selectedPlayerCount),
            MaxPlayersPerTeamRegistrationRule maxRule => EvaluateMaxPlayersRule(maxRule, selectedPlayerCount),
            MaxPlayerRankRatingRegistrationRule maxRankRule => EvaluateMaxPlayerRankRule(maxRankRule, playerEligibility, rankSystem),
            MaxTeamAverageRankRatingRegistrationRule maxAverageRule => EvaluateMaxTeamAverageRule(maxAverageRule, playerEligibility, rankSystem),
            _ => new TournamentRegistrationRuleCheckDto("Unknown rule", "This tournament has an unsupported registration rule.", false, "warn")
        };

    private static TournamentRegistrationRuleCheckDto EvaluateMinPlayersRule(
        MinPlayersPerTeamRegistrationRule rule,
        int selectedPlayerCount)
    {
        var passed = selectedPlayerCount >= rule.Value;
        return new TournamentRegistrationRuleCheckDto(
            "Minimum players",
            passed
                ? $"{selectedPlayerCount} selected players meets the minimum of {rule.Value}."
                : $"Select at least {rule.Value} players.",
            passed,
            passed ? "positive" : "warn");
    }

    private static TournamentRegistrationRuleCheckDto EvaluateMaxPlayersRule(
        MaxPlayersPerTeamRegistrationRule rule,
        int selectedPlayerCount)
    {
        var passed = selectedPlayerCount <= rule.Value;
        return new TournamentRegistrationRuleCheckDto(
            "Maximum players",
            passed
                ? $"{selectedPlayerCount} selected players is within the maximum of {rule.Value}."
                : $"Select no more than {rule.Value} players.",
            passed,
            passed ? "positive" : "warn");
    }

    private static TournamentRegistrationRuleCheckDto EvaluateMaxPlayerRankRule(
        MaxPlayerRankRatingRegistrationRule rule,
        IReadOnlyList<TournamentRegistrationPlayerEligibilityDto> playerEligibility,
        IGameRankSystem rankSystem)
    {
        var cap = rankSystem.DescribeRating(rule.Value);
        var selectedPlayers = playerEligibility.Where(player => player.Selected).ToList();
        var passed = selectedPlayers.All(player => player.RankRating.HasValue && player.RankRating.Value <= rule.Value);

        return new TournamentRegistrationRuleCheckDto(
            "Player rank cap",
            passed
                ? $"Every selected player is at or below {cap.Label}."
                : $"Every selected player must have known MMR at or below {cap.Label}.",
            passed,
            passed ? "positive" : "warn");
    }

    private static TournamentRegistrationRuleCheckDto EvaluateMaxTeamAverageRule(
        MaxTeamAverageRankRatingRegistrationRule rule,
        IReadOnlyList<TournamentRegistrationPlayerEligibilityDto> playerEligibility,
        IGameRankSystem rankSystem)
    {
        var cap = rankSystem.DescribeRating(rule.Value);
        var selectedRatings = playerEligibility
            .Where(player => player.Selected)
            .Select(player => player.RankRating)
            .ToList();
        var hasAllRatings = selectedRatings.Count > 0 && selectedRatings.All(value => value.HasValue);
        var averageRating = hasAllRatings ? selectedRatings.Average(value => value!.Value) : double.NaN;
        var passed = hasAllRatings && averageRating <= rule.Value;

        return new TournamentRegistrationRuleCheckDto(
            "Average rank cap",
            passed
                ? $"Selected roster average is at or below {cap.Label}."
                : $"Selected roster needs known MMR with an average at or below {cap.Label}.",
            passed,
            passed ? "positive" : "warn");
    }

    private static TournamentRegistrationRuleDto ToRuleDto(TournamentRegistrationRule rule, IGameRankSystem rankSystem)
        => rule switch
        {
            MinPlayersPerTeamRegistrationRule => new TournamentRegistrationRuleDto("MinPlayersPerTeam", "Minimum players", rule.Value, rule.Value.ToString()),
            MaxPlayersPerTeamRegistrationRule => new TournamentRegistrationRuleDto("MaxPlayersPerTeam", "Maximum players", rule.Value, rule.Value.ToString()),
            MaxPlayerRankRatingRegistrationRule => new TournamentRegistrationRuleDto("MaxPlayerRankRating", "Maximum player MMR", rule.Value, $"{rankSystem.DescribeRating(rule.Value).Label} ({rule.Value} MMR)"),
            MaxTeamAverageRankRatingRegistrationRule => new TournamentRegistrationRuleDto("MaxTeamAverageRankRating", "Maximum team average MMR", rule.Value, $"{rankSystem.DescribeRating(rule.Value).Label} ({rule.Value} MMR)"),
            _ => new TournamentRegistrationRuleDto("Unknown", "Unknown rule", rule.Value, rule.Value.ToString())
        };

    private static IReadOnlyList<TournamentRegistrationRule> GetEffectiveRegistrationRules(Tournament tournament)
    {
        if (tournament.RegistrationRules.Count > 0)
            return tournament.RegistrationRules.OrderBy(rule => rule.SortOrder).ToList();

        return
        [
            new MinPlayersPerTeamRegistrationRule { SortOrder = 0, Value = 1 },
            new MaxPlayersPerTeamRegistrationRule { SortOrder = 1, Value = 99 }
        ];
    }

    private static TournamentRegistrationPlayerEligibilityDto CreatePlayerEligibility(
        PlayerProfile profile,
        bool selected,
        IGameRankSystem rankSystem,
        int? maxPlayerRankRating)
    {
        var reasons = new List<string>();
        var hasRank = rankSystem.TryDescribeRating(profile.RankRating, out var rank);
        var rankLabel = hasRank ? rank.Label : "MMR missing";

        if (maxPlayerRankRating is int maximumRating)
        {
            if (!hasRank)
            {
                reasons.Add("MMR is required for this tournament.");
            }
            else if (rank.Rating > maximumRating)
            {
                reasons.Add($"MMR is above the player cap of {rankSystem.DescribeRating(maximumRating).Label}.");
            }
        }

        return new TournamentRegistrationPlayerEligibilityDto(
            profile.Id,
            profile.Name,
            profile.Type.ToDto(),
            profile.UserId,
            profile.RankRating,
            rankLabel,
            hasRank ? rank.Rank : null,
            hasRank ? rank.Division : null,
            hasRank ? rank.Points : null,
            selected,
            reasons.Count == 0,
            reasons);
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
