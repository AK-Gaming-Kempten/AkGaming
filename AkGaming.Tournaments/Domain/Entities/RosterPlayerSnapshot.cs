using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class RosterPlayerSnapshot
{
    public Guid Id { get; set; }
    public Guid RosterId { get; set; }
    public Guid? SourcePlayerProfileId { get; set; }
    public PlayerProfileType PlayerProfileType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTimeOffset SourcePlayerProfileLastRevisionUtc { get; set; }
    public DateTimeOffset SnapshotCreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public Roster? Roster { get; set; }
    public PlayerProfile? SourcePlayerProfile { get; set; }

    public bool IsPotentiallyOutdated(PlayerProfile playerProfile)
    {
        ArgumentNullException.ThrowIfNull(playerProfile);

        if (SourcePlayerProfileId != playerProfile.Id)
        {
            throw new InvalidOperationException("The provided player profile does not match this snapshot.");
        }

        return playerProfile.LastRevisionUtc > SourcePlayerProfileLastRevisionUtc;
    }
}
