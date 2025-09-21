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

        if (!string.IsNullOrWhiteSpace(message.LobbyIdFilter))
        {
            query = query.Where(room => string.Equals(room.LobbyId, message.LobbyIdFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (message.VisibilityFilter >= 0)
        {
            query = query.Where(room => (int)room.Visibility == message.VisibilityFilter);
        }

        if (!string.IsNullOrWhiteSpace(message.GameModeFilter))
        {
            query = query.Where(room => string.Equals(room.GameMode, message.GameModeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (message.OnlyJoinable)
        {
            query = query.Where(room => room.MaxPlayers <= 0 || room.ActivePlayerCount < room.MaxPlayers);
        }

        var rooms = query.ToList();

        var roomInfos = rooms
            .Select(room =>
            {
                var info = room.ToRoomInfo();
                if (!message.IncludeQueues)
                {
                    info.QueueCount = 0;
                }
                if (!message.IncludeTags)
                {
                    info.Tags.Clear();
                }
                return info;
            })
            .ToList();

        var response = new RoomListResponse
        {
            Success = true,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Rooms = roomInfos,
            IncludesQueues = message.IncludeQueues,
            IncludesTags = message.IncludeTags
        };

        if (message.IncludeMembers)
        {
            foreach (var room in rooms)
            {
                response.MemberLists.Add(new RoomMemberList
                {
                    RoomId = room.RoomId,
                    Members = room.GetMemberSnapshot(),
                    MemberInfos = room.GetMemberInfoSnapshot()
                });
            }
        }

        if (message.IncludeLobbySummary)
        {
            response.LobbySummaries = _rooms.GetLobbySummaries().ToList();
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
