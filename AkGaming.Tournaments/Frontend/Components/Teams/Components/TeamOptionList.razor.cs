using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Teams.Components;

public partial class TeamOptionList : ComponentBase
{
    [Parameter] public IReadOnlyList<TeamDto> Teams { get; set; } = [];
    [Parameter] public IReadOnlyList<GameDto> Games { get; set; } = [];
    [Parameter] public Guid? SelectedTeamId { get; set; }
    [Parameter] public string? CurrentUserId { get; set; }
    [Parameter] public string EmptyState { get; set; } = "You are not part of any teams yet.";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<TeamDto> OnSelected { get; set; }

    private string GetGameName(string gameId)
        => Games.FirstOrDefault(game => string.Equals(game.Id, gameId, StringComparison.OrdinalIgnoreCase))?.Name ?? gameId;

    private string GetUserRoleLabel(TeamDto team)
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return "Member";

        var role = team.Memberships.FirstOrDefault(member =>
            string.Equals(member.UserId, CurrentUserId, StringComparison.OrdinalIgnoreCase))?.Role;

        return role?.ToString() ?? "Member";
    }
}
