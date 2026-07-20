using AkGaming.GamelyBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.GamelyBot.Api.Controllers;

[ApiController]
[Route("api/debug/deliveries")]
[AllowAnonymous]
public sealed class DebugDeliveriesController(GamelyBotDbContext dbContext, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();
        var storedDeliveries = await dbContext.Deliveries
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var deliveries = storedDeliveries
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .Select(item => new
            {
                item.Id,
                item.Kind,
                item.Target,
                item.Title,
                item.Body,
                item.Status,
                item.AttemptCount,
                item.ExternalMessageId,
                item.LastError,
                item.CreatedAtUtc,
                item.DeliveredAtUtc
            })
            .ToList();
        return Ok(deliveries);
    }
}
