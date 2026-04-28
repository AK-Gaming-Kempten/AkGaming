using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TournamentCatalogServiceTests
{
    private InMemoryStore Store { get; set; } = null!;
    private TournamentCatalogService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        Service = new TournamentCatalogService(new InMemoryTournamentRepository(Store));
    }

    [Test]
    [Description("Verifies that loading a tournament by slug returns its persisted timeline and info sections.")]
    public async Task GetTournamentBySlugAsync_ReturnsPersistedTournamentContent()
    {
        // Arrange
        var game = TournamentTestData.AddGame(Store);
        var tournament = TournamentTestData.AddTournament(Store, name: "Rift Rumble", slug: "rift-rumble");
        tournament.Game = game;
        tournament.RegistrationOpenUtc = new DateTimeOffset(2026, 4, 1, 16, 0, 0, TimeSpan.Zero);
        tournament.InfoSections.Add(new TournamentInfoSection
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Header = "Overview",
            ContentMarkdown = "Public content",
            SortOrder = 0
        });

        // Act
        var result = await Service.GetTournamentBySlugAsync("rift-rumble");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Rift Rumble"));
            Assert.That(result.GameName, Is.EqualTo(game.Name));
            Assert.That(result.RegistrationOpenUtc, Is.EqualTo(tournament.RegistrationOpenUtc));
            Assert.That(result.InfoSections.Select(section => section.Header), Is.EqualTo(new[] { "Overview" }));
        });
    }
}
