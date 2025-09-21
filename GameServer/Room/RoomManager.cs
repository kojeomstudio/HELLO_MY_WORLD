using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServerApp;
using SharedProtocol;

namespace GameServerApp.Rooms;

/// <summary>
/// Central registry for game rooms and player membership with lobby-aware queueing support.
/// </summary>
public class RoomManager
{
    public const string DefaultLobbyId = "lobby";

    private readonly SessionManager _sessions;
    private readonly Dictionary<string, GameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _playerRoom = new(StringComparer.OrdinalIgnoreCase);

    public RoomManager(SessionManager sessions)
    {
        _sessions = sessions;
        CreateRoom(DefaultLobbyId, 1, "Lobby", maxPlayers: 0, isLobby: true, lobbyId: DefaultLobbyId, visibility: RoomVisibility.Public);
    }

    public bool CreateRoom(
        string roomId,
        int worldId,
        string? displayName = null,
        int maxPlayers = 0,
        bool isLobby = false,
        string lobbyId = DefaultLobbyId,
        string gameMode = "default",
        RoomVisibility visibility = RoomVisibility.Public,
        string? password = null)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return false;
        if (_rooms.ContainsKey(roomId)) return false;

        var room = new GameRoom(roomId, worldId, displayName ?? roomId, maxPlayers, isLobby, lobbyId, gameMode, visibility, password);
        _rooms[roomId] = room;
        return true;
    }

    public bool RemoveRoom(string roomId)
    {
        if (!_rooms.Remove(roomId, out var room)) return false;

        foreach (var member in room.GetMemberSnapshot())
        {
            _playerRoom.Remove(member);
        }

        foreach (var info in room.GetMemberInfoSnapshot().Where(i => i.IsQueued))
        {
            _playerRoom.Remove(info.UserName);
        }

        return true;
    }

    public RoomAssignmentResult TryAssignPlayerToRoom(string userName, string roomId, RoomJoinOptions? joinOptions = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RoomAssignmentResult.Fail("잘못된 사용자 이름입니다.");
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return RoomAssignmentResult.Fail("잘못된 방 ID 입니다.");
        }

        if (!_rooms.TryGetValue(roomId, out var targetRoom))
        {
            return RoomAssignmentResult.Fail("존재하지 않는 방입니다.");
        }

        joinOptions ??= RoomJoinOptions.Default;

        string? previousRoomId = null;
        GameRoom? previousRoom = null;

        if (_playerRoom.TryGetValue(userName, out var currentRoomId))
        {
            if (string.Equals(currentRoomId, roomId, StringComparison.OrdinalIgnoreCase))
            {
                targetRoom.TryGetMember(userName, out var existingMember);
                return RoomAssignmentResult.FromExisting(targetRoom, existingMember);
            }

            previousRoomId = currentRoomId;
            _rooms.TryGetValue(currentRoomId, out previousRoom);
        }

        var joinResult = targetRoom.TryJoin(userName, joinOptions);
        if (!joinResult.Success)
        {
            return RoomAssignmentResult.Fail(joinResult.FailureReason);
        }

        _playerRoom[userName] = roomId;

        if (previousRoom != null && !string.Equals(previousRoomId, roomId, StringComparison.OrdinalIgnoreCase))
        {
            if (previousRoom.TryRemoveMember(userName, out var removedFromActive))
            {
                _ = HandleQueuePromotion(previousRoom, removedFromActive);
            }
        }

        _ = HandleQueuePromotion(targetRoom, seatFreed: false);

        return RoomAssignmentResult.FromJoin(targetRoom, joinResult);
    }

    public bool AssignPlayerToRoom(string userName, string roomId)
    {
        var result = TryAssignPlayerToRoom(userName, roomId, new RoomJoinOptions { AllowQueue = false });
        return result.Success && !result.Queued;
    }

    public RoomAssignmentResult AutoAssign(string userName, string lobbyId, RoomJoinOptions? options = null)
    {
        var candidates = _rooms.Values
            .Where(r => string.Equals(r.LobbyId, lobbyId, StringComparison.OrdinalIgnoreCase))
            .Where(r => r.Visibility != RoomVisibility.Private)
            .OrderBy(r => r.QueueCount)
            .ThenBy(r => r.ActivePlayerCount)
            .ToList();

        foreach (var room in candidates)
        {
            var clonedOptions = options == null
                ? RoomJoinOptions.Default
                : new RoomJoinOptions
                {
                    AllowQueue = options.AllowQueue,
                    PreferredRole = options.PreferredRole,
                    JoinAsSpectator = options.JoinAsSpectator,
                    Password = options.Password
                };

            var result = TryAssignPlayerToRoom(userName, room.RoomId, clonedOptions);
            if (result.Success)
            {
                return result;
            }
        }

        return RoomAssignmentResult.Fail("참여 가능한 방이 없습니다.");
    }

    public RoomRemovalResult RemovePlayer(string userName)
    {
        if (!_playerRoom.TryGetValue(userName, out var roomId))
        {
            return RoomRemovalResult.Fail();
        }

        if (!_rooms.TryGetValue(roomId, out var room))
        {
            _playerRoom.Remove(userName);
            return RoomRemovalResult.Fail();
        }

        if (!room.TryRemoveMember(userName, out var removedFromActive))
        {
            return RoomRemovalResult.Fail();
        }

        _playerRoom.Remove(userName);
        var promoted = HandleQueuePromotion(room, removedFromActive);

        return RoomRemovalResult.Successful(roomId, room, removedFromActive, promoted);
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

    public IEnumerable<GameRoom> GetRooms()
    {
        return _rooms.Values;
    }

    public IReadOnlyCollection<string> GetMembers(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room.GetMemberSnapshot();
        }

        return Array.Empty<string>();
    }

    public List<RoomMemberInfo> GetMemberInfos(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            return room.GetMemberInfoSnapshot();
        }

        return new List<RoomMemberInfo>();
    }

    public IEnumerable<LobbySummary> GetLobbySummaries()
    {
        return _rooms.Values
            .GroupBy(r => r.LobbyId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LobbySummary
            {
                LobbyId = group.Key,
                DisplayName = group.FirstOrDefault(r => r.IsLobby)?.DisplayName ?? group.Key,
                RoomCount = group.Count(),
                PlayerCount = group.Sum(r => r.ActivePlayerCount + r.SpectatorCount),
                QueueCount = group.Sum(r => r.QueueCount),
                ActiveRooms = group.Count(r => r.ActivePlayerCount > 0)
            })
            .ToList();
    }

    public async Task BroadcastToRoomAsync<T>(string roomId, MessageType type, T message) where T : class
    {
        if (!_rooms.TryGetValue(roomId, out var room)) return;

        var recipients = room.GetMemberSnapshot();
        var tasks = new List<Task>(recipients.Count);
        foreach (var name in recipients)
        {
            var session = _sessions.GetSession(name);
            if (session != null)
            {
                tasks.Add(session.SendAsync(type, message));
            }
        }

        await Task.WhenAll(tasks);
    }

    private RoomMember? HandleQueuePromotion(GameRoom room, bool seatFreed)
    {
        RoomMember? promoted = null;
        if (seatFreed)
        {
            promoted = room.PromoteNextFromQueue();
            if (promoted != null)
            {
                _playerRoom[promoted.UserName] = room.RoomId;

                var promotion = new RoomPromotionMessage
                {
                    RoomId = room.RoomId,
                    IsNowActive = true,
                    Member = room.BuildMemberInfo(promoted),
                    Room = room.ToRoomInfo()
                };

                var session = _sessions.GetSession(promoted.UserName);
                if (session != null)
                {
                    _ = session.SendAsync(MessageType.RoomPromotionNotice, promotion);
                }
            }
        }

        _ = BroadcastQueueUpdateAsync(room);
        return promoted;
    }

    private async Task BroadcastQueueUpdateAsync(GameRoom room)
    {
        var memberInfos = room.GetMemberInfoSnapshot();
        var queueEntries = memberInfos
            .Where(info => info.IsQueued)
            .Select(info => new RoomQueueEntry
            {
                UserName = info.UserName,
                Position = info.QueuePosition,
                EstimatedWaitMs = Math.Max(0, info.QueuePosition - 1) * 15000L
            })
            .ToList();

        var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in room.GetMemberSnapshot())
        {
            recipients.Add(member);
        }

        foreach (var entry in queueEntries)
        {
            recipients.Add(entry.UserName);
        }

        if (recipients.Count == 0)
        {
            return;
        }

        var update = new RoomQueueUpdateMessage
        {
            RoomId = room.RoomId,
            Queue = queueEntries
        };

        var tasks = new List<Task>(recipients.Count);
        foreach (var name in recipients)
        {
            var session = _sessions.GetSession(name);
            if (session != null)
            {
                tasks.Add(session.SendAsync(MessageType.RoomQueueUpdate, update));
            }
        }

        await Task.WhenAll(tasks);
    }
}

