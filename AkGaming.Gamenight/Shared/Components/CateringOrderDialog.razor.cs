using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringOrderDialog
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Parameter, EditorRequired] public CateringMenu Menu { get; set; } = default!;
    [Parameter, EditorRequired] public CateringView Account { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback Changed { get; set; }
    private readonly Dictionary<Guid, int> _quantities = [];
    private bool _acknowledged, _busy;
    private string? _error;
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    private int Quantity(Guid id) => _quantities.GetValueOrDefault(id);
    private void SetQuantity(Guid id, int quantity)
    {
        if (quantity > 0 && Quantity(id) == 0 && _quantities.Count(q => q.Value > 0) >= 100)
        { _error = "Maximal 100 verschiedene Gerichte pro Bestellung."; return; }
        var value = Math.Clamp(quantity, 0, 100);
        var gross = Account.Lines.Sum(l => (long)l.UnitPriceCents * l.Quantity)
            + Menu.Items.Sum(i => (long)i.PriceCents * (i.Id == id ? value : Quantity(i.Id)));
        if (gross > 100_000_000) { _error = "Maximaler Bestellwert überschritten."; return; }
        _quantities[id] = value; _error = null;
    }
    private void ChangeQuantity(CateringQuantity quantity) => SetQuantity(quantity.ItemId, quantity.Quantity);
    private int NewSubtotal => Menu.Items.Sum(i => i.PriceCents * Quantity(i.Id));
    private int PreviewDiscount => !Account.DiscountEligible || Account.Lines.Any(l => l.Paid && l.DiscountApplied) ? 0 :
        Account.Lines.Where(l => !l.Paid).Select(l => l.PossibleDiscountCents)
        .Concat(Menu.Items.Where(i => Quantity(i.Id) > 0 && i.DiscountEligible).Select(i => Math.Min(i.PriceCents, i.DiscountOverrideCents ?? Menu.DefaultDiscountCents))).DefaultIfEmpty(0).Max();
    private int PreviewTotal => Account.Lines.Where(l => !l.Paid).Sum(l => l.UnitPriceCents * l.Quantity)
        + Menu.Items.Sum(i => i.PriceCents * Quantity(i.Id)) - PreviewDiscount;
    private async Task Close() { if (!_busy) await OnClose.InvokeAsync(); }
    private async Task Submit()
    {
        if (_busy) return;
        _busy = true; _error = null;
        try
        {
            await Client.SendAsync<CateringView>(HttpMethod.Post, $"api/catering/orders/{Account.RegistrationId}",
                new PlaceCateringOrder(Menu.EventId, Menu.Version, Account.Version,
                    _quantities.Where(q => q.Value > 0).Select(q => new CateringQuantity(q.Key, q.Value)).ToList(), _acknowledged));
            await Changed.InvokeAsync();
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}
