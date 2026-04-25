using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class TeamDetails : ComponentBase
{
    [Parameter] public Guid TeamId { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private TournamentRegistrationsApiClient RegistrationsClient { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<PlayerProfileDto> availableProfiles = [];
    private IReadOnlyList<TournamentRegistrationDto> registrations = [];
    private TeamDto? team;
    private string? currentUserId;
    private string? errorMessage;
    private bool isAuthenticated;
    private bool isLoading = true;
    private bool isBusy;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        errorMessage = null;
        availableProfiles = [];
        registrations = [];

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? authState.User.FindFirstValue("sub");

        try
        {
            games = await GamesClient.GetGamesAsync();
            team = await TeamsClient.GetTeamAsync(TeamId);
            if (team is not null)
            {
                availableProfiles = await TeamsClient.GetAvailableProfilesAsync(team.Id, team.GameId);
                registrations = await RegistrationsClient.GetTeamRegistrationsAsync(team.Id);
            }
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

    private string GetGameName(string gameId)
        => games.FirstOrDefault(game => string.Equals(game.Id, gameId, StringComparison.OrdinalIgnoreCase))?.Name ?? gameId;

    private async Task SetTeamLogoAsync(MediaAssetDto asset)
    {
        await UpdateTeamLogoAsync(asset.Id);
    }

    private async Task ClearTeamLogoAsync()
    {
        await UpdateTeamLogoAsync(null);
    }

    private async Task UpdateTeamLogoAsync(Guid? logoAssetId)
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId))
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            team = await TeamsClient.UpdateTeamLogoAsync(team.Id, currentUserId, logoAssetId);
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
