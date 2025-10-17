using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Linq;

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
    private readonly ConcurrentDictionary<string, int> _chunkResidencyByPlayer = new(StringComparer.OrdinalIgnoreCase);
    private long _totalChunkResidency;
    private int _playersWithChunkResidency;
    private int _peakChunksPerPlayer;
    private string _busiestChunkPlayer = string.Empty;
    private readonly object _chunkResidencyLock = new();

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
            ContainerHashMismatches = Interlocked.Read(ref _containerHashMismatchCount),
            TotalTrackedChunks = Interlocked.Read(ref _totalChunkResidency),
            PlayersWithChunkResidency = Volatile.Read(ref _playersWithChunkResidency),
            PeakChunksPerPlayer = Volatile.Read(ref _peakChunksPerPlayer),
            BusiestChunkPlayer = Volatile.Read(ref _busiestChunkPlayer) ?? string.Empty
        };
    }

    public void IncrementContainerHashMismatch()
    {
        Interlocked.Increment(ref _containerHashMismatchCount);
    }

    public void UpdateChunkResidency(string playerId, int chunkCount)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        _chunkResidencyByPlayer.AddOrUpdate(playerId,
            _ => Math.Max(0, chunkCount),
            (_, _) => Math.Max(0, chunkCount));

        RecalculateChunkResidency();
    }

    public void ClearChunkResidency(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        _chunkResidencyByPlayer.TryRemove(playerId, out _);
        RecalculateChunkResidency();
    }

    private void RecalculateChunkResidency()
    {
        lock (_chunkResidencyLock)
        {
            if (_chunkResidencyByPlayer.IsEmpty)
            {
                Interlocked.Exchange(ref _totalChunkResidency, 0);
                Volatile.Write(ref _playersWithChunkResidency, 0);
                Volatile.Write(ref _peakChunksPerPlayer, 0);
                Interlocked.Exchange(ref _busiestChunkPlayer, string.Empty);
                return;
            }

            var snapshot = _chunkResidencyByPlayer.ToArray();
            long total = 0;
            var peak = 0;
            var topPlayer = string.Empty;

            foreach (var entry in snapshot)
            {
                total += entry.Value;
                if (entry.Value > peak)
                {
                    peak = entry.Value;
                    topPlayer = entry.Key;
                }
            }

            Interlocked.Exchange(ref _totalChunkResidency, total);
            Volatile.Write(ref _playersWithChunkResidency, snapshot.Length);
            Volatile.Write(ref _peakChunksPerPlayer, peak);
            Interlocked.Exchange(ref _busiestChunkPlayer, topPlayer);
        }
    }
}

public record ServerStatusSnapshot
{
    public int OnlinePlayers { get; init; }
    public string ServerVersion { get; init; } = string.Empty;
    public TimeSpan Uptime { get; init; }
    public long ContainerHashMismatches { get; init; }
    public long TotalTrackedChunks { get; init; }
    public int PlayersWithChunkResidency { get; init; }
    public int PeakChunksPerPlayer { get; init; }
    public string BusiestChunkPlayer { get; init; } = string.Empty;

    public long UptimeMilliseconds => (long)Uptime.TotalMilliseconds;
    public double AverageChunksPerPlayer =>
        PlayersWithChunkResidency <= 0 ? 0 : (double)TotalTrackedChunks / PlayersWithChunkResidency;
}
