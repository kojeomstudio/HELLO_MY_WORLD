using System;
using System.Threading;
using System.Threading.Tasks;
using SharedProtocol;

namespace GameServerApp.Systems
{
    /// <summary>
    /// Periodically advances the server world clock and broadcasts time updates to all sessions.
    /// </summary>
    public sealed class WorldTimeSystem : IDisposable
    {
        private const long DayCycleLength = 24000L;

        private readonly SessionManager _sessions;
        private readonly Timer? _timer;
        private readonly object _syncRoot = new();
        private readonly double _ticksPerSecond;
        private readonly bool _isEnabled;

        private double _accumulatedTicks;
        private long _worldTime;
        private long _dayTime;

        public WorldTimeSystem(SessionManager sessions, WorldSettings settings)
        {
            _sessions = sessions;
            _worldTime = Math.Max(0, settings.InitialWorldTime);
            _dayTime = NormalizeDayTime(settings.InitialDayTime);

            _isEnabled = settings.EnableDayNightCycle && settings.DayNightCycleSecondsPerDay > 0;
            if (_isEnabled)
            {
                _ticksPerSecond = DayCycleLength / Math.Max(1, (double)settings.DayNightCycleSecondsPerDay);
                _timer = new Timer(Tick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
            else
            {
                _ticksPerSecond = 0;
            }

            _ = BroadcastSnapshotAsync();
        }

        private async void Tick(object? state)
        {
            try
            {
                if (!_isEnabled || _ticksPerSecond <= 0)
                {
                    return;
                }

                long currentWorldTime;
                long currentDayTime;

                lock (_syncRoot)
                {
                    _accumulatedTicks += _ticksPerSecond;
                    var wholeTicks = (long)_accumulatedTicks;
                    if (wholeTicks <= 0)
                    {
                        return;
                    }

                    _accumulatedTicks -= wholeTicks;
                    unchecked
                    {
                        _worldTime += wholeTicks;
                    }

                    _dayTime = NormalizeDayTime(_dayTime + wholeTicks);
                    currentWorldTime = _worldTime;
                    currentDayTime = _dayTime;
                }

                await _sessions.BroadcastMinecraftAsync(MinecraftMessageType.TimeUpdate, new TimeUpdateMessage
                {
                    WorldTime = currentWorldTime,
                    DayTime = currentDayTime
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorldTimeSystem tick error: {ex.Message}");
            }
        }

        private Task BroadcastSnapshotAsync()
        {
            var snapshot = new TimeUpdateMessage
            {
                WorldTime = _worldTime,
                DayTime = _dayTime
            };

            return _sessions.BroadcastMinecraftAsync(MinecraftMessageType.TimeUpdate, snapshot);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private static long NormalizeDayTime(long value)
        {
            var normalized = value % DayCycleLength;
            return normalized < 0 ? normalized + DayCycleLength : normalized;
        }
    }
}

