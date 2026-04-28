using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.General;

public partial class PlayerProfileList : ComponentBase
{
    [Parameter] public IReadOnlyList<PlayerProfileDto> Profiles { get; set; } = [];
    [Parameter] public string EmptyState { get; set; } = "No player profiles found.";
    [Parameter] public bool Selectable { get; set; }
    [Parameter] public IReadOnlySet<Guid> SelectedProfileIds { get; set; } = new HashSet<Guid>();
    [Parameter] public EventCallback<Guid> OnProfileSelected { get; set; }
    [Parameter] public EventCallback<Guid> OnProfileDeselected { get; set; }
    [Parameter] public bool AllowActions { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<PlayerProfileDto> OnEditRequested { get; set; }

    private Guid? openMenuProfileId;

    private Task ToggleSelection(Guid profileId, ChangeEventArgs args)
    {
        var isChecked = args.Value as bool? ?? false;
        return isChecked
            ? OnProfileSelected.InvokeAsync(profileId)
            : OnProfileDeselected.InvokeAsync(profileId);
    }

    private static string FormatOwner(PlayerProfileDto profile)
        => profile.Type == PlayerProfileTypeDto.User ? "User profile" : "Guest";

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
        await OnEditRequested.InvokeAsync(profile);
    }
}
