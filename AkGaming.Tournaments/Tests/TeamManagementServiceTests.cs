using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests;

public sealed class TeamManagementServiceTests
{
    [Test]
    public void CreateTeamAsync_AssignsCreatorAsOwner()
    {
        var store = new InMemoryStore();
        store.Games.Add(new Game { Id = "lol", Name = "League of Legends" });
        var service = CreateService(store);

        var team = service.CreateTeamAsync("captain-1", "lol", "AKG Blue").GetAwaiter().GetResult();

        Assert.That(team.Memberships, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(team.GameId, Is.EqualTo("lol"));
            Assert.That(team.Name, Is.EqualTo("AKG Blue"));
            Assert.That(team.Memberships[0].UserId, Is.EqualTo("captain-1"));
            Assert.That(team.Memberships[0].Role, Is.EqualTo(TeamRoleDto.Owner));
        });
    }

    [Test]
    public void AddMemberAsync_RejectsNonOwnerMembershipManagement()
    {
        var store = new InMemoryStore();
        store.Teams.Add(new Team
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership { Id = Guid.NewGuid(), UserId = "editor-1", Role = TeamRole.Editor }
            ]
        });

        var service = CreateService(store);

        Assert.ThrowsAsync<ForbiddenException>(() =>
            service.AddMemberAsync(store.Teams[0].Id, "editor-1", "member-1", TeamRoleDto.Member));
    }

    [Test]
    public void GetAvailableProfilesAsync_ReturnsMemberAndGuestProfilesForGame()
    {
        var store = new InMemoryStore();
        store.Games.Add(new Game { Id = "lol", Name = "League of Legends" });
        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership { Id = Guid.NewGuid(), UserId = "captain-1", Role = TeamRole.Owner },
                new TeamMembership { Id = Guid.NewGuid(), UserId = "member-1", Role = TeamRole.Member }
            ]
        };
        var guestProfile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = "lol",
            Name = "Guest Mid",
            Type = PlayerProfileType.Guest
        };
        team.GuestPlayerProfiles.Add(guestProfile);
        store.Teams.Add(team);
        store.PlayerProfiles.Add(guestProfile);
        store.PlayerProfiles.Add(new PlayerProfile
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "Member Jungle",
            Type = PlayerProfileType.User,
            UserId = "member-1"
        });
        store.PlayerProfiles.Add(new PlayerProfile
        {
            Id = Guid.NewGuid(),
            GameId = "valorant",
            Name = "Wrong Game",
            Type = PlayerProfileType.User,
            UserId = "member-1"
        });

        var service = CreateService(store);

        var profiles = service.GetAvailableProfilesAsync(team.Id, "lol").GetAwaiter().GetResult();

        Assert.That(profiles.Select(profile => profile.Name), Is.EquivalentTo(new[] { "Guest Mid", "Member Jungle" }));
    }

    [Test]
    public void CreateGuestPlayerProfileAsync_UsesTeamGame()
    {
        var store = new InMemoryStore();
        store.Games.Add(new Game { Id = "lol", Name = "League of Legends" });
        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership { Id = Guid.NewGuid(), UserId = "captain-1", Role = TeamRole.Owner }
            ]
        };
        store.Teams.Add(team);

        var service = CreateService(store);

        var profile = service.CreateGuestPlayerProfileAsync(team.Id, "captain-1", "Guest Mid").GetAwaiter().GetResult();

        Assert.That(profile.GameId, Is.EqualTo("lol"));
    }

    [Test]
    public void GetAvailableProfilesAsync_RejectsGameThatDoesNotMatchTeam()
    {
        var store = new InMemoryStore();
        store.Games.Add(new Game { Id = "lol", Name = "League of Legends" });
        store.Games.Add(new Game { Id = "valorant", Name = "Valorant" });
        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "AKG Blue",
            Memberships =
            [
                new TeamMembership { Id = Guid.NewGuid(), UserId = "captain-1", Role = TeamRole.Owner }
            ]
        };
        store.Teams.Add(team);

        var service = CreateService(store);

        Assert.ThrowsAsync<ValidationException>(() => service.GetAvailableProfilesAsync(team.Id, "valorant"));
    }

    private static TeamManagementService CreateService(InMemoryStore store)
        => new(
            new InMemoryGameRepository(store),
            new InMemoryPlayerProfileRepository(store),
            new InMemoryTeamRepository(store),
            new FakeUnitOfWork());
}
