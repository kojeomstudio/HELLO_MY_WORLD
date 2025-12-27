using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for entity spawning (mobs, animals, etc.)
    /// </summary>
    public class EntitySpawnLayer : IContentLayer
    {
        private readonly EntitySpawnConfig _config;
        private readonly Dictionary<string, EntityType> _entityTypes;
        private readonly FastNoise _spawnNoise;
        
        public string LayerId => "EntitySpawn";
        public int Priority => 30; // After all terrain and structure generation
        public bool IsEnabled { get; set; } = true;
        
        public EntitySpawnLayer(EntitySpawnConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityTypes = new Dictionary<string, EntityType>();
            _spawnNoise = new FastNoise();
            
            // Initialize entity types from configuration
            foreach (var entityType in _config.EntityTypes)
            {
                _entityTypes[entityType.Name] = entityType;
            }
            
            // Initialize spawn noise
            _spawnNoise.SetNoiseType(FastNoise.NoiseType.Value);
            _spawnNoise.SetFrequency(_config.SpawnFrequency);
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(EntitySpawnConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var entitySpawns = new List<EntitySpawnData>();
            
            // Generate entity spawns for this chunk
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeConfig = _config.BiomeConfigs.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Check if entities should spawn at this position
                    if (ShouldSpawnEntities(worldX, worldZ, context))
                    {
                        var spawns = GenerateEntitySpawns(localX, localZ, biome, biomeConfig, context);
                        entitySpawns.AddRange(spawns);
                    }
                }
            }
            
            // Update context with generated entity spawns
            context.EntitySpawns = entitySpawns.ToArray();
            
            Console.WriteLine($"[EntitySpawnLayer] Generated {entitySpawns.Count} entity spawns for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private bool ShouldSpawnEntities(int worldX, int worldZ, TerrainGenerationContext context)
        {
            // Use noise to determine if entities should spawn at this position
            var noiseValue = _spawnNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            return normalizedNoise < _config.SpawnChance;
        }
        
        private List<EntitySpawnData> GenerateEntitySpawns(int localX, int localZ, BiomeType biome, BiomeEntityConfig biomeConfig, TerrainGenerationContext context)
        {
            var spawns = new List<EntitySpawnData>();
            
            // Get ground position for spawning
            var groundY = FindGroundLevel(localX, localZ, context);
            if (groundY < 0)
                return spawns;
            
            // Get entity types that can spawn in this biome
            var eligibleEntities = GetEligibleEntities(biome, biomeConfig, groundY, context);
            
            // Select entities to spawn based on spawn weights
            foreach (var entityType in eligibleEntities)
            {
                if (context.Random.NextDouble() < entityType.SpawnChance)
                {
                    var spawnData = CreateEntitySpawn(entityType, localX, localZ, groundY, context);
                    if (spawnData != null)
                    {
                        spawns.Add(spawnData);
                    }
                }
            }
            
            return spawns;
        }
        
        private int FindGroundLevel(int localX, int localZ, TerrainGenerationContext context)
        {
            // Find first non-air block from top to bottom
            for (int y = context.Config.MaxHeight - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    return y + 1; // Spawn position is one block above ground
                }
            }
            
            return -1; // No ground found
        }
        
        private List<EntityType> GetEligibleEntities(BiomeType biome, BiomeEntityConfig biomeConfig, int groundY, TerrainGenerationContext context)
        {
            var eligibleEntities = new List<EntityType>();
            
            // Get all entity types that can spawn in this biome
            var biomeEntities = _entityTypes.Values
                .Where(e => e.AllowedBiomes.Contains(biome))
                .ToList();
            
            // Check spawn conditions for each entity type
            foreach (var entityType in biomeEntities)
            {
                if (IsEntityEligible(entityType, groundY, context))
                {
                    eligibleEntities.Add(entityType);
                }
            }
            
            return eligibleEntities;
        }
        
        private bool IsEntityEligible(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            // Check height requirements
            if (groundY < entityType.MinSpawnHeight || groundY > entityType.MaxSpawnHeight)
                return false;
            
            // Check light level requirements
            if (entityType.RequiresDarkness && !IsDarkEnough(context, groundY))
                return false;
            
            if (entityType.RequiresLight && IsDarkEnough(context, groundY))
                return false;
            
            // Check time requirements
            if (entityType.SpawnTime == SpawnTime.Night && !IsNightTime(context))
                return false;
            
            if (entityType.SpawnTime == SpawnTime.Day && IsNightTime(context))
                return false;
            
            // Check block requirements
            if (!CheckSpawnBlockRequirements(entityType, groundY, context))
                return false;
            
            return true;
        }
        
        private bool IsDarkEnough(TerrainGenerationContext context, int y)
        {
            // Simple darkness check - in a real implementation, this would check actual light levels
            // For now, we'll use a simple heuristic based on depth
            return y < 50; // Below y=50 is considered dark enough
        }
        
        private bool IsNightTime(TerrainGenerationContext context)
        {
            // Simple time check - in a real implementation, this would check actual world time
            // For now, we'll use a random check
            return context.Random.NextDouble() < 0.5f;
        }
        
        private bool CheckSpawnBlockRequirements(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            if (entityType.RequiredSpawnBlocks.Count == 0)
                return true;
            
            // Check if ground block matches required blocks
            foreach (var localX in Enumerable.Range(0, context.ChunkSize))
            {
                foreach (var localZ in Enumerable.Range(0, context.ChunkSize))
                {
                    var groundBlock = context.BlockTypes[localX, groundY - 1, localZ];
                    if (entityType.RequiredSpawnBlocks.Contains(groundBlock.ToString()))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private EntitySpawnData CreateEntitySpawn(EntityType entityType, int localX, int localZ, int groundY, TerrainGenerationContext context)
        {
            var worldX = context.ChunkX * context.ChunkSize + localX;
            var worldZ = context.ChunkZ * context.ChunkSize + localZ;
            
            var spawnData = new EntitySpawnData
            {
                EntityType = entityType.Name,
                X = worldX,
                Y = groundY,
                Z = worldZ,
                SpawnChance = entityType.SpawnChance,
                Properties = new Dictionary<string, object>()
            };
            
            // Add entity-specific properties
            spawnData.Properties["health"] = entityType.MaxHealth;
            spawnData.Properties["ai_type"] = entityType.AIType;
            spawnData.Properties["behavior"] = entityType.BehaviorType;
            
            // Add random variations
            if (entityType.HasVariations)
            {
                spawnData.Properties["variant"] = SelectEntityVariant(entityType, context);
            }
            
            // Add equipment for hostile mobs
            if (entityType.CanHaveEquipment)
            {
                var equipment = GenerateEquipment(entityType, context);
                if (equipment.Count > 0)
                {
                    spawnData.Properties["equipment"] = equipment;
                }
            }
            
            return spawnData;
        }
        
        private string SelectEntityVariant(EntityType entityType, TerrainGenerationContext context)
        {
            if (entityType.Variants.Count == 0)
                return "default";
            
            var random = context.Random.NextDouble();
            var cumulativeChance = 0.0;
            
            foreach (var variant in entityType.Variants)
            {
                cumulativeChance += variant.Chance;
                if (random < cumulativeChance)
                    return variant.Name;
            }
            
            return entityType.Variants.Last().Name;
        }
        
        private Dictionary<string, object> GenerateEquipment(EntityType entityType, TerrainGenerationContext context)
        {
            var equipment = new Dictionary<string, object>();
            
            // Generate weapon
            if (context.Random.NextDouble() < entityType.WeaponChance)
            {
                var weapon = SelectWeapon(entityType, context);
                if (weapon != null)
                {
                    equipment["weapon"] = weapon;
                }
            }
            
            // Generate armor
            if (context.Random.NextDouble() < entityType.ArmorChance)
            {
                var armor = SelectArmor(entityType, context);
                if (armor.Count > 0)
                {
                    equipment["armor"] = armor;
                }
            }
            
            return equipment;
        }
        
        private object SelectWeapon(EntityType entityType, TerrainGenerationContext context)
        {
            var availableWeapons = entityType.PossibleWeapons;
            if (availableWeapons.Count == 0)
                return null;
            
            var weaponIndex = context.Random.Next(availableWeapons.Count);
            return availableWeapons[weaponIndex];
        }
        
        private Dictionary<string, object> SelectArmor(EntityType entityType, TerrainGenerationContext context)
        {
            var armor = new Dictionary<string, object>();
            var availableArmor = entityType.PossibleArmor;
            
            foreach (var armorPiece in availableArmor)
            {
                if (context.Random.NextDouble() < 0.3f) // 30% chance for each armor piece
                {
                    armor[armorPiece.Slot] = armorPiece.Type;
                }
            }
            
            return armor;
        }
    }
    
    /// <summary>
    /// Configuration for entity spawning
    /// </summary>
    public class EntitySpawnConfig
    {
        public List<EntityType> EntityTypes { get; set; } = new();
        public List<BiomeEntityConfig> BiomeConfigs { get; set; } = new();
        public float SpawnFrequency { get; set; } = 0.05f;
        public float SpawnChance { get; set; } = 0.1f;
        public int MaxEntitiesPerChunk { get; set; } = 20;
        public bool EnablePackSpawning { get; set; } = true;
        public float PackSpawnChance { get; set; } = 0.3f;
        public int MaxPackSize { get; set; } = 6;
        public float GlobalSpawnRateModifier { get; set; } = 1.0f;
        public bool EnableLightLevelRestrictions { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for entity spawning in a specific biome
    /// </summary>
    public class BiomeEntityConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> EntityModifiers { get; set; } = new();
        public float OverallSpawnModifier { get; set; } = 1.0f;
        public List<string> ExcludedEntities { get; set; } = new();
    }
    
    /// <summary>
    /// Configuration for a specific entity type
    /// </summary>
    public class EntityType
    {
        public string Name { get; set; }
        public string Category { get; set; } // "hostile", "passive", "neutral"
        public float SpawnChance { get; set; } = 0.1f;
        public int MinSpawnHeight { get; set; } = 0;
        public int MaxSpawnHeight { get; set; } = 256;
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        public List<BiomeType> ForbiddenBiomes { get; set; } = new();
        public bool RequiresDarkness { get; set; } = false;
        public bool RequiresLight { get; set; } = false;
        public SpawnTime SpawnTime { get; set; } = SpawnTime.Any;
        public List<string> RequiredSpawnBlocks { get; set; } = new();
        public float MaxHealth { get; set; } = 20f;
        public string AIType { get; set; } = "basic";
        public string BehaviorType { get; set; } = "wander";
        public bool HasVariations { get; set; } = false;
        public List<EntityVariant> Variants { get; set; } = new();
        public bool CanHaveEquipment { get; set; } = false;
        public float WeaponChance { get; set; } = 0.1f;
        public float ArmorChance { get; set; } = 0.1f;
        public List<string> PossibleWeapons { get; set; } = new();
        public List<ArmorPiece> PossibleArmor { get; set; } = new();
        public bool CanSpawnUnderground { get; set; } = true;
        public bool CanSpawnOnSurface { get; set; } = true;
        public int MinGroupSize { get; set; } = 1;
        public int MaxGroupSize { get; set; } = 1;
        public float Rarity { get; set; } = 1.0f;
        public AIBehavior AIBehavior { get; set; } = new();
        public List<EquipmentDrop> Equipment { get; set; } = new();
    }
    
    /// <summary>
    /// Entity variant configuration
    /// </summary>
    public class EntityVariant
    {
        public string Name { get; set; }
        public float Chance { get; set; } = 1.0f;
        public float Weight { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Armor piece configuration
    /// </summary>
    public class ArmorPiece
    {
        public string Slot { get; set; } // "helmet", "chestplate", "leggings", "boots"
        public string Type { get; set; } // "leather", "iron", "gold", "diamond"
    }
    
    /// <summary>
    /// Spawn time enumeration
    /// </summary>
    public enum SpawnTime
    {
        Any,
        Day,
        Night
    }
    
    /// <summary>
    /// AI behavior configuration
    /// </summary>
    public class AIBehavior
    {
        public float Health { get; set; } = 20f;
        public List<string> Behaviors { get; set; } = new();
        public List<string> Targets { get; set; } = new();
    }
    
    /// <summary>
    /// Equipment drop configuration
    /// </summary>
    public class EquipmentDrop
    {
        public string Slot { get; set; }
        public string ItemId { get; set; }
        public float Chance { get; set; }
        public int MinEnchantmentLevel { get; set; } = 0;
        public int MaxEnchantmentLevel { get; set; } = 0;
    }
    
    /// <summary>
    /// Factory for creating default entity spawn configurations
    /// </summary>
    public static class EntitySpawnConfigFactory
    {
        /// <summary>
        /// Creates a default entity spawn configuration
        /// </summary>
        public static EntitySpawnConfig CreateDefault()
        {
            var config = new EntitySpawnConfig();
            
            // Add standard entity types
            config.EntityTypes.AddRange(GetStandardEntityTypes());
            
            // Add biome configurations
            config.BiomeConfigs.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard entity types
        /// </summary>
        private static List<EntityType> GetStandardEntityTypes()
        {
            return new List<EntityType>
            {
                // Hostile mobs
                new EntityType
                {
                    Name = "zombie",
                    Category = "hostile",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "hostile",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.2f,
                    ArmorChance = 0.1f,
                    PossibleWeapons = new List<string> { "wooden_sword", "stone_sword", "iron_sword" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "leather" },
                        new ArmorPiece { Slot = "chestplate", Type = "leather" },
                        new ArmorPiece { Slot = "leggings", Type = "leather" },
                        new ArmorPiece { Slot = "boots", Type = "leather" }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 20f,
                        Behaviors = new List<string> { "wander", "attack_player", "attack_villager" },
                        Targets = new List<string> { "player", "villager" }
                    },
                    Equipment = new List<EquipmentDrop>
                    {
                        new EquipmentDrop { Slot = "weapon", ItemId = "iron_sword", Chance = 0.1f },
                        new EquipmentDrop { Slot = "helmet", ItemId = "iron_helmet", Chance = 0.05f }
                    }
                },
                new EntityType
                {
                    Name = "skeleton",
                    Category = "hostile",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "ranged",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.8f,
                    ArmorChance = 0.3f,
                    PossibleWeapons = new List<string> { "bow" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "iron" },
                        new ArmorPiece { Slot = "chestplate", Type = "iron" },
                        new ArmorPiece { Slot = "leggings", Type = "iron" },
                        new ArmorPiece { Slot = "boots", Type = "iron" }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 20f,
                        Behaviors = new List<string> { "wander", "ranged_attack", "keep_distance" },
                        Targets = new List<string> { "player" }
                    },
                    Equipment = new List<EquipmentDrop>
                    {
                        new EquipmentDrop { Slot = "weapon", ItemId = "bow", Chance = 0.8f },
                        new EquipmentDrop { Slot = "helmet", ItemId = "iron_helmet", Chance = 0.3f }
                    }
                },
                new EntityType
                {
                    Name = "creeper",
                    Category = "hostile",
                    SpawnChance = 0.4f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "explosive",
                    BehaviorType = "aggressive",
                    AIBehavior = new AIBehavior
                    {
                        Health = 20f,
                        Behaviors = new List<string> { "wander", "explode_near_player" },
                        Targets = new List<string> { "player" }
                    }
                },
                
                // Passive mobs
                new EntityType
                {
                    Name = "cow",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.9f },
                        new EntityVariant { Name = "mooshroom", Chance = 0.1f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 10f,
                        Behaviors = new List<string> { "graze", "flee_from_player" },
                        Targets = new List<string>()
                    }
                },
                new EntityType
                {
                    Name = "pig",
                    Category = "passive",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    AIBehavior = new AIBehavior
                    {
                        Health = 10f,
                        Behaviors = new List<string> { "wander", "flee_from_player" },
                        Targets = new List<string>()
                    }
                },
                new EntityType
                {
                    Name = "chicken",
                    Category = "passive",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 4f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.95f },
                        new EntityVariant { Name = "baby", Chance = 0.05f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 4f,
                        Behaviors = new List<string> { "wander", "flee_from_player" },
                        Targets = new List<string>()
                    }
                },
                new EntityType
                {
                    Name = "sheep",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 8f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "white", Chance = 0.8f },
                        new EntityVariant { Name = "black", Chance = 0.05f },
                        new EntityVariant { Name = "gray", Chance = 0.05f },
                        new EntityVariant { Name = "brown", Chance = 0.05f },
                        new EntityVariant { Name = "pink", Chance = 0.05f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 8f,
                        Behaviors = new List<string> { "graze", "flee_from_player" },
                        Targets = new List<string>()
                    }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome entity configurations
        /// </summary>
        private static List<BiomeEntityConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeEntityConfig>
            {
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Plains,
                    OverallSpawnModifier = 1.2f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 1.5f,
                        ["sheep"] = 1.3f,
                        ["chicken"] = 1.2f,
                        ["pig"] = 1.4f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Forest,
                    OverallSpawnModifier = 1.0f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["wolf"] = 1.5f,
                        ["cow"] = 0.8f,
                        ["sheep"] = 1.2f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallSpawnModifier = 0.6f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 0.3f,
                        ["sheep"] = 0.2f,
                        ["chicken"] = 0.8f
                    },
                    ExcludedEntities = new List<string> { "pig" }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallSpawnModifier = 0.8f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["goat"] = 2.0f,
                        ["cow"] = 0.4f,
                        ["sheep"] = 0.6f
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
    /// Content layer for entity spawning (mobs, animals, etc.)
    /// </summary>
    public class EntitySpawnLayer : IContentLayer
    {
        private readonly EntitySpawnConfig _config;
        private readonly Dictionary<string, EntityType> _entityTypes;
        private readonly FastNoise _spawnNoise;
        
        public string LayerId => "EntitySpawn";
        public int Priority => 30; // After all terrain and structure generation
        public bool IsEnabled { get; set; } = true;
        
        public EntitySpawnLayer(EntitySpawnConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityTypes = new Dictionary<string, EntityType>();
            _spawnNoise = new FastNoise();
            
            // Initialize entity types from configuration
            foreach (var entityType in _config.EntityTypes)
            {
                _entityTypes[entityType.Name] = entityType;
            }
            
            // Initialize spawn noise
            _spawnNoise.SetNoiseType(FastNoise.NoiseType.Value);
            _spawnNoise.SetFrequency(_config.SpawnFrequency);
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(EntitySpawnConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var entitySpawns = new List<EntitySpawnData>();
            
            // Generate entity spawns for this chunk
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeConfig = _config.BiomeConfigs.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Check if entities should spawn at this position
                    if (ShouldSpawnEntities(worldX, worldZ, context))
                    {
                        var spawns = GenerateEntitySpawns(localX, localZ, biome, biomeConfig, context);
                        entitySpawns.AddRange(spawns);
                    }
                }
            }
            
            // Update context with generated entity spawns
            context.EntitySpawns = entitySpawns.ToArray();
            
            Console.WriteLine($"[EntitySpawnLayer] Generated {entitySpawns.Count} entity spawns for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private bool ShouldSpawnEntities(int worldX, int worldZ, TerrainGenerationContext context)
        {
            // Use noise to determine if entities should spawn at this position
            var noiseValue = _spawnNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            return normalizedNoise < _config.SpawnChance;
        }
        
        private List<EntitySpawnData> GenerateEntitySpawns(int localX, int localZ, BiomeType biome, BiomeEntityConfig biomeConfig, TerrainGenerationContext context)
        {
            var spawns = new List<EntitySpawnData>();
            
            // Get ground position for spawning
            var groundY = FindGroundLevel(localX, localZ, context);
            if (groundY < 0)
                return spawns;
            
            // Get entity types that can spawn in this biome
            var eligibleEntities = GetEligibleEntities(biome, biomeConfig, groundY, context);
            
            // Select entities to spawn based on spawn weights
            foreach (var entityType in eligibleEntities)
            {
                if (context.Random.NextDouble() < entityType.SpawnChance)
                {
                    var spawnData = CreateEntitySpawn(entityType, localX, localZ, groundY, context);
                    if (spawnData != null)
                    {
                        spawns.Add(spawnData);
                    }
                }
            }
            
            return spawns;
        }
        
        private int FindGroundLevel(int localX, int localZ, TerrainGenerationContext context)
        {
            // Find first non-air block from top to bottom
            for (int y = context.Config.MaxHeight - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    return y + 1; // Spawn position is one block above ground
                }
            }
            
            return -1; // No ground found
        }
        
        private List<EntityType> GetEligibleEntities(BiomeType biome, BiomeEntityConfig biomeConfig, int groundY, TerrainGenerationContext context)
        {
            var eligibleEntities = new List<EntityType>();
            
            // Get all entity types that can spawn in this biome
            var biomeEntities = _entityTypes.Values
                .Where(e => e.AllowedBiomes.Contains(biome))
                .ToList();
            
            // Check spawn conditions for each entity type
            foreach (var entityType in biomeEntities)
            {
                if (IsEntityEligible(entityType, groundY, context))
                {
                    eligibleEntities.Add(entityType);
                }
            }
            
            return eligibleEntities;
        }
        
        private bool IsEntityEligible(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            // Check height requirements
            if (groundY < entityType.MinSpawnHeight || groundY > entityType.MaxSpawnHeight)
                return false;
            
            // Check light level requirements
            if (entityType.RequiresDarkness && !IsDarkEnough(context, groundY))
                return false;
            
            if (entityType.RequiresLight && IsDarkEnough(context, groundY))
                return false;
            
            // Check time requirements
            if (entityType.SpawnTime == SpawnTime.Night && !IsNightTime(context))
                return false;
            
            if (entityType.SpawnTime == SpawnTime.Day && IsNightTime(context))
                return false;
            
            // Check block requirements
            if (!CheckSpawnBlockRequirements(entityType, groundY, context))
                return false;
            
            return true;
        }
        
        private bool IsDarkEnough(TerrainGenerationContext context, int y)
        {
            // Simple darkness check - in a real implementation, this would check actual light levels
            // For now, we'll use a simple heuristic based on depth
            return y < 50; // Below y=50 is considered dark enough
        }
        
        private bool IsNightTime(TerrainGenerationContext context)
        {
            // Simple time check - in a real implementation, this would check actual world time
            // For now, we'll use a random check
            return context.Random.NextDouble() < 0.5f;
        }
        
        private bool CheckSpawnBlockRequirements(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            if (entityType.RequiredSpawnBlocks.Count == 0)
                return true;
            
            // Check if ground block matches required blocks
            foreach (var localX in Enumerable.Range(0, context.ChunkSize))
            {
                foreach (var localZ in Enumerable.Range(0, context.ChunkSize))
                {
                    var groundBlock = context.BlockTypes[localX, groundY - 1, localZ];
                    if (entityType.RequiredSpawnBlocks.Contains(groundBlock.ToString()))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private EntitySpawnData CreateEntitySpawn(EntityType entityType, int localX, int localZ, int groundY, TerrainGenerationContext context)
        {
            var worldX = context.ChunkX * context.ChunkSize + localX;
            var worldZ = context.ChunkZ * context.ChunkSize + localZ;
            
            var spawnData = new EntitySpawnData
            {
                EntityType = entityType.Name,
                X = worldX,
                Y = groundY,
                Z = worldZ,
                SpawnChance = entityType.SpawnChance,
                Properties = new Dictionary<string, object>()
            };
            
            // Add entity-specific properties
            spawnData.Properties["health"] = entityType.MaxHealth;
            spawnData.Properties["ai_type"] = entityType.AIType;
            spawnData.Properties["behavior"] = entityType.BehaviorType;
            
            // Add random variations
            if (entityType.HasVariations)
            {
                spawnData.Properties["variant"] = SelectEntityVariant(entityType, context);
            }
            
            // Add equipment for hostile mobs
            if (entityType.CanHaveEquipment)
            {
                var equipment = GenerateEquipment(entityType, context);
                if (equipment.Count > 0)
                {
                    spawnData.Properties["equipment"] = equipment;
                }
            }
            
            return spawnData;
        }
        
        private string SelectEntityVariant(EntityType entityType, TerrainGenerationContext context)
        {
            if (entityType.Variants.Count == 0)
                return "default";
            
            var random = context.Random.NextDouble();
            var cumulativeChance = 0.0;
            
            foreach (var variant in entityType.Variants)
            {
                cumulativeChance += variant.Chance;
                if (random < cumulativeChance)
                    return variant.Name;
            }
            
            return entityType.Variants.Last().Name;
        }
        
        private Dictionary<string, object> GenerateEquipment(EntityType entityType, TerrainGenerationContext context)
        {
            var equipment = new Dictionary<string, object>();
            
            // Generate weapon
            if (context.Random.NextDouble() < entityType.WeaponChance)
            {
                var weapon = SelectWeapon(entityType, context);
                if (weapon != null)
                {
                    equipment["weapon"] = weapon;
                }
            }
            
            // Generate armor
            if (context.Random.NextDouble() < entityType.ArmorChance)
            {
                var armor = SelectArmor(entityType, context);
                if (armor.Count > 0)
                {
                    equipment["armor"] = armor;
                }
            }
            
            return equipment;
        }
        
        private object SelectWeapon(EntityType entityType, TerrainGenerationContext context)
        {
            var availableWeapons = entityType.PossibleWeapons;
            if (availableWeapons.Count == 0)
                return null;
            
            var weaponIndex = context.Random.Next(availableWeapons.Count);
            return availableWeapons[weaponIndex];
        }
        
        private Dictionary<string, object> SelectArmor(EntityType entityType, TerrainGenerationContext context)
        {
            var armor = new Dictionary<string, object>();
            var availableArmor = entityType.PossibleArmor;
            
            foreach (var armorPiece in availableArmor)
            {
                if (context.Random.NextDouble() < 0.3f) // 30% chance for each armor piece
                {
                    armor[armorPiece.Slot] = armorPiece.Type;
                }
            }
            
            return armor;
        }
    }
    
    /// <summary>
    /// Configuration for entity spawning
    /// </summary>
    public class EntitySpawnConfig
    {
        public List<EntityType> EntityTypes { get; set; } = new();
        public List<BiomeEntityConfig> BiomeConfigs { get; set; } = new();
        public float SpawnFrequency { get; set; } = 0.05f;
        public float SpawnChance { get; set; } = 0.1f;
        public int MaxEntitiesPerChunk { get; set; } = 20;
        public bool EnablePackSpawning { get; set; } = true;
        public float PackSpawnChance { get; set; } = 0.3f;
        public int MaxPackSize { get; set; } = 6;
        public float GlobalSpawnRateModifier { get; set; } = 1.0f;
        public bool EnableLightLevelRestrictions { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for entity spawning in a specific biome
    /// </summary>
    public class BiomeEntityConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> EntityModifiers { get; set; } = new();
        public float OverallSpawnModifier { get; set; } = 1.0f;
        public List<string> ExcludedEntities { get; set; } = new();
    }
    
    /// <summary>
    /// Configuration for a specific entity type
    /// </summary>
    public class EntityType
    {
        public string Name { get; set; }
        public string Category { get; set; } // "hostile", "passive", "neutral"
        public float SpawnChance { get; set; } = 0.1f;
        public int MinSpawnHeight { get; set; } = 0;
        public int MaxSpawnHeight { get; set; } = 256;
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        public List<BiomeType> ForbiddenBiomes { get; set; } = new();
        public bool RequiresDarkness { get; set; } = false;
        public bool RequiresLight { get; set; } = false;
        public SpawnTime SpawnTime { get; set; } = SpawnTime.Any;
        public List<string> RequiredSpawnBlocks { get; set; } = new();
        public float MaxHealth { get; set; } = 20f;
        public string AIType { get; set; } = "basic";
        public string BehaviorType { get; set; } = "wander";
        public bool HasVariations { get; set; } = false;
        public List<EntityVariant> Variants { get; set; } = new();
        public bool CanHaveEquipment { get; set; } = false;
        public float WeaponChance { get; set; } = 0.1f;
        public float ArmorChance { get; set; } = 0.1f;
        public List<string> PossibleWeapons { get; set; } = new();
        public List<ArmorPiece> PossibleArmor { get; set; } = new();
        public bool CanSpawnUnderground { get; set; } = true;
        public bool CanSpawnOnSurface { get; set; } = true;
        public int MinGroupSize { get; set; } = 1;
        public int MaxGroupSize { get; set; } = 1;
        public float Rarity { get; set; } = 1.0f;
        public AIBehavior AIBehavior { get; set; } = new();
        public List<EquipmentDrop> Equipment { get; set; } = new();
    }
    
    /// <summary>
    /// Entity variant configuration
    /// </summary>
    public class EntityVariant
    {
        public string Name { get; set; }
        public float Chance { get; set; } = 1.0f;
        public float Weight { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Armor piece configuration
    /// </summary>
    public class ArmorPiece
    {
        public string Slot { get; set; } // "helmet", "chestplate", "leggings", "boots"
        public string Type { get; set; } // "leather", "iron", "gold", "diamond"
    }
    
    /// <summary>
    /// Spawn time enumeration
    /// </summary>
    public enum SpawnTime
    {
        Any,
        Day,
        Night
    }
    
    /// <summary>
    /// AI behavior configuration
    /// </summary>
    public class AIBehavior
    {
        public float Health { get; set; } = 20f;
        public List<string> Behaviors { get; set; } = new();
        public List<string> Targets { get; set; } = new();
    }
    
    /// <summary>
    /// Equipment drop configuration
    /// </summary>
    public class EquipmentDrop
    {
        public string Slot { get; set; }
        public string ItemId { get; set; }
        public float Chance { get; set; }
        public int MinEnchantmentLevel { get; set; } = 0;
        public int MaxEnchantmentLevel { get; set; } = 0;
    }
    
    /// <summary>
    /// Factory for creating default entity spawn configurations
    /// </summary>
    public static class EntitySpawnConfigFactory
    {
        /// <summary>
        /// Creates a default entity spawn configuration
        /// </summary>
        public static EntitySpawnConfig CreateDefault()
        {
            var config = new EntitySpawnConfig();
            
            // Add standard entity types
            config.EntityTypes.AddRange(GetStandardEntityTypes());
            
            // Add biome configurations
            config.BiomeConfigs.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard entity types
        /// </summary>
        private static List<EntityType> GetStandardEntityTypes()
        {
            return new List<EntityType>
            {
                // Hostile mobs
                new EntityType
                {
                    Name = "zombie",
                    Category = "hostile",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "hostile",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.2f,
                    ArmorChance = 0.1f,
                    PossibleWeapons = new List<string> { "wooden_sword", "stone_sword", "iron_sword" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "leather" },
                        new ArmorPiece { Slot = "chestplate", Type = "leather" },
                        new ArmorPiece { Slot = "leggings", Type = "leather" },
                        new ArmorPiece { Slot = "boots", Type = "leather" }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 20f,
                        Behaviors = new List<string> { "wander", "attack_player", "attack_villager" },
                        Targets = new List<string> { "player", "villager" }
                    },
                    Equipment = new List<EquipmentDrop>
                    {
                        new EquipmentDrop { Slot = "weapon", ItemId = "iron_sword", Chance = 0.1f },
                        new EquipmentDrop { Slot = "helmet", ItemId = "iron_helmet", Chance = 0.05f }
                    }
                },
                new EntityType
                {
                    Name = "skeleton",
                    Category = "hostile",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "ranged",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.8f,
                    ArmorChance = 0.3f,
                    PossibleWeapons = new List<string> { "bow" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "iron" },
                        new ArmorPiece { Slot = "chestplate", Type = "iron" },
                        new ArmorPiece { Slot = "leggings", Type = "iron" },
                        new ArmorPiece { Slot = "boots", Type = "iron" }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 20f,
                        Behaviors = new List<string> { "wander", "ranged_attack", "keep_distance" },
                        Targets = new List<string> { "player" }
                    },
                    Equipment = new List<EquipmentDrop>
                    {
                        new EquipmentDrop { Slot = "weapon", ItemId = "bow", Chance = 0.8f },
                        new EquipmentDrop { Slot = "helmet", ItemId = "iron_helmet", Chance = 0.3f }
                    }
                },
                new EntityType
                {
                    Name = "creeper",
                    Category = "hostile",
                    SpawnChance = 0.4f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "explosive",
                    BehaviorType = "aggressive",
                    AIBehavior = new AIBehavior
                    {
                        Health = 20f,
                        Behaviors = new List<string> { "wander", "explode_near_player" },
                        Targets = new List<string> { "player" }
                    }
                },
                
                // Passive mobs
                new EntityType
                {
                    Name = "cow",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.9f },
                        new EntityVariant { Name = "mooshroom", Chance = 0.1f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 10f,
                        Behaviors = new List<string> { "graze", "flee_from_player" },
                        Targets = new List<string>()
                    }
                },
                new EntityType
                {
                    Name = "pig",
                    Category = "passive",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    AIBehavior = new AIBehavior
                    {
                        Health = 10f,
                        Behaviors = new List<string> { "wander", "flee_from_player" },
                        Targets = new List<string>()
                    }
                },
                new EntityType
                {
                    Name = "chicken",
                    Category = "passive",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 4f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.95f },
                        new EntityVariant { Name = "baby", Chance = 0.05f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 4f,
                        Behaviors = new List<string> { "wander", "flee_from_player" },
                        Targets = new List<string>()
                    }
                },
                new EntityType
                {
                    Name = "sheep",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 8f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "white", Chance = 0.8f },
                        new EntityVariant { Name = "black", Chance = 0.05f },
                        new EntityVariant { Name = "gray", Chance = 0.05f },
                        new EntityVariant { Name = "brown", Chance = 0.05f },
                        new EntityVariant { Name = "pink", Chance = 0.05f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        Health = 8f,
                        Behaviors = new List<string> { "graze", "flee_from_player" },
                        Targets = new List<string>()
                    }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome entity configurations
        /// </summary>
        private static List<BiomeEntityConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeEntityConfig>
            {
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Plains,
                    OverallSpawnModifier = 1.2f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 1.5f,
                        ["sheep"] = 1.3f,
                        ["chicken"] = 1.2f,
                        ["pig"] = 1.4f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Forest,
                    OverallSpawnModifier = 1.0f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["wolf"] = 1.5f,
                        ["cow"] = 0.8f,
                        ["sheep"] = 1.2f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallSpawnModifier = 0.6f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 0.3f,
                        ["sheep"] = 0.2f,
                        ["chicken"] = 0.8f
                    },
                    ExcludedEntities = new List<string> { "pig" }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallSpawnModifier = 0.8f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["goat"] = 2.0f,
                        ["cow"] = 0.4f,
                        ["sheep"] = 0.6f
                    }
                }
            };
        }
    }
}
}
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.9f },
                        new EntityVariant { Name = "mooshroom", Chance = 0.1f }
                    }
                },
                new EntityType
                {
                    Name = "pig",
                    Category = "passive",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "wander"
                },
                new EntityType
                {
                    Name = "chicken",
                    Category = "passive",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 4f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.95f },
                        new EntityVariant { Name = "baby", Chance = 0.05f }
                    }
                },
                new EntityType
                {
                    Name = "sheep",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 8f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "white", Chance = 0.8f },
                        new EntityVariant { Name = "black", Chance = 0.05f },
                        new EntityVariant { Name = "gray", Chance = 0.05f },
                        new EntityVariant { Name = "brown", Chance = 0.05f },
                        new EntityVariant { Name = "pink", Chance = 0.05f }
                    }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome entity configurations
        /// </summary>
        private static List<BiomeEntityConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeEntityConfig>
            {
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Plains,
                    OverallSpawnModifier = 1.2f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 1.5f,
                        ["sheep"] = 1.3f,
                        ["chicken"] = 1.2f,
                        ["pig"] = 1.4f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Forest,
                    OverallSpawnModifier = 1.0f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["wolf"] = 1.5f,
                        ["cow"] = 0.8f,
                        ["sheep"] = 1.2f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallSpawnModifier = 0.6f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 0.3f,
                        ["sheep"] = 0.2f,
                        ["chicken"] = 0.8f
                    },
                    ExcludedEntities = new List<string> { "pig" }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallSpawnModifier = 0.8f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["goat"] = 2.0f,
                        ["cow"] = 0.4f,
                        ["sheep"] = 0.6f
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
    /// Content layer for entity spawning (mobs, animals, etc.)
    /// </summary>
    public class EntitySpawnLayer : IContentLayer
    {
        private readonly EntitySpawnConfig _config;
        private readonly Dictionary<string, EntityType> _entityTypes;
        private readonly FastNoise _spawnNoise;
        
        public string LayerId => "EntitySpawn";
        public int Priority => 30; // After all terrain and structure generation
        public bool IsEnabled { get; set; } = true;
        
        public EntitySpawnLayer(EntitySpawnConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityTypes = new Dictionary<string, EntityType>();
            _spawnNoise = new FastNoise();
            
            // Initialize entity types from configuration
            foreach (var entityType in _config.EntityTypes)
            {
                _entityTypes[entityType.Name] = entityType;
            }
            
            // Initialize spawn noise
            _spawnNoise.SetNoiseType(FastNoise.NoiseType.Value);
            _spawnNoise.SetFrequency(_config.SpawnFrequency);
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(EntitySpawnConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var entitySpawns = new List<EntitySpawnData>();
            
            // Generate entity spawns for this chunk
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeConfig = _config.BiomeConfigs.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Check if entities should spawn at this position
                    if (ShouldSpawnEntities(worldX, worldZ, context))
                    {
                        var spawns = GenerateEntitySpawns(localX, localZ, biome, biomeConfig, context);
                        entitySpawns.AddRange(spawns);
                    }
                }
            }
            
            // Update context with generated entity spawns
            context.EntitySpawns = entitySpawns.ToArray();
            
            Console.WriteLine($"[EntitySpawnLayer] Generated {entitySpawns.Count} entity spawns for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private bool ShouldSpawnEntities(int worldX, int worldZ, TerrainGenerationContext context)
        {
            // Use noise to determine if entities should spawn at this position
            var noiseValue = _spawnNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            return normalizedNoise < _config.SpawnChance;
        }
        
        private List<EntitySpawnData> GenerateEntitySpawns(int localX, int localZ, BiomeType biome, BiomeEntityConfig biomeConfig, TerrainGenerationContext context)
        {
            var spawns = new List<EntitySpawnData>();
            
            // Get ground position for spawning
            var groundY = FindGroundLevel(localX, localZ, context);
            if (groundY < 0)
                return spawns;
            
            // Get entity types that can spawn in this biome
            var eligibleEntities = GetEligibleEntities(biome, biomeConfig, groundY, context);
            
            // Select entities to spawn based on spawn weights
            foreach (var entityType in eligibleEntities)
            {
                if (context.Random.NextDouble() < entityType.SpawnChance)
                {
                    var spawnData = CreateEntitySpawn(entityType, localX, localZ, groundY, context);
                    if (spawnData != null)
                    {
                        spawns.Add(spawnData);
                    }
                }
            }
            
            return spawns;
        }
        
        private int FindGroundLevel(int localX, int localZ, TerrainGenerationContext context)
        {
            // Find first non-air block from top to bottom
            for (int y = context.Config.MaxHeight - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    return y + 1; // Spawn position is one block above ground
                }
            }
            
            return -1; // No ground found
        }
        
        private List<EntityType> GetEligibleEntities(BiomeType biome, BiomeEntityConfig biomeConfig, int groundY, TerrainGenerationContext context)
        {
            var eligibleEntities = new List<EntityType>();
            
            // Get all entity types that can spawn in this biome
            var biomeEntities = _entityTypes.Values
                .Where(e => e.AllowedBiomes.Contains(biome))
                .ToList();
            
            // Check spawn conditions for each entity type
            foreach (var entityType in biomeEntities)
            {
                if (IsEntityEligible(entityType, groundY, context))
                {
                    eligibleEntities.Add(entityType);
                }
            }
            
            return eligibleEntities;
        }
        
        private bool IsEntityEligible(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            // Check height requirements
            if (groundY < entityType.MinSpawnHeight || groundY > entityType.MaxSpawnHeight)
                return false;
            
            // Check light level requirements
            if (entityType.RequiresDarkness && !IsDarkEnough(context, groundY))
                return false;
            
            if (entityType.RequiresLight && IsDarkEnough(context, groundY))
                return false;
            
            // Check time requirements
            if (entityType.SpawnTime == SpawnTime.Night && !IsNightTime(context))
                return false;
            
            if (entityType.SpawnTime == SpawnTime.Day && IsNightTime(context))
                return false;
            
            // Check block requirements
            if (!CheckSpawnBlockRequirements(entityType, groundY, context))
                return false;
            
            return true;
        }
        
        private bool IsDarkEnough(TerrainGenerationContext context, int y)
        {
            // Simple darkness check - in a real implementation, this would check actual light levels
            // For now, we'll use a simple heuristic based on depth
            return y < 50; // Below y=50 is considered dark enough
        }
        
        private bool IsNightTime(TerrainGenerationContext context)
        {
            // Simple time check - in a real implementation, this would check actual world time
            // For now, we'll use a random check
            return context.Random.NextDouble() < 0.5f;
        }
        
        private bool CheckSpawnBlockRequirements(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            if (entityType.RequiredSpawnBlocks.Count == 0)
                return true;
            
            // Check if ground block matches required blocks
            foreach (var localX in Enumerable.Range(0, context.ChunkSize))
            {
                foreach (var localZ in Enumerable.Range(0, context.ChunkSize))
                {
                    var groundBlock = context.BlockTypes[localX, groundY - 1, localZ];
                    if (entityType.RequiredSpawnBlocks.Contains(groundBlock.ToString()))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private EntitySpawnData CreateEntitySpawn(EntityType entityType, int localX, int localZ, int groundY, TerrainGenerationContext context)
        {
            var worldX = context.ChunkX * context.ChunkSize + localX;
            var worldZ = context.ChunkZ * context.ChunkSize + localZ;
            
            var spawnData = new EntitySpawnData
            {
                EntityType = entityType.Name,
                X = worldX,
                Y = groundY,
                Z = worldZ,
                SpawnChance = entityType.SpawnChance,
                Properties = new Dictionary<string, object>()
            };
            
            // Add entity-specific properties
            spawnData.Properties["health"] = entityType.MaxHealth;
            spawnData.Properties["ai_type"] = entityType.AIType;
            spawnData.Properties["behavior"] = entityType.BehaviorType;
            
            // Add random variations
            if (entityType.HasVariations)
            {
                spawnData.Properties["variant"] = SelectEntityVariant(entityType, context);
            }
            
            // Add equipment for hostile mobs
            if (entityType.CanHaveEquipment)
            {
                var equipment = GenerateEquipment(entityType, context);
                if (equipment.Count > 0)
                {
                    spawnData.Properties["equipment"] = equipment;
                }
            }
            
            return spawnData;
        }
        
        private string SelectEntityVariant(EntityType entityType, TerrainGenerationContext context)
        {
            if (entityType.Variants.Count == 0)
                return "default";
            
            var random = context.Random.NextDouble();
            var cumulativeChance = 0.0;
            
            foreach (var variant in entityType.Variants)
            {
                cumulativeChance += variant.Chance;
                if (random < cumulativeChance)
                    return variant.Name;
            }
            
            return entityType.Variants.Last().Name;
        }
        
        private Dictionary<string, object> GenerateEquipment(EntityType entityType, TerrainGenerationContext context)
        {
            var equipment = new Dictionary<string, object>();
            
            // Generate weapon
            if (context.Random.NextDouble() < entityType.WeaponChance)
            {
                var weapon = SelectWeapon(entityType, context);
                if (weapon != null)
                {
                    equipment["weapon"] = weapon;
                }
            }
            
            // Generate armor
            if (context.Random.NextDouble() < entityType.ArmorChance)
            {
                var armor = SelectArmor(entityType, context);
                if (armor.Count > 0)
                {
                    equipment["armor"] = armor;
                }
            }
            
            return equipment;
        }
        
        private object SelectWeapon(EntityType entityType, TerrainGenerationContext context)
        {
            var availableWeapons = entityType.PossibleWeapons;
            if (availableWeapons.Count == 0)
                return null;
            
            var weaponIndex = context.Random.Next(availableWeapons.Count);
            return availableWeapons[weaponIndex];
        }
        
        private Dictionary<string, object> SelectArmor(EntityType entityType, TerrainGenerationContext context)
        {
            var armor = new Dictionary<string, object>();
            var availableArmor = entityType.PossibleArmor;
            
            foreach (var armorPiece in availableArmor)
            {
                if (context.Random.NextDouble() < 0.3f) // 30% chance for each armor piece
                {
                    armor[armorPiece.Slot] = armorPiece.Type;
                }
            }
            
            return armor;
        }
    }
    
    /// <summary>
    /// Configuration for entity spawning
    /// </summary>
    public class EntitySpawnConfig
    {
        public List<EntityType> EntityTypes { get; set; } = new();
        public List<BiomeEntityConfig> BiomeConfigs { get; set; } = new();
        public float SpawnFrequency { get; set; } = 0.05f;
        public float SpawnChance { get; set; } = 0.1f;
        public int MaxEntitiesPerChunk { get; set; } = 20;
        public bool EnablePackSpawning { get; set; } = true;
        public float PackSpawnChance { get; set; } = 0.3f;
        public int MaxPackSize { get; set; } = 6;
    }
    
    /// <summary>
    /// Configuration for entity spawning in a specific biome
    /// </summary>
    public class BiomeEntityConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> EntityModifiers { get; set; } = new();
        public float OverallSpawnModifier { get; set; } = 1.0f;
        public List<string> ExcludedEntities { get; set; } = new();
    }
    
    /// <summary>
    /// Configuration for a specific entity type
    /// </summary>
    public class EntityType
    {
        public string Name { get; set; }
        public string Category { get; set; } // "hostile", "passive", "neutral"
        public float SpawnChance { get; set; } = 0.1f;
        public int MinSpawnHeight { get; set; } = 0;
        public int MaxSpawnHeight { get; set; } = 256;
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        public bool RequiresDarkness { get; set; } = false;
        public bool RequiresLight { get; set; } = false;
        public SpawnTime SpawnTime { get; set; } = SpawnTime.Any;
        public List<string> RequiredSpawnBlocks { get; set; } = new();
        public float MaxHealth { get; set; } = 20f;
        public string AIType { get; set; } = "basic";
        public string BehaviorType { get; set; } = "wander";
        public bool HasVariations { get; set; } = false;
        public List<EntityVariant> Variants { get; set; } = new();
        public bool CanHaveEquipment { get; set; } = false;
        public float WeaponChance { get; set; } = 0.1f;
        public float ArmorChance { get; set; } = 0.1f;
        public List<string> PossibleWeapons { get; set; } = new();
        public List<ArmorPiece> PossibleArmor { get; set; } = new();
    }
    
    /// <summary>
    /// Entity variant configuration
    /// </summary>
    public class EntityVariant
    {
        public string Name { get; set; }
        public float Chance { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Armor piece configuration
    /// </summary>
    public class ArmorPiece
    {
        public string Slot { get; set; } // "helmet", "chestplate", "leggings", "boots"
        public string Type { get; set; } // "leather", "iron", "gold", "diamond"
    }
    
    /// <summary>
    /// Spawn time enumeration
    /// </summary>
    public enum SpawnTime
    {
        Any,
        Day,
        Night
    }
    
    /// <summary>
    /// Factory for creating default entity spawn configurations
    /// </summary>
    public static class EntitySpawnConfigFactory
    {
        /// <summary>
        /// Creates a default entity spawn configuration
        /// </summary>
        public static EntitySpawnConfig CreateDefault()
        {
            var config = new EntitySpawnConfig();
            
            // Add standard entity types
            config.EntityTypes.AddRange(GetStandardEntityTypes());
            
            // Add biome configurations
            config.BiomeConfigs.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard entity types
        /// </summary>
        private static List<EntityType> GetStandardEntityTypes()
        {
            return new List<EntityType>
            {
                // Hostile mobs
                new EntityType
                {
                    Name = "zombie",
                    Category = "hostile",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "hostile",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.2f,
                    ArmorChance = 0.1f,
                    PossibleWeapons = new List<string> { "wooden_sword", "stone_sword", "iron_sword" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "leather" },
                        new ArmorPiece { Slot = "chestplate", Type = "leather" },
                        new ArmorPiece { Slot = "leggings", Type = "leather" },
                        new ArmorPiece { Slot = "boots", Type = "leather" }
                    }
                },
                new EntityType
                {
                    Name = "skeleton",
                    Category = "hostile",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "ranged",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.8f,
                    ArmorChance = 0.3f,
                    PossibleWeapons = new List<string> { "bow" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "iron" },
                        new ArmorPiece { Slot = "chestplate", Type = "iron" },
                        new ArmorPiece { Slot = "leggings", Type = "iron" },
                        new ArmorPiece { Slot = "boots", Type = "iron" }
                    }
                },
                new EntityType
                {
                    Name = "creeper",
                    Category = "hostile",
                    SpawnChance = 0.4f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "explosive",
                    BehaviorType = "aggressive"
                },
                
                // Passive mobs
                new EntityType
                {
                    Name = "cow",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.9f },
                        new EntityVariant { Name = "mooshroom", Chance = 0.1f }
                    }
                },
                new EntityType
                {
                    Name = "pig",
                    Category = "passive",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "wander"
                },
                new EntityType
                {
                    Name = "chicken",
                    Category = "passive",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 4f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.95f },
                        new EntityVariant { Name = "baby", Chance = 0.05f }
                    }
                },
                new EntityType
                {
                    Name = "sheep",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 8f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "white", Chance = 0.8f },
                        new EntityVariant { Name = "black", Chance = 0.05f },
                        new EntityVariant { Name = "gray", Chance = 0.05f },
                        new EntityVariant { Name = "brown", Chance = 0.05f },
                        new EntityVariant { Name = "pink", Chance = 0.05f }
                    }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome entity configurations
        /// </summary>
        private static List<BiomeEntityConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeEntityConfig>
            {
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Plains,
                    OverallSpawnModifier = 1.2f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 1.5f,
                        ["sheep"] = 1.3f,
                        ["chicken"] = 1.2f,
                        ["pig"] = 1.4f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Forest,
                    OverallSpawnModifier = 1.0f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["wolf"] = 1.5f,
                        ["cow"] = 0.8f,
                        ["sheep"] = 1.2f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallSpawnModifier = 0.6f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 0.3f,
                        ["sheep"] = 0.2f,
                        ["chicken"] = 0.8f
                    },
                    ExcludedEntities = new List<string> { "pig" }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallSpawnModifier = 0.8f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["goat"] = 2.0f,
                        ["cow"] = 0.4f,
                        ["sheep"] = 0.6f
                    }
                }
            };
        }
    }
}
}
using GameServerApp.World.Generation.Interfaces;

