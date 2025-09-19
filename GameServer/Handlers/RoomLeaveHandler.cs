using System;
using System.Threading.Tasks;
using GameServerApp.Rooms;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// Handles requests to leave the current room. Players are transferred back to lobby by default.
/// </summary>
public class RoomLeaveHandler : MessageHandler<RoomLeaveRequest>
{
    private readonly SessionManager _sessions;
    private readonly RoomManager _rooms;

    public RoomLeaveHandler(SessionManager sessions, RoomManager rooms)
        : base(MessageType.RoomLeaveRequest)
    {
        _sessions = sessions;
        _rooms = rooms;
    }

    protected override async Task HandleAsync(Session session, RoomLeaveRequest message)
    {
        if (!_sessions.ValidateSession(session))
        {
            await SendFailure(session, "인증되지 않은 세션입니다.");
            return;
        }

        var currentRoomId = _rooms.GetPlayerRoomId(session.UserName!);
        if (string.IsNullOrEmpty(currentRoomId))
        {
            await SendFailure(session, "현재 참여 중인 방이 없습니다.");
            return;
        }

        if (!string.IsNullOrEmpty(message.RoomId) && currentRoomId != message.RoomId)
        {
            await SendFailure(session, "요청한 방과 현재 방이 일치하지 않습니다.");
            return;
        }

        if (currentRoomId == RoomManager.DefaultLobbyId)
        {
            await SendFailure(session, "이미 로비에 있습니다.");
            return;
        }

        _rooms.RemovePlayer(session.UserName!);
        _rooms.AssignPlayerToRoom(session.UserName!, RoomManager.DefaultLobbyId);
        _sessions.UpdatePlayerWorld(session.UserName!, worldId: 1, chunkX: 0, chunkZ: 0);

        var response = new RoomLeaveResponse
        {
            Success = true,
            Message = "로비로 이동했습니다.",
            PreviousRoomId = currentRoomId
        };

        await session.SendAsync(MessageType.RoomLeaveResponse, response);

        var leaveNotice = new ChatMessage
        {
            SenderId = "System",
            SenderName = "System",
            Type = (int)ChatType.System,
            Message = $"{session.UserName} 님이 방을 떠났습니다.",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await _rooms.BroadcastToRoomAsync(currentRoomId, MessageType.ChatMessage, leaveNotice);
    }

    private Task SendFailure(Session session, string message)
    {
        var response = new RoomLeaveResponse
        {
            Success = false,
            Message = message
        };
        return session.SendAsync(MessageType.RoomLeaveResponse, response);
    }
}
