using System;
using System.Linq;
using System.Threading.Tasks;
using GameServerApp.Rooms;
using SharedProtocol;


namespace GameServerApp.Handlers;

/// <summary>
/// Handles requests for the list of available rooms/lobbies.
/// </summary>
public class RoomListHandler : MessageHandler<RoomListRequest>
{
    private readonly SessionManager _sessions;
    private readonly RoomManager _rooms;

    public RoomListHandler(SessionManager sessions, RoomManager rooms)
        : base(MessageType.RoomListRequest)
    {
        _sessions = sessions;
        _rooms = rooms;
    }

    protected override async Task HandleAsync(Session session, RoomListRequest message)
    {
        if (!_sessions.ValidateSession(session))
        {
            await SendFailureAsync(session, "인증되지 않은 세션입니다.");
            return;
        }

        var query = _rooms.GetRooms();
        if (message.WorldIdFilter >= 0)
        {
            query = query.Where(room => room.WorldId == message.WorldIdFilter);
        }

        var rooms = query.ToList();
        var response = new RoomListResponse
        {
            Success = true,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Rooms = rooms.Select(room => room.ToRoomInfo()).ToList()
        };

        if (message.IncludeMembers)
        {
            foreach (var room in rooms)
            {
                response.MemberLists.Add(new RoomMemberList
                {
                    RoomId = room.RoomId,
                    Members = room.GetMemberSnapshot()
                });
            }
        }

        await session.SendAsync(MessageType.RoomListResponse, response);
    }

    private Task SendFailureAsync(Session session, string message)
    {
        var response = new RoomListResponse
        {
            Success = false,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        return session.SendAsync(MessageType.RoomListResponse, response);
    }
}
