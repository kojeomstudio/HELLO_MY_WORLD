using GameServerApp.Models;
using GameServerApp.Database;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// 플레이어 이동 요청을 처리하는 핸들러
/// 이동 유효성 검증, 위치 업데이트, 다른 플레이어에게 알림 등을 담당합니다.
/// </summary>
public class MovementHandler : MessageHandler<MoveRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    
    // 이동 속도 제한 (단위: 유닛/초)
    private const float MAX_MOVEMENT_SPEED = 10.0f;
    private const float MIN_MOVEMENT_SPEED = 0.1f;

    public MovementHandler(DatabaseHelper database, SessionManager sessions) : base(MessageType.MoveRequest)
    {
        _database = database;
        _sessions = sessions;
    }

    protected override async Task HandleAsync(Session session, MoveRequest message)
    {
        try
        {
            // 세션 인증 확인
            if (string.IsNullOrEmpty(session.SessionToken) || string.IsNullOrEmpty(session.UserName))
            {
                await SendMoveFailure(session, "인증되지 않은 세션입니다.");
                return;
            }

            // 등록된 세션인지 확인
            if (_sessions.GetSession(session.UserName) != session)
            {
                await SendMoveFailure(session, "잘못된 세션입니다.");
                return;
            }

            // 입력 검증
            if (message.TargetPosition == null)
            {
                await SendMoveFailure(session, "목표 위치가 지정되지 않았습니다.");
                return;
            }

            // 이동 속도 검증
            if (message.MovementSpeed < MIN_MOVEMENT_SPEED || message.MovementSpeed > MAX_MOVEMENT_SPEED)
            {
                await SendMoveFailure(session, $"잘못된 이동 속도입니다. (허용 범위: {MIN_MOVEMENT_SPEED} - {MAX_MOVEMENT_SPEED})");
                return;
            }

            // 현재 플레이어 정보 가져오기
            var currentPlayers = _database.GetPlayers().ToList();
            var currentCharacter = currentPlayers.FirstOrDefault(p => p.Name == session.UserName);
            
            if (currentCharacter == null)
            {
                await SendMoveFailure(session, "플레이어 정보를 찾을 수 없습니다.");
                return;
            }

            // 이동 거리 및 유효성 검증
            var currentPos = new Vector3((float)currentCharacter.X, (float)currentCharacter.Y, 0);
            var targetPos = message.TargetPosition;
            
            if (!await ValidateMovement(currentPos, targetPos, message.MovementSpeed))
            {
                await SendMoveFailure(session, "잘못된 이동 요청입니다.");
                return;
            }

            // 플레이어 위치 업데이트
            currentCharacter.X = targetPos.X;
            currentCharacter.Y = targetPos.Y;
            _database.SavePlayer(currentCharacter);

            // 세션의 플레이어 정보도 업데이트
            if (session.PlayerInfo != null)
            {
                session.PlayerInfo.Position = new Vector3(targetPos.X, targetPos.Y, targetPos.Z);
            }

            // 성공 응답 전송
            var response = new MoveResponse
            {
                Success = true,
                NewPosition = new Vector3(targetPos.X, targetPos.Y, targetPos.Z),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            await session.SendAsync(MessageType.MoveResponse, response);

            // 다른 플레이어들에게 위치 업데이트 브로드캐스트 (선택사항)
            await BroadcastPlayerMovement(session, targetPos);

            Console.WriteLine($"Player {session.UserName} moved to ({targetPos.X:F2}, {targetPos.Y:F2}, {targetPos.Z:F2})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Movement error for user '{session.UserName}': {ex.Message}");
            await SendMoveFailure(session, "이동 처리 중 오류가 발생했습니다.");
        }
    }

    /// <summary>
    /// 이동 실패 응답을 보냅니다.
    /// </summary>
    private async Task SendMoveFailure(Session session, string errorMessage)
    {
        var response = new MoveResponse 
        { 
            Success = false,
            NewPosition = session.PlayerInfo?.Position ?? new Vector3(0, 0, 0),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.MoveResponse, response);
        Console.WriteLine($"Movement failed for {session.UserName}: {errorMessage}");
    }

    /// <summary>
    /// 이동 요청의 유효성을 검증합니다.
    /// </summary>
    private async Task<bool> ValidateMovement(Vector3 currentPos, Vector3 targetPos, float movementSpeed)
    {
        await Task.Delay(5); // 검증 처리 시뮬레이션
        
        // 거리 계산
        var deltaX = targetPos.X - currentPos.X;
        var deltaY = targetPos.Y - currentPos.Y;
        var deltaZ = targetPos.Z - currentPos.Z;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        
        // 너무 멀리 이동하는 것을 방지 (치트 방지)
        const double MAX_SINGLE_MOVE_DISTANCE = 50.0; // 한 번에 최대 50유닛까지만 이동 가능
        if (distance > MAX_SINGLE_MOVE_DISTANCE)
        {
            Console.WriteLine($"Movement rejected: distance too large ({distance:F2} > {MAX_SINGLE_MOVE_DISTANCE})");
            return false;
        }

        // TODO: 추가 검증
        // - 장애물 충돌 검사
        // - 맵 경계 검사
        // - 플레이어 상태 확인 (기절, 정지 등)
        // - 이동 가능 지형 확인
        
        return true;
    }

    /// <summary>
    /// 다른 플레이어들에게 플레이어의 이동을 브로드캐스트합니다.
    /// </summary>
    private async Task BroadcastPlayerMovement(Session movedSession, Vector3 newPosition)
    {
        // TODO: 실제로는 근처에 있는 플레이어들에게만 브로드캐스트해야 함
        // 현재는 간단한 구현으로 모든 플레이어에게 전송하지 않음 (성능상 이유)
        
        // 실제 구현 시에는 PlayerInfoUpdate 메시지를 사용하여
        // 근처 플레이어들에게만 위치 업데이트를 전송할 수 있음
        
        await Task.CompletedTask; // 현재는 브로드캐스트하지 않음
    }
}