namespace GameServerApp.World.Generation.Content
{
    /// <summary>
    /// Content layer for entity spawning (mobs, animals, etc.)
    /// </summary>
    public class EntitySpawnLayer : IContentLayer
    {
        private readonly EntitySpawnConfig _config;
        private readonly Dictionary<string, EntityType> _entityTypes;
        private readonly FastNoise _spawnNoise;
        
        public string LayerId => "EntitySpawn";
        public int Priority => 30; // After all terrain and structure generation
        public bool IsEnabled { get; set; } = true;
        
        public EntitySpawnLayer(EntitySpawnConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityTypes = new Dictionary<string, EntityType>();
            _spawnNoise = new FastNoise();
            
            // Initialize entity types from configuration
            foreach (var entityType in _config.EntityTypes)
            {
                _entityTypes[entityType.Name] = entityType;
            }
            
            // Initialize spawn noise
            _spawnNoise.SetNoiseType(FastNoise.NoiseType.Value);
            _spawnNoise.SetFrequency(_config.SpawnFrequency);
        }
        
        public T GetConfig<T>() where T : class, new()
        {
            if (typeof(T) == typeof(EntitySpawnConfig))
            {
                return _config as T;
            }
            return new T();
        }
        
        public void Execute(TerrainGenerationContext context)
        {
            var chunkSize = context.ChunkSize;
            var entitySpawns = new List<EntitySpawnData>();
            
            // Generate entity spawns for this chunk
            for (int localX = 0; localX < chunkSize; localX++)
            {
                for (int localZ = 0; localZ < chunkSize; localZ++)
                {
                    var worldX = context.ChunkX * chunkSize + localX;
                    var worldZ = context.ChunkZ * chunkSize + localZ;
                    
                    // Get biome for this position
                    var biome = context.GetBiome(localX, localZ);
                    var biomeConfig = _config.BiomeConfigs.FirstOrDefault(b => b.BiomeType == biome);
                    
                    // Check if entities should spawn at this position
                    if (ShouldSpawnEntities(worldX, worldZ, context))
                    {
                        var spawns = GenerateEntitySpawns(localX, localZ, biome, biomeConfig, context);
                        entitySpawns.AddRange(spawns);
                    }
                }
            }
            
            // Update context with generated entity spawns
            context.EntitySpawns = entitySpawns.ToArray();
            
            Console.WriteLine($"[EntitySpawnLayer] Generated {entitySpawns.Count} entity spawns for chunk ({context.ChunkX},{context.ChunkZ})");
        }
        
