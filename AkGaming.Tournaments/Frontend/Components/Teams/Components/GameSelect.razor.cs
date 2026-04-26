using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class GameSelect : ComponentBase
{
    [Parameter] public string Id { get; set; } = "game";
    [Parameter] public string CssClass { get; set; } = "form-input";
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = "Select game";
    [Parameter] public bool Disabled { get; set; }

    private Task HandleChanged(ChangeEventArgs args)
        => ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
