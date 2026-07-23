using AkGaming.Core.Notifications;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Identity.Api.Controllers;

[ApiController]
[Route("internal/audit-summary")]
[Authorize(Policy = "IdentityAuditSummaries")]
public sealed class InternalAuditSummaryController(AuthDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditSummaryResponse>> Get(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        if (fromUtc >= toUtc)
            return BadRequest("fromUtc must be earlier than toUtc.");

        var from = fromUtc.UtcDateTime;
        var to = toUtc.UtcDateTime;
        var events = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(item => item.CreatedAtUtc >= from && item.CreatedAtUtc < to)
            .Select(item => new { item.EventType, item.Success, item.UserId, item.Details })
            .ToListAsync(cancellationToken);
        var categories = events
            .GroupBy(item => item.EventType)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(10)
            .Select(group => new AuditSummaryCategory(group.Key, group.Count()))
            .ToList();
        var sections = new List<AuditSummarySection>
        {
            new("Account activity",
            [
                Metric("New accounts", events.Count(item =>
                    item.EventType == "register.success"
                    || item.EventType == "discord.login.success" && item.Details == "created_user")),
                Metric("Discord accounts linked", Count("discord.link.success")),
                Metric("Email addresses verified", Count("email_verification.success")),
                Metric("Password reset requests", Count("password_reset.issued")),
                Metric("Password resets completed", Count("password_reset.success")),
                Metric("Successful logins", Count("login.success") + Count("discord.login.success"))
            ]),
            new("Security signals",
            [
                Metric("Failed account registrations", Count("register.failed")),
                Metric("Failed login attempts", Count("login.failed") + Count("discord.login.failed")),
                Metric("Account lockouts", Count("login.locked")),
                Metric("Refresh token reuse detections", Count("refresh.reuse_detected")),
                Metric("Failed Discord linking attempts", Count("discord.link.failed")),
                Metric("Failed email verification attempts", Count("email_verification.failed")),
                Metric("Failed password reset attempts", Count("password_reset.failed")),
                Metric("Identity email delivery failures",
                    Count("email_verification.email_send_failed") + Count("password_reset.email_send_failed"))
            ])
        };
        var response = new AuditSummaryResponse("Identity", fromUtc, toUtc, events.Count,
            events.Where(item => item.UserId.HasValue).Select(item => item.UserId).Distinct().Count(),
            events.Count(item => item.Success), events.Count(item => !item.Success), categories, sections);
        return Ok(response);

        int Count(string eventType)
        {
            return events.Count(item => item.EventType == eventType);
        }

        static AuditSummaryCategory Metric(string name, int count)
        {
            return new AuditSummaryCategory(name, count);
        }
    }
}
