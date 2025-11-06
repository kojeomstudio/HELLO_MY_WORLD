using GameServerApp.Database;
using GameServerApp.Systems;
using GameServerApp.World;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Google.Protobuf;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// 마인크래프트 청크 데이터 요청을 처리하는 핸들러
    /// 클라이언트의 시야 거리에 따른 청크 로딩 및 언로딩을 관리합니다.
    /// </summary>
    public class MinecraftChunkHandler : IMessageHandler, IMinecraftMessageHandler<ChunkUnloadNotificationMessage>
    {
        private sealed record PlayerChunkResidency(int ChunkX, int ChunkZ, DateTime LastServedUtc);

        private readonly DatabaseHelper _database;
        private readonly SessionManager _sessions;
        private readonly WorldManager _worldManager;
        private readonly WorldSettings _worldSettings;
        private readonly ServerMetricsService _metrics;

        private const int CHUNK_SIZE_X = 16;
        private const int CHUNK_SIZE_Z = 16;
        private const int CHUNK_HEIGHT = 256;

        private const int COMPRESSION_THRESHOLD = 1024;
        private const int RESIDENCY_DISTANCE_PADDING = 1;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, PlayerChunkResidency>> _playerLoadedChunks = new();
        private readonly TimeSpan _chunkResidencyTimeout;
        private readonly TimeSpan _residencyCleanupInterval = TimeSpan.FromMinutes(5);
        private DateTime _lastCleanup = DateTime.MinValue;

        private static long ToChunkKey(int chunkX, int chunkZ)
        {
            return ((long)chunkX << 32) | (uint)chunkZ;
        }

        private bool HasPlayerLoadedChunk(string playerId, int chunkX, int chunkZ)
        {
            return _playerLoadedChunks.TryGetValue(playerId, out var chunkSet) && chunkSet.ContainsKey(ToChunkKey(chunkX, chunkZ));
        }

        public MinecraftChunkHandler(DatabaseHelper database, SessionManager sessions, WorldManager worldManager, WorldSettings worldSettings, ServerMetricsService metrics)
        {
            _database = database;
            _sessions = sessions;
            _worldManager = worldManager;
            _worldSettings = worldSettings;
            _metrics = metrics;
            _chunkResidencyTimeout = TimeSpan.FromMinutes(Math.Max(1, _worldSettings.ChunkUnloadTimeoutMinutes));
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.ChunkDataRequest);
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.ChunkDataResponse);
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.ChunkUnloadNotification);
            ProtocolRegistry.EnsureRegistered(MinecraftMessageType.ChunkUnloadAcknowledge);
        }

        public MessageType Type => (MessageType)MinecraftMessageType.ChunkDataRequest;

        /// <summary>
        /// 청크 데이터 요청 처리
        /// </summary>
        public async Task HandleAsync(Session session, object message)
        {
            if (message is byte[] messageData)
            {
                await HandleChunkRequestAsync(session, messageData);
            }
            else
            {
                Console.WriteLine("Invalid message format for MinecraftChunkHandler");
            }
        }

        private bool TryParseEnhancedChunkLoadRequest(byte[] messageData, out EnhancedMinecraftProtocol.ChunkLoadRequest? request)
        {
            try
            {
                request = EnhancedMinecraftProtocol.ChunkLoadRequest.Parser.ParseFrom(messageData);
                if (request.ChunkPositions.Count == 0)
                {
                    request = null;
                    return false;
                }
                return true;
            }
            catch (InvalidProtocolBufferException)
            {
                request = null;
                return false;
            }
        }

        private async Task HandleEnhancedChunkRequestAsync(Session session, EnhancedMinecraftProtocol.ChunkLoadRequest request)
        {
            var playerId = session.UserName ?? string.Empty;
            var playerState = string.IsNullOrWhiteSpace(playerId) ? null : _sessions.GetPlayerState(playerId);
            if (playerState == null)
            {
                if (request.ChunkPositions.Count > 0)
                {
                    var first = request.ChunkPositions[0];
                    await SendErrorResponse(session, first.X, first.Z, "플레이어 상태를 찾을 수 없습니다.");
                }
                return;
            }

            var totalRequested = Math.Max(1, request.ChunkPositions.Count);
            var requestedViewDistance = request.ViewDistance > 0 ? request.ViewDistance : _worldSettings.ChunkLoadRadius;
            foreach (var position in request.ChunkPositions)
            {
                await HandleSingleChunkAsync(session, playerId, playerState, position.X, position.Z, requestedViewDistance, totalRequested);
            }
        }

        private async Task HandleLegacyChunkRequestAsync(Session session, ChunkDataRequestMessage chunkRequest)
        {
            var playerId = session.UserName ?? string.Empty;
            var playerState = string.IsNullOrWhiteSpace(playerId) ? null : _sessions.GetPlayerState(playerId);
            if (playerState == null)
            {
                await SendErrorResponse(session, chunkRequest.ChunkX, chunkRequest.ChunkZ, "플레이어 상태를 찾을 수 없습니다.");
                return;
            }

            var viewDistance = chunkRequest.ViewDistance > 0 ? chunkRequest.ViewDistance : _worldSettings.ChunkLoadRadius;
            await HandleSingleChunkAsync(session, playerId, playerState, chunkRequest.ChunkX, chunkRequest.ChunkZ, viewDistance, totalRequested: 1);
        }

        private async Task<bool> HandleSingleChunkAsync(Session session, string playerId, PlayerState playerState, int chunkX, int chunkZ, int viewDistance, int totalRequested)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                await SendErrorResponse(session, chunkX, chunkZ, "Unauthenticated session");
                return false;
            }

            var playerChunkX = (int)Math.Floor(playerState.Position.X / CHUNK_SIZE_X);
            var playerChunkZ = (int)Math.Floor(playerState.Position.Z / CHUNK_SIZE_Z);
            var distance = Math.Max(Math.Abs(chunkX - playerChunkX), Math.Abs(chunkZ - playerChunkZ));

            if (distance > viewDistance)
            {
                await SendErrorResponse(session, chunkX, chunkZ, "요청된 청크가 시야 거리를 벗어났습니다.");
                return false;
            }

            var chunkResult = await LoadOrGenerateChunkPayload(chunkX, chunkZ);
            if (chunkResult == null)
            {
                await SendErrorResponse(session, chunkX, chunkZ, "Unable to load chunk data.");
                return false;
            }

            var (compressedBlockData, biomeBytes, biomeInfo, isFromStorage) = chunkResult.Value;
            var alreadyServed = HasPlayerLoadedChunk(playerId, chunkX, chunkZ);

            var entities = await _worldManager.GetEntitiesInChunk(chunkX, chunkZ);

            var response = new ChunkDataResponseMessage
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Success = true,
                CompressedBlockData = compressedBlockData,
                Entities = entities.Select(ConvertToEntityInfo).ToList(),
                BiomeData = biomeInfo,
                IsFromCache = isFromStorage || alreadyServed
            };

            var generationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var enhancedResponse = ChunkPayloadBuilder.BuildLoadResponse(
                chunkX,
                chunkZ,
                compressedBlockData,
                biomeBytes,
                generationTimestamp,
                totalRequested);
            response.EnhancedPayload = enhancedResponse.ToByteArray();

            ChunkPayloadBuilder.ValidateChunkPayload(chunkX, chunkZ, compressedBlockData, biomeBytes);
            await SendChunkResponse(session, response);

            await UpdatePlayerLoadedChunks(playerId, chunkX, chunkZ, viewDistance);

            Console.WriteLine($"청크 [{chunkX}, {chunkZ}] 데이터를 플레이어 {playerId}에게 전송 완료");
            return true;
        }
        async Task IMinecraftMessageHandler.HandleAsync(Session session, byte[] messageData)
        {
            using var stream = new MemoryStream(messageData);
            var message = ProtoBuf.Serializer.Deserialize<ChunkUnloadNotificationMessage>(stream);
            await HandleAsync(session, message);
        }

        public Task HandleAsync(Session session, ChunkUnloadNotificationMessage message)
        {
            return HandleChunkUnloadAsync(session, message);
        }

        /// <summary>
        /// 청크 요청 메시지 처리
        /// </summary>
        private async Task HandleChunkRequestAsync(Session session, byte[] messageData)
        {
            try
            {
                if (TryParseEnhancedChunkLoadRequest(messageData, out var enhancedRequest))
                {
                    await HandleEnhancedChunkRequestAsync(session, enhancedRequest!);
                    return;
                }

                using var stream = new MemoryStream(messageData);
                var chunkRequest = ProtoBuf.Serializer.Deserialize<ChunkDataRequestMessage>(stream);
                await HandleLegacyChunkRequestAsync(session, chunkRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"청크 요청 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 청크 데이터를 로드하거나 새로 생성하고 전송에 필요한 메타데이터를 준비한다.
        /// </summary>
        private async Task<(byte[] CompressedBlockData, byte[] BiomeBytes, BiomeInfo BiomeInfo, bool IsFromDatabase)?> LoadOrGenerateChunkPayload(int chunkX, int chunkZ)
        {
            try
            {
                bool isPersisted = await _database.ChunkExistsAsync(chunkX, chunkZ);

                var chunk = await _worldManager.GetChunkAsync(chunkX, chunkZ);
                if (chunk == null)
                {
                    return null;
                }

                var (blockBytes, storedBiomeBytes) = chunk.ToBytes();
                var compressed = CompressChunkData(blockBytes);
                var biomeInfo = BuildBiomeInfo(chunkX, chunkZ, storedBiomeBytes, chunk);
                var biomeBytes = storedBiomeBytes.Length > 0
                    ? storedBiomeBytes
                    : ConvertBiomeIdsToBytes(biomeInfo.BiomeIds);

                return (compressed, biomeBytes, biomeInfo, isPersisted);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chunk [{chunkX}, {chunkZ}] load/generation error: {ex.Message}");
                return null;
            }
        }

        private BiomeInfo BuildBiomeInfo(int chunkX, int chunkZ, byte[] storedBiomeBytes, ChunkData chunk)
        {
            var biomeIds = new List<int>(CHUNK_SIZE_X * CHUNK_SIZE_Z);
            double tempSum = 0;
            double humiditySum = 0;

            if (storedBiomeBytes.Length >= CHUNK_SIZE_X * CHUNK_SIZE_Z)
            {
                for (int index = 0; index < CHUNK_SIZE_X * CHUNK_SIZE_Z; index++)
                {
                    var biome = (BiomeType)storedBiomeBytes[index];
                    biomeIds.Add((int)biome);

                    var climate = GetBiomeClimate(biome);
                    tempSum += climate.temp;
                    humiditySum += climate.humidity;
                }
            }
            else
            {
                for (int z = 0; z < CHUNK_SIZE_Z; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE_X; x++)
                    {
                        var biome = chunk?.GetBiome(x, z)
                                     ?? _worldManager.SampleBiome(chunkX * CHUNK_SIZE_X + x, chunkZ * CHUNK_SIZE_Z + z);
                        biomeIds.Add((int)biome);

                        var climate = GetBiomeClimate(biome);
                        tempSum += climate.temp;
                        humiditySum += climate.humidity;
                    }
                }
            }

            var sampleCount = biomeIds.Count;
            float averageTemp = sampleCount > 0 ? (float)(tempSum / sampleCount) : 0.5f;
            float averageHumidity = sampleCount > 0 ? (float)(humiditySum / sampleCount) : 0.5f;

            return new BiomeInfo
            {
                BiomeIds = biomeIds,
                Temperature = averageTemp,
                Humidity = averageHumidity
            };
        }

        /// <summary>
        /// 청크 데이터 압축
        /// </summary>
        private byte[] CompressChunkData(byte[] data)
        {
            if (data.Length < COMPRESSION_THRESHOLD)
            {
                return data; // 작은 데이터는 압축하지 않음
            }

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            
            var compressed = output.ToArray();
            
            // 압축 효율이 좋지 않으면 원본 반환
            return compressed.Length < data.Length * 0.9 ? compressed : data;
        }



        private static byte[] ConvertBiomeIdsToBytes(IReadOnlyList<int> biomeIds)
        {
            if (biomeIds.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[biomeIds.Count];
            for (int i = 0; i < biomeIds.Count; i++)
            {
                buffer[i] = (byte)Math.Clamp(biomeIds[i], 0, 255);
            }

            return buffer;
        }

        private (double temp, double humidity) GetBiomeClimate(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Desert => (0.95, 0.15),
                BiomeType.Tundra => (0.2, 0.35),
                BiomeType.Forest => (0.7, 0.75),
                BiomeType.Ocean => (0.6, 0.85),
                BiomeType.Mountains => (0.35, 0.4),
                BiomeType.Hills => (0.55, 0.5),
                BiomeType.Cliffs => (0.4, 0.35),
                BiomeType.Beach => (0.85, 0.65),
                _ => (0.6, 0.45)
            };
        }

        /// <summary>
        /// 엔티티 정보 변환
        /// </summary>
        private EntityInfo ConvertToEntityInfo(Models.Entity entity)
        {
            return new EntityInfo
            {
                EntityId = entity.Id,
                EntityType = (EntityType)entity.Type,
                Position = new Vector3D(entity.X, entity.Y, entity.Z),
                Rotation = new Vector3D(entity.RotationX, entity.RotationY, entity.RotationZ),
                Velocity = new Vector3D(entity.VelocityX, entity.VelocityY, entity.VelocityZ),
                Health = entity.Health,
                MaxHealth = entity.MaxHealth,
                CustomData = entity.Data ?? ""
            };
        }

        /// <summary>
        /// 청크 응답 전송
        /// </summary>
        private async Task SendChunkResponse(Session session, ChunkDataResponseMessage response)
        {
            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, response);
            await session.SendAsync((int)MinecraftMessageType.ChunkDataResponse, stream.ToArray());
        }

        /// <summary>
        /// 오류 응답 전송
        /// </summary>
        private async Task SendErrorResponse(Session session, int chunkX, int chunkZ, string errorMessage)
        {
            var errorResponse = new ChunkDataResponseMessage
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Success = false,
                CompressedBlockData = Array.Empty<byte>(),
                Entities = new List<EntityInfo>(),
                BiomeData = new BiomeInfo()
            };

            await SendChunkResponse(session, errorResponse);
            Console.WriteLine($"청크 [{chunkX}, {chunkZ}] 요청 오류 - {errorMessage}");
        }

        /// <summary>
        /// 플레이어의 로드된 청크 목록 업데이트
        /// </summary>
        private async Task HandleChunkUnloadAsync(Session session, ChunkUnloadNotificationMessage message)
        {
            var playerId = session.UserName;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = string.IsNullOrWhiteSpace(message.PlayerId) ? string.Empty : message.PlayerId;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                var ack = new ChunkUnloadAcknowledgeMessage
                {
                    ChunkX = message.ChunkX,
                    ChunkZ = message.ChunkZ,
                    Accepted = false,
                    RemainingChunks = 0,
                    Note = "Unauthenticated session"
                };
                await SendChunkUnloadAckAsync(session, ack);
                return;
            }

            if (!string.IsNullOrEmpty(message.PlayerId) && message.PlayerId != playerId)
            {
                Console.WriteLine($"Chunk unload identity mismatch: session {playerId} reported payload for {message.PlayerId}");
            }

            if (!_playerLoadedChunks.TryGetValue(playerId, out var chunkSet))
            {
                var ack = new ChunkUnloadAcknowledgeMessage
                {
                    ChunkX = message.ChunkX,
                    ChunkZ = message.ChunkZ,
                    Accepted = false,
                    RemainingChunks = 0,
                    Note = "Chunk residency not tracked"
                };
                await SendChunkUnloadAckAsync(session, ack);
                return;
            }

            var chunkKey = ToChunkKey(message.ChunkX, message.ChunkZ);
            var removed = chunkSet.TryRemove(chunkKey, out _);
            if (chunkSet.IsEmpty)
            {
                _playerLoadedChunks.TryRemove(playerId, out _);
                UpdateResidencyMetrics(playerId, null);
            }
            else
            {
                UpdateResidencyMetrics(playerId, chunkSet);
            }

            var remaining = chunkSet.Count;

            var ackMessage = new ChunkUnloadAcknowledgeMessage
            {
                ChunkX = message.ChunkX,
                ChunkZ = message.ChunkZ,
                Accepted = removed,
                RemainingChunks = remaining,
                Note = removed ? message.Reason.ToString() : "Chunk residency not found"
            };

            await SendChunkUnloadAckAsync(session, ackMessage);

            if (removed)
            {
                Console.WriteLine($"Player {playerId} unloaded chunk [{message.ChunkX}, {message.ChunkZ}] (reason: {message.Reason})");
            }
            else
            {
                Console.WriteLine($"Player {playerId} attempted to unload unknown chunk [{message.ChunkX}, {message.ChunkZ}]");
            }
        }

        private async Task SendChunkUnloadAckAsync(Session session, ChunkUnloadAcknowledgeMessage ack)
        {
            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, ack);
            await session.SendAsync((int)MinecraftMessageType.ChunkUnloadAcknowledge, stream.ToArray());
        }

        private Task UpdatePlayerLoadedChunks(string playerId, int chunkX, int chunkZ, int requestedViewDistance)
        {
            var now = DateTime.UtcNow;
            var chunkKey = ToChunkKey(chunkX, chunkZ);
            var chunkSet = _playerLoadedChunks.GetOrAdd(playerId, _ => new ConcurrentDictionary<long, PlayerChunkResidency>());

            chunkSet.AddOrUpdate(
                chunkKey,
                _ => new PlayerChunkResidency(chunkX, chunkZ, now),
                (_, existing) => existing with { LastServedUtc = now });

            var playerState = _sessions.GetPlayerState(playerId);
            var worldId = playerState?.CurrentWorldId ?? 1;
            _sessions.UpdatePlayerWorld(playerId, worldId, chunkX, chunkZ);

            var trimmed = TrimPlayerResidency(playerId, chunkSet, requestedViewDistance, playerState, chunkX, chunkZ, now);
            if (trimmed > 0)
            {
                Console.WriteLine($"Trimmed {trimmed} stale chunks for {playerId}");
            }

            if (chunkSet.IsEmpty)
            {
                _playerLoadedChunks.TryRemove(playerId, out _);
                UpdateResidencyMetrics(playerId, null);
            }
            else
            {
                UpdateResidencyMetrics(playerId, chunkSet);
            }

            Console.WriteLine($"Player {playerId} loaded chunk [{chunkX}, {chunkZ}]");
            return Task.CompletedTask;
        }

        private int TrimPlayerResidency(string playerId, ConcurrentDictionary<long, PlayerChunkResidency> chunkSet, int requestedViewDistance, PlayerState? playerState, int latestChunkX, int latestChunkZ, DateTime now)
        {
            if (now - _lastCleanup >= _residencyCleanupInterval)
            {
                CleanupExpiredResidency(now);
                _lastCleanup = now;

                if (!_playerLoadedChunks.TryGetValue(playerId, out var currentSet) || !ReferenceEquals(currentSet, chunkSet))
                {
                    return 0;
                }
            }

            var safeViewDistance = Math.Max(1, requestedViewDistance);
            var maxRadius = Math.Min(safeViewDistance, Math.Max(1, _worldSettings.ChunkLoadRadius));

            var playerChunkX = latestChunkX;
            var playerChunkZ = latestChunkZ;
            if (playerState != null)
            {
                playerChunkX = (int)Math.Floor(playerState.Position.X / CHUNK_SIZE_X);
                playerChunkZ = (int)Math.Floor(playerState.Position.Z / CHUNK_SIZE_Z);
            }

            var removed = 0;

            foreach (var kvp in chunkSet.ToArray())
            {
                var residency = kvp.Value;
                if (now - residency.LastServedUtc > _chunkResidencyTimeout)
                {
                    if (chunkSet.TryRemove(kvp.Key, out _))
                    {
                        removed++;
                    }
                    continue;
                }

                var distance = Math.Max(Math.Abs(residency.ChunkX - playerChunkX), Math.Abs(residency.ChunkZ - playerChunkZ));
                if (distance > maxRadius + RESIDENCY_DISTANCE_PADDING)
                {
                    if (chunkSet.TryRemove(kvp.Key, out _))
                    {
                        removed++;
                    }
                }
            }

            var width = (maxRadius * 2) + 1;
            var allowedChunks = width * width;
            var currentCount = chunkSet.Count;

            if (currentCount > allowedChunks)
            {
                var overflow = currentCount - allowedChunks;
                foreach (var kvp in chunkSet.ToArray().OrderBy(entry => entry.Value.LastServedUtc).Take(overflow))
                {
                    if (chunkSet.TryRemove(kvp.Key, out _))
                    {
                        removed++;
                    }
                }
            }

            return removed;
        }

        private void CleanupExpiredResidency(DateTime now)
        {
            foreach (var entry in _playerLoadedChunks.ToArray())
            {
                var playerId = entry.Key;
                var chunkSet = entry.Value;
                var state = _sessions.GetPlayerState(playerId);

                if (state == null || !state.IsOnline)
                {
                    _playerLoadedChunks.TryRemove(playerId, out _);
                    UpdateResidencyMetrics(playerId, null);
                    continue;
                }

                foreach (var chunk in chunkSet.ToArray())
                {
                    if (now - chunk.Value.LastServedUtc > _chunkResidencyTimeout)
                    {
                        chunkSet.TryRemove(chunk.Key, out _);
                    }
                }

                if (chunkSet.IsEmpty)
                {
                    _playerLoadedChunks.TryRemove(playerId, out _);
                    UpdateResidencyMetrics(playerId, null);
                }
                else
                {
                    UpdateResidencyMetrics(playerId, chunkSet);
                }
            }
        }

        private void UpdateResidencyMetrics(string playerId, ConcurrentDictionary<long, PlayerChunkResidency>? chunkSet)
        {
            if (_metrics == null || string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            if (chunkSet == null || chunkSet.IsEmpty)
            {
                _metrics.ClearChunkResidency(playerId);
            }
            else
            {
                _metrics.UpdateChunkResidency(playerId, chunkSet.Count);
            }
        }
    }
}
