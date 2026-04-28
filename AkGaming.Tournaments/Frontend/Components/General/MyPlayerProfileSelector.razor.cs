using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class MyPlayerProfileSelector : ComponentBase
{
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public IReadOnlyList<PlayerProfileDto> Profiles { get; set; } = [];
    [Parameter] public PlayerProfileDto? SelectedProfile { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> ProfileSelected { get; set; }
    [Parameter] public EventCallback CreateRequested { get; set; }

    private async Task ToggleAsync()
        => await SetOpenAsync(!IsOpen);

    private async Task CloseAsync()
        => await SetOpenAsync(false);

    private async Task SelectAsync(PlayerProfileDto profile)
    {
        if (ProfileSelected.HasDelegate)
            await ProfileSelected.InvokeAsync(profile);

        await SetOpenAsync(false);
    }

    private async Task CreateAsync()
    {
        if (CreateRequested.HasDelegate)
            await CreateRequested.InvokeAsync();

        await SetOpenAsync(false);
    }

    private async Task SetOpenAsync(bool value)
    {
        if (IsOpen == value)
            return;

        IsOpen = value;
        await IsOpenChanged.InvokeAsync(value);
    }

    private string GetSelectedTitle()
        => SelectedProfile is null ? "Select profile" : SelectedProfile.Name;

    private string GetSelectedSummary()
    {
        if (SelectedProfile is null)
            return "Choose one of your profiles or create a new one.";

        var gameName = Games.FirstOrDefault(game => string.Equals(game.Id, SelectedProfile.GameId, StringComparison.OrdinalIgnoreCase))?.Name
                       ?? SelectedProfile.GameId;
        return $"{gameName} · {PlayerRankFormatter.Format(SelectedProfile.GameId, SelectedProfile.RankRating)}";
    }
}
