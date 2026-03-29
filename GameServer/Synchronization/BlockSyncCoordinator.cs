using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Utils;

namespace GameServer.Synchronization
{
    /// <summary>
    /// 블록 변경 이벤트
    /// </summary>
    public class BlockChangeEvent : ISyncable
    {
        public Vector3 Position { get; set; }
        public int OldBlockId { get; set; }
        public int NewBlockId { get; set; }
        public int Metadata { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public long Version { get; set; }
        public DateTime LastModified { get; set; }
        public string ChangeReason { get; set; } = string.Empty;

        public string GetStateHash()
        {
            return $"{Position.X}_{Position.Y}_{Position.Z}_{Version}_{NewBlockId}";
        }

        public string GetPositionKey() => $"{(int)Position.X}_{(int)Position.Y}_{(int)Position.Z}";
    }

    /// <summary>
    /// 블록 파괴 진행도
    /// </summary>
    public class BlockBreakProgress
    {
        public Vector3 Position { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public float Progress { get; set; } // 0.0 - 1.0
        public DateTime StartTime { get; set; }
        public DateTime LastUpdate { get; set; }
        public int ToolItemId { get; set; }
        public int SequenceId { get; set; }
    }

    /// <summary>
    /// 블록 동기화 코디네이터
    /// - Optimistic Concurrency Control (낙관적 잠금)
    /// - 블록 파괴 진행도 브로드캐스트
    /// - 충돌 해결 메커니즘
    /// </summary>
    public class BlockSyncCoordinator
    {
        private readonly Logger _logger = Logger.Instance;
        private readonly ConcurrentDictionary<string, BlockChangeEvent> _blockStates;
        private readonly ConcurrentDictionary<string, BlockBreakProgress> _breakProgress;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _blockLocks;
        private readonly PerformanceMonitor _perfMonitor;

        // 설정
        private const int MaxConcurrentBlockOperations = 100;
        private const double BlockInteractionMaxDistance = 6.0; // 블록 단위
        private const float BreakProgressBroadcastInterval = 0.2f; // 초
        private const float BreakProgressTimeout = 10.0f; // 초

        private readonly SemaphoreSlim _globalOperationSemaphore;

        public BlockSyncCoordinator()
        {
            _blockStates = new ConcurrentDictionary<string, BlockChangeEvent>();
            _breakProgress = new ConcurrentDictionary<string, BlockBreakProgress>();
            _blockLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
            _perfMonitor = new PerformanceMonitor();
            _globalOperationSemaphore = new SemaphoreSlim(MaxConcurrentBlockOperations);
        }

