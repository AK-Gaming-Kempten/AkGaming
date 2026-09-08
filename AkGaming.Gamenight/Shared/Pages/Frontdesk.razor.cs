using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace AkGaming.Gamenight.Shared.Pages;
public partial class Frontdesk
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Inject] private IUserSession Session { get; set; } = default!;
    [Inject] private IRegistrationExport Exporter { get; set; } = default!;
    private List<RegistrationView> _rows = [];
    private HashSet<string> _permissions = [];
    private bool _allowed;
    private Guid? _selected;
    private string _search = "", _filter = "";
    private string? _error;
    protected override async Task OnInitializedAsync()
    {
        _permissions = (await Session.UserAsync()).FindAll("permission").Select(c => c.Value).ToHashSet();
        _allowed = _permissions.Contains(Permissions.Read);
        if (_allowed) await Load(); else _error = "Zum Öffnen des Front Desk benötigst du die Berechtigung zum Lesen von Anmeldungen.";
    }
    private IEnumerable<RegistrationView> Filtered => _rows.Where(r =>
        (r.Form.FirstName + " " + r.Form.LastName + " " + r.Form.Email).Contains(_search, StringComparison.OrdinalIgnoreCase))
        .Where(r => _filter switch { "waiting" => !r.CheckedIn && !r.Cancelled, "checked" => r.CheckedIn, "unpaid" => !r.Paid && r.PriceCents != 0 && !r.Cancelled, "staff" => r.StaffApproved && !r.Cancelled, "cancelled" => r.Cancelled, _ => true })
        .OrderBy(r => r.Form.LastName).ThenBy(r => r.Form.FirstName);
    private int TotalMeals(string meal) => _rows.Where(r => !r.Cancelled && r.Form.Meal == meal).Sum(r => r.Form.MealQuantity ?? 0);
    private async Task Load()
    {
        _error = null;
        try { _rows = await Client.SendAsync<List<RegistrationView>>(HttpMethod.Get, "api/registrations"); if (!_rows.Any(r => r.Id == _selected)) _selected = null; }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
    private async Task Export()
    {
        try { var csv = await Client.SendAsync<string>(HttpMethod.Get, "api/registrations/export"); await Exporter.SaveAsync("gamenight-anmeldungen.csv", csv); }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
}
