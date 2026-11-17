using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringAccountPanel
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Parameter, EditorRequired] public CateringView Account { get; set; } = default!;
    [Parameter, EditorRequired] public CateringMenu Menu { get; set; } = default!;
    [Parameter] public RegistrationView? Registration { get; set; }
    [Parameter] public bool CanOrder { get; set; } = true;
    [Parameter] public bool Staff { get; set; }
    [Parameter] public bool ShowPerson { get; set; }
    [Parameter] public EventCallback Changed { get; set; }
    private bool _ordering, _busy;
    private string? _action, _error;
    private string _reason = "", _eligibility = "auto";
    private Guid? _lineId;
    private int _quantity = 1;
    private int Remaining => Account.Lines.Where(l => l.Id == _lineId).Select(l => l.Quantity - l.CollectedQuantity).FirstOrDefault();
    private int PaymentCents => Account.OutstandingCents + (_action == "settle" && Registration is { Paid: false } ? Registration.PriceCents ?? 0 : 0);
    private string ActionTitle => _action switch { "eligibility" => "Rabattberechtigung", "pay" => "Catering bezahlen", "settle" => "Gesamtzahlung", "cancel" => "Position stornieren", "partial" => "Teilmenge abholen", _ => "Abholung korrigieren" };
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    private void Open(string action)
    {
        _action = action; _error = null; _reason = "";
        _eligibility = Account.EligibilityOverride switch { true => "yes", false => "no", _ => "auto" };
    }
    private void CloseOrder() { if (!_busy) _ordering = false; }
    private void CloseAction() { if (!_busy) { _action = null; _error = null; } }
    private async Task Ordered() { _ordering = false; await Changed.InvokeAsync(); }
    private async Task LineAction(CateringLineCommand command)
    {
        if (_busy) return;
        _lineId = command.LineId; _quantity = command.Quantity ?? 1;
        Open(command.Action);
        if (command.Action == "collect") await Apply();
    }
    private async Task Apply()
    {
        if (_busy) return;
        _busy = true; _error = null;
        try
        {
            if (_action == "settle" && Registration is not null)
                await Client.SendAsync<object>(HttpMethod.Post, $"api/catering/orders/{Account.RegistrationId}/settle", new SettleVisitor(Menu.EventId, Registration.Version, Account.Version, Registration.PriceCents));
            else
                await Client.SendAsync<CateringView>(HttpMethod.Post, $"api/catering/orders/{Account.RegistrationId}/actions",
                    new CateringAction(Menu.EventId, Account.Version, _action == "partial" ? "collect" : _action!, _lineId, _quantity,
                        _eligibility == "auto" ? null : _eligibility == "yes", _reason));
            _action = null;
            await Changed.InvokeAsync();
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}
