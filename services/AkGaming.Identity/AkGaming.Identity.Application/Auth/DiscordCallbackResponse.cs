namespace AkGaming.Identity.Application.Auth;

public sealed record DiscordCallbackResponse(
    bool Linked,
    bool CreatedUser,
    string DiscordUserId,
    string? DiscordUsername,
    AuthResponse? Tokens,
    string Message);
