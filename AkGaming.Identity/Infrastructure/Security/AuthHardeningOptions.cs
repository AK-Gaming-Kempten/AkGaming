using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Infrastructure.Security;

public sealed class AuthHardeningOptions : IAuthHardeningSettings
{
    public const string SectionName = "AuthHardening";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public bool RequireVerifiedEmailForLogin { get; set; }
    public int EmailVerificationTokenHours { get; set; } = 24;
    public bool ExposeEmailVerificationToken { get; set; }
}
