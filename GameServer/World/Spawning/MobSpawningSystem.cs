using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World.Spawning
{
    /// <summary>
    /// Basic mob spawning system that manages creature generation and lifecycle
    /// Supports various spawn conditions, mob types, and spawn behaviors
    /// </summary>
    public class MobSpawningSystem
    {
        private readonly ILogger<MobSpawningSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly MobSpawningConfig spawningConfig;
        
        // Mob storage
        private readonly ConcurrentDictionary<string, Mob> activeMobs;
        private readonly ConcurrentDictionary<string, MobSpawnPoint> spawnPoints;
        
        // Spawn management
        private readonly Timer spawnTimer;
        private readonly Random random;
        private readonly object lockObject = new object();
        
        // Performance tracking
        private int spawnedMobsPerCycle;
        private int despawnedMobsPerCycle;
        private DateTime lastSpawnCycle;
        
        // Spawn queues
        private readonly ConcurrentQueue<MobSpawnRequest> spawnQueue;
        private readonly ConcurrentQueue<string> despawnQueue;
        
        public MobSpawningSystem(ILogger<MobSpawningSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.spawningConfig = config.World.MobSpawning ?? new MobSpawningConfig();
            
            activeMobs = new ConcurrentDictionary<string, Mob>();
            spawnPoints = new ConcurrentDictionary<string, MobSpawnPoint>();
            spawnQueue = new ConcurrentQueue<MobSpawnRequest>();
            despawnQueue = new ConcurrentQueue<string>();
            
            random = new Random(config.World.WorldSeed);
            
            // Initialize spawn timer
            spawnTimer = new Timer(ProcessSpawnCycle, null, 
                TimeSpan.Zero, 
                TimeSpan.FromMilliseconds(spawningConfig.SpawnIntervalMs));
            
            lastSpawnCycle = DateTime.UtcNow;
            
            logger.LogInformation("[MobSpawningSystem] Initialized with spawn interval: {Interval}ms", 
                spawningConfig.SpawnIntervalMs);
        }
        
        /// <summary>
        /// Initializes spawn points for a chunk
        /// </summary>
        public void InitializeSpawnPoints(int chunkX, int chunkZ, ChunkData chunkData, BiomeData biomeData)
        {
            var chunkPos = new Vector3Int(chunkX, 0, chunkZ);
            
            // Generate spawn points based on biome
            for (int x = 0; x < chunkData.Size; x += 4) // Sample every 4 blocks
            {
                for (int z = 0; z < chunkData.Size; z += 4)
                {
                    var worldX = chunkX * chunkData.Size + x;
                    var worldZ = chunkZ * chunkData.Size + z;
                    var worldPos = new Vector3(worldX, chunkData.HeightMap[x, z], worldZ);
                    
                    // Check if this is a valid spawn location
                    if (IsValidSpawnLocation(worldPos, chunkData, biomeData, x, z))
                    {
                        var biomeType = biomeData.BiomeMap[x, z];
                        var spawnPoint = CreateSpawnPoint(worldPos, biomeType);
                        
                        spawnPoints[spawnPoint.Id] = spawnPoint;
                    }
                }
            }
            
            logger.LogDebug("[MobSpawningSystem] Initialized spawn points for chunk ({ChunkX}, {ChunkZ})", 
                chunkX, chunkZ);
        }
        
        /// <summary>
        /// Checks if a location is valid for spawning
        /// </summary>
        private bool IsValidSpawnLocation(Vector3 position, ChunkData chunkData, BiomeData biomeData, int localX, int localZ)
        {
            // Check if position is on solid ground
            var heightValue = chunkData.HeightMap[localX, localZ];
            if (position.Y < heightValue - 1 || position.Y > heightValue + 2)
                return false;
            
            // Check if position is not in water (unless water mob)
            var isInWater = position.Y < config.World.SeaLevel;
            
            // Check if position has enough space
            if (!HasEnoughSpace(position, 2f))
                return false;
            
            // Check light level
            var lightLevel = CalculateLightLevel(position, chunkData);
            if (lightLevel < spawningConfig.MinLightLevel)
                return false;
            
            // Check biome spawn rules
            var biomeType = biomeData.BiomeMap[localX, localZ];
            if (!IsBiomeValidForSpawning(biomeType, isInWater))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Checks if there's enough space for spawning
        /// </summary>
        private bool HasEnoughSpace(Vector3 position, float radius)
        {
            // Check for nearby mobs
            var nearbyMobs = GetMobsInRadius(position, radius);
            return nearbyMobs.Count < spawningConfig.MaxMobsPerArea;
        }
        
        /// <summary>
        /// Calculates light level at a position
        /// </summary>
        private float CalculateLightLevel(Vector3 position, ChunkData chunkData)
        {
            // Simplified light calculation based on height and time of day
            var baseLight = 0.8f; // Assume daylight for now
            var heightFactor = Math.Max(0, 1f - (position.Y / 100f)); // Less light underground
            
            return baseLight * heightFactor;
        }
        
        /// <summary>
        /// Checks if a biome is valid for spawning
        /// </summary>
        private bool IsBiomeValidForSpawning(BiomeType biomeType, bool isInWater)
        {
            var biomeSpawnRules = spawningConfig.GetBiomeSpawnRules(biomeType);
            if (biomeSpawnRules == null) return true;
            
            return isInWater ? biomeSpawnRules.AllowWaterSpawns : biomeSpawnRules.AllowLandSpawns;
        }
        
        /// <summary>
        /// Creates a spawn point
        /// </summary>
        private MobSpawnPoint CreateSpawnPoint(Vector3 position, BiomeType biomeType)
        {
            return new MobSpawnPoint
            {
                Id = Guid.NewGuid().ToString(),
                Position = position,
                BiomeType = biomeType,
                LastSpawnTime = DateTime.MinValue,
                SpawnCount = 0,
                IsActive = true
            };
        }
        
        /// <summary>
        /// Processes the spawn cycle
        /// </summary>
        private void ProcessSpawnCycle(object state)
        {
            spawnedMobsPerCycle = 0;
            despawnedMobsPerCycle = 0;
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Process despawns first
                ProcessDespawns();
                
                // Process spawn queue
                ProcessSpawnQueue();
                
                // Natural spawning
                ProcessNaturalSpawning();
                
                // Clean up inactive spawn points
                CleanupInactiveSpawnPoints();
                
                // Update performance metrics
                var cycleTime = DateTime.UtcNow - startTime;
                lastSpawnCycle = DateTime.UtcNow;
                
                logger.LogDebug("[MobSpawningSystem] Spawn cycle completed in {Time}ms - Spawned: {Spawned}, Despawned: {Despawned}", 
                    cycleTime.TotalMilliseconds, spawnedMobsPerCycle, despawnedMobsPerCycle);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[MobSpawningSystem] Error during spawn cycle");
            }
        }
        
        /// <summary>
        /// Processes despawn queue
        /// </summary>
        private void ProcessDespawns()
        {
            var processedCount = 0;
            
            while (despawnQueue.TryDequeue(out var mobId) && processedCount < spawningConfig.MaxDespawnsPerCycle)
            {
                if (activeMobs.TryRemove(mobId, out var mob))
                {
                    DespawnMob(mob);
                    despawnedMobsPerCycle++;
                    processedCount++;
                }
            }
        }
        
        /// <summary>
        /// Processes spawn queue
        /// </summary>
        private void ProcessSpawnQueue()
        {
            var processedCount = 0;
            
            while (spawnQueue.TryDequeue(out var request) && processedCount < spawningConfig.MaxSpawnsPerCycle)
            {
                if (TrySpawnMob(request))
                {
                    spawnedMobsPerCycle++;
                    processedCount++;
                }
            }
        }
        
        /// <summary>
        /// Processes natural spawning
        /// </summary>
        private void ProcessNaturalSpawning()
        {
            var activeSpawnPointsList = spawnPoints.Values.Where(sp => sp.IsActive).ToList();
            
            // Shuffle spawn points for randomness
            Shuffle(activeSpawnPointsList);
            
            var spawnedCount = 0;
            
            foreach (var spawnPoint in activeSpawnPointsList)
            {
                if (spawnedCount >= spawningConfig.MaxNaturalSpawnsPerCycle)
                    break;
                
                // Check if enough time has passed since last spawn
                var timeSinceLastSpawn = DateTime.UtcNow - spawnPoint.LastSpawnTime;
                if (timeSinceLastSpawn < TimeSpan.FromSeconds(spawningConfig.SpawnCooldownSeconds))
                    continue;
                
                // Check spawn conditions
                if (!CanSpawnAtPoint(spawnPoint))
                    continue;
                
                // Select mob type based on biome
                var mobType = SelectMobTypeForBiome(spawnPoint.BiomeType);
                if (mobType == MobType.None)
                    continue;
                
                // Spawn the mob
                var spawnRequest = new MobSpawnRequest
                {
                    MobType = mobType,
                    Position = spawnPoint.Position,
                    SpawnPoint = spawnPoint,
                    IsNaturalSpawn = true
                };
                
                if (TrySpawnMob(spawnRequest))
                {
                    spawnPoint.LastSpawnTime = DateTime.UtcNow;
                    spawnPoint.SpawnCount++;
                    spawnedCount++;
                }
            }
        }
        
        /// <summary>
        /// Cleans up inactive spawn points
        /// </summary>
        private void CleanupInactiveSpawnPoints()
        {
            var cutoffTime = DateTime.UtcNow - TimeSpan.FromHours(1);
            var inactivePoints = spawnPoints.Values.Where(sp => sp.LastSpawnTime < cutoffTime).ToList();
            
            foreach (var point in inactivePoints)
            {
                point.IsActive = false;
            }
            
            if (inactivePoints.Count > 0)
            {
                logger.LogDebug("[MobSpawningSystem] Deactivated {Count} inactive spawn points", inactivePoints.Count);
            }
        }
        
        /// <summary>
        /// Checks if a mob can spawn at a point
        /// </summary>
        private bool CanSpawnAtPoint(MobSpawnPoint spawnPoint)
        {
            // Check if too many mobs nearby
            var nearbyMobs = GetMobsInRadius(spawnPoint.Position, spawningConfig.MinSpawnDistance);
            if (nearbyMobs.Count >= spawningConfig.MaxMobsPerSpawnPoint)
                return false;
            
            // Check player proximity
            if (IsPlayerNearby(spawnPoint.Position, spawningConfig.MinPlayerDistance))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Gets mobs within a radius
        /// </summary>
        private List<Mob> GetMobsInRadius(Vector3 center, float radius)
        {
            return activeMobs.Values.Where(mob => Vector3.Distance(mob.Position, center) <= radius).ToList();
        }
        
        /// <summary>
        /// Checks if a player is nearby
        /// </summary>
        private bool IsPlayerNearby(Vector3 position, float radius)
        {
            // This would integrate with player system
            // For now, return false (no players nearby)
            return false;
        }
        
        /// <summary>
        /// Selects mob type for a biome
        /// </summary>
        private MobType SelectMobTypeForBiome(BiomeType biomeType)
        {
            var biomeSpawnRules = spawningConfig.GetBiomeSpawnRules(biomeType);
            if (biomeSpawnRules?.MobTypes == null || biomeSpawnRules.MobTypes.Count == 0)
                return MobType.None;
            
            // Select random mob type based on weights
            var totalWeight = biomeSpawnRules.MobTypes.Values.Sum(mt => mt.Weight);
            var randomValue = random.NextDouble() * totalWeight;
            
            var currentWeight = 0.0;
            foreach (var mobTypeEntry in biomeSpawnRules.MobTypes)
            {
                currentWeight += mobTypeEntry.Value.Weight;
                if (randomValue <= currentWeight)
                {
                    return mobTypeEntry.Key;
                }
            }
            
            return biomeSpawnRules.MobTypes.Keys.First();
        }
        
        /// <summary>
        /// Tries to spawn a mob
        /// </summary>
        private bool TrySpawnMob(MobSpawnRequest request)
        {
            try
            {
                var mobDefinition = spawningConfig.GetMobDefinition(request.MobType);
                if (mobDefinition == null)
                {
                    logger.LogWarning("[MobSpawningSystem] No definition found for mob type: {MobType}", request.MobType);
                    return false;
                }
                
                var mob = new Mob
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = request.MobType,
                    Position = request.Position,
                    Health = mobDefinition.MaxHealth,
                    MaxHealth = mobDefinition.MaxHealth,
                    IsAlive = true,
                    SpawnTime = DateTime.UtcNow,
                    LastUpdateTime = DateTime.UtcNow,
                    SpawnPoint = request.SpawnPoint,
                    IsNaturalSpawn = request.IsNaturalSpawn
                };
                
                // Apply initial behaviors
                ApplyInitialBehaviors(mob, mobDefinition);
                
                activeMobs[mob.Id] = mob;
                
                logger.LogDebug("[MobSpawningSystem] Spawned {MobType} at {Position}", 
                    request.MobType, request.Position);
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[MobSpawningSystem] Error spawning mob: {MobType}", request.MobType);
                return false;
            }
        }
        
        /// <summary>
        /// Applies initial behaviors to a spawned mob
        /// </summary>
        private void ApplyInitialBehaviors(Mob mob, MobDefinition mobDefinition)
        {
            // Set random initial behaviors
            mob.CurrentBehavior = SelectRandomBehavior(mobDefinition.Behaviors);
            mob.BehaviorTimer = 0f;
            
            // Set initial movement
            if (mobDefinition.Behaviors.TryGetValue(mob.CurrentBehavior, out var behavior))
            {
                mob.MovementSpeed = behavior.MovementSpeed;
                mob.TurnSpeed = behavior.TurnSpeed;
            }
        }
        
        /// <summary>
        /// Selects a random behavior
        /// </summary>
        private string SelectRandomBehavior(Dictionary<string, MobBehavior> behaviors)
        {
            if (behaviors == null || behaviors.Count == 0)
                return "idle";
            
            var behaviorList = behaviors.Values.ToList();
            return behaviorList[random.Next(behaviorList.Count)].Name;
        }
        
        /// <summary>
        /// Despawns a mob
        /// </summary>
        private void DespawnMob(Mob mob)
        {
            // Drop loot if configured
            if (mob.DropLootOnDeath)
            {
                DropLoot(mob);
            }
            
            // Remove from spawn point if this was a natural spawn
            if (mob.IsNaturalSpawn && mob.SpawnPoint != null)
            {
                mob.SpawnPoint.LastSpawnTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Drops loot for a mob
        /// </summary>
        private void DropLoot(Mob mob)
        {
            var mobDefinition = spawningConfig.GetMobDefinition(mob.Type);
            if (mobDefinition?.LootTable == null)
                return;
            
            // Select random loot
            var totalWeight = mobDefinition.LootTable.Values.Sum(item => item.Weight);
            var randomValue = random.NextDouble() * totalWeight;
            
            var currentWeight = 0.0;
            foreach (var lootEntry in mobDefinition.LootTable)
            {
                currentWeight += lootEntry.Value.Weight;
                if (randomValue <= currentWeight)
                {
                    // Create loot item at mob position
                    CreateLootItem(mob.Position, lootEntry.Value);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Creates a loot item in the world
        /// </summary>
        private void CreateLootItem(Vector3 position, LootItem lootItem)
        {
            // This would integrate with item system
            // For now, just log the loot drop
            logger.LogDebug("[MobSpawningSystem] Dropped loot {ItemType} x{Count} at {Position}", 
                lootItem.ItemType, lootItem.Count, position);
        }
        
        /// <summary>
        /// Queues a mob spawn request
        /// </summary>
        public void QueueSpawn(MobType mobType, Vector3 position, bool isNaturalSpawn = false)
        {
            var request = new MobSpawnRequest
            {
                MobType = mobType,
                Position = position,
                IsNaturalSpawn = isNaturalSpawn
            };
            
            spawnQueue.Enqueue(request);
        }
        
        /// <summary>
        /// Queues a mob despawn request
        /// </summary>
        public void QueueDespawn(string mobId)
        {
            despawnQueue.Enqueue(mobId);
        }
        
        /// <summary>
        /// Updates mob behavior and position
        /// </summary>
        public void UpdateMob(string mobId, Vector3 position, string behavior, float behaviorTimer)
        {
            if (activeMobs.TryGetValue(mobId, out var mob))
            {
                mob.Position = position;
                mob.CurrentBehavior = behavior;
                mob.BehaviorTimer = behaviorTimer;
                mob.LastUpdateTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Gets all active mobs
        /// </summary>
        public List<Mob> GetActiveMobs()
        {
            return activeMobs.Values.ToList();
        }
        
        /// <summary>
        /// Gets mobs within a radius
        /// </summary>
        public List<Mob> GetMobsInRadius(Vector3 center, float radius)
        {
            return GetMobsInRadius(center, radius);
        }
        
        /// <summary>
        /// Gets spawning statistics
        /// </summary>
        public MobSpawningStatistics GetStatistics()
        {
            lock (lockObject)
            {
                return new MobSpawningStatistics
                {
                    TotalActiveMobs = activeMobs.Count,
                    TotalSpawnPoints = spawnPoints.Count,
                    ActiveSpawnPoints = spawnPoints.Values.Count(sp => sp.IsActive),
                    SpawnedMobsPerCycle = spawnedMobsPerCycle,
                    DespawnedMobsPerCycle = despawnedMobsPerCycle,
                    LastSpawnCycle = lastSpawnCycle,
                    QueuedSpawns = spawnQueue.Count,
                    QueuedDespawns = despawnQueue.Count
                };
            }
        }
        
        /// <summary>
        /// Shuffles a list
        /// </summary>
        private void Shuffle<T>(IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        
        /// <summary>
        /// Disposes the spawning system
        /// </summary>
        public void Dispose()
        {
            spawnTimer?.Dispose();
            
            lock (lockObject)
            {
                activeMobs.Clear();
                spawnPoints.Clear();
                spawnQueue.Clear();
                despawnQueue.Clear();
            }
            
            logger.LogInformation("[MobSpawningSystem] Disposed");
        }
    }
    
    /// <summary>
    /// Mob data structure
    /// </summary>
    public class Mob
    {
        public string Id { get; set; } = string.Empty;
        public MobType Type { get; set; }
        public Vector3 Position { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public bool IsAlive { get; set; }
        public DateTime SpawnTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string CurrentBehavior { get; set; } = "idle";
        public float BehaviorTimer { get; set; }
        public float MovementSpeed { get; set; }
        public float TurnSpeed { get; set; }
        public MobSpawnPoint SpawnPoint { get; set; }
        public bool IsNaturalSpawn { get; set; }
        public bool DropLootOnDeath { get; set; } = true;
    }
    
    /// <summary>
    /// Spawn point data
    /// </summary>
    public class MobSpawnPoint
    {
        public string Id { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public BiomeType BiomeType { get; set; }
        public DateTime LastSpawnTime { get; set; }
        public int SpawnCount { get; set; }
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Spawn request data
    /// </summary>
    internal struct MobSpawnRequest
    {
        public MobType MobType;
        public Vector3 Position;
        public MobSpawnPoint SpawnPoint;
        public bool IsNaturalSpawn;
    }
    
    /// <summary>
    /// Spawning statistics
    /// </summary>
    public class MobSpawningStatistics
    {
        public int TotalActiveMobs { get; set; }
        public int TotalSpawnPoints { get; set; }
        public int ActiveSpawnPoints { get; set; }
        public int SpawnedMobsPerCycle { get; set; }
        public int DespawnedMobsPerCycle { get; set; }
        public DateTime LastSpawnCycle { get; set; }
        public int QueuedSpawns { get; set; }
        public int QueuedDespawns { get; set; }
    }
    
    /// <summary>
    /// 3D integer vector
    /// </summary>
    public struct Vector3Int
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        
        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
    
    /// <summary>
    /// 3D float vector
    /// </summary>
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }
}using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World.Spawning
{
    /// <summary>
    /// Basic mob spawning system that manages creature generation and lifecycle
    /// Supports various spawn conditions, mob types, and spawn behaviors
    /// </summary>
    public class MobSpawningSystem
    {
        private readonly ILogger<MobSpawningSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly MobSpawningConfig spawningConfig;
        
        // Mob storage
        private readonly ConcurrentDictionary<string, Mob> activeMobs;
        private readonly ConcurrentDictionary<string, MobSpawnPoint> spawnPoints;
        
        // Spawn management
        private readonly Timer spawnTimer;
        private readonly Random random;
        private readonly object lockObject = new object();
        
        // Performance tracking
        private int spawnedMobsPerCycle;
        private int despawnedMobsPerCycle;
        private DateTime lastSpawnCycle;
        
        // Spawn queues
        private readonly ConcurrentQueue<MobSpawnRequest> spawnQueue;
        private readonly ConcurrentQueue<string> despawnQueue;
        
        public MobSpawningSystem(ILogger<MobSpawningSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.spawningConfig = config.World.MobSpawning ?? new MobSpawningConfig();
            
            activeMobs = new ConcurrentDictionary<string, Mob>();
            spawnPoints = new ConcurrentDictionary<string, MobSpawnPoint>();
            spawnQueue = new ConcurrentQueue<MobSpawnRequest>();
            despawnQueue = new ConcurrentQueue<string>();
            
            random = new Random(config.World.WorldSeed);
            
            // Initialize spawn timer
            spawnTimer = new Timer(ProcessSpawnCycle, null, 
                TimeSpan.Zero, 
                TimeSpan.FromMilliseconds(spawningConfig.SpawnIntervalMs));
            
            lastSpawnCycle = DateTime.UtcNow;
            
            logger.LogInformation("[MobSpawningSystem] Initialized with spawn interval: {Interval}ms", 
                spawningConfig.SpawnIntervalMs);
        }
        
        /// <summary>
        /// Initializes spawn points for a chunk
        /// </summary>
        public void InitializeSpawnPoints(int chunkX, int chunkZ, ChunkData chunkData, BiomeData biomeData)
        {
            var chunkPos = new Vector3Int(chunkX, 0, chunkZ);
            
            // Generate spawn points based on biome
            for (int x = 0; x < chunkData.Size; x += 4) // Sample every 4 blocks
            {
                for (int z = 0; z < chunkData.Size; z += 4)
                {
                    var worldX = chunkX * chunkData.Size + x;
                    var worldZ = chunkZ * chunkData.Size + z;
                    var worldPos = new Vector3(worldX, chunkData.HeightMap[x, z], worldZ);
                    
                    // Check if this is a valid spawn location
                    if (IsValidSpawnLocation(worldPos, chunkData, biomeData, x, z))
                    {
                        var biomeType = biomeData.BiomeMap[x, z];
                        var spawnPoint = CreateSpawnPoint(worldPos, biomeType);
                        
                        spawnPoints[spawnPoint.Id] = spawnPoint;
                    }
                }
            }
            
            logger.LogDebug("[MobSpawningSystem] Initialized spawn points for chunk ({ChunkX}, {ChunkZ})", 
                chunkX, chunkZ);
        }
        
        /// <summary>
        /// Checks if a location is valid for spawning
        /// </summary>
        private bool IsValidSpawnLocation(Vector3 position, ChunkData chunkData, BiomeData biomeData, int localX, int localZ)
        {
            // Check if position is on solid ground
            var heightValue = chunkData.HeightMap[localX, localZ];
            if (position.Y < heightValue - 1 || position.Y > heightValue + 2)
                return false;
            
            // Check if position is not in water (unless water mob)
            var isInWater = position.Y < config.World.SeaLevel;
            
            // Check if position has enough space
            if (!HasEnoughSpace(position, 2f))
                return false;
            
            // Check light level
            var lightLevel = CalculateLightLevel(position, chunkData);
            if (lightLevel < spawningConfig.MinLightLevel)
                return false;
            
            // Check biome spawn rules
            var biomeType = biomeData.BiomeMap[localX, localZ];
            if (!IsBiomeValidForSpawning(biomeType, isInWater))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Checks if there's enough space for spawning
        /// </summary>
        private bool HasEnoughSpace(Vector3 position, float radius)
        {
            // Check for nearby mobs
            var nearbyMobs = GetMobsInRadius(position, radius);
            return nearbyMobs.Count < spawningConfig.MaxMobsPerArea;
        }
        
        /// <summary>
        /// Calculates light level at a position
        /// </summary>
        private float CalculateLightLevel(Vector3 position, ChunkData chunkData)
        {
            // Simplified light calculation based on height and time of day
            var baseLight = 0.8f; // Assume daylight for now
            var heightFactor = Math.Max(0, 1f - (position.Y / 100f)); // Less light underground
            
            return baseLight * heightFactor;
        }
        
        /// <summary>
        /// Checks if a biome is valid for spawning
        /// </summary>
        private bool IsBiomeValidForSpawning(BiomeType biomeType, bool isInWater)
        {
            var biomeSpawnRules = spawningConfig.GetBiomeSpawnRules(biomeType);
            if (biomeSpawnRules == null) return true;
            
            return isInWater ? biomeSpawnRules.AllowWaterSpawns : biomeSpawnRules.AllowLandSpawns;
        }
        
        /// <summary>
        /// Creates a spawn point
        /// </summary>
        private MobSpawnPoint CreateSpawnPoint(Vector3 position, BiomeType biomeType)
        {
            return new MobSpawnPoint
            {
                Id = Guid.NewGuid().ToString(),
                Position = position,
                BiomeType = biomeType,
                LastSpawnTime = DateTime.MinValue,
                SpawnCount = 0,
                IsActive = true
            };
        }
        
        /// <summary>
        /// Processes the spawn cycle
        /// </summary>
        private void ProcessSpawnCycle(object state)
        {
            spawnedMobsPerCycle = 0;
            despawnedMobsPerCycle = 0;
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Process despawns first
                ProcessDespawns();
                
                // Process spawn queue
                ProcessSpawnQueue();
                
                // Natural spawning
                ProcessNaturalSpawning();
                
                // Clean up inactive spawn points
                CleanupInactiveSpawnPoints();
                
                // Update performance metrics
                var cycleTime = DateTime.UtcNow - startTime;
                lastSpawnCycle = DateTime.UtcNow;
                
                logger.LogDebug("[MobSpawningSystem] Spawn cycle completed in {Time}ms - Spawned: {Spawned}, Despawned: {Despawned}", 
                    cycleTime.TotalMilliseconds, spawnedMobsPerCycle, despawnedMobsPerCycle);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[MobSpawningSystem] Error during spawn cycle");
            }
        }
        
        /// <summary>
        /// Processes despawn queue
        /// </summary>
        private void ProcessDespawns()
        {
            var processedCount = 0;
            
            while (despawnQueue.TryDequeue(out var mobId) && processedCount < spawningConfig.MaxDespawnsPerCycle)
            {
                if (activeMobs.TryRemove(mobId, out var mob))
                {
                    DespawnMob(mob);
                    despawnedMobsPerCycle++;
                    processedCount++;
                }
            }
        }
        
        /// <summary>
        /// Processes spawn queue
        /// </summary>
        private void ProcessSpawnQueue()
        {
            var processedCount = 0;
            
            while (spawnQueue.TryDequeue(out var request) && processedCount < spawningConfig.MaxSpawnsPerCycle)
            {
                if (TrySpawnMob(request))
                {
                    spawnedMobsPerCycle++;
                    processedCount++;
                }
            }
        }
        
        /// <summary>
        /// Processes natural spawning
        /// </summary>
        private void ProcessNaturalSpawning()
        {
            var activeSpawnPointsList = spawnPoints.Values.Where(sp => sp.IsActive).ToList();
            
            // Shuffle spawn points for randomness
            Shuffle(activeSpawnPointsList);
            
            var spawnedCount = 0;
            
            foreach (var spawnPoint in activeSpawnPointsList)
            {
                if (spawnedCount >= spawningConfig.MaxNaturalSpawnsPerCycle)
                    break;
                
                // Check if enough time has passed since last spawn
                var timeSinceLastSpawn = DateTime.UtcNow - spawnPoint.LastSpawnTime;
                if (timeSinceLastSpawn < TimeSpan.FromSeconds(spawningConfig.SpawnCooldownSeconds))
                    continue;
                
                // Check spawn conditions
                if (!CanSpawnAtPoint(spawnPoint))
                    continue;
                
                // Select mob type based on biome
                var mobType = SelectMobTypeForBiome(spawnPoint.BiomeType);
                if (mobType == MobType.None)
                    continue;
                
                // Spawn the mob
                var spawnRequest = new MobSpawnRequest
                {
                    MobType = mobType,
                    Position = spawnPoint.Position,
                    SpawnPoint = spawnPoint,
                    IsNaturalSpawn = true
                };
                
                if (TrySpawnMob(spawnRequest))
                {
                    spawnPoint.LastSpawnTime = DateTime.UtcNow;
                    spawnPoint.SpawnCount++;
                    spawnedCount++;
                }
            }
        }
        
        /// <summary>
        /// Cleans up inactive spawn points
        /// </summary>
        private void CleanupInactiveSpawnPoints()
        {
            var cutoffTime = DateTime.UtcNow - TimeSpan.FromHours(1);
            var inactivePoints = spawnPoints.Values.Where(sp => sp.LastSpawnTime < cutoffTime).ToList();
            
            foreach (var point in inactivePoints)
            {
                point.IsActive = false;
            }
            
            if (inactivePoints.Count > 0)
            {
                logger.LogDebug("[MobSpawningSystem] Deactivated {Count} inactive spawn points", inactivePoints.Count);
            }
        }
        
        /// <summary>
        /// Checks if a mob can spawn at a point
        /// </summary>
        private bool CanSpawnAtPoint(MobSpawnPoint spawnPoint)
        {
            // Check if too many mobs nearby
            var nearbyMobs = GetMobsInRadius(spawnPoint.Position, spawningConfig.MinSpawnDistance);
            if (nearbyMobs.Count >= spawningConfig.MaxMobsPerSpawnPoint)
                return false;
            
            // Check player proximity
            if (IsPlayerNearby(spawnPoint.Position, spawningConfig.MinPlayerDistance))
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Gets mobs within a radius
        /// </summary>
        private List<Mob> GetMobsInRadius(Vector3 center, float radius)
        {
            return activeMobs.Values.Where(mob => Vector3.Distance(mob.Position, center) <= radius).ToList();
        }
        
        /// <summary>
        /// Checks if a player is nearby
        /// </summary>
        private bool IsPlayerNearby(Vector3 position, float radius)
        {
            // This would integrate with player system
            // For now, return false (no players nearby)
            return false;
        }
        
        /// <summary>
        /// Selects mob type for a biome
        /// </summary>
        private MobType SelectMobTypeForBiome(BiomeType biomeType)
        {
            var biomeSpawnRules = spawningConfig.GetBiomeSpawnRules(biomeType);
            if (biomeSpawnRules?.MobTypes == null || biomeSpawnRules.MobTypes.Count == 0)
                return MobType.None;
            
            // Select random mob type based on weights
            var totalWeight = biomeSpawnRules.MobTypes.Values.Sum(mt => mt.Weight);
            var randomValue = random.NextDouble() * totalWeight;
            
            var currentWeight = 0.0;
            foreach (var mobTypeEntry in biomeSpawnRules.MobTypes)
            {
                currentWeight += mobTypeEntry.Value.Weight;
                if (randomValue <= currentWeight)
                {
                    return mobTypeEntry.Key;
                }
            }
            
            return biomeSpawnRules.MobTypes.Keys.First();
        }
        
        /// <summary>
        /// Tries to spawn a mob
        /// </summary>
        private bool TrySpawnMob(MobSpawnRequest request)
        {
            try
            {
                var mobDefinition = spawningConfig.GetMobDefinition(request.MobType);
                if (mobDefinition == null)
                {
                    logger.LogWarning("[MobSpawningSystem] No definition found for mob type: {MobType}", request.MobType);
                    return false;
                }
                
                var mob = new Mob
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = request.MobType,
                    Position = request.Position,
                    Health = mobDefinition.MaxHealth,
                    MaxHealth = mobDefinition.MaxHealth,
                    IsAlive = true,
                    SpawnTime = DateTime.UtcNow,
                    LastUpdateTime = DateTime.UtcNow,
                    SpawnPoint = request.SpawnPoint,
                    IsNaturalSpawn = request.IsNaturalSpawn
                };
                
                // Apply initial behaviors
                ApplyInitialBehaviors(mob, mobDefinition);
                
                activeMobs[mob.Id] = mob;
                
                logger.LogDebug("[MobSpawningSystem] Spawned {MobType} at {Position}", 
                    request.MobType, request.Position);
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[MobSpawningSystem] Error spawning mob: {MobType}", request.MobType);
                return false;
            }
        }
        
        /// <summary>
        /// Applies initial behaviors to a spawned mob
        /// </summary>
        private void ApplyInitialBehaviors(Mob mob, MobDefinition mobDefinition)
        {
            // Set random initial behaviors
            mob.CurrentBehavior = SelectRandomBehavior(mobDefinition.Behaviors);
            mob.BehaviorTimer = 0f;
            
            // Set initial movement
            if (mobDefinition.Behaviors.TryGetValue(mob.CurrentBehavior, out var behavior))
            {
                mob.MovementSpeed = behavior.MovementSpeed;
                mob.TurnSpeed = behavior.TurnSpeed;
            }
        }
        
        /// <summary>
        /// Selects a random behavior
        /// </summary>
        private string SelectRandomBehavior(Dictionary<string, MobBehavior> behaviors)
        {
            if (behaviors == null || behaviors.Count == 0)
                return "idle";
            
            var behaviorList = behaviors.Values.ToList();
            return behaviorList[random.Next(behaviorList.Count)].Name;
        }
        
        /// <summary>
        /// Despawns a mob
        /// </summary>
        private void DespawnMob(Mob mob)
        {
            // Drop loot if configured
            if (mob.DropLootOnDeath)
            {
                DropLoot(mob);
            }
            
            // Remove from spawn point if this was a natural spawn
            if (mob.IsNaturalSpawn && mob.SpawnPoint != null)
            {
                mob.SpawnPoint.LastSpawnTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Drops loot for a mob
        /// </summary>
        private void DropLoot(Mob mob)
        {
            var mobDefinition = spawningConfig.GetMobDefinition(mob.Type);
            if (mobDefinition?.LootTable == null)
                return;
            
            // Select random loot
            var totalWeight = mobDefinition.LootTable.Values.Sum(item => item.Weight);
            var randomValue = random.NextDouble() * totalWeight;
            
            var currentWeight = 0.0;
            foreach (var lootEntry in mobDefinition.LootTable)
            {
                currentWeight += lootEntry.Value.Weight;
                if (randomValue <= currentWeight)
                {
                    // Create loot item at mob position
                    CreateLootItem(mob.Position, lootEntry.Value);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Creates a loot item in the world
        /// </summary>
        private void CreateLootItem(Vector3 position, LootItem lootItem)
        {
            // This would integrate with item system
            // For now, just log the loot drop
            logger.LogDebug("[MobSpawningSystem] Dropped loot {ItemType} x{Count} at {Position}", 
                lootItem.ItemType, lootItem.Count, position);
        }
        
        /// <summary>
        /// Queues a mob spawn request
        /// </summary>
        public void QueueSpawn(MobType mobType, Vector3 position, bool isNaturalSpawn = false)
        {
            var request = new MobSpawnRequest
            {
                MobType = mobType,
                Position = position,
                IsNaturalSpawn = isNaturalSpawn
            };
            
            spawnQueue.Enqueue(request);
        }
        
        /// <summary>
        /// Queues a mob despawn request
        /// </summary>
        public void QueueDespawn(string mobId)
        {
            despawnQueue.Enqueue(mobId);
        }
        
        /// <summary>
        /// Updates mob behavior and position
        /// </summary>
        public void UpdateMob(string mobId, Vector3 position, string behavior, float behaviorTimer)
        {
            if (activeMobs.TryGetValue(mobId, out var mob))
            {
                mob.Position = position;
                mob.CurrentBehavior = behavior;
                mob.BehaviorTimer = behaviorTimer;
                mob.LastUpdateTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Gets all active mobs
        /// </summary>
        public List<Mob> GetActiveMobs()
        {
            return activeMobs.Values.ToList();
        }
        
        /// <summary>
        /// Gets mobs within a radius
        /// </summary>
        public List<Mob> GetMobsInRadius(Vector3 center, float radius)
        {
            return GetMobsInRadius(center, radius);
        }
        
        /// <summary>
        /// Gets spawning statistics
        /// </summary>
        public MobSpawningStatistics GetStatistics()
        {
            lock (lockObject)
            {
                return new MobSpawningStatistics
                {
                    TotalActiveMobs = activeMobs.Count,
                    TotalSpawnPoints = spawnPoints.Count,
                    ActiveSpawnPoints = spawnPoints.Values.Count(sp => sp.IsActive),
                    SpawnedMobsPerCycle = spawnedMobsPerCycle,
                    DespawnedMobsPerCycle = despawnedMobsPerCycle,
                    LastSpawnCycle = lastSpawnCycle,
                    QueuedSpawns = spawnQueue.Count,
                    QueuedDespawns = despawnQueue.Count
                };
            }
        }
        
        /// <summary>
        /// Shuffles a list
        /// </summary>
        private void Shuffle<T>(IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        
        /// <summary>
        /// Disposes the spawning system
        /// </summary>
        public void Dispose()
        {
            spawnTimer?.Dispose();
            
            lock (lockObject)
            {
                activeMobs.Clear();
                spawnPoints.Clear();
                spawnQueue.Clear();
                despawnQueue.Clear();
            }
            
            logger.LogInformation("[MobSpawningSystem] Disposed");
        }
    }
    
    /// <summary>
    /// Mob data structure
    /// </summary>
    public class Mob
    {
        public string Id { get; set; } = string.Empty;
        public MobType Type { get; set; }
        public Vector3 Position { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public bool IsAlive { get; set; }
        public DateTime SpawnTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string CurrentBehavior { get; set; } = "idle";
        public float BehaviorTimer { get; set; }
        public float MovementSpeed { get; set; }
        public float TurnSpeed { get; set; }
        public MobSpawnPoint SpawnPoint { get; set; }
        public bool IsNaturalSpawn { get; set; }
        public bool DropLootOnDeath { get; set; } = true;
    }
    
    /// <summary>
    /// Spawn point data
    /// </summary>
    public class MobSpawnPoint
    {
        public string Id { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public BiomeType BiomeType { get; set; }
        public DateTime LastSpawnTime { get; set; }
        public int SpawnCount { get; set; }
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Spawn request data
    /// </summary>
    internal struct MobSpawnRequest
    {
        public MobType MobType;
        public Vector3 Position;
        public MobSpawnPoint SpawnPoint;
        public bool IsNaturalSpawn;
    }
    
    /// <summary>
    /// Spawning statistics
    /// </summary>
    public class MobSpawningStatistics
    {
        public int TotalActiveMobs { get; set; }
        public int TotalSpawnPoints { get; set; }
        public int ActiveSpawnPoints { get; set; }
        public int SpawnedMobsPerCycle { get; set; }
        public int DespawnedMobsPerCycle { get; set; }
        public DateTime LastSpawnCycle { get; set; }
        public int QueuedSpawns { get; set; }
        public int QueuedDespawns { get; set; }
    }
    
    /// <summary>
    /// 3D integer vector
    /// </summary>
    public struct Vector3Int
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        
        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
    
    /// <summary>
    /// 3D float vector
    /// </summary>
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }
}
