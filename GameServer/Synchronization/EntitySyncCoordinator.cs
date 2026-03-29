using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using GameServer.Utils;

namespace GameServer.Synchronization
{
    /// <summary>
    /// 엔티티 동기화 상태
    /// </summary>
    public class EntitySyncState : ISyncable
    {
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Rotation { get; set; }
        public long Version { get; set; }
        public DateTime LastModified { get; set; }
        public float Health { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public string GetStateHash()
        {
            return $"{Version}_{Position.X:F2}_{Position.Y:F2}_{Position.Z:F2}_{LastModified.Ticks}";
        }
    }

    /// <summary>
    /// 엔티티 움직임 검증 정보
    /// </summary>
    public class EntityMovementValidator
    {
        private const float MaxSpeed = 48.0f; // 블록/초
        private const float MaxAcceleration = 50.0f; // 블록/초²
        private const float TeleportThreshold = 64.0f; // 텔레포트 판정 거리

        private readonly ConcurrentDictionary<string, MovementHistory> _history;
        private readonly Logger _logger = Logger.Instance;

        public EntityMovementValidator()
        {
            _history = new ConcurrentDictionary<string, MovementHistory>();
        }

        public class MovementHistory
        {
            public Vector3 LastPosition { get; set; }
            public Vector3 LastVelocity { get; set; }
            public DateTime LastUpdateTime { get; set; }
            public int ViolationCount { get; set; }
        }

        /// <summary>
        /// 움직임 검증 (치팅 방지)
        /// </summary>
        public MovementValidationResult Validate(string entityId, Vector3 newPosition, Vector3 newVelocity)
        {
            var now = DateTime.UtcNow;
            var history = _history.GetOrAdd(entityId, _ => new MovementHistory
            {
                LastPosition = newPosition,
                LastVelocity = Vector3.Zero,
                LastUpdateTime = now,
                ViolationCount = 0
            });

            var deltaTime = (float)(now - history.LastUpdateTime).TotalSeconds;
            if (deltaTime < 0.001f) // 너무 빠른 업데이트
            {
                return new MovementValidationResult
                {
                    IsValid = false,
                    Reason = "Update too frequent",
                    SuggestedPosition = history.LastPosition
                };
            }

            // 1. 거리 검증
            var distance = Vector3.Distance(history.LastPosition, newPosition);
            var maxDistance = MaxSpeed * deltaTime;

            if (distance > TeleportThreshold)
            {
                // 텔레포트 의심 (정상적인 텔레포트는 별도 처리 필요)
                history.ViolationCount++;
                _logger.Warning("EntitySync",
                    $"Entity {entityId} possible teleport: {distance:F2} blocks in {deltaTime:F3}s");

                if (history.ViolationCount > 3)
                {
                    return new MovementValidationResult
                    {
                        IsValid = false,
                        Reason = "Teleport hack detected",
                        SuggestedPosition = history.LastPosition
                    };
                }
            }
            else if (distance > maxDistance * 1.2f) // 20% 여유
            {
                // 속도 제한 위반
                history.ViolationCount++;
                _logger.Warning("EntitySync",
                    $"Entity {entityId} speed violation: {distance:F2} blocks in {deltaTime:F3}s (max: {maxDistance:F2})");

                return new MovementValidationResult
                {
                    IsValid = false,
                    Reason = "Speed limit exceeded",
                    SuggestedPosition = history.LastPosition + Vector3.Normalize(newPosition - history.LastPosition) * maxDistance
                };
            }

            // 2. 가속도 검증
            var currentSpeed = distance / deltaTime;
            var lastSpeed = history.LastVelocity.Length();
            var acceleration = Math.Abs(currentSpeed - lastSpeed) / deltaTime;

            if (acceleration > MaxAcceleration)
            {
                history.ViolationCount++;
                _logger.Warning("EntitySync",
                    $"Entity {entityId} acceleration violation: {acceleration:F2} m/s²");

                return new MovementValidationResult
                {
                    IsValid = false,
                    Reason = "Acceleration limit exceeded",
                    SuggestedPosition = history.LastPosition
                };
            }

            // 3. 속도 벡터 검증
            var velocityMagnitude = newVelocity.Length();
            if (velocityMagnitude > MaxSpeed)
            {
                // 속도 정규화
                var normalizedVelocity = Vector3.Normalize(newVelocity) * MaxSpeed;

                return new MovementValidationResult
                {
                    IsValid = true,
                    IsAdjusted = true,
                    Reason = "Velocity normalized",
                    SuggestedPosition = newPosition,
                    SuggestedVelocity = normalizedVelocity
                };
            }

            // 검증 통과
            history.LastPosition = newPosition;
            history.LastVelocity = newVelocity;
            history.LastUpdateTime = now;
            history.ViolationCount = Math.Max(0, history.ViolationCount - 1); // 점진적 감소

            return new MovementValidationResult
            {
                IsValid = true,
                Reason = "Valid movement",
                SuggestedPosition = newPosition,
                SuggestedVelocity = newVelocity
            };
        }

