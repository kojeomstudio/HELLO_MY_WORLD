#if false
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World.Physics
{
    /// <summary>
    /// Entity collision system that handles entity-terrain and entity-entity collisions
    /// Supports various collision shapes and response types
    /// </summary>
    public class EntityCollisionSystem
    {
        private readonly ILogger<EntityCollisionSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly EntityCollisionConfig collisionConfig;
        
        // Entity storage
        private readonly ConcurrentDictionary<string, Entity> entities;
        private readonly ConcurrentDictionary<Vector3Int, ChunkCollisionData> chunkCollisionData;
        
        // Spatial partitioning for performance
        private readonly SpatialGrid spatialGrid;
        
        // Performance tracking
        private int processedCollisionsPerTick;
        private DateTime lastUpdateTime;
        
        // Thread synchronization
        private readonly object lockObject = new object();
        private volatile bool isProcessing;
        
        public EntityCollisionSystem(ILogger<EntityCollisionSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.collisionConfig = new EntityCollisionConfig();
            
            entities = new ConcurrentDictionary<string, Entity>();
            chunkCollisionData = new ConcurrentDictionary<Vector3Int, ChunkCollisionData>();
            
            // Initialize spatial grid
            var worldSize = config.ChunkSize * Math.Max(config.RenderDistance, config.SimulationDistance) * 2;
            spatialGrid = new SpatialGrid(worldSize, collisionConfig.GridCellSize);
            
            lastUpdateTime = DateTime.UtcNow;
            
            logger.LogInformation("[EntityCollisionSystem] Initialized with grid cell size: {Size}", 
                collisionConfig.GridCellSize);
        }
        
        /// <summary>
        /// Registers an entity for collision detection
        /// </summary>
        public void RegisterEntity(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            
            entities[entity.Id] = entity;
            spatialGrid.AddEntity(entity);
            
            logger.LogDebug("[EntityCollisionSystem] Registered entity {EntityId} at {Position}", 
                entity.Id, entity.Position);
        }
        
        /// <summary>
        /// Unregisters an entity from collision detection
        /// </summary>
        public void UnregisterEntity(string entityId)
        {
            if (entities.TryRemove(entityId, out var entity))
            {
                spatialGrid.RemoveEntity(entity);
                logger.LogDebug("[EntityCollisionSystem] Unregistered entity {EntityId}", entityId);
            }
        }
        
        /// <summary>
        /// Updates entity position and velocity
        /// </summary>
        public void UpdateEntity(string entityId, Vector3 newPosition, Vector3 newVelocity)
        {
            if (entities.TryGetValue(entityId, out var entity))
            {
                var oldPosition = entity.Position;
                entity.Position = newPosition;
                entity.Velocity = newVelocity;
                
                // Update spatial grid if position changed significantly
                if (Vector3.Distance(oldPosition, newPosition) > collisionConfig.GridCellSize)
                {
                    spatialGrid.UpdateEntity(entity);
                }
            }
        }
        
        /// <summary>
        /// Initializes collision data for a chunk
        /// </summary>
        public void InitializeChunk(int chunkX, int chunkZ, ChunkData chunkData)
        {
            var chunkPos = new Vector3Int(chunkX, 0, chunkZ);
            
            if (chunkCollisionData.ContainsKey(chunkPos)) return;
            
            var collisionData = new ChunkCollisionData
            {
                ChunkPosition = chunkPos,
                HeightMap = chunkData.HeightMap,
                SolidBlocks = GenerateSolidBlockMap(chunkData),
                CollisionShapes = GenerateCollisionShapes(chunkData)
            };
            
            chunkCollisionData[chunkPos] = collisionData;
            
            logger.LogDebug("[EntityCollisionSystem] Initialized collision data for chunk ({ChunkX}, {ChunkZ})", 
                chunkX, chunkZ);
        }
        
        /// <summary>
        /// Generates solid block map from chunk data
        /// </summary>
        private bool[,] GenerateSolidBlockMap(ChunkData chunkData)
        {
            var size = chunkData.Size;
            var solidMap = new bool[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var height = (int)chunkData.HeightMap[x, z];
                    solidMap[x, z] = height > 0; // Simplified - any non-zero height is solid
                }
            }
            
            return solidMap;
        }
        
        /// <summary>
        /// Generates collision shapes from chunk data
        /// </summary>
        private List<CollisionShape> GenerateCollisionShapes(ChunkData chunkData)
        {
            var shapes = new List<CollisionShape>();
            var size = chunkData.Size;
            
            // Generate simplified collision shapes for terrain features
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    var worldX = chunkData.ChunkX * size + x;
                    var worldZ = chunkData.ChunkZ * size + z;
                    var height = chunkData.HeightMap[x, z];
                    
                    // Create box collision shape for solid terrain
                    if (height > 0)
                    {
                        var shape = new CollisionShape
                        {
                            Type = CollisionShapeType.Box,
                            Position = new Vector3(worldX, height, worldZ),
                            Size = new Vector3(1, height, 1),
                            IsStatic = true,
                            IsTerrain = true
                        };
                        shapes.Add(shape);
                    }
                }
            }
            
            return shapes;
        }
        
        /// <summary>
        /// Processes collision detection for all entities
        /// </summary>
        public void ProcessCollisions()
        {
            if (isProcessing) return;
            
            isProcessing = true;
            processedCollisionsPerTick = 0;
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Process entity-terrain collisions
                ProcessEntityTerrainCollisions();
                
                // Process entity-entity collisions
                ProcessEntityEntityCollisions();
                
                // Update performance metrics
                var updateTime = DateTime.UtcNow - startTime;
                lastUpdateTime = DateTime.UtcNow;
                
                if (updateTime.TotalMilliseconds > collisionConfig.MaxUpdateTimeMs)
                {
                    logger.LogWarning("[EntityCollisionSystem] Collision update took {Time}ms (target: {Target}ms)", 
                        updateTime.TotalMilliseconds, collisionConfig.MaxUpdateTimeMs);
                }
                
                logger.LogDebug("[EntityCollisionSystem] Processed {Count} collisions in {Time}ms", 
                    processedCollisionsPerTick, updateTime.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[EntityCollisionSystem] Error during collision processing");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Processes entity-terrain collisions
        /// </summary>
        private void ProcessEntityTerrainCollisions()
        {
            var entitiesToCheck = entities.Values.Where(e => e.IsCollisionEnabled).ToList();
            
            foreach (var entity in entitiesToCheck)
            {
                // Get terrain collision data at entity position
                var terrainCollision = GetTerrainCollisionAt(entity.Position);
                
                if (terrainCollision != null)
                {
                    // Check collision with terrain
                    var collision = CheckEntityTerrainCollision(entity, terrainCollision);
                    
                    if (collision != null)
                    {
                        ProcessCollisionResponse(entity, collision);
                        processedCollisionsPerTick++;
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets terrain collision data at a position
        /// </summary>
        private TerrainCollision GetTerrainCollisionAt(Vector3 position)
        {
            var chunkX = (int)Math.Floor(position.X / config.ChunkSize);
            var chunkZ = (int)Math.Floor(position.Z / config.ChunkSize);
            var chunkPos = new Vector3Int(chunkX, 0, chunkZ);
            
            if (!chunkCollisionData.TryGetValue(chunkPos, out var collisionData))
            {
                return null;
            }
            
            // Get local position within chunk
            var localX = (int)Math.Floor(position.X) - chunkX * config.ChunkSize;
            var localZ = (int)Math.Floor(position.Z) - chunkZ * config.ChunkSize;
            
            // Check bounds
            if (localX < 0 || localX >= config.ChunkSize || 
                localZ < 0 || localZ >= config.ChunkSize)
            {
                return null;
            }
            
            // Get height at position
            var terrainHeight = collisionData.HeightMap[localX, localZ];
            var isSolid = collisionData.SolidBlocks[localX, localZ];
            
            return new TerrainCollision
            {
                Position = position,
                TerrainHeight = terrainHeight,
                IsSolid = isSolid,
                Normal = CalculateTerrainNormal(collisionData, localX, localZ)
            };
        }
        
        /// <summary>
        /// Calculates terrain normal at a position
        /// </summary>
        private Vector3 CalculateTerrainNormal(ChunkCollisionData collisionData, int localX, int localZ)
        {
            var size = config.ChunkSize;
            var height = collisionData.HeightMap[localX, localZ];
            
            // Sample neighboring heights
            var heightLeft = localX > 0 ? collisionData.HeightMap[localX - 1, localZ] : height;
            var heightRight = localX < size - 1 ? collisionData.HeightMap[localX + 1, localZ] : height;
            var heightUp = localZ < size - 1 ? collisionData.HeightMap[localX, localZ + 1] : height;
            var heightDown = localZ > 0 ? collisionData.HeightMap[localX, localZ - 1] : height;
            
            // Calculate normal using finite differences
            var dx = (heightRight - heightLeft) / 2f;
            var dz = (heightUp - heightDown) / 2f;
            
            return Vector3.Normalize(new Vector3(-dx, 1f, -dz));
        }
        
        /// <summary>
        /// Checks collision between entity and terrain
        /// </summary>
        private Collision CheckEntityTerrainCollision(Entity entity, TerrainCollision terrain)
        {
            // Simple sphere-terrain collision
            if (entity.CollisionShape.Type == CollisionShapeType.Sphere)
            {
                var sphere = entity.CollisionShape;
                var distanceToTerrain = entity.Position.Y - terrain.TerrainHeight;
                var penetration = sphere.Radius - distanceToTerrain;
                
                if (penetration > 0)
                {
                    return new Collision
                    {
                        Type = CollisionType.EntityTerrain,
                        Entity = entity,
                        Terrain = terrain,
                        PenetrationDepth = penetration,
                        ContactNormal = Vector3.Up,
                        ContactPoint = new Vector3(entity.Position.X, terrain.TerrainHeight, entity.Position.Z)
                    };
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Processes entity-entity collisions
        /// </summary>
        private void ProcessEntityEntityCollisions()
        {
            // Use spatial grid to find potential collisions
            var entitiesToCheck = entities.Values.Where(e => e.IsCollisionEnabled).ToList();
            
            foreach (var entity in entitiesToCheck)
            {
                // Find nearby entities using spatial grid
                var nearbyEntities = spatialGrid.GetNearbyEntities(entity, collisionConfig.MaxCollisionDistance);
                
                foreach (var other in nearbyEntities)
                {
                    if (other.Id == entity.Id || !other.IsCollisionEnabled) continue;
                    
                    // Check collision between entities
                    var collision = CheckEntityEntityCollision(entity, other);
                    
                    if (collision != null)
                    {
                        ProcessCollisionResponse(entity, collision);
                        processedCollisionsPerTick++;
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks collision between two entities
        /// </summary>
        private Collision CheckEntityEntityCollision(Entity entity1, Entity entity2)
        {
            // Simple sphere-sphere collision
            if (entity1.CollisionShape.Type == CollisionShapeType.Sphere && 
                entity2.CollisionShape.Type == CollisionShapeType.Sphere)
            {
                var sphere1 = entity1.CollisionShape;
                var sphere2 = entity2.CollisionShape;
                
                var distance = Vector3.Distance(entity1.Position, entity2.Position);
                var combinedRadius = sphere1.Radius + sphere2.Radius;
                
                if (distance < combinedRadius)
                {
                    var penetration = combinedRadius - distance;
                    var contactNormal = Vector3.Normalize(entity2.Position - entity1.Position);
                    var contactPoint = entity1.Position + contactNormal * sphere1.Radius;
                    
                    return new Collision
                    {
                        Type = CollisionType.EntityEntity,
                        Entity1 = entity1,
                        Entity2 = entity2,
                        PenetrationDepth = penetration,
                        ContactNormal = contactNormal,
                        ContactPoint = contactPoint
                    };
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Processes collision response
        /// </summary>
        private void ProcessCollisionResponse(Entity entity, Collision collision)
        {
            switch (collision.Type)
            {
                case CollisionType.EntityTerrain:
                    ProcessEntityTerrainResponse(entity, collision);
                    break;
                    
                case CollisionType.EntityEntity:
                    ProcessEntityEntityResponse(collision);
                    break;
            }
        }
        
        /// <summary>
        /// Processes entity-terrain collision response
        /// </summary>
        private void ProcessEntityTerrainResponse(Entity entity, Collision collision)
        {
            var response = entity.CollisionResponse;
            
            switch (response.Type)
            {
                case CollisionResponseType.Block:
                    // Stop movement and place entity on terrain surface
                    entity.Position = new Vector3(
                        entity.Position.X,
                        collision.Terrain.TerrainHeight + entity.CollisionShape.Radius,
                        entity.Position.Z
                    );
                    entity.Velocity = new Vector3(
                        entity.Velocity.X,
                        0,
                        entity.Velocity.Z
                    );
                    break;
                    
                case CollisionResponseType.Slide:
                    // Slide along terrain surface
                    var slideDirection = Vector3.ProjectOnPlane(entity.Velocity, collision.ContactNormal);
                    entity.Velocity = slideDirection * response.Friction;
                    break;
                    
                case CollisionResponseType.Bounce:
                    // Bounce off terrain
                    var bounceVelocity = Vector3.Reflect(entity.Velocity, collision.ContactNormal);
                    entity.Velocity = bounceVelocity * response.Restitution;
                    break;
                    
                case CollisionResponseType.Callback:
                    // Call entity-specific collision handler
                    entity.OnCollision?.Invoke(entity, collision);
                    break;
            }
            
            // Trigger collision event
            entity.OnCollisionEvent?.Invoke(entity, collision);
        }
        
        /// <summary>
        /// Processes entity-entity collision response
        /// </summary>
        private void ProcessEntityEntityResponse(Collision collision)
        {
            var entity1 = collision.Entity1;
            var entity2 = collision.Entity2;
            
            // Calculate impulse based on masses
            var totalMass = entity1.Mass + entity2.Mass;
            var impulse1 = collision.PenetrationDepth * (entity2.Mass / totalMass);
            var impulse2 = collision.PenetrationDepth * (entity1.Mass / totalMass);
            
            // Apply separation
            var separation1 = collision.ContactNormal * impulse1;
            var separation2 = -collision.ContactNormal * impulse2;
            
            entity1.Position += separation1;
            entity2.Position += separation2;
            
            // Process individual entity responses
            ProcessEntityTerrainResponse(entity1, collision);
            ProcessEntityTerrainResponse(entity2, collision);
        }
        
        /// <summary>
        /// Gets entities within a radius
        /// </summary>
        public List<Entity> GetEntitiesInRadius(Vector3 center, float radius)
        {
            return spatialGrid.GetEntitiesInRadius(center, radius);
        }
        
        /// <summary>
        /// Performs raycast against terrain
        /// </summary>
        public RaycastResult Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            var result = new RaycastResult { Hit = false };
            
            // Simple raycast implementation
            var stepSize = 0.5f;
            var currentPos = origin;
            var distance = 0f;
            
            while (distance < maxDistance)
            {
                var terrainCollision = GetTerrainCollisionAt(currentPos);
                
                if (terrainCollision != null && terrainCollision.IsSolid)
                {
                    result.Hit = true;
                    result.Position = currentPos;
                    result.Normal = terrainCollision.Normal;
                    result.Distance = distance;
                    break;
                }
                
                currentPos += direction * stepSize;
                distance += stepSize;
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets collision statistics
        /// </summary>
        public CollisionStatistics GetStatistics()
        {
            lock (lockObject)
            {
                return new CollisionStatistics
                {
                    TotalEntities = entities.Count,
                    ActiveEntities = entities.Values.Count(e => e.IsCollisionEnabled),
                    ProcessedCollisionsPerTick = processedCollisionsPerTick,
                    LastUpdateTime = lastUpdateTime,
                    SpatialGridCells = spatialGrid.GetCellCount(),
                    AverageEntitiesPerCell = spatialGrid.GetAverageEntitiesPerCell()
                };
            }
        }
        
        /// <summary>
        /// Disposes the collision system
        /// </summary>
        public void Dispose()
        {
            lock (lockObject)
            {
                entities.Clear();
                chunkCollisionData.Clear();
                spatialGrid?.Dispose();
            }
            
            logger.LogInformation("[EntityCollisionSystem] Disposed");
        }
    }
    
    /// <summary>
    /// Entity data structure
    /// </summary>
    public class Entity
    {
        public string Id { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public float Mass { get; set; } = 1.0f;
        public CollisionShape CollisionShape { get; set; }
        public CollisionResponse CollisionResponse { get; set; }
        public bool IsCollisionEnabled { get; set; } = true;
        public Action<Entity, Collision> OnCollision { get; set; }
        public Action<Entity, Collision> OnCollisionEvent { get; set; }
    }
    
    /// <summary>
    /// Collision shape definition
    /// </summary>
    public class CollisionShape
    {
        public CollisionShapeType Type { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Size { get; set; }
        public float Radius { get; set; }
        public bool IsStatic { get; set; }
        public bool IsTerrain { get; set; }
    }
    
    /// <summary>
    /// Collision response configuration
    /// </summary>
    public class CollisionResponse
    {
        public CollisionResponseType Type { get; set; }
        public float Friction { get; set; } = 0.5f;
        public float Restitution { get; set; } = 0.2f;
    }
    
    /// <summary>
    /// Collision data
    /// </summary>
    public class Collision
    {
        public CollisionType Type { get; set; }
        public Entity Entity { get; set; }
        public Entity Entity1 { get; set; }
        public Entity Entity2 { get; set; }
        public TerrainCollision Terrain { get; set; }
        public float PenetrationDepth { get; set; }
        public Vector3 ContactNormal { get; set; }
        public Vector3 ContactPoint { get; set; }
    }
    
    /// <summary>
    /// Terrain collision data
    /// </summary>
    public class TerrainCollision
    {
        public Vector3 Position { get; set; }
        public float TerrainHeight { get; set; }
        public bool IsSolid { get; set; }
        public Vector3 Normal { get; set; }
    }
    
    /// <summary>
    /// Chunk collision data
    /// </summary>
    internal class ChunkCollisionData
    {
        public Vector3Int ChunkPosition { get; set; }
        public float[,] HeightMap { get; set; }
        public bool[,] SolidBlocks { get; set; }
        public List<CollisionShape> CollisionShapes { get; set; }
    }
    
    /// <summary>
    /// Raycast result
    /// </summary>
    public class RaycastResult
    {
        public bool Hit { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public float Distance { get; set; }
    }
    
    /// <summary>
    /// Collision statistics
    /// </summary>
    public class CollisionStatistics
    {
        public int TotalEntities { get; set; }
        public int ActiveEntities { get; set; }
        public int ProcessedCollisionsPerTick { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public int SpatialGridCells { get; set; }
        public float AverageEntitiesPerCell { get; set; }
    }
    
    /// <summary>
    /// Spatial grid for efficient collision detection
    /// </summary>
    internal class SpatialGrid : IDisposable
    {
        private readonly ConcurrentDictionary<Vector3Int, List<Entity>> grid;
        private readonly float cellSize;
        private readonly int gridSize;
        
        public SpatialGrid(int worldSize, float cellSize)
        {
            this.cellSize = cellSize;
            this.gridSize = (int)Math.Ceiling(worldSize / cellSize);
            grid = new ConcurrentDictionary<Vector3Int, List<Entity>>();
        }
        
        public void AddEntity(Entity entity)
        {
            var cellPos = GetCellPosition(entity.Position);
            grid.AddOrUpdate(cellPos, new List<Entity> { entity }, (key, list) =>
            {
                list.Add(entity);
                return list;
            });
        }
        
        public void RemoveEntity(Entity entity)
        {
            var cellPos = GetCellPosition(entity.Position);
            if (grid.TryGetValue(cellPos, out var list))
            {
                list.Remove(entity);
            }
        }
        
        public void UpdateEntity(Entity entity)
        {
            // This is simplified - in practice would need to track old position
            RemoveEntity(entity);
            AddEntity(entity);
        }
        
        public List<Entity> GetNearbyEntities(Entity entity, float radius)
        {
            var nearbyEntities = new List<Entity>();
            var cellRadius = (int)Math.Ceiling(radius / cellSize);
            var centerCell = GetCellPosition(entity.Position);
            
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    var cellPos = new Vector3Int(centerCell.X + x, centerCell.Y, centerCell.Z + z);
                    
                    if (grid.TryGetValue(cellPos, out var entities))
                    {
                        nearbyEntities.AddRange(entities);
                    }
                }
            }
            
            return nearbyEntities.Where(e => e.Id != entity.Id).ToList();
        }
        
        public List<Entity> GetEntitiesInRadius(Vector3 center, float radius)
        {
            var nearbyEntities = new List<Entity>();
            var cellRadius = (int)Math.Ceiling(radius / cellSize);
            var centerCell = GetCellPosition(center);
            
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    var cellPos = new Vector3Int(centerCell.X + x, centerCell.Y, centerCell.Z + z);
                    
                    if (grid.TryGetValue(cellPos, out var entities))
                    {
                        nearbyEntities.AddRange(entities);
                    }
                }
            }
            
            return nearbyEntities.Where(e => Vector3.Distance(e.Position, center) <= radius).ToList();
        }
        
        private Vector3Int GetCellPosition(Vector3 position)
        {
            return new Vector3Int(
                (int)Math.Floor(position.X / cellSize),
                (int)Math.Floor(position.Y / cellSize),
                (int)Math.Floor(position.Z / cellSize)
            );
        }
        
        public int GetCellCount()
        {
            return grid.Count;
        }
        
        public float GetAverageEntitiesPerCell()
        {
            var totalEntities = grid.Values.Sum(list => list.Count);
            return grid.Count > 0 ? (float)totalEntities / grid.Count : 0f;
        }
        
        public void Dispose()
        {
            grid.Clear();
        }
    }
    
    /// <summary>
    /// Collision shape types
    /// </summary>
    public enum CollisionShapeType
    {
        Sphere,
        Box,
        Capsule,
        Mesh
    }
    
    /// <summary>
    /// Collision types
    /// </summary>
    public enum CollisionType
    {
        EntityTerrain,
        EntityEntity
    }
    
    /// <summary>
    /// Collision response types
    /// </summary>
    public enum CollisionResponseType
    {
        Block,
        Slide,
        Bounce,
        Callback
    }
    
    /// <summary>
    /// 3D vector with math extensions
    /// </summary>
    public static class Vector3Extensions
    {
        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 normal)
        {
            return vector - Vector3.Dot(vector, normal) * normal;
        }
        
        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            return vector - 2f * Vector3.Dot(vector, normal) * normal;
        }
        
        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }
        
        public static Vector3 Normalize(Vector3 vector)
        {
            var length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            if (length < 0.0001f)
                return Vector3.Zero;
                
            return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
        }
    }
}
#endif

