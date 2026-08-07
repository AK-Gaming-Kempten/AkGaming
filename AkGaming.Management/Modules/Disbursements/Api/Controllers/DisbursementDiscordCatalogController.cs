using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.Disbursements.Api.Controllers;

[ApiController]
[Route("disbursements/admin/discord")]
[Authorize(Policy = "management.disbursements.manage")]
public sealed class DisbursementDiscordCatalogController(IDiscordGuildCatalogService catalog) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<ActionResult<DiscordGuildCatalogDto>> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await catalog.GetAsync(cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        return Ok(result.Value);
    }
}
