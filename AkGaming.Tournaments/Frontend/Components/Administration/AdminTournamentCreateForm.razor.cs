using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminTournamentCreateForm : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public string TournamentName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TournamentNameChanged { get; set; }
    [Parameter] public string TournamentSlug { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TournamentSlugChanged { get; set; }
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameIdChanged { get; set; }
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private Task HandleTournamentNameChanged(ChangeEventArgs args)
        => TournamentNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleTournamentSlugChanged(ChangeEventArgs args)
        => TournamentSlugChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleGameIdChanged(ChangeEventArgs args)
        => GameIdChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleVisibilityChanged(ChangeEventArgs args)
        => IsVisibleChanged.InvokeAsync(args.Value is bool value && value);
}
