using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameProtocol;
using ProtoVector3 = GameProtocol.Vector3;
using ServerVector3 = GameServerApp.Vector3;

namespace GameServerApp.AI
{
    /// <summary>
    /// Server-side AI Manager
    ///
    /// 기능:
    /// - Server-authoritative AI 관리
    /// - AI 상태 업데이트 및 동기화
    /// - AI 스폰/제거
    /// - LOD 기반 업데이트 스케줄링
    ///
    /// 클라이언트는 AI 상태만 수신하고 애니메이션/비주얼만 처리
    /// </summary>
    public class ServerAIManager
    {
        private Dictionary<int, ServerAIActor> _aiActors = new Dictionary<int, ServerAIActor>();
        private int _nextActorId = 1000; // AI Actor ID는 1000부터 시작
        private float _syncInterval = 0.1f; // 100ms마다 동기화
        private DateTime _lastSyncTime = DateTime.UtcNow;

        /// <summary>
        /// AI 액터 스폰
        /// </summary>
        public ServerAIActor SpawnAI(string aiType, ProtoVector3 spawnPosition, string worldId)
        {
            int actorId = _nextActorId++;
            var serverPosition = new ServerVector3(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);

            ServerAIActor actor = new ServerAIActor
            {
                ActorId = actorId,
                ActorName = $"{aiType}_{actorId}",
                AIType = aiType,
                Position = serverPosition,
                WorldId = worldId,
                State = AIState.AiIdle,
                Health = GetMaxHealthForType(aiType),
                MaxHealth = GetMaxHealthForType(aiType),
                AttackPower = GetAttackPowerForType(aiType),
                DetectionRange = GetDetectionRangeForType(aiType),
                AttackRange = GetAttackRangeForType(aiType)
            };

            _aiActors[actorId] = actor;

            Console.WriteLine($"[ServerAIManager] Spawned {aiType} (ID: {actorId}) at {serverPosition.X},{serverPosition.Y},{serverPosition.Z}");

            return actor;
        }

