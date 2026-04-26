using AkGaming.Tournaments.Frontend.Components.Data;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class StatusList : ComponentBase
{
    [Parameter] public IReadOnlyList<StatusListItem> Items { get; set; } = [];
    [Parameter] public string EmptyState { get; set; } = "Nothing here yet.";
}
