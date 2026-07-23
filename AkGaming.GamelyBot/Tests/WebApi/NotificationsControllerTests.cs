using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Api.Controllers;
using AkGaming.GamelyBot.Application;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.GamelyBot.Tests.WebApi;

[TestFixture]
public sealed class NotificationsControllerTests
{
    private Mock<INotificationInbox> _inbox = null!;
    private NotificationsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _inbox = new Mock<INotificationInbox>(MockBehavior.Strict);
        _controller = new NotificationsController(_inbox.Object);
    }

    [Test]
    [Description("Accepts a supported notification after the durable inbox stores it.")]
    public async Task Submit_WhenNotificationIsNew_ReturnsAccepted()
    {
        // Arrange
        var request = ValidRequest();
        _inbox.Setup(inbox => inbox.AcceptAsync(request, CancellationToken.None)).ReturnsAsync(false);

        // Act
        var response = await _controller.Submit(request, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<AcceptedResult>());
        _inbox.Verify(inbox => inbox.AcceptAsync(request, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Returns an idempotent success response when a notification event already exists.")]
    public async Task Submit_WhenNotificationIsDuplicate_ReturnsOk()
    {
        // Arrange
        var request = ValidRequest();
        _inbox.Setup(inbox => inbox.AcceptAsync(request, CancellationToken.None)).ReturnsAsync(true);

        // Act
        var response = await _controller.Submit(request, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (NotificationAcceptedResponse)((OkObjectResult)response.Result!).Value!;
        Assert.That(result.IsDuplicate, Is.True);
    }

    [TestCase(NotificationEventTypes.MembershipApplicationStatusChanged)]
    [TestCase(NotificationEventTypes.MemberLinkingRequestStatusChanged)]
    [TestCase(NotificationEventTypes.MembershipStatusChanged)]
    [TestCase(NotificationEventTypes.BoardMeetingAvailabilityChanged)]
    [Description("Accepts supported update events so their outboxes can deliver or update Discord messages.")]
    public async Task Submit_WhenUpdateTypeIsSupported_ReturnsAccepted(string notificationType)
    {
        // Arrange
        var request = ValidRequest(notificationType);
        _inbox.Setup(inbox => inbox.AcceptAsync(request, CancellationToken.None)).ReturnsAsync(false);

        // Act
        var response = await _controller.Submit(request, CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.TypeOf<AcceptedResult>());
        _inbox.Verify(inbox => inbox.AcceptAsync(request, CancellationToken.None), Times.Once);
    }

    private static NotificationEnvelope ValidRequest(
        string notificationType = NotificationEventTypes.ReimbursementSubmitted)
    {
        return new NotificationEnvelope(
            Guid.NewGuid(),
            notificationType,
            "management",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            JsonSerializer.SerializeToElement(new { reimbursementId = Guid.NewGuid() }));
    }
}
