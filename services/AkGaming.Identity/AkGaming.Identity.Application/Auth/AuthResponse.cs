namespace AkGaming.Identity.Application.Auth;

public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);
