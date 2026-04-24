using AkGaming.Tournaments.Application.Abstractions;
using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TeamManagementService(
    IGameRepository gameRepository,
    IPlayerProfileRepository playerProfileRepository,
    ITeamRepository teamRepository,
    IUnitOfWork unitOfWork) : ITeamManagementService
{
    public async Task<TeamDto?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await teamRepository.GetByIdAsync(teamId, cancellationToken);
        return team?.ToDto();
    }

    public async Task<TeamDto> CreateTeamAsync(string actingUserId, string name, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateName(name, "Team");

        var team = new Team
        {
            Id = Guid.NewGuid(),
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

    public async Task<PlayerProfileDto> CreateGuestPlayerProfileAsync(Guid teamId, string actingUserId, string gameId, string name, CancellationToken cancellationToken = default)
    {
        ValidateUserId(actingUserId);
        ValidateName(name, "Player profile");
        await RequireGameAsync(gameId, cancellationToken);

        var team = await RequireTeamAsync(teamId, cancellationToken);
        EnsureCanEditTeam(team, actingUserId);

        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = gameId.Trim(),
            Name = name.Trim(),
            Type = PlayerProfileType.Guest
        };

        await playerProfileRepository.AddAsync(profile, cancellationToken);
        team.GuestPlayerProfiles.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }

    public async Task<PlayerProfileDto> UpdateGuestPlayerProfileAsync(Guid teamId, Guid playerProfileId, string actingUserId, string name, CancellationToken cancellationToken = default)
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
        profile.LastRevisionUtc = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }

    public async Task<IReadOnlyList<PlayerProfileDto>> GetAvailableProfilesAsync(Guid teamId, string gameId, CancellationToken cancellationToken = default)
    {
        await RequireGameAsync(gameId, cancellationToken);

        var team = await RequireTeamAsync(teamId, cancellationToken);
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
}
