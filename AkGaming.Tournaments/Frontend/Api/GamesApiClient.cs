using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class GamesApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<GameDto>>("api/games", cancellationToken);
}
