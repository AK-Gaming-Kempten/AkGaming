using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminTournamentOptionList : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentSummaryDto> Tournaments { get; set; } = [];
    [Parameter] public TournamentSummaryDto? SelectedTournament { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<TournamentSummaryDto> OnSelected { get; set; }
}
