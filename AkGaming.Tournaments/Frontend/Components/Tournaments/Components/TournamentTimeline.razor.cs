using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class TournamentTimeline : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentTimelineItem> Items { get; set; } = [];
}
