using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace GameServer.Utils
{
    /// <summary>
    /// 성능 모니터링 유틸리티
    /// </summary>
    public class PerformanceMonitor
    {
        private static readonly Logger _logger = Logger.Instance;
        private readonly ConcurrentDictionary<string, MetricData> _metrics;
        private readonly Stopwatch _uptime;

        private class MetricData
        {
            public long TotalCalls { get; set; }
            public long TotalMilliseconds { get; set; }
            public long MinMilliseconds { get; set; } = long.MaxValue;
            public long MaxMilliseconds { get; set; }
            public DateTime LastCall { get; set; }
        }

        public PerformanceMonitor()
        {
            _metrics = new ConcurrentDictionary<string, MetricData>();
            _uptime = Stopwatch.StartNew();
        }

        /// <summary>
        /// 작업 실행 시간 측정
        /// </summary>
        public T Measure<T>(string operationName, Func<T> operation)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return operation();
            }
            finally
            {
                sw.Stop();
                RecordMetric(operationName, sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// void 작업 실행 시간 측정
        /// </summary>
        public void Measure(string operationName, Action operation)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                operation();
            }
            finally
            {
                sw.Stop();
                RecordMetric(operationName, sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// 메트릭 기록
        /// </summary>
        private void RecordMetric(string name, long milliseconds)
        {
            var metric = _metrics.GetOrAdd(name, _ => new MetricData());

            lock (metric)
            {
                metric.TotalCalls++;
                metric.TotalMilliseconds += milliseconds;
                metric.MinMilliseconds = Math.Min(metric.MinMilliseconds, milliseconds);
                metric.MaxMilliseconds = Math.Max(metric.MaxMilliseconds, milliseconds);
                metric.LastCall = DateTime.UtcNow;
            }

            // 느린 작업 경고 (100ms 이상)
            if (milliseconds > 100)
            {
                _logger.Warning("Performance", $"Slow operation detected: {name} took {milliseconds}ms");
            }
        }

        /// <summary>
        /// 모든 메트릭 통계 출력
        /// </summary>
        public void LogStatistics()
        {
            if (_metrics.IsEmpty)
            {
                _logger.Info("Performance", "No performance metrics recorded");
                return;
            }

            _logger.Info("Performance", "=== Performance Statistics ===");
            _logger.Info("Performance", $"Server Uptime: {_uptime.Elapsed:dd\\.hh\\:mm\\:ss}");
            _logger.Info("Performance", "");

            foreach (var kvp in _metrics.OrderByDescending(x => x.Value.TotalMilliseconds))
            {
                var name = kvp.Key;
                var data = kvp.Value;

                lock (data)
                {
                    var avgMs = data.TotalCalls > 0 ? data.TotalMilliseconds / data.TotalCalls : 0;
                    _logger.Info("Performance", $"{name}:");
                    _logger.Info("Performance", $"  Total Calls: {data.TotalCalls:N0}");
                    _logger.Info("Performance", $"  Total Time: {data.TotalMilliseconds:N0}ms");
                    _logger.Info("Performance", $"  Avg Time: {avgMs:N2}ms");
                    _logger.Info("Performance", $"  Min Time: {data.MinMilliseconds}ms");
                    _logger.Info("Performance", $"  Max Time: {data.MaxMilliseconds}ms");
                    _logger.Info("Performance", $"  Last Call: {data.LastCall:yyyy-MM-dd HH:mm:ss}");
                    _logger.Info("Performance", "");
                }
            }

            _logger.Info("Performance", "==============================");
        }

        /// <summary>
        /// 특정 작업의 통계 조회
        /// </summary>
        public (long calls, long totalMs, long avgMs, long minMs, long maxMs)? GetStatistics(string operationName)
        {
            if (_metrics.TryGetValue(operationName, out var data))
            {
                lock (data)
                {
                    var avgMs = data.TotalCalls > 0 ? data.TotalMilliseconds / data.TotalCalls : 0;
                    return (data.TotalCalls, data.TotalMilliseconds, avgMs, data.MinMilliseconds, data.MaxMilliseconds);
                }
            }
            return null;
        }

        /// <summary>
        /// 메트릭 초기화
        /// </summary>
        public void Reset()
        {
            _metrics.Clear();
            _logger.Info("Performance", "Performance metrics reset");
        }
    }
}
