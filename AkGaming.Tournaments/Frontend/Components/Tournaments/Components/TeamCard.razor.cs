using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TeamCard : ComponentBase
{
    [Parameter] public TeamCardModel Team { get; set; } = default!;
}
