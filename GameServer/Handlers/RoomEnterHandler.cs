using System;
using System.Threading.Tasks;
using GameServerApp.Rooms;
using SharedProtocol;
using RoomRole = SharedProtocol.Common.Enums.CoreEnums.RoomRole;

namespace GameServerApp.Handlers;

/// <summary>
/// Handles requests for entering a specific room.
/// </summary>
public class RoomEnterHandler : MessageHandler<RoomEnterRequest>
{
    private readonly SessionManager _sessions;
    private readonly RoomManager _rooms;

    public RoomEnterHandler(SessionManager sessions, RoomManager rooms)
        : base(MessageType.RoomEnterRequest)
    {
        _sessions = sessions;
        _rooms = rooms;
    }

    protected override async Task HandleAsync(Session session, RoomEnterRequest message)
    {
        if (!_sessions.ValidateSession(session))
        {
            await SendFailure(session, "인증되지 않은 세션입니다.");
            return;
        }

        var options = new RoomJoinOptions
        {
            AllowQueue = message.AllowQueue,
            JoinAsSpectator = message.JoinAsSpectator,
            PreferredRole = Enum.IsDefined(typeof(RoomRole), message.PreferredRole)
                ? (RoomRole)message.PreferredRole
                : RoomRole.Player,
            Password = message.Password
        };

        RoomAssignmentResult result;
        if (message.AutoAssign)
        {
            var lobbyId = string.IsNullOrWhiteSpace(message.LobbyId)
                ? RoomManager.DefaultLobbyId
                : message.LobbyId;
            result = _rooms.AutoAssign(session.UserName!, lobbyId, options);
        }
        else
        {
            var roomId = string.IsNullOrWhiteSpace(message.RoomId)
                ? RoomManager.DefaultLobbyId
                : message.RoomId;
            result = _rooms.TryAssignPlayerToRoom(session.UserName!, roomId, options);
        }

        if (!result.Success || result.Room == null)
        {
            await SendFailure(session, string.IsNullOrEmpty(result.FailureReason)
                ? "방이 가득 찼거나 입장할 수 없습니다."
                : result.FailureReason);
            return;
        }

        var room = result.Room;
        _sessions.UpdatePlayerWorld(session.UserName!, room.WorldId, 0, 0);

        var response = new RoomEnterResponse
        {
            Success = true,
            Message = result.Queued
                ? "방이 가득 차 대기열에 등록되었습니다."
                : $"{room.DisplayName} 방에 입장했습니다.",
            Room = room.ToRoomInfo(),
            Members = room.GetMemberSnapshot(),
            IsQueued = result.Queued,
            QueuePosition = result.QueuePosition,
            JoinedAsSpectator = result.Member?.Role == RoomRole.Spectator,
            EstimatedWaitMs = result.Queued ? Math.Max(0, result.QueuePosition - 1) * 15000L : 0,
            Member = result.Member != null ? room.BuildMemberInfo(result.Member) : null
        };

        await session.SendAsync(MessageType.RoomEnterResponse, response);

        if (!result.Queued && !result.AlreadyInRoom)
        {
            var joinNotice = new ChatMessage
            {
                SenderId = "System",
                SenderName = "System",
                Type = (int)ChatType.System,
                Message = response.JoinedAsSpectator
                    ? $"{session.UserName} 님이 관전 모드로 방에 합류했습니다."
                    : $"{session.UserName} 님이 방에 입장했습니다.",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await _rooms.BroadcastToRoomAsync(room.RoomId, MessageType.ChatMessage, joinNotice);
        }
    }

    private Task SendFailure(Session session, string message)
    {
        var response = new RoomEnterResponse
        {
            Success = false,
            Message = message
        };
        return session.SendAsync(MessageType.RoomEnterResponse, response);
    }
}
