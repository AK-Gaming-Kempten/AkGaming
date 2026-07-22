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
            .Select(item => new { item.EventType, item.Success, item.UserId })
            .ToListAsync(cancellationToken);
        var categories = events
            .GroupBy(item => item.EventType)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(10)
            .Select(group => new AuditSummaryCategory(group.Key, group.Count()))
            .ToList();
        var response = new AuditSummaryResponse("Identity", fromUtc, toUtc, events.Count,
            events.Where(item => item.UserId.HasValue).Select(item => item.UserId).Distinct().Count(),
            events.Count(item => item.Success), events.Count(item => !item.Success), categories);
        return Ok(response);
    }
}
