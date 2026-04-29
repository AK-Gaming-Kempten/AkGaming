using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Application.UseCases;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TournamentContentManagementServiceTests
{
    private InMemoryStore Store { get; set; } = null!;
    private FakeUnitOfWork UnitOfWork { get; set; } = null!;
    private TournamentContentManagementService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new TournamentContentManagementService(new InMemoryTournamentRepository(Store), UnitOfWork);
    }

    [Test]
    [Description("Verifies that updating tournament content persists timeline fields and replaces info sections in order.")]
    public async Task UpdateTournamentContentAsync_ReplacesTimelineAndInfoSections()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var tournament = TournamentTestData.AddTournament(Store);
        tournament.InfoSections.Add(new TournamentInfoSection
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Header = "Old",
            ContentMarkdown = "Old content",
            SortOrder = 0
        });

        var infoSections = new[]
        {
            new TournamentInfoSectionUpdateDto("Format", "Markdown body"),
            new TournamentInfoSectionUpdateDto("Rules", "- One\n- Two")
        };
        var registrationOpenUtc = new DateTimeOffset(2026, 4, 1, 16, 0, 0, TimeSpan.Zero);
        var registrationClosedUtc = new DateTimeOffset(2026, 4, 10, 16, 0, 0, TimeSpan.Zero);
        var startUtc = new DateTimeOffset(2026, 4, 12, 12, 0, 0, TimeSpan.Zero);
        var endUtc = new DateTimeOffset(2026, 4, 13, 12, 0, 0, TimeSpan.Zero);

        // Act
        var result = await Service.UpdateTournamentContentAsync(
            tournament.Id,
            "Updated name",
            TournamentStatusDto.InProgress,
            registrationOpenUtc,
            registrationClosedUtc,
            startUtc,
            endUtc,
            infoSections);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Updated name"));
            Assert.That(result.Status, Is.EqualTo(TournamentStatusDto.InProgress));
            Assert.That(result.RegistrationOpenUtc, Is.EqualTo(registrationOpenUtc));
            Assert.That(result.InfoSections.Select(section => section.Header), Is.EqualTo(new[] { "Format", "Rules" }));
            Assert.That(tournament.InfoSections.Select(section => section.SortOrder), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that updating tournament content rejects timelines where registration closes after the tournament starts.")]
    public void UpdateTournamentContentAsync_RejectsInvalidTimeline()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var tournament = TournamentTestData.AddTournament(Store);

        // Act
        Task Act() => Service.UpdateTournamentContentAsync(
            tournament.Id,
            tournament.Name,
            TournamentStatusDto.RegistrationOpen,
            new DateTimeOffset(2026, 4, 1, 16, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 15, 16, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero),
            null,
            []);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }
}
