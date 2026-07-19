using System.Collections.Concurrent;

namespace AkGaming.Management.Modules.GeneralMeetings.Api.Realtime;

public sealed class MeetingPresenceTracker
{
    private readonly ConcurrentDictionary<(Guid MeetingId, Guid UserId), ConcurrentDictionary<string, byte>> _connections = new();
    public bool Add(Guid meetingId, Guid userId, string connectionId) { var set = _connections.GetOrAdd((meetingId, userId), _ => new()); set[connectionId] = 0; return set.Count == 1; }
    public bool Remove(Guid meetingId, Guid userId, string connectionId) { if (!_connections.TryGetValue((meetingId, userId), out var set)) return false; set.TryRemove(connectionId, out _); if (!set.IsEmpty) return false; _connections.TryRemove((meetingId, userId), out _); return true; }
    public bool IsOnline(Guid meetingId, Guid? userId) => userId.HasValue && _connections.ContainsKey((meetingId, userId.Value));
}
