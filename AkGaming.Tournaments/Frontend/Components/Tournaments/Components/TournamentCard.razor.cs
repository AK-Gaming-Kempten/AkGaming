using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class TournamentCard : ComponentBase
{
    [Parameter] public TournamentSummary Summary { get; set; } = default!;

    private string StatusClass => Summary.StatusTone switch
    {
        "positive" => "status-pill-positive",
        "warn" => "status-pill-warn",
        _ => "status-pill-neutral"
    };
}
