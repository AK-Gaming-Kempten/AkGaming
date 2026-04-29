using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class TournamentRegistrationRulesEditor : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentRegistrationRuleDto> Rules { get; set; } = [];
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<TournamentRegistrationRuleUpdateRequest>> SaveRequested { get; set; }

    private static readonly IReadOnlyList<RuleOption> ruleOptions =
    [
        new("MinPlayersPerTeam", "Minimum players", "Minimum number of rostered players required."),
        new("MaxPlayersPerTeam", "Maximum players", "Maximum number of rostered players allowed."),
        new("MaxPlayerRankRating", "Maximum player MMR", "Highest allowed MMR for an individual player."),
        new("MaxTeamAverageRankRating", "Maximum team average MMR", "Highest allowed average MMR across the roster.")
    ];

    private List<EditableRule> rules = [];
    private string currentStateKey = string.Empty;

    protected override void OnParametersSet()
    {
        var nextStateKey = string.Join(";", Rules.Select(rule => $"{rule.Type}:{rule.Value}"));
        if (string.Equals(currentStateKey, nextStateKey, StringComparison.Ordinal))
            return;

        currentStateKey = nextStateKey;
        rules = Rules
            .Select(rule => new EditableRule(Guid.NewGuid(), rule.Type, rule.Value))
            .ToList();
    }

    private void AddRule()
    {
        rules.Add(new EditableRule(Guid.NewGuid(), ruleOptions[0].Type, 0));
    }

    private void RemoveRule(Guid ruleId)
    {
        rules.RemoveAll(rule => rule.Id == ruleId);
    }

    private void HandleRuleTypeChanged(Guid ruleId, ChangeEventArgs args)
    {
        var rule = rules.FirstOrDefault(candidate => candidate.Id == ruleId);
        if (rule is not null)
            rule.Type = args.Value?.ToString() ?? rule.Type;
    }

    private void HandleRuleValueChanged(Guid ruleId, ChangeEventArgs args)
    {
        var rule = rules.FirstOrDefault(candidate => candidate.Id == ruleId);
        if (rule is not null && int.TryParse(args.Value?.ToString(), out var parsed))
            rule.Value = parsed;
    }

    private Task SaveAsync()
        => SaveRequested.InvokeAsync(
            rules.Select(rule => new TournamentRegistrationRuleUpdateRequest(rule.Type, rule.Value)).ToList());

    private static string GetRuleDescription(string type)
        => ruleOptions.FirstOrDefault(option => string.Equals(option.Type, type, StringComparison.Ordinal))?.Description
           ?? "Registration rule";

    private sealed class EditableRule(Guid id, string type, int value)
    {
        public Guid Id { get; } = id;
        public string Type { get; set; } = type;
        public int Value { get; set; } = value;
    }

    private sealed record RuleOption(string Type, string Label, string Description);
}
