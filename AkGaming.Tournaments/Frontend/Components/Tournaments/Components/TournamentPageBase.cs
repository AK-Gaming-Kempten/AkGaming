using Microsoft.AspNetCore.Components;
using AkGaming.Tournaments.Frontend.Components.Data;

namespace AkGaming.Tournaments.Frontend.Components.Pages;

public abstract class TournamentPageBase : ComponentBase
{
    [Inject] protected MockTournamentCatalog Catalog { get; set; } = default!;

    [Parameter] public string TournamentSlug { get; set; } = string.Empty;

    protected TournamentDetail Tournament { get; private set; } = default!;
    protected bool RequestedTournamentWasMissing { get; private set; }

    protected override void OnParametersSet()
    {
        Tournament = Catalog.Find(TournamentSlug) ?? Catalog.GetFeatured();
        RequestedTournamentWasMissing = Catalog.Find(TournamentSlug) is null;
    }
}
