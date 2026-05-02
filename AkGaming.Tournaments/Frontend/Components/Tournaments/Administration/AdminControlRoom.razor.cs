using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class AdminControlRoom
{
    private bool isBusy;
    private string? errorMessage;
    private string? successMessage;

    private async Task HandleIdentitySaveRequestedAsync(TournamentIdentitySaveRequest request)
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
                Tournament.BannerAssetId,
                request.PrimaryColor,
                Tournament.RegistrationOpenUtc,
                Tournament.RegistrationClosedUtc,
                Tournament.StartUtc,
                Tournament.EndUtc,
                Tournament.InfoSections);
            successMessage = "Tournament identity saved.";
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

    private async Task HandleTimelineSaveRequestedAsync(TournamentTimelineSaveRequest request)
    {
        if (Tournament is null)
            return;

        isBusy = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            Tournament = await TournamentsClient.UpdateTournamentContentAsync(
                Tournament.Id,
                Tournament.Name,
                Tournament.Status,
                Tournament.BannerAssetId,
                Tournament.PrimaryColor,
                request.RegistrationOpenUtc,
                request.RegistrationClosedUtc,
                request.StartUtc,
                request.EndUtc,
                Tournament.InfoSections);
            successMessage = "Tournament timeline saved.";
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

    private async Task HandleInfoSectionsSaveRequestedAsync(IReadOnlyList<TournamentInfoSectionDto> infoSections)
    {
        if (Tournament is null)
            return;

        isBusy = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            Tournament = await TournamentsClient.UpdateTournamentContentAsync(
                Tournament.Id,
                Tournament.Name,
                Tournament.Status,
                Tournament.BannerAssetId,
                Tournament.PrimaryColor,
                Tournament.RegistrationOpenUtc,
                Tournament.RegistrationClosedUtc,
                Tournament.StartUtc,
                Tournament.EndUtc,
                infoSections);
            successMessage = "Tournament info sections saved.";
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
        successMessage = null;

        try
        {
            var updatedRules = await TournamentsClient.ReplaceTournamentRegistrationRulesAsync(Tournament.Id, rules);
            Tournament = Tournament with { RegistrationRules = updatedRules };
            successMessage = "Tournament registration rules saved.";
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

    private Task HandleBannerUploadedAsync(MediaAssetDto asset)
    {
        if (Tournament is null)
            return Task.CompletedTask;

        Tournament = Tournament with { BannerAssetId = asset.Id };
        return Task.CompletedTask;
    }

    private Task HandleClearBannerRequestedAsync()
    {
        if (Tournament is null)
            return Task.CompletedTask;

        Tournament = Tournament with { BannerAssetId = null };
        return Task.CompletedTask;
    }

    private async Task UpdateLogoAsync(Guid? logoAssetId)
    {
        if (Tournament is null)
            return;

        isBusy = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            await TournamentsClient.UpdateTournamentLogoAsync(Tournament.Id, logoAssetId);
            Tournament = await TournamentsClient.GetTournamentAsync(Tournament.Slug);
            successMessage = logoAssetId.HasValue ? "Tournament logo saved." : "Tournament logo cleared.";
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
