using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class PlayerProfileLogoRow : ComponentBase
{
    [Parameter] public PlayerProfileDto Profile { get; set; } = default!;
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<PlayerProfileLogoUpload> LogoUploaded { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> LogoCleared { get; set; }

    private Task HandleLogoUploadedAsync(MediaAssetDto asset)
        => LogoUploaded.InvokeAsync(new PlayerProfileLogoUpload(Profile, asset));

    private Task HandleLogoClearedAsync()
        => LogoCleared.InvokeAsync(Profile);
}
