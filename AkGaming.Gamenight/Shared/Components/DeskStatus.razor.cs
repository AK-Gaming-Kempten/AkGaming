using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class DeskStatus
{
    [Parameter] public string Icon { get; set; } = "clock";
    [Parameter] public string Tone { get; set; } = "neutral";
    [Parameter] public string Text { get; set; } = "";
}
