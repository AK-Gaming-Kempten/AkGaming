using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace AkGaming.Tournaments.Frontend.Components.Layout;

public partial class NavMenu : ComponentBase, IDisposable
{
    [Parameter] public EventCallback OnNavigate { get; set; }

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private TournamentsApiClient TournamentsClient { get; set; } = default!;

    private IReadOnlyList<TournamentSummaryDto> tournaments = [];
    private TournamentSummaryDto? selectedTournament;
    private string selectedTournamentSlug = string.Empty;
    private bool isAuthenticated;
    private bool isAdmin;
    private bool isPublicExpanded = true;
    private bool isPlayerExpanded = true;
    private bool isAdministrationExpanded = true;

    private bool HasSelectedTournament => selectedTournament is not null;
    private bool ShowTournamentContext => isAdmin || HasSelectedTournament;
    private bool CanChangeTournament => isAuthenticated;
    private string CurrentTournamentName => selectedTournament?.Name ?? "No tournament selected";
    private string? CurrentTournamentSubline => selectedTournament?.GameName;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        isAuthenticated = user.Identity?.IsAuthenticated ?? false;
        isAdmin = isAuthenticated && user.Claims.Any(claim =>
            claim.Type == "permission" &&
            (claim.Value == "tournaments.games.manage"
             || claim.Value == "tournaments.tournaments.manage"
             || claim.Value == "tournaments.registrations.manage"));

        try
        {
            tournaments = await TournamentsClient.GetTournamentsAsync();
        }
        catch (TournamentApiException)
        {
            tournaments = [];
        }

        UpdateSelectedTournamentFromLocation();

        Nav.LocationChanged += HandleLocationChanged;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateSelectedTournamentFromLocation();
        InvokeAsync(StateHasChanged);
    }

    private void UpdateSelectedTournamentFromLocation()
    {
        var path = Nav.ToBaseRelativePath(Nav.Uri);
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var requestedSlug = segments.Length >= 2 && string.Equals(segments[0], "tournaments", StringComparison.OrdinalIgnoreCase)
            ? segments[1]
            : null;

        selectedTournament = string.IsNullOrWhiteSpace(requestedSlug)
            ? null
            : tournaments.FirstOrDefault(tournament => string.Equals(tournament.Slug, requestedSlug, StringComparison.OrdinalIgnoreCase));
        selectedTournamentSlug = selectedTournament?.Slug ?? string.Empty;
    }

    private Task HandleTournamentChanged(ChangeEventArgs args)
    {
        var targetSlug = args.Value?.ToString();
        Nav.NavigateTo(BuildTournamentHrefForTournament(targetSlug));
        return Task.CompletedTask;
    }

    private string BuildTournamentHrefForTournament(string? targetSlug)
    {
        if (string.IsNullOrWhiteSpace(targetSlug))
            return "/discover";

        var path = Nav.ToBaseRelativePath(Nav.Uri);
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count >= 2 && string.Equals(segments[0], "tournaments", StringComparison.OrdinalIgnoreCase))
        {
            segments[1] = targetSlug;
            return "/" + string.Join('/', segments);
        }

        return $"/tournaments/{targetSlug}";
    }

    private void TogglePublicExpanded() => isPublicExpanded = !isPublicExpanded;
    private void TogglePlayerExpanded() => isPlayerExpanded = !isPlayerExpanded;
    private void ToggleAdministrationExpanded() => isAdministrationExpanded = !isAdministrationExpanded;

    private Task NotifyNavigation()
        => OnNavigate.HasDelegate ? OnNavigate.InvokeAsync() : Task.CompletedTask;

    public void Dispose()
    {
        Nav.LocationChanged -= HandleLocationChanged;
    }
}
