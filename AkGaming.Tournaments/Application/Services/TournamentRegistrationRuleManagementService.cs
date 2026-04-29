using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Persistence;
using AkGaming.Tournaments.Application.RegistrationRules;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;

namespace AkGaming.Tournaments.Application.Services;

public sealed class TournamentRegistrationRuleManagementService(
    ITournamentRepository tournamentRepository,
    IGameRankSystemRegistry rankSystemRegistry,
    IUnitOfWork unitOfWork) : ITournamentRegistrationRuleManagementService
{
    public async Task<IReadOnlyList<TournamentRegistrationRuleDto>> ReplaceRegistrationRulesAsync(
        Guid tournamentId,
        IReadOnlyList<TournamentRegistrationRuleUpdateDto> rules,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
                         ?? throw new NotFoundException($"Tournament '{tournamentId}' was not found.");
        if (rules.Count == 0)
        {
            throw new ValidationException("At least one registration rule is required.");
        }

        var replacementRules = new List<TournamentRegistrationRule>();
        for (var index = 0; index < rules.Count; index++)
        {
            replacementRules.Add(CreateRule(rules[index], index, tournamentId));
        }

        await tournamentRepository.ReplaceRegistrationRulesAsync(tournament.Id, replacementRules, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        tournament.RegistrationRules = replacementRules;

        var rankSystem = rankSystemRegistry.GetRankSystem(tournament.GameId);
        return replacementRules
            .OrderBy(rule => rule.SortOrder)
            .Select(rule => ToDto(rule, rankSystem))
            .ToList();
    }

    private static TournamentRegistrationRule CreateRule(
        TournamentRegistrationRuleUpdateDto update,
        int sortOrder,
        Guid tournamentId)
    {
        if (update.Value < 0)
        {
            throw new ValidationException("Registration rule values must not be negative.");
        }

        TournamentRegistrationRule rule = update.Type.Trim() switch
        {
            "MinPlayersPerTeam" => new MinPlayersPerTeamRegistrationRule(),
            "MaxPlayersPerTeam" => new MaxPlayersPerTeamRegistrationRule(),
            "MaxPlayerRankRating" => new MaxPlayerRankRatingRegistrationRule(),
            "MaxTeamAverageRankRating" => new MaxTeamAverageRankRatingRegistrationRule(),
            _ => throw new ValidationException($"Registration rule type '{update.Type}' is not supported.")
        };
        rule.Id = Guid.NewGuid();
        rule.TournamentId = tournamentId;
        rule.SortOrder = sortOrder;
        rule.Value = update.Value;
        return rule;
    }

    private static TournamentRegistrationRuleDto ToDto(TournamentRegistrationRule rule, IGameRankSystem rankSystem)
        => rule switch
        {
            MinPlayersPerTeamRegistrationRule => new TournamentRegistrationRuleDto("MinPlayersPerTeam", "Minimum players", rule.Value, rule.Value.ToString()),
            MaxPlayersPerTeamRegistrationRule => new TournamentRegistrationRuleDto("MaxPlayersPerTeam", "Maximum players", rule.Value, rule.Value.ToString()),
            MaxPlayerRankRatingRegistrationRule => new TournamentRegistrationRuleDto("MaxPlayerRankRating", "Maximum player MMR", rule.Value, $"{rankSystem.DescribeRating(rule.Value).Label} ({rule.Value} MMR)"),
            MaxTeamAverageRankRatingRegistrationRule => new TournamentRegistrationRuleDto("MaxTeamAverageRankRating", "Maximum team average MMR", rule.Value, $"{rankSystem.DescribeRating(rule.Value).Label} ({rule.Value} MMR)"),
            _ => new TournamentRegistrationRuleDto("Unknown", "Unknown rule", rule.Value, rule.Value.ToString())
        };
}
