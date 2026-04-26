using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class ApiErrorPanel : ComponentBase
{
    [Parameter] public string? Message { get; set; }
}
