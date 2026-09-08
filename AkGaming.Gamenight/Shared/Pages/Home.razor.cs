using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Pages;
public partial class Home
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Inject] private IUserSession Session { get; set; } = default!;
    private ActiveEvent? _active;
    private SignupForm _form = new();
    private string? _error, _receipt;
    private bool _busy;
    protected override async Task OnInitializedAsync()
    {
        try { _active = await Client.SendAsync<ActiveEvent>(HttpMethod.Get, "api/events/active"); var user = await Session.UserAsync(); _form.Email = user.FindFirst("email")?.Value ?? ""; }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
    private async Task Submit()
    {
        if (_busy) return;
        _busy = true; _error = null;
        try { var receipt = await Client.SendAsync<Receipt>(HttpMethod.Post, "api/registrations", new SubmitSignup(_active!.Event!.Id, _form)); _receipt = receipt.Message; }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
    private Task Login() => Session.LoginAsync();
    private static string BerlinTime(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")).ToString("dd.MM.yyyy HH:mm");
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
}
