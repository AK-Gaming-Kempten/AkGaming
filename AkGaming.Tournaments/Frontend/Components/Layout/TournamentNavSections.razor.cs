using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Layout;

public partial class TournamentNavSections : ComponentBase
{
    [Parameter] public bool HasSelectedTournament { get; set; }
    [Parameter] public bool IsAuthenticated { get; set; }
    [Parameter] public bool IsAdmin { get; set; }
    [Parameter] public bool IsPublicExpanded { get; set; }
    [Parameter] public bool IsPlayerExpanded { get; set; }
    [Parameter] public bool IsAdministrationExpanded { get; set; }
    [Parameter] public string SelectedTournamentSlug { get; set; } = string.Empty;
    [Parameter] public EventCallback TogglePublicExpanded { get; set; }
    [Parameter] public EventCallback TogglePlayerExpanded { get; set; }
    [Parameter] public EventCallback ToggleAdministrationExpanded { get; set; }
    [Parameter] public EventCallback NotifyNavigation { get; set; }

    private string BuildTournamentHref(string? childPath = null)
    {
        if (string.IsNullOrWhiteSpace(childPath))
            return $"/tournaments/{SelectedTournamentSlug}";

        return $"/tournaments/{SelectedTournamentSlug}/{childPath}";
    }
}
