using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminTournamentOptionButton : ComponentBase
{
    [Parameter] public TournamentSummaryDto Tournament { get; set; } = default!;
    [Parameter] public bool Selected { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<TournamentSummaryDto> OnSelected { get; set; }

    private Task SelectAsync()
        => OnSelected.InvokeAsync(Tournament);
}
