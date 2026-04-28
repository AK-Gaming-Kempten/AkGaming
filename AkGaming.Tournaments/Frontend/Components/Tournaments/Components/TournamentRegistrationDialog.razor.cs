using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class TournamentRegistrationDialog : ComponentBase
{
    [Parameter] public TournamentDto Tournament { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<TournamentRegistrationDto> RegistrationSubmitted { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private TournamentRegistrationsApiClient RegistrationsClient { get; set; } = default!;

    private IReadOnlyList<TeamDto> teams = [];
    private TeamDto? selectedTeam;
    private TournamentRegistrationEligibilityDto? eligibility;
    private HashSet<Guid> selectedPlayerProfileIds = [];
    private string? currentUserId;
    private string? errorMessage;
    private string? successMessage;
    private bool wasOpen;
    private bool isLoading;
    private bool isBusy;

    private bool CanSubmit => !isLoading && !isBusy && eligibility?.CanSubmit == true && selectedTeam is not null && !string.IsNullOrWhiteSpace(currentUserId);

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !wasOpen)
        {
            wasOpen = true;
            await LoadAsync();
        }
        else if (!IsOpen)
        {
            wasOpen = false;
        }
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        isBusy = false;
        errorMessage = null;
        successMessage = null;
        eligibility = null;
        selectedTeam = null;
        selectedPlayerProfileIds = [];

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? authState.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            isLoading = false;
            return;
        }

        await RunApiActionAsync(async () =>
        {
            var userTeams = await TeamsClient.GetUserTeamsAsync(currentUserId);
            teams = userTeams
                .Where(team => string.Equals(team.GameId, Tournament.GameId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            selectedTeam = teams.FirstOrDefault();

            if (selectedTeam is not null)
                await RefreshEligibilityAsync(useCurrentSelection: false);
        });

        isLoading = false;
    }

    private async Task SelectTeamAsync(TeamDto team)
    {
        selectedTeam = team;
        successMessage = null;
        selectedPlayerProfileIds = [];
        await RefreshEligibilityAsync(useCurrentSelection: false);
    }

    private async Task SetPlayerSelectionAsync(PlayerSelectionChanged selection)
    {
        if (selection.Selected)
            selectedPlayerProfileIds.Add(selection.PlayerProfileId);
        else
            selectedPlayerProfileIds.Remove(selection.PlayerProfileId);

        successMessage = null;
        await RefreshEligibilityAsync(useCurrentSelection: true);
    }

    private async Task RefreshEligibilityAsync(bool useCurrentSelection)
    {
        if (selectedTeam is null || string.IsNullOrWhiteSpace(currentUserId))
            return;

        await RunApiActionAsync(async () =>
        {
            var selectedIds = useCurrentSelection ? selectedPlayerProfileIds.ToArray() : Array.Empty<Guid>();
            eligibility = await RegistrationsClient.GetEligibilityAsync(
                selectedTeam.Id,
                Tournament.Id,
                currentUserId,
                selectedIds);
            selectedPlayerProfileIds = eligibility.Players
                .Where(player => player.Selected)
                .Select(player => player.PlayerProfileId)
                .ToHashSet();
        });
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmit || selectedTeam is null || string.IsNullOrWhiteSpace(currentUserId))
            return;

        await RunApiActionAsync(async () =>
        {
            var registration = await RegistrationsClient.SubmitRegistrationAsync(
                selectedTeam.Id,
                Tournament.Id,
                currentUserId,
                selectedPlayerProfileIds.ToArray());
            if (RegistrationSubmitted.HasDelegate)
            {
                await RegistrationSubmitted.InvokeAsync(registration);
                return;
            }

            successMessage = $"Registration submitted with status {registration.Status}.";
            await RefreshEligibilityAsync(useCurrentSelection: true);
        });
    }

    private async Task CloseAsync()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
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
}
