using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Domain.Entities;

public sealed class TeamMembership
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Team? Team { get; set; }
}
