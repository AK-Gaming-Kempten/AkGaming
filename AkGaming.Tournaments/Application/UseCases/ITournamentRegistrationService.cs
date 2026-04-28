using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface ITournamentRegistrationService
{
    Task<IReadOnlyList<TournamentRegistrationDto>> GetTeamRegistrationsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TournamentRegistrationDto>> GetTournamentRegistrationsAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDto?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationEligibilityDto> GetRegistrationEligibilityAsync(Guid teamId, Guid tournamentId, string actingUserId, IReadOnlyCollection<Guid> playerProfileIds, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDto> SubmitRegistrationAsync(Guid teamId, Guid tournamentId, string actingUserId, IReadOnlyCollection<Guid> playerProfileIds, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDto> ReviewRegistrationAsync(Guid registrationId, bool approve, string? reviewNote, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDto> SubmitRosterChangeAsync(Guid registrationId, string actingUserId, IReadOnlyCollection<Guid> playerProfileIds, CancellationToken cancellationToken = default);
    Task<TournamentRegistrationDto> ReviewRosterAsync(Guid registrationId, Guid rosterId, bool approve, string? reviewNote, CancellationToken cancellationToken = default);
}
