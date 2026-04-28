using Microsoft.AspNetCore.Components;
using AkGaming.Tournaments.Frontend.Components.Data;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public abstract class TournamentPageBase : ComponentBase
{
    [Inject] protected MockTournamentCatalog Catalog { get; set; } = default!;
    [Inject] protected TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] protected TournamentRegistrationsApiClient RegistrationsClient { get; set; } = default!;

    [Parameter] public string TournamentSlug { get; set; } = string.Empty;

    protected TournamentDetail Tournament { get; private set; } = default!;
    protected bool RequestedTournamentWasMissing { get; private set; }
    protected bool IsRegistrationDialogOpen { get; private set; }
    protected IReadOnlyList<TeamDto> RegisteredTeams { get; private set; } = [];
    protected string? PublicPageErrorMessage { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        Tournament = Catalog.Find(TournamentSlug) ?? Catalog.GetFeatured();
        RequestedTournamentWasMissing = Catalog.Find(TournamentSlug) is null;
        PublicPageErrorMessage = null;
        RegisteredTeams = [];

        try
        {
            var registrations = await RegistrationsClient.GetTournamentRegistrationsAsync(Tournament.Summary.Id);
            var distinctTeamIds = registrations
                .Select(registration => registration.TeamId)
                .Distinct()
                .ToArray();

            var teams = new List<TeamDto>();
            foreach (var teamId in distinctTeamIds)
            {
                var team = await TeamsClient.GetTeamAsync(teamId);
                if (team is not null)
                {
                    teams.Add(team);
                }
            }

            RegisteredTeams = teams
                .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (TournamentApiException ex)
        {
            PublicPageErrorMessage = ex.Message;
        }
    }

    protected Task ShowRegistrationDialogAsync()
    {
        IsRegistrationDialogOpen = true;
        return Task.CompletedTask;
    }

    protected Task SetRegistrationDialogOpenAsync(bool value)
    {
        IsRegistrationDialogOpen = value;
        return Task.CompletedTask;
    }
}
