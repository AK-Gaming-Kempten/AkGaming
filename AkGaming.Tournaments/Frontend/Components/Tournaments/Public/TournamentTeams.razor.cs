using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Public;

public partial class TournamentTeams
{
    private static string GetStatusClass(TournamentRegistrationStatusDto? status)
        => status switch
        {
            TournamentRegistrationStatusDto.Approved => "status-pill-positive",
            TournamentRegistrationStatusDto.Pending => "status-pill-warn",
            _ => "status-pill-neutral"
        };
}
