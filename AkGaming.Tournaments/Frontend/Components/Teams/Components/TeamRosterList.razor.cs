using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Components.General;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamRosterList : ComponentBase
{
    [Parameter] public IReadOnlyList<PlayerProfileDto> Profiles { get; set; } = [];
    [Parameter] public string EmptyState { get; set; } = "No player profiles are available.";
    [Parameter] public bool CanEdit { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> EditGuestRequested { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> DeleteGuestRequested { get; set; }

    private Guid? openMenuProfileId;

    private static string FormatProfileMeta(PlayerProfileDto profile)
    {
        var source = profile.Type == PlayerProfileTypeDto.Guest ? "Guest profile" : "User profile";
        return $"{profile.GameId} · {source} · {PlayerRankFormatter.Format(profile.GameId, profile.RankRating)}";
    }

    private Task ToggleMenuAsync(Guid profileId)
    {
        openMenuProfileId = openMenuProfileId == profileId ? null : profileId;
        return Task.CompletedTask;
    }

    private Task CloseMenuAsync()
    {
        openMenuProfileId = null;
        return Task.CompletedTask;
    }

    private async Task RequestEditAsync(PlayerProfileDto profile)
    {
        openMenuProfileId = null;
        await EditGuestRequested.InvokeAsync(profile);
    }

    private async Task RequestDeleteAsync(PlayerProfileDto profile)
    {
        openMenuProfileId = null;
        await DeleteGuestRequested.InvokeAsync(profile);
    }
}
