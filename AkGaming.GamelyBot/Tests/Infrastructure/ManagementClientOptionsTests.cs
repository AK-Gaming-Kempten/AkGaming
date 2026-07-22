using AkGaming.GamelyBot.Infrastructure;

namespace AkGaming.GamelyBot.Tests.Infrastructure;

[TestFixture]
public sealed class ManagementClientOptionsTests
{
    [Test]
    [Description("Builds board meeting links from the frontend base URL rather than the Management API URL.")]
    public void GetBoardMeetingsFrontendUrl_WithSeparateApiAndFrontend_ReturnsFrontendRoute()
    {
        // Arrange
        var options = new ManagementClientOptions
        {
            BaseUrl = "https://management.test.akgaming.de/api/",
            FrontendBaseUrl = "https://management.test.akgaming.de/"
        };

        // Act
        var result = options.GetBoardMeetingsFrontendUrl();
        var meetingId = Guid.Parse("bbeb2c03-ce1e-4a52-b900-2e1706ff6b45");
        var meetingResult = options.GetBoardMeetingFrontendUrl(meetingId);

        // Assert
        Assert.That(result, Is.EqualTo("https://management.test.akgaming.de/board/meetings"));
        Assert.That(result, Does.Not.Contain("/api/"));
        Assert.That(meetingResult, Is.EqualTo("https://management.test.akgaming.de/board/meetings/bbeb2c03-ce1e-4a52-b900-2e1706ff6b45"));
    }

    [Test]
    [Description("Rejects a missing frontend base URL instead of silently deriving links from the API address.")]
    public void GetBoardMeetingsFrontendUrl_WithoutFrontendBaseUrl_ThrowsConfigurationError()
    {
        // Arrange
        var options = new ManagementClientOptions
        {
            BaseUrl = "https://management.test.akgaming.de/api/"
        };

        // Act
        var action = options.GetBoardMeetingsFrontendUrl;

        // Assert
        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("FrontendBaseUrl"));
    }
}
