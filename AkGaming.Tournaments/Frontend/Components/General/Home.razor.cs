using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.General;

public partial class Home : ComponentBase
{
    [Inject] private TournamentsApiClient TournamentsClient { get; set; } = default!;

    private IReadOnlyList<TournamentSummaryDto> tournaments = [];
    private bool isLoading = true;
    private string? errorMessage;

    private TournamentSummaryDto? FeaturedTournament => tournaments.FirstOrDefault();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            tournaments = await TournamentsClient.GetTournamentsAsync();
        }
        catch (TournamentApiException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
