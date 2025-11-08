using GameServerApp.AI;
using GameProtocol;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// AI 스폰 요청 핸들러 (GM 명령어)
    /// </summary>
    public class AISpawnHandler : MessageHandler<AISpawnRequest>
    {
        private readonly ServerAIManager _aiManager;
        private readonly SessionManager _sessions;

        public AISpawnHandler(ServerAIManager aiManager, SessionManager sessions)
            : base(MessageType.AISpawnRequest)
        {
            _aiManager = aiManager;
            _sessions = sessions;
        }

        protected override async Task HandleAsync(Session session, AISpawnRequest message)
        {
            Console.WriteLine($"[AISpawnHandler] {session.UserName} requests spawn: {message.AIType} at ({message.SpawnPosition.X}, {message.SpawnPosition.Y}, {message.SpawnPosition.Z})");

            try
            {
                // TODO: 권한 체크 (GM only)
                // For now, allow all authenticated users to spawn AI for testing

                var actor = _aiManager.SpawnAI(message.AIType, message.SpawnPosition, message.WorldId);

                var response = new AISpawnResponse
                {
                    Success = true,
                    Message = $"Successfully spawned {message.AIType}",
                    SpawnedActorId = actor.ActorId
                };

                await session.SendAsync(MessageType.AISpawnResponse, response);

                Console.WriteLine($"[AISpawnHandler] Spawned AI {actor.ActorId} ({message.AIType})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AISpawnHandler] Error: {ex.Message}");

                var response = new AISpawnResponse
                {
                    Success = false,
                    Message = $"Failed to spawn AI: {ex.Message}",
                    SpawnedActorId = 0
                };

                await session.SendAsync(MessageType.AISpawnResponse, response);
            }
        }
    }

    /// <summary>
    /// AI 디버그 정보 요청 핸들러
    /// </summary>
    public class AIDebugInfoHandler : MessageHandler<AIDebugInfoRequest>
    {
        private readonly ServerAIManager _aiManager;
        private readonly SessionManager _sessions;

        public AIDebugInfoHandler(ServerAIManager aiManager, SessionManager sessions)
            : base(MessageType.AIDebugInfoRequest)
        {
            _aiManager = aiManager;
            _sessions = sessions;
        }

        protected override async Task HandleAsync(Session session, AIDebugInfoRequest message)
        {
            Console.WriteLine($"[AIDebugInfoHandler] {session.UserName} requests debug info for actor {message.ActorId}");

            try
            {
                var response = new AIDebugInfoResponse();

                var actors = message.ActorId == 0
                    ? _aiManager.GetAllActors()
                    : new[] { _aiManager.GetActor(message.ActorId) }.Where(a => a != null).Cast<ServerAIActor>();

                foreach (var actor in actors)
                {
                    var debugInfo = new AIActorDebugInfo
                    {
                        ActorId = actor.ActorId,
                        ActorName = actor.ActorName,
                        CurrentState = actor.State,
                        CurrentBehaviorTreeNode = "N/A", // Simplified server AI doesn't track BT nodes
                        AggroLevel = 0f, // TODO: Calculate from aggro list
                        PerceivedEntitiesCount = 0, // Server doesn't have perception system
                        LODLevel = "FullSpeed", // Server always updates at full speed
                        UpdateRate = 60f // Server tick rate
                    };

                    response.Actors.Add(debugInfo);
                }

                await session.SendAsync(MessageType.AIDebugInfoResponse, response);

                Console.WriteLine($"[AIDebugInfoHandler] Sent debug info for {response.Actors.Count} actors");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIDebugInfoHandler] Error: {ex.Message}");
            }
        }
    }
}