        private bool ShouldSpawnEntities(int worldX, int worldZ, TerrainGenerationContext context)
        {
            // Use noise to determine if entities should spawn at this position
            var noiseValue = _spawnNoise.GetNoise(worldX, worldZ);
            var normalizedNoise = (noiseValue + 1.0f) * 0.5f;
            
            return normalizedNoise < _config.SpawnChance;
        }
        
        private List<EntitySpawnData> GenerateEntitySpawns(int localX, int localZ, BiomeType biome, BiomeEntityConfig biomeConfig, TerrainGenerationContext context)
        {
            var spawns = new List<EntitySpawnData>();
            
            // Get ground position for spawning
            var groundY = FindGroundLevel(localX, localZ, context);
            if (groundY < 0)
                return spawns;
            
            // Get entity types that can spawn in this biome
            var eligibleEntities = GetEligibleEntities(biome, biomeConfig, groundY, context);
            
            // Select entities to spawn based on spawn weights
            foreach (var entityType in eligibleEntities)
            {
                if (context.Random.NextDouble() < entityType.SpawnChance)
                {
                    var spawnData = CreateEntitySpawn(entityType, localX, localZ, groundY, context);
                    if (spawnData != null)
                    {
                        spawns.Add(spawnData);
                    }
                }
            }
            
            return spawns;
        }
        
