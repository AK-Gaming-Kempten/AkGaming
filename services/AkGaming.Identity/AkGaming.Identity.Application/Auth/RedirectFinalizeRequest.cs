namespace AkGaming.Identity.Application.Auth;

public sealed record RedirectFinalizeRequest(
    string RedirectUri,
    string? State,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);
