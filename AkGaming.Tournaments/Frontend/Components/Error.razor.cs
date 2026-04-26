using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components;

public partial class Error : ComponentBase
{
    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
    }
}
