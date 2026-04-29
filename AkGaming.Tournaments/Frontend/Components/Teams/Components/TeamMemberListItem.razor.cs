using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamMemberListItem : ComponentBase
{
    [Parameter] public TeamMembershipDto Member { get; set; } = default!;
    [Parameter] public string DisplayName { get; set; } = string.Empty;
    [Parameter] public string Initial { get; set; } = "?";
    [Parameter] public bool CanManageRoles { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<TeamMembershipDto> PromoteToEditorRequested { get; set; }
    [Parameter] public EventCallback<TeamMembershipDto> DemoteToMemberRequested { get; set; }
    [Parameter] public EventCallback<TeamMembershipDto> TransferOwnershipRequested { get; set; }

    private bool isMenuOpen;

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

    private async Task PromoteToEditorAsync()
    {
        isMenuOpen = false;
        await PromoteToEditorRequested.InvokeAsync(Member);
    }

    private async Task DemoteToMemberAsync()
    {
        isMenuOpen = false;
        await DemoteToMemberRequested.InvokeAsync(Member);
    }

    private async Task TransferOwnershipRequestedAsync()
    {
        isMenuOpen = false;
        await TransferOwnershipRequested.InvokeAsync(Member);
    }
}
