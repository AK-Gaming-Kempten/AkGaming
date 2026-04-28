using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public abstract class TournamentPageBase : ComponentBase
{
    [Inject] protected TournamentsApiClient TournamentsClient { get; set; } = default!;
    [Inject] protected TeamsApiClient TeamsClient { get; set; } = default!;
    [Inject] protected TournamentRegistrationsApiClient RegistrationsClient { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    [Parameter] public string TournamentSlug { get; set; } = string.Empty;

    protected TournamentDto? Tournament { get; set; }
    protected bool RequestedTournamentWasMissing { get; private set; }
    protected bool IsRegistrationDialogOpen { get; private set; }
    protected IReadOnlyList<TeamDto> RegisteredTeams { get; private set; } = [];
    protected IReadOnlyDictionary<Guid, TournamentRegistrationDto> RegistrationByTeamId { get; private set; } = new Dictionary<Guid, TournamentRegistrationDto>();
    protected IReadOnlyList<TournamentTimelineItem> TimelineItems { get; private set; } = [];
    protected string? PublicPageErrorMessage { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        PublicPageErrorMessage = null;
        RequestedTournamentWasMissing = false;
        RegisteredTeams = [];
        RegistrationByTeamId = new Dictionary<Guid, TournamentRegistrationDto>();
        TimelineItems = [];

        try
        {
            Tournament = await TournamentsClient.GetTournamentAsync(TournamentSlug);
            RequestedTournamentWasMissing = Tournament is null;
            if (Tournament is null)
            {
                return;
            }

            TimelineItems = BuildTimelineItems(Tournament);
            await LoadRegisteredTeamsAsync();
        }
        catch (TournamentApiException ex)
        {
            Tournament = null;
            PublicPageErrorMessage = ex.Message;
        }
    }

    private async Task LoadRegisteredTeamsAsync()
    {
        if (Tournament is null)
            return;

        var registrations = await RegistrationsClient.GetTournamentRegistrationsAsync(Tournament.Id);
        RegistrationByTeamId = registrations
            .GroupBy(registration => registration.TeamId)
            .ToDictionary(group => group.Key, group => group.First());

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

    protected async Task HandleRegistrationSubmittedAsync(TournamentRegistrationDto registration)
    {
        IsRegistrationDialogOpen = false;
        await LoadRegisteredTeamsAsync();

        if (Tournament is null)
            return;

        var targetPath = $"/tournaments/{Tournament.Slug}/teams";
        var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
        if (!string.Equals(currentPath, targetPath.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
        {
            Navigation.NavigateTo(targetPath);
        }
    }

    private static IReadOnlyList<TournamentTimelineItem> BuildTimelineItems(TournamentDto tournament)
    {
        var items = new List<TournamentTimelineItem>();
        AddTimelineItem(items, "Registration opens", tournament.RegistrationOpenUtc);
        AddTimelineItem(items, "Registration closes", tournament.RegistrationClosedUtc);
        AddTimelineItem(items, "Tournament starts", tournament.StartUtc);
        AddTimelineItem(items, "Tournament ends", tournament.EndUtc);
        return items;
    }

    private static void AddTimelineItem(ICollection<TournamentTimelineItem> items, string label, DateTimeOffset? value)
    {
        items.Add(new TournamentTimelineItem(label, value?.ToLocalTime().ToString("dd MMM yyyy, HH:mm") ?? "TBA"));
    }
}
