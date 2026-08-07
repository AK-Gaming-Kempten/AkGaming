using AkGaming.GamelyBot.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.GamelyBot.Api.Controllers;

[ApiController]
[Route("api/discord/catalog")]
[Authorize(Policy = "NotificationSubmitter")]
public sealed class DiscordCatalogController(IDiscordGuildCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DiscordGuildCatalog>> Get(CancellationToken cancellationToken)
    {
        var result = await catalog.GetAsync(cancellationToken);
        return Ok(result);
    }
}
