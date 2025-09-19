using System;
using System.Threading.Tasks;
using GameServerApp.Rooms;
using SharedProtocol;

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

        if (string.IsNullOrWhiteSpace(message.RoomId))
        {
            await SendFailure(session, "유효하지 않은 방 ID 입니다.");
            return;
        }

        var room = _rooms.GetRoom(message.RoomId);
        if (room == null)
        {
            await SendFailure(session, "존재하지 않는 방입니다.");
            return;
        }

        if (!_rooms.AssignPlayerToRoom(session.UserName!, room.RoomId))
        {
            await SendFailure(session, "방이 가득 찼거나 입장할 수 없습니다.");
            return;
        }

        _sessions.UpdatePlayerWorld(session.UserName!, room.WorldId, 0, 0);

        var response = new RoomEnterResponse
        {
            Success = true,
            Message = $"{room.DisplayName} 방에 입장했습니다.",
            Room = room.ToRoomInfo(),
            Members = room.GetMemberSnapshot()
        };

        await session.SendAsync(MessageType.RoomEnterResponse, response);

        // Broadcast join notification to other members
        var joinNotice = new ChatMessage
        {
            SenderId = "System",
            SenderName = "System",
            Type = (int)ChatType.System,
            Message = $"{session.UserName} 님이 방에 입장했습니다.",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await _rooms.BroadcastToRoomAsync(room.RoomId, MessageType.ChatMessage, joinNotice);
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
