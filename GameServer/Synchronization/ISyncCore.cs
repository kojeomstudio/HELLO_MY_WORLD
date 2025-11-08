using System;
using System.Threading.Tasks;

namespace GameServer.Synchronization
{
    /// <summary>
    /// 동기화 가능한 엔티티의 기본 인터페이스
    /// </summary>
    public interface ISyncable
    {
        /// <summary>
        /// 동기화 버전 번호 (Optimistic Concurrency Control)
        /// </summary>
        long Version { get; set; }

        /// <summary>
        /// 마지막 수정 시간
        /// </summary>
        DateTime LastModified { get; set; }

        /// <summary>
        /// 동기화 상태 해시 (체크섬)
        /// </summary>
        string GetStateHash();
    }

    /// <summary>
    /// 동기화 결과
    /// </summary>
    public enum SyncResult
    {
        Success,                // 성공
        Conflict,              // 버전 충돌
        ValidationFailed,      // 검증 실패
        NetworkError,          // 네트워크 오류
        Timeout,               // 타임아웃
        RateLimited           // 속도 제한
    }

    /// <summary>
    /// 동기화 결과 상세 정보
    /// </summary>
    public class SyncResultDetail
    {
        public SyncResult Result { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? ServerVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public object? ConflictData { get; set; }

        public bool IsSuccess => Result == SyncResult.Success;
        public bool RequiresRetry => Result == SyncResult.NetworkError || Result == SyncResult.Timeout;
        public bool RequiresResync => Result == SyncResult.Conflict;
    }

    /// <summary>
    /// 동기화 전략 인터페이스
    /// </summary>
    public interface ISyncStrategy
    {
        /// <summary>
        /// 충돌 해결 방법
        /// </summary>
        Task<object?> ResolveConflict(object clientData, object serverData);

        /// <summary>
        /// 재시도 정책
        /// </summary>
        bool ShouldRetry(int attemptNumber, SyncResult lastResult);

        /// <summary>
        /// 재시도 지연 시간 (밀리초)
        /// </summary>
        int GetRetryDelay(int attemptNumber);
    }

    /// <summary>
    /// 기본 동기화 전략 (Exponential Backoff)
    /// </summary>
    public class DefaultSyncStrategy : ISyncStrategy
    {
        private const int MaxRetries = 3;
        private const int BaseDelayMs = 1000;

        public virtual Task<object?> ResolveConflict(object clientData, object serverData)
        {
            // 기본: 서버 우선 (Server Wins)
            return Task.FromResult<object?>(serverData);
        }

        public virtual bool ShouldRetry(int attemptNumber, SyncResult lastResult)
        {
            if (attemptNumber >= MaxRetries)
                return false;

            return lastResult == SyncResult.NetworkError ||
                   lastResult == SyncResult.Timeout ||
                   lastResult == SyncResult.RateLimited;
        }

        public virtual int GetRetryDelay(int attemptNumber)
        {
            // Exponential backoff: 1s, 2s, 4s
            return BaseDelayMs * (int)Math.Pow(2, attemptNumber - 1);
        }
    }

    /// <summary>
    /// 클라이언트 우선 전략
    /// </summary>
    public class ClientWinsSyncStrategy : DefaultSyncStrategy
    {
        public override Task<object?> ResolveConflict(object clientData, object serverData)
        {
            return Task.FromResult<object?>(clientData);
        }
    }

    /// <summary>
    /// 최신 타임스탬프 우선 전략
    /// </summary>
    public class LastWriteWinsSyncStrategy : DefaultSyncStrategy
    {
        public override Task<object?> ResolveConflict(object clientData, object serverData)
        {
            if (clientData is ISyncable clientSyncable && serverData is ISyncable serverSyncable)
            {
                return Task.FromResult<object?>(
                    clientSyncable.LastModified > serverSyncable.LastModified
                        ? clientData
                        : serverData
                );
            }

            return base.ResolveConflict(clientData, serverData);
        }
    }

    /// <summary>
    /// 동기화 컨텍스트
    /// </summary>
    public class SyncContext
    {
        public string PlayerId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public DateTime RequestTime { get; set; }
        public int AttemptNumber { get; set; }
        public ISyncStrategy Strategy { get; set; } = new DefaultSyncStrategy();
    }

    /// <summary>
    /// 동기화 이벤트 인터페이스
    /// </summary>
    public interface ISyncEventListener
    {
        void OnSyncStarted(string entityType, string entityId);
        void OnSyncCompleted(string entityType, string entityId, SyncResultDetail result);
        void OnSyncFailed(string entityType, string entityId, SyncResultDetail result);
        void OnConflictDetected(string entityType, string entityId, object clientData, object serverData);
        void OnConflictResolved(string entityType, string entityId, object resolvedData);
    }

    /// <summary>
    /// 동기화 메트릭 수집
    /// </summary>
    public class SyncMetrics
    {
        public long TotalSyncAttempts { get; set; }
        public long SuccessfulSyncs { get; set; }
        public long FailedSyncs { get; set; }
        public long ConflictsDetected { get; set; }
        public long ConflictsResolved { get; set; }
        public double AverageSyncTimeMs { get; set; }
        public DateTime LastSyncTime { get; set; }

        public double SuccessRate => TotalSyncAttempts > 0
            ? (double)SuccessfulSyncs / TotalSyncAttempts * 100
            : 0;

        public double ConflictRate => TotalSyncAttempts > 0
            ? (double)ConflictsDetected / TotalSyncAttempts * 100
            : 0;
    }
}
