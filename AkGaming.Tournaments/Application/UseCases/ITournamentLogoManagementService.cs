namespace AkGaming.Tournaments.Application.UseCases;

public interface ITournamentLogoManagementService
{
    Task UpdateTournamentLogoAsync(Guid tournamentId, Guid? logoAssetId, CancellationToken cancellationToken = default);
}
