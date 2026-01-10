using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using Google.Protobuf;
using SharedProtocol;
using Enhanced = EnhancedMinecraftProtocol;

namespace GameServerApp.Systems
{
    /// <summary>
    /// Generates and broadcasts server-authoritative weather updates for the Minecraft-style world.
    /// </summary>
    public sealed class WeatherSystem : IDisposable
    {
        private readonly SessionManager _sessions;
        private readonly WorldSettings _settings;
        private readonly Random _random = new();
        private readonly object _syncRoot = new();
        private readonly Timer? _timer;
        private readonly bool _isEnabled;
        private readonly int _tickIntervalSeconds;

        private WeatherType _currentWeather = WeatherType.Clear;
        private int _remainingSeconds;

        public WeatherSystem(SessionManager sessions, WorldSettings settings)
        {
            _sessions = sessions;
            _settings = settings;
            _isEnabled = settings.EnableWeatherCycle;
            _tickIntervalSeconds = Math.Max(5, settings.WeatherTickIntervalSeconds);

            _remainingSeconds = GetDurationFor(_currentWeather);

            if (_isEnabled)
            {
                var interval = TimeSpan.FromSeconds(_tickIntervalSeconds);
                _timer = new Timer(Tick, null, interval, interval);
            }

            _sessions.SessionAdded += OnSessionAdded;

            _ = BroadcastSnapshotAsync();
        }

        private async void Tick(object? state)
        {
            WeatherChangeMessage? message = null;

            try
            {
                lock (_syncRoot)
                {
                    if (!_isEnabled)
                    {
                        return;
                    }

                    _remainingSeconds = Math.Max(0, _remainingSeconds - _tickIntervalSeconds);
                    if (_remainingSeconds > 0)
                    {
                        return;
                    }

                    var previous = _currentWeather;
                    _currentWeather = DetermineNextWeather(previous);
                    _remainingSeconds = GetDurationFor(_currentWeather);
                    message = CreateSnapshotInternal();

                    if (previous != _currentWeather)
                    {
                        Console.WriteLine($"Weather changed from {previous} to {_currentWeather} ({_remainingSeconds}s)");
                    }
                }

                if (message != null)
                {
                    await _sessions.BroadcastMinecraftDualAsync(
                        MinecraftMessageType.WeatherChange,
                        message,
                        BuildEnhancedWeatherBroadcast(message));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WeatherSystem tick error: {ex.Message}");
            }
        }

        private Task BroadcastSnapshotAsync()
        {
            var snapshot = CreateSnapshot();
            return _sessions.BroadcastMinecraftDualAsync(MinecraftMessageType.WeatherChange, snapshot, BuildEnhancedWeatherBroadcast(snapshot));
        }

        private WeatherChangeMessage CreateSnapshot()
        {
            lock (_syncRoot)
            {
                return CreateSnapshotInternal();
            }
        }

        private WeatherChangeMessage CreateSnapshotInternal()
        {
            return new WeatherChangeMessage
            {
                WeatherType = _currentWeather,
                Duration = Math.Max(0, _remainingSeconds),
                Intensity = GetIntensity(_currentWeather)
            };
        }

        private void OnSessionAdded(Session session)
        {
            _ = SendSnapshotToSessionAsync(session);
        }

        private async Task SendSnapshotToSessionAsync(Session session)
        {
            try
            {
                var snapshot = CreateSnapshot();
                if (session.UseEnhancedMinecraftProtocol)
                {
                    var enhancedSnapshot = BuildEnhancedWeatherBroadcast(snapshot);
                    await session.SendAsync((int)MinecraftMessageType.WeatherChange, enhancedSnapshot.ToByteArray());
                }
                else
                {
                    var payload = Serialize(snapshot);
                    await session.SendAsync((int)MinecraftMessageType.WeatherChange, payload);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WeatherSystem snapshot error: {ex.Message}");
            }
        }

        private static Enhanced.WeatherUpdateBroadcast BuildEnhancedWeatherBroadcast(WeatherChangeMessage legacy)
        {
            if (legacy == null) throw new ArgumentNullException(nameof(legacy));

            var weatherType = legacy.WeatherType switch
            {
                WeatherType.Rain => Enhanced.WeatherType.WeatherRain,
                WeatherType.Thunderstorm => Enhanced.WeatherType.WeatherStorm,
                WeatherType.Snow => Enhanced.WeatherType.WeatherSnow,
                _ => Enhanced.WeatherType.WeatherClear
            };

            var weatherInfo = new Enhanced.WeatherInfo
            {
                WeatherType = weatherType,
                DurationTicks = Math.Max(0, legacy.Duration) * 20,
                Intensity = legacy.Intensity,
                Thundering = legacy.WeatherType == WeatherType.Thunderstorm
            };

            return new Enhanced.WeatherUpdateBroadcast
            {
                Weather = weatherInfo,
                ChangeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private WeatherType DetermineNextWeather(WeatherType previous)
        {
            if (!_isEnabled)
            {
                return WeatherType.Clear;
            }

            if (previous != WeatherType.Clear && _random.NextDouble() < 0.6)
            {
                return WeatherType.Clear;
            }

            var stormChance = Clamp01(_settings.WeatherStormProbability);
            var snowChance = Clamp01(_settings.WeatherSnowProbability);
            var available = Math.Max(0.0, 1.0 - stormChance - snowChance);
            var rainChance = Math.Min(0.35, available);

            var roll = _random.NextDouble();
            if (roll < stormChance)
            {
                return WeatherType.Thunderstorm;
            }

            roll -= stormChance;
            if (roll < snowChance)
            {
                return WeatherType.Snow;
            }

            roll -= snowChance;
            if (roll < rainChance)
            {
                return WeatherType.Rain;
            }

            return WeatherType.Clear;
        }

        private int GetDurationFor(WeatherType weather)
        {
            var baseDuration = weather switch
            {
                WeatherType.Clear => _settings.ClearWeatherDurationSeconds,
                WeatherType.Rain => _settings.RainWeatherDurationSeconds,
                WeatherType.Thunderstorm => _settings.StormWeatherDurationSeconds,
                WeatherType.Snow => _settings.SnowWeatherDurationSeconds,
                _ => _settings.ClearWeatherDurationSeconds
            };

            baseDuration = Math.Max(_tickIntervalSeconds, baseDuration);

            if (!_isEnabled)
            {
                return baseDuration;
            }

            var variation = Math.Max(5, (int)(baseDuration * 0.25));
            var delta = _random.Next(-variation, variation + 1);
            return Math.Max(_tickIntervalSeconds, baseDuration + delta);
        }

        private static float GetIntensity(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => 0f,
                WeatherType.Rain => 0.65f,
                WeatherType.Thunderstorm => 1f,
                WeatherType.Snow => 0.5f,
                _ => 0f
            };
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _sessions.SessionAdded -= OnSessionAdded;
        }

        private static double Clamp01(double value)
        {
            return Math.Clamp(value, 0.0, 1.0);
        }

        private static byte[] Serialize(WeatherChangeMessage message)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, message);
            return stream.ToArray();
        }
    }
}
