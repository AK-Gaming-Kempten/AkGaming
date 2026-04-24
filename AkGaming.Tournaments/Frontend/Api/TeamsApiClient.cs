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

    public Task<IReadOnlyList<PlayerProfileDto>> GetAvailableProfilesAsync(Guid teamId, string gameId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<PlayerProfileDto>>($"api/teams/{teamId}/available-player-profiles/{Uri.EscapeDataString(gameId)}", cancellationToken);

    public Task<PlayerProfileDto> CreateGuestPlayerProfileAsync(Guid teamId, string actingUserId, string name, CancellationToken cancellationToken = default)
        => PostAsync<PlayerProfileDto>($"api/teams/{teamId}/guest-player-profiles", new CreateGuestPlayerProfileApiRequest(actingUserId, name), cancellationToken);

    private sealed record CreateTeamApiRequest(string ActingUserId, string GameId, string Name);
    private sealed record AddTeamMemberApiRequest(string ActingUserId, string UserId, TeamRoleDto Role);
    private sealed record CreateGuestPlayerProfileApiRequest(string ActingUserId, string Name);
}
