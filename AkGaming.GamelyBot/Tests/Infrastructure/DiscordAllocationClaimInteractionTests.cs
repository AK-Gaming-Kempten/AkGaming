using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;

namespace AkGaming.GamelyBot.Tests.Infrastructure;

[TestFixture]
public sealed class DiscordAllocationClaimInteractionTests
{
    [Test]
    [Description("Lets a linked Discord user object to an allocation claim without requiring board access.")]
    public async Task SetDecision_WhenLinkedUserIsNotBoardMember_SubmitsAllocationDecision()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = new RecordingHandler(userId);
        var service = CreateService(handler, out var text);

        // Act
        var result = await service.SetAllocationClaimDecisionAsync(
            "discord-user",
            applicationId,
            false,
            CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(text["AllocationClaimDecisionObjected"]));
            Assert.That(handler.ManagementPath,
                Is.EqualTo($"/api/disbursements/discord/applications/{applicationId}/decision"));
            Assert.That(handler.ManagementBody.GetProperty("userId").GetGuid(), Is.EqualTo(userId));
            Assert.That(handler.ManagementBody.GetProperty("approverName").GetString(), Is.EqualTo("Linked user"));
            Assert.That(handler.ManagementBody.GetProperty("isApproved").GetBoolean(), Is.False);
        });
    }

    [Test]
    [Description("Tells an unlinked Discord user how to link their account when they try to review an allocation claim.")]
    public async Task SetDecision_WhenDiscordAccountIsNotLinked_ReturnsLinkingInstructions()
    {
        // Arrange
        var handler = new RecordingHandler(Guid.NewGuid(), isLinked: false);
        var service = CreateService(handler, out var text);

        // Act
        var result = await service.SetAllocationClaimDecisionAsync(
            "unlinked-discord-user",
            Guid.NewGuid(),
            true,
            CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(text["DiscordAccountNotLinkedWithHelp"]));
        Assert.That(handler.ManagementPath, Is.Null);
    }

    private static DiscordInteractionService CreateService(RecordingHandler handler, out BotText text)
    {
        var clients = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        clients.Setup(factory => factory.CreateClient(nameof(DiscordInteractionService)))
            .Returns(() => new HttpClient(handler, disposeHandler: false));
        var identityOptions = Options.Create(new IdentityClientOptions
        {
            BaseUrl = "https://identity.test/",
            UseAuthentication = false
        });
        var tokens = new ClientCredentialsTokenProvider(clients.Object, identityOptions);
        text = new BotText(new BotLocalizationOptions { Culture = "en-GB" });
        return new DiscordInteractionService(
            clients.Object,
            tokens,
            identityOptions,
            Options.Create(new ManagementClientOptions { BaseUrl = "https://management.test/api/" }),
            Options.Create(new DiscordInteractionOptions()),
            Mock.Of<INotificationInbox>(),
            text);
    }

    private sealed class RecordingHandler(Guid userId, bool isLinked = true) : HttpMessageHandler
    {
        public string? ManagementPath { get; private set; }
        public JsonElement ManagementBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host == "identity.test")
            {
                if (!isLinked)
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                var link = new DiscordUserLinkResponse(
                    userId,
                    "Linked user",
                    true,
                    false,
                    false);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(link)
                };
            }

            ManagementPath = request.RequestUri?.AbsolutePath;
            ManagementBody = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
