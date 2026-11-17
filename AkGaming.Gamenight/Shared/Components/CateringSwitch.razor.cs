using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringSwitch
{
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
}
