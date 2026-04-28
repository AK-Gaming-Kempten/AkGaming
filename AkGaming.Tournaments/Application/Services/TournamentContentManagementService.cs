using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TournamentContentManagementService(
    ITournamentRepository tournamentRepository,
    IUnitOfWork unitOfWork) : ITournamentContentManagementService
{
    public async Task<TournamentDto> UpdateTournamentContentAsync(
        Guid tournamentId,
        DateTimeOffset? registrationOpenUtc,
        DateTimeOffset? registrationClosedUtc,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc,
        IReadOnlyList<TournamentInfoSectionUpdateDto> infoSections,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
                         ?? throw new NotFoundException($"Tournament '{tournamentId}' was not found.");

        ValidateTimeline(registrationOpenUtc, registrationClosedUtc, startUtc, endUtc);

        tournament.RegistrationOpenUtc = registrationOpenUtc;
        tournament.RegistrationClosedUtc = registrationClosedUtc;
        tournament.StartUtc = startUtc;
        tournament.EndUtc = endUtc;

        tournament.InfoSections.Clear();
        for (var index = 0; index < infoSections.Count; index++)
        {
            var update = infoSections[index];
            if (string.IsNullOrWhiteSpace(update.Header) && string.IsNullOrWhiteSpace(update.ContentMarkdown))
                continue;

            if (string.IsNullOrWhiteSpace(update.Header))
                throw new ValidationException("Tournament info section header is required.");

            tournament.InfoSections.Add(new TournamentInfoSection
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                Header = update.Header.Trim(),
                ContentMarkdown = update.ContentMarkdown?.Trim() ?? string.Empty,
                SortOrder = index
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tournament.ToDto();
    }

    private static void ValidateTimeline(
        DateTimeOffset? registrationOpenUtc,
        DateTimeOffset? registrationClosedUtc,
        DateTimeOffset? startUtc,
        DateTimeOffset? endUtc)
    {
        if (registrationOpenUtc.HasValue && registrationClosedUtc.HasValue && registrationOpenUtc > registrationClosedUtc)
            throw new ValidationException("Registration open must be before registration close.");

        if (registrationClosedUtc.HasValue && startUtc.HasValue && registrationClosedUtc > startUtc)
            throw new ValidationException("Registration close must be before tournament start.");

        if (startUtc.HasValue && endUtc.HasValue && startUtc > endUtc)
            throw new ValidationException("Tournament start must be before tournament end.");
    }
}
