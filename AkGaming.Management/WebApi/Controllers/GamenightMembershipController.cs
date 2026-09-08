using AkGaming.Management.Modules.MemberManagement.Domain.Enums;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.WebApi.Controllers;

[ApiController, Route("internal/gamenight-membership")]
[Authorize(Policy = "management.gamenight-membership")]
public sealed class GamenightMembershipController(MemberManagementDbContext db) : ControllerBase
{
    [HttpGet("periods")]
    public async Task<IActionResult> Periods(CancellationToken ct)
    {
        var periods = await db.MembershipPaymentPeriods.AsNoTracking()
            .OrderByDescending(p => p.DueDate).Select(p => new { p.Id, p.Name }).ToListAsync(ct);
        return Ok(periods);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Eligibility(Guid userId, [FromQuery] int periodId, CancellationToken ct)
    {
        if (!await db.MembershipPaymentPeriods.AnyAsync(p => p.Id == periodId, ct))
            return NotFound();
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.UserId == userId, ct);
        var active = member?.Status is MembershipStatus.Member or MembershipStatus.HonoraryMember or MembershipStatus.SupportingMember;
        var paid = active && await db.MembershipDues.AnyAsync(d =>
            d.MemberId == member!.Id && d.PaymentPeriodId == periodId && d.Status == MembershipDueStatus.Paid, ct);
        return Ok(new { Eligible = paid, Reason = paid ? "paid" : "not-eligible" });
    }
}
