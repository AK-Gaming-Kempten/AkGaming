using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class RegistrationStatIcon
{
    [Parameter, EditorRequired] public string Kind { get; set; } = string.Empty;
}
