using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Pages;
public partial class MyRegistration
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Inject] private IUserSession Session { get; set; } = default!;
    [Parameter] public Guid? Id { get; set; }
    [SupplyParameterFromQuery(Name = "token")] public string? Token { get; set; }
    private List<RegistrationView> _rows = [];
    private string? _error;
    private bool _loading = true, _signedIn;
    protected override Task OnParametersSetAsync() => Load();
    private async Task Load()
    {
        _loading = true; _error = null;
        try
        {
            _signedIn = (await Session.UserAsync()).Identity?.IsAuthenticated == true;
            if (_signedIn) _rows = await Client.SendAsync<List<RegistrationView>>(HttpMethod.Post, "api/registrations/mine");
            else if (Id is not null && Token is not null) _rows = [await Client.SendAsync<RegistrationView>(HttpMethod.Post, $"api/registrations/{Id}/guest", Token)];
        }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _loading = false; }
    }
    private Task Login() => Session.LoginAsync();
}

