using System.Security.Claims;
using System.Text.Json;
using AkGaming.Core.Components.Authentication;
using AkGaming.Gamenight.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Gamenight.Frontend;
public sealed class WebSession(IHttpContextAccessor accessor, AuthenticationStateProvider state, NavigationManager navigation,
    IHttpClientFactory factory, IConfiguration config, AuthenticationTicketTokenUpdater updater) : IUserSession
{
    private readonly SemaphoreSlim _gate = new(1);
    private string? _access, _refresh;
    private DateTimeOffset _expires;
    private bool _initialized;
    public async Task<ClaimsPrincipal> UserAsync() => (await state.GetAuthenticationStateAsync()).User;
    public Task LoginAsync()
    {
        var returnPath = new Uri(navigation.Uri).AbsolutePath;
        navigation.NavigateTo($"/authentication/login?returnUrl={Uri.EscapeDataString(returnPath)}", forceLoad: true);
        return Task.CompletedTask;
    }
    public Task LogoutAsync() { navigation.NavigateTo("/logout", forceLoad: true); return Task.CompletedTask; }
    public async Task<string?> TokenAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!_initialized && accessor.HttpContext is { } context)
            {
                _access = await context.GetTokenAsync("access_token");
                _refresh = await context.GetTokenAsync("refresh_token");
                DateTimeOffset.TryParse(await context.GetTokenAsync("expires_at"), out _expires);
                _initialized = true;
            }
            if (_access is null) return null;
            if (_expires > DateTimeOffset.UtcNow.AddMinutes(1)) return _access;
            if (_refresh is null) throw new HttpRequestException("Bitte erneut anmelden.");
            using var response = await factory.CreateClient().PostAsync(config["Identity:Authority"]!.TrimEnd('/') + "/connect/token",
                new FormUrlEncodedContent(new Dictionary<string,string> { ["grant_type"] = "refresh_token", ["refresh_token"] = _refresh, ["client_id"] = "akgaming-gamenight-web", ["client_secret"] = config["Identity:ClientSecret"]! }));
            if (!response.IsSuccessStatusCode) throw new HttpRequestException("Deine Sitzung ist abgelaufen. Bitte erneut anmelden.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            _access = json.RootElement.GetProperty("access_token").GetString();
            if (json.RootElement.TryGetProperty("refresh_token", out var refresh)) _refresh = refresh.GetString();
            _expires = DateTimeOffset.UtcNow.AddSeconds(json.RootElement.GetProperty("expires_in").GetInt32());
            await updater.UpdateTokensAsync(CookieAuthenticationDefaults.AuthenticationScheme, _access!, _refresh!, _expires.ToString("O"), CancellationToken.None);
            return _access;
        }
        finally { _gate.Release(); }
    }
}
