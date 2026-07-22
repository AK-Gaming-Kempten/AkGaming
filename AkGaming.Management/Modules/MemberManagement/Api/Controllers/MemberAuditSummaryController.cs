using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Api.Controllers;

[ApiController]
[Route("internal/audit-summary")]
[Authorize(Policy = "management.audit-summaries")]
public sealed class MemberAuditSummaryController(MemberManagementDbContext dbContext) : ControllerBase
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
        var events = await dbContext.MemberAuditLogs
            .AsNoTracking()
            .Where(item => item.OccurredAtUtc >= from && item.OccurredAtUtc < to)
            .Select(item => new { item.ActionType, item.PerformedByUserId })
            .ToListAsync(cancellationToken);
        var categories = events
            .GroupBy(item => item.ActionType)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(10)
            .Select(group => new AuditSummaryCategory(group.Key, group.Count()))
            .ToList();
        var response = new AuditSummaryResponse("Management", fromUtc, toUtc, events.Count,
            events.Where(item => item.PerformedByUserId.HasValue).Select(item => item.PerformedByUserId).Distinct().Count(),
            null, null, categories);
        return Ok(response);
    }
}
