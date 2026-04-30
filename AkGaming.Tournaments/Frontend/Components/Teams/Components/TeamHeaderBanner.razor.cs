using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamHeaderBanner : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public bool CanEdit { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback EditRequested { get; set; }
    [Parameter] public EventCallback InviteManagementRequested { get; set; }
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private bool isMenuOpen;
    private const string DefaultMediumGreen = "#1f7a52";
    private const string DefaultDarkGreen = "#0f3f2a";

    private string BannerStyle
    {
        get
        {
            var baseColor = string.IsNullOrWhiteSpace(Team.PrimaryColor) ? DefaultDarkGreen : Team.PrimaryColor!;
            var bannerUrl = Team.BannerAssetId is Guid bannerAssetId
                ? $"/media-assets/{bannerAssetId}/file"
                : null;
            var imageLayer = bannerUrl is null
                ? "none"
                : $"url('{bannerUrl}')";
            var leftColor = bannerUrl is null ? DefaultMediumGreen : baseColor;
            return $"--team-banner-left:{leftColor};--team-banner-base:{baseColor};--team-banner-image:{imageLayer};";
        }
    }

    private bool CanShowMenu => true;

    private Task ToggleMenuAsync()
    {
        isMenuOpen = !isMenuOpen;
        return Task.CompletedTask;
    }

    private Task CloseMenuAsync()
    {
        isMenuOpen = false;
        return Task.CompletedTask;
    }

    private async Task RequestEditAsync()
    {
        isMenuOpen = false;
        await EditRequested.InvokeAsync();
    }

    private async Task RequestInvitesAsync()
    {
        isMenuOpen = false;
        await InviteManagementRequested.InvokeAsync();
    }

    private async Task OpenTeamLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(Team.ProfileLink))
            return;

        await Js.InvokeVoidAsync("open", Team.ProfileLink, "_blank", "noopener,noreferrer");
        isMenuOpen = false;
    }
}
