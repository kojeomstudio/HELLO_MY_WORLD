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
    [Serializable]
    public class Vector3
    {
        public float X;
        public float Y;
        public float Z;

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
    [Serializable]
    public class AIActorInfo
    {
        public int ActorId;
        public string ActorName = string.Empty;
        public Vector3 Position = new Vector3();
        public AIState State;
        public int TargetId;
        public int Health;
        public int MaxHealth;
    }

    /// <summary>
    /// AI 상태 동기화 브로드캐스트 (game.proto AIStateSyncBroadcast)
    /// </summary>
    [Serializable]
    public class AIStateSyncBroadcast
    {
        public List<AIActorInfo> Actors = new List<AIActorInfo>();
        public long Timestamp;
    }

    /// <summary>
    /// AI 공격 이벤트 브로드캐스트 (game.proto AIAttackEventBroadcast)
    /// </summary>
    [Serializable]
    public class AIAttackEventBroadcast
    {
        public int AttackerId;
        public int TargetId;
        public int Damage;
        public Vector3 AttackPosition = new Vector3();
        public long Timestamp;
    }

    /// <summary>
    /// AI 사망 이벤트 브로드캐스트 (game.proto AIDeathEventBroadcast)
    /// </summary>
    [Serializable]
    public class AIDeathEventBroadcast
    {
        public int ActorId;
        public int KillerId;
        public Vector3 DeathPosition = new Vector3();
        public long Timestamp;
    }

    /// <summary>
    /// AI 스폰 요청 (game.proto AISpawnRequest)
    /// </summary>
    [Serializable]
    public class AISpawnRequest
    {
        public string AIType = string.Empty;
        public Vector3 SpawnPosition = new Vector3();
        public string WorldId = string.Empty;
    }

    /// <summary>
    /// AI 스폰 응답 (game.proto AISpawnResponse)
    /// </summary>
    [Serializable]
    public class AISpawnResponse
    {
        public bool Success;
        public string Message = string.Empty;
        public int SpawnedActorId;
    }

    /// <summary>
    /// AI 디버그 정보 요청 (game.proto AIDebugInfoRequest)
    /// </summary>
    [Serializable]
    public class AIDebugInfoRequest
    {
        public int ActorId;
    }

    /// <summary>
    /// AI 액터 디버그 정보 (game.proto AIActorDebugInfo)
    /// </summary>
    [Serializable]
    public class AIActorDebugInfo
    {
        public int ActorId;
        public string ActorName = string.Empty;
        public AIState CurrentState;
        public string CurrentBehaviorTreeNode = string.Empty;
        public float AggroLevel;
        public int PerceivedEntitiesCount;
        public string LodLevel = string.Empty;
        public float UpdateRate;
    }

    /// <summary>
    /// AI 디버그 정보 응답 (game.proto AIDebugInfoResponse)
    /// </summary>
    [Serializable]
    public class AIDebugInfoResponse
    {
        public List<AIActorDebugInfo> Actors = new List<AIActorDebugInfo>();
    }
}