        /// <summary>
        /// AI 액터 제거
        /// </summary>
        public bool RemoveAI(int actorId)
        {
            if (_aiActors.ContainsKey(actorId))
            {
                _aiActors.Remove(actorId);
                Console.WriteLine($"[ServerAIManager] Removed AI actor {actorId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// AI 업데이트 (모든 AI)
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var actor in _aiActors.Values)
            {
                UpdateSingleAI(actor, deltaTime);
            }

            // 동기화 타이머 체크
            if ((DateTime.UtcNow - _lastSyncTime).TotalSeconds >= _syncInterval)
            {
                _lastSyncTime = DateTime.UtcNow;
                // 동기화는 외부에서 BroadcastAIState() 호출하여 처리
            }
        }

        /// <summary>
        /// 단일 AI 업데이트
        /// </summary>
        private void UpdateSingleAI(ServerAIActor actor, float deltaTime)
        {
            if (actor.Health <= 0)
            {
                actor.State = AIState.AiDead;
                return;
            }

            // 간소화된 AI 로직
            switch (actor.State)
            {
                case AIState.AiIdle:
                case AIState.AiWander:
                    // TODO: 플레이어 감지
                    // 임시: 랜덤 배회
                    actor.WanderTimer += deltaTime;
                    if (actor.WanderTimer >= 3.0f)
                    {
                        actor.WanderTimer = 0f;
                        // 랜덤 위치로 이동
                        Random rand = new Random();
                        actor.TargetPosition = new ServerVector3(
                            actor.Position.X + rand.NextDouble() * 10 - 5,
                            actor.Position.Y,
                            actor.Position.Z + rand.NextDouble() * 10 - 5);
                        actor.State = AIState.AiWander;
                    }
                    break;

                case AIState.AiChase:
                    // TODO: 타겟 추적
                    // 임시: 타겟이 없으면 Idle로
                    if (actor.TargetId == 0)
                    {
                        actor.State = AIState.AiIdle;
                    }
                    break;

                case AIState.AiAttack:
                    // TODO: 공격 실행
                    actor.AttackCooldownTimer -= deltaTime;
                    if (actor.AttackCooldownTimer <= 0f)
                    {
                        // 공격 가능
                        actor.AttackCooldownTimer = 2.0f; // 2초 쿨다운
                    }
                    break;

                case AIState.AiFlee:
                    // TODO: 도주 로직
                    break;

                case AIState.AiDead:
                    // 사망 상태 - 아무것도 안 함
                    break;
            }

            // 위치 업데이트 (간단한 이동)
            if (actor.TargetPosition != null)
            {
                double moveSpeed = 5.0 * deltaTime; // 5 units/sec
                actor.Position.X += (actor.TargetPosition.X - actor.Position.X) * moveSpeed;
                actor.Position.Z += (actor.TargetPosition.Z - actor.Position.Z) * moveSpeed;

                // 목표 도달 체크
                double distance = Math.Sqrt(
                    Math.Pow(actor.TargetPosition.X - actor.Position.X, 2) +
                    Math.Pow(actor.TargetPosition.Z - actor.Position.Z, 2)
                );

                if (distance < 0.5)
                {
                    actor.TargetPosition = null;
                    actor.State = AIState.AiIdle;
                }
            }
        }

        /// <summary>
        /// AI 상태 동기화 메시지 생성
        /// </summary>
        public AIStateSyncBroadcast GetStateSyncBroadcast()
        {
            var broadcast = new AIStateSyncBroadcast
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            foreach (var actor in _aiActors.Values)
            {
                broadcast.Actors.Add(new AIActorInfo
                {
                    ActorId = actor.ActorId,
                    ActorName = actor.ActorName,
                    Position = new ProtoVector3((float)actor.Position.X, (float)actor.Position.Y, (float)actor.Position.Z),
                    State = actor.State,
                    TargetId = actor.TargetId,
                    Health = actor.Health,
                    MaxHealth = actor.MaxHealth
                });
            }

            return broadcast;
        }

        /// <summary>
        /// AI 데미지 처리
        /// </summary>
        public AIAttackEventBroadcast? ProcessDamage(int attackerId, int targetId, int damage)
        {
            if (!_aiActors.ContainsKey(targetId))
                return null;

            var target = _aiActors[targetId];
            target.Health -= damage;

            Console.WriteLine($"[ServerAIManager] AI {targetId} took {damage} damage (Health: {target.Health}/{target.MaxHealth})");

            // 어그로 추가 (향후 구현)
            target.TargetId = attackerId;
            if (target.State == AIState.AiIdle || target.State == AIState.AiWander)
            {
                target.State = AIState.AiChase;
            }

            // 사망 체크
            if (target.Health <= 0)
            {
                target.State = AIState.AiDead;
                return new AIAttackEventBroadcast
                {
                    AttackerId = attackerId,
                    TargetId = targetId,
                    Damage = damage,
                    AttackPosition = new ProtoVector3((float)target.Position.X, (float)target.Position.Y, (float)target.Position.Z),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }

            return new AIAttackEventBroadcast
            {
                AttackerId = attackerId,
                TargetId = targetId,
                Damage = damage,
                AttackPosition = new ProtoVector3((float)target.Position.X, (float)target.Position.Y, (float)target.Position.Z),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// 모든 AI 액터 가져오기
        /// </summary>
        public IEnumerable<ServerAIActor> GetAllActors()
        {
            return _aiActors.Values;
        }

        /// <summary>
        /// 특정 AI 액터 가져오기
        /// </summary>
        public ServerAIActor? GetActor(int actorId)
        {
            return _aiActors.ContainsKey(actorId) ? _aiActors[actorId] : null;
        }

        // ============================================================
        // AI 타입별 스탯 설정
        // ============================================================

        private int GetMaxHealthForType(string aiType)
        {
            return aiType switch
            {
                "Aggressive" => 100,
                "Defensive" => 120,
                "Coward" => 80,
                "Boss" => 500,
                "Flying" => 90,
                "Ranged" => 85,
                _ => 100
            };
        }

        private int GetAttackPowerForType(string aiType)
        {
            return aiType switch
            {
                "Aggressive" => 15,
                "Defensive" => 12,
                "Coward" => 5,
                "Boss" => 30,
                "Flying" => 10,
                "Ranged" => 12,
                _ => 10
            };
        }

        private float GetDetectionRangeForType(string aiType)
        {
            return aiType switch
            {
                "Aggressive" => 20f,
                "Defensive" => 15f,
                "Coward" => 25f,
                "Boss" => 30f,
                "Flying" => 25f,
                "Ranged" => 20f,
                _ => 20f
            };
        }

        private float GetAttackRangeForType(string aiType)
        {
            return aiType switch
            {
                "Aggressive" => 3f,
                "Defensive" => 3f,
                "Coward" => 0f, // 공격 안 함
                "Boss" => 5f,
                "Flying" => 10f, // 원거리
                "Ranged" => 15f, // 원거리
                _ => 3f
            };
        }
    }

    /// <summary>
    /// Server-side AI Actor 데이터
    /// </summary>
    public class ServerAIActor
    {
        public int ActorId { get; set; }
        public string ActorName { get; set; } = "";
        public string AIType { get; set; } = "";
        public string WorldId { get; set; } = "";
        public ServerVector3 Position { get; set; } = new ServerVector3();
        public ServerVector3? TargetPosition { get; set; }
        public AIState State { get; set; } = AIState.AiIdle;
        public int TargetId { get; set; } = 0;
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int AttackPower { get; set; }
        public float DetectionRange { get; set; }
        public float AttackRange { get; set; }
        public float WanderTimer { get; set; } = 0f;
        public float AttackCooldownTimer { get; set; } = 0f;
    }
}
