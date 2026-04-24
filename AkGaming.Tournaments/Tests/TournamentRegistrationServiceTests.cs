using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests;

public sealed class TournamentRegistrationServiceTests
{
    [Test]
    public void SubmitRegistrationAndApproveRoster_KeepsActiveRosterUntilNewOneIsApproved()
    {
        var store = new InMemoryStore();
        store.Games.Add(new Game { Id = "lol", Name = "League of Legends" });
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership { Id = Guid.NewGuid(), UserId = "captain-1", Role = TeamRole.Owner }
            ]
        };
        var memberProfile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "Captain Top",
            Type = PlayerProfileType.User,
            UserId = "captain-1"
        };
        var guestProfile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = "lol",
            Name = "Guest Support",
            Type = PlayerProfileType.Guest
        };
        team.GuestPlayerProfiles.Add(guestProfile);
        store.Teams.Add(team);
        store.PlayerProfiles.Add(memberProfile);
        store.PlayerProfiles.Add(guestProfile);
        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = "Campus Clash",
            GameId = "lol",
            Status = TournamentStatus.RegistrationOpen
        };
        store.Tournaments.Add(tournament);

        var service = CreateService(store);

        var submitted = service.SubmitRegistrationAsync(team.Id, tournament.Id, "captain-1", [memberProfile.Id, guestProfile.Id]).GetAwaiter().GetResult();
        var approved = service.ReviewRegistrationAsync(submitted.Id, true, "approved").GetAwaiter().GetResult();

        var replacementGuest = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = "lol",
            Name = "Guest ADC",
            Type = PlayerProfileType.Guest
        };
        team.GuestPlayerProfiles.Add(replacementGuest);
        store.PlayerProfiles.Add(replacementGuest);

        var pendingChange = service.SubmitRosterChangeAsync(approved.Id, "captain-1", [memberProfile.Id, replacementGuest.Id]).GetAwaiter().GetResult();

        Assert.That(pendingChange.ActiveRosterId, Is.EqualTo(approved.ActiveRosterId));
        Assert.That(pendingChange.Rosters, Has.Count.EqualTo(2));
        Assert.That(pendingChange.Rosters.Single(roster => roster.Version == 2).Status, Is.EqualTo(Contracts.DTOs.RosterStatusDto.Pending));

        var reviewed = service.ReviewRosterAsync(approved.Id, Guid.Parse(pendingChange.Rosters.Single(roster => roster.Version == 2).Id.ToString()), true, "approved change").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(reviewed.ActiveRosterId, Is.EqualTo(pendingChange.Rosters.Single(roster => roster.Version == 2).Id));
            Assert.That(reviewed.Rosters.Single(roster => roster.Version == 2).Status, Is.EqualTo(Contracts.DTOs.RosterStatusDto.Approved));
        });
    }

    private static TournamentRegistrationService CreateService(InMemoryStore store)
        => new(
            new InMemoryPlayerProfileRepository(store),
            new InMemoryTeamRepository(store),
            new InMemoryTournamentRegistrationRepository(store),
            new InMemoryTournamentRepository(store),
            new FakeUnitOfWork());
}
