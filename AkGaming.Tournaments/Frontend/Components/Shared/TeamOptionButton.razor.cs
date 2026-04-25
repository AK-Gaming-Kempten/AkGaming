using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TeamOptionButton : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public string RoleLabel { get; set; } = "Member";
    [Parameter] public bool Selected { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<TeamDto> OnSelected { get; set; }

    private Task SelectAsync()
        => OnSelected.InvokeAsync(Team);
}
