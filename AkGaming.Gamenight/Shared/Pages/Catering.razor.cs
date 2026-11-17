using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Pages;
public partial class Catering
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Inject] private IUserSession Session { get; set; } = default!;
    private CateringMenu? _menu;
    private List<CateringView> _accounts = [];
    private bool _signedIn;
    private string? _error;
    protected override async Task OnInitializedAsync()
    {
        _signedIn = (await Session.UserAsync()).Identity?.IsAuthenticated == true;
        if (_signedIn) await Load();
    }
    private async Task Load()
    {
        _error = null;
        try
        {
            var active = await Client.SendAsync<ActiveEvent>(HttpMethod.Get, "api/events/active");
            if (active.Event is null) { _error = "Keine aktive Game Night."; return; }
            _accounts = await Client.SendAsync<List<CateringView>>(HttpMethod.Get, "api/catering/mine");
            _menu = await Client.SendAsync<CateringMenu>(HttpMethod.Get, $"api/catering/menu/{active.Event.Id}");
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
}
