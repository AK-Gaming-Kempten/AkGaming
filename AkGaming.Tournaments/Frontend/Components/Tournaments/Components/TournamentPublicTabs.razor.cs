using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TournamentPublicTabs : ComponentBase
{
    [Parameter] public string TournamentSlug { get; set; } = string.Empty;
    [Parameter] public string ActiveSection { get; set; } = "overview";
    [Parameter] public EventCallback OnRegisterRequested { get; set; }

    private string BuildHref(string? childPath)
    {
        if (string.IsNullOrWhiteSpace(childPath))
            return $"/tournaments/{TournamentSlug}";

        return $"/tournaments/{TournamentSlug}/{childPath}";
    }

    private string GetTabClass(string section)
        => string.Equals(ActiveSection, section, StringComparison.OrdinalIgnoreCase)
            ? "tournament-tab is-active"
            : "tournament-tab";
}
