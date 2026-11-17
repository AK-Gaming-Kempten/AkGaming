using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class FrontdeskVisitorDetails
{
    [Parameter, EditorRequired] public RegistrationView Registration { get; set; } = default!;
    [Parameter] public HashSet<string> Granted { get; set; } = [];
    [Parameter] public CateringMenu? Menu { get; set; }
    [Parameter] public CateringView? Catering { get; set; }
    [Parameter] public EventCallback Changed { get; set; }
}
