using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using AkGaming.Tournaments.Frontend.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class PlayerProfiles : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private PlayerProfilesApiClient PlayerProfilesClient { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<PlayerProfileDto> playerProfiles = [];
    private string? currentUserId;
    private string? errorMessage;
    private string profileGameId = string.Empty;
    private string profileName = string.Empty;
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
                profileGameId = games[0].Id;

            if (!string.IsNullOrWhiteSpace(currentUserId))
                playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId);
        });

        isLoading = false;
    }

    private Task SetProfileGame(string gameId)
    {
        profileGameId = gameId;
        return Task.CompletedTask;
    }

    private Task SetProfileName(string value)
    {
        profileName = value;
        return Task.CompletedTask;
    }

    private async Task SaveProfileAsync()
    {
        if (!ValidateAuthenticatedUser() || !ValidateRequired(profileGameId, "Select a game.") || !ValidateRequired(profileName, "Enter a player profile name."))
            return;

        await RunApiActionAsync(async () =>
        {
            await PlayerProfilesClient.UpsertUserProfileAsync(currentUserId!, profileGameId, profileName);
            playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId!);
            profileName = string.Empty;
        });
    }

    private async Task SetProfileLogoAsync(PlayerProfileLogoUpload upload)
    {
        await UpdateProfileLogoAsync(upload.Profile, upload.Asset.Id);
    }

    private async Task ClearProfileLogoAsync(PlayerProfileDto profile)
    {
        await UpdateProfileLogoAsync(profile, null);
    }

    private async Task UpdateProfileLogoAsync(PlayerProfileDto profile, Guid? logoAssetId)
    {
        if (!ValidateAuthenticatedUser())
            return;

        await RunApiActionAsync(async () =>
        {
            await PlayerProfilesClient.UpdateUserProfileLogoAsync(currentUserId!, profile.GameId, logoAssetId);
            playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId!);
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

    private async Task<string?> ResolveCurrentUserIdAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? authState.User.FindFirstValue("sub");
    }
}
