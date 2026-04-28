using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Components;

public partial class MetricCard : ComponentBase
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public string Caption { get; set; } = string.Empty;
}
