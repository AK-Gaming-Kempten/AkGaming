using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.General;

public partial class PlayerProfileDetailsView : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public PlayerProfileDto? Profile { get; set; }
    [Parameter] public bool IsCreating { get; set; }
    [Parameter] public bool IsEditMode { get; set; }
    [Parameter] public string GameId { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> GameIdChanged { get; set; }
    [Parameter] public string ProfileName { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ProfileNameChanged { get; set; }
    [Parameter] public string ProfileLink { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ProfileLinkChanged { get; set; }
    [Parameter] public int? RankRating { get; set; }
    [Parameter] public EventCallback<int?> RankRatingChanged { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnEditRequested { get; set; }
    [Parameter] public EventCallback<MediaAssetDto> OnLogoUploaded { get; set; }
    [Parameter] public EventCallback OnLogoCleared { get; set; }

    private string GetGameName()
        => Games.FirstOrDefault(game => string.Equals(game.Id, Profile?.GameId, StringComparison.OrdinalIgnoreCase))?.Name
           ?? Profile?.GameId
           ?? string.Empty;
}
