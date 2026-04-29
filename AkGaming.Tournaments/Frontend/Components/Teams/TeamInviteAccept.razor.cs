using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Tournaments.Frontend.Components.Teams;

public partial class TeamInviteAccept : ComponentBase
{
    [Parameter] public Guid TeamId { get; set; }
    [Parameter] public string Key { get; set; } = string.Empty;

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private TeamDto? team;
    private string? currentUserId;
    private string? currentUserDisplayName;
    private string? errorMessage;
    private string loginUrl = "/authentication/login";
    private bool isAuthenticated;
    private bool isLoading = true;
    private bool isBusy;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        errorMessage = null;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        currentUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? authState.User.FindFirstValue("sub");
        currentUserDisplayName = authState.User.FindFirstValue("preferred_username")
                                 ?? authState.User.FindFirstValue(ClaimTypes.Name)
                                 ?? currentUserId;

        var currentPath = $"/teams/{TeamId}/invite/{Uri.EscapeDataString(Key)}";
        loginUrl = $"/authentication/login?returnUrl={Uri.EscapeDataString(currentPath)}";

        try
        {
            team = await TeamsClient.GetTeamAsync(TeamId);
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

    private async Task AcceptInviteAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId))
        {
            return;
        }

        isBusy = true;
        errorMessage = null;
        try
        {
            await TeamsClient.AcceptInviteKeyAsync(team.Id, Key, currentUserId);
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
}
