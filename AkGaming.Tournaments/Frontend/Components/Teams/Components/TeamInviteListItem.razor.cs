using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamInviteListItem : ComponentBase
{
    [Parameter] public TeamInviteKeyDto Invite { get; set; } = default!;
    [Parameter] public string InviteUrl { get; set; } = string.Empty;
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<TeamInviteKeyDto> CopyRequested { get; set; }
    [Parameter] public EventCallback<TeamInviteKeyDto> RevokeRequested { get; set; }

    private bool isMenuOpen;
    private bool isUrlVisible;

    private string InviteDisplayText
        => isUrlVisible ? InviteUrl : "Invite link hidden";

    private Task ToggleMenuAsync()
    {
        isMenuOpen = !isMenuOpen;
        return Task.CompletedTask;
    }

    private Task CloseMenuAsync()
    {
        isMenuOpen = false;
        return Task.CompletedTask;
    }

    private async Task OnCopy()
    {
        await CopyRequested.InvokeAsync(Invite);
    }

    private async Task OnRevoke()
    {
        isMenuOpen = false;
        await RevokeRequested.InvokeAsync(Invite);
    }

    private Task ToggleVisibilityAsync()
    {
        isUrlVisible = !isUrlVisible;
        return Task.CompletedTask;
    }
}
