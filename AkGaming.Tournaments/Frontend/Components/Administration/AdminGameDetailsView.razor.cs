using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public partial class AdminGameDetailsView : ComponentBase
{
    [Parameter] public GameDto? Game { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<MediaAssetDto> LogoUploaded { get; set; }
    [Parameter] public EventCallback ClearLogoRequested { get; set; }
    [Parameter] public EventCallback DeleteGameRequested { get; set; }
}
