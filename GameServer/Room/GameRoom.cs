using SharedProtocol;

namespace GameServerApp.Rooms;

/// <summary>
/// Represents a logical game room. Players in the same room share chats, world events, etc.
/// This is a lightweight orchestration layer separate from world/chunk management.
/// </summary>
public class GameRoom
{
    public string RoomId { get; }
    public int WorldId { get; }

    // Player names mapped to join time
    private readonly Dictionary<string, DateTime> _members = new();

    public GameRoom(string roomId, int worldId)
    {
        RoomId = roomId;
        WorldId = worldId;
    }

    public bool Add(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return false;
        _members[userName] = DateTime.UtcNow;
        return true;
    }

    public bool Remove(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return false;
        return _members.Remove(userName);
    }

    public bool Contains(string userName) => !string.IsNullOrWhiteSpace(userName) && _members.ContainsKey(userName);

    public IReadOnlyCollection<string> Members => _members.Keys.ToList();
}

