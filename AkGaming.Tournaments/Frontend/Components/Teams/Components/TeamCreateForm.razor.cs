using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamCreateForm : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameIdChanged { get; set; }
    [Parameter] public string TeamName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TeamNameChanged { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private Task HandleTeamNameChanged(ChangeEventArgs args)
        => TeamNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
