using System.Text.Json;
using AkGaming.Core.Notifications;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Domain;
using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Tests.Application;

[TestFixture]
public sealed class BotLocalizationTests
{
    [Test]
    [Description("Renders Discord-visible notification text and decimal values in German when de-DE is configured.")]
    public void Render_GermanCulture_ReturnsGermanMessageAndNumberFormat()
    {
        // Arrange
        var text = new BotText(new BotLocalizationOptions { Culture = "de-DE" });
        var renderer = new NotificationRenderer(
            Options.Create(new NotificationRoutingOptions { TreasurerRoleId = "treasurer" }),
            text);
        var payload = new ReimbursementSubmittedNotification(
            Guid.NewGuid(), "Antragsteller", "Reisekosten", 42.50m, "Submitted", null);
        var notification = new NotificationInboxItem
        {
            Type = NotificationEventTypes.ReimbursementSubmitted,
            DataJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

        // Act
        var rendered = renderer.Render(notification);

        // Assert
        Assert.That(rendered.ChannelMessage!.Title, Is.EqualTo("Neue Kostenerstattung"));
        Assert.That(rendered.ChannelMessage.Body, Does.Contain("42,50 EUR"));
        Assert.That(rendered.DirectMessage!.Body, Does.Contain("erfolgreich eingereicht"));
    }

    [Test]
    [Description("Returns German validation feedback for invalid board meeting rescheduling input.")]
    public void Parse_GermanCulture_ReturnsGermanValidationMessage()
    {
        // Arrange
        var text = new BotText(new BotLocalizationOptions { Culture = "de-DE" });
        var parser = new BoardRescheduleInputParser(
            Options.Create(new DiscordInteractionOptions { TimeZoneId = "Europe/Berlin" }),
            text);

        // Act
        var result = parser.Parse("morgen", "90");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Gib den vorgeschlagenen Termin"));
    }
}
