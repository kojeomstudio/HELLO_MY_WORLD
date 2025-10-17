using GameServerApp.Database;
using GameServerApp.Systems;
using GameServerApp.World;
using SharedProtocol;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

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
                var chunkRequest = ProtoBuf.Serializer.Deserialize<ChunkDataRequestMessage>(new MemoryStream(messageData));
                
                var playerState = _sessions.GetPlayerState(session.UserName!);
                if (playerState == null)
                {
                    await SendErrorResponse(session, chunkRequest.ChunkX, chunkRequest.ChunkZ, "플레이어 상태를 찾을 수 없습니다.");
                    return;
                }

                // 플레이어의 현재 위치와 요청된 청크의 거리 확인
                var playerChunkX = (int)Math.Floor(playerState.Position.X / CHUNK_SIZE_X);
                var playerChunkZ = (int)Math.Floor(playerState.Position.Z / CHUNK_SIZE_Z);
                var distance = Math.Max(Math.Abs(chunkRequest.ChunkX - playerChunkX), Math.Abs(chunkRequest.ChunkZ - playerChunkZ));

                if (distance > chunkRequest.ViewDistance)
                {
                    await SendErrorResponse(session, chunkRequest.ChunkX, chunkRequest.ChunkZ, "요청된 청크가 시야 거리를 벗어났습니다.");
                    return;
                }

                // 청크 데이터 로드 또는 생성
                var chunkResult = await LoadOrGenerateChunk(chunkRequest.ChunkX, chunkRequest.ChunkZ);
                if (chunkResult == null)
                {
                    await SendErrorResponse(session, chunkRequest.ChunkX, chunkRequest.ChunkZ, "Unable to load chunk data.");
                    return;
                }

                var (chunkData, isFromCache) = chunkResult.Value;
                var alreadyServed = HasPlayerLoadedChunk(session.UserName!, chunkRequest.ChunkX, chunkRequest.ChunkZ);

                var entities = await _worldManager.GetEntitiesInChunk(chunkRequest.ChunkX, chunkRequest.ChunkZ);
                var biomeData = GenerateBiomeData(chunkRequest.ChunkX, chunkRequest.ChunkZ);

                var response = new ChunkDataResponseMessage
                {
                    ChunkX = chunkRequest.ChunkX,
                    ChunkZ = chunkRequest.ChunkZ,
                    Success = true,
                    CompressedBlockData = chunkData,
                    Entities = entities.Select(ConvertToEntityInfo).ToList(),
                    BiomeData = biomeData,
                    IsFromCache = isFromCache || alreadyServed
                };

                await SendChunkResponse(session, response);

                await UpdatePlayerLoadedChunks(session.UserName!, chunkRequest.ChunkX, chunkRequest.ChunkZ, chunkRequest.ViewDistance);

                Console.WriteLine($"청크 [{chunkRequest.ChunkX}, {chunkRequest.ChunkZ}] 데이터를 플레이어 {session.UserName}에게 전송 완료");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"청크 요청 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 청크 데이터를 로드하거나 새로 생성
        /// </summary>
        private async Task<(byte[] Data, bool IsFromCache)?> LoadOrGenerateChunk(int chunkX, int chunkZ)
        {
            try
            {
                var existingChunkData = await _database.GetChunkDataAsync(chunkX, chunkZ);
                if (existingChunkData != null)
                {
                    return (CompressChunkData(existingChunkData), true);
                }

                var generatedData = await GenerateNewChunk(chunkX, chunkZ);
                if (generatedData != null)
                {
                    await _database.SaveChunkDataAsync(chunkX, chunkZ, generatedData);
                    return (CompressChunkData(generatedData), false);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chunk [{chunkX}, {chunkZ}] load/generation error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 새로운 청크 생성 (지형 생성 알고리즘)
        /// </summary>
        private async Task<byte[]> GenerateNewChunk(int chunkX, int chunkZ)
        {
            // 청크 블록 데이터 (16x256x16 = 65536 블록)
            var blockData = new byte[CHUNK_SIZE_X * CHUNK_HEIGHT * CHUNK_SIZE_Z];
            
            // 간단한 지형 생성 알고리즘
            var random = new Random(GetChunkSeed(chunkX, chunkZ));
            
            for (int x = 0; x < CHUNK_SIZE_X; x++)
            {
                for (int z = 0; z < CHUNK_SIZE_Z; z++)
                {
                    // 높이 계산 (간단한 노이즈 기반)
                    int worldX = chunkX * CHUNK_SIZE_X + x;
                    int worldZ = chunkZ * CHUNK_SIZE_Z + z;
                    int surfaceHeight = CalculateTerrainHeight(worldX, worldZ);
                    
                    for (int y = 0; y < CHUNK_HEIGHT; y++)
                    {
                        int blockIndex = GetBlockIndex(x, y, z);
                        
                        if (y == 0)
                        {
                            blockData[blockIndex] = 7; // 기반암
                        }
                        else if (y <= surfaceHeight - 4)
                        {
                            blockData[blockIndex] = 1; // 돌
                        }
                        else if (y <= surfaceHeight - 1)
                        {
                            blockData[blockIndex] = 2; // 흙
                        }
                        else if (y == surfaceHeight)
                        {
                            blockData[blockIndex] = (byte)(surfaceHeight > 62 ? 6 : 2); // 잔디 또는 흙 (해수면 기준)
                        }
                        else if (y <= 62) // 해수면
                        {
                            blockData[blockIndex] = 8; // 물
                        }
                        else
                        {
                            blockData[blockIndex] = 0; // 공기
                        }
                    }
                    
                    // 나무 생성 (확률적)
                    if (surfaceHeight > 62 && random.NextDouble() < 0.05) // 5% 확률
                    {
                        await GenerateTree(blockData, x, surfaceHeight + 1, z);
                    }
                }
            }

            return blockData;
        }

        /// <summary>
        /// 지형 높이 계산 (간단한 노이즈 함수)
        /// </summary>
        private int CalculateTerrainHeight(int x, int z)
        {
            // 간단한 사인파 기반 높이 맵
            double noise1 = Math.Sin(x * 0.01) * Math.Sin(z * 0.01) * 20;
            double noise2 = Math.Sin(x * 0.05) * Math.Cos(z * 0.05) * 10;
            double noise3 = Math.Sin(x * 0.1) * Math.Sin(z * 0.1) * 5;
            
            int baseHeight = 64; // 해수면
            int height = baseHeight + (int)(noise1 + noise2 + noise3);
            
            return Math.Clamp(height, 1, CHUNK_HEIGHT - 50); // 최소/최대 높이 제한
        }

        /// <summary>
        /// 나무 생성
        /// </summary>
        private async Task GenerateTree(byte[] blockData, int x, int y, int z)
        {
            if (y + 5 >= CHUNK_HEIGHT) return; // 높이 체크
            
            // 나무 줄기 (5블록 높이)
            for (int treeY = y; treeY < y + 5; treeY++)
            {
                int trunkIndex = GetBlockIndex(x, treeY, z);
                if (trunkIndex < blockData.Length)
                {
                    blockData[trunkIndex] = 3; // 나무 블록
                }
            }
            
            // 나무 잎 (간단한 구형)
            for (int leafX = x - 2; leafX <= x + 2; leafX++)
            {
                for (int leafZ = z - 2; leafZ <= z + 2; leafZ++)
                {
                    for (int leafY = y + 3; leafY <= y + 6; leafY++)
                    {
                        if (leafX >= 0 && leafX < CHUNK_SIZE_X && leafZ >= 0 && leafZ < CHUNK_SIZE_Z)
                        {
                            // 중심에서의 거리 계산
                            double distance = Math.Sqrt((leafX - x) * (leafX - x) + (leafZ - z) * (leafZ - z) + (leafY - (y + 4.5)) * (leafY - (y + 4.5)));
                            if (distance <= 2.5)
                            {
                                int leafIndex = GetBlockIndex(leafX, leafY, leafZ);
                                if (leafIndex < blockData.Length && blockData[leafIndex] == 0) // 공기인 경우만
                                {
                                    blockData[leafIndex] = 5; // 잎 블록
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 청크 시드 계산
        /// </summary>
        private int GetChunkSeed(int chunkX, int chunkZ)
        {
            // 월드 시드와 청크 좌표를 조합하여 고유한 시드 생성
            return (chunkX * 1000000 + chunkZ) ^ 12345; // 간단한 해시
        }

        /// <summary>
        /// 3D 좌표를 1D 배열 인덱스로 변환
        /// </summary>
        private int GetBlockIndex(int x, int y, int z)
        {
            return y * (CHUNK_SIZE_X * CHUNK_SIZE_Z) + z * CHUNK_SIZE_X + x;
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

        /// <summary>
        /// 바이옴 데이터 생성
        /// </summary>
        private BiomeInfo GenerateBiomeData(int chunkX, int chunkZ)
        {
            var biomeIds = new List<int>(16 * 16);
            double accumulatedTemperature = 0;
            double accumulatedHumidity = 0;

            for (int z = 0; z < 16; z++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int worldX = chunkX * 16 + x;
                    int worldZ = chunkZ * 16 + z;

                    var biome = _worldManager.SampleBiome(worldX, worldZ);
                    biomeIds.Add((int)biome);

                    var climate = GetBiomeClimate(biome);
                    accumulatedTemperature += climate.temp;
                    accumulatedHumidity += climate.humidity;
                }
            }

            int sampleCount = biomeIds.Count;
            float averageTemperature = sampleCount > 0 ? (float)(accumulatedTemperature / sampleCount) : 0.5f;
            float averageHumidity = sampleCount > 0 ? (float)(accumulatedHumidity / sampleCount) : 0.5f;

            return new BiomeInfo
            {
                BiomeIds = biomeIds,
                Temperature = averageTemperature,
                Humidity = averageHumidity
            };
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
