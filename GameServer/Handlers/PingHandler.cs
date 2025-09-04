using SharedProtocol;
using GameServerApp.Database;

namespace GameServerApp.Handlers;

/// <summary>
/// 핑/퐁 요청을 처리하는 핸들러
/// 네트워크 지연 시간 측정 및 연결 상태 확인에 사용됩니다.
/// </summary>
public class PingHandler : MessageHandler<PingRequest>
{
    public PingHandler(DatabaseHelper database, SessionManager sessions) : base(MessageType.PingRequest)
    {
    }

    protected override async Task HandleAsync(Session session, PingRequest message)
    {
        try
        {
            // 핑 응답 생성
            var response = new PingResponse
            {
                ClientTimestamp = message.ClientTimestamp,
                ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.PingResponse, response);

            // 로그는 너무 자주 발생할 수 있으므로 선택적으로만 출력
            // Console.WriteLine($"Ping from {session.UserName}: {message.ClientTimestamp}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ping error for user '{session.UserName}': {ex.Message}");
        }
    }
}
