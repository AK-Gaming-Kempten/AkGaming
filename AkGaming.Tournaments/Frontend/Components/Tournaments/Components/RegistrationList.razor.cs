using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class RegistrationList : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentRegistrationDto> Registrations { get; set; } = [];
    [Parameter] public string EmptyState { get; set; } = "No registrations found.";

    private static string StatusClass(TournamentRegistrationStatusDto status)
        => status switch
        {
            TournamentRegistrationStatusDto.Approved => "status-pill-positive",
            TournamentRegistrationStatusDto.Pending => "status-pill-warn",
            _ => "status-pill-neutral"
        };
}
