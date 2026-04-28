using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.General;

public partial class PlayerProfileOptionList : ComponentBase
{
    [Parameter] public IReadOnlyList<PlayerProfileDto> Profiles { get; set; } = [];
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public string? SelectedGameId { get; set; }
    [Parameter] public string EmptyState { get; set; } = "No player profiles found.";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> OnSelected { get; set; }

    private string GetGameName(string gameId)
        => Games.FirstOrDefault(game => string.Equals(game.Id, gameId, StringComparison.OrdinalIgnoreCase))?.Name ?? gameId;

    private Guid? GetGameLogoAssetId(string gameId)
        => Games.FirstOrDefault(game => string.Equals(game.Id, gameId, StringComparison.OrdinalIgnoreCase))?.LogoAssetId;

    private bool IsSelected(PlayerProfileDto profile)
        => !string.IsNullOrWhiteSpace(SelectedGameId)
           && string.Equals(profile.GameId, SelectedGameId, StringComparison.OrdinalIgnoreCase);
}
