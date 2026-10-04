using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenIddict.Abstractions;

namespace AkGaming.Identity.Infrastructure.Persistence;

public sealed class EmailChangeStore(
    AuthDbContext db,
    IOpenIddictTokenManager tokens,
    IOpenIddictAuthorizationManager authorizations) : IEmailChangeStore
{
    public Task<EmailChangeRequest?> GetPendingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return db.EmailChangeRequests.AsNoTracking().SingleOrDefaultAsync(
            x => x.UserId == userId && x.ConsumedAtUtc == null && x.CancelledAtUtc == null
                && x.ExpiresAtUtc > now && x.User.SecurityVersion == x.OriginalSecurityVersion
                && x.User.Email == x.OriginalEmail, cancellationToken);
    }

    public async Task IssueAsync(EmailChangeRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(request, cancellationToken);
        var previous = await db.EmailChangeRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);
        if (previous is not null && previous.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1))
        {
            throw new AuthException(429, "Wait one minute before requesting another confirmation email.");
        }

        if (await db.Users.AnyAsync(x => x.Email == request.NewEmail && x.Id != request.UserId, cancellationToken))
        {
            throw new AuthException(409, "This email address is unavailable.");
        }

        await db.EmailChangeRequests.Where(x => x.UserId == request.UserId).ExecuteDeleteAsync(cancellationToken);
        db.EmailChangeRequests.Add(request);
        AddAudit("email_change.issued", request, ipAddress);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // All mutations lock the user first, so cancellation and confirmation cannot interleave.
        await db.Users.Where(x => x.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Email, x => x.Email), cancellationToken);
        var request = await db.EmailChangeRequests.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (request is not null && request.ConsumedAtUtc is null && request.CancelledAtUtc is null)
        {
            await db.EmailChangeRequests.Where(x => x.Id == request.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CancelledAtUtc, DateTime.UtcNow), cancellationToken);
            AddAudit("email_change.cancelled", request, ipAddress);
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EmailChangeRequest> ConfirmAsync(string tokenHash, string? ipAddress, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var request = await db.EmailChangeRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        ValidateRequest(request);
        await LockUserAsync(request!, cancellationToken);
        // Read again after obtaining the lock: a resend or cancellation may have won the race.
        request = await db.EmailChangeRequests.AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        ValidateRequest(request);
        var now = DateTime.UtcNow;
        if (await db.Users.AnyAsync(x => x.Email == request!.NewEmail && x.Id != request.UserId, cancellationToken))
        {
            throw new AuthException(409, "This email address is unavailable. Request a different address.");
        }

        try
        {
            await db.Users.Where(x => x.Id == request!.UserId).ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Email, request!.NewEmail)
                .SetProperty(x => x.IsEmailVerified, true)
                .SetProperty(x => x.SecurityVersion, Guid.NewGuid()), cancellationToken);
        }
        catch (Exception exception) when (exception is PostgresException { SqlState: "23505" }
            or SqliteException { SqliteErrorCode: 19 })
        {
            throw new AuthException(409, "This email address is unavailable. Request a different address.");
        }

        await db.EmailChangeRequests.Where(x => x.Id == request!.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ConsumedAtUtc, now), cancellationToken);
        await db.EmailVerificationTokens.Where(x => x.UserId == request!.UserId && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ConsumedAtUtc, now), cancellationToken);
        await db.PasswordResetTokens.Where(x => x.UserId == request!.UserId && x.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ConsumedAtUtc, now), cancellationToken);
        await db.RefreshTokens.Where(x => x.UserId == request!.UserId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevokedByIp, ipAddress)
                .SetProperty(x => x.RevocationReason, "Email changed."), cancellationToken);
        // Per-entity revocation updates OpenIddict's entity caches as well as the database.
        // Bulk revocation alone can leave cached refresh credentials marked as valid.
        var subjectTokens = await MaterializeAsync(tokens.FindBySubjectAsync(request!.UserId.ToString(), cancellationToken));
        foreach (var token in subjectTokens)
        {
            if (!await tokens.TryRevokeAsync(token, cancellationToken))
                throw new InvalidOperationException("Failed to revoke an OpenID Connect token.");
        }
        var subjectAuthorizations = await MaterializeAsync(authorizations.FindBySubjectAsync(request.UserId.ToString(), cancellationToken));
        foreach (var authorization in subjectAuthorizations)
        {
            if (!await authorizations.TryRevokeAsync(authorization, cancellationToken))
                throw new InvalidOperationException("Failed to revoke an OpenID Connect authorization.");
        }
        await tokens.RevokeBySubjectAsync(request.UserId.ToString(), cancellationToken);
        await authorizations.RevokeBySubjectAsync(request.UserId.ToString(), cancellationToken);
        AddAudit("email_change.completed", request, ipAddress);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return request;
    }

    private static async Task<List<object>> MaterializeAsync(IAsyncEnumerable<object> entities)
    {
        // Close the query's reader before issuing writes on the same PostgreSQL connection.
        var results = new List<object>();
        await foreach (var entity in entities)
        {
            results.Add(entity);
        }
        return results;
    }

    private async Task LockUserAsync(EmailChangeRequest request, CancellationToken cancellationToken)
    {
        // A provider-neutral UPDATE acquires a write lock (including on SQLite).
        var count = await db.Users.Where(x => x.Id == request.UserId && x.Email == request.OriginalEmail
                && x.SecurityVersion == request.OriginalSecurityVersion)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Email, x => x.Email), cancellationToken);
        if (count != 1)
        {
            throw new AuthException(409, "Your account changed. Sign in again and request a new email change.");
        }
    }

    private static void ValidateRequest(EmailChangeRequest? request)
    {
        if (request is null || request.ConsumedAtUtc is not null || request.CancelledAtUtc is not null
            || request.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AuthException(400, "Email change link is invalid or expired.");
        }
    }

    private void AddAudit(string action, EmailChangeRequest request, string? ipAddress)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EventType = action, UserId = request.UserId, SubjectEmail = request.OriginalEmail,
            IpAddress = ipAddress, Success = true, Details = $"new_email:{request.NewEmail}"
        });
    }
}
