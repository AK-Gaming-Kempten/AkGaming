using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests;

public sealed class PlayerProfileManagementServiceTests
{
    [Test]
    public void UpsertUserProfileAsync_CreatesAndThenUpdatesSingleProfilePerGame()
    {
        var store = new InMemoryStore();
        store.Games.Add(new Game { Id = "lol", Name = "League of Legends" });

        var service = new PlayerProfileManagementService(
            new InMemoryGameRepository(store),
            new InMemoryPlayerProfileRepository(store),
            new FakeUnitOfWork());

        var created = service.UpsertUserProfileAsync("user-1", "lol", "Summoner One").GetAwaiter().GetResult();
        var updated = service.UpsertUserProfileAsync("user-1", "lol", "Summoner Prime").GetAwaiter().GetResult();

        Assert.That(store.PlayerProfiles, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(created.Type, Is.EqualTo(Contracts.DTOs.PlayerProfileTypeDto.User));
            Assert.That(updated.Id, Is.EqualTo(created.Id));
            Assert.That(updated.Name, Is.EqualTo("Summoner Prime"));
            Assert.That(store.PlayerProfiles[0].Type, Is.EqualTo(PlayerProfileType.User));
            Assert.That(store.PlayerProfiles[0].TeamId, Is.Null);
        });
    }
}
