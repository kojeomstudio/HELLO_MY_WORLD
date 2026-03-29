using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GameCommon.Blocks
{
    /// <summary>
    /// 블록 속성 레지스트리
    /// JSON 파일에서 블록 정의를 로드하고 관리
    /// </summary>
    public static class BlockRegistry
    {
        private static Dictionary<BlockType, BlockProperties> _registry = new();
        private static bool _initialized = false;

        /// <summary>
        /// JSON 파일에서 블록 정의 로드
        /// </summary>
        public static void LoadFromJson(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Block configuration file not found: {jsonPath}");
            }

            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var blockList = JsonSerializer.Deserialize<List<BlockProperties>>(jsonContent, options);

                if (blockList == null)
                {
                    throw new InvalidOperationException("Failed to deserialize block configuration");
                }

                _registry.Clear();
                foreach (var block in blockList)
                {
                    _registry[block.Type] = block;
                }

                _initialized = true;
                Console.WriteLine($"Loaded {_registry.Count} block definitions from {jsonPath}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse block configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 블록 타입으로 속성 조회
        /// </summary>
        public static BlockProperties Get(BlockType type)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("BlockRegistry not initialized. Call LoadFromJson first.");
            }

            if (_registry.TryGetValue(type, out var properties))
            {
                return properties;
            }

            // 기본값 반환 (Air)
            return _registry.GetValueOrDefault(BlockType.Air, new BlockProperties
            {
                Type = BlockType.Air,
                Name = "air",
                DisplayName = "Air"
            });
        }

        /// <summary>
        /// 블록 타입 존재 여부 확인
        /// </summary>
        public static bool Contains(BlockType type)
        {
            return _registry.ContainsKey(type);
        }

        /// <summary>
        /// 모든 등록된 블록 타입 반환
        /// </summary>
        public static IEnumerable<BlockType> GetAllTypes()
        {
            return _registry.Keys;
        }

        /// <summary>
        /// 이름으로 블록 타입 조회
        /// </summary>
        public static BlockType? GetTypeByName(string name)
        {
            foreach (var kvp in _registry)
            {
                if (kvp.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 레지스트리 초기화 (테스트용)
        /// </summary>
        public static void Reset()
        {
            _registry.Clear();
            _initialized = false;
        }
    }
}
