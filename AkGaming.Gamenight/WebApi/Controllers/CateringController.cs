using System.Security.Claims;
using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Gamenight.WebApi.Controllers;

[ApiController, Route("api/catering"), Authorize]
public sealed class CateringController(ICateringService service) : ControllerBase
{
    private Actor Actor => new(User.FindFirstValue("sub"),
        User.HasClaim("email_verified", "true") ? User.FindFirstValue("email") : null,
        User.FindAll("permission").Select(c => c.Value).ToHashSet());

    [HttpGet("menu/{eventId:guid}")]
    public async Task<ActionResult<CateringMenu>> Menu(Guid eventId, CancellationToken ct)
    {
        var result = await service.MenuAsync(eventId, Actor, ct);
        return Ok(result);
    }
    [HttpPut("menu"), Authorize(Policy = Permissions.Events)]
    public async Task<ActionResult<CateringMenu>> SaveMenu(CateringMenu request, CancellationToken ct)
    {
        var result = await service.SaveMenuAsync(request, Actor, ct);
        return Ok(result);
    }
    [HttpGet("mine")]
    public async Task<ActionResult<List<CateringView>>> Mine(CancellationToken ct)
    {
        var result = await service.OrdersAsync(Actor, true, ct);
        return Ok(result);
    }
    [HttpGet("orders"), Authorize(Policy = Permissions.Read)]
    public async Task<ActionResult<List<CateringView>>> Orders(CancellationToken ct)
    {
        var result = await service.OrdersAsync(Actor, false, ct);
        return Ok(result);
    }
    [HttpPost("orders/{registrationId:guid}")]
    public async Task<ActionResult<CateringView>> Place(Guid registrationId, PlaceCateringOrder request, CancellationToken ct)
    {
        var result = await service.PlaceAsync(registrationId, request, Actor, ct);
        return Ok(result);
    }
    [HttpPost("orders/{registrationId:guid}/actions")]
    public async Task<ActionResult<CateringView>> Action(Guid registrationId, CateringAction request, CancellationToken ct)
    {
        var result = await service.ActionAsync(registrationId, request, Actor, ct);
        return Ok(result);
    }
    [HttpPost("orders/{registrationId:guid}/settle"), Authorize(Policy = Permissions.Frontdesk), Authorize(Policy = Permissions.Read)]
    public async Task<IActionResult> Settle(Guid registrationId, SettleVisitor request, CancellationToken ct)
    {
        await service.SettleAsync(registrationId, request, Actor, ct);
        return NoContent();
    }
}
