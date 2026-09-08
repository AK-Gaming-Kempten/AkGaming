using System.Globalization;
using System.Text.Json;
using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Pages;
public partial class Admin
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Inject] private IUserSession Session { get; set; } = default!;
    private List<EventSettings> _events = [];
    private List<PaymentPeriodOption> _periods = [];
    private ActiveEvent? _active;
    private Guid? _selected;
    private EventSettings? _draft;
    private bool _allowed, _activate, _busy;
    private string? _error;
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    protected override async Task OnInitializedAsync()
    {
        _allowed = (await Session.UserAsync()).HasClaim("permission", Permissions.Events);
        if (_allowed) await Load(); else _error = "Keine Berechtigung zur Veranstaltungsverwaltung.";
    }
    private async Task Load()
    {
        try
        {
            _events = await Client.SendAsync<List<EventSettings>>(HttpMethod.Get, "api/events");
            _active = await Client.SendAsync<ActiveEvent>(HttpMethod.Get, "api/events/active");
            _selected ??= _active.Event?.Id;
            _periods = await Client.SendAsync<List<PaymentPeriodOption>>(HttpMethod.Get, "api/membership-periods");
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
    private void Create() { _draft = new(); _error = null; }
    private void Edit(EventSettings e) { _draft = JsonSerializer.Deserialize<EventSettings>(JsonSerializer.Serialize(e)); _error = null; }
    private void Close() { if (!_busy) { _draft = null; _activate = false; _error = null; } }
    private static string Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Berlin).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
    private void SetDate(string property, ChangeEventArgs args)
    {
        if (!DateTime.TryParse(args.Value?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) return;
        if (Berlin.IsInvalidTime(local)) { _error = "Diese Uhrzeit existiert wegen der Zeitumstellung nicht."; return; }
        typeof(EventSettings).GetProperty(property)!.SetValue(_draft, TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Berlin));
    }
    private async Task Save()
    {
        if (_busy) return;
        _busy = true; _error = null;
        try { var saved = await Client.SendAsync<EventSettings>(HttpMethod.Post, "api/events", _draft); _selected = saved.Id; _draft = null; await Load(); }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
    private async Task Activate()
    {
        if (_busy || _selected is null || _active is null) return;
        _busy = true; _error = null;
        try { await Client.SendAsync<object>(HttpMethod.Post, "api/events/active", new ActivateEvent(_selected.Value, _active.SelectionVersion)); _activate = false; await Load(); }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}

