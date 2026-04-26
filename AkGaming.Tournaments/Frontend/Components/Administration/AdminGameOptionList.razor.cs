using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class AdminGameOptionList : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public GameDto? SelectedGame { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<GameDto> OnSelected { get; set; }
}
