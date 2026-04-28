using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Tournaments.Tests.WebApi;

public sealed class TournamentsControllerTests
{
    private Mock<ITournamentCatalogService> CatalogService { get; set; } = null!;
    private Mock<ITournamentContentManagementService> ContentService { get; set; } = null!;
    private Mock<ITournamentLogoManagementService> LogoService { get; set; } = null!;
    private Mock<ITournamentRegistrationRuleManagementService> RuleService { get; set; } = null!;
    private TournamentsController Controller { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        CatalogService = new Mock<ITournamentCatalogService>();
        ContentService = new Mock<ITournamentContentManagementService>();
        LogoService = new Mock<ITournamentLogoManagementService>();
        RuleService = new Mock<ITournamentRegistrationRuleManagementService>();
        Controller = new TournamentsController(CatalogService.Object, ContentService.Object, LogoService.Object, RuleService.Object);
    }

    [Test]
    [Description("Verifies that the tournaments controller returns the catalog summary list from the application service.")]
    public async Task GetTournaments_ReturnsOkWithSummaries()
    {
        // Arrange
        var tournaments = new[]
        {
            new TournamentSummaryDto(Guid.NewGuid(), "rift-rumble", "lol", "League of Legends", "Rift Rumble", null, TournamentStatusDto.RegistrationOpen, null, null, null, null, 4)
        };
        CatalogService
            .Setup(mock => mock.GetTournamentsAsync(CancellationToken.None))
            .ReturnsAsync(tournaments);

        // Act
        var response = await Controller.GetTournaments(CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, tournaments);
    }

    [Test]
    [Description("Verifies that the tournaments controller returns not found when a requested slug does not exist.")]
    public async Task GetTournamentBySlug_ReturnsNotFoundWhenMissing()
    {
        // Arrange
        CatalogService
            .Setup(mock => mock.GetTournamentBySlugAsync("missing", CancellationToken.None))
            .ReturnsAsync((TournamentDto?)null);

        // Act
        var response = await Controller.GetTournamentBySlug("missing", CancellationToken.None);

        // Assert
        Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
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
    [Description("Verifies that the tournaments controller forwards timeline and info section edits to the application service.")]
    public async Task UpdateTournamentContent_ReturnsOkWithUpdatedTournament()
    {
        // Arrange
        var tournamentId = Guid.NewGuid();
        var request = new UpdateTournamentContentRequest(
            new DateTimeOffset(2026, 4, 1, 16, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 16, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 12, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 13, 12, 0, 0, TimeSpan.Zero),
            [new TournamentInfoSectionUpdateDto("Format", "Markdown")]);
        var tournament = new TournamentDto(
            tournamentId,
            "rift-rumble",
            "lol",
            "League of Legends",
            "Rift Rumble",
            null,
            TournamentStatusDto.RegistrationOpen,
            request.RegistrationOpenUtc,
            request.RegistrationClosedUtc,
            request.StartUtc,
            request.EndUtc,
            [new TournamentInfoSectionDto(Guid.NewGuid(), "Format", "Markdown", 0)]);
        ContentService
            .Setup(mock => mock.UpdateTournamentContentAsync(
                tournamentId,
                request.RegistrationOpenUtc,
                request.RegistrationClosedUtc,
                request.StartUtc,
                request.EndUtc,
                request.InfoSections,
                CancellationToken.None))
            .ReturnsAsync(tournament);

        // Act
        var response = await Controller.UpdateTournamentContent(tournamentId, request, CancellationToken.None);

        // Assert
        WebApiControllerTestHelpers.AssertOkValue(response, tournament);
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
