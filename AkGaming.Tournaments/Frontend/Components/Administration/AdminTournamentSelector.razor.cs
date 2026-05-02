using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminTournamentSelector : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentSummaryDto> Tournaments { get; set; } = [];
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public TournamentSummaryDto? SelectedTournament { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<TournamentSummaryDto> TournamentSelected { get; set; }
    [Parameter] public EventCallback<AdminTournamentCreateRequest> TournamentCreateRequested { get; set; }

    private string newTournamentName = string.Empty;
    private string newTournamentSlug = string.Empty;
    private string newTournamentGameId = string.Empty;
    private bool newTournamentIsVisible;
    private bool isCreateTournamentFormVisible;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(newTournamentGameId))
        {
            newTournamentGameId = Games.FirstOrDefault()?.Id ?? string.Empty;
        }
    }

    private async Task ToggleTournamentSelectorAsync()
        => await SetOpenAsync(!IsOpen);

    private async Task CloseTournamentSelectorAsync()
    {
        isCreateTournamentFormVisible = false;
        await SetOpenAsync(false);
    }

    private Task ShowCreateTournamentForm()
    {
        isCreateTournamentFormVisible = true;
        return Task.CompletedTask;
    }

    private Task HideCreateTournamentForm()
    {
        isCreateTournamentFormVisible = false;
        newTournamentName = string.Empty;
        newTournamentSlug = string.Empty;
        newTournamentIsVisible = false;
        return Task.CompletedTask;
    }

    private Task SetTournamentName(string value)
    {
        newTournamentName = value;
        return Task.CompletedTask;
    }

    private Task SetTournamentSlug(string value)
    {
        newTournamentSlug = value;
        return Task.CompletedTask;
    }

    private Task SetTournamentGame(string value)
    {
        newTournamentGameId = value;
        return Task.CompletedTask;
    }

    private Task SetTournamentVisibility(bool value)
    {
        newTournamentIsVisible = value;
        return Task.CompletedTask;
    }

    private async Task CreateTournamentAsync()
    {
        if (!TournamentCreateRequested.HasDelegate)
        {
            return;
        }

        await TournamentCreateRequested.InvokeAsync(new AdminTournamentCreateRequest(
            newTournamentName,
            newTournamentSlug,
            newTournamentGameId,
            newTournamentIsVisible));
        await HideCreateTournamentForm();
        await SetOpenAsync(false);
    }

    private async Task SelectTournamentAsync(TournamentSummaryDto tournament)
    {
        if (TournamentSelected.HasDelegate)
        {
            await TournamentSelected.InvokeAsync(tournament);
        }

        isCreateTournamentFormVisible = false;
        await SetOpenAsync(false);
    }

    private async Task SetOpenAsync(bool value)
    {
        if (IsOpen == value)
        {
            return;
        }

        IsOpen = value;
        await IsOpenChanged.InvokeAsync(value);
    }

    private string GetSelectedTitle()
        => SelectedTournament is null ? "Select tournament" : SelectedTournament.Name;

    private string GetSelectedSummary()
    {
        if (SelectedTournament is null)
        {
            return "Choose a tournament or create a new one.";
        }

        var visibility = SelectedTournament.IsVisible ? "Public" : "Hidden";
        return $"{SelectedTournament.GameName} · {visibility} · {SelectedTournament.Status}";
    }
}

public sealed record AdminTournamentCreateRequest(string Name, string Slug, string GameId, bool IsVisible);
