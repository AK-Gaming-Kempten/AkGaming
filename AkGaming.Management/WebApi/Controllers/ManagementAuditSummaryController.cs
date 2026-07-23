using AkGaming.Core.Notifications;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.Disbursements.Infrastructure.Persistence;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.WebApi.Controllers;

[ApiController]
[Route("internal/audit-summary")]
[Authorize(Policy = "management.audit-summaries")]
public sealed class ManagementAuditSummaryController(
    MemberManagementDbContext memberDbContext,
    DisbursementsDbContext disbursementsDbContext) : ControllerBase
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
        var memberEvents = await memberDbContext.MemberAuditLogs
            .AsNoTracking()
            .Where(item => item.OccurredAtUtc >= from && item.OccurredAtUtc < to)
            .Select(item => new { item.ActionType, item.PerformedByUserId })
            .ToListAsync(cancellationToken);
        var reimbursements = await disbursementsDbContext.Reimbursements
            .AsNoTracking()
            .Select(item => new { item.CreatedAt, item.Status })
            .ToListAsync(cancellationToken);

        var newReimbursements = reimbursements.Count(item =>
            item.CreatedAt >= fromUtc && item.CreatedAt < toUtc);
        var openReimbursements = reimbursements.Count(item =>
            item.Status is (int)DisbursementStatus.Submitted
                or (int)DisbursementStatus.UnderReview
                or (int)DisbursementStatus.Approved);
        var openApplications = await memberDbContext.MembershipApplicationRequests
            .AsNoTracking()
            .CountAsync(item => !item.IsResolved, cancellationToken);
        var openLinkingRequests = await memberDbContext.MemberLinkingRequests
            .AsNoTracking()
            .CountAsync(item => !item.IsResolved, cancellationToken);

        var categories = memberEvents
            .GroupBy(item => item.ActionType)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(10)
            .Select(group => new AuditSummaryCategory(group.Key, group.Count()))
            .ToList();
        var sections = new List<AuditSummarySection>
        {
            new("Activity during this period",
            [
                Metric("Membership applications received", Count("MembershipApplicationRequestCreated")),
                Metric("Membership applications accepted", Count("MembershipApplicationRequestAccepted")),
                Metric("Membership applications rejected", Count("MembershipApplicationRequestRejected")),
                Metric("Member linking requests received", Count("MemberLinkingRequestCreated")),
                Metric("Member linking requests accepted", Count("MemberLinkingRequestAccepted")),
                Metric("Member linking requests rejected", Count("MemberLinkingRequestRejected")),
                Metric("Reimbursements submitted", newReimbursements)
            ]),
            new("Currently requiring attention",
            [
                Metric("Open membership applications", openApplications),
                Metric("Open member linking requests", openLinkingRequests),
                Metric("Open reimbursements", openReimbursements)
            ])
        };
        var response = new AuditSummaryResponse("Management", fromUtc, toUtc,
            memberEvents.Count + newReimbursements,
            memberEvents.Where(item => item.PerformedByUserId.HasValue)
                .Select(item => item.PerformedByUserId)
                .Distinct()
                .Count(),
            null, null, categories, sections);
        return Ok(response);

        int Count(string actionType)
        {
            return memberEvents.Count(item => item.ActionType == actionType);
        }

        static AuditSummaryCategory Metric(string name, int count)
        {
            return new AuditSummaryCategory(name, count);
        }
    }
}
