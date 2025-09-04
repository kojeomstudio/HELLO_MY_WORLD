using SharedProtocol;

namespace GameServerApp.Rooms;

/// <summary>
/// Central registry for game rooms and player membership.
/// Provides room-scoped broadcast helpers leveraging SessionManager.
/// </summary>
public class RoomManager
{
    private readonly SessionManager _sessions;
    private readonly Dictionary<string, GameRoom> _rooms = new();
    private readonly Dictionary<string, string> _playerRoom = new(); // player -> roomId

    public RoomManager(SessionManager sessions)
    {
        _sessions = sessions;
        // Ensure there is always a default room bound to default world
        CreateRoom("lobby", worldId: 1);
    }

    public bool CreateRoom(string roomId, int worldId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return false;
        if (_rooms.ContainsKey(roomId)) return false;
        _rooms[roomId] = new GameRoom(roomId, worldId);
        return true;
    }

    public bool RemoveRoom(string roomId)
    {
        if (!_rooms.ContainsKey(roomId)) return false;
        // Evict members
        var members = _rooms[roomId].Members.ToList();
        foreach (var m in members) _playerRoom.Remove(m);
        return _rooms.Remove(roomId);
    }

    public bool AssignPlayerToRoom(string userName, string roomId)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(roomId)) return false;
        if (!_rooms.TryGetValue(roomId, out var room)) return false;

        // Remove from current room (if any)
        if (_playerRoom.TryGetValue(userName, out var current))
        {
            if (_rooms.TryGetValue(current, out var prev)) prev.Remove(userName);
        }

        room.Add(userName);
        _playerRoom[userName] = roomId;
        return true;
    }

    public void RemovePlayer(string userName)
    {
        if (_playerRoom.TryGetValue(userName, out var roomId))
        {
            if (_rooms.TryGetValue(roomId, out var room)) room.Remove(userName);
            _playerRoom.Remove(userName);
        }
    }

    public string? GetPlayerRoomId(string userName)
    {
        return _playerRoom.TryGetValue(userName, out var roomId) ? roomId : null;
    }

    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    /// <summary>
    /// Broadcasts a message to all members of a room.
    /// </summary>
    public async Task BroadcastToRoomAsync<T>(string roomId, MessageType type, T message) where T : class
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;
        var tasks = new List<Task>();
        foreach (var name in room.Members)
        {
            var session = _sessions.GetSession(name);
            if (session != null) tasks.Add(session.SendAsync(type, message));
        }
        await Task.WhenAll(tasks);
    }
}

