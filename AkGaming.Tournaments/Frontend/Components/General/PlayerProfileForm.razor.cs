using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class PlayerProfileForm : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameIdChanged { get; set; }
    [Parameter] public string ProfileName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ProfileNameChanged { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }

    private Task HandleProfileNameChanged(ChangeEventArgs args)
        => ProfileNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
