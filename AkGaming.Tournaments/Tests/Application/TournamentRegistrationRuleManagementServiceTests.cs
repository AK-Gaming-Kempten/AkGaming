using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.RegistrationRules;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TournamentRegistrationRuleManagementServiceTests
{
    private InMemoryStore Store { get; set; } = null!;
    private FakeUnitOfWork UnitOfWork { get; set; } = null!;
    private TournamentRegistrationRuleManagementService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new TournamentRegistrationRuleManagementService(
            new InMemoryTournamentRepository(Store),
            new GameRankSystemRegistry(),
            UnitOfWork);
    }

    [Test]
    [Description("Verifies that replacing tournament registration rules stores the selected rule objects in order.")]
    public void ReplaceRegistrationRulesAsync_ReplacesRulesInOrder()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var tournament = TournamentTestData.AddTournament(Store);
        var rules = new[]
        {
            new TournamentRegistrationRuleUpdateDto("MinPlayersPerTeam", 5),
            new TournamentRegistrationRuleUpdateDto("MaxPlayersPerTeam", 7),
            new TournamentRegistrationRuleUpdateDto("MaxTeamAverageRankRating", 1599)
        };

        // Act
        var updatedRules = Service.ReplaceRegistrationRulesAsync(tournament.Id, rules).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(tournament.RegistrationRules.Select(rule => rule.GetType()), Is.EqualTo(new[]
            {
                typeof(MinPlayersPerTeamRegistrationRule),
                typeof(MaxPlayersPerTeamRegistrationRule),
                typeof(MaxTeamAverageRankRatingRegistrationRule)
            }));
            Assert.That(updatedRules.Select(rule => rule.Type), Is.EqualTo(new[] { "MinPlayersPerTeam", "MaxPlayersPerTeam", "MaxTeamAverageRankRating" }));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that replacing tournament registration rules rejects unsupported rule types.")]
    public void ReplaceRegistrationRulesAsync_RejectsUnsupportedRuleType()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var tournament = TournamentTestData.AddTournament(Store);
        var rules = new[] { new TournamentRegistrationRuleUpdateDto("Unsupported", 1) };

        // Act
        Task Act() => Service.ReplaceRegistrationRulesAsync(tournament.Id, rules);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }
}
