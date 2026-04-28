using AkGaming.Tournaments.Application.RegistrationRules;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class GameRankSystemRegistryTests
{
    [Test]
    [Description("Verifies that League ratings above the old top slider range are described as Master+ instead of separate Grandmaster or Challenger tiers.")]
    public void DescribeRating_UsesMasterPlusForHighLeagueRatings()
    {
        // Arrange
        var registry = new GameRankSystemRegistry();
        var rankSystem = registry.GetRankSystem("lol");

        // Act
        var description = rankSystem.DescribeRating(3205);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(description.Rank, Is.EqualTo("Master+"));
            Assert.That(description.Label, Is.EqualTo("Master+ 405 LP"));
            Assert.That(description.Rating, Is.EqualTo(3205));
        });
    }

    [Test]
    [Description("Verifies that the top rank bands remain open-ended so ratings above the slider maximum are not clamped.")]
    public void DescribeRating_DoesNotClampRatingsAboveTopBand()
    {
        // Arrange
        var registry = new GameRankSystemRegistry();
        var rankSystem = registry.GetRankSystem("ea-sports-fc");

        // Act
        var description = rankSystem.DescribeRating(1525);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(description.Rank, Is.EqualTo("Elite"));
            Assert.That(description.Label, Is.EqualTo("Elite 525 LP"));
            Assert.That(description.Rating, Is.EqualTo(1525));
        });
    }
}
