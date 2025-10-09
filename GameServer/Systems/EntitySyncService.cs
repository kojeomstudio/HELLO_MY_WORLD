using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProtoBuf;
using SharedProtocol;

namespace GameServerApp.Systems
{
    /// <summary>
    /// Coordinates server-to-client synchronisation for player-controlled entities
    /// (spawn, update, despawn) and keeps lightweight velocity samples for interpolation.
    /// </summary>
    public sealed class EntitySyncService
    {
        private const double DefaultBroadcastRange = 128.0;
        private const double MinimumVelocityMagnitude = 0.05d;
        private const double MaxVelocityMagnitude = 48.0d;

        private readonly SessionManager _sessions;
        private readonly ConcurrentDictionary<string, PositionSample> _positionSamples = new(StringComparer.OrdinalIgnoreCase);

        public EntitySyncService(SessionManager sessions)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        }

        /// <summary>
        /// Sends spawn messages for an authenticated player to existing peers and
        /// mirrors current peers back to the newcomer so both sides stay in sync.
        /// </summary>
        public async Task SendSpawnSnapshotsAsync(Session newSession)
        {
            if (newSession == null || string.IsNullOrEmpty(newSession.UserName))
            {
                return;
            }

            var playerState = _sessions.GetPlayerState(newSession.UserName);
            if (playerState == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var playerPosition = ToDoubleVector(playerState.Position);
            _positionSamples[newSession.UserName] = new PositionSample(playerPosition, now);

            var otherSessions = _sessions.GetSessionsSnapshot()
                .Where(session => !string.IsNullOrEmpty(session.UserName) &&
                                   !string.Equals(session.UserName, newSession.UserName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var broadcastTasks = new List<Task>(otherSessions.Count * 2);

            if (otherSessions.Count > 0)
            {
                var newEntityPayload = Serialize(new EntitySpawnMessage
                {
                    Entity = BuildEntityInfo(playerState),
                    SpawnReason = SpawnReason.Natural
                });

                foreach (var session in otherSessions)
                {
                    broadcastTasks.Add(session.SendAsync((int)MinecraftMessageType.EntitySpawn, newEntityPayload));
                }
            }

            foreach (var session in otherSessions)
            {
                var otherName = session.UserName!;
                var otherState = _sessions.GetPlayerState(otherName);
                if (otherState == null || !otherState.IsOnline)
                {
                    continue;
                }

                var otherInfo = BuildEntityInfo(otherState);
                _positionSamples[otherName] = new PositionSample(ToDoubleVector(otherState.Position), now);

                var payload = Serialize(new EntitySpawnMessage
                {
                    Entity = otherInfo,
                    SpawnReason = SpawnReason.Natural
                });

                broadcastTasks.Add(newSession.SendAsync((int)MinecraftMessageType.EntitySpawn, payload));
            }

            if (broadcastTasks.Count > 0)
            {
                await Task.WhenAll(broadcastTasks);
            }
        }

        /// <summary>
        /// Broadcasts a movement update for the supplied session to nearby peers.
        /// </summary>
        public async Task BroadcastPlayerUpdateAsync(Session movedSession, SharedProtocol.Vector3 newPosition, double? broadcastRange = null)
        {
            if (movedSession == null || string.IsNullOrEmpty(movedSession.UserName))
            {
                return;
            }

            var playerState = _sessions.GetPlayerState(movedSession.UserName);
            if (playerState == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var current = ToDoubleVector(newPosition);
            var velocity = ComputeVelocity(movedSession.UserName, current, now);
            var sanitizedVelocity = SanitizeVelocity(velocity);

            _positionSamples[movedSession.UserName] = new PositionSample(current, now);

            var updateMessage = new EntityUpdateMessage
            {
                EntityId = movedSession.UserName,
                Position = new Vector3D { X = current.X, Y = current.Y, Z = current.Z },
                Rotation = new Vector3D
                {
                    X = playerState.RotationX,
                    Y = playerState.RotationY,
                    Z = 0d
                },
                Velocity = new Vector3D { X = sanitizedVelocity.X, Y = sanitizedVelocity.Y, Z = sanitizedVelocity.Z },
                Health = playerState.Health,
                UpdateFlags = new EntityUpdateFlags
                {
                    PositionUpdated = true,
                    RotationUpdated = Math.Abs(playerState.RotationX) > double.Epsilon || Math.Abs(playerState.RotationY) > double.Epsilon,
                    VelocityUpdated = true,
                    HealthUpdated = false
                }
            };

            var recipients = ResolveRecipients(playerState, current, broadcastRange);
            if (recipients.Count == 0)
            {
                return;
            }

            var payload = Serialize(updateMessage);
            var sendTasks = new List<Task>(recipients.Count);

            foreach (var name in recipients)
            {
                if (string.Equals(name, movedSession.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var target = _sessions.GetSession(name);
                if (target == null)
                {
                    continue;
                }

                sendTasks.Add(target.SendAsync((int)MinecraftMessageType.EntityUpdate, payload));
            }

            if (sendTasks.Count > 0)
            {
                await Task.WhenAll(sendTasks);
            }
        }

        /// <summary>
        /// Notifies peers that a player has disconnected and clears cached samples.
        /// </summary>
        public async Task BroadcastPlayerDespawnAsync(string? userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return;
            }

            _positionSamples.TryRemove(userName, out _);

            var payload = Serialize(new EntityDespawnMessage
            {
                EntityId = userName,
                Reason = DespawnReason.Logout
            });

            var sendTasks = new List<Task>();
            foreach (var session in _sessions.GetSessionsSnapshot())
            {
                if (string.IsNullOrEmpty(session.UserName) ||
                    string.Equals(session.UserName, userName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sendTasks.Add(session.SendAsync((int)MinecraftMessageType.EntityDespawn, payload));
            }

            if (sendTasks.Count > 0)
            {
                await Task.WhenAll(sendTasks);
            }
        }

        private static EntityInfo BuildEntityInfo(PlayerState state)
        {
            return new EntityInfo
            {
                EntityId = state.UserName,
                EntityType = EntityType.Player,
                Position = new Vector3D { X = state.Position.X, Y = state.Position.Y, Z = state.Position.Z },
                Rotation = new Vector3D { X = state.RotationX, Y = state.RotationY, Z = 0d },
                Velocity = new Vector3D(),
                Health = state.Health,
                MaxHealth = 100f,
                CustomData = string.Empty
            };
        }

        private List<string> ResolveRecipients(PlayerState state, DoubleVector currentPosition, double? rangeOverride)
        {
            var range = rangeOverride ?? DefaultBroadcastRange;
            if (range <= 0)
            {
                return _sessions.ConnectedUsers
                    .Where(name => !string.Equals(name, state.UserName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var position = new GameServerApp.Vector3(currentPosition.X, currentPosition.Y, currentPosition.Z);
            return _sessions.GetPlayersInRange(state.CurrentWorldId, position, range)
                .Where(name => !string.Equals(name, state.UserName, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DoubleVector ToDoubleVector(SharedProtocol.Vector3 vector)
        {
            return new DoubleVector(vector.X, vector.Y, vector.Z);
        }

        private static DoubleVector ToDoubleVector(GameServerApp.Vector3 vector)
        {
            return new DoubleVector(vector.X, vector.Y, vector.Z);
        }

        private DoubleVector ComputeVelocity(string userName, DoubleVector current, DateTime timestamp)
        {
            if (_positionSamples.TryGetValue(userName, out var sample))
            {
                var deltaSeconds = (timestamp - sample.Timestamp).TotalSeconds;
                if (deltaSeconds > 0.0001d)
                {
                    return new DoubleVector(
                        (current.X - sample.Position.X) / deltaSeconds,
                        (current.Y - sample.Position.Y) / deltaSeconds,
                        (current.Z - sample.Position.Z) / deltaSeconds);
                }
            }

            return DoubleVector.Zero;
        }

        private static DoubleVector SanitizeVelocity(DoubleVector velocity)
        {
            if (!velocity.HasMagnitude)
            {
                return DoubleVector.Zero;
            }

            var magnitudeSquared = velocity.SquaredMagnitude;
            if (magnitudeSquared < MinimumVelocityMagnitude * MinimumVelocityMagnitude)
            {
                return DoubleVector.Zero;
            }

            var maxMagnitudeSquared = MaxVelocityMagnitude * MaxVelocityMagnitude;
            if (magnitudeSquared <= maxMagnitudeSquared)
            {
                return velocity;
            }

            var magnitude = Math.Sqrt(magnitudeSquared);
            if (magnitude < double.Epsilon)
            {
                return DoubleVector.Zero;
            }

            var scale = MaxVelocityMagnitude / magnitude;
            return new DoubleVector(velocity.X * scale, velocity.Y * scale, velocity.Z * scale);
        }

        private static byte[] Serialize<T>(T message)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, message);
            return stream.ToArray();
        }

        private readonly struct PositionSample
        {
            public PositionSample(DoubleVector position, DateTime timestamp)
            {
                Position = position;
                Timestamp = timestamp;
            }

            public DoubleVector Position { get; }
            public DateTime Timestamp { get; }
        }

        private readonly struct DoubleVector
        {
            public DoubleVector(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }

            public bool HasMagnitude => Math.Abs(X) > double.Epsilon || Math.Abs(Y) > double.Epsilon || Math.Abs(Z) > double.Epsilon;

            public double SquaredMagnitude => (X * X) + (Y * Y) + (Z * Z);

            public static DoubleVector Zero => new DoubleVector(0d, 0d, 0d);
        }
    }
}




