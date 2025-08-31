using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SharedProtocol
{
    /// <summary>
    /// 마인크래프트 전용 메시지 디스패처 - 기존 MessageDispatcher와 연동하여 확장 기능 제공
    /// </summary>
    public class MinecraftMessageDispatcher
    {
        private readonly Dictionary<MinecraftMessageType, IMinecraftMessageHandler> _handlers = new();
        private readonly MessageDispatcher _baseDispatcher;

        public MinecraftMessageDispatcher(MessageDispatcher baseDispatcher)
        {
            _baseDispatcher = baseDispatcher;
        }

        /// <summary>
        /// 마인크래프트 전용 핸들러 등록
        /// </summary>
        public void RegisterHandler<T>(MinecraftMessageType messageType, IMinecraftMessageHandler<T> handler)
            where T : class
        {
            _handlers[messageType] = handler;
        }

        /// <summary>
        /// 마인크래프트 메시지 디스패치
        /// </summary>
        public async Task DispatchMinecraftMessageAsync(Session session, MinecraftMessageType messageType, byte[] messageData)
        {
            if (!_handlers.TryGetValue(messageType, out var handler))
            {
                Console.WriteLine($"No handler found for Minecraft message type: {messageType}");
                return;
            }

            try
            {
                await handler.HandleAsync(session, messageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling Minecraft message {messageType}: {ex.Message}");
                // 로그 시스템이 있다면 여기서 상세 로깅
            }
        }

        /// <summary>
        /// 모든 연결된 세션에 메시지 브로드캐스트
        /// </summary>
        public async Task BroadcastMessageAsync<T>(MinecraftMessageType messageType, T message, Session? excludeSession = null)
            where T : class
        {
            // SessionManager를 통해 모든 활성 세션에 브로드캐스트
            // 구현은 SessionManager에서 제공하는 API에 따라 조정
            Console.WriteLine($"Broadcasting Minecraft message: {messageType}");
            
            // TODO: SessionManager.GetAllSessions()를 통해 브로드캐스트 구현
        }

        /// <summary>
        /// 특정 플레이어에게 메시지 전송
        /// </summary>
        public async Task SendToPlayerAsync<T>(string playerId, MinecraftMessageType messageType, T message)
            where T : class
        {
            // TODO: SessionManager를 통해 특정 플레이어 세션 찾기
            Console.WriteLine($"Sending Minecraft message {messageType} to player: {playerId}");
        }

        /// <summary>
        /// 특정 청크 범위 내 플레이어들에게 메시지 전송
        /// </summary>
        public async Task SendToChunkPlayersAsync<T>(int chunkX, int chunkZ, int viewDistance, 
            MinecraftMessageType messageType, T message, Session? excludeSession = null)
            where T : class
        {
            // 청크 범위 내 플레이어들에게만 전송 (최적화)
            Console.WriteLine($"Sending message {messageType} to players in chunk [{chunkX}, {chunkZ}] with view distance {viewDistance}");
        }

        /// <summary>
        /// 등록된 핸들러 수
        /// </summary>
        public int HandlerCount => _handlers.Count;
    }

    /// <summary>
    /// 마인크래프트 메시지 핸들러 기본 인터페이스
    /// </summary>
    public interface IMinecraftMessageHandler
    {
        Task HandleAsync(Session session, byte[] messageData);
    }

    /// <summary>
    /// 타입이 지정된 마인크래프트 메시지 핸들러 인터페이스
    /// </summary>
    public interface IMinecraftMessageHandler<T> : IMinecraftMessageHandler where T : class
    {
        Task HandleAsync(Session session, T message);
    }

    /// <summary>
    /// 마인크래프트 메시지 핸들러 기본 클래스 - 직렬화/역직렬화 처리
    /// </summary>
    public abstract class MinecraftMessageHandlerBase<T> : IMinecraftMessageHandler<T> where T : class
    {
        public async Task HandleAsync(Session session, byte[] messageData)
        {
            try
            {
                // ProtoBuf를 사용한 역직렬화
                var message = ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(messageData));
                await HandleAsync(session, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing message of type {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }

        public abstract Task HandleAsync(Session session, T message);
    }
}