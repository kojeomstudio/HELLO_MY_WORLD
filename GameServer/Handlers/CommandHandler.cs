using System;
using System.Threading.Tasks;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// 명령어 핸들러 - GM 명령어 및 플레이어 명령어 처리
    /// </summary>
    public class CommandHandler : MessageHandler<CommandRequest>
    {
        private readonly CommandSystem _commandSystem;
        private readonly SessionManager _sessionManager;

        public CommandHandler(CommandSystem commandSystem, SessionManager sessionManager)
            : base(null, null)
        {
            _commandSystem = commandSystem;
            _sessionManager = sessionManager;
        }

        protected override async Task HandleAsync(Session session, CommandRequest message)
        {
            if (string.IsNullOrEmpty(session.UserName))
            {
                await session.SendAsync(MessageType.CommandResponse, new CommandResponse
                {
                    Success = false,
                    Message = "Not authenticated",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                return;
            }

            Console.WriteLine($"[Command] {session.UserName}: {message.CommandText}");

            // 명령어 실행
            var result = await _commandSystem.ExecuteCommandAsync(
                session.UserName,
                message.CommandText,
                session,
                _sessionManager
            );

            // 응답 전송
            var response = new CommandResponse
            {
                Success = result.Success,
                Message = result.Message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.CommandResponse, response);

            // 브로드캐스트가 필요한 경우
            if (result.Success && result.ShouldBroadcast)
            {
                var broadcast = new CommandBroadcast
                {
                    PlayerName = session.UserName,
                    Message = result.Message,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await _sessionManager.BroadcastToAllAsync(MessageType.CommandBroadcast, broadcast);

                Console.WriteLine($"[Command] Broadcast: {result.Message}");
            }
        }
    }
}