public class RoomAssignmentResult
{
    public bool Success { get; private set; }
    public bool Queued { get; private set; }
    public int QueuePosition { get; private set; }
    public GameRoom? Room { get; private set; }
    public RoomMember? Member { get; private set; }
    public string FailureReason { get; private set; } = string.Empty;
    public bool AlreadyInRoom { get; private set; }

    public static RoomAssignmentResult FromJoin(GameRoom room, RoomJoinResult join)
    {
        return new RoomAssignmentResult
        {
            Success = true,
            Queued = join.Queued,
            QueuePosition = join.QueuePosition,
            Room = room,
            Member = join.Member,
            AlreadyInRoom = false
        };
    }

    public static RoomAssignmentResult FromExisting(GameRoom room, RoomMember? member)
    {
        return new RoomAssignmentResult
        {
            Success = true,
            Queued = member?.IsQueued ?? false,
            QueuePosition = member?.QueuePosition ?? 0,
            Room = room,
            Member = member,
            AlreadyInRoom = true
        };
    }

    public static RoomAssignmentResult Fail(string reason)
    {
        return new RoomAssignmentResult
        {
            Success = false,
            Queued = false,
            QueuePosition = -1,
            Room = null,
            Member = null,
            FailureReason = reason,
            AlreadyInRoom = false
        };
    }
}

public class RoomRemovalResult
{
    public bool Success { get; private set; }
    public string RoomId { get; private set; } = string.Empty;
    public bool WasActiveMember { get; private set; }
    public GameRoom? Room { get; private set; }
    public RoomMember? PromotedMember { get; private set; }

    public static RoomRemovalResult Successful(string roomId, GameRoom room, bool wasActive, RoomMember? promoted)
    {
        return new RoomRemovalResult
        {
            Success = true,
            RoomId = roomId,
            WasActiveMember = wasActive,
            Room = room,
            PromotedMember = promoted
        };
    }

    public static RoomRemovalResult Fail()
    {
        return new RoomRemovalResult
        {
            Success = false,
            RoomId = string.Empty,
            WasActiveMember = false,
            Room = null,
            PromotedMember = null
        };
    }
}
