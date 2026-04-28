using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class TournamentCard : ComponentBase
{
    [Parameter] public TournamentSummaryDto Summary { get; set; } = default!;

    private string StatusClass => Summary.Status switch
    {
        TournamentStatusDto.RegistrationOpen => "status-pill-positive",
        TournamentStatusDto.RegistrationClosed => "status-pill-warn",
        _ => "status-pill-neutral"
    };

    private string StatusLabel => Summary.Status switch
    {
        TournamentStatusDto.RegistrationOpen => "Registration open",
        TournamentStatusDto.RegistrationClosed => "Registration closed",
        TournamentStatusDto.InProgress => "In progress",
        TournamentStatusDto.Completed => "Completed",
        TournamentStatusDto.Archived => "Archived",
        _ => "Draft"
    };

    private string DateLabel => Summary.StartUtc?.ToLocalTime().ToString("dd MMM yyyy")
                                ?? Summary.RegistrationClosedUtc?.ToLocalTime().ToString("dd MMM yyyy")
                                ?? "TBA";
}
