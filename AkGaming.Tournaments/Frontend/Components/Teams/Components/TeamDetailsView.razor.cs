using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TeamDetailsView : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<PlayerProfileDto> AvailableProfiles { get; set; } = [];
    [Parameter] public IReadOnlyList<TournamentRegistrationDto> Registrations { get; set; } = [];
    [Parameter] public bool IsAuthenticated { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<MediaAssetDto> OnLogoUploaded { get; set; }
    [Parameter] public EventCallback OnLogoCleared { get; set; }
}
