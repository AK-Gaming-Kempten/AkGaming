using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Application.Services;

internal static class MappingExtensions
{
    public static GameDto ToDto(this Game game)
        => new(game.Id, game.Name, game.LogoAssetId);

    public static PlayerProfileDto ToDto(this PlayerProfile playerProfile)
        => new(
            playerProfile.Id,
            playerProfile.GameId,
            playerProfile.TeamId,
            playerProfile.Type.ToDto(),
            playerProfile.Name,
            playerProfile.RankRating,
            playerProfile.UserId,
            playerProfile.LogoAssetId,
            playerProfile.LastRevisionUtc);

    public static TeamMembershipDto ToDto(this TeamMembership membership)
        => new(membership.UserId, membership.Role.ToDto(), membership.JoinedAtUtc);

    public static TeamDto ToDto(this Team team)
        => new(
            team.Id,
            team.GameId,
            team.Name,
            team.LogoAssetId,
            team.BannerAssetId,
            team.PrimaryColor,
            team.Memberships
                .OrderBy(member => member.Role)
                .ThenBy(member => member.UserId, StringComparer.OrdinalIgnoreCase)
                .Select(member => member.ToDto())
                .ToList(),
            team.GuestPlayerProfiles
                .OrderBy(profile => profile.GameId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
                .Select(profile => profile.ToDto())
                .ToList());

    public static TeamInviteKeyDto ToDto(this TeamInviteKey inviteKey)
        => new(
            inviteKey.Id,
            inviteKey.TeamId,
            inviteKey.Key,
            inviteKey.RemainingUses,
            inviteKey.CreatedUtc,
            inviteKey.RevokedUtc);

    public static TournamentSummaryDto ToSummaryDto(this Tournament tournament)
        => new(
            tournament.Id,
            tournament.Slug,
            tournament.GameId,
            tournament.Game?.Name ?? tournament.GameId,
            tournament.Name,
            tournament.LogoAssetId,
            tournament.BannerAssetId,
            tournament.PrimaryColor,
            tournament.Status.ToDto(),
            tournament.RegistrationOpenUtc,
            tournament.RegistrationClosedUtc,
            tournament.StartUtc,
            tournament.EndUtc,
            tournament.Registrations.Count);

    public static TournamentDto ToDto(this Tournament tournament)
        => new(
            tournament.Id,
            tournament.Slug,
            tournament.GameId,
            tournament.Game?.Name ?? tournament.GameId,
            tournament.Name,
            tournament.LogoAssetId,
            tournament.BannerAssetId,
            tournament.PrimaryColor,
            tournament.Status.ToDto(),
            tournament.RegistrationOpenUtc,
            tournament.RegistrationClosedUtc,
            tournament.StartUtc,
            tournament.EndUtc,
            tournament.InfoSections
                .OrderBy(section => section.SortOrder)
                .ThenBy(section => section.Header, StringComparer.OrdinalIgnoreCase)
                .Select(section => section.ToDto())
                .ToList(),
            tournament.RegistrationRules
                .OrderBy(rule => rule.SortOrder)
                .Select(rule => rule.ToDto())
                .ToList());

    public static TournamentInfoSectionDto ToDto(this TournamentInfoSection section)
        => new(section.Id, section.Header, section.ContentMarkdown, section.SortOrder);

    public static TournamentRegistrationRuleDto ToDto(this TournamentRegistrationRule rule)
        => rule switch
        {
            MinPlayersPerTeamRegistrationRule => new TournamentRegistrationRuleDto("MinPlayersPerTeam", "Minimum players", rule.Value, rule.Value.ToString()),
            MaxPlayersPerTeamRegistrationRule => new TournamentRegistrationRuleDto("MaxPlayersPerTeam", "Maximum players", rule.Value, rule.Value.ToString()),
            MaxPlayerRankRatingRegistrationRule => new TournamentRegistrationRuleDto("MaxPlayerRankRating", "Maximum player MMR", rule.Value, rule.Value.ToString()),
            MaxTeamAverageRankRatingRegistrationRule => new TournamentRegistrationRuleDto("MaxTeamAverageRankRating", "Maximum team average MMR", rule.Value, rule.Value.ToString()),
            _ => new TournamentRegistrationRuleDto("Unknown", "Unknown rule", rule.Value, rule.Value.ToString())
        };

    public static TournamentRegistrationDto ToDto(
        this TournamentRegistration registration,
        IReadOnlyDictionary<Guid, PlayerProfile> currentProfiles)
        => new(
            registration.Id,
            registration.TournamentId,
            registration.TeamId,
            registration.Status.ToDto(),
            registration.SubmittedAtUtc,
            registration.ReviewedAtUtc,
            registration.ReviewNote,
            registration.ActiveRosterId,
            registration.Rosters
                .OrderBy(roster => roster.Version)
                .Select(roster => roster.ToDto(currentProfiles))
                .ToList());

    public static RosterDto ToDto(this Roster roster, IReadOnlyDictionary<Guid, PlayerProfile> currentProfiles)
        => new(
            roster.Id,
            roster.Version,
            roster.Status.ToDto(),
            roster.SubmittedAtUtc,
            roster.ReviewedAtUtc,
            roster.ReviewNote,
            roster.PlayerSnapshots
                .OrderBy(snapshot => snapshot.Name, StringComparer.OrdinalIgnoreCase)
                .Select(snapshot => snapshot.ToDto(currentProfiles))
                .ToList());

    public static RosterPlayerSnapshotDto ToDto(this RosterPlayerSnapshot snapshot, IReadOnlyDictionary<Guid, PlayerProfile> currentProfiles)
    {
        var isPotentiallyOutdated = false;
        if (snapshot.SourcePlayerProfileId is Guid sourcePlayerProfileId
            && currentProfiles.TryGetValue(sourcePlayerProfileId, out var playerProfile))
        {
            isPotentiallyOutdated = snapshot.IsPotentiallyOutdated(playerProfile);
        }

        return new RosterPlayerSnapshotDto(
            snapshot.Id,
            snapshot.SourcePlayerProfileId,
            snapshot.PlayerProfileType.ToDto(),
            snapshot.Name,
            snapshot.UserId,
            snapshot.SourcePlayerProfileLastRevisionUtc,
            snapshot.SnapshotCreatedUtc,
            isPotentiallyOutdated);
    }

    public static PlayerProfileTypeDto ToDto(this PlayerProfileType type)
        => type switch
        {
            PlayerProfileType.Guest => PlayerProfileTypeDto.Guest,
            PlayerProfileType.User => PlayerProfileTypeDto.User,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static TeamRoleDto ToDto(this TeamRole role)
        => role switch
        {
            TeamRole.Member => TeamRoleDto.Member,
            TeamRole.Editor => TeamRoleDto.Editor,
            TeamRole.Owner => TeamRoleDto.Owner,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    public static TeamRole ToDomain(this TeamRoleDto role)
        => role switch
        {
            TeamRoleDto.Member => TeamRole.Member,
            TeamRoleDto.Editor => TeamRole.Editor,
            TeamRoleDto.Owner => TeamRole.Owner,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    public static RosterStatusDto ToDto(this RosterStatus status)
        => status switch
        {
            RosterStatus.Pending => RosterStatusDto.Pending,
            RosterStatus.Approved => RosterStatusDto.Approved,
            RosterStatus.Rejected => RosterStatusDto.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static TournamentRegistrationStatusDto ToDto(this TournamentRegistrationStatus status)
        => status switch
        {
            TournamentRegistrationStatus.Pending => TournamentRegistrationStatusDto.Pending,
            TournamentRegistrationStatus.Approved => TournamentRegistrationStatusDto.Approved,
            TournamentRegistrationStatus.Rejected => TournamentRegistrationStatusDto.Rejected,
            TournamentRegistrationStatus.Withdrawn => TournamentRegistrationStatusDto.Withdrawn,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static TournamentStatusDto ToDto(this TournamentStatus status)
        => status switch
        {
            TournamentStatus.Draft => TournamentStatusDto.Draft,
            TournamentStatus.RegistrationOpen => TournamentStatusDto.RegistrationOpen,
            TournamentStatus.RegistrationClosed => TournamentStatusDto.RegistrationClosed,
            TournamentStatus.InProgress => TournamentStatusDto.InProgress,
            TournamentStatus.Completed => TournamentStatusDto.Completed,
            TournamentStatus.Archived => TournamentStatusDto.Archived,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static TournamentStatus ToDomain(this TournamentStatusDto status)
        => status switch
        {
            TournamentStatusDto.Draft => TournamentStatus.Draft,
            TournamentStatusDto.RegistrationOpen => TournamentStatus.RegistrationOpen,
            TournamentStatusDto.RegistrationClosed => TournamentStatus.RegistrationClosed,
            TournamentStatusDto.InProgress => TournamentStatus.InProgress,
            TournamentStatusDto.Completed => TournamentStatus.Completed,
            TournamentStatusDto.Archived => TournamentStatus.Archived,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}
