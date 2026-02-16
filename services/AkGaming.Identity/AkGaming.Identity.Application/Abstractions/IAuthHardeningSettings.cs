namespace AkGaming.Identity.Application.Abstractions;

public interface IAuthHardeningSettings
{
    int MaxFailedLoginAttempts { get; }
    int LockoutMinutes { get; }
    bool RequireVerifiedEmailForLogin { get; }
    int EmailVerificationTokenHours { get; }
    bool ExposeEmailVerificationToken { get; }
}
