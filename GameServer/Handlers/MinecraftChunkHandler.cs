using GameServerApp.Database;
using GameServerApp.World;
using SharedProtocol;
using System.IO;
using System.IO.Compression;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// 마인크래프트 청크 데이터 요청을 처리하는 핸들러
    /// 클라이언트의 시야 거리에 따른 청크 로딩 및 언로딩을 관리합니다.
    /// </summary>
    public class MinecraftChunkHandler : IMessageHandler
    {
        private readonly DatabaseHelper _database;
        private readonly SessionManager _sessions;
        private readonly WorldManager _worldManager;
        
        // 청크 크기 상수 (표준 마인크래프트)
        private const int CHUNK_SIZE_X = 16;
        private const int CHUNK_SIZE_Z = 16; 
        private const int CHUNK_HEIGHT = 256;
        
        // 청크 압축 임계값 (바이트)
        private const int COMPRESSION_THRESHOLD = 1024;

        public MinecraftChunkHandler(DatabaseHelper database, SessionManager sessions, WorldManager worldManager)
        {
            _database = database;
            _sessions = sessions;
            _worldManager = worldManager;
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
                var chunkData = await LoadOrGenerateChunk(chunkRequest.ChunkX, chunkRequest.ChunkZ);
                if (chunkData == null)
                {
                    await SendErrorResponse(session, chunkRequest.ChunkX, chunkRequest.ChunkZ, "청크 데이터를 로드할 수 없습니다.");
                    return;
                }

                // 청크 내 엔티티 정보 수집
                var entities = await _worldManager.GetEntitiesInChunk(chunkRequest.ChunkX, chunkRequest.ChunkZ);

                // 바이옴 데이터 생성
                var biomeData = GenerateBiomeData(chunkRequest.ChunkX, chunkRequest.ChunkZ);

                // 응답 생성 및 전송
                var response = new ChunkDataResponseMessage
                {
                    ChunkX = chunkRequest.ChunkX,
                    ChunkZ = chunkRequest.ChunkZ,
                    Success = true,
                    CompressedBlockData = chunkData,
                    Entities = entities.Select(ConvertToEntityInfo).ToList(),
                    BiomeData = biomeData,
                    IsFromCache = false // TODO: 캐시 여부 추적
                };

                await SendChunkResponse(session, response);

                // 플레이어의 로드된 청크 목록 업데이트
                await UpdatePlayerLoadedChunks(session.UserName!, chunkRequest.ChunkX, chunkRequest.ChunkZ);

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
        private async Task<byte[]?> LoadOrGenerateChunk(int chunkX, int chunkZ)
        {
            try
            {
                // 먼저 데이터베이스에서 기존 청크 데이터 확인
                var existingChunkData = await _database.GetChunkDataAsync(chunkX, chunkZ);
                if (existingChunkData != null)
                {
                    return CompressChunkData(existingChunkData);
                }

                // 기존 데이터가 없으면 새로 생성
                var generatedData = await GenerateNewChunk(chunkX, chunkZ);
                if (generatedData != null)
                {
                    // 생성된 데이터를 데이터베이스에 저장
                    await _database.SaveChunkDataAsync(chunkX, chunkZ, generatedData);
                    return CompressChunkData(generatedData);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"청크 [{chunkX}, {chunkZ}] 로드/생성 오류: {ex.Message}");
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
            var biomeIds = new List<int>();
            
            // 16x16 바이옴 배열 생성
            for (int z = 0; z < 16; z++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int worldX = chunkX * 16 + x;
                    int worldZ = chunkZ * 16 + z;
                    
                    // 간단한 바이옴 결정 (온도와 습도 기반)
                    double temperature = Math.Sin(worldX * 0.001) + Math.Cos(worldZ * 0.001);
                    double humidity = Math.Sin(worldX * 0.002) * Math.Cos(worldZ * 0.002);
                    
                    int biomeId = DetermineBiome(temperature, humidity);
                    biomeIds.Add(biomeId);
                }
            }
            
            return new BiomeInfo
            {
                BiomeIds = biomeIds,
                Temperature = 0.8f, // 평균 온도
                Humidity = 0.6f     // 평균 습도
            };
        }

        /// <summary>
        /// 온도와 습도에 따른 바이옴 결정
        /// </summary>
        private int DetermineBiome(double temperature, double humidity)
        {
            return (temperature, humidity) switch
            {
                ( > 0.5, > 0.5) => 1,    // 정글
                ( > 0.5, <= 0.5) => 2,  // 사막
                ( <= 0.5, > 0.5) => 3,  // 늪지
                _ => 0                   // 평원 (기본)
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
        private async Task UpdatePlayerLoadedChunks(string playerId, int chunkX, int chunkZ)
        {
            // TODO: 플레이어별 로드된 청크 추적 및 관리
            // 시야 거리를 벗어난 청크는 언로드하고, 새로운 청크는 로드 목록에 추가
            Console.WriteLine($"Player {playerId} loaded chunk [{chunkX}, {chunkZ}]");
        }
    }
}