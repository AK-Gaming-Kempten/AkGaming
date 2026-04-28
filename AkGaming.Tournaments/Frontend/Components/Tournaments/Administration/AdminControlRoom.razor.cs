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
}
