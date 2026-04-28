using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class TournamentInfoFieldGrid : ComponentBase
{
    [Parameter] public IReadOnlyList<TournamentInfoSectionDto> Sections { get; set; } = [];
}
