using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class GamesApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public Task<IReadOnlyList<GameDto>> GetGamesAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<GameDto>>("api/games", cancellationToken);

    public Task<GameDto> CreateGameAsync(string id, string name, Guid? logoAssetId, CancellationToken cancellationToken = default)
        => PostAsync<GameDto>("api/games", new CreateGameApiRequest(id, name, logoAssetId), cancellationToken);

    public Task<GameDto> UpdateGameLogoAsync(string gameId, Guid? logoAssetId, CancellationToken cancellationToken = default)
        => PutAsync<GameDto>($"api/games/{Uri.EscapeDataString(gameId)}/logo", new UpdateGameLogoApiRequest(logoAssetId), cancellationToken);

    public Task DeleteGameAsync(string gameId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/games/{Uri.EscapeDataString(gameId)}", cancellationToken);

    private sealed record CreateGameApiRequest(string Id, string Name, Guid? LogoAssetId);
    private sealed record UpdateGameLogoApiRequest(Guid? LogoAssetId);
}
