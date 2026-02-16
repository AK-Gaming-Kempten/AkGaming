namespace AkGaming.Identity.Application.Auth;

public sealed record EmailVerificationResponse(string Message, string? VerificationToken = null);
