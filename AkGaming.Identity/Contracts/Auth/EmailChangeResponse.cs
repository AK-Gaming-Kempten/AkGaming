namespace AkGaming.Identity.Contracts.Auth;

public sealed record EmailChangeResponse(string NewEmail, DateTime ExpiresAtUtc);
