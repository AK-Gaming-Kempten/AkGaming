using System.Security.Claims;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AkGaming.Tournaments.Frontend.Components.Teams;

public partial class TeamInvites : ComponentBase
{
    [Parameter] public Guid TeamId { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private TeamDto? team;
    private IReadOnlyList<TeamInviteKeyDto> invites = [];
    private string? currentUserId;
    private string? errorMessage;
    private bool isAuthenticated;
    private bool canEdit;
    private bool isLoading = true;
    private bool isBusy;
    private int maxUses = 1;
    private bool isCreateDialogOpen;

    protected override async Task OnParametersSetAsync()
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

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            team = await TeamsClient.GetTeamAsync(TeamId);
            canEdit = team is not null && team.Memberships.Any(member =>
                string.Equals(member.UserId, currentUserId, StringComparison.OrdinalIgnoreCase)
                && (member.Role == TeamRoleDto.Owner || member.Role == TeamRoleDto.Editor));

            if (team is not null && canEdit)
            {
                invites = await TeamsClient.GetInviteKeysAsync(team.Id, currentUserId!);
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

    private Task HandleMaxUsesChanged(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var value))
        {
            maxUses = Math.Max(1, value);
        }

        return Task.CompletedTask;
    }

    private async Task CreateInviteAsync()
    {
        if (team is null || string.IsNullOrWhiteSpace(currentUserId))
        {
            return;
        }

        isBusy = true;
        errorMessage = null;
        try
        {
            await TeamsClient.CreateInviteKeyAsync(team.Id, currentUserId, maxUses);
            invites = await TeamsClient.GetInviteKeysAsync(team.Id, currentUserId);
            isCreateDialogOpen = false;
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

    private async Task RevokeInviteAsync(TeamInviteKeyDto invite)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return;
        }

        isBusy = true;
        errorMessage = null;
        try
        {
            await TeamsClient.RevokeInviteKeyAsync(invite.TeamId, invite.Key, currentUserId);
            invites = await TeamsClient.GetInviteKeysAsync(invite.TeamId, currentUserId);
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

    private async Task CopyInviteAsync(TeamInviteKeyDto invite)
    {
        var inviteUrl = GetInviteUrl(invite);
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", inviteUrl);
    }

    private Task OpenCreateDialogAsync()
    {
        isCreateDialogOpen = true;
        maxUses = 1;
        return Task.CompletedTask;
    }

    private Task CloseCreateDialogAsync()
    {
        isCreateDialogOpen = false;
        return Task.CompletedTask;
    }

    private string GetInviteUrl(TeamInviteKeyDto invite)
        => $"{Nav.BaseUri.TrimEnd('/')}/teams/{invite.TeamId}/invite/{invite.Key}";
}
