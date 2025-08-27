using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// 월드 블록 변경 요청을 처리하는 핸들러
/// 블록 변경을 처리하고 다른 플레이어들에게 브로드캐스트합니다.
/// </summary>
public class WorldBlockHandler : MessageHandler<WorldBlockChangeRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;

    public WorldBlockHandler(DatabaseHelper database, SessionManager sessions) 
        : base(MessageType.WorldBlockChangeRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override async Task HandleAsync(Session session, WorldBlockChangeRequest message)
    {
        try
        {
            // 세션 인증 확인
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendFailureResponse(session, "인증되지 않은 세션입니다.");
                return;
            }

            // 입력 검증
            if (string.IsNullOrEmpty(message.AreaId) || string.IsNullOrEmpty(message.SubworldId) || 
                message.BlockPosition == null)
            {
                await SendFailureResponse(session, "잘못된 블록 변경 요청입니다.");
                return;
            }

            // 블록 변경 권한 확인 (실제 환경에서는 더 복잡한 권한 시스템 필요)
            if (!await ValidateBlockChangePermission(session, message))
            {
                await SendFailureResponse(session, "블록을 변경할 권한이 없습니다.");
                return;
            }

            // 블록 변경 처리 (실제로는 월드 데이터 저장소에 저장)
            await ProcessBlockChange(session, message);

            // 성공 응답 전송
            var response = new WorldBlockChangeResponse
            {
                Success = true,
                Message = "블록이 성공적으로 변경되었습니다.",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await session.SendAsync(MessageType.WorldBlockChangeResponse, response);

            // 다른 플레이어들에게 브로드캐스트
            await BroadcastBlockChange(session, message);

            Console.WriteLine($"Block changed by {session.UserName}: {message.AreaId}/{message.SubworldId} at ({message.BlockPosition.X}, {message.BlockPosition.Y}, {message.BlockPosition.Z})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Block change error for user '{session.UserName}': {ex.Message}");
            await SendFailureResponse(session, "블록 변경 처리 중 오류가 발생했습니다.");
        }
    }

    /// <summary>
    /// 실패 응답을 보냅니다.
    /// </summary>
    private async Task SendFailureResponse(Session session, string errorMessage)
    {
        var response = new WorldBlockChangeResponse 
        { 
            Success = false, 
            Message = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.WorldBlockChangeResponse, response);
    }

    /// <summary>
    /// 블록 변경 권한을 확인합니다.
    /// </summary>
    private async Task<bool> ValidateBlockChangePermission(Session session, WorldBlockChangeRequest message)
    {
        await Task.Delay(10); // DB 접근 지연 시뮬레이션
        
        // TODO: 실제 권한 시스템 구현
        // - 플레이어가 해당 지역에 블록을 변경할 권한이 있는지 확인
        // - 보호된 지역인지 확인
        // - 플레이어 레벨/권한 확인 등
        
        return true; // 현재는 모든 변경을 허용
    }

    /// <summary>
    /// 블록 변경을 실제로 처리합니다.
    /// </summary>
    private async Task ProcessBlockChange(Session session, WorldBlockChangeRequest message)
    {
        await Task.Delay(20); // 월드 데이터 처리 지연 시뮬레이션
        
        // TODO: 실제 월드 데이터 저장소에 블록 변경 사항 저장
        // - 데이터베이스 또는 파일 시스템에 블록 변경 사항 저장
        // - 청크 데이터 업데이트
        // - 백업 시스템 호출 등
    }

    /// <summary>
    /// 블록 변경 사항을 다른 플레이어들에게 브로드캐스트합니다.
    /// </summary>
    private async Task BroadcastBlockChange(Session originSession, WorldBlockChangeRequest message)
    {
        var broadcast = new WorldBlockChangeBroadcast
        {
            AreaId = message.AreaId,
            SubworldId = message.SubworldId,
            BlockPosition = message.BlockPosition,
            BlockType = message.BlockType,
            ChunkType = message.ChunkType,
            PlayerId = originSession.PlayerInfo?.PlayerId ?? originSession.UserName ?? "Unknown",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // 해당 영역에 있는 다른 모든 플레이어들에게 브로드캐스트
        var tasks = new List<Task>();
        foreach (var playerName in _sessions.ConnectedUsers)
        {
            var playerSession = _sessions.GetSession(playerName);
            if (playerSession != null && playerSession != originSession)
            {
                // TODO: 실제로는 플레이어가 해당 영역에 있는지 확인해야 함
                tasks.Add(playerSession.SendAsync(MessageType.WorldBlockChangeBroadcast, broadcast));
            }
        }

        await Task.WhenAll(tasks);
    }
}