using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using System.Security.Cryptography;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TeamManagementService(
    IGameRepository gameRepository,
    IMediaAssetRepository mediaAssetRepository,
    IPlayerProfileRepository playerProfileRepository,
    ITeamRepository teamRepository,
    IUnitOfWork unitOfWork) : ITeamManagementService
{
    public async Task<TeamDto?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId, cancellationToken);
        return team?.ToDto();
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var teams = await teamRepository.GetByUserIdAsync(userId.Trim(), cancellationToken);
        return teams
            .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select(team => team.ToDto())
            .ToList();
    }

    public async Task<TeamDto> CreateTeamAsync(string actingUserId, string gameId, string name, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateName(name, "Team");
        await RequireGameAsync(gameId, cancellationToken);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = gameId.Trim(),
            Name = name.Trim(),
            Memberships =
            [
                new TeamMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = actingUserId.Trim(),
                    Role = TeamRole.Owner
                }
            ]
        };

        await teamRepository.AddAsync(team, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<TeamDto> AddMemberAsync(Guid teamId, string actingUserId, string userId, TeamRoleDto role, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateUserId(userId);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureOwner(team, actingUserId);

        if (team.Memberships.Any(member => string.Equals(member.UserId, userId.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException($"User '{userId}' is already a member of team '{teamId}'.");
        }

        team.Memberships.Add(new TeamMembership
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = userId.Trim(),
            Role = role.ToDomain()
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<TeamDto> UpdateMemberRoleAsync(Guid teamId, string actingUserId, string userId, TeamRoleDto role, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateUserId(userId);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureOwner(team, actingUserId);

        var membership = team.Memberships.FirstOrDefault(member => string.Equals(member.UserId, userId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (membership is null)
        {
            throw new NotFoundException($"User '{userId}' is not a member of team '{teamId}'.");
        }

        membership.Role = role.ToDomain();

        if (!team.Memberships.Any(member => member.Role == TeamRole.Owner))
        {
            throw new ValidationException("A team must always have at least one owner.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<TeamDto> TransferOwnershipAsync(Guid teamId, string actingUserId, string targetUserId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateUserId(targetUserId);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureOwner(team, actingUserId);

        var normalizedTargetUserId = targetUserId.Trim();
        var targetMembership = team.Memberships.FirstOrDefault(member =>
            string.Equals(member.UserId, normalizedTargetUserId, StringComparison.OrdinalIgnoreCase));
        if (targetMembership is null)
        {
            throw new NotFoundException($"User '{targetUserId}' is not a member of team '{teamId}'.");
        }

        var actingMembership = team.Memberships.First(member =>
            string.Equals(member.UserId, actingUserId.Trim(), StringComparison.OrdinalIgnoreCase));

        targetMembership.Role = TeamRole.Owner;
        if (!string.Equals(actingMembership.UserId, targetMembership.UserId, StringComparison.OrdinalIgnoreCase))
        {
            actingMembership.Role = TeamRole.Editor;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<TeamDto> UpdateTeamAsync(
        Guid teamId,
        string actingUserId,
        string name,
        Guid? bannerAssetId,
        string? primaryColor,
        string? profileLink,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateName(name, "Team");
        await RequireMediaAssetAsync(bannerAssetId, cancellationToken);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);
        team.Name = name.Trim();
        team.BannerAssetId = bannerAssetId;
        team.PrimaryColor = NormalizePrimaryColor(primaryColor);
        team.ProfileLink = NormalizeHttpsLink(profileLink, "Team profile link");

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<TeamDto> UpdateTeamLogoAsync(Guid teamId, string actingUserId, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        await RequireMediaAssetAsync(logoAssetId, cancellationToken);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);
        team.LogoAssetId = logoAssetId;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return team.ToDto();
    }

    public async Task<IReadOnlyList<TeamInviteKeyDto>> GetInviteKeysAsync(Guid teamId, string actingUserId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        return team.InviteKeys
            .OrderByDescending(invite => invite.CreatedUtc)
            .Select(invite => invite.ToDto())
            .ToList();
    }

    public async Task<TeamInviteKeyDto> CreateInviteKeyAsync(Guid teamId, string actingUserId, int maxUses = 1, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        var normalizedUses = Math.Max(1, maxUses);
        var invite = new TeamInviteKey
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            Key = GenerateInviteKey(),
            RemainingUses = normalizedUses,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        await teamRepository.AddInviteKeyAsync(invite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invite.ToDto();
    }

    public async Task<TeamInviteKeyDto> RevokeInviteKeyAsync(Guid teamId, string key, string actingUserId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ValidationException("Invite key is required.");
        }

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        var invite = await teamRepository.GetInviteKeyAsync(teamId, key.Trim(), cancellationToken);
        if (invite is null)
        {
            throw new NotFoundException($"Invite key '{key}' was not found for team '{teamId}'.");
        }

        invite.RemainingUses = 0;
        invite.RevokedUtc = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return invite.ToDto();
    }

    public async Task<TeamInviteKeyDto> AcceptInviteAsync(Guid teamId, string key, string userId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ValidationException("Invite key is required.");
        }

        var normalizedKey = key.Trim();
        var invite = await teamRepository.GetInviteKeyAsync(teamId, normalizedKey, cancellationToken)
            ?? throw new NotFoundException($"Invite key '{key}' was not found for team '{teamId}'.");

        if (invite.RemainingUses <= 0)
        {
            throw new ValidationException("This invite key has no remaining uses.");
        }

        var normalizedUserId = userId.Trim();
        if (await teamRepository.IsUserMemberAsync(teamId, normalizedUserId, cancellationToken))
        {
            throw new ConflictException($"User '{normalizedUserId}' is already a member of team '{teamId}'.");
        }

        invite.RemainingUses = Math.Max(0, invite.RemainingUses - 1);
        if (invite.RemainingUses == 0)
        {
            invite.RevokedUtc ??= DateTimeOffset.UtcNow;
        }

        await teamRepository.AddMembershipAsync(new TeamMembership
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = normalizedUserId,
            Role = TeamRole.Member
        }, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (string.Equals(ex.GetType().Name, "DbUpdateConcurrencyException", StringComparison.Ordinal))
        {
            throw new ValidationException("This invite key is no longer valid.");
        }
        catch (Exception ex) when (string.Equals(ex.GetType().Name, "DbUpdateException", StringComparison.Ordinal))
        {
            throw new ConflictException($"User '{normalizedUserId}' is already a member of team '{teamId}'.");
        }

        return invite.ToDto();
    }

    public async Task<PlayerProfileDto> CreateGuestPlayerProfileAsync(Guid teamId, string actingUserId, string name, int? rankRating = null, string? profileLink = null, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateName(name, "Player profile");

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = team.GameId,
            Name = name.Trim(),
            RankRating = NormalizeRankRating(rankRating),
            ProfileLink = NormalizeHttpsLink(profileLink, "Player profile link"),
            Type = PlayerProfileType.Guest
        };

        await playerProfileRepository.AddAsync(profile, cancellationToken);
        team.GuestPlayerProfiles.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }

    public async Task<PlayerProfileDto> UpdateGuestPlayerProfileAsync(Guid teamId, Guid playerProfileId, string actingUserId, string name, int? rankRating = null, string? profileLink = null, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateName(name, "Player profile");

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        var profile = team.GuestPlayerProfiles.FirstOrDefault(candidate => candidate.Id == playerProfileId);
        if (profile is null)
        {
            throw new NotFoundException($"Guest player profile '{playerProfileId}' was not found for team '{teamId}'.");
        }

        profile.Name = name.Trim();
        profile.RankRating = NormalizeRankRating(rankRating);
        profile.ProfileLink = NormalizeHttpsLink(profileLink, "Player profile link");
        profile.LastRevisionUtc = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }

    public async Task<TeamDto> DeleteGuestPlayerProfileAsync(Guid teamId, Guid playerProfileId, string actingUserId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        var profile = team.GuestPlayerProfiles.FirstOrDefault(candidate => candidate.Id == playerProfileId);
        if (profile is null)
        {
            throw new NotFoundException($"Guest player profile '{playerProfileId}' was not found for team '{teamId}'.");
        }

        playerProfileRepository.Delete(profile);
        team.GuestPlayerProfiles.Remove(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return team.ToDto();
    }

    public async Task<IReadOnlyList<PlayerProfileDto>> GetAvailableProfilesAsync(Guid teamId, string gameId, CancellationToken cancellationToken = default)
    {
        await RequireGameAsync(gameId, cancellationToken);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureTeamGame(team, gameId);

        var memberUserIds = team.Memberships
            .Select(member => member.UserId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var memberProfiles = await playerProfileRepository.GetByUsersAndGameAsync(memberUserIds, gameId.Trim(), cancellationToken);
        var guestProfiles = team.GuestPlayerProfiles
            .Where(profile => string.Equals(profile.GameId, gameId.Trim(), StringComparison.OrdinalIgnoreCase));

        return memberProfiles
            .Concat(guestProfiles)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .Select(profile => profile.ToDto())
            .ToList();
    }

    private static void EnsureTeamGame(Team team, string gameId)
    {
        if (!string.Equals(team.GameId, gameId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Teams can only use player profiles for their game.");
        }
    }

    private async Task<Team> RequireTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await teamRepository.GetByIdAsync(teamId, cancellationToken)
               ?? throw new NotFoundException($"Team '{teamId}' was not found.");
    }

    private async Task RequireGameAsync(string gameId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            throw new ValidationException("Game id is required.");
        }

        if (await gameRepository.GetByIdAsync(gameId.Trim(), cancellationToken) is null)
        {
            throw new NotFoundException($"Game '{gameId}' was not found.");
        }
    }

    private async Task RequireMediaAssetAsync(Guid? mediaAssetId, CancellationToken cancellationToken)
    {
        if (mediaAssetId is not Guid assetId)
            return;

        if (await mediaAssetRepository.GetByIdAsync(assetId, cancellationToken) is null)
        {
            throw new NotFoundException($"Media asset '{assetId}' was not found.");
        }
    }

    private static void EnsureCanEditTeam(Team team, string actingUserId)
    {
        if (!team.Memberships.Any(member =>
                string.Equals(member.UserId, actingUserId.Trim(), StringComparison.OrdinalIgnoreCase)
                && (member.Role == TeamRole.Owner || member.Role == TeamRole.Editor)))
        {
            throw new ForbiddenException("Only owners and editors can edit this team.");
        }
    }

    private static void EnsureOwner(Team team, string actingUserId)
    {
        if (!team.Memberships.Any(member =>
                string.Equals(member.UserId, actingUserId.Trim(), StringComparison.OrdinalIgnoreCase)
                && member.Role == TeamRole.Owner))
        {
            throw new ForbiddenException("Only owners can manage team membership roles.");
        }
    }

    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ValidationException("User id is required.");
        }
    }

    private static void ValidateName(string name, string subject)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException($"{subject} name is required.");
        }
    }

    private static int? NormalizeRankRating(int? rankRating)
        => rankRating.HasValue ? Math.Max(0, rankRating.Value) : null;

    private static string? NormalizePrimaryColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        return normalized.Length is 4 or 7 ? normalized : null;
    }

    private static string? NormalizeHttpsLink(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            return uri.ToString();
        }

        throw new ValidationException($"{fieldName} must be a valid https URL.");
    }

    private static string GenerateInviteKey()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[16];

        for (var index = 0; index < chars.Length; index++)
        {
            chars[index] = alphabet[bytes[index] % alphabet.Length];
        }

        return new string(chars);
    }
}