        private int FindGroundLevel(int localX, int localZ, TerrainGenerationContext context)
        {
            // Find the first non-air block from top to bottom
            for (int y = context.Config.MaxHeight - 1; y >= 0; y--)
            {
                if (context.BlockTypes[localX, y, localZ] != 0) // Not air
                {
                    return y + 1; // Spawn position is one block above ground
                }
            }
            
            return -1; // No ground found
        }
        
        private List<EntityType> GetEligibleEntities(BiomeType biome, BiomeEntityConfig biomeConfig, int groundY, TerrainGenerationContext context)
        {
            var eligibleEntities = new List<EntityType>();
            
            // Get all entity types that can spawn in this biome
            var biomeEntities = _entityTypes.Values
                .Where(e => e.AllowedBiomes.Contains(biome))
                .ToList();
            
            // Check spawn conditions for each entity type
            foreach (var entityType in biomeEntities)
            {
                if (IsEntityEligible(entityType, groundY, context))
                {
                    eligibleEntities.Add(entityType);
                }
            }
            
            return eligibleEntities;
        }
        
        private bool IsEntityEligible(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            // Check height requirements
            if (groundY < entityType.MinSpawnHeight || groundY > entityType.MaxSpawnHeight)
                return false;
            
            // Check light level requirements
            if (entityType.RequiresDarkness && !IsDarkEnough(context, groundY))
                return false;
            
            if (entityType.RequiresLight && IsDarkEnough(context, groundY))
                return false;
            
            // Check time requirements
            if (entityType.SpawnTime == SpawnTime.Night && !IsNightTime(context))
                return false;
            
            if (entityType.SpawnTime == SpawnTime.Day && IsNightTime(context))
                return false;
            
            // Check block requirements
            if (!CheckSpawnBlockRequirements(entityType, groundY, context))
                return false;
            
            return true;
        }
        
