using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace AkGaming.Tournaments.Frontend.Components.Teams;

public partial class TeamDetails : ComponentBase
{
    [Parameter] public Guid TeamId { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private GamesApiClient GamesClient { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private TournamentsApiClient TournamentsClient { get; set; } = default!;
    [Inject] private TournamentRegistrationsApiClient RegistrationsClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private IReadOnlyList<GameDto> games = [];
    private IReadOnlyList<PlayerProfileDto> availableProfiles = [];
    private IReadOnlyList<TournamentRegistrationDto> registrations = [];
    private IReadOnlyDictionary<Guid, string> tournamentNames = new Dictionary<Guid, string>();
    private TeamDto? team;
    private string? currentUserId;
    private string? currentUserDisplayName;
    private string? errorMessage;
    private string teamName = string.Empty;
    private string teamPrimaryColor = string.Empty;
    private Guid? teamBannerAssetId;
    private string guestName = string.Empty;
    private int? guestRankRating;
    private TeamMembershipDto? transferOwnershipTargetMember;
    private PlayerProfileDto? editingGuestProfile;
    private bool isGuestFormOpen;
    private bool isTeamEditMode;
    private bool isTransferOwnershipDialogOpen;
    private bool isAuthenticated;
    private bool isLoading = true;
    private bool isBusy;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        errorMessage = null;
        availableProfiles = [];
        registrations = [];
        tournamentNames = new Dictionary<Guid, string>();

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? authState.User.FindFirstValue("sub");
        currentUserDisplayName = ResolveDisplayName(authState.User);

        try
        {
            games = await GamesClient.GetGamesAsync();
            tournamentNames = (await TournamentsClient.GetTournamentsAsync())
                .GroupBy(tournament => tournament.Id)
                .ToDictionary(group => group.Key, group => group.First().Name);
            team = await TeamsClient.GetTeamAsync(TeamId);
            if (team is not null)
            {
                teamName = team.Name;
                teamPrimaryColor = team.PrimaryColor ?? string.Empty;
                teamBannerAssetId = team.BannerAssetId;
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

    private Task SetGuestName(string value)
    {
        guestName = value;
        return Task.CompletedTask;
    }

    private Task SetTeamName(string value)
    {
        teamName = value;
        return Task.CompletedTask;
    }

    private Task SetTeamPrimaryColor(string value)
    {
        teamPrimaryColor = value;
        return Task.CompletedTask;
    }

    private Task StartTeamEditAsync()
    {
        if (team is not null)
        {
            teamName = team.Name;
            teamPrimaryColor = team.PrimaryColor ?? string.Empty;
            teamBannerAssetId = team.BannerAssetId;
        }

        isTeamEditMode = true;
        return Task.CompletedTask;
    }

    private Task CancelTeamEditAsync()
    {
        if (team is not null)
        {
            teamName = team.Name;
            teamPrimaryColor = team.PrimaryColor ?? string.Empty;
            teamBannerAssetId = team.BannerAssetId;
        }

        isTeamEditMode = false;
        return Task.CompletedTask;
    }

    private Task SetGuestRankRating(int? value)
    {
        guestRankRating = value;
        return Task.CompletedTask;
    }

    private Task StartGuestEdit(PlayerProfileDto profile)
    {
        isGuestFormOpen = true;
        editingGuestProfile = profile;
        guestName = profile.Name;
        guestRankRating = profile.RankRating;
        return Task.CompletedTask;
    }

    private Task StartGuestCreate()
    {
        isGuestFormOpen = true;
        editingGuestProfile = null;
        guestName = string.Empty;
        guestRankRating = 0;
        return Task.CompletedTask;
    }

    private Task CancelGuestEdit()
    {
        isGuestFormOpen = false;
        editingGuestProfile = null;
        guestName = string.Empty;
        guestRankRating = 0;
        return Task.CompletedTask;
    }

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

    private async Task SaveTeamAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(teamName))
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            team = await TeamsClient.UpdateTeamAsync(team.Id, currentUserId, teamName, teamBannerAssetId, teamPrimaryColor);
            isTeamEditMode = false;
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

    private Task OpenInviteManagementAsync()
    {
        if (team is not null)
        {
            Nav.NavigateTo($"/teams/{team.Id}/invite");
        }

        return Task.CompletedTask;
    }

    private Task SetTeamBannerAsync(MediaAssetDto asset)
    {
        teamBannerAssetId = asset.Id;
        return Task.CompletedTask;
    }

    private Task ClearTeamBannerAsync()
    {
        teamBannerAssetId = null;
        return Task.CompletedTask;
    }

    private async Task SaveGuestProfileAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId) || string.IsNullOrWhiteSpace(guestName))
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            if (editingGuestProfile is null)
            {
                await TeamsClient.CreateGuestPlayerProfileAsync(team.Id, currentUserId, guestName, guestRankRating);
            }
            else
            {
                await TeamsClient.UpdateGuestPlayerProfileAsync(team.Id, editingGuestProfile.Id, currentUserId, guestName, guestRankRating);
            }

            await RefreshTeamProfilesAsync();
            await CancelGuestEdit();
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

    private async Task PromoteToEditorAsync(TeamMembershipDto member)
    {
        await UpdateMemberRoleAsync(member.UserId, TeamRoleDto.Editor);
    }

    private async Task DemoteToMemberAsync(TeamMembershipDto member)
    {
        await UpdateMemberRoleAsync(member.UserId, TeamRoleDto.Member);
    }

    private Task StartTransferOwnershipAsync(TeamMembershipDto member)
    {
        transferOwnershipTargetMember = member;
        isTransferOwnershipDialogOpen = true;
        return Task.CompletedTask;
    }

    private Task CancelTransferOwnershipAsync()
    {
        isTransferOwnershipDialogOpen = false;
        transferOwnershipTargetMember = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmTransferOwnershipAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId) || transferOwnershipTargetMember is null)
            return;

