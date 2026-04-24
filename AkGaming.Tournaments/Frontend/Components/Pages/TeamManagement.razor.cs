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
    [Inject] private TournamentRegistrationsApiClient RegistrationsClient { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<PlayerProfileDto> availableProfiles = [];
    private IReadOnlyList<TournamentRegistrationDto> registrations = [];
    private HashSet<Guid> selectedProfileIds = [];
    private TeamDto? selectedTeam;
    private string? currentUserId;
    private string? errorMessage;
    private string teamGameId = string.Empty;
    private string teamName = string.Empty;
    private string teamLookupId = string.Empty;
    private string guestProfileName = string.Empty;
    private string tournamentId = string.Empty;
    private bool isLoading = true;
    private bool isBusy;

    protected override async Task OnInitializedAsync()
    {
        currentUserId = await ResolveCurrentUserIdAsync();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await RunApiActionAsync(async () =>
        {
            games = await GamesClient.GetGamesAsync();
            if (games.Count > 0)
                teamGameId = games[0].Id;
        });

        isLoading = false;
    }

    private Task SetTeamGame(string gameId)
    {
        teamGameId = gameId;
        return Task.CompletedTask;
    }

    private async Task CreateTeamAsync()
    {
        if (!ValidateAuthenticatedUser() || !ValidateRequired(teamGameId, "Select a game.") || !ValidateRequired(teamName, "Enter a team name."))
            return;

        await RunApiActionAsync(async () =>
        {
            selectedTeam = await TeamsClient.CreateTeamAsync(currentUserId!, teamGameId, teamName);
            teamLookupId = selectedTeam.Id.ToString();
            teamName = string.Empty;
            await RefreshTeamContextAsync();
        });
    }

    private async Task LoadTeamAsync()
    {
        if (!Guid.TryParse(teamLookupId, out var teamId))
        {
            errorMessage = "Enter a valid team id.";
            return;
        }

        await RunApiActionAsync(async () =>
        {
            selectedTeam = await TeamsClient.GetTeamAsync(teamId);
            if (selectedTeam is null)
            {
                errorMessage = "No team was found for that id.";
                return;
            }

            await RefreshTeamContextAsync();
        });
    }

    private async Task CreateGuestProfileAsync()
    {
        if (!ValidateAuthenticatedUser() || selectedTeam is null || !ValidateRequired(guestProfileName, "Enter a guest profile name."))
            return;

        await RunApiActionAsync(async () =>
        {
            await TeamsClient.CreateGuestPlayerProfileAsync(selectedTeam.Id, currentUserId!, guestProfileName);
            guestProfileName = string.Empty;
            selectedTeam = await TeamsClient.GetTeamAsync(selectedTeam.Id);
            await RefreshAvailableProfilesAsync();
        });
    }

    private async Task SubmitRegistrationAsync()
    {
        if (!ValidateAuthenticatedUser() || selectedTeam is null)
            return;

        if (!Guid.TryParse(tournamentId, out var parsedTournamentId))
        {
            errorMessage = "Enter a valid tournament id.";
            return;
        }

        if (selectedProfileIds.Count == 0)
        {
            errorMessage = "Select at least one roster profile.";
            return;
        }

        await RunApiActionAsync(async () =>
        {
            await RegistrationsClient.SubmitRegistrationAsync(selectedTeam.Id, parsedTournamentId, currentUserId!, selectedProfileIds.ToList());
            selectedProfileIds.Clear();
            registrations = await RegistrationsClient.GetTeamRegistrationsAsync(selectedTeam.Id);
        });
    }

    private async Task RefreshTeamContextAsync()
    {
        await RefreshAvailableProfilesAsync();
        if (selectedTeam is not null)
            registrations = await RegistrationsClient.GetTeamRegistrationsAsync(selectedTeam.Id);
    }

    private async Task RefreshAvailableProfilesAsync()
    {
        if (selectedTeam is null)
        {
            availableProfiles = [];
            return;
        }

        availableProfiles = await TeamsClient.GetAvailableProfilesAsync(selectedTeam.Id, selectedTeam.GameId);
        selectedProfileIds.RemoveWhere(profileId => availableProfiles.All(profile => profile.Id != profileId));
    }

    private Task SelectProfile(Guid profileId)
    {
        selectedProfileIds.Add(profileId);
        return Task.CompletedTask;
    }

    private Task DeselectProfile(Guid profileId)
    {
        selectedProfileIds.Remove(profileId);
        return Task.CompletedTask;
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

    private string GetGameName(string gameId)
        => games.FirstOrDefault(game => string.Equals(game.Id, gameId, StringComparison.OrdinalIgnoreCase))?.Name ?? gameId;

    private async Task<string?> ResolveCurrentUserIdAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? authState.User.FindFirstValue("sub");
    }
}
