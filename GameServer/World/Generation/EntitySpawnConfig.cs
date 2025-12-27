using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for entity spawning
    /// </summary>
    public class EntitySpawnConfig
    {
        /// <summary>
        /// List of entity types that can spawn
        /// </summary>
        [JsonPropertyName("entityTypes")]
        public List<EntityType> EntityTypes { get; set; } = new();
        
        /// <summary>
        /// Global spawn rate modifier
        /// </summary>
        [JsonPropertyName("globalSpawnRateModifier")]
        public float GlobalSpawnRateModifier { get; set; } = 1.0f;
        
        /// <summary>
        /// Maximum entities per chunk
        /// </summary>
        [JsonPropertyName("maxEntitiesPerChunk")]
        public int MaxEntitiesPerChunk { get; set; } = 20;
        
        /// <summary>
        /// Spawn distance from player
        /// </summary>
        [JsonPropertyName("spawnDistanceFromPlayer")]
        public int SpawnDistanceFromPlayer { get; set; } = 128;
        
        /// <summary>
        /// Enable night-only spawning for hostile entities
        /// </summary>
        [JsonPropertyName("enableNightOnlySpawning")]
        public bool EnableNightOnlySpawning { get; set; } = true;
        
        /// <summary>
        /// Enable light level restrictions
        /// </summary>
        [JsonPropertyName("enableLightLevelRestrictions")]
        public bool EnableLightLevelRestrictions { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for a specific entity type
    /// </summary>
    public class EntityType
    {
        /// <summary>
        /// Entity type name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Spawn rarity (higher = rarer)
        /// </summary>
        [JsonPropertyName("rarity")]
        public float Rarity { get; set; } = 1.0f;
        
        /// <summary>
        /// Minimum group size
        /// </summary>
        [JsonPropertyName("minGroupSize")]
        public int MinGroupSize { get; set; } = 1;
        
        /// <summary>
        /// Maximum group size
        /// </summary>
        [JsonPropertyName("maxGroupSize")]
        public int MaxGroupSize { get; set; } = 1;
        
        /// <summary>
        /// Minimum light level for spawning
        /// </summary>
        [JsonPropertyName("minLightLevel")]
        public int MinLightLevel { get; set; } = 0;
        
        /// <summary>
        /// Maximum light level for spawning
        /// </summary>
        [JsonPropertyName("maxLightLevel")]
        public int MaxLightLevel { get; set; } = 15;
        
        /// <summary>
        /// Allowed biomes for spawning
        /// </summary>
        [JsonPropertyName("allowedBiomes")]
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        
        /// <summary>
        /// Forbidden biomes for spawning
        /// </summary>
        [JsonPropertyName("forbiddenBiomes")]
        public List<BiomeType> ForbiddenBiomes { get; set; } = new();
        
        /// <summary>
        /// Spawn time restrictions
        /// </summary>
        [JsonPropertyName("spawnTime")]
        public SpawnTime SpawnTime { get; set; } = SpawnTime.Any;
        
        /// <summary>
        /// Whether this is a hostile entity
        /// </summary>
        [JsonPropertyName("isHostile")]
        public bool IsHostile { get; set; } = false;
        
        /// <summary>
        /// Whether this entity can spawn underground
        /// </summary>
        [JsonPropertyName("canSpawnUnderground")]
        public bool CanSpawnUnderground { get; set; } = true;
        
        /// <summary>
        /// Whether this entity can spawn on surface
        /// </summary>
        [JsonPropertyName("canSpawnOnSurface")]
        public bool CanSpawnOnSurface { get; set; } = true;
        
        /// <summary>
        /// Whether this entity can spawn in water
        /// </summary>
        [JsonPropertyName("canSpawnInWater")]
        public bool CanSpawnInWater { get; set; } = false;
        
        /// <summary>
        /// Entity variants with their weights
        /// </summary>
        [JsonPropertyName("variants")]
        public List<EntityVariant> Variants { get; set; } = new();
        
        /// <summary>
        /// Equipment that can be spawned with
        /// </summary>
        [JsonPropertyName("equipment")]
        public List<EntityEquipment> Equipment { get; set; } = new();
        
        /// <summary>
        /// AI behavior configuration
        /// </summary>
        [JsonPropertyName("aiBehavior")]
        public AIBehavior AIBehavior { get; set; } = new();
    }
    
    /// <summary>
    /// Entity variant configuration
    /// </summary>
    public class EntityVariant
    {
        /// <summary>
        /// Variant name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Spawn weight
        /// </summary>
        [JsonPropertyName("weight")]
        public float Weight { get; set; } = 1.0f;
        
        /// <summary>
        /// Variant-specific properties
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Entity equipment configuration
    /// </summary>
    public class EntityEquipment
    {
        /// <summary>
        /// Equipment slot
        /// </summary>
        [JsonPropertyName("slot")]
        public string Slot { get; set; }
        
        /// <summary>
        /// Item ID
        /// </summary>
        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }
        
        /// <summary>
        /// Chance to spawn with this equipment
        /// </summary>
        [JsonPropertyName("chance")]
        public float Chance { get; set; } = 0.1f;
        
        /// <summary>
        /// Minimum enchantment level
        /// </summary>
        [JsonPropertyName("minEnchantmentLevel")]
        public int MinEnchantmentLevel { get; set; } = 0;
        
        /// <summary>
        /// Maximum enchantment level
        /// </summary>
        [JsonPropertyName("maxEnchantmentLevel")]
        public int MaxEnchantmentLevel { get; set; } = 0;
    }
    
    /// <summary>
    /// AI behavior configuration
    /// </summary>
    public class AIBehavior
    {
        /// <summary>
        /// Movement speed
        /// </summary>
        [JsonPropertyName("movementSpeed")]
        public float MovementSpeed { get; set; } = 1.0f;
        
        /// <summary>
        /// Attack damage
        /// </summary>
        [JsonPropertyName("attackDamage")]
        public float AttackDamage { get; set; } = 1.0f;
        
        /// <summary>
        /// Health points
        /// </summary>
        [JsonPropertyName("health")]
        public float Health { get; set; } = 20.0f;
        
        /// <summary>
        /// Detection range
        /// </summary>
        [JsonPropertyName("detectionRange")]
        public float DetectionRange { get; set; } = 16.0f;
        
        /// <summary>
        /// Attack range
        /// </summary>
        [JsonPropertyName("attackRange")]
        public float AttackRange { get; set; } = 2.0f;
        
        /// <summary>
        /// AI behaviors
        /// </summary>
        [JsonPropertyName("behaviors")]
        public List<string> Behaviors { get; set; } = new();
        
        /// <summary>
        /// AI targets
        /// </summary>
        [JsonPropertyName("targets")]
        public List<string> Targets { get; set; } = new();
    }
    
    /// <summary>
    /// Spawn time restrictions
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
                    Rarity = 2.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 4,
                    MinLightLevel = 0,
                    MaxLightLevel = 7,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains, BiomeType.Swamp },
                    SpawnTime = SpawnTime.Night,
                    IsHostile = true,
                    CanSpawnUnderground = true,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f },
                        new EntityVariant { Name = "husk", Weight = 0.2f }
                    },
                    Equipment = new List<EntityEquipment>
                    {
                        new EntityEquipment { Slot = "hand", ItemId = 268, Chance = 0.1f }, // Iron sword
                        new EntityEquipment { Slot = "helmet", ItemId = 306, Chance = 0.05f } // Iron helmet
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.8f,
                        AttackDamage = 3.0f,
                        Health = 20.0f,
                        DetectionRange = 16.0f,
                        AttackRange = 2.0f,
                        Behaviors = new List<string> { "wander", "attack_player", "attack_villager" },
                        Targets = new List<string> { "player", "villager", "iron_golem" }
                    }
                },
                
                new EntityType
                {
                    Name = "skeleton",
                    Rarity = 3.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 2,
                    MinLightLevel = 0,
                    MaxLightLevel = 7,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains, BiomeType.Swamp },
                    SpawnTime = SpawnTime.Night,
                    IsHostile = true,
                    CanSpawnUnderground = true,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f },
                        new EntityVariant { Name = "stray", Weight = 0.1f }
                    },
                    Equipment = new List<EntityEquipment>
                    {
                        new EntityEquipment { Slot = "hand", ItemId = 261, Chance = 0.8f }, // Bow
                        new EntityEquipment { Slot = "hand", ItemId = 262, Chance = 0.2f, MinEnchantmentLevel = 1, MaxEnchantmentLevel = 3 } // Arrow
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.9f,
                        AttackDamage = 2.0f,
                        Health = 20.0f,
                        DetectionRange = 16.0f,
                        AttackRange = 15.0f,
                        Behaviors = new List<string> { "wander", "ranged_attack", "avoid_player" },
                        Targets = new List<string> { "player", "villager", "iron_golem" }
                    }
                },
                
                new EntityType
                {
                    Name = "creeper",
                    Rarity = 5.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 1,
                    MinLightLevel = 0,
                    MaxLightLevel = 7,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains, BiomeType.Swamp },
                    SpawnTime = SpawnTime.Night,
                    IsHostile = true,
                    CanSpawnUnderground = true,
                    CanSpawnOnSurface = true,
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 1.0f,
                        AttackDamage = 49.0f, // Explosion damage
                        Health = 20.0f,
                        DetectionRange = 16.0f,
                        AttackRange = 3.0f,
                        Behaviors = new List<string> { "wander", "explode_near_player" },
                        Targets = new List<string> { "player", "villager", "iron_golem" }
                    }
                },
                
                // Passive mobs
                new EntityType
                {
                    Name = "cow",
                    Rarity = 1.0f,
                    MinGroupSize = 2,
                    MaxGroupSize = 6,
                    MinLightLevel = 7,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.7f,
                        AttackDamage = 0.0f,
                        Health = 10.0f,
                        DetectionRange = 8.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "wander", "graze", "flee_player" },
                        Targets = new List<string>()
                    }
                },
                
                new EntityType
                {
                    Name = "pig",
                    Rarity = 1.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 4,
                    MinLightLevel = 7,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.8f,
                        AttackDamage = 0.0f,
                        Health = 10.0f,
                        DetectionRange = 8.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "wander", "forage", "flee_player" },
                        Targets = new List<string>()
                    }
                },
                
                new EntityType
                {
                    Name = "chicken",
                    Rarity = 1.0f,
                    MinGroupSize = 2,
                    MaxGroupSize = 4,
                    MinLightLevel = 7,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.9f,
                        AttackDamage = 0.0f,
                        Health = 4.0f,
                        DetectionRange = 6.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "wander", "peck", "flee_player", "lay_egg" },
                        Targets = new List<string>()
                    }
                },
                
                // Water mobs
                new EntityType
                {
                    Name = "squid",
                    Rarity = 1.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 4,
                    MinLightLevel = 0,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Ocean, BiomeType.River },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = false,
                    CanSpawnInWater = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.6f,
                        AttackDamage = 0.0f,
                        Health = 10.0f,
                        DetectionRange = 8.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "swim", "wander" },
                        Targets = new List<string>()
                    }
                }
            };
        }
    }
}using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Configuration for entity spawning
    /// </summary>
    public class EntitySpawnConfig
    {
        /// <summary>
        /// List of entity types that can spawn
        /// </summary>
        [JsonPropertyName("entityTypes")]
        public List<EntityType> EntityTypes { get; set; } = new();
        
        /// <summary>
        /// Global spawn rate modifier
        /// </summary>
        [JsonPropertyName("globalSpawnRateModifier")]
        public float GlobalSpawnRateModifier { get; set; } = 1.0f;
        
        /// <summary>
        /// Maximum entities per chunk
        /// </summary>
        [JsonPropertyName("maxEntitiesPerChunk")]
        public int MaxEntitiesPerChunk { get; set; } = 20;
        
        /// <summary>
        /// Spawn distance from player
        /// </summary>
        [JsonPropertyName("spawnDistanceFromPlayer")]
        public int SpawnDistanceFromPlayer { get; set; } = 128;
        
        /// <summary>
        /// Enable night-only spawning for hostile entities
        /// </summary>
        [JsonPropertyName("enableNightOnlySpawning")]
        public bool EnableNightOnlySpawning { get; set; } = true;
        
        /// <summary>
        /// Enable light level restrictions
        /// </summary>
        [JsonPropertyName("enableLightLevelRestrictions")]
        public bool EnableLightLevelRestrictions { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for a specific entity type
    /// </summary>
    public class EntityType
    {
        /// <summary>
        /// Entity type name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Spawn rarity (higher = rarer)
        /// </summary>
        [JsonPropertyName("rarity")]
        public float Rarity { get; set; } = 1.0f;
        
        /// <summary>
        /// Minimum group size
        /// </summary>
        [JsonPropertyName("minGroupSize")]
        public int MinGroupSize { get; set; } = 1;
        
        /// <summary>
        /// Maximum group size
        /// </summary>
        [JsonPropertyName("maxGroupSize")]
        public int MaxGroupSize { get; set; } = 1;
        
        /// <summary>
        /// Minimum light level for spawning
        /// </summary>
        [JsonPropertyName("minLightLevel")]
        public int MinLightLevel { get; set; } = 0;
        
        /// <summary>
        /// Maximum light level for spawning
        /// </summary>
        [JsonPropertyName("maxLightLevel")]
        public int MaxLightLevel { get; set; } = 15;
        
        /// <summary>
        /// Allowed biomes for spawning
        /// </summary>
        [JsonPropertyName("allowedBiomes")]
        public List<BiomeType> AllowedBiomes { get; set; } = new();
        
        /// <summary>
        /// Forbidden biomes for spawning
        /// </summary>
        [JsonPropertyName("forbiddenBiomes")]
        public List<BiomeType> ForbiddenBiomes { get; set; } = new();
        
        /// <summary>
        /// Spawn time restrictions
        /// </summary>
        [JsonPropertyName("spawnTime")]
        public SpawnTime SpawnTime { get; set; } = SpawnTime.Any;
        
        /// <summary>
        /// Whether this is a hostile entity
        /// </summary>
        [JsonPropertyName("isHostile")]
        public bool IsHostile { get; set; } = false;
        
        /// <summary>
        /// Whether this entity can spawn underground
        /// </summary>
        [JsonPropertyName("canSpawnUnderground")]
        public bool CanSpawnUnderground { get; set; } = true;
        
        /// <summary>
        /// Whether this entity can spawn on surface
        /// </summary>
        [JsonPropertyName("canSpawnOnSurface")]
        public bool CanSpawnOnSurface { get; set; } = true;
        
        /// <summary>
        /// Whether this entity can spawn in water
        /// </summary>
        [JsonPropertyName("canSpawnInWater")]
        public bool CanSpawnInWater { get; set; } = false;
        
        /// <summary>
        /// Entity variants with their weights
        /// </summary>
        [JsonPropertyName("variants")]
        public List<EntityVariant> Variants { get; set; } = new();
        
        /// <summary>
        /// Equipment that can be spawned with
        /// </summary>
        [JsonPropertyName("equipment")]
        public List<EntityEquipment> Equipment { get; set; } = new();
        
        /// <summary>
        /// AI behavior configuration
        /// </summary>
        [JsonPropertyName("aiBehavior")]
        public AIBehavior AIBehavior { get; set; } = new();
    }
    
    /// <summary>
    /// Entity variant configuration
    /// </summary>
    public class EntityVariant
    {
        /// <summary>
        /// Variant name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Spawn weight
        /// </summary>
        [JsonPropertyName("weight")]
        public float Weight { get; set; } = 1.0f;
        
        /// <summary>
        /// Variant-specific properties
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    /// <summary>
    /// Entity equipment configuration
    /// </summary>
    public class EntityEquipment
    {
        /// <summary>
        /// Equipment slot
        /// </summary>
        [JsonPropertyName("slot")]
        public string Slot { get; set; }
        
        /// <summary>
        /// Item ID
        /// </summary>
        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }
        
        /// <summary>
        /// Chance to spawn with this equipment
        /// </summary>
        [JsonPropertyName("chance")]
        public float Chance { get; set; } = 0.1f;
        
        /// <summary>
        /// Minimum enchantment level
        /// </summary>
        [JsonPropertyName("minEnchantmentLevel")]
        public int MinEnchantmentLevel { get; set; } = 0;
        
        /// <summary>
        /// Maximum enchantment level
        /// </summary>
        [JsonPropertyName("maxEnchantmentLevel")]
        public int MaxEnchantmentLevel { get; set; } = 0;
    }
    
    /// <summary>
    /// AI behavior configuration
    /// </summary>
    public class AIBehavior
    {
        /// <summary>
        /// Movement speed
        /// </summary>
        [JsonPropertyName("movementSpeed")]
        public float MovementSpeed { get; set; } = 1.0f;
        
        /// <summary>
        /// Attack damage
        /// </summary>
        [JsonPropertyName("attackDamage")]
        public float AttackDamage { get; set; } = 1.0f;
        
        /// <summary>
        /// Health points
        /// </summary>
        [JsonPropertyName("health")]
        public float Health { get; set; } = 20.0f;
        
        /// <summary>
        /// Detection range
        /// </summary>
        [JsonPropertyName("detectionRange")]
        public float DetectionRange { get; set; } = 16.0f;
        
        /// <summary>
        /// Attack range
        /// </summary>
        [JsonPropertyName("attackRange")]
        public float AttackRange { get; set; } = 2.0f;
        
        /// <summary>
        /// AI behaviors
        /// </summary>
        [JsonPropertyName("behaviors")]
        public List<string> Behaviors { get; set; } = new();
        
        /// <summary>
        /// AI targets
        /// </summary>
        [JsonPropertyName("targets")]
        public List<string> Targets { get; set; } = new();
    }
    
    /// <summary>
    /// Spawn time restrictions
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
                    Rarity = 2.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 4,
                    MinLightLevel = 0,
                    MaxLightLevel = 7,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains, BiomeType.Swamp },
                    SpawnTime = SpawnTime.Night,
                    IsHostile = true,
                    CanSpawnUnderground = true,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f },
                        new EntityVariant { Name = "husk", Weight = 0.2f }
                    },
                    Equipment = new List<EntityEquipment>
                    {
                        new EntityEquipment { Slot = "hand", ItemId = 268, Chance = 0.1f }, // Iron sword
                        new EntityEquipment { Slot = "helmet", ItemId = 306, Chance = 0.05f } // Iron helmet
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.8f,
                        AttackDamage = 3.0f,
                        Health = 20.0f,
                        DetectionRange = 16.0f,
                        AttackRange = 2.0f,
                        Behaviors = new List<string> { "wander", "attack_player", "attack_villager" },
                        Targets = new List<string> { "player", "villager", "iron_golem" }
                    }
                },
                
                new EntityType
                {
                    Name = "skeleton",
                    Rarity = 3.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 2,
                    MinLightLevel = 0,
                    MaxLightLevel = 7,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains, BiomeType.Swamp },
                    SpawnTime = SpawnTime.Night,
                    IsHostile = true,
                    CanSpawnUnderground = true,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f },
                        new EntityVariant { Name = "stray", Weight = 0.1f }
                    },
                    Equipment = new List<EntityEquipment>
                    {
                        new EntityEquipment { Slot = "hand", ItemId = 261, Chance = 0.8f }, // Bow
                        new EntityEquipment { Slot = "hand", ItemId = 262, Chance = 0.2f, MinEnchantmentLevel = 1, MaxEnchantmentLevel = 3 } // Arrow
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.9f,
                        AttackDamage = 2.0f,
                        Health = 20.0f,
                        DetectionRange = 16.0f,
                        AttackRange = 15.0f,
                        Behaviors = new List<string> { "wander", "ranged_attack", "avoid_player" },
                        Targets = new List<string> { "player", "villager", "iron_golem" }
                    }
                },
                
                new EntityType
                {
                    Name = "creeper",
                    Rarity = 5.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 1,
                    MinLightLevel = 0,
                    MaxLightLevel = 7,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga, BiomeType.Mountains, BiomeType.Swamp },
                    SpawnTime = SpawnTime.Night,
                    IsHostile = true,
                    CanSpawnUnderground = true,
                    CanSpawnOnSurface = true,
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 1.0f,
                        AttackDamage = 49.0f, // Explosion damage
                        Health = 20.0f,
                        DetectionRange = 16.0f,
                        AttackRange = 3.0f,
                        Behaviors = new List<string> { "wander", "explode_near_player" },
                        Targets = new List<string> { "player", "villager", "iron_golem" }
                    }
                },
                
                // Passive mobs
                new EntityType
                {
                    Name = "cow",
                    Rarity = 1.0f,
                    MinGroupSize = 2,
                    MaxGroupSize = 6,
                    MinLightLevel = 7,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.7f,
                        AttackDamage = 0.0f,
                        Health = 10.0f,
                        DetectionRange = 8.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "wander", "graze", "flee_player" },
                        Targets = new List<string>()
                    }
                },
                
                new EntityType
                {
                    Name = "pig",
                    Rarity = 1.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 4,
                    MinLightLevel = 7,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.8f,
                        AttackDamage = 0.0f,
                        Health = 10.0f,
                        DetectionRange = 8.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "wander", "forage", "flee_player" },
                        Targets = new List<string>()
                    }
                },
                
                new EntityType
                {
                    Name = "chicken",
                    Rarity = 1.0f,
                    MinGroupSize = 2,
                    MaxGroupSize = 4,
                    MinLightLevel = 7,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Plains, BiomeType.Forest, BiomeType.Taiga },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.9f,
                        AttackDamage = 0.0f,
                        Health = 4.0f,
                        DetectionRange = 6.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "wander", "peck", "flee_player", "lay_egg" },
                        Targets = new List<string>()
                    }
                },
                
                // Water mobs
                new EntityType
                {
                    Name = "squid",
                    Rarity = 1.0f,
                    MinGroupSize = 1,
                    MaxGroupSize = 4,
                    MinLightLevel = 0,
                    MaxLightLevel = 15,
                    AllowedBiomes = new List<BiomeType> { BiomeType.Ocean, BiomeType.River },
                    SpawnTime = SpawnTime.Any,
                    IsHostile = false,
                    CanSpawnUnderground = false,
                    CanSpawnOnSurface = false,
                    CanSpawnInWater = true,
                    Variants = new List<EntityVariant>
                    {
                        new EntityVariant { Name = "normal", Weight = 1.0f }
                    },
                    AIBehavior = new AIBehavior
                    {
                        MovementSpeed = 0.6f,
                        AttackDamage = 0.0f,
                        Health = 10.0f,
                        DetectionRange = 8.0f,
                        AttackRange = 0.0f,
                        Behaviors = new List<string> { "swim", "wander" },
                        Targets = new List<string>()
                    }
                }
            };
        }
    }
}
