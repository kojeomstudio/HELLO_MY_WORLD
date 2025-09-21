using System;
using System.Collections.Generic;
using System.Linq;
using SharedProtocol;

namespace GameServerApp.Rooms;

/// <summary>
/// Represents a logical game room with queueing and role management support.
/// </summary>
public class GameRoom
{
    private readonly object _sync = new();
    private readonly Dictionary<string, RoomMember> _members = new();
    private readonly List<RoomMember> _queue = new();
    private readonly Dictionary<string, string> _tags = new();
    private readonly string? _password;

    public GameRoom(
        string roomId,
        int worldId,
        string displayName,
        int maxPlayers = 0,
        bool isLobby = false,
        string lobbyId = RoomManager.DefaultLobbyId,
        string gameMode = "default",
        RoomVisibility visibility = RoomVisibility.Public,
        string? password = null)
    {
        RoomId = roomId;
        WorldId = worldId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? roomId : displayName;
        MaxPlayers = Math.Max(0, maxPlayers);
        IsLobby = isLobby;
        LobbyId = string.IsNullOrWhiteSpace(lobbyId) ? RoomManager.DefaultLobbyId : lobbyId;
        GameMode = gameMode;
        Visibility = visibility;
        Status = RoomStatus.Waiting;
        _password = string.IsNullOrWhiteSpace(password) ? null : password;

        _tags["gameMode"] = GameMode;
        _tags["lobbyId"] = LobbyId;
    }

    public string RoomId { get; }
    public string DisplayName { get; private set; }
    public int WorldId { get; }
    public int MaxPlayers { get; }
    public bool IsLobby { get; }
    public string LobbyId { get; }
    public string GameMode { get; private set; }
    public RoomVisibility Visibility { get; private set; }
    public RoomStatus Status { get; private set; }
    public string? Owner { get; private set; }
    public bool RequiresPassword => !string.IsNullOrEmpty(_password);

    public int ActivePlayerCount
    {
        get
        {
            lock (_sync)
            {
                return _members.Values.Count(m => m.Role != RoomRole.Spectator);
            }
        }
    }

    public int SpectatorCount
    {
        get
        {
            lock (_sync)
            {
                return _members.Values.Count(m => m.Role == RoomRole.Spectator);
            }
        }
    }

    public int QueueCount
    {
        get
        {
            lock (_sync)
            {
                return _queue.Count;
            }
        }
    }

    public IReadOnlyDictionary<string, string> Tags
    {
        get
        {
            lock (_sync)
            {
                return new Dictionary<string, string>(_tags);
            }
        }
    }

