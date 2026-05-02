using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TournamentAdministrationServiceTests
{
    private InMemoryStore Store { get; set; } = null!;
    private FakeUnitOfWork UnitOfWork { get; set; } = null!;
    private TournamentAdministrationService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new TournamentAdministrationService(new InMemoryTournamentRepository(Store), new InMemoryGameRepository(Store), UnitOfWork);
    }

    [Test]
    [Description("Verifies that tournament administration lists hidden and visible tournaments for admins.")]
    public async Task GetTournamentsAsync_ReturnsVisibleAndHiddenTournaments()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        TournamentTestData.AddTournament(Store, name: "Visible", slug: "visible", isVisible: true);
        TournamentTestData.AddTournament(Store, name: "Hidden", slug: "hidden", isVisible: false);

        // Act
        var result = await Service.GetTournamentsAsync();

        // Assert
        Assert.That(result.Select(tournament => tournament.Slug), Is.EqualTo(new[] { "hidden", "visible" }));
    }

    [Test]
    [Description("Verifies that administrators can create a hidden draft tournament.")]
    public async Task CreateTournamentAsync_CreatesHiddenDraftTournament()
    {
        // Arrange
        TournamentTestData.AddGame(Store);

        // Act
        var result = await Service.CreateTournamentAsync("spring-showdown", TournamentTestData.GameId, "Spring Showdown", false);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Slug, Is.EqualTo("spring-showdown"));
            Assert.That(result.Name, Is.EqualTo("Spring Showdown"));
            Assert.That(result.IsVisible, Is.False);
            Assert.That(result.Status, Is.EqualTo(AkGaming.Tournaments.Contracts.DTOs.TournamentStatusDto.Draft));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that administrators can update tournament visibility.")]
    public async Task UpdateTournamentVisibilityAsync_UpdatesVisibility()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var tournament = TournamentTestData.AddTournament(Store, isVisible: false);

        // Act
        var result = await Service.UpdateTournamentVisibilityAsync(tournament.Id, true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsVisible, Is.True);
            Assert.That(tournament.IsVisible, Is.True);
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that tournaments with registrations cannot be deleted.")]
    public void DeleteTournamentAsync_RejectsTournamentWithRegistrations()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store);
        var tournament = TournamentTestData.AddTournament(Store);
        TournamentTestData.AddRegistration(Store, team, tournament);

        // Act
        Task Act() => Service.DeleteTournamentAsync(tournament.Id);

        // Assert
        Assert.ThrowsAsync<ConflictException>(Act);
    }
}
