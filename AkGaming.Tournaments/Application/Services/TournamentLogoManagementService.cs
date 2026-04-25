using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TournamentLogoManagementService(
    IMediaAssetRepository mediaAssetRepository,
    ITournamentRepository tournamentRepository,
    IUnitOfWork unitOfWork) : ITournamentLogoManagementService
{
    public async Task UpdateTournamentLogoAsync(Guid tournamentId, Guid? logoAssetId, CancellationToken cancellationToken = default)
    {
        if (logoAssetId is Guid assetId && await mediaAssetRepository.GetByIdAsync(assetId, cancellationToken) is null)
        {
            throw new NotFoundException($"Media asset '{assetId}' was not found.");
        }

        var tournament = await tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
                         ?? throw new NotFoundException($"Tournament '{tournamentId}' was not found.");

        tournament.LogoAssetId = logoAssetId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
