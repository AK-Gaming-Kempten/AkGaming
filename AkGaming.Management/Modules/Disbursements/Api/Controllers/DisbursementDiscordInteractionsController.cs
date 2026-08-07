using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.Management.Modules.Disbursements.Api.Controllers;

[ApiController]
[Route("disbursements/discord")]
[Authorize(Policy = "management.disbursements.discord-interactions")]
public sealed class DisbursementDiscordInteractionsController(IDisbursementService service) : ControllerBase
{
    [HttpPut("applications/{applicationId:guid}/decision")]
    public async Task<ActionResult<AllocationApplicationDto>> Decide(
        Guid applicationId,
        [FromBody] DiscordAllocationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.DecideFromDiscordAsync(applicationId, request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Error);
        return Ok(result.Value);
    }
}
