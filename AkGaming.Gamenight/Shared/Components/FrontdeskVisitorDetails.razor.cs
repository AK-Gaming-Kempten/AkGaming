using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class FrontdeskVisitorDetails
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Parameter, EditorRequired] public RegistrationView Registration { get; set; } = default!;
    [Parameter] public HashSet<string> Granted { get; set; } = [];
    [Parameter] public CateringMenu? Menu { get; set; }
    [Parameter] public CateringView? Catering { get; set; }
    [Parameter] public EventCallback Changed { get; set; }
    private bool _details, _busy;
    private string? _error;
    private static string Money(int? cents) => cents is null ? "Preisprüfung ausstehend" : (cents.Value / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    private async Task CheckIn()
    {
        if (_busy) return;
        _busy = true; _error = null;
        try
        {
            await Client.SendAsync<RegistrationView>(HttpMethod.Post, $"api/registrations/{Registration.Id}/actions", new DeskAction(Registration.Version, "checkin"));
            await Changed.InvokeAsync();
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}
