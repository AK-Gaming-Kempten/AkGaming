using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class PlayerProfilesApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<PlayerProfileDto>> GetUserProfilesAsync(string userId, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<PlayerProfileDto>>($"api/users/{Uri.EscapeDataString(userId)}/player-profiles", cancellationToken);

    public Task<PlayerProfileDto> UpsertUserProfileAsync(string userId, string gameId, string name, CancellationToken cancellationToken = default)
        => PutAsync<PlayerProfileDto>(
            $"api/users/{Uri.EscapeDataString(userId)}/player-profiles/{Uri.EscapeDataString(gameId)}",
            new UpsertUserPlayerProfileApiRequest(name),
            cancellationToken);

    private sealed record UpsertUserPlayerProfileApiRequest(string Name);
}
