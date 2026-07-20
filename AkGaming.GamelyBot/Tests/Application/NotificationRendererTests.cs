using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Domain;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Tests.Application;

[TestFixture]
public sealed class NotificationRendererTests
{
    [Test]
    [Description("Renders a reimbursement submission for the treasurer channel and the linked applicant.")]
    public void Render_WhenReimbursementIsSubmitted_ReturnsChannelAndDirectMessages()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions { TreasurerRoleId = "treasurer" }));
        var payload = new ReimbursementSubmittedNotification(Guid.NewGuid(), "Applicant", "Travel", 42.50m, "Submitted", "https://management.test/reimbursement");
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.ReimbursementSubmitted,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage, Is.Not.Null);
        Assert.That(rendered.ChannelMessage!.RoleId, Is.EqualTo("treasurer"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("42,50 EUR"));
        Assert.That(rendered.DirectMessage, Is.Not.Null);
    }

    [Test]
    [Description("Renders reimbursement status changes only as a private applicant notification.")]
    public void Render_WhenReimbursementStatusChanges_ReturnsOnlyDirectMessage()
    {
        // Arrange
        var renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions()));
        var payload = new ReimbursementStatusChangedNotification(Guid.NewGuid(), "Applicant", "Travel", 42.50m, "Submitted", "Approved", "Looks good", null);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.ReimbursementStatusChanged,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage, Is.Null);
        Assert.That(rendered.DirectMessage!.Body, Does.Contain("Approved"));
        Assert.That(rendered.DirectMessage.Body, Does.Contain("Looks good"));
    }
}
