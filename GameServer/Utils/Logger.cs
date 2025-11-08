using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer.Utils
{
    /// <summary>
    /// 구조화된 로깅 시스템
    /// 성능을 위한 비동기 로깅 지원
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public class Logger : IDisposable
    {
        private static readonly Lazy<Logger> _instance = new(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly BlockingCollection<LogEntry> _logQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _loggerTask;
        private readonly string _logFilePath;
        private LogLevel _minLogLevel = LogLevel.Info;

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public Exception? Exception { get; set; }
        }

        private Logger()
        {
            _logQueue = new BlockingCollection<LogEntry>(1000);
            _cancellationTokenSource = new CancellationTokenSource();

            // 로그 파일 경로 설정
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"server_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            // 백그라운드 로거 스레드 시작
            _loggerTask = Task.Run(() => ProcessLogQueue(_cancellationTokenSource.Token));
        }

        public void SetMinLogLevel(LogLevel level)
        {
            _minLogLevel = level;
        }

        public void Trace(string category, string message)
        {
            Log(LogLevel.Trace, category, message, null);
        }

        public void Debug(string category, string message)
        {
            Log(LogLevel.Debug, category, message, null);
        }

        public void Info(string category, string message)
        {
            Log(LogLevel.Info, category, message, null);
        }

        public void Warning(string category, string message)
        {
            Log(LogLevel.Warning, category, message, null);
        }

        public void Error(string category, string message, Exception? ex = null)
        {
            Log(LogLevel.Error, category, message, ex);
        }

        public void Critical(string category, string message, Exception? ex = null)
        {
            Log(LogLevel.Critical, category, message, ex);
        }

        private void Log(LogLevel level, string category, string message, Exception? ex)
        {
            if (level < _minLogLevel)
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Exception = ex
            };

            // 큐가 가득 찬 경우 로그 드롭 (성능 보호)
            if (!_logQueue.TryAdd(entry, TimeSpan.FromMilliseconds(10)))
            {
                Console.WriteLine("[Logger] Warning: Log queue full, dropping message");
            }
        }

        private void ProcessLogQueue(CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(fileStream) { AutoFlush = true };

                foreach (var entry in _logQueue.GetConsumingEnumerable(cancellationToken))
                {
                    var logLine = FormatLogEntry(entry);

                    // 콘솔 출력 (색상 적용)
                    Console.ForegroundColor = GetConsoleColor(entry.Level);
                    Console.WriteLine(logLine);
                    Console.ResetColor();

                    // 파일 출력
                    writer.WriteLine(logLine);

                    // 예외 스택 트레이스 출력
                    if (entry.Exception != null)
                    {
                        var exceptionInfo = $"  Exception: {entry.Exception.GetType().Name}\n  Message: {entry.Exception.Message}\n  StackTrace:\n{entry.Exception.StackTrace}";
                        Console.WriteLine(exceptionInfo);
                        writer.WriteLine(exceptionInfo);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger] Fatal error in log processing: {ex.Message}");
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level,-8}] [{entry.Category,-20}] {entry.Message}";
        }

        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _logQueue.CompleteAdding();

            try
            {
                _loggerTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger] Error during shutdown: {ex.Message}");
            }

            _logQueue.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
