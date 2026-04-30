using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using AkGaming.Tournaments.Frontend.Components.General;
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
    private string? transferOwnershipDialogErrorMessage;
    private string? rosterRefreshDialogErrorMessage;
    private string teamName = string.Empty;
    private string teamProfileLink = string.Empty;
    private string teamPrimaryColor = string.Empty;
    private Guid? teamBannerAssetId;
    private string guestName = string.Empty;
    private string guestProfileLink = string.Empty;
    private int? guestRankRating;
    private TeamMembershipDto? transferOwnershipTargetMember;
    private PlayerProfileDto? editingGuestProfile;
    private TournamentRegistrationDto? rosterRefreshRegistration;
    private HashSet<Guid> rosterRefreshSelectedProfileIds = [];
    private bool isGuestFormOpen;
    private bool isTeamEditMode;
    private bool isTransferOwnershipDialogOpen;
    private bool isRosterRefreshDialogOpen;
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
                teamProfileLink = team.ProfileLink ?? string.Empty;
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

    private Task SetGuestProfileLink(string value)
    {
        guestProfileLink = value;
        return Task.CompletedTask;
    }

    private Task SetTeamName(string value)
    {
        teamName = value;
        return Task.CompletedTask;
    }

    private Task SetTeamProfileLink(string value)
    {
        teamProfileLink = value;
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
            teamProfileLink = team.ProfileLink ?? string.Empty;
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
            teamProfileLink = team.ProfileLink ?? string.Empty;
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
        guestProfileLink = profile.ProfileLink ?? string.Empty;
        guestRankRating = profile.RankRating;
        return Task.CompletedTask;
    }

    private Task StartGuestCreate()
    {
        isGuestFormOpen = true;
        editingGuestProfile = null;
        guestName = string.Empty;
        guestProfileLink = string.Empty;
        guestRankRating = 0;
        return Task.CompletedTask;
    }

    private Task CancelGuestEdit()
    {
        isGuestFormOpen = false;
        editingGuestProfile = null;
        guestName = string.Empty;
        guestProfileLink = string.Empty;
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
            team = await TeamsClient.UpdateTeamAsync(team.Id, currentUserId, teamName, teamBannerAssetId, teamPrimaryColor, teamProfileLink);
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
                await TeamsClient.CreateGuestPlayerProfileAsync(team.Id, currentUserId, guestName, guestRankRating, guestProfileLink);
            }
            else
            {
                await TeamsClient.UpdateGuestPlayerProfileAsync(team.Id, editingGuestProfile.Id, currentUserId, guestName, guestRankRating, guestProfileLink);
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
        transferOwnershipDialogErrorMessage = null;
        isTransferOwnershipDialogOpen = true;
        return Task.CompletedTask;
    }

    private Task CancelTransferOwnershipAsync()
    {
        isTransferOwnershipDialogOpen = false;
        transferOwnershipTargetMember = null;
        transferOwnershipDialogErrorMessage = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmTransferOwnershipAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId) || transferOwnershipTargetMember is null)
            return;

        isBusy = true;
        errorMessage = null;
        transferOwnershipDialogErrorMessage = null;
        try
        {
            team = await TeamsClient.TransferOwnershipAsync(team.Id, currentUserId, transferOwnershipTargetMember.UserId);
            await RefreshTeamProfilesAsync();
            await CancelTransferOwnershipAsync();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
            transferOwnershipDialogErrorMessage = ex.Message;
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
            teamProfileLink = team.ProfileLink ?? string.Empty;
            teamPrimaryColor = team.PrimaryColor ?? string.Empty;
            teamBannerAssetId = team.BannerAssetId;
            availableProfiles = await TeamsClient.GetAvailableProfilesAsync(team.Id, team.GameId);
            registrations = await RegistrationsClient.GetTeamRegistrationsAsync(team.Id);
        }
    }

    private Task StartRosterRefreshAsync(TournamentRegistrationDto registration)
    {
        rosterRefreshRegistration = registration;
        rosterRefreshSelectedProfileIds = GetInitialRosterRefreshSelection(registration);
        rosterRefreshDialogErrorMessage = null;
        isRosterRefreshDialogOpen = true;
        return Task.CompletedTask;
    }

    private Task CancelRosterRefreshAsync()
    {
        isRosterRefreshDialogOpen = false;
        rosterRefreshRegistration = null;
        rosterRefreshSelectedProfileIds = [];
        rosterRefreshDialogErrorMessage = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmRosterRefreshAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId) || rosterRefreshRegistration is null)
            return;

        var selectedProfileIds = rosterRefreshSelectedProfileIds.ToArray();
        if (selectedProfileIds.Length == 0)
        {
            rosterRefreshDialogErrorMessage = "Select at least one roster profile.";
            errorMessage = rosterRefreshDialogErrorMessage;
            return;
        }

        isBusy = true;
        errorMessage = null;
        rosterRefreshDialogErrorMessage = null;
        try
        {
            await RegistrationsClient.SubmitRosterChangeAsync(rosterRefreshRegistration.Id, currentUserId, selectedProfileIds);
            await RefreshTeamProfilesAsync();
            await CancelRosterRefreshAsync();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
            rosterRefreshDialogErrorMessage = ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private Task SetRosterRefreshSelectionAsync(Guid profileId, bool selected)
    {
        rosterRefreshDialogErrorMessage = null;

        if (selected)
        {
            rosterRefreshSelectedProfileIds.Add(profileId);
        }
        else
        {
            rosterRefreshSelectedProfileIds.Remove(profileId);
        }

        return Task.CompletedTask;
    }

    private static string FormatRank(PlayerProfileDto profile)
    {
        return PlayerRankFormatter.Format(profile.GameId, profile.RankRating);
    }

    private HashSet<Guid> GetInitialRosterRefreshSelection(TournamentRegistrationDto registration)
    {
        var selection = new HashSet<Guid>();
        var activeRoster = registration.Rosters.FirstOrDefault(roster => roster.Id == registration.ActiveRosterId);
        if (activeRoster is null)
        {
            return selection;
        }

        foreach (var profile in availableProfiles)
        {
            if (activeRoster.PlayerSnapshots.Any(snapshot => snapshot.SourcePlayerProfileId == profile.Id))
            {
                selection.Add(profile.Id);
            }
        }

        if (selection.Count == 0)
        {
            foreach (var profile in availableProfiles)
            {
                selection.Add(profile.Id);
            }
        }

        return selection;
    }

    private RosterRefreshDiffDto GetRosterRefreshDiff()
    {
        if (rosterRefreshRegistration is null)
        {
            return new RosterRefreshDiffDto([], [], [], [], []);
        }

        var activeRoster = rosterRefreshRegistration.Rosters.FirstOrDefault(roster => roster.Id == rosterRefreshRegistration.ActiveRosterId);
        if (activeRoster is null)
        {
            return new RosterRefreshDiffDto([], [], [], [], []);
        }

        var currentProfilesById = availableProfiles.ToDictionary(profile => profile.Id, profile => profile);
        var selectedNewIds = rosterRefreshSelectedProfileIds;
        var oldSnapshotBySourceId = activeRoster.PlayerSnapshots
            .Where(snapshot => snapshot.SourcePlayerProfileId is Guid)
            .ToDictionary(snapshot => snapshot.SourcePlayerProfileId!.Value, snapshot => snapshot);

        var oldRoster = activeRoster.PlayerSnapshots
            .Select(snapshot => snapshot.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var added = new List<string>();
        var removed = new List<string>();
        var renamed = new List<string>();
        var unchanged = new List<string>();

        foreach (var oldSnapshot in activeRoster.PlayerSnapshots)
        {
            if (oldSnapshot.SourcePlayerProfileId is not Guid sourceId)
            {
                removed.Add($"{oldSnapshot.Name} (legacy snapshot)");
                continue;
            }

            if (!selectedNewIds.Contains(sourceId))
            {
                removed.Add(oldSnapshot.Name);
                continue;
            }

            if (!currentProfilesById.TryGetValue(sourceId, out var currentProfile))
            {
                removed.Add($"{oldSnapshot.Name} (profile unavailable)");
                continue;
            }

            if (!string.Equals(oldSnapshot.Name, currentProfile.Name, StringComparison.Ordinal))
            {
                renamed.Add($"{oldSnapshot.Name} -> {currentProfile.Name}");
            }
            else
            {
                unchanged.Add(currentProfile.Name);
            }
        }

        foreach (var profileId in selectedNewIds)
        {
            if (!currentProfilesById.TryGetValue(profileId, out var profile))
            {
                continue;
            }

            if (!oldSnapshotBySourceId.ContainsKey(profileId))
            {
                added.Add(profile.Name);
            }
        }

        added.Sort(StringComparer.OrdinalIgnoreCase);
        removed.Sort(StringComparer.OrdinalIgnoreCase);
        renamed.Sort(StringComparer.OrdinalIgnoreCase);
        unchanged.Sort(StringComparer.OrdinalIgnoreCase);

        return new RosterRefreshDiffDto(oldRoster, added, removed, renamed, unchanged);
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

public sealed record RosterRefreshDiffDto(
    IReadOnlyList<string> OldRoster,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Renamed,
    IReadOnlyList<string> Unchanged);
