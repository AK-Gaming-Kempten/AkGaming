using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;

namespace AkGaming.GamelyBot.Tests.Infrastructure;

[TestFixture]
public sealed class DiscordClaimMessageUpdateTests
{
    [TestCase(HttpStatusCode.OK)]
    [TestCase(HttpStatusCode.NotFound)]
    [Description("Only attempts to edit the claim message without posting or pinging, even when Discord reports it missing.")]
    public async Task UpdateChannel_OnlyEditsExistingMessage(HttpStatusCode statusCode)
    {
        // Arrange
        using var handler = new RecordingHandler(statusCode);
        using var client = new HttpClient(handler);
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(factory => factory.CreateClient(nameof(DiscordRestNotificationTransport))).Returns(client);
        var transport = new DiscordRestNotificationTransport(clients.Object, Options.Create(new DiscordOptions
        {
            Token = "test-token",
            GuildId = "guild",
            AdministrationChannelId = "administration"
        }));
        var message = new RenderedMessage("Claim", "Approvals: Anna; Objections: Berta",
            RoleId: "role-456", ChannelId: "channel-123");

        // Act
        var result = await transport.UpdateChannelAsync("claim-message", message, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.EqualTo(statusCode == HttpStatusCode.OK));
            Assert.That(result.IsPermanentFailure, Is.EqualTo(statusCode == HttpStatusCode.NotFound));
            Assert.That(result.ExternalMessageId, Is.EqualTo(statusCode == HttpStatusCode.OK ? "claim-message" : null));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Patch));
            Assert.That(handler.Path, Is.EqualTo("/api/v10/channels/channel-123/messages/claim-message"));
            Assert.That(handler.Body.GetProperty("content").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(handler.Body.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength(), Is.Zero);
            Assert.That(handler.Body.GetProperty("allowed_mentions").GetProperty("roles").GetArrayLength(), Is.Zero);
            Assert.That(handler.Body.GetProperty("embeds")[0].GetProperty("description").GetString(), Is.EqualTo(message.Body));
        });
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public JsonElement Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            Path = request.RequestUri!.AbsolutePath;
            Body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return new HttpResponseMessage(statusCode) { Content = JsonContent.Create(new { id = "claim-message" }) };
        }
    }
}
