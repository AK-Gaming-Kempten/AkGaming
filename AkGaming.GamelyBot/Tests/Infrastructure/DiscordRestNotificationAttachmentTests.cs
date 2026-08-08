using System.Net;
using System.Net.Http.Json;
using System.Text;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;

namespace AkGaming.GamelyBot.Tests.Infrastructure;

[TestFixture]
public sealed class DiscordRestNotificationAttachmentTests
{
    [Test]
    [Description("Downloads a rendered guide and uploads it as a Discord message attachment with the role mention.")]
    public async Task SendChannel_WhenMessageHasAttachment_UploadsMultipartMessage()
    {
        // Arrange
        var handler = new RecordingHandler();
        var clients = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        clients.Setup(factory => factory.CreateClient(nameof(DiscordRestNotificationTransport)))
            .Returns(() => new HttpClient(handler, disposeHandler: false));
        var transport = new DiscordRestNotificationTransport(clients.Object, Options.Create(new DiscordOptions
        {
            Token = "bot-token",
            GuildId = "guild-123",
            AdministrationChannelId = "administration-channel"
        }));
        var message = new RenderedMessage(
            "Prize money available",
            "The allocation can now be claimed.",
            "https://management.test/claim",
            "role-456",
            "channel-123",
            Attachment: new RenderedAttachment(
                "https://management.test/guides/disbursement-claim-guide-de.png",
                "disbursement-claim-guide-de.png",
                "image/png"));

        // Act
        var result = await transport.SendChannelAsync(message, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ExternalMessageId, Is.EqualTo("message-789"));
            Assert.That(handler.GuidePath, Is.EqualTo("/guides/disbursement-claim-guide-de.png"));
            Assert.That(handler.DiscordPath, Is.EqualTo("/api/v10/channels/channel-123/messages"));
            Assert.That(handler.DiscordContentType, Is.EqualTo("multipart/form-data"));
            Assert.That(handler.DiscordBody, Does.Contain("name=payload_json"));
            Assert.That(handler.DiscordBody, Does.Contain("name=\"files[0]\""));
            Assert.That(handler.DiscordBody, Does.Contain("filename=disbursement-claim-guide-de.png"));
            Assert.That(handler.DiscordBody, Does.Contain("guide-image-bytes"));
            Assert.That(handler.DiscordBody, Does.Contain("\\u003C@\\u0026role-456\\u003E"));
            Assert.That(handler.DiscordBody, Does.Contain("\"attachments\":[{\"id\":0,\"filename\":\"disbursement-claim-guide-de.png\"}]"));
        });
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? GuidePath { get; private set; }
        public string? DiscordPath { get; private set; }
        public string? DiscordContentType { get; private set; }
        public string? DiscordBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host == "management.test")
            {
                GuidePath = request.RequestUri.AbsolutePath;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("guide-image-bytes"))
                };
            }

            DiscordPath = request.RequestUri?.AbsolutePath;
            DiscordContentType = request.Content?.Headers.ContentType?.MediaType;
            DiscordBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = "message-789" })
            };
        }
    }
}