        isBusy = true;
        errorMessage = null;
        try
        {
            team = await TeamsClient.TransferOwnershipAsync(team.Id, currentUserId, transferOwnershipTargetMember.UserId);
            await RefreshTeamProfilesAsync();
            await CancelTransferOwnershipAsync();
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

    private async Task UpdateMemberRoleAsync(string memberUserId, TeamRoleDto role)
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId))
            return;

        isBusy = true;
        errorMessage = null;
        try
        {
            team = await TeamsClient.UpdateMemberRoleAsync(team.Id, currentUserId, memberUserId, role);
            await RefreshTeamProfilesAsync();
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

    private async Task DeleteGuestProfileAsync(PlayerProfileDto profile)
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId))
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            team = await TeamsClient.DeleteGuestPlayerProfileAsync(team.Id, profile.Id, currentUserId);
            await RefreshTeamProfilesAsync();
            if (editingGuestProfile?.Id == profile.Id)
            {
                await CancelGuestEdit();
            }
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

    private async Task RefreshTeamProfilesAsync()
    {
        if (team is null)
            return;

        team = await TeamsClient.GetTeamAsync(team.Id);
        if (team is not null)
        {
            teamName = team.Name;
            teamPrimaryColor = team.PrimaryColor ?? string.Empty;
            teamBannerAssetId = team.BannerAssetId;
            availableProfiles = await TeamsClient.GetAvailableProfilesAsync(team.Id, team.GameId);
        }
    }

    private static string? ResolveDisplayName(ClaimsPrincipal user)
        => user.Claims.FirstOrDefault(claim => claim.Type == "preferred_username")?.Value
           ?? user.Claims.FirstOrDefault(claim => claim.Type == "username")?.Value
           ?? user.Claims.FirstOrDefault(claim => claim.Type == "discord_username")?.Value
           ?? user.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value
           ?? user.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

    private string GetMemberDisplayName(TeamMembershipDto member)
    {
        if (!string.IsNullOrWhiteSpace(currentUserId)
            && string.Equals(member.UserId, currentUserId, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(currentUserDisplayName))
        {
            return currentUserDisplayName;
        }

        var profileName = availableProfiles
            .FirstOrDefault(profile => profile.Type == PlayerProfileTypeDto.User
                                       && !string.IsNullOrWhiteSpace(profile.UserId)
                                       && string.Equals(profile.UserId, member.UserId, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        return !string.IsNullOrWhiteSpace(profileName) ? profileName : member.UserId;
    }
}
