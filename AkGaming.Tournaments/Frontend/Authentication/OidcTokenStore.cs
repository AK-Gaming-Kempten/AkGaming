namespace AkGaming.Tournaments.Frontend.Authentication;

public sealed class OidcTokenStore
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? ExpiresAt { get; private set; }
    public bool IsInitialized { get; private set; }
    public SemaphoreSlim RefreshLock => _refreshLock;

    public void Initialize(string? accessToken, string? refreshToken, string? expiresAt)
    {
        if (IsInitialized)
            return;

        if (string.IsNullOrWhiteSpace(accessToken) &&
            string.IsNullOrWhiteSpace(refreshToken) &&
            string.IsNullOrWhiteSpace(expiresAt))
        {
            return;
        }

        SetTokens(accessToken, refreshToken, expiresAt);
        IsInitialized = true;
    }

    public void SetTokens(string? accessToken, string? refreshToken, string? expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        ExpiresAt = null;
    }
}
