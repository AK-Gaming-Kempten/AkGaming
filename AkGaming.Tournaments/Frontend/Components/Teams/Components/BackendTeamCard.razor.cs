using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class BackendTeamCard : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
}
