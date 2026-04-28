using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TournamentRegistrationTeamPicker : ComponentBase
{
    [Parameter] public IReadOnlyList<TeamDto> Teams { get; set; } = [];
    [Parameter] public TeamDto? SelectedTeam { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<TeamDto> TeamSelected { get; set; }

    private bool isOpen;

    private Task ToggleOpenAsync()
    {
        isOpen = !isOpen;
        return Task.CompletedTask;
    }

    private async Task SelectTeamAsync(TeamDto team)
    {
        isOpen = false;
        await TeamSelected.InvokeAsync(team);
    }
}
