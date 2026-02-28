using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class AuthHardeningSettingsStub : IAuthHardeningSettings
{
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public bool RequireVerifiedEmailForLogin { get; set; }
    public int EmailVerificationTokenHours { get; set; } = 24;
    public bool ExposeEmailVerificationToken { get; set; } = true;
}
