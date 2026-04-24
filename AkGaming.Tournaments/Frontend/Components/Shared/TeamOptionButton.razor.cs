using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class TeamOptionButton : ComponentBase
{
    [Parameter] public TeamDto Team { get; set; } = default!;
    [Parameter] public string GameName { get; set; } = string.Empty;
    [Parameter] public string RoleLabel { get; set; } = "Member";
    [Parameter] public bool Selected { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<TeamDto> OnSelected { get; set; }

    private Task SelectAsync()
        => OnSelected.InvokeAsync(Team);

    private string GetTeamInitials()
        => GetInitials(Team.Name);

    private string GetGameInitials()
        => GetInitials(GameName);

    private static string GetInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return "--";

        if (parts.Length == 1)
            return parts[0][..Math.Min(parts[0].Length, 2)].ToUpperInvariant();

        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }
}
