using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Tests.Domain;

public sealed class RosterPlayerSnapshotTests
{
    [Test]
    [Description("Verifies that outdated checks reject a player profile that does not match the snapshot source profile id.")]
    public void IsPotentiallyOutdated_RejectsMismatchedProfile()
    {
        // Arrange
        var profile = CreateProfile(DateTimeOffset.UtcNow);
        var snapshot = CreateSnapshot(profile);
        var otherProfile = CreateProfile(DateTimeOffset.UtcNow);

        // Act
        void Act() => snapshot.IsPotentiallyOutdated(otherProfile);

        // Assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Test]
    [Description("Verifies that a roster snapshot is not outdated when the source player profile has not been revised after the snapshot was created.")]
    public void IsPotentiallyOutdated_ReturnsFalseWhenSourceRevisionMatchesSnapshot()
    {
        // Arrange
        var profile = CreateProfile(DateTimeOffset.UtcNow);
        var snapshot = CreateSnapshot(profile);

        // Act
        var isOutdated = snapshot.IsPotentiallyOutdated(profile);

        // Assert
        Assert.That(isOutdated, Is.False);
    }

    [Test]
    [Description("Verifies that a roster snapshot is marked outdated when the source player profile was revised after the snapshot was created.")]
    public void IsPotentiallyOutdated_ReturnsTrueWhenSourceWasRevised()
    {
        // Arrange
        var profile = CreateProfile(DateTimeOffset.UtcNow);
        var snapshot = CreateSnapshot(profile);
        profile.LastRevisionUtc = profile.LastRevisionUtc.AddMinutes(1);

        // Act
        var isOutdated = snapshot.IsPotentiallyOutdated(profile);

        // Assert
        Assert.That(isOutdated, Is.True);
    }

    private static PlayerProfile CreateProfile(DateTimeOffset lastRevisionUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            GameId = "lol",
            Name = "Captain Top",
            Type = PlayerProfileType.User,
            UserId = "captain-1",
            LastRevisionUtc = lastRevisionUtc
        };


    private static RosterPlayerSnapshot CreateSnapshot(PlayerProfile profile)
        => new()
        {
            Id = Guid.NewGuid(),
            SourcePlayerProfileId = profile.Id,
            PlayerProfileType = profile.Type,
            Name = profile.Name,
            UserId = profile.UserId,
            SourcePlayerProfileLastRevisionUtc = profile.LastRevisionUtc,
            SnapshotCreatedUtc = DateTimeOffset.UtcNow
        };
}
