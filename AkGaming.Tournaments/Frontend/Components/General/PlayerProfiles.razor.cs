using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class PlayerProfiles : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private PlayerProfilesApiClient PlayerProfilesClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<PlayerProfileDto> playerProfiles = [];
    private string? currentUserId;
    private string? errorMessage;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        currentUserId = await ResolveCurrentUserIdAsync();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        errorMessage = null;

        try
        {
            games = await GamesClient.GetGamesAsync();
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId);
            }
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }

        isLoading = false;
    }

    private Task StartCreateProfileAsync()
    {
        Nav.NavigateTo("/player-profiles/new");
        return Task.CompletedTask;
    }

    private Task OpenProfileAsync(PlayerProfileDto profile)
    {
        Nav.NavigateTo($"/player-profiles/{Uri.EscapeDataString(profile.GameId)}");
        return Task.CompletedTask;
    }

    private async Task<string?> ResolveCurrentUserIdAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? authState.User.FindFirstValue("sub");
    }
}
