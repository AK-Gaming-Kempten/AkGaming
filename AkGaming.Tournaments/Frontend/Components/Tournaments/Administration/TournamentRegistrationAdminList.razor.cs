using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class TournamentRegistrationAdminList : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentRegistrationDto> Registrations { get; set; } = [];
    [Parameter] public IReadOnlyDictionary<Guid, string> TeamNames { get; set; } = new Dictionary<Guid, string>();
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback<TournamentRegistrationReviewAction> ReviewRequested { get; set; }
    [Parameter] public EventCallback<Guid> DeleteRequested { get; set; }

    private Task ReviewAsync(Guid registrationId, bool approve)
        => ReviewRequested.InvokeAsync(new TournamentRegistrationReviewAction(registrationId, approve));

    private Task DeleteAsync(Guid registrationId)
        => DeleteRequested.InvokeAsync(registrationId);

    private string GetTeamName(Guid teamId)
        => TeamNames.TryGetValue(teamId, out var name) ? name : teamId.ToString();

    private static string GetStatusClass(TournamentRegistrationStatusDto status)
        => status switch
        {
            TournamentRegistrationStatusDto.Approved => "status-pill-positive",
            TournamentRegistrationStatusDto.Pending => "status-pill-warn",
            _ => "status-pill-neutral"
        };
}

public sealed record TournamentRegistrationReviewAction(Guid RegistrationId, bool Approve);
