namespace AkGaming.Identity.Application.Hardening;

public static class AuditEventTypes
{
    public const string RegisterSuccess = "register.success";
    public const string RegisterFailed = "register.failed";
    public const string LoginSuccess = "login.success";
    public const string LoginFailed = "login.failed";
    public const string LoginLocked = "login.locked";
    public const string LoginUnverified = "login.unverified";
    public const string RefreshSuccess = "refresh.success";
    public const string RefreshFailed = "refresh.failed";
    public const string RefreshReuseDetected = "refresh.reuse_detected";
    public const string RefreshExpired = "refresh.expired";
    public const string LogoutSuccess = "logout.success";
    public const string EmailVerificationRequest = "email_verification.request";
    public const string EmailVerificationIssued = "email_verification.issued";
    public const string EmailVerificationSuccess = "email_verification.success";
    public const string EmailVerificationFailed = "email_verification.failed";
    public const string DiscordLoginSuccess = "discord.login.success";
    public const string DiscordLoginFailed = "discord.login.failed";
    public const string DiscordLinkSuccess = "discord.link.success";
    public const string DiscordLinkFailed = "discord.link.failed";
}
