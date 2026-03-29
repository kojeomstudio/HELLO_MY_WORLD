using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Utils;

namespace GameServer.Synchronization
{
    /// <summary>
    /// 청크 동기화 상태
    /// </summary>
    public class ChunkSyncState : ISyncable
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public long Version { get; set; }
        public DateTime LastModified { get; set; }
        public byte[]? CompressedData { get; set; }
        public string? BiomeData { get; set; }
        public DateTime GenerationTimestamp { get; set; }

        public string GetStateHash()
        {
            // 간단한 해시: 버전 + 타임스탬프
            return $"{Version}_{LastModified.Ticks}";
        }

        public string GetKey() => $"{ChunkX}_{ChunkZ}";
    }

    /// <summary>
    /// 플레이어별 청크 추적 정보
    /// </summary>
    public class PlayerChunkTracker
    {
        public string PlayerId { get; set; } = string.Empty;
        public ConcurrentDictionary<string, ChunkLoadInfo> LoadedChunks { get; } = new();
        public int ViewDistance { get; set; }
        public DateTime LastUpdate { get; set; }

        public class ChunkLoadInfo
        {
            public int ChunkX { get; set; }
            public int ChunkZ { get; set; }
            public long ClientVersion { get; set; }
            public DateTime LoadTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public int AccessCount { get; set; }
        }
    }

    /// <summary>
    /// 청크 동기화 코디네이터
    /// 유지보수에 유리하도록 단일 책임 원칙을 따름
    /// </summary>
    public class ChunkSyncCoordinator
    {
        private readonly Logger _logger = Logger.Instance;
        private readonly ConcurrentDictionary<string, ChunkSyncState> _chunkCache;
        private readonly ConcurrentDictionary<string, PlayerChunkTracker> _playerTrackers;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _chunkLocks;
        private readonly PerformanceMonitor _perfMonitor;

        // 설정
        private const int MaxConcurrentChunkLoads = 8;
        private const int ChunkCacheMaxSize = 1000;
        private const int ChunkTimeoutMinutes = 30;
        private const double MaxChunkLoadDistanceFromPlayer = 256.0; // 블록 단위

        private readonly SemaphoreSlim _globalLoadSemaphore;

        public ChunkSyncCoordinator()
        {
            _chunkCache = new ConcurrentDictionary<string, ChunkSyncState>();
            _playerTrackers = new ConcurrentDictionary<string, PlayerChunkTracker>();
            _chunkLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
            _perfMonitor = new PerformanceMonitor();
            _globalLoadSemaphore = new SemaphoreSlim(MaxConcurrentChunkLoads);
        }

        /// <summary>
        /// 청크 로드 요청 처리 (레이스 컨디션 방지)
        /// </summary>
        public async Task<SyncResultDetail> HandleChunkLoadRequest(
            string playerId,
            int chunkX,
            int chunkZ,
            long clientVersion,
            int viewDistance,
            Func<int, int, Task<ChunkSyncState?>> loadChunkFunc)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);

            return await _perfMonitor.Measure($"ChunkLoad_{chunkKey}", async () =>
            {
                // 1. 플레이어 추적 정보 업데이트
                var tracker = GetOrCreatePlayerTracker(playerId);
                tracker.ViewDistance = viewDistance;
                tracker.LastUpdate = DateTime.UtcNow;

                // 2. 거리 검증
                if (!ValidateChunkDistance(playerId, chunkX, chunkZ))
                {
                    _logger.Warning("ChunkSync",
                        $"Player {playerId} requested chunk too far away: ({chunkX}, {chunkZ})");
                    return new SyncResultDetail
                    {
                        Result = SyncResult.ValidationFailed,
                        Message = "Chunk too far from player position",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // 3. 중복 로드 방지
                if (IsChunkAlreadyLoaded(playerId, chunkKey, clientVersion))
                {
                    _logger.Debug("ChunkSync",
                        $"Chunk {chunkKey} already loaded by player {playerId} with version {clientVersion}");
                    return new SyncResultDetail
                    {
                        Result = SyncResult.Success,
                        Message = "Chunk already loaded",
                        ServerVersion = clientVersion,
                        Timestamp = DateTime.UtcNow
                    };
                }

                // 4. 청크별 잠금 획득 (레이스 컨디션 방지)
                var chunkLock = _chunkLocks.GetOrAdd(chunkKey, _ => new SemaphoreSlim(1, 1));

                try
                {
                    // 전역 동시 로드 제한
                    await _globalLoadSemaphore.WaitAsync();

                    try
                    {
                        await chunkLock.WaitAsync();

                        // 5. 캐시 확인
                        if (_chunkCache.TryGetValue(chunkKey, out var cachedChunk))
                        {
                            // 버전 비교
                            if (cachedChunk.Version == clientVersion)
                            {
                                // 클라이언트가 최신 버전 보유
                                UpdatePlayerChunkInfo(playerId, chunkX, chunkZ, clientVersion);
                                return new SyncResultDetail
                                {
                                    Result = SyncResult.Success,
                                    Message = "Client has latest version",
                                    ServerVersion = cachedChunk.Version,
                                    Timestamp = DateTime.UtcNow
                                };
                            }

                            // 서버 버전이 더 최신
                            UpdatePlayerChunkInfo(playerId, chunkX, chunkZ, cachedChunk.Version);
                            return new SyncResultDetail
                            {
                                Result = SyncResult.Success,
                                Message = "Chunk loaded from cache",
                                ServerVersion = cachedChunk.Version,
                                Timestamp = DateTime.UtcNow
                            };
                        }

                        // 6. 청크 로드/생성
                        _logger.Info("ChunkSync", $"Loading chunk {chunkKey} for player {playerId}");
                        var chunk = await loadChunkFunc(chunkX, chunkZ);

                        if (chunk == null)
                        {
                            _logger.Error("ChunkSync", $"Failed to load chunk {chunkKey}");
                            return new SyncResultDetail
                            {
                                Result = SyncResult.ValidationFailed,
                                Message = "Chunk load failed",
                                Timestamp = DateTime.UtcNow
                            };
                        }

                        // 7. 캐시 업데이트 (크기 제한)
                        AddToCache(chunkKey, chunk);

                        // 8. 플레이어 추적 정보 업데이트
                        UpdatePlayerChunkInfo(playerId, chunkX, chunkZ, chunk.Version);

                        return new SyncResultDetail
                        {
                            Result = SyncResult.Success,
                            Message = "Chunk loaded successfully",
                            ServerVersion = chunk.Version,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                    finally
                    {
                        chunkLock.Release();
                    }
                }
                finally
                {
                    _globalLoadSemaphore.Release();
                }
            });
        }

        /// <summary>
        /// 청크 언로드 처리
        /// </summary>
        public SyncResultDetail HandleChunkUnload(string playerId, int chunkX, int chunkZ)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);

            if (_playerTrackers.TryGetValue(playerId, out var tracker))
            {
                if (tracker.LoadedChunks.TryRemove(chunkKey, out var info))
                {
                    _logger.Debug("ChunkSync",
                        $"Player {playerId} unloaded chunk {chunkKey} (was accessed {info.AccessCount} times)");

                    return new SyncResultDetail
                    {
                        Result = SyncResult.Success,
                        Message = "Chunk unloaded",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }

            return new SyncResultDetail
            {
                Result = SyncResult.ValidationFailed,
                Message = "Chunk was not loaded",
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 플레이어 연결 해제 시 정리
        /// </summary>
        public void CleanupPlayer(string playerId)
        {
            if (_playerTrackers.TryRemove(playerId, out var tracker))
            {
                var chunkCount = tracker.LoadedChunks.Count;
                _logger.Info("ChunkSync",
                    $"Cleaned up {chunkCount} chunks for disconnected player {playerId}");
            }
        }

        /// <summary>
        /// 오래된 캐시 정리 (메모리 관리)
        /// </summary>
        public void CleanupExpiredCache()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _chunkCache
                .Where(kvp => (now - kvp.Value.LastModified).TotalMinutes > ChunkTimeoutMinutes)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                if (_chunkCache.TryRemove(key, out _))
                {
                    _logger.Debug("ChunkSync", $"Removed expired chunk from cache: {key}");
                }
            }

            if (expiredKeys.Count > 0)
            {
                _logger.Info("ChunkSync", $"Cleaned up {expiredKeys.Count} expired chunks from cache");
            }
        }

        /// <summary>
        /// 동기화 통계 조회
        /// </summary>
        public ChunkSyncStatistics GetStatistics()
        {
            return new ChunkSyncStatistics
            {
                TotalCachedChunks = _chunkCache.Count,
                TotalActivePlayers = _playerTrackers.Count,
                TotalLoadedChunks = _playerTrackers.Values.Sum(t => t.LoadedChunks.Count),
                AverageChunksPerPlayer = _playerTrackers.Count > 0
                    ? _playerTrackers.Values.Average(t => t.LoadedChunks.Count)
                    : 0,
                CacheHitRate = CalculateCacheHitRate()
            };
        }

        // ==================== Private Helper Methods ====================

        private PlayerChunkTracker GetOrCreatePlayerTracker(string playerId)
        {
            return _playerTrackers.GetOrAdd(playerId, _ => new PlayerChunkTracker
            {
                PlayerId = playerId,
                LastUpdate = DateTime.UtcNow
            });
        }

        private bool ValidateChunkDistance(string playerId, int chunkX, int chunkZ)
        {
            // TODO: 실제 플레이어 위치 가져오기
            // 현재는 간단한 검증만 수행
            return true;
        }

        private bool IsChunkAlreadyLoaded(string playerId, string chunkKey, long clientVersion)
        {
            if (_playerTrackers.TryGetValue(playerId, out var tracker))
            {
                if (tracker.LoadedChunks.TryGetValue(chunkKey, out var info))
                {
                    // 접근 통계 업데이트
                    info.LastAccessTime = DateTime.UtcNow;
                    info.AccessCount++;

                    // 버전 일치 여부
                    return info.ClientVersion == clientVersion;
                }
            }
            return false;
        }

        private void UpdatePlayerChunkInfo(string playerId, int chunkX, int chunkZ, long version)
        {
            var tracker = GetOrCreatePlayerTracker(playerId);
            var chunkKey = GetChunkKey(chunkX, chunkZ);

            tracker.LoadedChunks.AddOrUpdate(chunkKey,
                _ => new PlayerChunkTracker.ChunkLoadInfo
                {
                    ChunkX = chunkX,
                    ChunkZ = chunkZ,
                    ClientVersion = version,
                    LoadTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    AccessCount = 1
                },
                (_, existing) =>
                {
                    existing.ClientVersion = version;
                    existing.LastAccessTime = DateTime.UtcNow;
                    existing.AccessCount++;
                    return existing;
                });
        }

        private void AddToCache(string chunkKey, ChunkSyncState chunk)
        {
            // 캐시 크기 제한
            if (_chunkCache.Count >= ChunkCacheMaxSize)
            {
                // LRU: 가장 오래된 항목 제거
                var oldest = _chunkCache
                    .OrderBy(kvp => kvp.Value.LastModified)
                    .FirstOrDefault();

                if (oldest.Key != null)
                {
                    _chunkCache.TryRemove(oldest.Key, out _);
                    _logger.Debug("ChunkSync", $"Evicted old chunk from cache: {oldest.Key}");
                }
            }

            chunk.LastModified = DateTime.UtcNow;
            _chunkCache.AddOrUpdate(chunkKey, chunk, (_, __) => chunk);
        }

        private double CalculateCacheHitRate()
        {
            // TODO: 실제 히트율 계산 로직
            return 0.0;
        }

        private string GetChunkKey(int x, int z) => $"{x}_{z}";
    }

    /// <summary>
    /// 청크 동기화 통계
    /// </summary>
    public class ChunkSyncStatistics
    {
        public int TotalCachedChunks { get; set; }
        public int TotalActivePlayers { get; set; }
        public int TotalLoadedChunks { get; set; }
        public double AverageChunksPerPlayer { get; set; }
        public double CacheHitRate { get; set; }
    }
}
