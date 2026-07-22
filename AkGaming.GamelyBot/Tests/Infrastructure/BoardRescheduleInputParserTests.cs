using AkGaming.GamelyBot.Infrastructure;
using Microsoft.Extensions.Options;

namespace AkGaming.GamelyBot.Tests.Infrastructure;

[TestFixture]
public sealed class BoardRescheduleInputParserTests
{
    [Test]
    [Description("Converts a documented German local meeting time to UTC using the configured club time zone.")]
    public void Parse_ValidClubTime_ReturnsUtcTime()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var result = parser.Parse("24.07.2026 19:30", "90");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.ProposedAtUtc, Is.EqualTo(new DateTimeOffset(2026, 7, 24, 17, 30, 0, TimeSpan.Zero)));
        Assert.That(result.DurationMinutes, Is.EqualTo(90));
    }

    [Test]
    [Description("Rejects a proposed date that does not use one of the documented unambiguous formats.")]
    public void Parse_InvalidDateFormat_ReturnsValidationError()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var result = parser.Parse("next Friday", "90");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("DD.MM.YYYY HH:mm"));
    }

    [Test]
    [Description("Rejects local times that are skipped during the daylight-saving transition.")]
    public void Parse_InvalidDaylightSavingTime_ReturnsValidationError()
    {
        // Arrange
        var parser = CreateParser();

        // Act
        var result = parser.Parse("29.03.2026 02:30", "90");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("daylight-saving"));
    }

    private static BoardRescheduleInputParser CreateParser()
    {
        return new BoardRescheduleInputParser(Options.Create(new DiscordInteractionOptions
        {
            TimeZoneId = "Europe/Berlin"
        }));
    }
}
