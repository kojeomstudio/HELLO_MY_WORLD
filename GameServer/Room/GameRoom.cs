using System;
using System.Collections.Generic;
using System.Linq;
using SharedProtocol;

namespace GameServerApp.Rooms;

/// <summary>
/// Represents a logical game room. Players in the same room share chats, world events, etc.
/// This is a lightweight orchestration layer separate from world/chunk management.
/// </summary>
public class GameRoom
{
    public string RoomId { get; }
    public string DisplayName { get; }
    public int WorldId { get; }
    public int MaxPlayers { get; }
    public bool IsLobby { get; }

    // Player names mapped to join time
    private readonly Dictionary<string, DateTime> _members = new();

    public GameRoom(string roomId, int worldId, string displayName, int maxPlayers = 0, bool isLobby = false)
    {
        RoomId = roomId;
        WorldId = worldId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? roomId : displayName;
        MaxPlayers = Math.Max(0, maxPlayers);
        IsLobby = isLobby;
    }

    public bool Add(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return false;
        if (MaxPlayers > 0 && _members.Count >= MaxPlayers) return false;
        _members[userName] = DateTime.UtcNow;
        return true;
    }

    public bool Remove(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return false;
        return _members.Remove(userName);
    }

    public bool Contains(string userName) => !string.IsNullOrWhiteSpace(userName) && _members.ContainsKey(userName);

    public IReadOnlyCollection<string> Members => _members.Keys;

    public int PlayerCount => _members.Count;

    public RoomInfo ToRoomInfo()
    {
        return new RoomInfo
        {
            RoomId = RoomId,
            DisplayName = DisplayName,
            WorldId = WorldId,
            PlayerCount = PlayerCount,
            Capacity = MaxPlayers,
            IsLobby = IsLobby
        };
    }

    public List<string> GetMemberSnapshot()
    {
        return _members.Keys.ToList();
    }
}
