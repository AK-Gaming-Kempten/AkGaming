using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AkGaming.GamelyBot.Application;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordRestNotificationTransport(IHttpClientFactory httpClientFactory, IOptions<DiscordOptions> options) : INotificationTransport
{
    private readonly DiscordOptions _options = options.Value;

    public Task<TransportResult> SendChannelAsync(RenderedMessage message, CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        var content = string.IsNullOrWhiteSpace(message.RoleId) ? null : $"<@&{message.RoleId}>";
        var allowedRoleIds = string.IsNullOrWhiteSpace(message.RoleId) ? Array.Empty<string>() : new[] { message.RoleId };
        var payload = BuildPayload(content, allowedRoleIds, message);
        var channelId = string.IsNullOrWhiteSpace(message.ChannelId) ? _options.AdministrationChannelId : message.ChannelId;
        return SendMessageAsync($"channels/{channelId}/messages", payload, cancellationToken);
    }

    public async Task<TransportResult> UpdateChannelAsync(string externalMessageId, RenderedMessage message, CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        var channelId = string.IsNullOrWhiteSpace(message.ChannelId) ? _options.AdministrationChannelId : message.ChannelId;
        var payload = BuildPayload(null, [], message);
        var result = await SendAsync(HttpMethod.Patch, $"channels/{channelId}/messages/{externalMessageId}", payload, cancellationToken);
        if (result.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return await SendMessageAsync($"channels/{channelId}/messages", payload, cancellationToken);
        }
        if (!result.Response.IsSuccessStatusCode)
        {
            return ToFailure(result.Response.StatusCode, result.Body);
        }
        return TransportResult.Success(externalMessageId);
    }

    public async Task<TransportResult> SendDirectMessageAsync(string discordUserId, RenderedMessage message, CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        var memberResult = await SendWithoutBodyAsync(HttpMethod.Get, $"guilds/{_options.GuildId}/members/{discordUserId}", cancellationToken);
        if (!memberResult.Response.IsSuccessStatusCode)
            return memberResult.Response.StatusCode == HttpStatusCode.NotFound
                ? TransportResult.PermanentFailure("The linked Discord user is not a member of the configured club server.")
                : ToFailure(memberResult.Response.StatusCode, memberResult.Body);
        var dmResult = await SendAsync(HttpMethod.Post, "users/@me/channels", new { recipient_id = discordUserId }, cancellationToken);
        if (!dmResult.Response.IsSuccessStatusCode)
            return ToFailure(dmResult.Response.StatusCode, dmResult.Body);
        var channel = JsonSerializer.Deserialize<DiscordIdResponse>(dmResult.Body, JsonOptions());
        if (string.IsNullOrWhiteSpace(channel?.Id))
            return TransportResult.TemporaryFailure("Discord did not return a DM channel ID.");
        return await SendMessageAsync($"channels/{channel.Id}/messages", BuildPayload(null, [], message), cancellationToken);
    }

    private async Task<TransportResult> SendMessageAsync(string path, object payload, CancellationToken cancellationToken)
    {
        var result = await SendAsync(HttpMethod.Post, path, payload, cancellationToken);
        if (!result.Response.IsSuccessStatusCode)
            return ToFailure(result.Response.StatusCode, result.Body);
        var message = JsonSerializer.Deserialize<DiscordIdResponse>(result.Body, JsonOptions());
        return TransportResult.Success(message?.Id);
    }

    private async Task<(HttpResponseMessage Response, string Body)> SendAsync(HttpMethod method, string path, object payload, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(DiscordRestNotificationTransport));
        client.BaseAddress = new Uri("https://discord.com/api/v10/");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);
        request.Content = JsonContent.Create(payload);
        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response, body);
    }

    private async Task<(HttpResponseMessage Response, string Body)> SendWithoutBodyAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(DiscordRestNotificationTransport));
        client.BaseAddress = new Uri("https://discord.com/api/v10/");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);
        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response, body);
    }

    private static object BuildPayload(string? content, IReadOnlyCollection<string> allowedRoleIds, RenderedMessage message)
    {
        var components = message.Buttons is null || message.Buttons.Count == 0
            ? Array.Empty<object>()
            : new object[] { new { type = 1, components = message.Buttons.Select(button => new { type = 2, style = button.Style, label = button.Label, custom_id = button.CustomId }).ToArray() } };
        return new
        {
            content,
            allowed_mentions = new { parse = Array.Empty<string>(), roles = allowedRoleIds },
            embeds = new[]
            {
                new { title = message.Title, description = message.Body, url = message.Url, color = 0x5865F2 }
            },
            components
        };
    }

    private static TransportResult ToFailure(HttpStatusCode statusCode, string body)
    {
        var error = $"Discord returned {(int)statusCode}: {body}";
        return statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound
            ? TransportResult.PermanentFailure(error)
            : TransportResult.TemporaryFailure(error);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Token) || string.IsNullOrWhiteSpace(_options.GuildId) || string.IsNullOrWhiteSpace(_options.AdministrationChannelId))
            throw new InvalidOperationException("Discord Token, GuildId, and AdministrationChannelId are required for the Discord transport.");
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
    private sealed record DiscordIdResponse([property: JsonPropertyName("id")] string Id);
}
