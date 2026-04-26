using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class AdminGameCreateForm : ComponentBase
{
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameIdChanged { get; set; }
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameNameChanged { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private Task HandleGameIdChanged(ChangeEventArgs args)
        => GameIdChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleGameNameChanged(ChangeEventArgs args)
        => GameNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
