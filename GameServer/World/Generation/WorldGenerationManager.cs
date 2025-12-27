using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameServerApp.World.Generation.Content;
using GameServerApp.World.Generation.Core;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Main world generation manager that coordinates all generation layers
    /// </summary>
    public class WorldGenerationManager
    {
        private readonly List<ICoreLayer> _coreLayers;
        private readonly List<IContentLayer> _contentLayers;
        private readonly WorldGenerationConfig _config;
        private readonly FastNoise _noise;
        
        public WorldGenerationConfig Config => _config;
        
        public WorldGenerationManager(WorldGenerationConfig config = null)
        {
            _config = config ?? CreateDefaultConfig();
            _coreLayers = new List<ICoreLayer>();
            _contentLayers = new List<IContentLayer>();
            _noise = new FastNoise();
            
            InitializeLayers();
        }
        
        /// <summary>
        /// Initializes all generation layers
        /// </summary>
        private void InitializeLayers()
        {
            // Initialize core layers
            _coreLayers.Add(new BiomeGenerationLayer(_config.BiomeConfig));
            
            // Initialize content layers
            _contentLayers.Add(new OreDistributionLayer(_config.OreDistributionConfig));
            _contentLayers.Add(new StructureGenerationLayer(_config.StructureGenerationConfig));
            _contentLayers.Add(new EntitySpawnLayer(_config.EntitySpawnConfig));
            
            // Sort layers by priority
            _coreLayers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _contentLayers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        
        /// <summary>
        /// Generates a chunk at the specified coordinates
        /// </summary>
        public TerrainGenerationContext GenerateChunk(int chunkX, int chunkZ, int worldSeed)
        {
            var context = new TerrainGenerationContext
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Seed = worldSeed,
                Config = _config
            };
            
            // Initialize the context
            context.Initialize();
            
            // Execute core layers first
            foreach (var layer in _coreLayers.Where(l => l.IsEnabled))
            {
                try
                {
                    layer.Execute(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldGenerationManager] Error in core layer {layer.LayerId}: {ex.Message}");
                }
            }
            
            // Execute content layers
            foreach (var layer in _contentLayers.Where(l => l.IsEnabled))
            {
                try
                {
                    layer.Execute(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldGenerationManager] Error in content layer {layer.LayerId}: {ex.Message}");
                }
            }
            
            // Generate final terrain
            GenerateFinalTerrain(context);
            
            Console.WriteLine($"[WorldGenerationManager] Generated chunk ({chunkX},{chunkZ}) with seed {worldSeed}");
            
            return context;
        }
        
        /// <summary>
        /// Generates the final terrain based on all layer data
        /// </summary>
        private void GenerateFinalTerrain(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.MaxHeight;
            var seaLevel = (int)(maxHeight * _config.World.SeaLevel);
            
            // Generate base terrain
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Get biome and height data
                    var biome = context.GetBiome(x, z);
                    var height = context.GetHeight(x, z);
                    
                    // Generate terrain columns
                    GenerateTerrainColumn(context, x, z, biome, height, seaLevel);
                    
                    // Apply caves
                    ApplyCaves(context, x, z);
                    
                    // Apply rivers
                    ApplyRivers(context, x, z, seaLevel);
                    
                    // Apply lakes
                    ApplyLakes(context, x, z, seaLevel);
                    
                    // Apply ore distribution
                    ApplyOreDistribution(context, x, z);
                    
                    // Apply structures
                    ApplyStructures(context, x, z);
                }
            }
        }
        
        /// <summary>
        /// Generates a terrain column at the specified position
        /// </summary>
        private void GenerateTerrainColumn(TerrainGenerationContext context, int x, int z, BiomeType biome, int height, int seaLevel)
        {
            var maxHeight = context.MaxHeight;
            var temperature = context.GetTemperature(x, z);
            var humidity = context.GetHumidity(x, z);
            
            // Get biome configuration
            var biomeConfig = _config.BiomeConfig.Biomes.FirstOrDefault(b => b.Type == biome);
            if (biomeConfig == null)
            {
                biomeConfig = _config.BiomeConfig.Biomes.FirstOrDefault(b => b.Type == BiomeType.Plains);
            }
            
            // Generate terrain layers
            for (int y = 0; y < maxHeight; y++)
            {
                int blockType;
                
                if (y > height)
                {
                    // Air above surface
                    blockType = 0;
                }
                else if (y == height)
                {
                    // Surface block
                    blockType = GetSurfaceBlockType(biomeConfig, temperature, humidity);
                }
                else if (y > height - 3)
                {
                    // Sub-surface layer
                    blockType = GetSubSurfaceBlockType(biomeConfig, temperature, humidity);
                }
                else if (y > seaLevel - 5)
                {
                    // Dirt layer
                    blockType = 3; // Dirt
                }
                else
                {
                    // Stone layer
                    blockType = 1; // Stone
                }
                
                // Apply water for ocean biomes
                if (biome == BiomeType.Ocean && y <= seaLevel)
                {
                    blockType = 8; // Water
                }
                else if (biome == BiomeType.River && y <= seaLevel + 1)
                {
                    blockType = 8; // Water
                }
                
                context.SetBlockType(x, y, z, blockType);
            }
        }
        
        /// <summary>
        /// Gets the surface block type based on biome and climate
        /// </summary>
        private int GetSurfaceBlockType(BiomeDefinition biomeConfig, float temperature, float humidity)
        {
            if (biomeConfig != null && !string.IsNullOrEmpty(biomeConfig.SurfaceBlock))
            {
                return GetBlockIdByName(biomeConfig.SurfaceBlock);
            }
            
            // Default surface blocks based on biome
            switch (biomeConfig?.Type)
            {
                case BiomeType.Desert:
                    return 12; // Sand
                case BiomeType.Ocean:
                case BiomeType.River:
                case BiomeType.Beach:
                    return 12; // Sand
                case BiomeType.SnowyTundra:
                    return 80; // Snow
                case BiomeType.Swamp:
                    return 2; // Grass
                default:
                    return 2; // Grass
            }
        }
        
        /// <summary>
        /// Gets the sub-surface block type based on biome and climate
        /// </summary>
        private int GetSubSurfaceBlockType(BiomeDefinition biomeConfig, float temperature, float humidity)
        {
            if (biomeConfig != null && !string.IsNullOrEmpty(biomeConfig.SubSurfaceBlock))
            {
                return GetBlockIdByName(biomeConfig.SubSurfaceBlock);
            }
            
            // Default sub-surface blocks based on biome
            switch (biomeConfig?.Type)
            {
                case BiomeType.Desert:
                case BiomeType.Ocean:
                case BiomeType.River:
                case BiomeType.Beach:
                    return 24; // Sandstone
                case BiomeType.SnowyTundra:
                    return 3; // Dirt
                default:
                    return 3; // Dirt
            }
        }
        
        /// <summary>
        /// Applies caves to the terrain
        /// </summary>
        private void ApplyCaves(TerrainGenerationContext context, int x, int z)
        {
            var maxHeight = context.MaxHeight;
            
            for (int y = 0; y < maxHeight; y++)
            {
                if (context.IsCave(x, y, z))
                {
                    context.SetBlockType(x, y, z, 0); // Air
                }
            }
        }
        
        /// <summary>
        /// Applies rivers to the terrain
        /// </summary>
        private void ApplyRivers(TerrainGenerationContext context, int x, int z, int seaLevel)
        {
            var maxHeight = context.MaxHeight;
            
            for (int y = 0; y < maxHeight; y++)
            {
                if (context.IsRiver(x, y, z))
                {
                    context.SetBlockType(x, y, z, 8); // Water
                }
            }
        }
        
        /// <summary>
        /// Applies lakes to the terrain
        /// </summary>
        private void ApplyLakes(TerrainGenerationContext context, int x, int z, int seaLevel)
        {
            var maxHeight = context.MaxHeight;
            
            for (int y = 0; y < maxHeight; y++)
            {
                if (context.IsLake(x, y, z))
                {
                    context.SetBlockType(x, y, z, 8); // Water
                }
            }
        }
        
        /// <summary>
        /// Applies ore distribution to the terrain
        /// </summary>
        private void ApplyOreDistribution(TerrainGenerationContext context, int x, int z)
        {
            var oreData = context.OreData[x, z];
            if (oreData == null) return;
            
            var maxHeight = context.MaxHeight;
            var height = context.GetHeight(x, z);
            
            // Apply ore veins based on distribution data
            foreach (var oreVein in oreData.OreVeins)
            {
                var oreType = _config.OreDistributionConfig.OreTypes.FirstOrDefault(o => o.Name == oreVein.Key);
                if (oreType == null) continue;
                
                // Generate ore vein at appropriate depth
                var veinDepth = Math.Max(oreType.MinDepth, Math.Min(oreType.MaxDepth, oreData.Depth));
                var veinY = height - veinDepth;
                
                if (veinY >= 0 && veinY < maxHeight)
                {
                    context.SetBlockType(x, veinY, z, oreType.BlockId);
                }
            }
        }
        
        /// <summary>
        /// Applies structures to the terrain
        /// </summary>
        private void ApplyStructures(TerrainGenerationContext context, int x, int z)
        {
            var structureData = context.StructureData[x, z];
            if (structureData?.Structure == null) return;
            
            var structure = structureData.Structure;
            var template = structure.Template;
            
            if (template == null || template.Blocks == null) return;
            
            // Apply structure blocks to terrain
            for (int sx = 0; sx < template.Size.X; sx++)
            {
                for (int sy = 0; sy < template.Size.Y; sy++)
                {
                    for (int sz = 0; sz < template.Size.Z; sz++)
                    {
                        var worldX = structure.Position.X + sx;
                        var worldY = structure.Position.Y + sy;
                        var worldZ = structure.Position.Z + sz;
                        
                        var localX = worldX - context.ChunkX * context.ChunkSize;
                        var localZ = worldZ - context.ChunkZ * context.ChunkSize;
                        
                        if (localX >= 0 && localX < context.ChunkSize && 
                            worldY >= 0 && worldY < context.MaxHeight && 
                            localZ >= 0 && localZ < context.ChunkSize)
                        {
                            var block = template.Blocks[sx, sy, sz];
                            if (block != null && block.BlockId != 0)
                            {
                                context.SetBlockType(localX, worldY, localZ, block.BlockId);
                                context.SetBlockMetadata(localX, worldY, localZ, block.Metadata);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets a block ID by name
        /// </summary>
        private int GetBlockIdByName(string blockName)
        {
            // Simple mapping of block names to IDs
            var blockMap = new Dictionary<string, int>
            {
                ["air"] = 0,
                ["stone"] = 1,
                ["grass"] = 2,
                ["dirt"] = 3,
                ["cobblestone"] = 4,
                ["wood_planks"] = 5,
                ["sapling"] = 6,
                ["bedrock"] = 7,
                ["water"] = 8,
                ["lava"] = 10,
                ["sand"] = 12,
                ["gravel"] = 13,
                ["gold_ore"] = 14,
                ["iron_ore"] = 15,
                ["coal_ore"] = 16,
                ["wood"] = 17,
                ["leaves"] = 18,
                ["glass"] = 20,
                ["lapis_ore"] = 21,
                ["lapis_block"] = 22,
                ["sandstone"] = 24,
                ["note_block"] = 25,
                ["bed"] = 26,
                ["powered_rail"] = 27,
                ["detector_rail"] = 28,
                ["sticky_piston"] = 29,
                ["web"] = 30,
                ["tall_grass"] = 31,
                ["dead_bush"] = 32,
                ["piston"] = 33,
                ["piston_head"] = 34,
                ["wool"] = 35,
                ["flower"] = 37,
                ["flower_pot"] = 38,
                ["mushroom"] = 39,
                ["gold_block"] = 41,
                ["iron_block"] = 42,
                ["stone_slab"] = 44,
                ["brick_block"] = 45,
                ["tnt"] = 46,
                ["bookshelf"] = 47,
                ["mossy_cobblestone"] = 48,
                ["obsidian"] = 49,
                ["torch"] = 50,
                ["fire"] = 51,
                ["mob_spawner"] = 52,
                ["wood_stairs"] = 53,
                ["chest"] = 54,
                ["redstone_wire"] = 55,
                ["diamond_ore"] = 56,
                ["diamond_block"] = 57,
                ["crafting_table"] = 58,
                ["wheat"] = 59,
                ["farmland"] = 60,
                ["furnace"] = 61,
                ["sign"] = 63,
                ["wooden_door"] = 64,
                ["ladder"] = 65,
                ["rail"] = 66,
                ["stone_stairs"] = 67,
                ["lever"] = 69,
                ["stone_pressure_plate"] = 70,
                ["iron_door"] = 71,
                ["wooden_pressure_plate"] = 72,
                ["redstone_ore"] = 73,
                ["redstone_torch"] = 75,
                ["stone_button"] = 77,
                ["snow"] = 78,
                ["ice"] = 79,
                ["snow_block"] = 80,
                ["cactus"] = 81,
                ["clay"] = 82,
                ["sugar_cane"] = 83,
                ["jukebox"] = 84,
                ["fence"] = 85,
                ["pumpkin"] = 86,
                ["netherrack"] = 87,
                ["soul_sand"] = 88,
                ["glowstone"] = 89,
                ["jack_o_lantern"] = 91,
                ["cake"] = 92,
                ["redstone_repeater"] = 93,
                ["stained_glass"] = 95,
                ["trapdoor"] = 96,
                ["monster_egg"] = 97,
                ["stone_bricks"] = 98,
                ["brown_mushroom_block"] = 99,
                ["red_mushroom_block"] = 100,
                ["iron_bars"] = 101,
                ["glass_pane"] = 102,
                ["melon"] = 103,
                ["pumpkin_stem"] = 104,
                ["melon_stem"] = 105,
                ["vine"] = 106,
                ["fence_gate"] = 107,
                ["brick_stairs"] = 108,
                ["stone_brick_stairs"] = 109,
                ["mycelium"] = 110,
                ["lily_pad"] = 111,
                ["nether_brick"] = 112,
                ["nether_brick_fence"] = 113,
                ["nether_brick_stairs"] = 114,
                ["nether_wart"] = 115,
                ["enchanting_table"] = 116,
                ["brewing_stand"] = 117,
                ["cauldron"] = 118,
                ["end_portal"] = 119,
                ["end_portal_frame"] = 120,
                ["end_stone"] = 121,
                ["dragon_egg"] = 122,
                ["redstone_lamp"] = 123,
                ["cocoa"] = 127,
                ["sandstone_stairs"] = 128,
                ["emerald_ore"] = 129,
                ["ender_chest"] = 130,
                ["tripwire_hook"] = 131,
                ["tripwire"] = 132,
                ["emerald_block"] = 133,
                ["spruce_stairs"] = 134,
                ["birch_stairs"] = 135,
                ["jungle_stairs"] = 136,
                ["command_block"] = 137,
                ["beacon"] = 138,
                ["cobblestone_wall"] = 139,
                ["flower_pot"] = 140,
                ["carrots"] = 141,
                ["potatoes"] = 142,
                ["wooden_button"] = 143,
                ["skull"] = 144,
                ["anvil"] = 145,
                ["trapped_chest"] = 146,
                ["light_weighted_pressure_plate"] = 147,
                ["heavy_weighted_pressure_plate"] = 148,
                ["comparator"] = 149,
                ["daylight_detector"] = 151,
                ["redstone_block"] = 152,
                ["nether_quartz_ore"] = 153,
                ["hopper"] = 154,
                ["quartz_block"] = 155,
                ["quartz_stairs"] = 156,
                ["activator_rail"] = 157,
                ["dropper"] = 158,
                ["stained_hardened_clay"] = 159,
                ["stained_glass_pane"] = 160,
                ["leaves2"] = 161,
                ["log2"] = 162,
                ["acacia_stairs"] = 163,
                ["dark_oak_stairs"] = 164,
                ["slime"] = 165,
                ["barrier"] = 166,
                ["iron_trapdoor"] = 167,
                ["prismarine"] = 168,
                ["sea_lantern"] = 169,
                ["hay_block"] = 170,
                ["carpet"] = 171,
                ["hardened_clay"] = 172,
                ["coal_block"] = 173,
                ["packed_ice"] = 174,
                ["double_plant"] = 175,
                ["standing_banner"] = 176,
                ["wall_banner"] = 177,
                ["daylight_detector_inverted"] = 178,
                ["red_sandstone"] = 179,
                ["red_sandstone_stairs"] = 180,
                ["double_stone_slab"] = 181,
                ["double_wooden_slab"] = 182,
                ["spruce_fence_gate"] = 183,
                ["birch_fence_gate"] = 184,
                ["jungle_fence_gate"] = 185,
                ["dark_oak_fence_gate"] = 186,
                ["acacia_fence_gate"] = 187,
                ["spruce_fence"] = 188,
                ["birch_fence"] = 189,
                ["jungle_fence"] = 190,
                ["dark_oak_fence"] = 191,
                ["acacia_fence"] = 192,
                ["spruce_door"] = 193,
                ["birch_door"] = 194,
                ["jungle_door"] = 195,
                ["acacia_door"] = 196,
                ["dark_oak_door"] = 197
            };
            
            return blockMap.TryGetValue(blockName.ToLower(), out var blockId) ? blockId : 1; // Default to stone
        }
        
        /// <summary>
        /// Creates a default world generation configuration
        /// </summary>
        private WorldGenerationConfig CreateDefaultConfig()
        {
            return new WorldGenerationConfig
            {
                BiomeConfig = BiomeConfigFactory.CreateDefault(),
                OreDistributionConfig = OreDistributionConfigFactory.CreateDefault(),
                StructureGenerationConfig = StructureGenerationConfigFactory.CreateDefault(),
                EntitySpawnConfig = EntitySpawnConfigFactory.CreateDefault(),
                World = new WorldConfig
                {
                    Height = 256,
                    SeaLevel = 64,
                    ClimateZones = 8
                }
            };
        }
        
        /// <summary>
        /// Loads configuration from a JSON file
        /// </summary>
        public static WorldGenerationConfig LoadConfig(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<WorldGenerationConfig>(json);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldGenerationManager] Error loading config from {filePath}: {ex.Message}");
            }
            
            // Return default config if loading fails
            var manager = new WorldGenerationManager();
            return manager.Config;
        }
        
        /// <summary>
        /// Saves configuration to a JSON file
        /// </summary>
        public static void SaveConfig(WorldGenerationConfig config, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, json);
                Console.WriteLine($"[WorldGenerationManager] Saved config to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldGenerationManager] Error saving config to {filePath}: {ex.Message}");
            }
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GameServerApp.World.Generation.Content;
using GameServerApp.World.Generation.Core;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Main world generation manager that coordinates all generation layers
    /// </summary>
    public class WorldGenerationManager
    {
        private readonly List<ICoreLayer> _coreLayers;
        private readonly List<IContentLayer> _contentLayers;
        private readonly WorldGenerationConfig _config;
        private readonly FastNoise _noise;
        
        public WorldGenerationConfig Config => _config;
        
        public WorldGenerationManager(WorldGenerationConfig config = null)
        {
            _config = config ?? CreateDefaultConfig();
            _coreLayers = new List<ICoreLayer>();
            _contentLayers = new List<IContentLayer>();
            _noise = new FastNoise();
            
            InitializeLayers();
        }
        
        /// <summary>
        /// Initializes all generation layers
        /// </summary>
        private void InitializeLayers()
        {
            // Initialize core layers
            _coreLayers.Add(new BiomeGenerationLayer(_config.BiomeConfig));
            
            // Initialize content layers
            _contentLayers.Add(new OreDistributionLayer(_config.OreDistributionConfig));
            _contentLayers.Add(new StructureGenerationLayer(_config.StructureGenerationConfig));
            _contentLayers.Add(new EntitySpawnLayer(_config.EntitySpawnConfig));
            
            // Sort layers by priority
            _coreLayers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            _contentLayers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        
        /// <summary>
        /// Generates a chunk at the specified coordinates
        /// </summary>
        public TerrainGenerationContext GenerateChunk(int chunkX, int chunkZ, int worldSeed)
        {
            var context = new TerrainGenerationContext
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Seed = worldSeed,
                Config = _config
            };
            
            // Initialize the context
            context.Initialize();
            
            // Execute core layers first
            foreach (var layer in _coreLayers.Where(l => l.IsEnabled))
            {
                try
                {
                    layer.Execute(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldGenerationManager] Error in core layer {layer.LayerId}: {ex.Message}");
                }
            }
            
            // Execute content layers
            foreach (var layer in _contentLayers.Where(l => l.IsEnabled))
            {
                try
                {
                    layer.Execute(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldGenerationManager] Error in content layer {layer.LayerId}: {ex.Message}");
                }
            }
            
            // Generate final terrain
            GenerateFinalTerrain(context);
            
            Console.WriteLine($"[WorldGenerationManager] Generated chunk ({chunkX},{chunkZ}) with seed {worldSeed}");
            
            return context;
        }
        
        /// <summary>
        /// Generates the final terrain based on all layer data
        /// </summary>
        private void GenerateFinalTerrain(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var maxHeight = context.MaxHeight;
            var seaLevel = (int)(maxHeight * _config.World.SeaLevel);
            
            // Generate base terrain
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Get biome and height data
                    var biome = context.GetBiome(x, z);
                    var height = context.GetHeight(x, z);
                    
                    // Generate terrain columns
                    GenerateTerrainColumn(context, x, z, biome, height, seaLevel);
                    
                    // Apply caves
                    ApplyCaves(context, x, z);
                    
                    // Apply rivers
                    ApplyRivers(context, x, z, seaLevel);
                    
                    // Apply lakes
                    ApplyLakes(context, x, z, seaLevel);
                    
                    // Apply ore distribution
                    ApplyOreDistribution(context, x, z);
                    
                    // Apply structures
                    ApplyStructures(context, x, z);
                }
            }
        }
        
        /// <summary>
        /// Generates a terrain column at the specified position
        /// </summary>
        private void GenerateTerrainColumn(TerrainGenerationContext context, int x, int z, BiomeType biome, int height, int seaLevel)
        {
            var maxHeight = context.MaxHeight;
            var temperature = context.GetTemperature(x, z);
            var humidity = context.GetHumidity(x, z);
            
            // Get biome configuration
            var biomeConfig = _config.BiomeConfig.Biomes.FirstOrDefault(b => b.Type == biome);
            if (biomeConfig == null)
            {
                biomeConfig = _config.BiomeConfig.Biomes.FirstOrDefault(b => b.Type == BiomeType.Plains);
            }
            
            // Generate terrain layers
            for (int y = 0; y < maxHeight; y++)
            {
                int blockType;
                
                if (y > height)
                {
                    // Air above surface
                    blockType = 0;
                }
                else if (y == height)
                {
                    // Surface block
                    blockType = GetSurfaceBlockType(biomeConfig, temperature, humidity);
                }
                else if (y > height - 3)
                {
                    // Sub-surface layer
                    blockType = GetSubSurfaceBlockType(biomeConfig, temperature, humidity);
                }
                else if (y > seaLevel - 5)
                {
                    // Dirt layer
                    blockType = 3; // Dirt
                }
                else
                {
                    // Stone layer
                    blockType = 1; // Stone
                }
                
                // Apply water for ocean biomes
                if (biome == BiomeType.Ocean && y <= seaLevel)
                {
                    blockType = 8; // Water
                }
                else if (biome == BiomeType.River && y <= seaLevel + 1)
                {
                    blockType = 8; // Water
                }
                
                context.SetBlockType(x, y, z, blockType);
            }
        }
        
        /// <summary>
        /// Gets the surface block type based on biome and climate
        /// </summary>
        private int GetSurfaceBlockType(BiomeDefinition biomeConfig, float temperature, float humidity)
        {
            if (biomeConfig != null && !string.IsNullOrEmpty(biomeConfig.SurfaceBlock))
            {
                return GetBlockIdByName(biomeConfig.SurfaceBlock);
            }
            
            // Default surface blocks based on biome
            switch (biomeConfig?.Type)
            {
                case BiomeType.Desert:
                    return 12; // Sand
                case BiomeType.Ocean:
                case BiomeType.River:
                case BiomeType.Beach:
                    return 12; // Sand
                case BiomeType.SnowyTundra:
                    return 80; // Snow
                case BiomeType.Swamp:
                    return 2; // Grass
                default:
                    return 2; // Grass
            }
        }
        
        /// <summary>
        /// Gets the sub-surface block type based on biome and climate
        /// </summary>
        private int GetSubSurfaceBlockType(BiomeDefinition biomeConfig, float temperature, float humidity)
        {
            if (biomeConfig != null && !string.IsNullOrEmpty(biomeConfig.SubSurfaceBlock))
            {
                return GetBlockIdByName(biomeConfig.SubSurfaceBlock);
            }
            
            // Default sub-surface blocks based on biome
            switch (biomeConfig?.Type)
            {
                case BiomeType.Desert:
                case BiomeType.Ocean:
                case BiomeType.River:
                case BiomeType.Beach:
                    return 24; // Sandstone
                case BiomeType.SnowyTundra:
                    return 3; // Dirt
                default:
                    return 3; // Dirt
            }
        }
        
        /// <summary>
        /// Applies caves to the terrain
        /// </summary>
        private void ApplyCaves(TerrainGenerationContext context, int x, int z)
        {
            var maxHeight = context.MaxHeight;
            
            for (int y = 0; y < maxHeight; y++)
            {
                if (context.IsCave(x, y, z))
                {
                    context.SetBlockType(x, y, z, 0); // Air
                }
            }
        }
        
        /// <summary>
        /// Applies rivers to the terrain
        /// </summary>
        private void ApplyRivers(TerrainGenerationContext context, int x, int z, int seaLevel)
        {
            var maxHeight = context.MaxHeight;
            
            for (int y = 0; y < maxHeight; y++)
            {
                if (context.IsRiver(x, y, z))
                {
                    context.SetBlockType(x, y, z, 8); // Water
                }
            }
        }
        
        /// <summary>
        /// Applies lakes to the terrain
        /// </summary>
        private void ApplyLakes(TerrainGenerationContext context, int x, int z, int seaLevel)
        {
            var maxHeight = context.MaxHeight;
            
            for (int y = 0; y < maxHeight; y++)
            {
                if (context.IsLake(x, y, z))
                {
                    context.SetBlockType(x, y, z, 8); // Water
                }
            }
        }
        
        /// <summary>
        /// Applies ore distribution to the terrain
        /// </summary>
        private void ApplyOreDistribution(TerrainGenerationContext context, int x, int z)
        {
            var oreData = context.OreData[x, z];
            if (oreData == null) return;
            
            var maxHeight = context.MaxHeight;
            var height = context.GetHeight(x, z);
            
            // Apply ore veins based on distribution data
            foreach (var oreVein in oreData.OreVeins)
            {
                var oreType = _config.OreDistributionConfig.OreTypes.FirstOrDefault(o => o.Name == oreVein.Key);
                if (oreType == null) continue;
                
                // Generate ore vein at appropriate depth
                var veinDepth = Math.Max(oreType.MinDepth, Math.Min(oreType.MaxDepth, oreData.Depth));
                var veinY = height - veinDepth;
                
                if (veinY >= 0 && veinY < maxHeight)
                {
                    context.SetBlockType(x, veinY, z, oreType.BlockId);
                }
            }
        }
        
        /// <summary>
        /// Applies structures to the terrain
        /// </summary>
        private void ApplyStructures(TerrainGenerationContext context, int x, int z)
        {
            var structureData = context.StructureData[x, z];
            if (structureData?.Structure == null) return;
            
            var structure = structureData.Structure;
            var template = structure.Template;
            
            if (template == null || template.Blocks == null) return;
            
            // Apply structure blocks to terrain
            for (int sx = 0; sx < template.Size.X; sx++)
            {
                for (int sy = 0; sy < template.Size.Y; sy++)
                {
                    for (int sz = 0; sz < template.Size.Z; sz++)
                    {
                        var worldX = structure.Position.X + sx;
                        var worldY = structure.Position.Y + sy;
                        var worldZ = structure.Position.Z + sz;
                        
                        var localX = worldX - context.ChunkX * context.ChunkSize;
                        var localZ = worldZ - context.ChunkZ * context.ChunkSize;
                        
                        if (localX >= 0 && localX < context.ChunkSize && 
                            worldY >= 0 && worldY < context.MaxHeight && 
                            localZ >= 0 && localZ < context.ChunkSize)
                        {
                            var block = template.Blocks[sx, sy, sz];
                            if (block != null && block.BlockId != 0)
                            {
                                context.SetBlockType(localX, worldY, localZ, block.BlockId);
                                context.SetBlockMetadata(localX, worldY, localZ, block.Metadata);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets a block ID by name
        /// </summary>
        private int GetBlockIdByName(string blockName)
        {
            // Simple mapping of block names to IDs
            var blockMap = new Dictionary<string, int>
            {
                ["air"] = 0,
                ["stone"] = 1,
                ["grass"] = 2,
                ["dirt"] = 3,
                ["cobblestone"] = 4,
                ["wood_planks"] = 5,
                ["sapling"] = 6,
                ["bedrock"] = 7,
                ["water"] = 8,
                ["lava"] = 10,
                ["sand"] = 12,
                ["gravel"] = 13,
                ["gold_ore"] = 14,
                ["iron_ore"] = 15,
                ["coal_ore"] = 16,
                ["wood"] = 17,
                ["leaves"] = 18,
                ["glass"] = 20,
                ["lapis_ore"] = 21,
                ["lapis_block"] = 22,
                ["sandstone"] = 24,
                ["note_block"] = 25,
                ["bed"] = 26,
                ["powered_rail"] = 27,
                ["detector_rail"] = 28,
                ["sticky_piston"] = 29,
                ["web"] = 30,
                ["tall_grass"] = 31,
                ["dead_bush"] = 32,
                ["piston"] = 33,
                ["piston_head"] = 34,
                ["wool"] = 35,
                ["flower"] = 37,
                ["flower_pot"] = 38,
                ["mushroom"] = 39,
                ["gold_block"] = 41,
                ["iron_block"] = 42,
                ["stone_slab"] = 44,
                ["brick_block"] = 45,
                ["tnt"] = 46,
                ["bookshelf"] = 47,
                ["mossy_cobblestone"] = 48,
                ["obsidian"] = 49,
                ["torch"] = 50,
                ["fire"] = 51,
                ["mob_spawner"] = 52,
                ["wood_stairs"] = 53,
                ["chest"] = 54,
                ["redstone_wire"] = 55,
                ["diamond_ore"] = 56,
                ["diamond_block"] = 57,
                ["crafting_table"] = 58,
                ["wheat"] = 59,
                ["farmland"] = 60,
                ["furnace"] = 61,
                ["sign"] = 63,
                ["wooden_door"] = 64,
                ["ladder"] = 65,
                ["rail"] = 66,
                ["stone_stairs"] = 67,
                ["lever"] = 69,
                ["stone_pressure_plate"] = 70,
                ["iron_door"] = 71,
                ["wooden_pressure_plate"] = 72,
                ["redstone_ore"] = 73,
                ["redstone_torch"] = 75,
                ["stone_button"] = 77,
                ["snow"] = 78,
                ["ice"] = 79,
                ["snow_block"] = 80,
                ["cactus"] = 81,
                ["clay"] = 82,
                ["sugar_cane"] = 83,
                ["jukebox"] = 84,
                ["fence"] = 85,
                ["pumpkin"] = 86,
                ["netherrack"] = 87,
                ["soul_sand"] = 88,
                ["glowstone"] = 89,
                ["jack_o_lantern"] = 91,
                ["cake"] = 92,
                ["redstone_repeater"] = 93,
                ["stained_glass"] = 95,
                ["trapdoor"] = 96,
                ["monster_egg"] = 97,
                ["stone_bricks"] = 98,
                ["brown_mushroom_block"] = 99,
                ["red_mushroom_block"] = 100,
                ["iron_bars"] = 101,
                ["glass_pane"] = 102,
                ["melon"] = 103,
                ["pumpkin_stem"] = 104,
                ["melon_stem"] = 105,
                ["vine"] = 106,
                ["fence_gate"] = 107,
                ["brick_stairs"] = 108,
                ["stone_brick_stairs"] = 109,
                ["mycelium"] = 110,
                ["lily_pad"] = 111,
                ["nether_brick"] = 112,
                ["nether_brick_fence"] = 113,
                ["nether_brick_stairs"] = 114,
                ["nether_wart"] = 115,
                ["enchanting_table"] = 116,
                ["brewing_stand"] = 117,
                ["cauldron"] = 118,
                ["end_portal"] = 119,
                ["end_portal_frame"] = 120,
                ["end_stone"] = 121,
                ["dragon_egg"] = 122,
                ["redstone_lamp"] = 123,
                ["cocoa"] = 127,
                ["sandstone_stairs"] = 128,
                ["emerald_ore"] = 129,
                ["ender_chest"] = 130,
                ["tripwire_hook"] = 131,
                ["tripwire"] = 132,
                ["emerald_block"] = 133,
                ["spruce_stairs"] = 134,
                ["birch_stairs"] = 135,
                ["jungle_stairs"] = 136,
                ["command_block"] = 137,
                ["beacon"] = 138,
                ["cobblestone_wall"] = 139,
                ["flower_pot"] = 140,
                ["carrots"] = 141,
                ["potatoes"] = 142,
                ["wooden_button"] = 143,
                ["skull"] = 144,
                ["anvil"] = 145,
                ["trapped_chest"] = 146,
                ["light_weighted_pressure_plate"] = 147,
                ["heavy_weighted_pressure_plate"] = 148,
                ["comparator"] = 149,
                ["daylight_detector"] = 151,
                ["redstone_block"] = 152,
                ["nether_quartz_ore"] = 153,
                ["hopper"] = 154,
                ["quartz_block"] = 155,
                ["quartz_stairs"] = 156,
                ["activator_rail"] = 157,
                ["dropper"] = 158,
                ["stained_hardened_clay"] = 159,
                ["stained_glass_pane"] = 160,
                ["leaves2"] = 161,
                ["log2"] = 162,
                ["acacia_stairs"] = 163,
                ["dark_oak_stairs"] = 164,
                ["slime"] = 165,
                ["barrier"] = 166,
                ["iron_trapdoor"] = 167,
                ["prismarine"] = 168,
                ["sea_lantern"] = 169,
                ["hay_block"] = 170,
                ["carpet"] = 171,
                ["hardened_clay"] = 172,
                ["coal_block"] = 173,
                ["packed_ice"] = 174,
                ["double_plant"] = 175,
                ["standing_banner"] = 176,
                ["wall_banner"] = 177,
                ["daylight_detector_inverted"] = 178,
                ["red_sandstone"] = 179,
                ["red_sandstone_stairs"] = 180,
                ["double_stone_slab"] = 181,
                ["double_wooden_slab"] = 182,
                ["spruce_fence_gate"] = 183,
                ["birch_fence_gate"] = 184,
                ["jungle_fence_gate"] = 185,
                ["dark_oak_fence_gate"] = 186,
                ["acacia_fence_gate"] = 187,
                ["spruce_fence"] = 188,
                ["birch_fence"] = 189,
                ["jungle_fence"] = 190,
                ["dark_oak_fence"] = 191,
                ["acacia_fence"] = 192,
                ["spruce_door"] = 193,
                ["birch_door"] = 194,
                ["jungle_door"] = 195,
                ["acacia_door"] = 196,
                ["dark_oak_door"] = 197
            };
            
            return blockMap.TryGetValue(blockName.ToLower(), out var blockId) ? blockId : 1; // Default to stone
        }
        
        /// <summary>
        /// Creates a default world generation configuration
        /// </summary>
        private WorldGenerationConfig CreateDefaultConfig()
        {
            return new WorldGenerationConfig
            {
                BiomeConfig = BiomeConfigFactory.CreateDefault(),
                OreDistributionConfig = OreDistributionConfigFactory.CreateDefault(),
                StructureGenerationConfig = StructureGenerationConfigFactory.CreateDefault(),
                EntitySpawnConfig = EntitySpawnConfigFactory.CreateDefault(),
                World = new WorldConfig
                {
                    Height = 256,
                    SeaLevel = 64,
                    ClimateZones = 8
                }
            };
        }
        
        /// <summary>
        /// Loads configuration from a JSON file
        /// </summary>
        public static WorldGenerationConfig LoadConfig(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<WorldGenerationConfig>(json);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldGenerationManager] Error loading config from {filePath}: {ex.Message}");
            }
            
            // Return default config if loading fails
            var manager = new WorldGenerationManager();
            return manager.Config;
        }
        
        /// <summary>
        /// Saves configuration to a JSON file
        /// </summary>
        public static void SaveConfig(WorldGenerationConfig config, string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, json);
                Console.WriteLine($"[WorldGenerationManager] Saved config to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldGenerationManager] Error saving config to {filePath}: {ex.Message}");
            }
        }
    }
}
}
    {
        public bool Enabled { get; set; } = true;
        public float Frequency { get; set; } = 0.01f;
        public int Width { get; set; } = 3;
        public float Depth { get; set; } = 0.3f;
        public float MeanderStrength { get; set; } = 0.8f;
    }
    
    /// <summary>
    /// Lake generation configuration
    /// </summary>
    public class LakeConfig
    {
        public bool Enabled { get; set; } = true;
        public float Frequency { get; set; } = 0.008f;
        public int MinSize { get; set; } = 8;
        public int MaxSize { get; set; } = 24;
        public float Depth { get; set; } = 0.4f;
        public float ShoreSmoothness { get; set; } = 0.6f;
    }
    
    /// <summary>
    /// Layer execution configuration
    /// </summary>
    public class LayerConfig
    {
        public List<LayerExecutionConfig> ExecutionOrder { get; set; } = new();
    }
    
    /// <summary>
    /// Configuration for individual layer execution
    /// </summary>
    public class LayerExecutionConfig
    {
        public string LayerId { get; set; }
        public bool Enabled { get; set; } = true;
        public int Priority { get; set; }
    }
}
