using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class AdminControlRoom
{
    private bool isBusy;
    private string? errorMessage;

    private async Task HandleSaveRequestedAsync(TournamentContentSaveRequest request)
    {
        if (Tournament is null)
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            Tournament = await TournamentsClient.UpdateTournamentContentAsync(
                Tournament.Id,
                request.Name,
                request.Status,
                request.RegistrationOpenUtc,
                request.RegistrationClosedUtc,
                request.StartUtc,
                request.EndUtc,
                request.InfoSections);
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

    private async Task HandleRegistrationRulesSaveRequestedAsync(IReadOnlyList<TournamentRegistrationRuleUpdateRequest> rules)
    {
        if (Tournament is null)
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            var updatedRules = await TournamentsClient.ReplaceTournamentRegistrationRulesAsync(Tournament.Id, rules);
            Tournament = Tournament with { RegistrationRules = updatedRules };
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

    private async Task HandleLogoUploadedAsync(MediaAssetDto asset)
    {
        await UpdateLogoAsync(asset.Id);
    }

    private async Task HandleClearLogoRequestedAsync()
    {
        await UpdateLogoAsync(null);
    }

    private async Task UpdateLogoAsync(Guid? logoAssetId)
    {
        if (Tournament is null)
            return;

        isBusy = true;
        errorMessage = null;

        try
        {
            await TournamentsClient.UpdateTournamentLogoAsync(Tournament.Id, logoAssetId);
            Tournament = await TournamentsClient.GetTournamentAsync(Tournament.Slug);
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
}
