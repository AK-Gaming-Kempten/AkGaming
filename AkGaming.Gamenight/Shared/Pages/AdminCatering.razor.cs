using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Pages;
public partial class AdminCatering
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Inject] private IUserSession Session { get; set; } = default!;
    private List<CateringView> _accounts = [];
    private CateringMenu? _menu;
    private bool _allowed, _staff, _busy;
    private string? _error;
    private Guid? _selected;
    private string _view = "orders", _search = "", _item = "", _payment = "", _collection = "", _sort = "person";
    private IEnumerable<KeyValuePair<Guid, string>> Meals => _accounts.SelectMany(a => a.Lines).GroupBy(l => l.ItemId).Select(g => new KeyValuePair<Guid, string>(g.Key, g.First().ItemName)).OrderBy(p => p.Value);
    private IEnumerable<(CateringView Account, CateringLine Line)> Filtered
    {
        get
        {
            var rows = _accounts.Where(a => (a.Person + " " + a.Email).Contains(_search, StringComparison.OrdinalIgnoreCase))
                .SelectMany(a => a.Lines.Select(l => (Account: a, Line: l)))
                .Where(r => _item == "" || r.Line.ItemId.ToString() == _item)
                .Where(r => _payment == "" || r.Line.Paid == (_payment == "paid"))
                .Where(r => _collection switch { "pending" => r.Line.CollectedQuantity < r.Line.Quantity, "partial" => r.Line.CollectedQuantity > 0 && r.Line.CollectedQuantity < r.Line.Quantity, "collected" => r.Line.CollectedQuantity == r.Line.Quantity, _ => true });
            return _sort switch
            {
                "person-desc" => rows.OrderByDescending(r => r.Account.Person),
                "meal" => rows.OrderBy(r => r.Line.ItemName).ThenBy(r => r.Account.Person),
                "meal-desc" => rows.OrderByDescending(r => r.Line.ItemName).ThenBy(r => r.Account.Person),
                "oldest" => rows.OrderBy(r => r.Line.OrderedAt),
                "newest" => rows.OrderByDescending(r => r.Line.OrderedAt),
                _ => rows.OrderBy(r => r.Account.Person).ThenBy(r => r.Line.OrderedAt)
            };
        }
    }
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    protected override async Task OnInitializedAsync()
    {
        var user = await Session.UserAsync();
        _allowed = user.HasClaim("permission", Permissions.Read);
        _staff = user.HasClaim("permission", Permissions.Frontdesk);
        if (_allowed) await Load(); else _error = "Keine Berechtigung zum Lesen von Bestellungen.";
    }
    private async Task Load()
    {
        _error = null;
        try
        {
            var active = await Client.SendAsync<ActiveEvent>(HttpMethod.Get, "api/events/active");
            if (active.Event is null) { _error = "Keine aktive Game Night."; return; }
            _accounts = await Client.SendAsync<List<CateringView>>(HttpMethod.Get, "api/catering/orders");
            _menu = await Client.SendAsync<CateringMenu>(HttpMethod.Get, $"api/catering/menu/{active.Event.Id}");
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
    private async Task Collect(CateringView account, CateringLine line)
    {
        if (_busy || _menu is null) return;
        _busy = true; _error = null;
        try
        {
            await Client.SendAsync<CateringView>(HttpMethod.Post, $"api/catering/orders/{account.RegistrationId}/actions", new CateringAction(_menu.EventId, account.Version, "collect", line.Id, line.Quantity - line.CollectedQuantity));
            await Load();
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}
