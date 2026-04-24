using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TournamentRegistrationReviewTests
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
    [Description("Verifies that roster snapshots stay immutable and are marked outdated when the source player profile is edited later.")]
    public void GetRegistrationAsync_MarksSnapshotOutdatedAfterProfileRevision()
    {
        var (registration, memberProfile) = AddApprovedRegistration();
        memberProfile.Name = "Captain Renamed";
        memberProfile.LastRevisionUtc = DateTimeOffset.UtcNow.AddMinutes(1);

        var dto = Service.GetRegistrationAsync(registration.Id).GetAwaiter().GetResult();
        var snapshot = dto!.Rosters.Single().PlayerSnapshots.Single(snapshot => snapshot.SourcePlayerProfileId == memberProfile.Id);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Name, Is.EqualTo("Captain Top"));
            Assert.That(snapshot.IsPotentiallyOutdated, Is.True);
        });
    }

    [Test]
    [Description("Verifies that approving a pending registration approves its pending roster and makes that roster active.")]
    public void ReviewRegistrationAsync_ApprovesPendingRegistrationAndActivatesRoster()
    {
        var (registration, _) = AddPendingRegistration();

        var reviewed = Service.ReviewRegistrationAsync(registration.Id, true, " approved ").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(reviewed.Status, Is.EqualTo(TournamentRegistrationStatusDto.Approved));
            Assert.That(reviewed.ReviewNote, Is.EqualTo("approved"));
            Assert.That(reviewed.ActiveRosterId, Is.EqualTo(reviewed.Rosters.Single().Id));
            Assert.That(reviewed.Rosters.Single().Status, Is.EqualTo(RosterStatusDto.Approved));
        });
    }

    [Test]
    [Description("Verifies that only pending registrations can be reviewed.")]
    public void ReviewRegistrationAsync_RejectsNonPendingRegistration()
    {
        var (registration, _) = AddPendingRegistration();
        registration.Status = TournamentRegistrationStatus.Approved;

        Assert.ThrowsAsync<ValidationException>(() => Service.ReviewRegistrationAsync(registration.Id, true, null));
    }

    [Test]
    [Description("Verifies that rejecting a pending registration rejects its roster and does not activate a roster.")]
    public void ReviewRegistrationAsync_RejectsPendingRegistrationWithoutActiveRoster()
    {
        var (registration, _) = AddPendingRegistration();

        var reviewed = Service.ReviewRegistrationAsync(registration.Id, false, " rejected ").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(reviewed.Status, Is.EqualTo(TournamentRegistrationStatusDto.Rejected));
            Assert.That(reviewed.ActiveRosterId, Is.Null);
            Assert.That(reviewed.Rosters.Single().Status, Is.EqualTo(RosterStatusDto.Rejected));
            Assert.That(reviewed.Rosters.Single().ReviewNote, Is.EqualTo("rejected"));
        });
    }

    [Test]
    [Description("Verifies that reviewing a registration requires a pending roster to review.")]
    public void ReviewRegistrationAsync_RejectsRegistrationWithoutPendingRoster()
    {
        var (registration, _) = AddPendingRegistration();
        registration.Rosters.Single().Status = RosterStatus.Approved;

        Assert.ThrowsAsync<ValidationException>(() => Service.ReviewRegistrationAsync(registration.Id, true, null));
    }

    [Test]
    [Description("Verifies that approving a pending roster revision replaces the active roster.")]
    public void ReviewRosterAsync_ApprovesPendingRosterAndReplacesActiveRoster()
    {
        var (registration, memberProfile) = AddApprovedRegistration();
        var replacementGuest = TournamentTestData.AddGuestProfile(Store, registration.Team!, "Guest ADC");
        var pending = Service.SubmitRosterChangeAsync(
            registration.Id,
            TournamentTestData.OwnerId,
            [memberProfile.Id, replacementGuest.Id]).GetAwaiter().GetResult();
        var pendingRosterId = pending.Rosters.Single(roster => roster.Version == 2).Id;

        var reviewed = Service.ReviewRosterAsync(registration.Id, pendingRosterId, true, "approved change").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(reviewed.ActiveRosterId, Is.EqualTo(pendingRosterId));
            Assert.That(reviewed.Rosters.Single(roster => roster.Version == 2).Status, Is.EqualTo(RosterStatusDto.Approved));
        });
    }

    [Test]
    [Description("Verifies that rejecting a pending roster revision leaves the previous active roster unchanged.")]
    public void ReviewRosterAsync_RejectsPendingRosterAndKeepsPreviousActiveRoster()
    {
        var (registration, memberProfile) = AddApprovedRegistration();
        var previousActiveRosterId = registration.ActiveRosterId;
        var replacementGuest = TournamentTestData.AddGuestProfile(Store, registration.Team!, "Guest ADC");
        var pending = Service.SubmitRosterChangeAsync(
            registration.Id,
            TournamentTestData.OwnerId,
            [memberProfile.Id, replacementGuest.Id]).GetAwaiter().GetResult();
        var pendingRosterId = pending.Rosters.Single(roster => roster.Version == 2).Id;

        var reviewed = Service.ReviewRosterAsync(registration.Id, pendingRosterId, false, "rejected change").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(reviewed.ActiveRosterId, Is.EqualTo(previousActiveRosterId));
            Assert.That(reviewed.Rosters.Single(roster => roster.Version == 2).Status, Is.EqualTo(RosterStatusDto.Rejected));
        });
    }

    [Test]
    [Description("Verifies that roster reviews require an approved registration.")]
    public void ReviewRosterAsync_RejectsRegistrationThatIsNotApproved()
    {
        var (registration, _) = AddPendingRegistration();
        var rosterId = registration.Rosters.Single().Id;

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.ReviewRosterAsync(registration.Id, rosterId, true, null));
    }

    [Test]
    [Description("Verifies that already reviewed rosters cannot be reviewed again.")]
    public void ReviewRosterAsync_RejectsRosterThatIsNotPending()
    {
        var (registration, _) = AddApprovedRegistration();
        var rosterId = registration.ActiveRosterId!.Value;

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.ReviewRosterAsync(registration.Id, rosterId, true, null));
    }

    [Test]
    [Description("Verifies that roster reviews fail for an unknown roster id.")]
    public void ReviewRosterAsync_RejectsUnknownRoster()
    {
        var (registration, _) = AddApprovedRegistration();

        Assert.ThrowsAsync<NotFoundException>(() =>
            Service.ReviewRosterAsync(registration.Id, Guid.NewGuid(), true, null));
    }

    [Test]
    [Description("Verifies that roster changes are submitted as a new pending roster revision while the approved roster remains active.")]
    public void SubmitRosterChangeAsync_CreatesPendingRevisionAndKeepsActiveRoster()
    {
        var (registration, memberProfile) = AddApprovedRegistration();
        var replacementGuest = TournamentTestData.AddGuestProfile(Store, registration.Team!, "Guest ADC");

        var updated = Service.SubmitRosterChangeAsync(
            registration.Id,
            TournamentTestData.OwnerId,
            [memberProfile.Id, replacementGuest.Id]).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(updated.ActiveRosterId, Is.EqualTo(registration.ActiveRosterId));
            Assert.That(updated.Rosters, Has.Count.EqualTo(2));
            Assert.That(updated.Rosters.Single(roster => roster.Version == 2).Status, Is.EqualTo(RosterStatusDto.Pending));
        });
    }

    [Test]
    [Description("Verifies that roster changes can only be submitted for approved registrations.")]
    public void SubmitRosterChangeAsync_RejectsRegistrationThatIsNotApproved()
    {
        var (registration, memberProfile) = AddPendingRegistration();

        Assert.ThrowsAsync<ValidationException>(() =>
            Service.SubmitRosterChangeAsync(registration.Id, TournamentTestData.OwnerId, [memberProfile.Id]));
    }

    [Test]
    [Description("Verifies that only one pending roster change can exist for an approved registration.")]
    public void SubmitRosterChangeAsync_RejectsWhenPendingRosterAlreadyExists()
    {
        var (registration, memberProfile) = AddApprovedRegistration();
        var pendingRoster = new Roster
        {
            Id = Guid.NewGuid(),
            TournamentRegistrationId = registration.Id,
            Version = 2,
            Status = RosterStatus.Pending
        };
        registration.Rosters.Add(pendingRoster);

        Assert.ThrowsAsync<ConflictException>(() =>
            Service.SubmitRosterChangeAsync(registration.Id, TournamentTestData.OwnerId, [memberProfile.Id]));
    }

    private (TournamentRegistration Registration, PlayerProfile MemberProfile) AddPendingRegistration()
    {
        var (team, tournament, memberProfile, guestProfile) = AddTeamTournamentAndProfiles();
        var registration = TournamentTestData.AddRegistration(
            Store,
            team,
            tournament,
            TournamentRegistrationStatus.Pending,
            RosterStatus.Pending,
            profiles: [memberProfile, guestProfile]);

        return (registration, memberProfile);
    }


    private (TournamentRegistration Registration, PlayerProfile MemberProfile) AddApprovedRegistration()
    {
        var (team, tournament, memberProfile, guestProfile) = AddTeamTournamentAndProfiles();
        var registration = TournamentTestData.AddRegistration(
            Store,
            team,
            tournament,
            TournamentRegistrationStatus.Approved,
            RosterStatus.Approved,
            profiles: [memberProfile, guestProfile]);

        return (registration, memberProfile);
    }


    private (Team Team, Tournament Tournament, PlayerProfile MemberProfile, PlayerProfile GuestProfile) AddTeamTournamentAndProfiles()
    {
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store);
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
