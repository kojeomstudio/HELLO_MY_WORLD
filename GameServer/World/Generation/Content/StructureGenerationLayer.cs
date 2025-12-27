using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for structure generation (dungeons, villages, etc.)
    /// </summary>
    public class StructureGenerationLayer : IContentLayer
    {
        private readonly StructureGenerationConfig _config;
        private readonly Dictionary<string, StructureType> _structureTypes;
        private readonly FastNoise _structureNoise;
        
        public string LayerId => "StructureGeneration";
        public int Priority => 30; // After terrain, caves, and ore distribution
        public bool IsEnabled { get; set; } = true;
        
        public StructureGenerationLayer(StructureGenerationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _structureTypes = new Dictionary<string, StructureType>();
            _structureNoise = new FastNoise();
            
            // Initialize structure types from configuration
            foreach (var structureConfig in _config.StructureTypes)
            {
                _structureTypes[structureConfig.Name] = structureConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(StructureGenerationConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            // Initialize structure data
            context.StructureData = new StructureData[chunkSize, chunkSize];
            
            // Generate structures for this chunk
            GenerateStructures(context);
            
            Console.WriteLine($"[StructureGenerationLayer] Generated structures for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private void GenerateStructures(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var worldSeed = context.Seed;
            
            // Check for structure generation at chunk boundaries
            // This ensures structures are not cut off at chunk edges
            for (int x = -1; x <= chunkSize; x++)
            {
                for (int z = -1; z <= chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Only process structures that would affect this chunk
                    if (x >= 0 && x < chunkSize && z >= 0 && z < chunkSize)
                    {
                        // Check for structures at this position
                        CheckForStructureAtPosition(context, worldX, worldZ, x, z);
                    }
                }
            }
            
            // Generate large structures that span multiple chunks
            GenerateLargeStructures(context);
        }
        
        private void CheckForStructureAtPosition(TerrainGenerationContext context, int worldX, int worldZ, int localX, int localZ)
        {
            var biome = context.GetBiome(localX, localZ);
            var height = context.GetHeight(localX, localZ);
            
            // Check each structure type for generation at this position
            foreach (var structureType in _structureTypes.Values)
            {
                if (CanGenerateStructure(structureType, worldX, worldZ, biome, height))
                {
                    var structure = GenerateStructure(structureType, worldX, worldZ, context);
                    if (structure != null)
                    {
                        PlaceStructure(context, structure, localX, localZ);
                    }
                }
            }
        }
        
        private bool CanGenerateStructure(StructureType structureType, int worldX, int worldZ, BiomeType biome, int height)
        {
            // Check biome restrictions
            if (structureType.BiomeRestrictions.Count > 0 && !structureType.BiomeRestrictions.Contains(biome))
            {
                return false;
            }
            
            // Check height restrictions
            if (height < structureType.MinHeight || height > structureType.MaxHeight)
            {
                return false;
            }
            
            // Check spacing requirements
            if (!CheckStructureSpacing(structureType, worldX, worldZ))
            {
                return false;
            }
            
            // Generate noise value for this position
            var noiseValue = _structureNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Check if structure should generate based on rarity
            return normalizedNoise < (1.0f / structureType.Rarity);
        }
        
        private bool CheckStructureSpacing(StructureType structureType, int worldX, int worldZ)
        {
            // Simple spacing check based on structure rarity and minimum spacing
            // In a real implementation, this would check against existing structures
            var spacing = structureType.MinSpacing;
            var gridX = worldX / spacing;
            var gridZ = worldZ / spacing;
            
            // Use a hash function to ensure consistent spacing
            var hash = (gridX * 73856093) ^ (gridZ * 19349663);
            var normalizedHash = (hash % 1000000) / 1000000.0f;
            
            return normalizedHash < (1.0f / structureType.Rarity);
        }
        
        private Structure GenerateStructure(StructureType structureType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var structure = new Structure();
            structure.Type = structureType.Name;
            structure.Position = new Position3D(worldX, context.GetHeight(worldX % context.ChunkSize, worldZ % context.ChunkSize), worldZ);
            structure.Rotation = (StructureRotation)(context.Random.Next(4));
            structure.Variant = SelectStructureVariant(structureType, context);
            
            // Generate structure template
            structure.Template = GenerateStructureTemplate(structureType, structure.Variant, context);
            
            return structure;
        }
        
        private string SelectStructureVariant(StructureType structureType, TerrainGenerationContext context)
        {
            if (structureType.Variants.Count == 0)
            {
                return "default";
            }
            
            var totalWeight = structureType.Variants.Sum(v => v.Weight);
            var randomValue = (float)(context.Random.NextDouble() * totalWeight);
            
            var currentWeight = 0f;
            foreach (var variant in structureType.Variants)
            {
                currentWeight += variant.Weight;
                if (randomValue <= currentWeight)
                {
                    return variant.Name;
                }
            }
            
            return structureType.Variants.Last().Name;
        }
        
        private StructureTemplate GenerateStructureTemplate(StructureType structureType, string variantName, TerrainGenerationContext context)
        {
            // In a real implementation, this would load structure templates from files
            // For now, we'll create simple procedural templates
            
            var template = new StructureTemplate();
            template.Name = $"{structureType.Name}_{variantName}";
            template.Size = new Position3D(structureType.Width, structureType.Height, structureType.Depth);
            
            // Generate structure blocks based on type
            switch (structureType.Name.ToLower())
            {
                case "dungeon":
                    GenerateDungeonTemplate(template, context);
                    break;
                case "village_house":
                    GenerateVillageHouseTemplate(template, context);
                    break;
                case "mineshaft":
                    GenerateMineshaftTemplate(template, context);
                    break;
                case "temple":
                    GenerateTempleTemplate(template, context);
                    break;
                default:
                    GenerateDefaultTemplate(template, context);
                    break;
            }
            
            return template;
        }
        
        private void GenerateDungeonTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple 5x5x5 dungeon with spawner and chests
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add stone walls
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 1 }; // Stone floor
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 1 }; // Stone ceiling
                }
            }
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 1 }; // Stone wall
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 1 }; // Stone wall
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 1 }; // Stone wall
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 1 }; // Stone wall
                }
            }
            
            // Add spawner in center
            var centerX = width / 2;
            var centerZ = depth / 2;
            template.Blocks[centerX, 1, centerZ] = new StructureBlock { BlockId = 52 }; // Monster spawner
            
            // Add chests
            template.Blocks[1, 1, 1] = new StructureBlock { BlockId = 54 }; // Chest
            template.Blocks[width - 2, 1, depth - 2] = new StructureBlock { BlockId = 54 }; // Chest
        }
        
        private void GenerateVillageHouseTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple wooden house
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add wooden floor
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
            }
            
            // Add walls
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
            }
            
            // Add door
            var doorX = width / 2;
            template.Blocks[doorX, 1, 0] = new StructureBlock { BlockId = 64 }; // Wooden door
            template.Blocks[doorX, 2, 0] = new StructureBlock { BlockId = 64 }; // Wooden door
            
            // Add roof
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 53 }; // Wood stairs
                }
            }
        }
        
        private void GenerateMineshaftTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple mineshaft corridor
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air (the tunnel)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add wooden supports
            for (int y = 0; y < height; y++)
            {
                template.Blocks[0, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[width - 1, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[0, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[width - 1, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
            }
            
            // Add occasional rails
            if (context.Random.NextDouble() < 0.3f)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[width / 2, 0, z] = new StructureBlock { BlockId = 66 }; // Rails
                }
            }
        }
        
        private void GenerateTempleTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple temple structure
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add stone base
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 1 }; // Stone
                }
            }
            
            // Add stone brick walls
            for (int y = 1; y < height - 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 98 }; // Stone bricks
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                }
            }
            
            // Add roof
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, height - 2, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 44 }; // Stone slabs
                }
            }
            
            // Add altar in center
            var centerX = width / 2;
            var centerZ = depth / 2;
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int z = centerZ - 1; z <= centerZ + 1; z++)
                {
                    if (x >= 0 && x < width && z >= 0 && z < depth)
                    {
                        template.Blocks[x, 1, z] = new StructureBlock { BlockId = 24 }; // Sandstone
                    }
                }
            }
        }
        
        private void GenerateDefaultTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Default simple structure
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with stone
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 1 }; // Stone
                    }
                }
            }
        }
        
        private void PlaceStructure(TerrainGenerationContext context, Structure structure, int localX, int localZ)
        {
            if (localX < 0 || localX >= context.ChunkSize || localZ < 0 || localZ >= context.ChunkSize)
            {
                return;
            }
            
            var structureData = new StructureData();
            structureData.Structure = structure;
            structureData.IsPlaced = true;
            
            context.StructureData[localX, localZ] = structureData;
        }
        
        private void GenerateLargeStructures(TerrainGenerationContext context)
        {
            // Generate structures that span multiple chunks
            // This would be implemented based on specific structure types
            // For now, we'll leave this as a placeholder
        }
    }
    
    /// <summary>
    /// Configuration for structure generation
    /// </summary>
    public class StructureGenerationConfig
    {
        public List<StructureType> StructureTypes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public int MaxStructuresPerChunk { get; set; } = 10;
        public bool EnableStructureVariants { get; set; } = true;
        public bool EnableStructureRotation { get; set; } = true;
        public bool EnableStructureModification { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for a specific structure type
    /// </summary>
    public class StructureType
    {
        public string Name { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinHeight { get; set; } = 0;
        public int MaxHeight { get; set; } = 256;
        public int Width { get; set; } = 5;
        public int Height { get; set; } = 5;
        public int Depth { get; set; } = 5;
        public int MinSpacing { get; set; } = 32;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public List<StructureVariant> Variants { get; set; } = new();
        public List<StructureModification> Modifications { get; set; } = new();
        public bool GenerateUnderground { get; set; } = false;
        public bool GenerateOnSurface { get; set; } = true;
        public bool GenerateInWater { get; set; } = false;
    }
    
    /// <summary>
    /// Structure variant configuration
    /// </summary>
    public class StructureVariant
    {
        public string Name { get; set; }
        public float Weight { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Structure modification configuration
    /// </summary>
    public class StructureModification
    {
        public string Type { get; set; } // "mossy", "cracked", "vines", etc.
        public float Chance { get; set; } = 0.1f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default structure generation configurations
    /// </summary>
    public static class StructureGenerationConfigFactory
    {
        /// <summary>
        /// Creates a default structure generation configuration
        /// </summary>
        public static StructureGenerationConfig CreateDefault()
        {
            var config = new StructureGenerationConfig();
            
            // Add standard structure types
            config.StructureTypes.AddRange(GetStandardStructureTypes());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard structure types
        /// </summary>
        private static List<StructureType> GetStandardStructureTypes()
        {
            return new List<StructureType>
            {
                new StructureType
                {
                    Name = "dungeon",
                    Rarity = 50.0f,
                    MinHeight = 0,
                    MaxHeight = 50,
                    Width = 5,
                    Height = 5,
                    Depth = 5,
                    MinSpacing = 64,
                    GenerateUnderground = true,
                    GenerateOnSurface = false,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "default", Weight = 1.0f },
                        new StructureVariant { Name = "spider", Weight = 0.3f },
                        new StructureVariant { Name = "zombie", Weight = 0.3f },
                        new StructureVariant { Name = "skeleton", Weight = 0.3f }
                    },
                    Modifications = new List<StructureModification>
                    {
                        new StructureModification { Type = "mossy", Chance = 0.2f },
                        new StructureModification { Type = "cracked", Chance = 0.1f }
                    }
                },
                new StructureType
                {
                    Name = "village_house",
                    Rarity = 20.0f,
                    MinHeight = 60,
                    MaxHeight = 100,
                    Width = 7,
                    Height = 6,
                    Depth = 7,
                    MinSpacing = 32,
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Plains, BiomeType.Desert, BiomeType.Savanna },
                    GenerateUnderground = false,
                    GenerateOnSurface = true,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "small", Weight = 0.4f },
                        new StructureVariant { Name = "medium", Weight = 0.4f },
                        new StructureVariant { Name = "large", Weight = 0.2f }
                    }
                },
                new StructureType
                {
                    Name = "mineshaft",
                    Rarity = 15.0f,
                    MinHeight = 0,
                    MaxHeight = 60,
                    Width = 3,
                    Height = 3,
                    Depth = 20,
                    MinSpacing = 80,
                    GenerateUnderground = true,
                    GenerateOnSurface = false,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "corridor", Weight = 1.0f },
                        new StructureVariant { Name = "intersection", Weight = 0.3f },
                        new StructureVariant { Name = "crossing", Weight = 0.2f }
                    }
                },
                new StructureType
                {
                    Name = "temple",
                    Rarity = 100.0f,
                    MinHeight = 60,
                    MaxHeight = 100,
                    Width = 15,
                    Height = 10,
                    Depth = 15,
                    MinSpacing = 128,
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Desert, BiomeType.Jungle },
                    GenerateUnderground = false,
                    GenerateOnSurface = true,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "desert", Weight = 1.0f },
                        new StructureVariant { Name = "jungle", Weight = 1.0f }
                    }
                }
            };
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for structure generation (dungeons, villages, etc.)
    /// </summary>
    public class StructureGenerationLayer : IContentLayer
    {
        private readonly StructureGenerationConfig _config;
        private readonly Dictionary<string, StructureType> _structureTypes;
        private readonly FastNoise _structureNoise;
        
        public string LayerId => "StructureGeneration";
        public int Priority => 30; // After terrain, caves, and ore distribution
        public bool IsEnabled { get; set; } = true;
        
        public StructureGenerationLayer(StructureGenerationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _structureTypes = new Dictionary<string, StructureType>();
            _structureNoise = new FastNoise();
            
            // Initialize structure types from configuration
            foreach (var structureConfig in _config.StructureTypes)
            {
                _structureTypes[structureConfig.Name] = structureConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(StructureGenerationConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            // Initialize structure data
            context.StructureData = new StructureData[chunkSize, chunkSize];
            
            // Generate structures for this chunk
            GenerateStructures(context);
            
            Console.WriteLine($"[StructureGenerationLayer] Generated structures for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private void GenerateStructures(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var worldSeed = context.Seed;
            
            // Check for structure generation at chunk boundaries
            // This ensures structures are not cut off at chunk edges
            for (int x = -1; x <= chunkSize; x++)
            {
                for (int z = -1; z <= chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Only process structures that would affect this chunk
                    if (x >= 0 && x < chunkSize && z >= 0 && z < chunkSize)
                    {
                        // Check for structures at this position
                        CheckForStructureAtPosition(context, worldX, worldZ, x, z);
                    }
                }
            }
            
            // Generate large structures that span multiple chunks
            GenerateLargeStructures(context);
        }
        
        private void CheckForStructureAtPosition(TerrainGenerationContext context, int worldX, int worldZ, int localX, int localZ)
        {
            var biome = context.GetBiome(localX, localZ);
            var height = context.GetHeight(localX, localZ);
            
            // Check each structure type for generation at this position
            foreach (var structureType in _structureTypes.Values)
            {
                if (CanGenerateStructure(structureType, worldX, worldZ, biome, height))
                {
                    var structure = GenerateStructure(structureType, worldX, worldZ, context);
                    if (structure != null)
                    {
                        PlaceStructure(context, structure, localX, localZ);
                    }
                }
            }
        }
        
        private bool CanGenerateStructure(StructureType structureType, int worldX, int worldZ, BiomeType biome, int height)
        {
            // Check biome restrictions
            if (structureType.BiomeRestrictions.Count > 0 && !structureType.BiomeRestrictions.Contains(biome))
            {
                return false;
            }
            
            // Check height restrictions
            if (height < structureType.MinHeight || height > structureType.MaxHeight)
            {
                return false;
            }
            
            // Check spacing requirements
            if (!CheckStructureSpacing(structureType, worldX, worldZ))
            {
                return false;
            }
            
            // Generate noise value for this position
            var noiseValue = _structureNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Check if structure should generate based on rarity
            return normalizedNoise < (1.0f / structureType.Rarity);
        }
        
        private bool CheckStructureSpacing(StructureType structureType, int worldX, int worldZ)
        {
            // Simple spacing check based on structure rarity and minimum spacing
            // In a real implementation, this would check against existing structures
            var spacing = structureType.MinSpacing;
            var gridX = worldX / spacing;
            var gridZ = worldZ / spacing;
            
            // Use a hash function to ensure consistent spacing
            var hash = (gridX * 73856093) ^ (gridZ * 19349663);
            var normalizedHash = (hash % 1000000) / 1000000.0f;
            
            return normalizedHash < (1.0f / structureType.Rarity);
        }
        
        private Structure GenerateStructure(StructureType structureType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var structure = new Structure();
            structure.Type = structureType.Name;
            structure.Position = new Position3D(worldX, context.GetHeight(worldX % context.ChunkSize, worldZ % context.ChunkSize), worldZ);
            structure.Rotation = (StructureRotation)(context.Random.Next(4));
            structure.Variant = SelectStructureVariant(structureType, context);
            
            // Generate structure template
            structure.Template = GenerateStructureTemplate(structureType, structure.Variant, context);
            
            return structure;
        }
        
        private string SelectStructureVariant(StructureType structureType, TerrainGenerationContext context)
        {
            if (structureType.Variants.Count == 0)
            {
                return "default";
            }
            
            var totalWeight = structureType.Variants.Sum(v => v.Weight);
            var randomValue = (float)(context.Random.NextDouble() * totalWeight);
            
            var currentWeight = 0f;
            foreach (var variant in structureType.Variants)
            {
                currentWeight += variant.Weight;
                if (randomValue <= currentWeight)
                {
                    return variant.Name;
                }
            }
            
            return structureType.Variants.Last().Name;
        }
        
        private StructureTemplate GenerateStructureTemplate(StructureType structureType, string variantName, TerrainGenerationContext context)
        {
            // In a real implementation, this would load structure templates from files
            // For now, we'll create simple procedural templates
            
            var template = new StructureTemplate();
            template.Name = $"{structureType.Name}_{variantName}";
            template.Size = new Position3D(structureType.Width, structureType.Height, structureType.Depth);
            
            // Generate structure blocks based on type
            switch (structureType.Name.ToLower())
            {
                case "dungeon":
                    GenerateDungeonTemplate(template, context);
                    break;
                case "village_house":
                    GenerateVillageHouseTemplate(template, context);
                    break;
                case "mineshaft":
                    GenerateMineshaftTemplate(template, context);
                    break;
                case "temple":
                    GenerateTempleTemplate(template, context);
                    break;
                default:
                    GenerateDefaultTemplate(template, context);
                    break;
            }
            
            return template;
        }
        
        private void GenerateDungeonTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple 5x5x5 dungeon with spawner and chests
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add stone walls
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 1 }; // Stone floor
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 1 }; // Stone ceiling
                }
            }
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 1 }; // Stone wall
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 1 }; // Stone wall
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 1 }; // Stone wall
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 1 }; // Stone wall
                }
            }
            
            // Add spawner in center
            var centerX = width / 2;
            var centerZ = depth / 2;
            template.Blocks[centerX, 1, centerZ] = new StructureBlock { BlockId = 52 }; // Monster spawner
            
            // Add chests
            template.Blocks[1, 1, 1] = new StructureBlock { BlockId = 54 }; // Chest
            template.Blocks[width - 2, 1, depth - 2] = new StructureBlock { BlockId = 54 }; // Chest
        }
        
        private void GenerateVillageHouseTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple wooden house
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add wooden floor
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
            }
            
            // Add walls
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
            }
            
            // Add door
            var doorX = width / 2;
            template.Blocks[doorX, 1, 0] = new StructureBlock { BlockId = 64 }; // Wooden door
            template.Blocks[doorX, 2, 0] = new StructureBlock { BlockId = 64 }; // Wooden door
            
            // Add roof
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 53 }; // Wood stairs
                }
            }
        }
        
        private void GenerateMineshaftTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple mineshaft corridor
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air (the tunnel)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add wooden supports
            for (int y = 0; y < height; y++)
            {
                template.Blocks[0, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[width - 1, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[0, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[width - 1, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
            }
            
            // Add occasional rails
            if (context.Random.NextDouble() < 0.3f)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[width / 2, 0, z] = new StructureBlock { BlockId = 66 }; // Rails
                }
            }
        }
        
        private void GenerateTempleTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple temple structure
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add stone base
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 1 }; // Stone
                }
            }
            
            // Add stone brick walls
            for (int y = 1; y < height - 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 98 }; // Stone bricks
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                }
            }
            
            // Add roof
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, height - 2, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 44 }; // Stone slabs
                }
            }
            
            // Add altar in center
            var centerX = width / 2;
            var centerZ = depth / 2;
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int z = centerZ - 1; z <= centerZ + 1; z++)
                {
                    if (x >= 0 && x < width && z >= 0 && z < depth)
                    {
                        template.Blocks[x, 1, z] = new StructureBlock { BlockId = 24 }; // Sandstone
                    }
                }
            }
        }
        
        private void GenerateDefaultTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Default simple structure
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with stone
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 1 }; // Stone
                    }
                }
            }
        }
        
        private void PlaceStructure(TerrainGenerationContext context, Structure structure, int localX, int localZ)
        {
            if (localX < 0 || localX >= context.ChunkSize || localZ < 0 || localZ >= context.ChunkSize)
            {
                return;
            }
            
            var structureData = new StructureData();
            structureData.Structure = structure;
            structureData.IsPlaced = true;
            
            context.StructureData[localX, localZ] = structureData;
        }
        
        private void GenerateLargeStructures(TerrainGenerationContext context)
        {
            // Generate structures that span multiple chunks
            // This would be implemented based on specific structure types
            // For now, we'll leave this as a placeholder
        }
    }
    
    /// <summary>
    /// Configuration for structure generation
    /// </summary>
    public class StructureGenerationConfig
    {
        public List<StructureType> StructureTypes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public int MaxStructuresPerChunk { get; set; } = 10;
        public bool EnableStructureVariants { get; set; } = true;
        public bool EnableStructureRotation { get; set; } = true;
        public bool EnableStructureModification { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for a specific structure type
    /// </summary>
    public class StructureType
    {
        public string Name { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinHeight { get; set; } = 0;
        public int MaxHeight { get; set; } = 256;
        public int Width { get; set; } = 5;
        public int Height { get; set; } = 5;
        public int Depth { get; set; } = 5;
        public int MinSpacing { get; set; } = 32;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public List<StructureVariant> Variants { get; set; } = new();
        public List<StructureModification> Modifications { get; set; } = new();
        public bool GenerateUnderground { get; set; } = false;
        public bool GenerateOnSurface { get; set; } = true;
        public bool GenerateInWater { get; set; } = false;
    }
    
    /// <summary>
    /// Structure variant configuration
    /// </summary>
    public class StructureVariant
    {
        public string Name { get; set; }
        public float Weight { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Structure modification configuration
    /// </summary>
    public class StructureModification
    {
        public string Type { get; set; } // "mossy", "cracked", "vines", etc.
        public float Chance { get; set; } = 0.1f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default structure generation configurations
    /// </summary>
    public static class StructureGenerationConfigFactory
    {
        /// <summary>
        /// Creates a default structure generation configuration
        /// </summary>
        public static StructureGenerationConfig CreateDefault()
        {
            var config = new StructureGenerationConfig();
            
            // Add standard structure types
            config.StructureTypes.AddRange(GetStandardStructureTypes());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard structure types
        /// </summary>
        private static List<StructureType> GetStandardStructureTypes()
        {
            return new List<StructureType>
            {
                new StructureType
                {
                    Name = "dungeon",
                    Rarity = 50.0f,
                    MinHeight = 0,
                    MaxHeight = 50,
                    Width = 5,
                    Height = 5,
                    Depth = 5,
                    MinSpacing = 64,
                    GenerateUnderground = true,
                    GenerateOnSurface = false,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "default", Weight = 1.0f },
                        new StructureVariant { Name = "spider", Weight = 0.3f },
                        new StructureVariant { Name = "zombie", Weight = 0.3f },
                        new StructureVariant { Name = "skeleton", Weight = 0.3f }
                    },
                    Modifications = new List<StructureModification>
                    {
                        new StructureModification { Type = "mossy", Chance = 0.2f },
                        new StructureModification { Type = "cracked", Chance = 0.1f }
                    }
                },
                new StructureType
                {
                    Name = "village_house",
                    Rarity = 20.0f,
                    MinHeight = 60,
                    MaxHeight = 100,
                    Width = 7,
                    Height = 6,
                    Depth = 7,
                    MinSpacing = 32,
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Plains, BiomeType.Desert, BiomeType.Savanna },
                    GenerateUnderground = false,
                    GenerateOnSurface = true,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "small", Weight = 0.4f },
                        new StructureVariant { Name = "medium", Weight = 0.4f },
                        new StructureVariant { Name = "large", Weight = 0.2f }
                    }
                },
                new StructureType
                {
                    Name = "mineshaft",
                    Rarity = 15.0f,
                    MinHeight = 0,
                    MaxHeight = 60,
                    Width = 3,
                    Height = 3,
                    Depth = 20,
                    MinSpacing = 80,
                    GenerateUnderground = true,
                    GenerateOnSurface = false,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "corridor", Weight = 1.0f },
                        new StructureVariant { Name = "intersection", Weight = 0.3f },
                        new StructureVariant { Name = "crossing", Weight = 0.2f }
                    }
                },
                new StructureType
                {
                    Name = "temple",
                    Rarity = 100.0f,
                    MinHeight = 60,
                    MaxHeight = 100,
                    Width = 15,
                    Height = 10,
                    Depth = 15,
                    MinSpacing = 128,
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Desert, BiomeType.Jungle },
                    GenerateUnderground = false,
                    GenerateOnSurface = true,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "desert", Weight = 1.0f },
                        new StructureVariant { Name = "jungle", Weight = 1.0f }
                    }
                }
            };
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for structure generation (dungeons, villages, etc.)
    /// </summary>
    public class StructureGenerationLayer : IContentLayer
    {
        private readonly StructureGenerationConfig _config;
        private readonly Dictionary<string, StructureType> _structureTypes;
        private readonly FastNoise _structureNoise;
        
        public string LayerId => "StructureGeneration";
        public int Priority => 30; // After terrain, caves, and ore distribution
        public bool IsEnabled { get; set; } = true;
        
        public StructureGenerationLayer(StructureGenerationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _structureTypes = new Dictionary<string, StructureType>();
            _structureNoise = new FastNoise();
            
            // Initialize structure types from configuration
            foreach (var structureConfig in _config.StructureTypes)
            {
                _structureTypes[structureConfig.Name] = structureConfig;
            }
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(StructureGenerationConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            
            // Initialize structure data
            context.StructureData = new StructureData[chunkSize, chunkSize];
            
            // Generate structures for this chunk
            GenerateStructures(context);
            
            Console.WriteLine($"[StructureGenerationLayer] Generated structures for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private void GenerateStructures(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var worldSeed = context.Seed;
            
            // Check for structure generation at chunk boundaries
            // This ensures structures are not cut off at chunk edges
            for (int x = -1; x <= chunkSize; x++)
            {
                for (int z = -1; z <= chunkSize; z++)
                {
                    var worldX = context.ChunkX * chunkSize + x;
                    var worldZ = context.ChunkZ * chunkSize + z;
                    
                    // Only process structures that would affect this chunk
                    if (x >= 0 && x < chunkSize && z >= 0 && z < chunkSize)
                    {
                        // Check for structures at this position
                        CheckForStructureAtPosition(context, worldX, worldZ, x, z);
                    }
                }
            }
            
            // Generate large structures that span multiple chunks
            GenerateLargeStructures(context);
        }
        
        private void CheckForStructureAtPosition(TerrainGenerationContext context, int worldX, int worldZ, int localX, int localZ)
        {
            var biome = context.GetBiome(localX, localZ);
            var height = context.GetHeight(localX, localZ);
            
            // Check each structure type for generation at this position
            foreach (var structureType in _structureTypes.Values)
            {
                if (CanGenerateStructure(structureType, worldX, worldZ, biome, height))
                {
                    var structure = GenerateStructure(structureType, worldX, worldZ, context);
                    if (structure != null)
                    {
                        PlaceStructure(context, structure, localX, localZ);
                    }
                }
            }
        }
        
        private bool CanGenerateStructure(StructureType structureType, int worldX, int worldZ, BiomeType biome, int height)
        {
            // Check biome restrictions
            if (structureType.BiomeRestrictions.Count > 0 && !structureType.BiomeRestrictions.Contains(biome))
            {
                return false;
            }
            
            // Check height restrictions
            if (height < structureType.MinHeight || height > structureType.MaxHeight)
            {
                return false;
            }
            
            // Check spacing requirements
            if (!CheckStructureSpacing(structureType, worldX, worldZ))
            {
                return false;
            }
            
            // Generate noise value for this position
            var noiseValue = _structureNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            // Check if structure should generate based on rarity
            return normalizedNoise < (1.0f / structureType.Rarity);
        }
        
        private bool CheckStructureSpacing(StructureType structureType, int worldX, int worldZ)
        {
            // Simple spacing check based on structure rarity and minimum spacing
            // In a real implementation, this would check against existing structures
            var spacing = structureType.MinSpacing;
            var gridX = worldX / spacing;
            var gridZ = worldZ / spacing;
            
            // Use a hash function to ensure consistent spacing
            var hash = (gridX * 73856093) ^ (gridZ * 19349663);
            var normalizedHash = (hash % 1000000) / 1000000.0f;
            
            return normalizedHash < (1.0f / structureType.Rarity);
        }
        
        private Structure GenerateStructure(StructureType structureType, int worldX, int worldZ, TerrainGenerationContext context)
        {
            var structure = new Structure();
            structure.Type = structureType.Name;
            structure.Position = new Position3D(worldX, context.GetHeight(worldX % context.ChunkSize, worldZ % context.ChunkSize), worldZ);
            structure.Rotation = (StructureRotation)(context.Random.Next(4));
            structure.Variant = SelectStructureVariant(structureType, context);
            
            // Generate structure template
            structure.Template = GenerateStructureTemplate(structureType, structure.Variant, context);
            
            return structure;
        }
        
        private string SelectStructureVariant(StructureType structureType, TerrainGenerationContext context)
        {
            if (structureType.Variants.Count == 0)
            {
                return "default";
            }
            
            var totalWeight = structureType.Variants.Sum(v => v.Weight);
            var randomValue = (float)(context.Random.NextDouble() * totalWeight);
            
            var currentWeight = 0f;
            foreach (var variant in structureType.Variants)
            {
                currentWeight += variant.Weight;
                if (randomValue <= currentWeight)
                {
                    return variant.Name;
                }
            }
            
            return structureType.Variants.Last().Name;
        }
        
        private StructureTemplate GenerateStructureTemplate(StructureType structureType, string variantName, TerrainGenerationContext context)
        {
            // In a real implementation, this would load structure templates from files
            // For now, we'll create simple procedural templates
            
            var template = new StructureTemplate();
            template.Name = $"{structureType.Name}_{variantName}";
            template.Size = new Position3D(structureType.Width, structureType.Height, structureType.Depth);
            
            // Generate structure blocks based on type
            switch (structureType.Name.ToLower())
            {
                case "dungeon":
                    GenerateDungeonTemplate(template, context);
                    break;
                case "village_house":
                    GenerateVillageHouseTemplate(template, context);
                    break;
                case "mineshaft":
                    GenerateMineshaftTemplate(template, context);
                    break;
                case "temple":
                    GenerateTempleTemplate(template, context);
                    break;
                default:
                    GenerateDefaultTemplate(template, context);
                    break;
            }
            
            return template;
        }
        
        private void GenerateDungeonTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple 5x5x5 dungeon with spawner and chests
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add stone walls
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 1 }; // Stone floor
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 1 }; // Stone ceiling
                }
            }
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 1 }; // Stone wall
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 1 }; // Stone wall
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 1 }; // Stone wall
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 1 }; // Stone wall
                }
            }
            
            // Add spawner in center
            var centerX = width / 2;
            var centerZ = depth / 2;
            template.Blocks[centerX, 1, centerZ] = new StructureBlock { BlockId = 52 }; // Monster spawner
            
            // Add chests
            template.Blocks[1, 1, 1] = new StructureBlock { BlockId = 54 }; // Chest
            template.Blocks[width - 2, 1, depth - 2] = new StructureBlock { BlockId = 54 }; // Chest
        }
        
        private void GenerateVillageHouseTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple wooden house
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add wooden floor
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
            }
            
            // Add walls
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 5 }; // Wood planks
                }
            }
            
            // Add door
            var doorX = width / 2;
            template.Blocks[doorX, 1, 0] = new StructureBlock { BlockId = 64 }; // Wooden door
            template.Blocks[doorX, 2, 0] = new StructureBlock { BlockId = 64 }; // Wooden door
            
            // Add roof
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 53 }; // Wood stairs
                }
            }
        }
        
        private void GenerateMineshaftTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple mineshaft corridor
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air (the tunnel)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add wooden supports
            for (int y = 0; y < height; y++)
            {
                template.Blocks[0, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[width - 1, y, 0] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[0, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
                template.Blocks[width - 1, y, depth - 1] = new StructureBlock { BlockId = 5 }; // Wood planks
            }
            
            // Add occasional rails
            if (context.Random.NextDouble() < 0.3f)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[width / 2, 0, z] = new StructureBlock { BlockId = 66 }; // Rails
                }
            }
        }
        
        private void GenerateTempleTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Simple temple structure
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with air
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 0 }; // Air
                    }
                }
            }
            
            // Add stone base
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, 0, z] = new StructureBlock { BlockId = 1 }; // Stone
                }
            }
            
            // Add stone brick walls
            for (int y = 1; y < height - 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    template.Blocks[x, y, 0] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[x, y, depth - 1] = new StructureBlock { BlockId = 98 }; // Stone bricks
                }
                
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[0, y, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[width - 1, y, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                }
            }
            
            // Add roof
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    template.Blocks[x, height - 2, z] = new StructureBlock { BlockId = 98 }; // Stone bricks
                    template.Blocks[x, height - 1, z] = new StructureBlock { BlockId = 44 }; // Stone slabs
                }
            }
            
            // Add altar in center
            var centerX = width / 2;
            var centerZ = depth / 2;
            for (int x = centerX - 1; x <= centerX + 1; x++)
            {
                for (int z = centerZ - 1; z <= centerZ + 1; z++)
                {
                    if (x >= 0 && x < width && z >= 0 && z < depth)
                    {
                        template.Blocks[x, 1, z] = new StructureBlock { BlockId = 24 }; // Sandstone
                    }
                }
            }
        }
        
        private void GenerateDefaultTemplate(StructureTemplate template, TerrainGenerationContext context)
        {
            // Default simple structure
            var width = (int)template.Size.X;
            var height = (int)template.Size.Y;
            var depth = (int)template.Size.Z;
            
            template.Blocks = new StructureBlock[width, height, depth];
            
            // Fill with stone
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        template.Blocks[x, y, z] = new StructureBlock { BlockId = 1 }; // Stone
                    }
                }
            }
        }
        
        private void PlaceStructure(TerrainGenerationContext context, Structure structure, int localX, int localZ)
        {
            if (localX < 0 || localX >= context.ChunkSize || localZ < 0 || localZ >= context.ChunkSize)
            {
                return;
            }
            
            var structureData = new StructureData();
            structureData.Structure = structure;
            structureData.IsPlaced = true;
            
            context.StructureData[localX, localZ] = structureData;
        }
        
        private void GenerateLargeStructures(TerrainGenerationContext context)
        {
            // Generate structures that span multiple chunks
            // This would be implemented based on specific structure types
            // For now, we'll leave this as a placeholder
        }
    }
    
    /// <summary>
    /// Configuration for structure generation
    /// </summary>
    public class StructureGenerationConfig
    {
        public List<StructureType> StructureTypes { get; set; } = new();
        public float GlobalRarityModifier { get; set; } = 1.0f;
        public int MaxStructuresPerChunk { get; set; } = 10;
        public bool EnableStructureVariants { get; set; } = true;
        public bool EnableStructureRotation { get; set; } = true;
        public bool EnableStructureModification { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for a specific structure type
    /// </summary>
    public class StructureType
    {
        public string Name { get; set; }
        public float Rarity { get; set; } = 1.0f; // Higher = rarer
        public int MinHeight { get; set; } = 0;
        public int MaxHeight { get; set; } = 256;
        public int Width { get; set; } = 5;
        public int Height { get; set; } = 5;
        public int Depth { get; set; } = 5;
        public int MinSpacing { get; set; } = 32;
        public List<BiomeType> BiomeRestrictions { get; set; } = new();
        public List<StructureVariant> Variants { get; set; } = new();
        public List<StructureModification> Modifications { get; set; } = new();
        public bool GenerateUnderground { get; set; } = false;
        public bool GenerateOnSurface { get; set; } = true;
        public bool GenerateInWater { get; set; } = false;
    }
    
    /// <summary>
    /// Structure variant configuration
    /// </summary>
    public class StructureVariant
    {
        public string Name { get; set; }
        public float Weight { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Structure modification configuration
    /// </summary>
    public class StructureModification
    {
        public string Type { get; set; } // "mossy", "cracked", "vines", etc.
        public float Chance { get; set; } = 0.1f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Factory for creating default structure generation configurations
    /// </summary>
    public static class StructureGenerationConfigFactory
    {
        /// <summary>
        /// Creates a default structure generation configuration
        /// </summary>
        public static StructureGenerationConfig CreateDefault()
        {
            var config = new StructureGenerationConfig();
            
            // Add standard structure types
            config.StructureTypes.AddRange(GetStandardStructureTypes());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard structure types
        /// </summary>
        private static List<StructureType> GetStandardStructureTypes()
        {
            return new List<StructureType>
            {
                new StructureType
                {
                    Name = "dungeon",
                    Rarity = 50.0f,
                    MinHeight = 0,
                    MaxHeight = 50,
                    Width = 5,
                    Height = 5,
                    Depth = 5,
                    MinSpacing = 64,
                    GenerateUnderground = true,
                    GenerateOnSurface = false,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "default", Weight = 1.0f },
                        new StructureVariant { Name = "spider", Weight = 0.3f },
                        new StructureVariant { Name = "zombie", Weight = 0.3f },
                        new StructureVariant { Name = "skeleton", Weight = 0.3f }
                    },
                    Modifications = new List<StructureModification>
                    {
                        new StructureModification { Type = "mossy", Chance = 0.2f },
                        new StructureModification { Type = "cracked", Chance = 0.1f }
                    }
                },
                new StructureType
                {
                    Name = "village_house",
                    Rarity = 20.0f,
                    MinHeight = 60,
                    MaxHeight = 100,
                    Width = 7,
                    Height = 6,
                    Depth = 7,
                    MinSpacing = 32,
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Plains, BiomeType.Desert, BiomeType.Savanna },
                    GenerateUnderground = false,
                    GenerateOnSurface = true,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "small", Weight = 0.4f },
                        new StructureVariant { Name = "medium", Weight = 0.4f },
                        new StructureVariant { Name = "large", Weight = 0.2f }
                    }
                },
                new StructureType
                {
                    Name = "mineshaft",
                    Rarity = 15.0f,
                    MinHeight = 0,
                    MaxHeight = 60,
                    Width = 3,
                    Height = 3,
                    Depth = 20,
                    MinSpacing = 80,
                    GenerateUnderground = true,
                    GenerateOnSurface = false,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "corridor", Weight = 1.0f },
                        new StructureVariant { Name = "intersection", Weight = 0.3f },
                        new StructureVariant { Name = "crossing", Weight = 0.2f }
                    }
                },
                new StructureType
                {
                    Name = "temple",
                    Rarity = 100.0f,
                    MinHeight = 60,
                    MaxHeight = 100,
                    Width = 15,
                    Height = 10,
                    Depth = 15,
                    MinSpacing = 128,
                    BiomeRestrictions = new List<BiomeType> { BiomeType.Desert, BiomeType.Jungle },
                    GenerateUnderground = false,
                    GenerateOnSurface = true,
                    Variants = new List<StructureVariant>
                    {
                        new StructureVariant { Name = "desert", Weight = 1.0f },
                        new StructureVariant { Name = "jungle", Weight = 1.0f }
                    }
                }
            };
        }
    }
}
}
            structure.Properties["components"] = components;
            
            return structure;
        }
        
        private (int x, int z) FindOptimalPosition(StructureType structureType, StructureTemplate template, TerrainGenerationContext context)
        {
            var bestX = 8;
            var bestZ = 8;
            var bestScore = float.MinValue;
            
            // Search for best position within the chunk
            var searchRadius = Math.Min(6, context.ChunkSize / 2 - 1);
            
            for (int x = searchRadius; x < context.ChunkSize - searchRadius; x++)
            {
                for (int z = searchRadius; z < context.ChunkSize - searchRadius; z++)
                {
                    var score = EvaluatePosition(x, z, template, context);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }
            
            return (bestX, bestZ);
        }
        
        private float EvaluatePosition(int x, int z, StructureTemplate template, TerrainGenerationContext context)
        {
            var score = 0f;
            
            // Height score (prefer moderate heights)
            var height = context.GetHeight(x, z);
            var heightScore = 1.0f - Math.Abs(height - 64) / 64.0f;
            score += heightScore * 0.3f;
            
            // Flatness score
            if (template.RequiresFlatTerrain)
            {
                var flatnessScore = IsTerrainFlat(context, x, z, template.RequiredFlatnessRadius) ? 1.0f : 0.0f;
                score += flatnessScore * 0.5f;
            }
            
            // Biome score
            var biome = context.GetBiome(x, z);
            var biomeScore = template.PreferredBiomes.Contains(biome) ? 1.0f : 0.5f;
            score += biomeScore * 0.2f;
            
            return score;
        }
        
        private string SelectStructureVariant(StructureTemplate template, TerrainGenerationContext context)
        {
            if (template.Variants.Count == 0)
                return "default";
            
            var random = context.Random.NextDouble();
            var cumulativeChance = 0.0;
            
            foreach (var variant in template.Variants)
            {
                cumulativeChance += variant.Chance;
                if (random < cumulativeChance)
                    return variant.Name;
            }
            
            return template.Variants.Last().Name;
        }
        
        private List<StructureComponent> GenerateStructureComponents(StructureTemplate template, StructureData structure, TerrainGenerationContext context)
        {
            var components = new List<StructureComponent>();
            var rotation = (int)structure.Properties["rotation"];
            var variant = (string)structure.Properties["variant"];
            
            // Generate main structure components
            foreach (var componentTemplate in template.Components)
            {
                var component = new StructureComponent
                {
                    Type = componentTemplate.Type,
                    RelativeX = componentTemplate.RelativeX,
                    RelativeY = componentTemplate.RelativeY,
                    RelativeZ = componentTemplate.RelativeZ,
                    BlockType = componentTemplate.BlockType,
                    Properties = new Dictionary<string, object>(componentTemplate.Properties)
                };
                
                // Apply rotation to relative position
                var rotatedPos = RotatePosition(component.RelativeX, component.RelativeZ, rotation);
                component.RelativeX = rotatedPos.x;
                component.RelativeZ = rotatedPos.z;
                
                // Apply variant-specific modifications
                ApplyVariantModifications(component, variant, template);
                
                components.Add(component);
            }
            
            return components;
        }
        
        private (int x, int z) RotatePosition(int x, int z, int rotation)
        {
            return rotation switch
            {
                90 => (-z, x),
                180 => (-x, -z),
                270 => (z, -x),
                _ => (x, z)
            };
        }
        
        private void ApplyVariantModifications(StructureComponent component, string variant, StructureTemplate template)
        {
            var variantTemplate = template.Variants.FirstOrDefault(v => v.Name == variant);
            if (variantTemplate == null)
                return;
            
            // Apply variant-specific modifications
            foreach (var modification in variantTemplate.Modifications)
            {
                if (modification.ComponentType == component.Type)
                {
                    foreach (var prop in modification.Properties)
                    {
                        component.Properties[prop.Key] = prop.Value;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Configuration for structure generation
    /// </summary>
    public class StructureGenerationConfig
    {
        public List<StructureType> StructureTypes { get; set; } = new();
        public List<StructureTemplate> Templates { get; set; } = new();
        public float PlacementFrequency { get; set; } = 0.01f;
        public int MaxStructuresPerChunk { get; set; } = 3;
        public bool EnableStructureMerging { get; set; } = true;
        public float StructureMergeChance { get; set; } = 0.1f;
    }
    
    /// <summary>
    /// Configuration for a specific structure type
    /// </summary>
    public class StructureType
    {
        public string Name { get; set; }
        public string TemplateName { get; set; }
        public bool Enabled { get; set; } = true;
        public float SpawnChance { get; set; } = 0.1f;
        public int MinChunkSpacing { get; set; } = 8;
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        public int MinHeight { get; set; } = 0;
        public int MaxHeight { get; set; } = 256;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Template for structure generation
    /// </summary>
    public class StructureTemplate
    {
        public string Name { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
        public bool RequiresFlatTerrain { get; set; } = true;
        public int RequiredFlatnessRadius { get; set; } = 4;
        public bool RequiresWater { get; set; } = false;
        public int WaterSearchRadius { get; set; } = 8;
        public int GroundLevelOffset { get; set; } = 0;
        public List<BiomeType> PreferredBiomes { get; set; } = new();
        public List<StructureComponentTemplate> Components { get; set; } = new();
        public List<StructureVariant> Variants { get; set; } = new();
    }
    
    /// <summary>
    /// Template for a structure component
    /// </summary>
    public class StructureComponentTemplate
    {
        public string Type { get; set; }
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }
        public int RelativeZ { get; set; }
        public string BlockType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Structure variant with different configurations
    /// </summary>
    public class StructureVariant
    {
        public string Name { get; set; }
        public float Chance { get; set; } = 1.0f;
        public List<StructureVariantModification> Modifications { get; set; } = new();
    }
    
    /// <summary>
    /// Modification for structure variants
    /// </summary>
    public class StructureVariantModification
    {
        public string ComponentType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Generated structure component
    /// </summary>
    public class StructureComponent
    {
        public string Type { get; set; }
        public int RelativeX { get; set; }
        public int RelativeY { get; set; }
        public int RelativeZ { get; set; }
        public string BlockType { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
