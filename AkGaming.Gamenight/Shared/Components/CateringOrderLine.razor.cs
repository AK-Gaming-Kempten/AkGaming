using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public sealed record CateringLineCommand(string Action, Guid LineId, int? Quantity = null);
public partial class CateringOrderLine
{
    [Parameter, EditorRequired] public CateringLine Line { get; set; } = default!;
    [Parameter] public bool Staff { get; set; }
    [Parameter] public bool OrdersOpen { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback<CateringLineCommand> Action { get; set; }
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
}
