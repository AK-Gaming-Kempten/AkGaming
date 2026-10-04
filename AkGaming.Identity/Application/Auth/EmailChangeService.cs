using System.Net.Mail;
using AkGaming.Core.Common.Email;
using AkGaming.Core.Constants;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Application.ExternalAuth;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AkGaming.Identity.Application.Auth;

public sealed class EmailChangeService(
    IIdentityRepository repository,
    IEmailChangeStore store,
    IAuthService auth,
    IRefreshTokenService tokenService,
    IEmailSender emailSender,
    IAppUrlSettings urls,
    IDiscordOAuthService discord,
    IDiscordStateService discordState,
    ILogger<EmailChangeService> logger) : IEmailChangeService
{
    public async Task<EmailChangeResponse?> GetPendingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var request = await store.GetPendingAsync(userId, cancellationToken);
        return request is null ? null : new EmailChangeResponse(request.NewEmail, request.ExpiresAtUtc);
    }

    public async Task RequestAsync(Guid userId, string newEmail, string password, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        // Reuse login hardening, including failed-attempt counting and account lockout.
        await auth.LoginInteractiveAsync(new LoginRequest(user.Email, password), ipAddress, cancellationToken);
        await IssueAsync(user, newEmail, ipAddress, cancellationToken);
    }

    public async Task<string> StartDiscordAsync(Guid userId, string newEmail, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var email = NormalizeEmail(newEmail);
        EnsureDifferentEmail(user, email);
        var profile = await auth.GetCurrentUserAsync(userId, cancellationToken);
        if (profile.Discord is null)
            throw new AuthException(400, "Link a Discord account before using Discord to confirm your identity.");

        var state = discordState.CreateState(new DiscordOAuthState("email_change", userId,
            DateTime.UtcNow.AddMinutes(10), Guid.NewGuid().ToString("N"), State: email));
        try
        {
            // Explicit consent prevents an existing Discord session from silently approving the request.
            return discord.BuildAuthorizationUrl(state) + "&prompt=consent";
        }
        catch (InvalidOperationException)
        {
            throw new AuthException(503, "Discord authentication is unavailable. Try again later.");
        }
    }

    public async Task RequestWithDiscordAsync(Guid userId, string code, string state, string? ipAddress, CancellationToken cancellationToken)
    {
        var payload = discordState.ReadState(state);
        if (payload is null || payload.Purpose != "email_change" || payload.UserId != userId
            || payload.ExpiresAtUtc <= DateTime.UtcNow || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(payload.State))
            throw new AuthException(400, "Discord confirmation is invalid or expired.");

        DiscordIdentity identity;
        try
        {
            identity = await discord.GetIdentityFromAuthorizationCodeAsync(code, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new AuthException(401, "Discord authentication failed. Try again.");
        }
        var linked = await repository.GetExternalLoginAsync("discord", identity.UserId, cancellationToken);
        var user = await GetUserAsync(userId, cancellationToken);
        if (linked?.UserId != userId || user.LockoutEndUtc > DateTime.UtcNow)
        {
            await AuditFailureAsync(user, "discord_identity_mismatch_or_locked", ipAddress, cancellationToken);
            throw new AuthException(403, "Authenticate with the Discord account already linked to this account.");
        }
        await IssueAsync(user, payload.State, ipAddress, cancellationToken);
    }

    public Task CancelAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        return store.CancelAsync(userId, ipAddress, cancellationToken);
    }

    public async Task ConfirmAsync(string token, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
            throw new AuthException(400, "Email change link is invalid or expired.");
        var request = await store.ConfirmAsync(tokenService.HashToken(token), ipAddress, cancellationToken);
        var text = $"Your AK Gaming Identity email address changed from {request.OriginalEmail} to {request.NewEmail}. " +
            $"If you did not make this change, contact {ClubConstants.EmailAddresses.Identity}.";
        // The security change is committed. Delivery failure must not report it as an unsuccessful change.
        try
        {
            var message = EmailChangeEmailComposer.ComposeNotification(request.OriginalEmail, request.NewEmail, text);
            await emailSender.SendAsync(request.OriginalEmail, message.Subject, message.TextBody, message.HtmlBody, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Email change notification failed for user {UserId}.", request.UserId);
            await AuditFailureAsync(new User { Id = request.UserId, Email = request.OriginalEmail },
                "notification_delivery_failed", ipAddress, cancellationToken);
        }
    }

    private async Task IssueAsync(User user, string newEmail, string? ipAddress, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(newEmail);
        EnsureDifferentEmail(user, normalized);
        var rawToken = tokenService.GenerateToken();
        var request = new EmailChangeRequest
        {
            UserId = user.Id, OriginalEmail = user.Email, OriginalSecurityVersion = user.SecurityVersion,
            NewEmail = normalized, TokenHash = tokenService.HashToken(rawToken), ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };
        await store.IssueAsync(request, ipAddress, cancellationToken);
        var link = $"{urls.PublicBaseUrl.TrimEnd('/')}/account/email-change?token={Uri.EscapeDataString(rawToken)}";
        var message = EmailChangeEmailComposer.ComposeConfirmation(normalized, link);
        try
        {
            await emailSender.SendAsync(normalized, message.Subject, message.TextBody, message.HtmlBody, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Email change confirmation delivery failed for user {UserId}.", user.Id);
            await AuditFailureAsync(user, "confirmation_delivery_failed", ipAddress, cancellationToken);
            throw new AuthException(503, "The confirmation email could not be sent. Your current email is unchanged. Try resending in one minute.");
        }
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await repository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new AuthException(401, "User account was not found.");
    }

    private async Task AuditFailureAsync(User user, string reason, string? ipAddress, CancellationToken cancellationToken)
    {
        await repository.AddAuditLogAsync(new AuditLog
        {
            EventType = "email_change.failed", UserId = user.Id, SubjectEmail = user.Email,
            Success = false, IpAddress = ipAddress, Details = reason
        }, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length > 320 || !MailAddress.TryCreate(normalized, out var address)
            || !string.Equals(address.Address, normalized, StringComparison.Ordinal))
            throw new AuthException(400, "Enter a valid email address.");
        return normalized;
    }

    private static void EnsureDifferentEmail(User user, string email)
    {
        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new AuthException(400, "Enter an email address different from your current address.");
    }
}
