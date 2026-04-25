using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class AdminGames : ComponentBase
{
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private GameDto? selectedGame;
    private string? errorMessage;
    private string newGameId = string.Empty;
    private string newGameName = string.Empty;
    private bool isLoading = true;
    private bool isBusy;
    private bool isGameSelectorOpen;
    private bool isCreateGameFormVisible;

    protected override async Task OnInitializedAsync()
    {
        await LoadGamesAsync();
        selectedGame = games.FirstOrDefault();
        isLoading = false;
    }

    private async Task LoadGamesAsync()
    {
        await RunApiActionAsync(async () =>
        {
            var selectedGameId = selectedGame?.Id;
            games = await GamesClient.GetGamesAsync();
            selectedGame = !string.IsNullOrWhiteSpace(selectedGameId)
                ? games.FirstOrDefault(game => string.Equals(game.Id, selectedGameId, StringComparison.OrdinalIgnoreCase))
                : selectedGame;
        });
    }

    private Task ToggleGameSelector()
    {
        isGameSelectorOpen = !isGameSelectorOpen;
        return Task.CompletedTask;
    }

    private Task CloseGameSelector()
    {
        isGameSelectorOpen = false;
        isCreateGameFormVisible = false;
        return Task.CompletedTask;
    }

    private Task ShowCreateGameForm()
    {
        isCreateGameFormVisible = true;
        return Task.CompletedTask;
    }

    private Task HideCreateGameForm()
    {
        isCreateGameFormVisible = false;
        newGameId = string.Empty;
        newGameName = string.Empty;
        return Task.CompletedTask;
    }

    private async Task CreateGameAsync()
    {
        await RunApiActionAsync(async () =>
        {
            var createdGame = await GamesClient.CreateGameAsync(newGameId, newGameName, null);
            newGameId = string.Empty;
            newGameName = string.Empty;
            isCreateGameFormVisible = false;
            isGameSelectorOpen = false;
            selectedGame = createdGame;
            await LoadGamesAsync();
        });
    }

    private Task SelectGameAsync(GameDto game)
    {
        selectedGame = game;
        isGameSelectorOpen = false;
        isCreateGameFormVisible = false;
        return Task.CompletedTask;
    }

    private async Task SetUploadedLogoAsync(MediaAssetDto asset)
    {
        if (selectedGame is null)
            return;

        await RunApiActionAsync(async () =>
        {
            selectedGame = await GamesClient.UpdateGameLogoAsync(selectedGame.Id, asset.Id);
            await LoadGamesAsync();
        });
    }

    private async Task ClearSelectedLogoAsync()
    {
        if (selectedGame is null)
            return;

        await RunApiActionAsync(async () =>
        {
            selectedGame = await GamesClient.UpdateGameLogoAsync(selectedGame.Id, null);
            await LoadGamesAsync();
        });
    }

    private async Task DeleteSelectedGameAsync()
    {
        if (selectedGame is null)
            return;

        var deletedGameId = selectedGame.Id;
        await RunApiActionAsync(async () =>
        {
            await GamesClient.DeleteGameAsync(deletedGameId);
            selectedGame = null;
            await LoadGamesAsync();
            selectedGame = games.FirstOrDefault();
        });
    }

    private async Task RunApiActionAsync(Func<Task> action)
    {
        errorMessage = null;
        isBusy = true;

        try
        {
            await action();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

}
