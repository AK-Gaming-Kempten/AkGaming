using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Layout;

public partial class TournamentContextPanel : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public bool CanChangeTournament { get; set; }
    [Parameter] public bool IsAdmin { get; set; }
    [Parameter] public string CurrentTournamentName { get; set; } = string.Empty;
    [Parameter] public string? CurrentTournamentSubline { get; set; }
    [Parameter] public string SelectedTournamentSlug { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<TournamentSummary> Tournaments { get; set; } = [];
    [Parameter] public EventCallback<ChangeEventArgs> TournamentChanged { get; set; }
}
