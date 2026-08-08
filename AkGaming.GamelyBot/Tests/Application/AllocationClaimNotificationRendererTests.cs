using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Domain;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Tests.Application;

[TestFixture]
public sealed class AllocationClaimNotificationRendererTests
{
    private NotificationRenderer _renderer = null!;

    [SetUp]
    public void SetUp()
    {
        _renderer = new NotificationRenderer(Options.Create(new NotificationRoutingOptions()));
    }

    [Test]
    [Description("Routes an allocation claim to its configured channel and role with current decisions and Discord buttons.")]
    public void Render_WhenClaimIsActive_IncludesRoutingDecisionsAndButtons()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var notification = Notification(new AllocationClaimChangedNotification(
            applicationId,
            "Summer cup",
            "Team prize",
            "Chris",
            200m,
            "Paid to the team captain",
            "Submitted",
            ["Anna", "Dora"],
            ["Berta"],
            "https://management.test/claim",
            "channel-123",
            "role-456"));

        // Act
        var rendered = _renderer.Render(notification);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rendered.ChannelMessage?.ChannelId, Is.EqualTo("channel-123"));
            Assert.That(rendered.ChannelMessage?.RoleId, Is.EqualTo("role-456"));
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("Anna, Dora"));
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("Berta"));
            Assert.That(rendered.ChannelMessage?.Buttons, Has.Count.EqualTo(2));
            Assert.That(rendered.ChannelMessage?.Buttons?[0].CustomId, Is.EqualTo($"ac:{applicationId}:a"));
            Assert.That(rendered.ChannelMessage?.Buttons?[1].CustomId, Is.EqualTo($"ac:{applicationId}:o"));
            Assert.That(rendered.DirectMessage, Is.Null);
        });
    }

    [Test]
    [Description("Removes allocation decision buttons after the claim reaches a terminal status.")]
    public void Render_WhenClaimIsPaid_RemovesDecisionButtons()
    {
        // Arrange
        var notification = Notification(new AllocationClaimChangedNotification(
            Guid.NewGuid(), "Summer cup", "Team prize", "Chris", 200m, null, "Paid",
            ["Anna"], [], null, "channel-123", "role-456"));

        // Act
        var rendered = _renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage?.Buttons, Is.Null);
    }

    [Test]
    [Description("Renders an available-prize announcement in the allocation channel with its role mention and claim link.")]
    public void Render_WhenAllocationIsAvailable_IncludesPrizeDetailsAndRouting()
    {
        // Arrange
        var data = new AllocationAvailableNotification(
            Guid.NewGuid(),
            "Summer cup",
            "Team prize",
            "First place prize",
            200m,
            "https://management.test/claim",
            "https://management.test/guides/disbursement-claim-guide-de.png",
            "channel-123",
            "role-456");
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.AllocationAvailable,
            DataJson = JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = _renderer.Render(notification);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rendered.ChannelMessage?.ChannelId, Is.EqualTo("channel-123"));
            Assert.That(rendered.ChannelMessage?.RoleId, Is.EqualTo("role-456"));
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("Team prize"));
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("200.00 EUR"));
            Assert.That(rendered.ChannelMessage?.Body, Does.Contain("https://management.test/claim"));
            Assert.That(rendered.ChannelMessage?.Attachment?.Url,
                Is.EqualTo("https://management.test/guides/disbursement-claim-guide-de.png"));
            Assert.That(rendered.ChannelMessage?.Attachment?.FileName,
                Is.EqualTo("disbursement-claim-guide-de.png"));
            Assert.That(rendered.DirectMessage, Is.Null);
        });
    }

    private static NotificationInboxItem Notification(AllocationClaimChangedNotification data)
    {
        return new NotificationInboxItem
        {
            Type = NotificationEventTypes.AllocationClaimChanged,
            DataJson = JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };
    }
}
