using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class DeskPaymentSummary
{
    [Parameter, EditorRequired] public RegistrationView Registration { get; set; } = default!;
    [Parameter, EditorRequired] public CateringView Account { get; set; } = default!;
    [Parameter] public bool Staff { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback Pay { get; set; }
    private bool CanPay => Staff && !Account.Cancelled && Registration.PriceCents is not null && ((!Registration.Paid && Registration.PriceCents > 0) || Account.Lines.Any(l => !l.Paid));
    private static string Money(int? cents) => cents is null ? "—" : (cents.Value / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
}
