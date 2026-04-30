using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.General;

public partial class PlayerProfileDetails : ComponentBase
{
    [Parameter] public string? GameId { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private PlayerProfilesApiClient PlayerProfilesClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<PlayerProfileDto> playerProfiles = [];
    private PlayerProfileDto? selectedProfile;
    private string? currentUserId;
    private string? errorMessage;
    private string profileGameId = string.Empty;
    private string profileName = string.Empty;
    private string profileLink = string.Empty;
    private int? profileRankRating;
    private bool isEditMode;
    private bool isLoading = true;
    private bool isBusy;
    private bool isProfileSelectorOpen;

    private bool IsCreateMode => string.IsNullOrWhiteSpace(GameId);

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        await LoadDataAsync();
        isLoading = false;
    }

    private async Task LoadDataAsync()
    {
        currentUserId = await ResolveCurrentUserIdAsync();
        errorMessage = null;

        try
        {
            games = await GamesClient.GetGamesAsync();
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId);
            }
            else
            {
                playerProfiles = [];
            }

            selectedProfile = IsCreateMode
                ? null
                : playerProfiles.FirstOrDefault(profile => string.Equals(profile.GameId, GameId, StringComparison.OrdinalIgnoreCase));

            if (IsCreateMode)
            {
                isEditMode = true;
                profileGameId = GetAvailableCreateGames().FirstOrDefault()?.Id ?? games.FirstOrDefault()?.Id ?? string.Empty;
                profileName = string.Empty;
                profileLink = string.Empty;
                profileRankRating = 0;
            }
            else if (selectedProfile is not null)
            {
                isEditMode = false;
                profileGameId = selectedProfile.GameId;
                profileName = selectedProfile.Name;
                profileLink = selectedProfile.ProfileLink ?? string.Empty;
                profileRankRating = selectedProfile.RankRating;
            }
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
    }

    private IReadOnlyList<GameDto> GetFormGames()
    {
        if (IsCreateMode)
            return GetAvailableCreateGames();

        return games;
    }

    private IReadOnlyList<GameDto> GetAvailableCreateGames()
    {
        var existingGameIds = playerProfiles
            .Select(profile => profile.GameId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return games
            .Where(game => !existingGameIds.Contains(game.Id))
            .ToList();
    }

    private Task SetProfileGameAsync(string gameId)
    {
        profileGameId = gameId;
        profileRankRating = 0;
        return Task.CompletedTask;
    }

    private Task SetProfileNameAsync(string value)
    {
        profileName = value;
        return Task.CompletedTask;
    }

    private Task SetProfileLinkAsync(string value)
    {
        profileLink = value;
        return Task.CompletedTask;
    }

    private Task SetProfileRankAsync(int? value)
    {
        profileRankRating = value;
        return Task.CompletedTask;
    }

    private Task StartEditAsync()
    {
        if (selectedProfile is null)
            return Task.CompletedTask;

        isEditMode = true;
        profileGameId = selectedProfile.GameId;
        profileName = selectedProfile.Name;
        profileLink = selectedProfile.ProfileLink ?? string.Empty;
        profileRankRating = selectedProfile.RankRating;
        return Task.CompletedTask;
    }

    private Task StartCreateAsync()
    {
        Nav.NavigateTo("/player-profiles/new");
        return Task.CompletedTask;
    }

    private Task SelectProfileAsync(PlayerProfileDto profile)
    {
        Nav.NavigateTo($"/player-profiles/{Uri.EscapeDataString(profile.GameId)}");
        return Task.CompletedTask;
    }

    private Task CancelAsync()
    {
        if (IsCreateMode)
        {
            Nav.NavigateTo("/player-profiles");
            return Task.CompletedTask;
        }

        isEditMode = false;
        if (selectedProfile is not null)
        {
            profileGameId = selectedProfile.GameId;
            profileName = selectedProfile.Name;
            profileLink = selectedProfile.ProfileLink ?? string.Empty;
            profileRankRating = selectedProfile.RankRating;
        }

        return Task.CompletedTask;
    }

    private async Task SaveProfileAsync()
    {
        if (!ValidateAuthenticatedUser() || !ValidateRequired(profileName, "Enter a player profile name."))
            return;

        var targetGameId = IsCreateMode ? profileGameId : selectedProfile?.GameId ?? string.Empty;
        if (!ValidateRequired(targetGameId, "Select a game."))
            return;

        if (IsCreateMode && playerProfiles.Any(profile => string.Equals(profile.GameId, targetGameId, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "A profile for that game already exists.";
            return;
        }

        await RunApiActionAsync(async () =>
        {
            var savedProfile = await PlayerProfilesClient.UpsertUserProfileAsync(currentUserId!, targetGameId, profileName, profileRankRating, profileLink);
            playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId!);
            selectedProfile = playerProfiles.FirstOrDefault(profile => string.Equals(profile.GameId, savedProfile.GameId, StringComparison.OrdinalIgnoreCase))
                              ?? savedProfile;
            profileGameId = selectedProfile.GameId;
            profileName = selectedProfile.Name;
            profileLink = selectedProfile.ProfileLink ?? string.Empty;
            profileRankRating = selectedProfile.RankRating;

            if (IsCreateMode)
            {
                Nav.NavigateTo($"/player-profiles/{Uri.EscapeDataString(savedProfile.GameId)}");
                return;
            }

            isEditMode = false;
        });
    }

    private async Task SetProfileLogoAsync(MediaAssetDto asset)
    {
        if (selectedProfile is null)
            return;

        await UpdateProfileLogoAsync(selectedProfile, asset.Id);
    }

    private async Task ClearProfileLogoAsync()
    {
        if (selectedProfile is null)
            return;

        await UpdateProfileLogoAsync(selectedProfile, null);
    }

    private async Task UpdateProfileLogoAsync(PlayerProfileDto profile, Guid? logoAssetId)
    {
        if (!ValidateAuthenticatedUser())
            return;

        await RunApiActionAsync(async () =>
        {
            await PlayerProfilesClient.UpdateUserProfileLogoAsync(currentUserId!, profile.GameId, logoAssetId);
            playerProfiles = await PlayerProfilesClient.GetUserProfilesAsync(currentUserId!);
            selectedProfile = playerProfiles.FirstOrDefault(candidate => string.Equals(candidate.GameId, profile.GameId, StringComparison.OrdinalIgnoreCase));
            if (selectedProfile is not null)
            {
                profileName = selectedProfile.Name;
                profileLink = selectedProfile.ProfileLink ?? string.Empty;
                profileRankRating = selectedProfile.RankRating;
            }
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
