using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminTournaments : ComponentBase
{
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private TournamentsApiClient TournamentsClient { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<TournamentSummaryDto> tournaments = [];
    private TournamentSummaryDto? selectedTournament;
    private bool selectedTournamentIsVisible;
    private string? errorMessage;
    private string? successMessage;
    private bool isLoading = true;
    private bool isBusy;
    private bool isTournamentSelectorOpen;

    protected override async Task OnInitializedAsync()
    {
        await RunApiActionAsync(LoadPageStateCoreAsync);
        isLoading = false;
    }

    private async Task LoadPageStateCoreAsync()
    {
        var selectedTournamentId = selectedTournament?.Id;
        games = await GamesClient.GetGamesAsync();
        tournaments = await TournamentsClient.GetAdminTournamentsAsync();
        selectedTournament = selectedTournamentId.HasValue
            ? tournaments.FirstOrDefault(tournament => tournament.Id == selectedTournamentId.Value)
            : tournaments.FirstOrDefault();
        selectedTournamentIsVisible = selectedTournament?.IsVisible ?? false;

    }

    private async Task CreateTournamentAsync(AdminTournamentCreateRequest request)
    {
        await RunApiActionAsync(async () =>
        {
            var createdTournament = await TournamentsClient.CreateTournamentAsync(
                request.Slug,
                request.GameId,
                request.Name,
                request.IsVisible);

            await LoadPageStateCoreAsync();
            selectedTournament = tournaments.FirstOrDefault(tournament => tournament.Id == createdTournament.Id) ?? selectedTournament;
            selectedTournamentIsVisible = selectedTournament?.IsVisible ?? createdTournament.IsVisible;
            successMessage = "Tournament created.";
        });
    }

    private Task SelectTournamentAsync(TournamentSummaryDto tournament)
    {
        selectedTournament = tournament;
        selectedTournamentIsVisible = tournament.IsVisible;
        successMessage = null;
        errorMessage = null;
        return Task.CompletedTask;
    }

    private async Task SaveSelectedTournamentVisibilityAsync()
    {
        if (selectedTournament is null)
        {
            return;
        }

        await RunApiActionAsync(async () =>
        {
            var updatedTournament = await TournamentsClient.UpdateTournamentVisibilityAsync(selectedTournament.Id, selectedTournamentIsVisible);
            await LoadPageStateCoreAsync();
            selectedTournament = tournaments.FirstOrDefault(tournament => tournament.Id == updatedTournament.Id) ?? selectedTournament;
            selectedTournamentIsVisible = updatedTournament.IsVisible;
            successMessage = updatedTournament.IsVisible
                ? "Tournament visibility updated to public."
                : "Tournament visibility updated to hidden.";
        });
    }

    private async Task DeleteSelectedTournamentAsync()
    {
        if (selectedTournament is null)
        {
            return;
        }

        var deletedTournamentId = selectedTournament.Id;
        await RunApiActionAsync(async () =>
        {
            await TournamentsClient.DeleteTournamentAsync(deletedTournamentId);
            await LoadPageStateCoreAsync();
            selectedTournament = tournaments.FirstOrDefault();
            selectedTournamentIsVisible = selectedTournament?.IsVisible ?? false;
            successMessage = "Tournament deleted.";
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

    private Task SetSelectedTournamentVisibilityAsync(bool value)
    {
        selectedTournamentIsVisible = value;
        return Task.CompletedTask;
    }
}
