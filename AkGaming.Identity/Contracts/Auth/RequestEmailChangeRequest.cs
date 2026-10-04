namespace AkGaming.Identity.Contracts.Auth;

public sealed record RequestEmailChangeRequest(string NewEmail, string CurrentPassword);
