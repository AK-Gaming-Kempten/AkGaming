using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminGameSelector : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public GameDto? SelectedGame { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<GameDto> GameSelected { get; set; }
    [Parameter] public EventCallback<AdminGameCreateRequest> GameCreateRequested { get; set; }

    private string newGameId = string.Empty;
    private string newGameName = string.Empty;
    private bool isCreateGameFormVisible;

    private async Task ToggleGameSelectorAsync()
        => await SetOpenAsync(!IsOpen);

    private async Task CloseGameSelectorAsync()
    {
        isCreateGameFormVisible = false;
        await SetOpenAsync(false);
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

    private Task SetNewGameId(string value)
    {
        newGameId = value;
        return Task.CompletedTask;
    }

    private Task SetNewGameName(string value)
    {
        newGameName = value;
        return Task.CompletedTask;
    }

    private async Task CreateGameAsync()
    {
        if (!GameCreateRequested.HasDelegate)
            return;

        await GameCreateRequested.InvokeAsync(new AdminGameCreateRequest(newGameId, newGameName));
        newGameId = string.Empty;
        newGameName = string.Empty;
        isCreateGameFormVisible = false;
        await SetOpenAsync(false);
    }

    private async Task SelectGameAsync(GameDto game)
    {
        if (GameSelected.HasDelegate)
            await GameSelected.InvokeAsync(game);

        isCreateGameFormVisible = false;
        await SetOpenAsync(false);
    }

    private async Task SetOpenAsync(bool value)
    {
        if (IsOpen == value)
            return;

        IsOpen = value;
        await IsOpenChanged.InvokeAsync(value);
    }

    private string GetSelectedTitle()
        => SelectedGame is null ? "Select game" : SelectedGame.Name;

    private string GetSelectedSummary()
        => SelectedGame is null ? "Choose one of the supported games." : $"{SelectedGame.Id} selected.";
}

public sealed record AdminGameCreateRequest(string Id, string Name);
