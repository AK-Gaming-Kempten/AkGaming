using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class AdminRegistrations
{
    private bool isBusy;
    private string? errorMessage;
    private IReadOnlyList<TournamentRegistrationDto> registrations = [];
    private IReadOnlyDictionary<Guid, string> teamNames = new Dictionary<Guid, string>();
    private IReadOnlyDictionary<Guid, Guid?> teamLogos = new Dictionary<Guid, Guid?>();

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await RefreshRegistrationStateAsync();
    }

    private async Task HandleReviewRequestedAsync(TournamentRegistrationReviewAction action)
    {
        isBusy = true;
        errorMessage = null;

        try
        {
            await RegistrationsClient.ReviewRegistrationAsync(action.RegistrationId, action.Approve, null);
            await RefreshRegistrationStateAsync();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task HandleDeleteRequestedAsync(Guid registrationId)
    {
        isBusy = true;
        errorMessage = null;

        try
        {
            await RegistrationsClient.DeleteRegistrationAsync(registrationId);
            await RefreshRegistrationStateAsync();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task HandleRosterReviewRequestedAsync(TournamentRosterReviewAction action)
    {
        isBusy = true;
        errorMessage = null;

        try
        {
            await RegistrationsClient.ReviewRosterChangeAsync(action.RegistrationId, action.RosterId, action.Approve, null);
            await RefreshRegistrationStateAsync();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    private async Task RefreshRegistrationStateAsync()
    {
        if (Tournament is null)
        {
            registrations = [];
            teamNames = new Dictionary<Guid, string>();
            teamLogos = new Dictionary<Guid, Guid?>();
            return;
        }

        await LoadRegisteredTeamsAsync();
        registrations = RegistrationByTeamId.Values
            .OrderBy(registration => registration.SubmittedAtUtc)
            .ToList();
        teamNames = RegisteredTeams.ToDictionary(team => team.Id, team => team.Name);
        teamLogos = RegisteredTeams.ToDictionary(team => team.Id, team => team.LogoAssetId);
    }
}
