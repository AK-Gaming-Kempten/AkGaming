using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TeamHeaderBanner : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public bool CanEdit { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback EditRequested { get; set; }

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

    private async Task RequestEditAsync()
    {
        isMenuOpen = false;
        await EditRequested.InvokeAsync();
    }
}
