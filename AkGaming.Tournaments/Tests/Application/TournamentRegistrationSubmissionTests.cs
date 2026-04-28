using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.RegistrationRules;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TournamentRegistrationSubmissionTests
{
    private InMemoryStore Store { get; set; } = null!;

    private TournamentRegistrationService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        Service = CreateService();
    }

    [Test]
    [Description("Verifies that getting an unknown registration returns null.")]
    public void GetRegistrationAsync_ReturnsNullForUnknownRegistration()
    {
        // Arrange
        var registrationId = Guid.NewGuid();

        // Act
        var registration = Service.GetRegistrationAsync(registrationId).GetAwaiter().GetResult();

        // Assert
        Assert.That(registration, Is.Null);
    }

    [Test]
    [Description("Verifies that listing registrations requires the team to exist.")]
    public void GetTeamRegistrationsAsync_RejectsUnknownTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        Task Act() => Service.GetTeamRegistrationsAsync(teamId);

        // Assert
        Assert.ThrowsAsync<NotFoundException>(Act);
    }

    [Test]
    [Description("Verifies that team registrations are returned newest first for a known team.")]
    public void GetTeamRegistrationsAsync_ReturnsRegistrationsNewestFirst()
    {
        // Arrange
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var older = TournamentTestData.AddRegistration(Store, team, tournament);
        older.SubmittedAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var newerTournament = TournamentTestData.AddTournament(Store, name: "Campus Clash 2");
        var newer = TournamentTestData.AddRegistration(Store, team, newerTournament);
        newer.SubmittedAtUtc = DateTimeOffset.UtcNow;

        // Act
        var registrations = Service.GetTeamRegistrationsAsync(team.Id).GetAwaiter().GetResult();

        // Assert
        Assert.That(registrations.Select(registration => registration.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
    }

    [Test]
    [Description("Verifies that editors, not only owners, can submit registrations for a team.")]
    public void SubmitRegistrationAsync_AllowsEditorsToRegisterTeam()
    {
        // Arrange
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament(includeEditor: true);

        // Act
        var registration = Service.SubmitRegistrationAsync(
            team.Id,
            tournament.Id,
            TournamentTestData.EditorId,
            [memberProfile.Id]).GetAwaiter().GetResult();

        // Assert
        Assert.That(registration.Status, Is.EqualTo(TournamentRegistrationStatusDto.Pending));
    }

    [Test]
    [Description("Verifies that submitting a registration creates a tournament-specific pending registration with an immutable initial roster snapshot.")]
    public void SubmitRegistrationAsync_CreatesPendingRegistrationWithInitialRosterSnapshots()
    {
        // Arrange
        var (team, tournament, memberProfile, guestProfile) = AddRegisterableTeamAndTournament();

        // Act
        var registration = Service.SubmitRegistrationAsync(
            team.Id,
            tournament.Id,
            TournamentTestData.OwnerId,
            [memberProfile.Id, guestProfile.Id]).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(registration.TeamId, Is.EqualTo(team.Id));
            Assert.That(registration.TournamentId, Is.EqualTo(tournament.Id));
            Assert.That(registration.Status, Is.EqualTo(TournamentRegistrationStatusDto.Pending));
            Assert.That(registration.ActiveRosterId, Is.Null);
            Assert.That(registration.Rosters, Has.Count.EqualTo(1));
            Assert.That(registration.Rosters.Single().Version, Is.EqualTo(1));
            Assert.That(registration.Rosters.Single().Status, Is.EqualTo(RosterStatusDto.Pending));
            Assert.That(registration.Rosters.Single().PlayerSnapshots.Select(snapshot => snapshot.Name), Is.EquivalentTo(new[] { "Captain Top", "Guest Support" }));
        });
    }

    [Test]
    [Description("Verifies that a team can only register once for the same tournament.")]
    public void SubmitRegistrationAsync_RejectsDuplicateRegistrationForTournament()
    {
        // Arrange
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament();
        TournamentTestData.AddRegistration(Store, team, tournament);

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [memberProfile.Id]);

        // Assert
        Assert.ThrowsAsync<ConflictException>(Act);
    }

    [Test]
    [Description("Verifies that a submitted roster must contain at least one player profile.")]
    public void SubmitRegistrationAsync_RejectsEmptyRoster()
    {
        // Arrange
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, []);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that guest profiles are available only to the team that owns them.")]
    public void SubmitRegistrationAsync_RejectsGuestProfileOwnedByAnotherTeam()
    {
        // Arrange
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var otherTeam = TournamentTestData.AddTeam(Store, name: "AKG Red");
        var otherGuest = TournamentTestData.AddGuestProfile(Store, otherTeam, "Other Guest");

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [otherGuest.Id]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that every selected player profile id must resolve to an existing profile.")]
    public void SubmitRegistrationAsync_RejectsMissingPlayerProfile()
    {
        // Arrange
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament();

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [memberProfile.Id, Guid.NewGuid()]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that selected player profiles must belong to the same game as the tournament.")]
    public void SubmitRegistrationAsync_RejectsPlayerProfileFromDifferentGame()
    {
        // Arrange
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var wrongGameProfile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.OtherGameId, "Wrong Game");

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [wrongGameProfile.Id]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that regular members cannot submit tournament registrations.")]
    public void SubmitRegistrationAsync_RejectsRegularMemberActor()
    {
        // Arrange
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament(includeMember: true);

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.MemberId, [memberProfile.Id]);

        // Assert
        Assert.ThrowsAsync<ForbiddenException>(Act);
    }

    [Test]
    [Description("Verifies that teams can only register for tournaments in the team's game.")]
    public void SubmitRegistrationAsync_RejectsTournamentForDifferentGame()
    {
        // Arrange
        var (team, _, memberProfile, _) = AddRegisterableTeamAndTournament();
        var otherTournament = TournamentTestData.AddTournament(Store, TournamentTestData.OtherGameId);

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, otherTournament.Id, TournamentTestData.OwnerId, [memberProfile.Id]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that user-backed profiles are available only when the linked user is a team member.")]
    public void SubmitRegistrationAsync_RejectsUserProfileForNonMember()
    {
        // Arrange
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var nonMemberProfile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OtherUserId, TournamentTestData.GameId, "Other User");

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [nonMemberProfile.Id]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that registration submission enforces tournament rank caps.")]
    public void SubmitRegistrationAsync_RejectsRosterAboveRankCaps()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store);
        var tournament = TournamentTestData.AddTournament(
            Store,
            minimumPlayers: 1,
            maximumPlayers: 5,
            maximumPlayerRankRating: 1599,
            maximumTeamAverageRankRating: 1599);
        var profile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.GameId, "Captain Top", rankRating: 1999);

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [profile.Id]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that registration submission enforces tournament average rank caps independently from player rank caps.")]
    public void SubmitRegistrationAsync_RejectsRosterAboveAverageRankCap()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store);
        var tournament = TournamentTestData.AddTournament(
            Store,
            minimumPlayers: 2,
            maximumPlayers: 5,
            maximumTeamAverageRankRating: 1599);
        var silverProfile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.GameId, "Captain Top", rankRating: 1199);
        var diamondProfile = TournamentTestData.AddGuestProfile(Store, team, "Guest Mid", rankRating: 2799);

        // Act
        Task Act() => Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [silverProfile.Id, diamondProfile.Id]);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that registration eligibility returns player and team rule checks before submission.")]
    public void GetRegistrationEligibilityAsync_ReturnsRuleChecksForSelectedRoster()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store);
        var tournament = TournamentTestData.AddTournament(
            Store,
            minimumPlayers: 1,
            maximumPlayers: 5,
            maximumPlayerRankRating: 1599,
            maximumTeamAverageRankRating: 1599);
        var profile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.GameId, "Captain Top", rankRating: 1199);

        // Act
        var eligibility = Service.GetRegistrationEligibilityAsync(
            team.Id,
            tournament.Id,
            TournamentTestData.OwnerId,
            [profile.Id]).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eligibility.CanSubmit, Is.True);
            Assert.That(eligibility.Players.Single().RankName, Is.EqualTo("Silver"));
            Assert.That(eligibility.Players.Single().RankRating, Is.EqualTo(1199));
            Assert.That(eligibility.Rules.Select(rule => rule.Type), Does.Contain("MaxPlayerRankRating"));
            Assert.That(eligibility.Checks.All(check => check.Passed), Is.True);
        });
    }

    [Test]
    [Description("Verifies that registration eligibility reports rank and roster-size failures before submission.")]
    public void GetRegistrationEligibilityAsync_ReturnsFailuresForInvalidSelectedRoster()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store);
        var tournament = TournamentTestData.AddTournament(
            Store,
            minimumPlayers: 2,
            maximumPlayers: 5,
            maximumPlayerRankRating: 1599,
            maximumTeamAverageRankRating: 1599);
        var profile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.GameId, "Captain Top", rankRating: 1999);

        // Act
        var eligibility = Service.GetRegistrationEligibilityAsync(
            team.Id,
            tournament.Id,
            TournamentTestData.OwnerId,
            [profile.Id]).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eligibility.CanSubmit, Is.False);
            Assert.That(eligibility.Players.Single().Qualifies, Is.False);
            Assert.That(eligibility.Checks.Single(check => check.Label == "Minimum players").Passed, Is.False);
            Assert.That(eligibility.Checks.Single(check => check.Label == "Player rank cap").Passed, Is.False);
        });
    }

    private (Team Team, Tournament Tournament, PlayerProfile MemberProfile, PlayerProfile GuestProfile)
        AddRegisterableTeamAndTournament(bool includeEditor = false, bool includeMember = false)
    {
        TournamentTestData.AddGame(Store);
        TournamentTestData.AddGame(Store, TournamentTestData.OtherGameId, "Valorant");
        var memberships = new List<(string UserId, TeamRole Role)> { (TournamentTestData.OwnerId, TeamRole.Owner) };
        if (includeEditor)
        {
            memberships.Add((TournamentTestData.EditorId, TeamRole.Editor));
        }

        if (includeMember)
        {
            memberships.Add((TournamentTestData.MemberId, TeamRole.Member));
        }

        var team = TournamentTestData.AddTeam(Store, memberships: memberships);
        var tournament = TournamentTestData.AddTournament(Store);
        var memberProfile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.GameId, "Captain Top");
        var guestProfile = TournamentTestData.AddGuestProfile(Store, team);
        return (team, tournament, memberProfile, guestProfile);
    }

    private TournamentRegistrationService CreateService()
        => new(
            new InMemoryPlayerProfileRepository(Store),
            new InMemoryTeamRepository(Store),
            new InMemoryTournamentRegistrationRepository(Store),
            new InMemoryTournamentRepository(Store),
            new GameRankSystemRegistry(),
            new FakeUnitOfWork());
}
