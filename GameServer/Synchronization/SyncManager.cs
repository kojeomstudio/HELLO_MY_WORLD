using System;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Utils;

namespace GameServer.Synchronization
{
    /// <summary>
    /// 중앙화된 동기화 관리자
    /// 모든 동기화 코디네이터를 통합 관리하고 조정합니다.
    /// </summary>
    public class SyncManager : IDisposable
    {
        private readonly Logger _logger = Logger.Instance;
        private readonly PerformanceMonitor _perfMonitor;

        // 코디네이터들
        public ChunkSyncCoordinator ChunkSync { get; }
        public EntitySyncCoordinator EntitySync { get; }
        public BlockSyncCoordinator BlockSync { get; }

        // 정리 타이머
        private Timer? _cleanupTimer;
        private const int CleanupIntervalMinutes = 5;

        // 통계
        private readonly SyncMetrics _metrics;
        private DateTime _startTime;

        public SyncManager()
        {
            _perfMonitor = new PerformanceMonitor();

            // 코디네이터 초기화
            ChunkSync = new ChunkSyncCoordinator();
            EntitySync = new EntitySyncCoordinator();
            BlockSync = new BlockSyncCoordinator();

            _metrics = new SyncMetrics();
            _startTime = DateTime.UtcNow;

            // 정기 정리 작업 시작
            StartCleanupTask();

            _logger.Info("SyncManager", "Synchronization manager initialized");
        }

        /// <summary>
        /// 정기 정리 작업 시작
        /// </summary>
        private void StartCleanupTask()
        {
            _cleanupTimer = new Timer(
                async _ => await PerformCleanupAsync(),
                null,
                TimeSpan.FromMinutes(CleanupIntervalMinutes),
                TimeSpan.FromMinutes(CleanupIntervalMinutes)
            );
        }

        /// <summary>
        /// 정기 정리 작업 수행
        /// </summary>
        private async Task PerformCleanupAsync()
        {
            try
            {
                await _perfMonitor.Measure("SyncCleanup", async () =>
                {
                    _logger.Info("SyncManager", "Starting periodic cleanup...");

                    // 청크 캐시 정리
                    await Task.Run(() => ChunkSync.CleanupExpiredCache());

                    // 블록 파괴 진행도 정리
                    await Task.Run(() => BlockSync.CleanupExpiredBreakProgress());

                    _logger.Info("SyncManager", "Periodic cleanup completed");
                });
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, "SyncManager.PerformCleanupAsync");
            }
        }

        /// <summary>
        /// 플레이어 연결 해제 시 정리
        /// </summary>
        public void CleanupPlayer(string playerId)
        {
            try
            {
                _logger.Info("SyncManager", $"Cleaning up sync data for player {playerId}");

                ChunkSync.CleanupPlayer(playerId);
                BlockSync.CleanupPlayer(playerId);

                _logger.Info("SyncManager", $"Cleanup completed for player {playerId}");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, $"SyncManager.CleanupPlayer({playerId})");
            }
        }

        /// <summary>
        /// 종합 동기화 통계 조회
        /// </summary>
        public ComprehensiveSyncStatistics GetComprehensiveStatistics()
        {
            var uptime = DateTime.UtcNow - _startTime;

            return new ComprehensiveSyncStatistics
            {
                Uptime = uptime,
                ChunkStats = ChunkSync.GetStatistics(),
                EntityStats = EntitySync.GetStatistics(),
                BlockStats = BlockSync.GetStatistics(),
                GlobalMetrics = _metrics
            };
        }

        /// <summary>
        /// 통계 출력
        /// </summary>
        public void LogStatistics()
        {
            var stats = GetComprehensiveStatistics();

            _logger.Info("SyncManager", "=== Synchronization Statistics ===");
            _logger.Info("SyncManager", $"Uptime: {stats.Uptime:dd\\.hh\\:mm\\:ss}");
            _logger.Info("SyncManager", "");

            _logger.Info("SyncManager", "Chunk Synchronization:");
            _logger.Info("SyncManager", $"  Cached Chunks: {stats.ChunkStats.TotalCachedChunks}");
            _logger.Info("SyncManager", $"  Active Players: {stats.ChunkStats.TotalActivePlayers}");
            _logger.Info("SyncManager", $"  Total Loaded Chunks: {stats.ChunkStats.TotalLoadedChunks}");
            _logger.Info("SyncManager", $"  Avg Chunks/Player: {stats.ChunkStats.AverageChunksPerPlayer:F2}");
            _logger.Info("SyncManager", $"  Cache Hit Rate: {stats.ChunkStats.CacheHitRate:F2}%");
            _logger.Info("SyncManager", "");

            _logger.Info("SyncManager", "Entity Synchronization:");
            _logger.Info("SyncManager", $"  Total Entities: {stats.EntityStats.TotalEntities}");
            _logger.Info("SyncManager", $"  Update Rate: {stats.EntityStats.UpdateRate:F1} tick/s");
            foreach (var kvp in stats.EntityStats.EntitiesByType)
            {
                _logger.Info("SyncManager", $"  {kvp.Key}: {kvp.Value}");
            }
            _logger.Info("SyncManager", "");

            _logger.Info("SyncManager", "Block Synchronization:");
            _logger.Info("SyncManager", $"  Total Blocks: {stats.BlockStats.TotalBlocks}");
            _logger.Info("SyncManager", $"  Active Break Progress: {stats.BlockStats.ActiveBreakProgress}");
            _logger.Info("SyncManager", $"  Players Breaking: {stats.BlockStats.UniquePlayersBreaking}");
            _logger.Info("SyncManager", "");

            _logger.Info("SyncManager", "Global Metrics:");
            _logger.Info("SyncManager", $"  Total Sync Attempts: {stats.GlobalMetrics.TotalSyncAttempts:N0}");
            _logger.Info("SyncManager", $"  Successful Syncs: {stats.GlobalMetrics.SuccessfulSyncs:N0}");
            _logger.Info("SyncManager", $"  Failed Syncs: {stats.GlobalMetrics.FailedSyncs:N0}");
            _logger.Info("SyncManager", $"  Success Rate: {stats.GlobalMetrics.SuccessRate:F2}%");
            _logger.Info("SyncManager", $"  Conflicts Detected: {stats.GlobalMetrics.ConflictsDetected:N0}");
            _logger.Info("SyncManager", $"  Conflicts Resolved: {stats.GlobalMetrics.ConflictsResolved:N0}");
            _logger.Info("SyncManager", $"  Conflict Rate: {stats.GlobalMetrics.ConflictRate:F2}%");
            _logger.Info("SyncManager", $"  Avg Sync Time: {stats.GlobalMetrics.AverageSyncTimeMs:F2}ms");
            _logger.Info("SyncManager", "===================================");

            // 성능 모니터링 통계도 출력
            _perfMonitor.LogStatistics();
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            _logger.Info("SyncManager", "Shutting down synchronization manager...");

            _cleanupTimer?.Dispose();

            _logger.Info("SyncManager", "Synchronization manager shut down");
        }
    }

    /// <summary>
    /// 종합 동기화 통계
    /// </summary>
    public class ComprehensiveSyncStatistics
    {
        public TimeSpan Uptime { get; set; }
        public ChunkSyncStatistics ChunkStats { get; set; } = new();
        public EntitySyncStatistics EntityStats { get; set; } = new();
        public BlockSyncStatistics BlockStats { get; set; } = new();
        public SyncMetrics GlobalMetrics { get; set; } = new();
    }
}
