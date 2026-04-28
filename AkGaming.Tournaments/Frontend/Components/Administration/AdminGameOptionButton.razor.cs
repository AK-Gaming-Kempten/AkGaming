using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Administration;

public partial class AdminGameOptionButton : ComponentBase
{
    [Parameter] public GameDto Game { get; set; } = default!;
    [Parameter] public bool Selected { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<GameDto> OnSelected { get; set; }

    private Task SelectAsync()
        => OnSelected.InvokeAsync(Game);
}