    public bool Contains(string userName)
    {
        lock (_sync)
        {
            return _members.ContainsKey(userName) ||
                   _queue.Any(m => string.Equals(m.UserName, userName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public RoomJoinResult TryJoin(string userName, RoomJoinOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return RoomJoinResult.Failed("잘못된 사용자 이름입니다.");
        }

        options ??= RoomJoinOptions.Default;

        lock (_sync)
        {
            if (_members.TryGetValue(userName, out var existing))
            {
                return RoomJoinResult.Joined(existing);
            }

            var queued = _queue.FirstOrDefault(m => string.Equals(m.UserName, userName, StringComparison.OrdinalIgnoreCase));
            if (queued != null)
            {
                return RoomJoinResult.FromQueue(queued);
            }

            if (RequiresPassword && !IsLobby)
            {
                if (string.IsNullOrEmpty(options.Password) || !string.Equals(options.Password, _password))
                {
                    return RoomJoinResult.Failed("비밀번호가 일치하지 않습니다.");
                }
            }

            bool joinAsSpectator = options.JoinAsSpectator || options.PreferredRole == RoomRole.Spectator;
            bool hasSeat = MaxPlayers <= 0 ||
                           _members.Values.Count(m => m.Role != RoomRole.Spectator) < MaxPlayers;

            if (!joinAsSpectator && !hasSeat)
            {
                if (!options.AllowQueue)
                {
                    return RoomJoinResult.Failed("방이 가득 찼습니다.");
                }

                var queuedMember = new RoomMember(userName, RoomRole.Queue, DateTime.UtcNow)
                {
                    IsReady = false,
                    IsQueued = true,
                    QueuePosition = _queue.Count + 1
                };
                _queue.Add(queuedMember);
                return RoomJoinResult.FromQueue(queuedMember);
            }

            var role = joinAsSpectator ? RoomRole.Spectator : options.PreferredRole;
            if (role == RoomRole.Queue)
            {
                role = RoomRole.Player;
            }

            if (string.IsNullOrEmpty(Owner) && role != RoomRole.Spectator)
            {
                Owner = userName;
                role = RoomRole.Host;
                _tags["owner"] = Owner;
            }

            var member = new RoomMember(userName, role, DateTime.UtcNow)
            {
                IsReady = false,
                IsQueued = false,
                QueuePosition = 0
            };
            _members[userName] = member;

        return RoomJoinResult.Joined(member);
        }
    }

    public bool TryRemoveMember(string userName, out bool removedFromActive)
    {
        lock (_sync)
        {
            if (_members.Remove(userName, out var removed))
            {
                removedFromActive = removed.Role != RoomRole.Spectator;
                if (Owner == userName)
                {
                    Owner = _members.Values
                        .Where(m => m.Role != RoomRole.Spectator)
                        .Select(m => m.UserName)
                        .FirstOrDefault();

                    if (Owner == null && _members.Count > 0)
                    {
                        Owner = _members.Values.First().UserName;
                    }

                    if (Owner != null && _members.TryGetValue(Owner, out var newOwner))
                    {
                        newOwner.Role = RoomRole.Host;
                        _tags["owner"] = Owner;
                    }
                    else
                    {
                        _tags.Remove("owner");
                    }
                }
                else if (!_members.Values.Any(m => m.Role == RoomRole.Host) && Owner != null)
                {
                    if (_members.TryGetValue(Owner, out var ownerMember))
                    {
                        ownerMember.Role = RoomRole.Host;
                    }
                }
                return true;
            }

            var index = _queue.FindIndex(m => string.Equals(m.UserName, userName, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _queue.RemoveAt(index);
                RefreshQueuePositions();
                removedFromActive = false;
                return true;
            }

            removedFromActive = false;
            return false;
        }
    }

    public RoomMember? PromoteNextFromQueue()
    {
        lock (_sync)
        {
            if (_queue.Count == 0)
                return null;

            bool hasSeat = MaxPlayers <= 0 ||
                           _members.Values.Count(m => m.Role != RoomRole.Spectator) < MaxPlayers;
            if (!hasSeat)
                return null;

            var next = _queue[0];
            _queue.RemoveAt(0);
            next.IsQueued = false;
            next.QueuePosition = 0;
            next.JoinedAt = DateTime.UtcNow;
            next.Role = RoomRole.Player;
            _members[next.UserName] = next;

            if (string.IsNullOrEmpty(Owner))
            {
                Owner = next.UserName;
                next.Role = RoomRole.Host;
                _tags["owner"] = Owner;
            }

            RefreshQueuePositions();
            return next;
        }
    }

    public List<string> GetMemberSnapshot()
    {
        lock (_sync)
        {
            return _members.Keys.ToList();
        }
    }

    public List<RoomMemberInfo> GetMemberInfoSnapshot()
    {
        lock (_sync)
        {
            var infos = new List<RoomMemberInfo>();
            infos.AddRange(_members.Values.Select(ToMemberInfo));
            infos.AddRange(_queue.Select(ToMemberInfo));
            return infos;
        }
    }

    public RoomMemberInfo BuildMemberInfo(RoomMember member)
    {
        return ToMemberInfo(member);
    }

    public bool TryGetMember(string userName, out RoomMember? member)
    {
        lock (_sync)
        {
            if (_members.TryGetValue(userName, out var active))
            {
                member = active;
                return true;
            }

            member = _queue.FirstOrDefault(m => string.Equals(m.UserName, userName, StringComparison.OrdinalIgnoreCase));
            return member != null;
        }
    }

    public RoomInfo ToRoomInfo()
    {
        lock (_sync)
        {
            return new RoomInfo
            {
                RoomId = RoomId,
                DisplayName = DisplayName,
                WorldId = WorldId,
                PlayerCount = _members.Values.Count(m => m.Role != RoomRole.Spectator),
                Capacity = MaxPlayers,
                IsLobby = IsLobby,
                LobbyId = LobbyId,
                Owner = Owner ?? string.Empty,
                GameMode = GameMode,
                QueueCount = _queue.Count,
                Status = (int)Status,
                Visibility = (int)Visibility,
                RequiresPassword = RequiresPassword,
                SpectatorCount = _members.Values.Count(m => m.Role == RoomRole.Spectator),
                Tags = new Dictionary<string, string>(_tags)
            };
        }
    }

    public void UpdateStatus(RoomStatus status)
    {
        lock (_sync)
        {
            Status = status;
        }
    }

    public void UpdateGameMode(string gameMode)
    {
        if (string.IsNullOrWhiteSpace(gameMode)) return;
        lock (_sync)
        {
            GameMode = gameMode;
            _tags["gameMode"] = GameMode;
        }
    }

    public void UpdateVisibility(RoomVisibility visibility)
    {
        lock (_sync)
        {
            Visibility = visibility;
        }
    }

    private static RoomMemberInfo ToMemberInfo(RoomMember member)
    {
        return new RoomMemberInfo
        {
            UserName = member.UserName,
            Role = (int)member.Role,
            IsReady = member.IsReady,
            JoinedAt = new DateTimeOffset(member.JoinedAt).ToUnixTimeMilliseconds(),
            IsSpectator = member.Role == RoomRole.Spectator,
            IsQueued = member.IsQueued,
            QueuePosition = member.QueuePosition
        };
    }

    private void RefreshQueuePositions()
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            _queue[i].QueuePosition = i + 1;
            _queue[i].IsQueued = true;
        }
    }
}

public class RoomJoinOptions
{
    public bool AllowQueue { get; set; } = true;
    public RoomRole PreferredRole { get; set; } = RoomRole.Player;
    public bool JoinAsSpectator { get; set; }
    public string? Password { get; set; }

    public static RoomJoinOptions Default => new();
}

public class RoomJoinResult
{
    public bool Success { get; private set; }
    public bool Queued { get; private set; }
    public int QueuePosition { get; private set; }
    public RoomMember? Member { get; private set; }
    public string FailureReason { get; private set; } = string.Empty;

    public static RoomJoinResult Joined(RoomMember member)
    {
        return new RoomJoinResult
        {
            Success = true,
            Queued = false,
            QueuePosition = 0,
            Member = member,
            FailureReason = string.Empty
        };
    }

    public static RoomJoinResult FromQueue(RoomMember member)
    {
        return new RoomJoinResult
        {
            Success = true,
            Queued = true,
            QueuePosition = member.QueuePosition,
            Member = member,
            FailureReason = string.Empty
        };
    }

    public static RoomJoinResult Failed(string message)
    {
        return new RoomJoinResult
        {
            Success = false,
            Queued = false,
            QueuePosition = -1,
            Member = null,
            FailureReason = message
        };
    }
}

public class RoomMember
{
    public RoomMember(string userName, RoomRole role, DateTime joinedAt)
    {
        UserName = userName;
        Role = role;
        JoinedAt = joinedAt;
    }

    public string UserName { get; }
    public RoomRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsReady { get; set; }
    public bool IsQueued { get; set; }
    public int QueuePosition { get; set; }
    public bool IsSpectator => Role == RoomRole.Spectator;
}
