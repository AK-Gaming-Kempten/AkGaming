using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class TournamentRegistrationEligibilityView : ComponentBase
{
    [Parameter] public TournamentRegistrationEligibilityDto Eligibility { get; set; } = default!;
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<PlayerSelectionChanged> PlayerSelectionChanged { get; set; }

    private Task TogglePlayerAsync(Guid playerProfileId, ChangeEventArgs args)
    {
        var isSelected = args.Value as bool? ?? false;
        return PlayerSelectionChanged.InvokeAsync(new PlayerSelectionChanged(playerProfileId, isSelected));
    }
}

public sealed record PlayerSelectionChanged(Guid PlayerProfileId, bool Selected);
