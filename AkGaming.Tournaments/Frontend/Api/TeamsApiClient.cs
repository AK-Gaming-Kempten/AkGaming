using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class TeamsApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<TeamDto> CreateTeamAsync(string actingUserId, string gameId, string name, CancellationToken cancellationToken = default)
        => PostAsync<TeamDto>("api/teams", new CreateTeamApiRequest(actingUserId, gameId, name), cancellationToken);

    public Task<TeamDto?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<TeamDto>($"api/teams/{teamId}", cancellationToken);

    public Task<IReadOnlyList<TeamDto>> GetUserTeamsAsync(string userId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TeamDto>>($"api/users/{Uri.EscapeDataString(userId)}/teams", cancellationToken);

    public Task<TeamDto> AddMemberAsync(Guid teamId, string actingUserId, string userId, TeamRoleDto role, CancellationToken cancellationToken = default)
        => PostAsync<TeamDto>($"api/teams/{teamId}/members", new AddTeamMemberApiRequest(actingUserId, userId, role), cancellationToken);

    public Task<TeamDto> UpdateMemberRoleAsync(Guid teamId, string actingUserId, string userId, TeamRoleDto role, CancellationToken cancellationToken = default)
        => PutAsync<TeamDto>($"api/teams/{teamId}/members/{Uri.EscapeDataString(userId)}", new UpdateTeamMemberRoleApiRequest(actingUserId, role), cancellationToken);

    public Task<TeamDto> TransferOwnershipAsync(Guid teamId, string actingUserId, string targetUserId, CancellationToken cancellationToken = default)
        => PostAsync<TeamDto>($"api/teams/{teamId}/members/{Uri.EscapeDataString(targetUserId)}/transfer-ownership", new TransferOwnershipApiRequest(actingUserId), cancellationToken);

    public Task<TeamDto> UpdateTeamAsync(Guid teamId, string actingUserId, string name, CancellationToken cancellationToken = default)
        => PutAsync<TeamDto>($"api/teams/{teamId}", new UpdateTeamApiRequest(actingUserId, name), cancellationToken);

    public Task<TeamDto> UpdateTeamLogoAsync(Guid teamId, string actingUserId, Guid? logoAssetId, CancellationToken cancellationToken = default)
        => PutAsync<TeamDto>($"api/teams/{teamId}/logo", new UpdateTeamLogoApiRequest(actingUserId, logoAssetId), cancellationToken);

    public Task<IReadOnlyList<TeamInviteKeyDto>> GetInviteKeysAsync(Guid teamId, string actingUserId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<TeamInviteKeyDto>>($"api/teams/{teamId}/invite-keys?actingUserId={Uri.EscapeDataString(actingUserId)}", cancellationToken);

    public Task<TeamInviteKeyDto> CreateInviteKeyAsync(Guid teamId, string actingUserId, int maxUses = 1, CancellationToken cancellationToken = default)
        => PostAsync<TeamInviteKeyDto>($"api/teams/{teamId}/invite-keys", new CreateTeamInviteKeyApiRequest(actingUserId, maxUses), cancellationToken);

    public Task<TeamInviteKeyDto> RevokeInviteKeyAsync(Guid teamId, string key, string actingUserId, CancellationToken cancellationToken = default)
        => DeleteAsync<TeamInviteKeyDto>($"api/teams/{teamId}/invite-keys/{Uri.EscapeDataString(key)}?actingUserId={Uri.EscapeDataString(actingUserId)}", cancellationToken);

    public Task<TeamInviteKeyDto> AcceptInviteKeyAsync(Guid teamId, string key, string userId, CancellationToken cancellationToken = default)
        => PostAsync<TeamInviteKeyDto>($"api/teams/{teamId}/invite-keys/{Uri.EscapeDataString(key)}/accept", new AcceptTeamInviteKeyApiRequest(userId), cancellationToken);

    public Task<IReadOnlyList<PlayerProfileDto>> GetAvailableProfilesAsync(Guid teamId, string gameId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<PlayerProfileDto>>($"api/teams/{teamId}/available-player-profiles/{Uri.EscapeDataString(gameId)}", cancellationToken);

    public Task<PlayerProfileDto> CreateGuestPlayerProfileAsync(Guid teamId, string actingUserId, string name, int? rankRating = null, CancellationToken cancellationToken = default)
        => PostAsync<PlayerProfileDto>($"api/teams/{teamId}/guest-player-profiles", new CreateGuestPlayerProfileApiRequest(actingUserId, name, rankRating), cancellationToken);

    public Task<PlayerProfileDto> UpdateGuestPlayerProfileAsync(Guid teamId, Guid playerProfileId, string actingUserId, string name, int? rankRating = null, CancellationToken cancellationToken = default)
        => PutAsync<PlayerProfileDto>($"api/teams/{teamId}/guest-player-profiles/{playerProfileId}", new UpdateGuestPlayerProfileApiRequest(actingUserId, name, rankRating), cancellationToken);

    public Task<TeamDto> DeleteGuestPlayerProfileAsync(Guid teamId, Guid playerProfileId, string actingUserId, CancellationToken cancellationToken = default)
        => DeleteAsync<TeamDto>($"api/teams/{teamId}/guest-player-profiles/{playerProfileId}?actingUserId={Uri.EscapeDataString(actingUserId)}", cancellationToken);

    private sealed record CreateTeamApiRequest(string ActingUserId, string GameId, string Name);
    private sealed record AddTeamMemberApiRequest(string ActingUserId, string UserId, TeamRoleDto Role);
    private sealed record UpdateTeamMemberRoleApiRequest(string ActingUserId, TeamRoleDto Role);
    private sealed record TransferOwnershipApiRequest(string ActingUserId);
    private sealed record UpdateTeamApiRequest(string ActingUserId, string Name);
    private sealed record UpdateTeamLogoApiRequest(string ActingUserId, Guid? LogoAssetId);
    private sealed record CreateTeamInviteKeyApiRequest(string ActingUserId, int MaxUses);
    private sealed record AcceptTeamInviteKeyApiRequest(string UserId);
    private sealed record CreateGuestPlayerProfileApiRequest(string ActingUserId, string Name, int? RankRating);
    private sealed record UpdateGuestPlayerProfileApiRequest(string ActingUserId, string Name, int? RankRating);
}
