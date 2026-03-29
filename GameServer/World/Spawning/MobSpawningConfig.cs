#if false
using System;
using System.Collections.Generic;

namespace GameServerApp.World.Spawning
{
    /// <summary>
    /// Stubbed mob spawning config (disabled pending integration with current world config model).
    /// </summary>
    public class MobSpawningConfig
    {
        public int SpawnIntervalMs { get; set; } = 5000;
        public int MaxSpawnsPerCycle { get; set; } = 10;
        public int MaxDespawnsPerCycle { get; set; } = 10;
        public int MaxNaturalSpawnsPerCycle { get; set; } = 5;
        public int SpawnCooldownSeconds { get; set; } = 30;
        public float MinLightLevel { get; set; } = 0.1f;
        public float MaxMobsPerArea { get; set; } = 5f;
        public float MinSpawnDistance { get; set; } = 8f;
        public float MaxMobsPerSpawnPoint { get; set; } = 3f;
        public float MinPlayerDistance { get; set; } = 16f;
        public int MaxTotalMobs { get; set; } = 200;
        public int MaxMobsPerChunk { get; set; } = 15;
        public int MaxMobsPerPlayer { get; set; } = 20;
        public TimeSpan MobLifetime { get; set; } = TimeSpan.FromMinutes(30);
        public float DespawnDistanceThreshold { get; set; } = 128f;
        public Dictionary<string, float> TimeSpawnModifiers { get; set; } = new();
        public Dictionary<string, float> SeasonSpawnModifiers { get; set; } = new();
        public Dictionary<string, float> WeatherSpawnModifiers { get; set; } = new();
    }
}
#endif
