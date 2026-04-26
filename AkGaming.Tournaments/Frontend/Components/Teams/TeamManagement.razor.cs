using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class TeamManagement : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<TeamDto> userTeams = [];
    private string? currentUserId;
    private string? errorMessage;
    private string teamGameId = string.Empty;
    private string teamName = string.Empty;
    private bool isAuthenticated;
    private bool isLoading = true;
    private bool isBusy;
    private bool isCreateTeamFormVisible;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? authState.User.FindFirstValue("sub");

        if (!isAuthenticated || string.IsNullOrWhiteSpace(currentUserId))
        {
            isLoading = false;
            return;
        }

        try
        {
            games = await GamesClient.GetGamesAsync();
            if (games.Count > 0)
                teamGameId = games[0].Id;

            userTeams = await TeamsClient.GetUserTeamsAsync(currentUserId);
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isLoading = false;
        }
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
        if (!ValidateRequired(teamGameId, "Select a game.") || !ValidateRequired(teamName, "Enter a team name."))
            return;

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            errorMessage = "Login did not provide a user id claim.";
            return;
        }

        isBusy = true;
        errorMessage = null;

        try
        {
            var team = await TeamsClient.CreateTeamAsync(currentUserId, teamGameId, teamName);
            userTeams = await TeamsClient.GetUserTeamsAsync(currentUserId);
            teamName = string.Empty;
            isCreateTeamFormVisible = false;
            Nav.NavigateTo($"/teams/{team.Id}");
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

    private Task OpenTeamAsync(TeamDto team)
    {
        Nav.NavigateTo($"/teams/{team.Id}");
        return Task.CompletedTask;
    }

    private bool ValidateRequired(string value, string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return true;

        errorMessage = message;
        return false;
    }
}
