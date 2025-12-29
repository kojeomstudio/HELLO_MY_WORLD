using System;
using System.Collections.Generic;

namespace GameServerApp.World.Spawning
{
    /// <summary>
    /// Configuration for mob spawning system
    /// Controls spawn rates, conditions, mob types, and behaviors
    /// </summary>
    public class MobSpawningConfig
    {
        // Spawn timing and rate settings
        public int SpawnIntervalMs { get; set; } = 5000; // 5 seconds
        public int MaxSpawnsPerCycle { get; set; } = 10;
        public int MaxDespawnsPerCycle { get; set; } = 10;
        public int MaxNaturalSpawnsPerCycle { get; set; } = 5;
        public int SpawnCooldownSeconds { get; set; } = 30;
        
        // Spawn condition settings
        public float MinLightLevel { get; set; } = 0.1f;
        public float MaxMobsPerArea { get; set; } = 5f;
        public float MinSpawnDistance { get; set; } = 8f;
        public float MaxMobsPerSpawnPoint { get; set; } = 3f;
        public float MinPlayerDistance { get; set; } = 16f;
        
        // Global mob limits
        public int MaxTotalMobs { get; set; } = 200;
        public int MaxMobsPerChunk { get; set; } = 15;
        public int MaxMobsPerPlayer { get; set; } = 20;
        
        // Despawn settings
        public TimeSpan MobLifetime { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan MobDespawnDistance { get; set; } = TimeSpan.FromMinutes(5);
        public float DespawnDistanceThreshold { get; set; } = 128f;
        
        // Mob definitions by type
        public Dictionary<MobType, MobDefinition> MobDefinitions { get; set; } = new Dictionary<MobType, MobDefinition>();
        
        // Biome-specific spawn rules
        public Dictionary<BiomeType, BiomeSpawnRules> BiomeSpawnRules { get; set; } = new Dictionary<BiomeType, BiomeSpawnRules>();
        
        // Time-based spawn modifiers
        public Dictionary<TimeOfDay, float> TimeSpawnModifiers { get; set; } = new Dictionary<TimeOfDay, float>();
        
        // Season-based spawn modifiers
        public Dictionary<Season, float> SeasonSpawnModifiers { get; set; } = new Dictionary<Season, float>();
        
        // Weather-based spawn modifiers
