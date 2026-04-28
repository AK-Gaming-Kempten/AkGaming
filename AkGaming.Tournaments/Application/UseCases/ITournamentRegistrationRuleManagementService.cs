using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Application.UseCases;

public interface ITournamentRegistrationRuleManagementService
{
    Task<IReadOnlyList<TournamentRegistrationRuleDto>> ReplaceRegistrationRulesAsync(
        Guid tournamentId,
        IReadOnlyList<TournamentRegistrationRuleUpdateDto> rules,
        CancellationToken cancellationToken = default);
}

public sealed record TournamentRegistrationRuleUpdateDto(string Type, int Value);