        private bool IsDarkEnough(TerrainGenerationContext context, int y)
        {
            // Simple darkness check - in a real implementation, this would check actual light levels
            // For now, we'll use a simple heuristic based on depth
            return y < 50; // Below y=50 is considered dark enough
        }
        
        private bool IsNightTime(TerrainGenerationContext context)
        {
            // Simple time check - in a real implementation, this would check actual world time
            // For now, we'll use a random check
            return context.Random.NextDouble() < 0.5f;
        }
        
        private bool CheckSpawnBlockRequirements(EntityType entityType, int groundY, TerrainGenerationContext context)
        {
            if (entityType.RequiredSpawnBlocks.Count == 0)
                return true;
            
            // Check if the ground block matches required blocks
            foreach (var localX in Enumerable.Range(0, context.ChunkSize))
            {
                foreach (var localZ in Enumerable.Range(0, context.ChunkSize))
                {
                    var groundBlock = context.BlockTypes[localX, groundY - 1, localZ];
                    if (entityType.RequiredSpawnBlocks.Contains(groundBlock.ToString()))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private EntitySpawnData CreateEntitySpawn(EntityType entityType, int localX, int localZ, int groundY, TerrainGenerationContext context)
        {
            var worldX = context.ChunkX * context.ChunkSize + localX;
            var worldZ = context.ChunkZ * context.ChunkSize + localZ;
            
            var spawnData = new EntitySpawnData
            {
                EntityType = entityType.Name,
                X = worldX,
                Y = groundY,
                Z = worldZ,
                SpawnChance = entityType.SpawnChance,
                Properties = new Dictionary<string, object>()
            };
            
            // Add entity-specific properties
            spawnData.Properties["health"] = entityType.MaxHealth;
            spawnData.Properties["ai_type"] = entityType.AIType;
            spawnData.Properties["behavior"] = entityType.BehaviorType;
            
            // Add random variations
            if (entityType.HasVariations)
            {
                spawnData.Properties["variant"] = SelectEntityVariant(entityType, context);
            }
            
            // Add equipment for hostile mobs
            if (entityType.CanHaveEquipment)
            {
                var equipment = GenerateEquipment(entityType, context);
                if (equipment.Count > 0)
                {
                    spawnData.Properties["equipment"] = equipment;
                }
            }
            
            return spawnData;
        }
        
        private string SelectEntityVariant(EntityType entityType, TerrainGenerationContext context)
        {
            if (entityType.Variants.Count == 0)
                return "default";
            
            var random = context.Random.NextDouble();
            var cumulativeChance = 0.0;
            
            foreach (var variant in entityType.Variants)
            {
                cumulativeChance += variant.Chance;
                if (random < cumulativeChance)
                    return variant.Name;
            }
            
            return entityType.Variants.Last().Name;
        }
        
        private Dictionary<string, object> GenerateEquipment(EntityType entityType, TerrainGenerationContext context)
        {
            var equipment = new Dictionary<string, object>();
            
            // Generate weapon
            if (context.Random.NextDouble() < entityType.WeaponChance)
            {
                var weapon = SelectWeapon(entityType, context);
                if (weapon != null)
                {
                    equipment["weapon"] = weapon;
                }
            }
            
            // Generate armor
            if (context.Random.NextDouble() < entityType.ArmorChance)
            {
                var armor = SelectArmor(entityType, context);
                if (armor.Count > 0)
                {
                    equipment["armor"] = armor;
                }
            }
            
            return equipment;
        }
        
        private object SelectWeapon(EntityType entityType, TerrainGenerationContext context)
        {
            var availableWeapons = entityType.PossibleWeapons;
            if (availableWeapons.Count == 0)
                return null;
            
            var weaponIndex = context.Random.Next(availableWeapons.Count);
            return availableWeapons[weaponIndex];
        }
        
        private Dictionary<string, object> SelectArmor(EntityType entityType, TerrainGenerationContext context)
        {
            var armor = new Dictionary<string, object>();
            var availableArmor = entityType.PossibleArmor;
            
            foreach (var armorPiece in availableArmor)
            {
                if (context.Random.NextDouble() < 0.3f) // 30% chance for each armor piece
                {
                    armor[armorPiece.Slot] = armorPiece.Type;
                }
            }
            
            return armor;
        }
    }
    
    /// <summary>
    /// Configuration for entity spawning
    /// </summary>
    public class EntitySpawnConfig
    {
        public List<EntityType> EntityTypes { get; set; } = new();
        public List<BiomeEntityConfig> BiomeConfigs { get; set; } = new();
        public float SpawnFrequency { get; set; } = 0.05f;
        public float SpawnChance { get; set; } = 0.1f;
        public int MaxEntitiesPerChunk { get; set; } = 20;
        public bool EnablePackSpawning { get; set; } = true;
        public float PackSpawnChance { get; set; } = 0.3f;
        public int MaxPackSize { get; set; } = 6;
    }
    
    /// <summary>
    /// Configuration for entity spawning in a specific biome
    /// </summary>
    public class BiomeEntityConfig
    {
        public BiomeType BiomeType { get; set; }
        public Dictionary<string, float> EntityModifiers { get; set; } = new();
        public float OverallSpawnModifier { get; set; } = 1.0f;
        public List<string> ExcludedEntities { get; set; } = new();
    }
    
    /// <summary>
    /// Configuration for a specific entity type
    /// </summary>
    public class EntityType
    {
        public string Name { get; set; }
        public string Category { get; set; } // "hostile", "passive", "neutral"
        public float SpawnChance { get; set; } = 0.1f;
        public int MinSpawnHeight { get; set; } = 0;
        public int MaxSpawnHeight { get; set; } = 256;
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        public bool RequiresDarkness { get; set; } = false;
        public bool RequiresLight { get; set; } = false;
        public SpawnTime SpawnTime { get; set; } = SpawnTime.Any;
        public List<string> RequiredSpawnBlocks { get; set; } = new();
        public float MaxHealth { get; set; } = 20f;
        public string AIType { get; set; } = "basic";
        public string BehaviorType { get; set; } = "wander";
        public bool HasVariations { get; set; } = false;
        public List<EntityVariant> Variants { get; set; } = new();
        public bool CanHaveEquipment { get; set; } = false;
        public float WeaponChance { get; set; } = 0.1f;
        public float ArmorChance { get; set; } = 0.1f;
        public List<string> PossibleWeapons { get; set; } = new();
        public List<ArmorPiece> PossibleArmor { get; set; } = new();
    }
    
    /// <summary>
    /// Entity variant configuration
    /// </summary>
    public class EntityVariant
    {
        public string Name { get; set; }
        public float Chance { get; set; } = 1.0f;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Armor piece configuration
    /// </summary>
    public class ArmorPiece
    {
        public string Slot { get; set; } // "helmet", "chestplate", "leggings", "boots"
        public string Type { get; set; } // "leather", "iron", "gold", "diamond"
    }
    
    /// <summary>
    /// Spawn time enumeration
    /// </summary>
    public enum SpawnTime
    {
        Any,
        Day,
        Night
    }
    
    /// <summary>
    /// Factory for creating default entity spawn configurations
    /// </summary>
    public static class EntitySpawnConfigFactory
    {
        /// <summary>
        /// Creates a default entity spawn configuration
        /// </summary>
        public static EntitySpawnConfig CreateDefault()
        {
            var config = new EntitySpawnConfig();
            
            // Add standard entity types
            config.EntityTypes.AddRange(GetStandardEntityTypes());
            
            // Add biome configurations
            config.BiomeConfigs.AddRange(GetStandardBiomeConfigs());
            
            return config;
        }
        
        /// <summary>
        /// Gets standard entity types
        /// </summary>
        private static List<EntityType> GetStandardEntityTypes()
        {
            return new List<EntityType>
            {
                // Hostile mobs
                new EntityType
                {
                    Name = "zombie",
                    Category = "hostile",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "hostile",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.2f,
                    ArmorChance = 0.1f,
                    PossibleWeapons = new List<string> { "wooden_sword", "stone_sword", "iron_sword" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "leather" },
                        new ArmorPiece { Slot = "chestplate", Type = "leather" },
                        new ArmorPiece { Slot = "leggings", Type = "leather" },
                        new ArmorPiece { Slot = "boots", Type = "leather" }
                    }
                },
                new EntityType
                {
                    Name = "skeleton",
                    Category = "hostile",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "ranged",
                    BehaviorType = "aggressive",
                    CanHaveEquipment = true,
                    WeaponChance = 0.8f,
                    ArmorChance = 0.3f,
                    PossibleWeapons = new List<string> { "bow" },
                    PossibleArmor = new List<ArmorPiece>
                    {
                        new ArmorPiece { Slot = "helmet", Type = "iron" },
                        new ArmorPiece { Slot = "chestplate", Type = "iron" },
                        new ArmorPiece { Slot = "leggings", Type = "iron" },
                        new ArmorPiece { Slot = "boots", Type = "iron" }
                    }
                },
                new EntityType
                {
                    Name = "creeper",
                    Category = "hostile",
                    SpawnChance = 0.4f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Mountains },
                    RequiresDarkness = true,
                    SpawnTime = SpawnTime.Night,
                    MaxHealth = 20f,
                    AIType = "explosive",
                    BehaviorType = "aggressive"
                },
                
                // Passive mobs
                new EntityType
                {
                    Name = "cow",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.9f },
                        new EntityVariant { Name = "mooshroom", Chance = 0.1f }
                    }
                },
                new EntityType
                {
                    Name = "pig",
                    Category = "passive",
                    SpawnChance = 0.6f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 10f,
                    AIType = "passive",
                    BehaviorType = "wander"
                },
                new EntityType
                {
                    Name = "chicken",
                    Category = "passive",
                    SpawnChance = 0.8f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Desert },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 4f,
                    AIType = "passive",
                    BehaviorType = "wander",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Chance = 0.95f },
                        new EntityVariant { Name = "baby", Chance = 0.05f }
                    }
                },
                new EntityType
                {
                    Name = "sheep",
                    Category = "passive",
                    SpawnChance = 0.7f,
                    MinSpawnHeight = 0,
                    MaxSpawnHeight = 256,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    RequiresLight = true,
                    SpawnTime = SpawnTime.Day,
                    MaxHealth = 8f,
                    AIType = "passive",
                    BehaviorType = "graze",
                    HasVariations = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "white", Chance = 0.8f },
                        new EntityVariant { Name = "black", Chance = 0.05f },
                        new EntityVariant { Name = "gray", Chance = 0.05f },
                        new EntityVariant { Name = "brown", Chance = 0.05f },
                        new EntityVariant { Name = "pink", Chance = 0.05f }
                    }
                }
            };
        }
        
        /// <summary>
        /// Gets standard biome entity configurations
        /// </summary>
        private static List<BiomeEntityConfig> GetStandardBiomeConfigs()
        {
            return new List<BiomeEntityConfig>
            {
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Plains,
                    OverallSpawnModifier = 1.2f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 1.5f,
                        ["sheep"] = 1.3f,
                        ["chicken"] = 1.2f,
                        ["pig"] = 1.4f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Forest,
                    OverallSpawnModifier = 1.0f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["wolf"] = 1.5f,
                        ["cow"] = 0.8f,
                        ["sheep"] = 1.2f
                    }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Desert,
                    OverallSpawnModifier = 0.6f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["cow"] = 0.3f,
                        ["sheep"] = 0.2f,
                        ["chicken"] = 0.8f
                    },
                    ExcludedEntities = new List<string> { "pig" }
                },
                new BiomeEntityConfig
                {
                    BiomeType = BiomeType.Mountains,
                    OverallSpawnModifier = 0.8f,
                    EntityModifiers = new Dictionary<string, float>
                    {
                        ["goat"] = 2.0f,
                        ["cow"] = 0.4f,
                        ["sheep"] = 0.6f
                    }
                }
            };
        }
    }
}
