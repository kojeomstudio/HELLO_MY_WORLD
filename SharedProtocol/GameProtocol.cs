using System;
using System.Collections.Generic;

namespace GameProtocol
{
    /// <summary>
    /// AI 상태 enum (game.proto AIState)
    /// </summary>
    public enum AIState
    {
        AiIdle = 0,
        AiWander = 1,
        AiChase = 2,
        AiAttack = 3,
        AiFlee = 4,
        AiDead = 5
    }

    /// <summary>
    /// Vector3 (game.proto Vector3)
    /// </summary>
    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3() { }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// AI 액터 정보 (game.proto AIActorInfo)
    /// </summary>
    public class AIActorInfo
    {
        public int ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public Vector3 Position { get; set; } = new Vector3();
        public AIState State { get; set; }
        public int TargetId { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
    }

    /// <summary>
    /// AI 상태 동기화 브로드캐스트 (game.proto AIStateSyncBroadcast)
    /// </summary>
    public class AIStateSyncBroadcast
    {
        public List<AIActorInfo> Actors { get; set; } = new List<AIActorInfo>();
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// AI 공격 이벤트 브로드캐스트 (game.proto AIAttackEventBroadcast)
    /// </summary>
    public class AIAttackEventBroadcast
    {
        public int AttackerId { get; set; }
        public int TargetId { get; set; }
        public int Damage { get; set; }
        public Vector3 AttackPosition { get; set; } = new Vector3();
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// AI 사망 이벤트 브로드캐스트 (game.proto AIDeathEventBroadcast)
    /// </summary>
    public class AIDeathEventBroadcast : AIAttackEventBroadcast
    {
        public int ActorId { get; set; }
        public int KillerId { get; set; }
        public Vector3 DeathPosition { get; set; } = new Vector3();
    }

    /// <summary>
    /// AI 스폰 요청 (game.proto AISpawnRequest)
    /// </summary>
    public class AISpawnRequest
    {
        public string AIType { get; set; } = string.Empty;
        public Vector3 SpawnPosition { get; set; } = new Vector3();
        public string WorldId { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI 스폰 응답 (game.proto AISpawnResponse)
    /// </summary>
    public class AISpawnResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SpawnedActorId { get; set; }
    }

    /// <summary>
    /// AI 디버그 정보 요청 (game.proto AIDebugInfoRequest)
    /// </summary>
    public class AIDebugInfoRequest
    {
        public int ActorId { get; set; }
    }

    /// <summary>
    /// AI 액터 디버그 정보 (game.proto AIActorDebugInfo)
    /// </summary>
    public class AIActorDebugInfo
    {
        public int ActorId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public AIState CurrentState { get; set; }
        public string CurrentBehaviorTreeNode { get; set; } = string.Empty;
        public float AggroLevel { get; set; }
        public int PerceivedEntitiesCount { get; set; }
        public string LodLevel { get; set; } = string.Empty;
        public float UpdateRate { get; set; }
    }

    /// <summary>
    /// AI 디버그 정보 응답 (game.proto AIDebugInfoResponse)
    /// </summary>
    public class AIDebugInfoResponse
    {
        public List<AIActorDebugInfo> Actors { get; set; } = new List<AIActorDebugInfo>();
    }
}
