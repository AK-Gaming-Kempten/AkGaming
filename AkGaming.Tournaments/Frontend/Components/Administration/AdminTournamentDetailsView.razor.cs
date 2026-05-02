using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminTournamentDetailsView : ComponentBase
{
    [Parameter] public TournamentSummaryDto? Tournament { get; set; }
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback SaveVisibilityRequested { get; set; }
    [Parameter] public EventCallback DeleteTournamentRequested { get; set; }

    private Task HandleVisibilityChanged(ChangeEventArgs args)
        => IsVisibleChanged.InvokeAsync(args.Value is bool value && value);

    private string GetBaseSettingsHref()
    {
        if (Tournament is null)
        {
            return "#";
        }

        return $"/tournaments/{Tournament.Slug}/administration/base-settings";
    }
}
