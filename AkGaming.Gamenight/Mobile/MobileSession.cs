using System.Security.Claims;
using System.Text.Json;
using AkGaming.Gamenight.Shared;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
namespace AkGaming.Gamenight.Mobile;

public sealed class MobileSession : IUserSession
{
    private const string StorageKey = "gamenight-session";
    private readonly SemaphoreSlim _gate = new(1);
    private readonly OidcClient _oidc = new(new OidcClientOptions
    {
        Authority = "https://identity.akgaming.de",
        ClientId = "akgaming-gamenight-mobile",
        Scope = "openid profile email roles offline_access gamenight_api",
        RedirectUri = "de.akgaming.gamenight://callback",
        Browser = new SystemBrowser(),
        LoadProfile = false
    });
    private SessionData? _session;
    private bool _loaded;
    public event Action? Changed;
    private async Task Load()
    {
        if (_loaded) return;
        var json = await SecureStorage.Default.GetAsync(StorageKey);
        _session = json is null ? null : JsonSerializer.Deserialize<SessionData>(json);
        _loaded = true;
    }
    public async Task<ClaimsPrincipal> UserAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await Load();
            return _session is null ? new ClaimsPrincipal(new ClaimsIdentity()) :
                new ClaimsPrincipal(new ClaimsIdentity(_session.Claims.Select(c => new Claim(c.Type, c.Value)), "oidc", "email", "role"));
        }
        finally { _gate.Release(); }
    }
    public async Task<string?> TokenAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await Load();
            if (_session is null) return null;
            if (_session.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1))
            {
                var result = await _oidc.RefreshTokenAsync(_session.RefreshToken);
                if (result.IsError) throw new HttpRequestException("Bitte erneut anmelden.");
                _session = _session with { AccessToken = result.AccessToken, RefreshToken = result.RefreshToken ?? _session.RefreshToken, ExpiresAt = result.AccessTokenExpiration };
                await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(_session));
            }
            return _session.AccessToken;
        }
        finally { _gate.Release(); }
    }
    public async Task LoginAsync()
    {
        var result = await _oidc.LoginAsync();
        if (result.IsError) return;
        await _gate.WaitAsync();
        try
        {
            _session = new(result.AccessToken, result.RefreshToken, result.AccessTokenExpiration,
                result.User.Claims.Select(c => new StoredClaim(c.Type, c.Value)).ToList());
            _loaded = true;
            await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(_session));
        }
        finally { _gate.Release(); }
        Changed?.Invoke();
    }
    public async Task LogoutAsync()
    {
        await _gate.WaitAsync();
        try { SecureStorage.Default.Remove(StorageKey); _session = null; _loaded = true; }
        finally { _gate.Release(); }
        Changed?.Invoke();
    }
    public sealed record StoredClaim(string Type, string Value);
    public sealed record SessionData(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, List<StoredClaim> Claims);
}
internal sealed class SystemBrowser : Duende.IdentityModel.OidcClient.Browser.IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(new Uri(options.StartUrl), new Uri(options.EndUrl));
            return new BrowserResult
            {
                ResultType = BrowserResultType.Success,
                Response = options.EndUrl + "?" + string.Join("&", result.Properties.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)))
            };
        }
        catch (TaskCanceledException) { return new BrowserResult { ResultType = BrowserResultType.UserCancel }; }
    }
}
