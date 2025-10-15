using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace GameServerApp.Systems;

/// <summary>
/// Tracks high-level runtime metrics that can be queried by status endpoints.
/// </summary>
public class ServerMetricsService
{
    private readonly SessionManager _sessions;
    private readonly Stopwatch _uptimeStopwatch = new();
    private readonly string _serverVersion;
    private long _containerHashMismatchCount;

    public ServerMetricsService(SessionManager sessions)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _serverVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "dev";
    }

    /// <summary>
    /// Marks the moment when the server is ready to accept players.
    /// </summary>
    public void MarkServerStarted()
    {
        if (_uptimeStopwatch.IsRunning)
        {
            _uptimeStopwatch.Restart();
        }
        else
        {
            _uptimeStopwatch.Start();
        }
    }

    /// <summary>
    /// Stops uptime tracking, typically when the server shuts down.
    /// </summary>
    public void MarkServerStopped()
    {
        if (_uptimeStopwatch.IsRunning)
        {
            _uptimeStopwatch.Stop();
        }
    }

    public ServerStatusSnapshot CaptureStatus()
    {
        return new ServerStatusSnapshot
        {
            OnlinePlayers = _sessions.OnlinePlayerCount,
            ServerVersion = _serverVersion,
            Uptime = _uptimeStopwatch.Elapsed,
            ContainerHashMismatches = Interlocked.Read(ref _containerHashMismatchCount)
        };
    }

    public void IncrementContainerHashMismatch()
    {
        Interlocked.Increment(ref _containerHashMismatchCount);
    }
}

public record ServerStatusSnapshot
{
    public int OnlinePlayers { get; init; }
    public string ServerVersion { get; init; } = string.Empty;
    public TimeSpan Uptime { get; init; }
    public long ContainerHashMismatches { get; init; }

    public long UptimeMilliseconds => (long)Uptime.TotalMilliseconds;
}
