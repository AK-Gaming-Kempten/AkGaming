using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class TournamentsControllerTests
{
    private Mock<ITournamentLogoManagementService> LogoService { get; set; } = null!;
    private Mock<ITournamentRegistrationRuleManagementService> RuleService { get; set; } = null!;
    private TournamentsController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        LogoService = new Mock<ITournamentLogoManagementService>();
        RuleService = new Mock<ITournamentRegistrationRuleManagementService>();
        Controller = new TournamentsController(LogoService.Object, RuleService.Object);
    }

    [Test]
    [Description("Verifies that the tournaments controller passes logo updates to the application service.")]
    public async Task UpdateTournamentLogo_ReturnsNoContent()
    {
        // Arrange
        var tournamentId = Guid.NewGuid();
        var logoAssetId = Guid.NewGuid();
        LogoService
            .Setup(mock => mock.UpdateTournamentLogoAsync(tournamentId, logoAssetId, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var response = await Controller.UpdateTournamentLogo(tournamentId, new UpdateTournamentLogoRequest(logoAssetId), CancellationToken.None);

        // Assert
        Assert.That(response, Is.InstanceOf<NoContentResult>());
        LogoService.Verify(mock => mock.UpdateTournamentLogoAsync(tournamentId, logoAssetId, CancellationToken.None), Times.Once);
    }

    [Test]
    [Description("Verifies that the tournaments controller passes registration rule replacement values to the application service.")]
    public async Task ReplaceTournamentRegistrationRules_ReturnsOkWithRules()
    {
        // Arrange
        var tournamentId = Guid.NewGuid();
        var ruleUpdates = new[] { new TournamentRegistrationRuleUpdateDto("MinPlayersPerTeam", 5) };
        var rules = new[] { new TournamentRegistrationRuleDto("MinPlayersPerTeam", "Minimum players", 5, "5") };
        var request = new ReplaceTournamentRegistrationRulesRequest(ruleUpdates);
        RuleService
            .Setup(mock => mock.ReplaceRegistrationRulesAsync(tournamentId, ruleUpdates, CancellationToken.None))
            .ReturnsAsync(rules);

        // Act
        var response = await Controller.ReplaceTournamentRegistrationRules(tournamentId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, rules);
        RuleService.Verify(mock => mock.ReplaceRegistrationRulesAsync(tournamentId, ruleUpdates, CancellationToken.None), Times.Once);
    }
}
