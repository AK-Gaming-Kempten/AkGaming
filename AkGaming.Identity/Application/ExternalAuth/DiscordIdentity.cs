namespace AkGaming.Identity.Application.ExternalAuth;

public sealed record DiscordIdentity(string UserId, string Username, string? Email, bool EmailVerified);