        public void RemoveEntity(string entityId)
        {
            _history.TryRemove(entityId, out _);
        }
    }

    public class MovementValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsAdjusted { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Vector3 SuggestedPosition { get; set; }
        public Vector3? SuggestedVelocity { get; set; }
    }

    /// <summary>
    /// 엔티티 동기화 코디네이터
    /// - 클라이언트 예측 + 서버 조정
    /// - 위치 검증 및 치팅 방지
    /// - 틱 레이트 제한
    /// </summary>
    public class EntitySyncCoordinator
    {
        private readonly Logger _logger = Logger.Instance;
        private readonly ConcurrentDictionary<string, EntitySyncState> _entities;
        private readonly EntityMovementValidator _movementValidator;
        private readonly PerformanceMonitor _perfMonitor;

        // 설정
        private const float UpdateInterval = 0.05f; // 20 tick/s
        private const double BroadcastRange = 128.0; // 블록 단위
        private const int MaxEntitiesPerUpdate = 50;

        private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes;

        public EntitySyncCoordinator()
        {
            _entities = new ConcurrentDictionary<string, EntitySyncState>();
            _movementValidator = new EntityMovementValidator();
            _perfMonitor = new PerformanceMonitor();
            _lastUpdateTimes = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// 엔티티 스폰 처리
        /// </summary>
        public SyncResultDetail HandleEntitySpawn(EntitySyncState entity)
        {
            return _perfMonitor.Measure($"EntitySpawn_{entity.EntityId}", () =>
            {
                entity.Version = 1;
                entity.LastModified = DateTime.UtcNow;

                if (_entities.TryAdd(entity.EntityId, entity))
                {
                    _logger.Info("EntitySync",
                        $"Entity spawned: {entity.EntityId} ({entity.EntityType}) at ({entity.Position.X:F2}, {entity.Position.Y:F2}, {entity.Position.Z:F2})");

                    return new SyncResultDetail
                    {
                        Result = SyncResult.Success,
                        Message = "Entity spawned",
                        ServerVersion = entity.Version,
                        Timestamp = DateTime.UtcNow
                    };
                }

                _logger.Error("EntitySync", $"Failed to spawn entity: {entity.EntityId} (already exists)");
                return new SyncResultDetail
                {
                    Result = SyncResult.Conflict,
                    Message = "Entity already exists",
                    Timestamp = DateTime.UtcNow
                };
            });
        }

        /// <summary>
        /// 엔티티 업데이트 처리 (틱 레이트 제한 + 위치 검증)
        /// </summary>
        public SyncResultDetail HandleEntityUpdate(
            string entityId,
            Vector3 newPosition,
            Vector3 newVelocity,
            Vector3 newRotation,
            long clientVersion)
        {
            return _perfMonitor.Measure($"EntityUpdate_{entityId}", () =>
            {
                var now = DateTime.UtcNow;

                // 1. 틱 레이트 제한
                if (_lastUpdateTimes.TryGetValue(entityId, out var lastUpdateTime))
                {
                    var timeSinceLastUpdate = (now - lastUpdateTime).TotalSeconds;
                    if (timeSinceLastUpdate < UpdateInterval)
                    {
                        return new SyncResultDetail
                        {
                            Result = SyncResult.RateLimited,
                            Message = $"Update too frequent (wait {UpdateInterval - timeSinceLastUpdate:F3}s)",
                            Timestamp = now
                        };
                    }
                }

                // 2. 엔티티 존재 확인
                if (!_entities.TryGetValue(entityId, out var entity))
                {
                    _logger.Warning("EntitySync", $"Entity not found: {entityId}");
                    return new SyncResultDetail
                    {
                        Result = SyncResult.ValidationFailed,
                        Message = "Entity not found",
                        Timestamp = now
                    };
                }

                // 3. 버전 확인 (Optimistic Concurrency Control)
                if (clientVersion > 0 && entity.Version != clientVersion)
                {
                    _logger.Warning("EntitySync",
                        $"Version mismatch for entity {entityId}: client={clientVersion}, server={entity.Version}");

                    return new SyncResultDetail
                    {
                        Result = SyncResult.Conflict,
                        Message = "Version mismatch - client needs resync",
                        ServerVersion = entity.Version,
                        ConflictData = entity,
                        Timestamp = now
                    };
                }

                // 4. 움직임 검증
                var validation = _movementValidator.Validate(entityId, newPosition, newVelocity);

                if (!validation.IsValid)
                {
                    _logger.Warning("EntitySync",
                        $"Invalid movement for entity {entityId}: {validation.Reason}");

                    // 서버가 조정한 위치 반환
                    return new SyncResultDetail
                    {
                        Result = SyncResult.ValidationFailed,
                        Message = validation.Reason,
                        ServerVersion = entity.Version,
                        ConflictData = new
                        {
                            Position = validation.SuggestedPosition,
                            Velocity = validation.SuggestedVelocity ?? Vector3.Zero
                        },
                        Timestamp = now
                    };
                }

                // 5. 엔티티 상태 업데이트
                entity.Position = validation.SuggestedPosition;
                entity.Velocity = validation.SuggestedVelocity ?? newVelocity;
                entity.Rotation = newRotation;
                entity.Version++;
                entity.LastModified = now;

                _lastUpdateTimes[entityId] = now;

                var resultMessage = validation.IsAdjusted
                    ? $"Entity updated (adjusted: {validation.Reason})"
                    : "Entity updated";

                return new SyncResultDetail
                {
                    Result = SyncResult.Success,
                    Message = resultMessage,
                    ServerVersion = entity.Version,
                    ConflictData = validation.IsAdjusted
                        ? new { Position = entity.Position, Velocity = entity.Velocity }
                        : null,
                    Timestamp = now
                };
            });
        }

        /// <summary>
        /// 엔티티 디스폰 처리
        /// </summary>
        public SyncResultDetail HandleEntityDespawn(string entityId)
        {
            if (_entities.TryRemove(entityId, out var entity))
            {
                _movementValidator.RemoveEntity(entityId);
                _lastUpdateTimes.TryRemove(entityId, out _);

                _logger.Info("EntitySync", $"Entity despawned: {entityId} ({entity.EntityType})");

                return new SyncResultDetail
                {
                    Result = SyncResult.Success,
                    Message = "Entity despawned",
                    ServerVersion = entity.Version,
                    Timestamp = DateTime.UtcNow
                };
            }

            return new SyncResultDetail
            {
                Result = SyncResult.ValidationFailed,
                Message = "Entity not found",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 범위 내 엔티티 조회 (브로드캐스트용)
        /// </summary>
        public List<EntitySyncState> GetEntitiesInRange(Vector3 center, double range = BroadcastRange)
        {
            return _entities.Values
                .Where(e =>
                {
                    var distance = Vector3.Distance(e.Position, center);
                    return distance <= range;
                })
                .OrderBy(e => Vector3.Distance(e.Position, center))
                .Take(MaxEntitiesPerUpdate)
                .ToList();
        }

        /// <summary>
        /// 엔티티 상태 조회
        /// </summary>
        public EntitySyncState? GetEntity(string entityId)
        {
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }

        /// <summary>
        /// 동기화 통계 조회
        /// </summary>
        public EntitySyncStatistics GetStatistics()
        {
            return new EntitySyncStatistics
            {
                TotalEntities = _entities.Count,
                EntitiesByType = _entities.Values
                    .GroupBy(e => e.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageEntitiesPerUpdate = MaxEntitiesPerUpdate,
                UpdateRate = 1.0f / UpdateInterval
            };
        }
    }

    /// <summary>
    /// 엔티티 동기화 통계
    /// </summary>
    public class EntitySyncStatistics
    {
        public int TotalEntities { get; set; }
        public Dictionary<string, int> EntitiesByType { get; set; } = new();
        public int AverageEntitiesPerUpdate { get; set; }
        public float UpdateRate { get; set; }
    }
}
