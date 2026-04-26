using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class MyTeamSelector : ComponentBase
{
    [Parameter] public Guid? SelectedTeamId { get; set; }
    [Parameter] public bool ShowLoginSuggestion { get; set; }
    [Parameter] public EventCallback<TeamDto> TeamSelected { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<TeamDto> userTeams = [];
    private TeamDto? selectedTeam;
    private string? currentUserId;
    private string? errorMessage;
    private string teamGameId = string.Empty;
    private string teamName = string.Empty;
    private bool isAuthenticated;
    private bool isLoading = true;
    private bool isBusy;
    private bool isTeamPickerOpen;
    private bool isCreateTeamFormVisible;

    protected override async Task OnParametersSetAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        errorMessage = null;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? authState.User.FindFirstValue("sub");

        if (!isAuthenticated || string.IsNullOrWhiteSpace(currentUserId))
        {
            isLoading = false;
            return;
        }

        await RunApiActionAsync(async () =>
        {
            games = await GamesClient.GetGamesAsync();
            if (games.Count > 0)
                teamGameId = games[0].Id;

            userTeams = await TeamsClient.GetUserTeamsAsync(currentUserId);
            selectedTeam = SelectedTeamId is Guid selectedTeamId
                ? userTeams.FirstOrDefault(team => team.Id == selectedTeamId)
                : userTeams.FirstOrDefault();
        });

        isLoading = false;
    }

    private Task ToggleTeamPicker()
    {
        isTeamPickerOpen = !isTeamPickerOpen;
        return Task.CompletedTask;
    }

    private Task CloseTeamPicker()
    {
        isTeamPickerOpen = false;
        isCreateTeamFormVisible = false;
        return Task.CompletedTask;
    }

    private Task ShowCreateTeamForm()
    {
        isCreateTeamFormVisible = true;
        return Task.CompletedTask;
    }

    private Task HideCreateTeamForm()
    {
        isCreateTeamFormVisible = false;
        teamName = string.Empty;
        return Task.CompletedTask;
    }

    private Task SetTeamGame(string gameId)
    {
        teamGameId = gameId;
        return Task.CompletedTask;
    }

    private Task SetTeamName(string value)
    {
        teamName = value;
        return Task.CompletedTask;
    }

    private async Task CreateTeamAsync()
    {
        if (!ValidateAuthenticatedUser() || !ValidateRequired(teamGameId, "Select a game.") || !ValidateRequired(teamName, "Enter a team name."))
            return;

        await RunApiActionAsync(async () =>
        {
            var createdTeam = await TeamsClient.CreateTeamAsync(currentUserId!, teamGameId, teamName);
            userTeams = await TeamsClient.GetUserTeamsAsync(currentUserId!);
            selectedTeam = userTeams.FirstOrDefault(team => team.Id == createdTeam.Id) ?? createdTeam;
            teamName = string.Empty;
            isCreateTeamFormVisible = false;
            isTeamPickerOpen = false;
            await NotifyTeamSelectedAsync(selectedTeam);
        });
    }

    private async Task SelectTeamAsync(TeamDto team)
    {
        selectedTeam = team;
        isTeamPickerOpen = false;
        isCreateTeamFormVisible = false;
        await NotifyTeamSelectedAsync(team);
    }

    private async Task NotifyTeamSelectedAsync(TeamDto team)
    {
        if (TeamSelected.HasDelegate)
            await TeamSelected.InvokeAsync(team);

        Nav.NavigateTo($"/teams/{team.Id}");
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

    private bool ValidateAuthenticatedUser()
    {
        if (!string.IsNullOrWhiteSpace(currentUserId))
            return true;

        errorMessage = "Login did not provide a user id claim.";
        return false;
    }

    private bool ValidateRequired(string value, string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return true;

        errorMessage = message;
        return false;
    }

}
