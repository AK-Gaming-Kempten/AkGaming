using AkGaming.Tournaments.Application.Exceptions;
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
        var registration = Service.GetRegistrationAsync(Guid.NewGuid()).GetAwaiter().GetResult();

        Assert.That(registration, Is.Null);
    }

    [Test]
    [Description("Verifies that listing registrations requires the team to exist.")]
    public void GetTeamRegistrationsAsync_RejectsUnknownTeam()
    {
        Assert.ThrowsAsync<NotFoundException>(() => Service.GetTeamRegistrationsAsync(Guid.NewGuid()));
    }

    [Test]
    [Description("Verifies that team registrations are returned newest first for a known team.")]
    public void GetTeamRegistrationsAsync_ReturnsRegistrationsNewestFirst()
    {
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var older = TournamentTestData.AddRegistration(Store, team, tournament);
        older.SubmittedAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var newerTournament = TournamentTestData.AddTournament(Store, name: "Campus Clash 2");
        var newer = TournamentTestData.AddRegistration(Store, team, newerTournament);
        newer.SubmittedAtUtc = DateTimeOffset.UtcNow;

        var registrations = Service.GetTeamRegistrationsAsync(team.Id).GetAwaiter().GetResult();

        Assert.That(registrations.Select(registration => registration.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
    }

    [Test]
    [Description("Verifies that editors, not only owners, can submit registrations for a team.")]
    public void SubmitRegistrationAsync_AllowsEditorsToRegisterTeam()
    {
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament(includeEditor: true);

        var registration = Service.SubmitRegistrationAsync(
            team.Id,
            tournament.Id,
            TournamentTestData.EditorId,
            [memberProfile.Id]).GetAwaiter().GetResult();

        Assert.That(registration.Status, Is.EqualTo(TournamentRegistrationStatusDto.Pending));
    }

    [Test]
    [Description("Verifies that submitting a registration creates a tournament-specific pending registration with an immutable initial roster snapshot.")]
    public void SubmitRegistrationAsync_CreatesPendingRegistrationWithInitialRosterSnapshots()
    {
        var (team, tournament, memberProfile, guestProfile) = AddRegisterableTeamAndTournament();

        var registration = Service.SubmitRegistrationAsync(
            team.Id,
            tournament.Id,
            TournamentTestData.OwnerId,
            [memberProfile.Id, guestProfile.Id]).GetAwaiter().GetResult();

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
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament();
        TournamentTestData.AddRegistration(Store, team, tournament);

        Assert.ThrowsAsync<ConflictException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [memberProfile.Id]));
    }

    [Test]
    [Description("Verifies that a submitted roster must contain at least one player profile.")]
    public void SubmitRegistrationAsync_RejectsEmptyRoster()
    {
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, []));
    }

    [Test]
    [Description("Verifies that guest profiles are available only to the team that owns them.")]
    public void SubmitRegistrationAsync_RejectsGuestProfileOwnedByAnotherTeam()
    {
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var otherTeam = TournamentTestData.AddTeam(Store, name: "AKG Red");
        var otherGuest = TournamentTestData.AddGuestProfile(Store, otherTeam, "Other Guest");

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [otherGuest.Id]));
    }

    [Test]
    [Description("Verifies that every selected player profile id must resolve to an existing profile.")]
    public void SubmitRegistrationAsync_RejectsMissingPlayerProfile()
    {
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament();

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [memberProfile.Id, Guid.NewGuid()]));
    }

    [Test]
    [Description("Verifies that selected player profiles must belong to the same game as the tournament.")]
    public void SubmitRegistrationAsync_RejectsPlayerProfileFromDifferentGame()
    {
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var wrongGameProfile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OwnerId, TournamentTestData.OtherGameId, "Wrong Game");

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [wrongGameProfile.Id]));
    }

    [Test]
    [Description("Verifies that regular members cannot submit tournament registrations.")]
    public void SubmitRegistrationAsync_RejectsRegularMemberActor()
    {
        var (team, tournament, memberProfile, _) = AddRegisterableTeamAndTournament(includeMember: true);

        Assert.ThrowsAsync<ForbiddenException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.MemberId, [memberProfile.Id]));
    }

    [Test]
    [Description("Verifies that teams can only register for tournaments in the team's game.")]
    public void SubmitRegistrationAsync_RejectsTournamentForDifferentGame()
    {
        var (team, _, memberProfile, _) = AddRegisterableTeamAndTournament();
        var otherTournament = TournamentTestData.AddTournament(Store, TournamentTestData.OtherGameId);

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRegistrationAsync(team.Id, otherTournament.Id, TournamentTestData.OwnerId, [memberProfile.Id]));
    }

    [Test]
    [Description("Verifies that user-backed profiles are available only when the linked user is a team member.")]
    public void SubmitRegistrationAsync_RejectsUserProfileForNonMember()
    {
        var (team, tournament, _, _) = AddRegisterableTeamAndTournament();
        var nonMemberProfile = TournamentTestData.AddUserProfile(Store, TournamentTestData.OtherUserId, TournamentTestData.GameId, "Other User");

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRegistrationAsync(team.Id, tournament.Id, TournamentTestData.OwnerId, [nonMemberProfile.Id]));
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
            new FakeUnitOfWork());
}
