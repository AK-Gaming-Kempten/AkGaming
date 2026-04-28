using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class PlayerProfileOptionButton : ComponentBase
{
    [Parameter] public PlayerProfileDto Profile { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public Guid? GameLogoAssetId { get; set; }
    [Parameter] public bool Selected { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> OnSelected { get; set; }

    private Task SelectAsync()
        => OnSelected.InvokeAsync(Profile);
}
