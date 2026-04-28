using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TournamentInfoFieldGrid : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentInfoField> Fields { get; set; } = [];
}
