using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.General;

public partial class PlayerProfileForm : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameIdChanged { get; set; }
    [Parameter] public bool GameSelectionEnabled { get; set; } = true;
    [Parameter] public string ProfileName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ProfileNameChanged { get; set; }
    [Parameter] public int? RankRating { get; set; }
    [Parameter] public EventCallback<int?> RankRatingChanged { get; set; }
    [Parameter] public PlayerProfileDto? EditingProfile { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback<MediaAssetDto> OnLogoUploaded { get; set; }
    [Parameter] public EventCallback OnLogoCleared { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool CanEditLogo => EditingProfile is not null;
    private string SubmitLabel => EditingProfile is null ? "Create profile" : "Save profile";

    private Task HandleProfileNameChanged(ChangeEventArgs args)
        => ProfileNameChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);

    private Task HandleLogoUploadedAsync(MediaAssetDto asset)
        => OnLogoUploaded.InvokeAsync(asset);

    private string GetGameName()
        => Games.FirstOrDefault(game => string.Equals(game.Id, GameId, StringComparison.OrdinalIgnoreCase))?.Name ?? GameId;

    private Guid? GetGameLogoAssetId()
        => Games.FirstOrDefault(game => string.Equals(game.Id, GameId, StringComparison.OrdinalIgnoreCase))?.LogoAssetId;
}
