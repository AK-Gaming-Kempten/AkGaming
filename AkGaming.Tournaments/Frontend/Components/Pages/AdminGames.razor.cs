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
    private string selectedLogoAssetId = string.Empty;
    private bool isLoading = true;
    private bool isBusy;
    private bool isGameSelectorOpen;
    private bool isCreateGameFormVisible;

    protected override async Task OnInitializedAsync()
    {
        await LoadGamesAsync();
        selectedGame = games.FirstOrDefault();
        SetSelectedLogoInput();
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

            SetSelectedLogoInput();
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
        SetSelectedLogoInput();
        isGameSelectorOpen = false;
        isCreateGameFormVisible = false;
        return Task.CompletedTask;
    }

    private async Task UpdateSelectedLogoAsync()
    {
        if (selectedGame is null || !TryParseOptionalLogoAssetId(selectedLogoAssetId, out var logoAssetId))
            return;

        await RunApiActionAsync(async () =>
        {
            selectedGame = await GamesClient.UpdateGameLogoAsync(selectedGame.Id, logoAssetId);
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
            SetSelectedLogoInput();
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

    private bool TryParseOptionalLogoAssetId(string value, out Guid? logoAssetId)
    {
        logoAssetId = null;

        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (Guid.TryParse(value, out var parsed))
        {
            logoAssetId = parsed;
            return true;
        }

        errorMessage = "Enter a valid logo asset id.";
        return false;
    }

    private void SetSelectedLogoInput()
        => selectedLogoAssetId = selectedGame?.LogoAssetId?.ToString() ?? string.Empty;

    private static string GetGameInitials(GameDto game)
    {
        var parts = game.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return "--";

        if (parts.Length == 1)
            return parts[0][..Math.Min(parts[0].Length, 2)].ToUpperInvariant();

        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }
}
