using System.Text;
using System.Text.Json;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace AkGaming.GamelyBot.Api.Controllers;

[ApiController]
[Route("api/discord/interactions")]
[AllowAnonymous]
public sealed class DiscordInteractionsController(DiscordInteractionService interactions, IOptions<DiscordOptions> discordOptions,
    IOptions<DiscordInteractionOptions> interactionOptions, ILogger<DiscordInteractionsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        if (interactionOptions.Value.ValidateSignatures && !HasValidSignature(body)) return Unauthorized();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.GetProperty("type").GetInt32() == 1) return Ok(new { type = 1 });
        if (root.GetProperty("type").GetInt32() != 3) return Ok(Ephemeral("Unsupported interaction."));
        var guildId = root.TryGetProperty("guild_id", out var guild) ? guild.GetString() : null;
        if (!string.Equals(guildId, discordOptions.Value.GuildId, StringComparison.Ordinal)) return Ok(Ephemeral("This bot only accepts interactions from the configured club server."));
        var customId = root.GetProperty("data").GetProperty("custom_id").GetString();
        var parts = customId?.Split(':');
        if (parts is not { Length: 4 } || parts[0] != "board-availability" || !Guid.TryParse(parts[1], out var meetingId) || !int.TryParse(parts[2], out var version)) return Ok(Ephemeral("This meeting action is invalid."));
        var discordUserId = root.GetProperty("member").GetProperty("user").GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(discordUserId)) return Ok(Ephemeral("Discord did not identify your account."));
        var status = parts[3] == "available" ? "Available" : "Unavailable";
        using var interactionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        interactionTimeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            var message = await interactions.SetAvailabilityAsync(discordUserId, meetingId, version, status, interactionTimeout.Token);
            return Ok(Ephemeral(message));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Discord availability interaction timed out for meeting {MeetingId}.", meetingId);
            return Ok(Ephemeral("The club services did not respond in time. Please try again or use the management tool."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discord availability interaction failed for meeting {MeetingId}.", meetingId);
            return Ok(Ephemeral("I could not save your availability right now. Please try again or use the management tool."));
        }
    }

    private bool HasValidSignature(string body)
    {
        var signatureHex = Request.Headers["X-Signature-Ed25519"].ToString();
        var timestamp = Request.Headers["X-Signature-Timestamp"].ToString();
        if (string.IsNullOrWhiteSpace(signatureHex) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(discordOptions.Value.ApplicationPublicKey)) return false;
        try
        {
            if (!long.TryParse(timestamp, out var timestampSeconds)) return false;
            var signedAt = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            if (DateTimeOffset.UtcNow - signedAt > TimeSpan.FromMinutes(5) || signedAt - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(1)) return false;
            var algorithm = SignatureAlgorithm.Ed25519;
            var publicKey = PublicKey.Import(algorithm, Convert.FromHexString(discordOptions.Value.ApplicationPublicKey), KeyBlobFormat.RawPublicKey);
            return algorithm.Verify(publicKey, Encoding.UTF8.GetBytes(timestamp + body), Convert.FromHexString(signatureHex));
        }
        catch (Exception) { return false; }
    }

    private static object Ephemeral(string content) => new { type = 4, data = new { content, flags = 64 } };
}
