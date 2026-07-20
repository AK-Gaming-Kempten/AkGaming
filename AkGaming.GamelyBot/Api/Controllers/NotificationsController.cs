using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkGaming.GamelyBot.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "NotificationSubmitter")]
public sealed class NotificationsController(INotificationInbox inbox) : ControllerBase
{
    private static readonly HashSet<string> SupportedTypes =
    [
        NotificationEventTypes.ReimbursementSubmitted,
        NotificationEventTypes.ReimbursementStatusChanged
    ];

    [HttpPost]
    public async Task<ActionResult<NotificationAcceptedResponse>> Submit([FromBody] NotificationEnvelope request, CancellationToken cancellationToken)
    {
        if (request.EventId == Guid.Empty || string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Source))
            return BadRequest("EventId, Type, and Source are required.");
        if (!SupportedTypes.Contains(request.Type))
            return BadRequest($"Notification type '{request.Type}' is not supported.");
        if (request.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return BadRequest("Data is required.");

        var isDuplicate = await inbox.AcceptAsync(request, cancellationToken);
        if (isDuplicate)
            return Ok(new NotificationAcceptedResponse(request.EventId, true));
        return Accepted(new NotificationAcceptedResponse(request.EventId, false));
    }
}