        /// <summary>
        /// 블록 배치 요청 처리 (충돌 방지)
        /// </summary>
        public async Task<SyncResultDetail> HandleBlockPlace(
            string playerId,
            Vector3 position,
            int blockId,
            int metadata,
            long clientVersion,
            Vector3 playerPosition)
        {
            var posKey = GetPositionKey(position);

            return await _perfMonitor.Measure($"BlockPlace_{posKey}", async () =>
            {
                // 1. 거리 검증
                var distance = Vector3.Distance(position, playerPosition);
                if (distance > BlockInteractionMaxDistance)
                {
                    _logger.Warning("BlockSync",
                        $"Player {playerId} tried to place block too far away: {distance:F2} blocks");

                    return new SyncResultDetail
                    {
                        Result = SyncResult.ValidationFailed,
                        Message = $"Block too far away (max: {BlockInteractionMaxDistance} blocks)",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // 2. 블록별 잠금 획득
                var blockLock = _blockLocks.GetOrAdd(posKey, _ => new SemaphoreSlim(1, 1));

                try
                {
                    await _globalOperationSemaphore.WaitAsync();

                    try
                    {
                        await blockLock.WaitAsync();

                        // 3. 현재 블록 상태 확인
                        if (_blockStates.TryGetValue(posKey, out var currentState))
                        {
                            // 버전 확인 (Optimistic Concurrency Control)
                            if (clientVersion > 0 && currentState.Version != clientVersion)
                            {
                                _logger.Warning("BlockSync",
                                    $"Version conflict at {posKey}: client={clientVersion}, server={currentState.Version}");

                                return new SyncResultDetail
                                {
                                    Result = SyncResult.Conflict,
                                    Message = "Another player modified this block",
                                    ServerVersion = currentState.Version,
                                    ConflictData = currentState,
                                    Timestamp = DateTime.UtcNow
                                };
                            }

                            // 블록이 이미 배치되어 있는 경우
                            if (currentState.NewBlockId != 0) // 0 = Air
                            {
                                _logger.Debug("BlockSync",
                                    $"Block already exists at {posKey}: {currentState.NewBlockId}");

                                return new SyncResultDetail
                                {
                                    Result = SyncResult.Conflict,
                                    Message = "Block already occupied",
                                    ServerVersion = currentState.Version,
                                    ConflictData = currentState,
                                    Timestamp = DateTime.UtcNow
                                };
                            }
                        }

                        // 4. 블록 배치
                        var blockEvent = new BlockChangeEvent
                        {
                            Position = position,
                            OldBlockId = currentState?.NewBlockId ?? 0,
                            NewBlockId = blockId,
                            Metadata = metadata,
                            PlayerId = playerId,
                            Version = (currentState?.Version ?? 0) + 1,
                            LastModified = DateTime.UtcNow,
                            ChangeReason = "PLAYER_PLACE"
                        };

                        _blockStates[posKey] = blockEvent;

                        _logger.Info("BlockSync",
                            $"Player {playerId} placed block {blockId} at {posKey} (version: {blockEvent.Version})");

                        return new SyncResultDetail
                        {
                            Result = SyncResult.Success,
                            Message = "Block placed successfully",
                            ServerVersion = blockEvent.Version,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                    finally
                    {
                        blockLock.Release();
                    }
                }
                finally
                {
                    _globalOperationSemaphore.Release();
                }
            });
        }

        /// <summary>
        /// 블록 파괴 시작
        /// </summary>
        public SyncResultDetail StartBlockBreak(
            string playerId,
            Vector3 position,
            int toolItemId,
            int sequenceId,
            Vector3 playerPosition)
        {
            var posKey = GetPositionKey(position);

            // 거리 검증
            var distance = Vector3.Distance(position, playerPosition);
            if (distance > BlockInteractionMaxDistance)
            {
                return new SyncResultDetail
                {
                    Result = SyncResult.ValidationFailed,
                    Message = "Block too far away",
                    Timestamp = DateTime.UtcNow
                };
            }

            // 진행도 추적 시작
            var progressKey = $"{playerId}_{posKey}";
            var progress = new BlockBreakProgress
            {
                Position = position,
                PlayerId = playerId,
                Progress = 0.0f,
                StartTime = DateTime.UtcNow,
                LastUpdate = DateTime.UtcNow,
                ToolItemId = toolItemId,
                SequenceId = sequenceId
            };

            _breakProgress[progressKey] = progress;

            _logger.Debug("BlockSync",
                $"Player {playerId} started breaking block at {posKey} (seq: {sequenceId})");

            return new SyncResultDetail
            {
                Result = SyncResult.Success,
                Message = "Block break started",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 블록 파괴 진행도 업데이트
        /// </summary>
        public SyncResultDetail UpdateBlockBreakProgress(
            string playerId,
            Vector3 position,
            float progress,
            int sequenceId)
        {
            var posKey = GetPositionKey(position);
            var progressKey = $"{playerId}_{posKey}";

            if (_breakProgress.TryGetValue(progressKey, out var existing))
            {
                // 시퀀스 ID 검증
                if (existing.SequenceId != sequenceId)
                {
                    return new SyncResultDetail
                    {
                        Result = SyncResult.ValidationFailed,
                        Message = "Sequence ID mismatch",
                        Timestamp = DateTime.UtcNow
                    };
                }

                existing.Progress = Math.Clamp(progress, 0.0f, 1.0f);
                existing.LastUpdate = DateTime.UtcNow;

                return new SyncResultDetail
                {
                    Result = SyncResult.Success,
                    Message = "Progress updated",
                    Timestamp = DateTime.UtcNow
                };
            }

            return new SyncResultDetail
            {
                Result = SyncResult.ValidationFailed,
                Message = "Block break not started",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 블록 파괴 완료
        /// </summary>
        public async Task<SyncResultDetail> CompleteBlockBreak(
            string playerId,
            Vector3 position,
            int sequenceId,
            long clientVersion)
        {
            var posKey = GetPositionKey(position);
            var progressKey = $"{playerId}_{posKey}";

            // 진행도 검증
            if (!_breakProgress.TryRemove(progressKey, out var progress))
            {
                return new SyncResultDetail
                {
                    Result = SyncResult.ValidationFailed,
                    Message = "Block break not started or already completed",
                    Timestamp = DateTime.UtcNow
                };
            }

            if (progress.SequenceId != sequenceId)
            {
                return new SyncResultDetail
                {
                    Result = SyncResult.ValidationFailed,
                    Message = "Sequence ID mismatch",
                    Timestamp = DateTime.UtcNow
                };
            }

            // 블록 제거 (블록 배치와 동일한 메커니즘 사용)
            var blockLock = _blockLocks.GetOrAdd(posKey, _ => new SemaphoreSlim(1, 1));

            try
            {
                await blockLock.WaitAsync();

                if (_blockStates.TryGetValue(posKey, out var currentState))
                {
                    // 버전 확인
                    if (clientVersion > 0 && currentState.Version != clientVersion)
                    {
                        return new SyncResultDetail
                        {
                            Result = SyncResult.Conflict,
                            Message = "Block was modified by another player",
                            ServerVersion = currentState.Version,
                            ConflictData = currentState,
                            Timestamp = DateTime.UtcNow
                        };
                    }

                    // 블록 제거 (Air로 변경)
                    var blockEvent = new BlockChangeEvent
                    {
                        Position = position,
                        OldBlockId = currentState.NewBlockId,
                        NewBlockId = 0, // Air
                        Metadata = 0,
                        PlayerId = playerId,
                        Version = currentState.Version + 1,
                        LastModified = DateTime.UtcNow,
                        ChangeReason = "PLAYER_BREAK"
                    };

                    _blockStates[posKey] = blockEvent;

                    _logger.Info("BlockSync",
                        $"Player {playerId} broke block at {posKey} (version: {blockEvent.Version})");

                    return new SyncResultDetail
                    {
                        Result = SyncResult.Success,
                        Message = "Block broken successfully",
                        ServerVersion = blockEvent.Version,
                        Timestamp = DateTime.UtcNow
                    };
                }

                return new SyncResultDetail
                {
                    Result = SyncResult.ValidationFailed,
                    Message = "Block not found",
                    Timestamp = DateTime.UtcNow
                };
            }
            finally
            {
                blockLock.Release();
            }
        }

        /// <summary>
        /// 블록 파괴 취소
        /// </summary>
        public SyncResultDetail AbortBlockBreak(string playerId, Vector3 position)
        {
            var posKey = GetPositionKey(position);
            var progressKey = $"{playerId}_{posKey}";

            if (_breakProgress.TryRemove(progressKey, out _))
            {
                _logger.Debug("BlockSync", $"Player {playerId} aborted breaking block at {posKey}");

                return new SyncResultDetail
                {
                    Result = SyncResult.Success,
                    Message = "Block break aborted",
                    Timestamp = DateTime.UtcNow
                };
            }

            return new SyncResultDetail
            {
                Result = SyncResult.ValidationFailed,
                Message = "Block break not in progress",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 브로드캐스트할 블록 파괴 진행도 조회
        /// </summary>
        public List<BlockBreakProgress> GetBreakProgressForBroadcast(DateTime since)
        {
            return _breakProgress.Values
                .Where(p => p.LastUpdate >= since)
                .ToList();
        }

        /// <summary>
        /// 오래된 진행도 정리 (타임아웃)
        /// </summary>
        public void CleanupExpiredBreakProgress()
        {
            var now = DateTime.UtcNow;
            var expired = _breakProgress
                .Where(kvp => (now - kvp.Value.LastUpdate).TotalSeconds > BreakProgressTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                if (_breakProgress.TryRemove(key, out var progress))
                {
                    _logger.Debug("BlockSync",
                        $"Removed expired break progress: {progress.PlayerId} at {GetPositionKey(progress.Position)}");
                }
            }

            if (expired.Count > 0)
            {
                _logger.Info("BlockSync", $"Cleaned up {expired.Count} expired break progress entries");
            }
        }

        /// <summary>
        /// 플레이어 연결 해제 시 정리
        /// </summary>
        public void CleanupPlayer(string playerId)
        {
            var playerProgress = _breakProgress
                .Where(kvp => kvp.Value.PlayerId == playerId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in playerProgress)
            {
                _breakProgress.TryRemove(key, out _);
            }

            if (playerProgress.Count > 0)
            {
                _logger.Info("BlockSync",
                    $"Cleaned up {playerProgress.Count} break progress entries for disconnected player {playerId}");
            }
        }

        /// <summary>
        /// 동기화 통계 조회
        /// </summary>
        public BlockSyncStatistics GetStatistics()
        {
            return new BlockSyncStatistics
            {
                TotalBlocks = _blockStates.Count,
                ActiveBreakProgress = _breakProgress.Count,
                UniquePlayersBreaking = _breakProgress.Values.Select(p => p.PlayerId).Distinct().Count()
            };
        }

        private string GetPositionKey(Vector3 position)
        {
            return $"{(int)position.X}_{(int)position.Y}_{(int)position.Z}";
        }
    }

    /// <summary>
    /// 블록 동기화 통계
    /// </summary>
    public class BlockSyncStatistics
    {
        public int TotalBlocks { get; set; }
        public int ActiveBreakProgress { get; set; }
        public int UniquePlayersBreaking { get; set; }
    }
}
