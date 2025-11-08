using System;
using System.Collections.Generic;
using ProtoBuf;

namespace GameProtocol
{
    /// <summary>
    /// AI 상태 enum (game.proto AIState)
    /// </summary>
    [ProtoContract]
    public enum AIState
    {
        [ProtoEnum] AiIdle = 0,
        [ProtoEnum] AiWander = 1,
        [ProtoEnum] AiChase = 2,
        [ProtoEnum] AiAttack = 3,
        [ProtoEnum] AiFlee = 4,
        [ProtoEnum] AiDead = 5
    }

    /// <summary>
    /// Vector3 (game.proto Vector3)
    /// </summary>
    [ProtoContract]
    public class Vector3
    {
        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
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
    [ProtoContract]
    public class AIActorInfo
    {
        [ProtoMember(1)]
        public int ActorId { get; set; }

        [ProtoMember(2)]
        public string ActorName { get; set; } = string.Empty;

        [ProtoMember(3)]
        public Vector3 Position { get; set; } = new Vector3();

        [ProtoMember(4)]
        public AIState State { get; set; }

        [ProtoMember(5)]
        public int TargetId { get; set; }

        [ProtoMember(6)]
        public int Health { get; set; }

        [ProtoMember(7)]
        public int MaxHealth { get; set; }
    }

    /// <summary>
    /// AI 상태 동기화 브로드캐스트 (game.proto AIStateSyncBroadcast)
    /// </summary>
    [ProtoContract]
    public class AIStateSyncBroadcast
    {
        [ProtoMember(1)]
        public List<AIActorInfo> Actors { get; set; } = new List<AIActorInfo>();

        [ProtoMember(2)]
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// AI 공격 이벤트 브로드캐스트 (game.proto AIAttackEventBroadcast)
    /// </summary>
    [ProtoContract]
    public class AIAttackEventBroadcast
    {
        [ProtoMember(1)]
        public int AttackerId { get; set; }

        [ProtoMember(2)]
        public int TargetId { get; set; }

        [ProtoMember(3)]
        public int Damage { get; set; }

        [ProtoMember(4)]
        public Vector3 AttackPosition { get; set; } = new Vector3();

        [ProtoMember(5)]
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// AI 사망 이벤트 브로드캐스트 (game.proto AIDeathEventBroadcast)
    /// </summary>
    [ProtoContract]
    public class AIDeathEventBroadcast
    {
        [ProtoMember(1)]
        public int ActorId { get; set; }

        [ProtoMember(2)]
        public int KillerId { get; set; }

        [ProtoMember(3)]
        public Vector3 DeathPosition { get; set; } = new Vector3();

        [ProtoMember(4)]
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// AI 스폰 요청 (game.proto AISpawnRequest)
    /// </summary>
    [ProtoContract]
    public class AISpawnRequest
    {
        [ProtoMember(1)]
        public string AIType { get; set; } = string.Empty;

        [ProtoMember(2)]
        public Vector3 SpawnPosition { get; set; } = new Vector3();

        [ProtoMember(3)]
        public string WorldId { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI 스폰 응답 (game.proto AISpawnResponse)
    /// </summary>
    [ProtoContract]
    public class AISpawnResponse
    {
        [ProtoMember(1)]
        public bool Success { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; } = string.Empty;

        [ProtoMember(3)]
        public int SpawnedActorId { get; set; }
    }

    /// <summary>
    /// AI 디버그 정보 요청 (game.proto AIDebugInfoRequest)
    /// </summary>
    [ProtoContract]
    public class AIDebugInfoRequest
    {
        [ProtoMember(1)]
        public int ActorId { get; set; }
    }

    /// <summary>
    /// AI 액터 디버그 정보 (game.proto AIActorDebugInfo)
    /// </summary>
    [ProtoContract]
    public class AIActorDebugInfo
    {
        [ProtoMember(1)]
        public int ActorId { get; set; }

        [ProtoMember(2)]
        public string ActorName { get; set; } = string.Empty;

        [ProtoMember(3)]
        public AIState CurrentState { get; set; }

        [ProtoMember(4)]
        public string CurrentBehaviorTreeNode { get; set; } = string.Empty;

        [ProtoMember(5)]
        public float AggroLevel { get; set; }

        [ProtoMember(6)]
        public int PerceivedEntitiesCount { get; set; }

        [ProtoMember(7)]
        public string LodLevel { get; set; } = string.Empty;

        [ProtoMember(8)]
        public float UpdateRate { get; set; }
    }

    /// <summary>
    /// AI 디버그 정보 응답 (game.proto AIDebugInfoResponse)
    /// </summary>
    [ProtoContract]
    public class AIDebugInfoResponse
    {
        [ProtoMember(1)]
        public List<AIActorDebugInfo> Actors { get; set; } = new List<AIActorDebugInfo>();
    }
}
