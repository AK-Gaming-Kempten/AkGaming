using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class RegistrationList : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentRegistrationDto> Registrations { get; set; } = [];
    [Parameter] public IReadOnlyDictionary<Guid, string> TournamentNames { get; set; } = new Dictionary<Guid, string>();
    [Parameter] public string EmptyState { get; set; } = "No registrations found.";
    [Parameter] public bool CanRequestRosterRefresh { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<TournamentRegistrationDto> RosterRefreshRequested { get; set; }

    private static string StatusClass(TournamentRegistrationStatusDto status)
        => status switch
        {
            TournamentRegistrationStatusDto.Approved => "status-pill-positive",
            TournamentRegistrationStatusDto.Pending => "status-pill-warn",
            _ => "status-pill-neutral"
        };

    private string GetTournamentName(Guid tournamentId)
        => TournamentNames.TryGetValue(tournamentId, out var name) ? name : tournamentId.ToString();

    private static bool HasPendingRoster(TournamentRegistrationDto registration)
        => registration.Rosters.Any(roster => roster.Status == RosterStatusDto.Pending);

    private async Task RequestRefreshAsync(TournamentRegistrationDto registration)
    {
        await RosterRefreshRequested.InvokeAsync(registration);
    }
}
