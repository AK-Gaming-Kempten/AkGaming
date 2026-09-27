using System.Text.Json;
using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class RegistrationDetails
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Parameter, EditorRequired] public RegistrationView Registration { get; set; } = default!;
    [Parameter] public HashSet<string> Granted { get; set; } = [];
    [Parameter] public EventCallback Changed { get; set; }
    private SignupForm? _edit;
    private string? _action, _error;
    private string _title = "";
    private bool _busy;
    private bool Has(string permission) => Granted.Contains(permission);
    private static string YesNo(bool? value) => value is null ? "—" : value.Value ? "Ja" : "Nein";
    private string FoodSummary()
    {
        var form = Registration.Form;
        if (form.WantsToOrder is null)
            return $"{form.Meal ?? "—"} · {form.MealQuantity?.ToString() ?? "keine Mengenangabe"}; Eis: {form.IceCream ?? "—"} · {form.Scoops?.ToString() ?? "keine Mengenangabe"} Kugeln";
        if (form.WantsToOrder == false) return "Möchte nichts bestellen";

        return $"Döner: {form.DonerQuantity ?? 0} · Pizza: {form.PizzaQuantity ?? 0} · Eis: {form.IceCreamQuantity ?? 0} Kugeln ({form.IceCream ?? "keine Auswahl"})";
    }
    private void Edit() { _error = null; _edit = JsonSerializer.Deserialize<SignupForm>(JsonSerializer.Serialize(Registration.Form)); }
    private void Confirm(string action, string title) { _error = null; _action = action; _title = title; }
    private void Close() { if (!_busy) { _edit = null; _action = null; _error = null; } }
    private Task Save() => Run(HttpMethod.Put, $"api/registrations/{Registration.Id}", new UpdateSignup(Registration.Version, _edit!));
    private Task Apply() => Run(HttpMethod.Post, $"api/registrations/{Registration.Id}/actions", new DeskAction(Registration.Version, _action!));
    private async Task Run(HttpMethod method, string path, object body)
    {
        if (_busy) return;
        _busy = true; _error = null;
        try { await Client.SendAsync<RegistrationView>(method, path, body); _action = null; _edit = null; await Changed.InvokeAsync(); }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}
