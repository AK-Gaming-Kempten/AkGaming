using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class PlayerProfileLogoList : ComponentBase
{
    [Parameter] public IReadOnlyList<PlayerProfileDto> Profiles { get; set; } = [];
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<PlayerProfileLogoUpload> LogoUploaded { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> LogoCleared { get; set; }
}

public sealed record PlayerProfileLogoUpload(PlayerProfileDto Profile, MediaAssetDto Asset);
