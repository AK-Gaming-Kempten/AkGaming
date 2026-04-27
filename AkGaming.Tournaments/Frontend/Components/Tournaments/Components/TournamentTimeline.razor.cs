using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TournamentTimeline : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentTimelineEntry> Items { get; set; } = [];
}
