namespace AkGaming.Identity.Application.ExternalAuth;

public sealed record DiscordOAuthState(
    string Purpose,
    Guid? UserId,
    DateTime ExpiresAtUtc,
    string Nonce,
    string? RedirectUri = null,
    string? State = null);
