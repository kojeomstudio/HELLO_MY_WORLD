using System;
using System.Security.Cryptography;
using System.Text;

namespace GameServerApp.World
{
    /// <summary>
    /// 월드 시드 설정 및 관리
    /// 동일한 시드를 사용하면 동일한 월드가 생성됩니다.
    /// </summary>
    public class WorldSeedConfig
    {
        /// <summary>
        /// 월드 시드 (정수)
        /// </summary>
        public int Seed { get; private set; }

        /// <summary>
        /// 시드의 원본 문자열 (옵션)
        /// </summary>
        public string? SeedString { get; private set; }

        /// <summary>
        /// 시드 생성 시각
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// 시드 버전 (향후 호환성 유지용)
        /// </summary>
        public int Version { get; private set; }

        private WorldSeedConfig()
        {
            Version = 1;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 정수 시드로 생성
        /// </summary>
        public static WorldSeedConfig FromSeed(int seed)
        {
            return new WorldSeedConfig
            {
                Seed = seed,
                SeedString = null,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 문자열에서 시드 생성 (해시 기반)
        /// </summary>
        public static WorldSeedConfig FromString(string seedString)
        {
            if (string.IsNullOrWhiteSpace(seedString))
                throw new ArgumentException("Seed string cannot be empty", nameof(seedString));

            // 문자열을 정수 시드로 변환 (해시 기반)
            int seed = GetHashCode(seedString);

            return new WorldSeedConfig
            {
                Seed = seed,
                SeedString = seedString,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 랜덤 시드 생성
        /// </summary>
        public static WorldSeedConfig Random()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int seed = BitConverter.ToInt32(bytes, 0);

            return new WorldSeedConfig
            {
                Seed = seed,
                SeedString = null,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 특정 영역에 대한 시드 생성
        /// 청크 좌표를 기반으로 결정적인(deterministic) 시드 생성
        /// </summary>
        public int GetChunkSeed(int chunkX, int chunkZ)
        {
            // 청크 좌표를 시드와 결합하여 고유한 시드 생성
            // Cantor pairing function을 사용하여 두 정수를 하나로 결합
            unchecked
            {
                int a = chunkX;
                int b = chunkZ;
                int combined = ((a + b) * (a + b + 1) / 2) + b;
                return Seed ^ combined;
            }
        }

        /// <summary>
        /// 특정 레이어(노이즈 타입 등)에 대한 시드 생성
        /// </summary>
        public int GetLayerSeed(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return Seed;

            return Seed ^ GetHashCode(layerName);
        }

        /// <summary>
        /// 바이옴 시드 생성
        /// </summary>
        public int GetBiomeSeed()
        {
            return GetLayerSeed("biome");
        }

        /// <summary>
        /// 동굴 생성 시드
        /// </summary>
        public int GetCaveSeed()
        {
            return GetLayerSeed("cave");
        }

        /// <summary>
        /// 광석 생성 시드
        /// </summary>
        public int GetOreSeed()
        {
            return GetLayerSeed("ore");
        }

        /// <summary>
        /// 식생 생성 시드
        /// </summary>
        public int GetVegetationSeed()
        {
            return GetLayerSeed("vegetation");
        }

        /// <summary>
        /// 강 생성 시드
        /// </summary>
        public int GetRiverSeed()
        {
            return GetLayerSeed("river");
        }

        /// <summary>
        /// 호수 생성 시드
        /// </summary>
        public int GetLakeSeed()
        {
            return GetLayerSeed("lake");
        }

        /// <summary>
        /// 구조물 생성 시드
        /// </summary>
        public int GetStructureSeed()
        {
            return GetLayerSeed("structure");
        }

        /// <summary>
        /// 문자열의 안정적인 해시코드 생성
        /// (String.GetHashCode()는 플랫폼/버전마다 다를 수 있음)
        /// </summary>
        private static int GetHashCode(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// 시드 정보를 JSON으로 직렬화
        /// </summary>
        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                seed = Seed,
                seed_string = SeedString,
                created_at = CreatedAt.ToString("O"),
                version = Version
            });
        }

        /// <summary>
        /// JSON에서 시드 정보 역직렬화
        /// </summary>
        public static WorldSeedConfig? FromJson(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                var config = new WorldSeedConfig
                {
                    Seed = root.GetProperty("seed").GetInt32(),
                    SeedString = root.TryGetProperty("seed_string", out var seedStr) && seedStr.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? seedStr.GetString()
                        : null,
                    CreatedAt = root.TryGetProperty("created_at", out var createdAt)
                        ? DateTime.Parse(createdAt.GetString()!)
                        : DateTime.UtcNow,
                    Version = root.TryGetProperty("version", out var version)
                        ? version.GetInt32()
                        : 1
                };

                return config;
            }
            catch
            {
                return null;
            }
        }

        public override string ToString()
        {
            return SeedString != null
                ? $"Seed: {Seed} (from '{SeedString}')"
                : $"Seed: {Seed}";
        }
    }
}
