using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringMealCard
{
    [Parameter, EditorRequired] public CateringItem Item { get; set; } = default!;
    [Parameter] public int DefaultDiscountCents { get; set; }
    [Parameter] public int Quantity { get; set; }
    [Parameter] public EventCallback<int> QuantityChanged { get; set; }
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
}
