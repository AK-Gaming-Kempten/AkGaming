using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class MatchCard : ComponentBase
{
    [Parameter] public MatchCardModel Match { get; set; } = default!;

    private string ToneClass => Match.Tone switch
    {
        "positive" => "status-pill-positive",
        "warn" => "status-pill-warn",
        _ => "status-pill-neutral"
    };
}
