using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World.Physics
{
    /// <summary>
    /// Water physics system that simulates water flow and pressure
    /// Implements realistic water behavior including pressure, flow, and evaporation
    /// </summary>
    public class WaterPhysicsSystem
    {
        private readonly ILogger<WaterPhysicsSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly WaterPhysicsConfig physicsConfig;
        
        // Water data storage
        private readonly ConcurrentDictionary<Vector3Int, WaterCell> waterCells;
        private readonly ConcurrentQueue<WaterUpdateRequest> updateQueue;
        private readonly Timer physicsTimer;
        
        // Performance tracking
        private int processedCellsPerTick;
        private DateTime lastUpdateTime;
        
        // Thread synchronization
        private readonly object lockObject = new object();
        private volatile bool isProcessing;
        
        public WaterPhysicsSystem(ILogger<WaterPhysicsSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.physicsConfig = config.Water.Physics ?? new WaterPhysicsConfig();
            
            waterCells = new ConcurrentDictionary<Vector3Int, WaterCell>();
            updateQueue = new ConcurrentQueue<WaterUpdateRequest>();
            
            // Start physics timer
            physicsTimer = new Timer(UpdatePhysics, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(physicsConfig.UpdateIntervalMs));
            
            lastUpdateTime = DateTime.UtcNow;
            
            logger.LogInformation("[WaterPhysicsSystem] Initialized with update interval: {Interval}ms", 
                physicsConfig.UpdateIntervalMs);
        }
        
        /// <summary>
        /// Initializes water system for a chunk
        /// </summary>
        public void InitializeChunk(int chunkX, int chunkZ, ChunkData chunkData)
        {
            var chunkSize = config.World.ChunkSize;
            var worldOriginX = chunkX * chunkSize;
            var worldOriginZ = chunkZ * chunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = worldOriginX + x;
                    var worldZ = worldOriginZ + z;
                    var worldPos = new Vector3Int(worldX, 0, worldZ);
                    
                    // Check if this position should have water
                    if (ShouldHaveWater(worldPos, chunkData, x, z))
                    {
                        InitializeWaterCell(worldPos, chunkData, x, z);
                    }
                }
            }
            
            logger.LogDebug("[WaterPhysicsSystem] Initialized water for chunk ({ChunkX}, {ChunkZ})", chunkX, chunkZ);
        }
        
        /// <summary>
        /// Checks if a position should have water
        /// </summary>
        private bool ShouldHaveWater(Vector3Int worldPos, ChunkData chunkData, int localX, int localZ)
        {
            // Check if position is at or below sea level
            var heightValue = chunkData.HeightMap[localX, localZ];
            var seaLevel = config.World.SeaLevel;
            
            if (heightValue < seaLevel)
            {
                return true;
            }
            
            // Check for river water
            if (chunkData.RiverMap != null && chunkData.RiverMap[localX, localZ] > 0.5f)
            {
                return true;
            }
            
            // Check for lake water
            if (chunkData.LakeMap != null && chunkData.LakeMap[localX, localZ] > 0.5f)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Initializes a water cell at the specified position
        /// </summary>
        private void InitializeWaterCell(Vector3Int worldPos, ChunkData chunkData, int localX, int localZ)
        {
            var heightValue = chunkData.HeightMap[localX, localZ];
            var seaLevel = config.World.SeaLevel;
            var waterLevel = Math.Max(seaLevel, heightValue + physicsConfig.DefaultWaterDepth);
            
            var waterCell = new WaterCell
            {
                Position = worldPos,
                WaterLevel = waterLevel,
                Pressure = CalculatePressure(worldPos, waterLevel),
                FlowVelocity = Vector3.Zero,
                IsSource = IsWaterSource(worldPos, chunkData, localX, localZ),
                Temperature = physicsConfig.DefaultWaterTemperature,
                IsStatic = false
            };
            
            waterCells[worldPos] = waterCell;
        }
        
        /// <summary>
        /// Checks if a position is a water source
        /// </summary>
        private bool IsWaterSource(Vector3Int worldPos, ChunkData chunkData, int localX, int localZ)
        {
            // Ocean and large lakes are water sources
            var seaLevel = config.World.SeaLevel;
            var heightValue = chunkData.HeightMap[localX, localZ];
            
            // Deep ocean water
            if (heightValue < seaLevel - 10)
            {
                return true;
            }
            
            // River sources (simplified)
            if (chunkData.RiverMap != null && chunkData.RiverMap[localX, localZ] > 0.8f)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculates water pressure at a position
        /// </summary>
        private float CalculatePressure(Vector3Int position, float waterLevel)
        {
            var depth = 0f;
            var checkPos = position;
            
            // Count water cells above this position
            while (waterCells.TryGetValue(checkPos, out var cell) && cell.WaterLevel > position.Y)
            {
                depth += cell.WaterLevel - position.Y;
                checkPos = new Vector3Int(position.X, checkPos.Y + 1, position.Z);
            }
            
            // Add atmospheric pressure
            var atmosphericPressure = physicsConfig.AtmosphericPressure;
            var hydrostaticPressure = depth * physicsConfig.WaterDensity * physicsConfig.Gravity;
            
            return atmosphericPressure + hydrostaticPressure;
        }
        
        /// <summary>
        /// Updates water physics for all cells
        /// </summary>
        private void UpdatePhysics(object state)
        {
            if (isProcessing) return;
            
            isProcessing = true;
            processedCellsPerTick = 0;
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Process queued updates first
                ProcessQueuedUpdates();
                
                // Update water physics for all cells
                UpdateWaterFlow();
                UpdateWaterPressure();
                ProcessWaterEvaporation();
                
                // Update performance metrics
                var updateTime = DateTime.UtcNow - startTime;
                lastUpdateTime = DateTime.UtcNow;
                
                if (updateTime.TotalMilliseconds > physicsConfig.MaxUpdateTimeMs)
                {
                    logger.LogWarning("[WaterPhysicsSystem] Physics update took {Time}ms (target: {Target}ms)", 
                        updateTime.TotalMilliseconds, physicsConfig.MaxUpdateTimeMs);
                }
                
                logger.LogDebug("[WaterPhysicsSystem] Updated {Count} water cells in {Time}ms", 
                    processedCellsPerTick, updateTime.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WaterPhysicsSystem] Error during physics update");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Processes queued water updates
        /// </summary>
        private void ProcessQueuedUpdates()
        {
            var processedCount = 0;
            
            while (updateQueue.TryDequeue(out var updateRequest) && processedCount < physicsConfig.MaxQueuedUpdatesPerTick)
            {
                ProcessUpdateRequest(updateRequest);
                processedCount++;
                processedCellsPerTick++;
            }
        }
        
        /// <summary>
        /// Processes a single water update request
        /// </summary>
        private void ProcessUpdateRequest(WaterUpdateRequest request)
        {
            if (waterCells.TryGetValue(request.Position, out var cell))
            {
                switch (request.Type)
                {
                    case WaterUpdateType.BlockAdded:
                        HandleBlockAdded(cell, request.BlockType);
                        break;
                        
                    case WaterUpdateType.BlockRemoved:
                        HandleBlockRemoved(cell, request.BlockType);
                        break;
                        
                    case WaterUpdateType.TemperatureChange:
                        cell.Temperature = request.NewValue;
                        break;
                        
                    case WaterUpdateType.SourceToggle:
                        cell.IsSource = request.NewValue > 0;
                        break;
                }
            }
        }
        
        /// <summary>
        /// Handles block addition at water cell
        /// </summary>
        private void HandleBlockAdded(WaterCell cell, BlockType blockType)
        {
            if (IsWaterDisplacingBlock(blockType))
            {
                // Displace water
                var displacedWater = cell.WaterLevel - cell.Position.Y;
                if (displacedWater > 0)
                {
                    DisplaceWater(cell.Position, displacedWater);
                }
                
                cell.WaterLevel = cell.Position.Y;
                cell.IsStatic = true;
            }
        }
        
        /// <summary>
        /// Handles block removal at water cell
        /// </summary>
        private void HandleBlockRemoved(WaterCell cell, BlockType blockType)
        {
            if (IsWaterDisplacingBlock(blockType))
            {
                // Allow water to flow back
                cell.IsStatic = false;
                
                // Trigger immediate flow update
                QueueFlowUpdate(cell.Position);
            }
        }
        
        /// <summary>
        /// Checks if a block type displaces water
        /// </summary>
        private bool IsWaterDisplacingBlock(BlockType blockType)
        {
            return blockType != BlockType.Air && 
                   blockType != BlockType.Water && 
                   !IsWaterBlock(blockType);
        }
        
        /// <summary>
        /// Checks if a block type is water-related
        /// </summary>
        private bool IsWaterBlock(BlockType blockType)
        {
            return blockType == BlockType.Water || 
                   blockType == BlockType.WaterSource ||
                   blockType == BlockType.Ice ||
                   blockType == BlockType.SnowBlock;
        }
        
        /// <summary>
        /// Updates water flow for all cells
        /// </summary>
        private void UpdateWaterFlow()
        {
            var cellsToUpdate = new List<WaterCell>();
            
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsStatic || cell.IsSource) continue;
                
                // Calculate flow based on pressure and gravity
                var newVelocity = CalculateFlowVelocity(cell);
                
                // Apply flow if significant
                if (Vector3.Distance(newVelocity, cell.FlowVelocity) > physicsConfig.MinFlowVelocity)
                {
                    cell.FlowVelocity = newVelocity;
                    cellsToUpdate.Add(cell);
                }
            }
            
            // Apply flow changes
            foreach (var cell in cellsToUpdate)
            {
                ApplyFlowChange(cell);
                processedCellsPerTick++;
            }
        }
        
        /// <summary>
        /// Calculates flow velocity for a water cell
        /// </summary>
        private Vector3 CalculateFlowVelocity(WaterCell cell)
        {
            var flowDirection = Vector3.Zero;
            var totalPressure = 0f;
            
            // Check neighbors for pressure differences
            var neighbors = GetNeighborCells(cell.Position);
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null) continue;
                
                var pressureDiff = cell.Pressure - neighbor.Pressure;
                if (pressureDiff > 0)
                {
                    var direction = Vector3.Normalize(neighbor.Position - cell.Position);
                    flowDirection += direction * pressureDiff;
                    totalPressure += Math.Abs(pressureDiff);
                }
            }
            
            // Apply flow rate based on pressure difference
            if (totalPressure > 0)
            {
                flowDirection = Vector3.Normalize(flowDirection);
                var flowRate = Math.Min(totalPressure * physicsConfig.FlowRate, physicsConfig.MaxFlowVelocity);
                
                return flowDirection * flowRate;
            }
            
            // Apply gravity for vertical flow
            var gravityFlow = Math.Min(cell.WaterLevel - cell.Position.Y, physicsConfig.MaxVerticalFlow);
            return new Vector3(flowDirection.X, -gravityFlow, flowDirection.Z);
        }
        
        /// <summary>
        /// Gets neighboring water cells
        /// </summary>
        private List<WaterCell> GetNeighborCells(Vector3Int position)
        {
            var neighbors = new List<WaterCell>();
            var directions = new[]
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
            };
            
            foreach (var dir in directions)
            {
                var neighborPos = position + dir;
                if (waterCells.TryGetValue(neighborPos, out var neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Applies flow change to a water cell
        /// </summary>
        private void ApplyFlowChange(WaterCell cell)
        {
            // Calculate water transfer based on velocity
            var waterTransfer = cell.FlowVelocity * physicsConfig.UpdateIntervalMs / 1000f;
            
            // Update water level
            var newWaterLevel = cell.WaterLevel + waterTransfer.Y;
            
            // Clamp to reasonable bounds
            newWaterLevel = Math.Max(cell.Position.Y, 
                Math.Min(cell.Position.Y + physicsConfig.MaxWaterDepth, newWaterLevel));
            
            cell.WaterLevel = newWaterLevel;
            
            // Update neighboring cells if significant flow
            if (Math.Abs(waterTransfer.X) > 0.01f || Math.Abs(waterTransfer.Z) > 0.01f)
            {
                QueueFlowUpdate(cell.Position);
            }
        }
        
        /// <summary>
        /// Updates water pressure for all cells
        /// </summary>
        private void UpdateWaterPressure()
        {
            foreach (var cell in waterCells.Values)
            {
                var newPressure = CalculatePressure(cell.Position, cell.WaterLevel);
                
                // Update pressure if significant change
                if (Math.Abs(newPressure - cell.Pressure) > physicsConfig.MinPressureChange)
                {
                    cell.Pressure = newPressure;
                    processedCellsPerTick++;
                }
            }
        }
        
        /// <summary>
        /// Processes water evaporation
        /// </summary>
        private void ProcessWaterEvaporation()
        {
            if (!physicsConfig.EnableEvaporation) return;
            
            var cellsToEvaporate = new List<WaterCell>();
            
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsSource || cell.WaterLevel <= cell.Position.Y) continue;
                
                // Calculate evaporation rate based on temperature and exposure
                var evaporationRate = CalculateEvaporationRate(cell);
                
                if (evaporationRate > 0)
                {
                    cell.WaterLevel -= evaporationRate;
                    cellsToEvaporate.Add(cell);
                    processedCellsPerTick++;
                }
            }
            
            logger.LogDebug("[WaterPhysicsSystem] Evaporated water from {Count} cells", cellsToEvaporate.Count);
        }
        
        /// <summary>
        /// Calculates evaporation rate for a water cell
        /// </summary>
        private float CalculateEvaporationRate(WaterCell cell)
        {
            // Base evaporation rate
            var baseRate = physicsConfig.BaseEvaporationRate;
            
            // Temperature factor
            var tempFactor = Math.Max(0, (cell.Temperature - 20) / 20f); // Above 20°C
            
            // Exposure factor (simplified)
            var exposureFactor = cell.WaterLevel - cell.Position.Y > 2 ? 1.0f : 0.5f;
            
            return baseRate * tempFactor * exposureFactor;
        }
        
        /// <summary>
        /// Displaces water to neighboring cells
        /// </summary>
        private void DisplaceWater(Vector3Int position, float waterAmount)
        {
            var directions = new[]
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
            };
            
            var remainingWater = waterAmount;
            var waterPerDirection = waterAmount / directions.Length;
            
            foreach (var dir in directions)
            {
                if (remainingWater <= 0) break;
                
                var targetPos = position + dir;
                if (waterCells.TryGetValue(targetPos, out var targetCell))
                {
                    var transferAmount = Math.Min(waterPerDirection, remainingWater);
                    targetCell.WaterLevel += transferAmount;
                    targetCell.IsStatic = false;
                    remainingWater -= transferAmount;
                }
            }
        }
        
        /// <summary>
        /// Queues a flow update for a position
        /// </summary>
        private void QueueFlowUpdate(Vector3Int position)
        {
            var neighbors = GetNeighborPositions(position);
            
            foreach (var neighborPos in neighbors)
            {
                if (waterCells.ContainsKey(neighborPos))
                {
                    updateQueue.Enqueue(new WaterUpdateRequest
                    {
                        Position = neighborPos,
                        Type = WaterUpdateType.FlowUpdate
                    });
                }
            }
        }
        
        /// <summary>
        /// Gets neighboring positions
        /// </summary>
        private List<Vector3Int> GetNeighborPositions(Vector3Int position)
        {
            return new List<Vector3Int>
            {
                position + new Vector3Int(1, 0, 0),
                position + new Vector3Int(-1, 0, 0),
                position + new Vector3Int(0, 0, 1),
                position + new Vector3Int(0, 0, -1),
                position + new Vector3Int(0, 1, 0),
                position + new Vector3Int(0, -1, 0)
            };
        }
        
        /// <summary>
        /// Adds a block change to the update queue
        /// </summary>
        public void QueueBlockChange(Vector3Int position, BlockType oldBlock, BlockType newBlock)
        {
            updateQueue.Enqueue(new WaterUpdateRequest
            {
                Position = position,
                Type = WaterUpdateType.BlockAdded,
                BlockType = newBlock
            });
            
            if (IsWaterDisplacingBlock(oldBlock) && IsWaterDisplacingBlock(newBlock))
            {
                // No water impact
                return;
            }
            
            if (IsWaterDisplacingBlock(oldBlock) && !IsWaterDisplacingBlock(newBlock))
            {
                // Block removed - water can flow
                updateQueue.Enqueue(new WaterUpdateRequest
                {
                    Position = position,
                    Type = WaterUpdateType.BlockRemoved,
                    BlockType = oldBlock
                });
            }
        }
        
        /// <summary>
        /// Gets water level at a position
        /// </summary>
        public float GetWaterLevel(Vector3Int position)
        {
            return waterCells.TryGetValue(position, out var cell) ? cell.WaterLevel : float.MinValue;
        }
        
        /// <summary>
        /// Checks if a position has water
        /// </summary>
        public bool HasWater(Vector3Int position)
        {
            return waterCells.ContainsKey(position) && 
                   waterCells[position].WaterLevel > position.Y;
        }
        
        /// <summary>
        /// Gets water physics statistics
        /// </summary>
        public WaterPhysicsStatistics GetStatistics()
        {
            lock (lockObject)
            {
                return new WaterPhysicsStatistics
                {
                    TotalWaterCells = waterCells.Count,
                    ActiveWaterCells = CountActiveCells(),
                    SourceWaterCells = CountSourceCells(),
                    StaticWaterCells = CountStaticCells(),
                    ProcessedCellsPerTick = processedCellsPerTick,
                    LastUpdateTime = lastUpdateTime,
                    AverageWaterLevel = CalculateAverageWaterLevel(),
                    AveragePressure = CalculateAveragePressure()
                };
            }
        }
        
        /// <summary>
        /// Counts active water cells
        /// </summary>
        private int CountActiveCells()
        {
            var count = 0;
            foreach (var cell in waterCells.Values)
            {
                if (!cell.IsStatic && cell.WaterLevel > cell.Position.Y)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Counts source water cells
        /// </summary>
        private int CountSourceCells()
        {
            var count = 0;
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsSource)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Counts static water cells
        /// </summary>
        private int CountStaticCells()
        {
            var count = 0;
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsStatic)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Calculates average water level
        /// </summary>
        private float CalculateAverageWaterLevel()
        {
            var total = 0f;
            var count = 0;
            
            foreach (var cell in waterCells.Values)
            {
                total += cell.WaterLevel;
                count++;
            }
            
            return count > 0 ? total / count : 0f;
        }
        
        /// <summary>
        /// Calculates average pressure
        /// </summary>
        private float CalculateAveragePressure()
        {
            var total = 0f;
            var count = 0;
            
            foreach (var cell in waterCells.Values)
            {
                total += cell.Pressure;
                count++;
            }
            
            return count > 0 ? total / count : 0f;
        }
        
        /// <summary>
        /// Disposes the water physics system
        /// </summary>
        public void Dispose()
        {
            physicsTimer?.Dispose();
            
            lock (lockObject)
            {
                waterCells.Clear();
                updateQueue.Clear();
            }
            
            logger.LogInformation("[WaterPhysicsSystem] Disposed");
        }
    }
    
    /// <summary>
    /// Water cell data
    /// </summary>
    public class WaterCell
    {
        public Vector3Int Position { get; set; }
        public float WaterLevel { get; set; }
        public float Pressure { get; set; }
        public Vector3 FlowVelocity { get; set; }
        public bool IsSource { get; set; }
        public float Temperature { get; set; }
        public bool IsStatic { get; set; }
    }
    
    /// <summary>
    /// Water update request
    /// </summary>
    internal struct WaterUpdateRequest
    {
        public Vector3Int Position;
        public WaterUpdateType Type;
        public BlockType BlockType;
        public float NewValue;
    }
    
    /// <summary>
    /// Water update types
    /// </summary>
    internal enum WaterUpdateType
    {
        FlowUpdate,
        BlockAdded,
        BlockRemoved,
        TemperatureChange,
        SourceToggle
    }
    
    /// <summary>
    /// Water physics configuration
    /// </summary>
    public class WaterPhysicsConfig
    {
        public int UpdateIntervalMs { get; set; } = 100; // 10 FPS
        public int MaxUpdateTimeMs { get; set; } = 50;
        public int MaxQueuedUpdatesPerTick { get; set; } = 100;
        
        public float Gravity { get; set; } = 9.81f;
        public float WaterDensity { get; set; } = 1000f; // kg/m³
        public float AtmosphericPressure { get; set; } = 101325f; // Pa
        
        public float FlowRate { get; set; } = 0.1f;
        public float MaxFlowVelocity { get; set; } = 5f;
        public float MinFlowVelocity { get; set; } = 0.01f;
        public float MaxVerticalFlow { get; set; } = 2f;
        
        public float DefaultWaterDepth { get; set; } = 1f;
        public float MaxWaterDepth { get; set; } = 10f;
        public float DefaultWaterTemperature { get; set; } = 20f; // Celsius
        
        public bool EnableEvaporation { get; set; } = true;
        public float BaseEvaporationRate { get; set; } = 0.001f;
        public float MinPressureChange { get; set; } = 0.1f;
    }
    
    /// <summary>
    /// Water physics statistics
    /// </summary>
    public class WaterPhysicsStatistics
    {
        public int TotalWaterCells { get; set; }
        public int ActiveWaterCells { get; set; }
        public int SourceWaterCells { get; set; }
        public int StaticWaterCells { get; set; }
        public int ProcessedCellsPerTick { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public float AverageWaterLevel { get; set; }
        public float AveragePressure { get; set; }
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
        
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
        
        public static Vector3 operator *(Vector3 a, float scalar)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        public static Vector3 Normalize(Vector3 vector)
        {
            var length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            if (length < 0.0001f)
                return Vector3.Zero;
                
            return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
        }
        
        public static Vector3 Zero => new Vector3(0, 0, 0);
        
        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }
}using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameServerApp.World.Physics
{
    /// <summary>
    /// Water physics system that simulates water flow and pressure
    /// Implements realistic water behavior including pressure, flow, and evaporation
    /// </summary>
    public class WaterPhysicsSystem
    {
        private readonly ILogger<WaterPhysicsSystem> logger;
        private readonly WorldGenerationConfig config;
        private readonly WaterPhysicsConfig physicsConfig;
        
        // Water data storage
        private readonly ConcurrentDictionary<Vector3Int, WaterCell> waterCells;
        private readonly ConcurrentQueue<WaterUpdateRequest> updateQueue;
        private readonly Timer physicsTimer;
        
        // Performance tracking
        private int processedCellsPerTick;
        private DateTime lastUpdateTime;
        
        // Thread synchronization
        private readonly object lockObject = new object();
        private volatile bool isProcessing;
        
        public WaterPhysicsSystem(ILogger<WaterPhysicsSystem> logger, WorldGenerationConfig config)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.physicsConfig = config.Water.Physics ?? new WaterPhysicsConfig();
            
            waterCells = new ConcurrentDictionary<Vector3Int, WaterCell>();
            updateQueue = new ConcurrentQueue<WaterUpdateRequest>();
            
            // Start physics timer
            physicsTimer = new Timer(UpdatePhysics, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(physicsConfig.UpdateIntervalMs));
            
            lastUpdateTime = DateTime.UtcNow;
            
            logger.LogInformation("[WaterPhysicsSystem] Initialized with update interval: {Interval}ms", 
                physicsConfig.UpdateIntervalMs);
        }
        
        /// <summary>
        /// Initializes water system for a chunk
        /// </summary>
        public void InitializeChunk(int chunkX, int chunkZ, ChunkData chunkData)
        {
            var chunkSize = config.World.ChunkSize;
            var worldOriginX = chunkX * chunkSize;
            var worldOriginZ = chunkZ * chunkSize;
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    var worldX = worldOriginX + x;
                    var worldZ = worldOriginZ + z;
                    var worldPos = new Vector3Int(worldX, 0, worldZ);
                    
                    // Check if this position should have water
                    if (ShouldHaveWater(worldPos, chunkData, x, z))
                    {
                        InitializeWaterCell(worldPos, chunkData, x, z);
                    }
                }
            }
            
            logger.LogDebug("[WaterPhysicsSystem] Initialized water for chunk ({ChunkX}, {ChunkZ})", chunkX, chunkZ);
        }
        
        /// <summary>
        /// Checks if a position should have water
        /// </summary>
        private bool ShouldHaveWater(Vector3Int worldPos, ChunkData chunkData, int localX, int localZ)
        {
            // Check if position is at or below sea level
            var heightValue = chunkData.HeightMap[localX, localZ];
            var seaLevel = config.World.SeaLevel;
            
            if (heightValue < seaLevel)
            {
                return true;
            }
            
            // Check for river water
            if (chunkData.RiverMap != null && chunkData.RiverMap[localX, localZ] > 0.5f)
            {
                return true;
            }
            
            // Check for lake water
            if (chunkData.LakeMap != null && chunkData.LakeMap[localX, localZ] > 0.5f)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Initializes a water cell at the specified position
        /// </summary>
        private void InitializeWaterCell(Vector3Int worldPos, ChunkData chunkData, int localX, int localZ)
        {
            var heightValue = chunkData.HeightMap[localX, localZ];
            var seaLevel = config.World.SeaLevel;
            var waterLevel = Math.Max(seaLevel, heightValue + physicsConfig.DefaultWaterDepth);
            
            var waterCell = new WaterCell
            {
                Position = worldPos,
                WaterLevel = waterLevel,
                Pressure = CalculatePressure(worldPos, waterLevel),
                FlowVelocity = Vector3.Zero,
                IsSource = IsWaterSource(worldPos, chunkData, localX, localZ),
                Temperature = physicsConfig.DefaultWaterTemperature,
                IsStatic = false
            };
            
            waterCells[worldPos] = waterCell;
        }
        
        /// <summary>
        /// Checks if a position is a water source
        /// </summary>
        private bool IsWaterSource(Vector3Int worldPos, ChunkData chunkData, int localX, int localZ)
        {
            // Ocean and large lakes are water sources
            var seaLevel = config.World.SeaLevel;
            var heightValue = chunkData.HeightMap[localX, localZ];
            
            // Deep ocean water
            if (heightValue < seaLevel - 10)
            {
                return true;
            }
            
            // River sources (simplified)
            if (chunkData.RiverMap != null && chunkData.RiverMap[localX, localZ] > 0.8f)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculates water pressure at a position
        /// </summary>
        private float CalculatePressure(Vector3Int position, float waterLevel)
        {
            var depth = 0f;
            var checkPos = position;
            
            // Count water cells above this position
            while (waterCells.TryGetValue(checkPos, out var cell) && cell.WaterLevel > position.Y)
            {
                depth += cell.WaterLevel - position.Y;
                checkPos = new Vector3Int(position.X, checkPos.Y + 1, position.Z);
            }
            
            // Add atmospheric pressure
            var atmosphericPressure = physicsConfig.AtmosphericPressure;
            var hydrostaticPressure = depth * physicsConfig.WaterDensity * physicsConfig.Gravity;
            
            return atmosphericPressure + hydrostaticPressure;
        }
        
        /// <summary>
        /// Updates water physics for all cells
        /// </summary>
        private void UpdatePhysics(object state)
        {
            if (isProcessing) return;
            
            isProcessing = true;
            processedCellsPerTick = 0;
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Process queued updates first
                ProcessQueuedUpdates();
                
                // Update water physics for all cells
                UpdateWaterFlow();
                UpdateWaterPressure();
                ProcessWaterEvaporation();
                
                // Update performance metrics
                var updateTime = DateTime.UtcNow - startTime;
                lastUpdateTime = DateTime.UtcNow;
                
                if (updateTime.TotalMilliseconds > physicsConfig.MaxUpdateTimeMs)
                {
                    logger.LogWarning("[WaterPhysicsSystem] Physics update took {Time}ms (target: {Target}ms)", 
                        updateTime.TotalMilliseconds, physicsConfig.MaxUpdateTimeMs);
                }
                
                logger.LogDebug("[WaterPhysicsSystem] Updated {Count} water cells in {Time}ms", 
                    processedCellsPerTick, updateTime.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WaterPhysicsSystem] Error during physics update");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Processes queued water updates
        /// </summary>
        private void ProcessQueuedUpdates()
        {
            var processedCount = 0;
            
            while (updateQueue.TryDequeue(out var updateRequest) && processedCount < physicsConfig.MaxQueuedUpdatesPerTick)
            {
                ProcessUpdateRequest(updateRequest);
                processedCount++;
                processedCellsPerTick++;
            }
        }
        
        /// <summary>
        /// Processes a single water update request
        /// </summary>
        private void ProcessUpdateRequest(WaterUpdateRequest request)
        {
            if (waterCells.TryGetValue(request.Position, out var cell))
            {
                switch (request.Type)
                {
                    case WaterUpdateType.BlockAdded:
                        HandleBlockAdded(cell, request.BlockType);
                        break;
                        
                    case WaterUpdateType.BlockRemoved:
                        HandleBlockRemoved(cell, request.BlockType);
                        break;
                        
                    case WaterUpdateType.TemperatureChange:
                        cell.Temperature = request.NewValue;
                        break;
                        
                    case WaterUpdateType.SourceToggle:
                        cell.IsSource = request.NewValue > 0;
                        break;
                }
            }
        }
        
        /// <summary>
        /// Handles block addition at water cell
        /// </summary>
        private void HandleBlockAdded(WaterCell cell, BlockType blockType)
        {
            if (IsWaterDisplacingBlock(blockType))
            {
                // Displace water
                var displacedWater = cell.WaterLevel - cell.Position.Y;
                if (displacedWater > 0)
                {
                    DisplaceWater(cell.Position, displacedWater);
                }
                
                cell.WaterLevel = cell.Position.Y;
                cell.IsStatic = true;
            }
        }
        
        /// <summary>
        /// Handles block removal at water cell
        /// </summary>
        private void HandleBlockRemoved(WaterCell cell, BlockType blockType)
        {
            if (IsWaterDisplacingBlock(blockType))
            {
                // Allow water to flow back
                cell.IsStatic = false;
                
                // Trigger immediate flow update
                QueueFlowUpdate(cell.Position);
            }
        }
        
        /// <summary>
        /// Checks if a block type displaces water
        /// </summary>
        private bool IsWaterDisplacingBlock(BlockType blockType)
        {
            return blockType != BlockType.Air && 
                   blockType != BlockType.Water && 
                   !IsWaterBlock(blockType);
        }
        
        /// <summary>
        /// Checks if a block type is water-related
        /// </summary>
        private bool IsWaterBlock(BlockType blockType)
        {
            return blockType == BlockType.Water || 
                   blockType == BlockType.WaterSource ||
                   blockType == BlockType.Ice ||
                   blockType == BlockType.SnowBlock;
        }
        
        /// <summary>
        /// Updates water flow for all cells
        /// </summary>
        private void UpdateWaterFlow()
        {
            var cellsToUpdate = new List<WaterCell>();
            
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsStatic || cell.IsSource) continue;
                
                // Calculate flow based on pressure and gravity
                var newVelocity = CalculateFlowVelocity(cell);
                
                // Apply flow if significant
                if (Vector3.Distance(newVelocity, cell.FlowVelocity) > physicsConfig.MinFlowVelocity)
                {
                    cell.FlowVelocity = newVelocity;
                    cellsToUpdate.Add(cell);
                }
            }
            
            // Apply flow changes
            foreach (var cell in cellsToUpdate)
            {
                ApplyFlowChange(cell);
                processedCellsPerTick++;
            }
        }
        
        /// <summary>
        /// Calculates flow velocity for a water cell
        /// </summary>
        private Vector3 CalculateFlowVelocity(WaterCell cell)
        {
            var flowDirection = Vector3.Zero;
            var totalPressure = 0f;
            
            // Check neighbors for pressure differences
            var neighbors = GetNeighborCells(cell.Position);
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null) continue;
                
                var pressureDiff = cell.Pressure - neighbor.Pressure;
                if (pressureDiff > 0)
                {
                    var direction = Vector3.Normalize(neighbor.Position - cell.Position);
                    flowDirection += direction * pressureDiff;
                    totalPressure += Math.Abs(pressureDiff);
                }
            }
            
            // Apply flow rate based on pressure difference
            if (totalPressure > 0)
            {
                flowDirection = Vector3.Normalize(flowDirection);
                var flowRate = Math.Min(totalPressure * physicsConfig.FlowRate, physicsConfig.MaxFlowVelocity);
                
                return flowDirection * flowRate;
            }
            
            // Apply gravity for vertical flow
            var gravityFlow = Math.Min(cell.WaterLevel - cell.Position.Y, physicsConfig.MaxVerticalFlow);
            return new Vector3(flowDirection.X, -gravityFlow, flowDirection.Z);
        }
        
        /// <summary>
        /// Gets neighboring water cells
        /// </summary>
        private List<WaterCell> GetNeighborCells(Vector3Int position)
        {
            var neighbors = new List<WaterCell>();
            var directions = new[]
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
            };
            
            foreach (var dir in directions)
            {
                var neighborPos = position + dir;
                if (waterCells.TryGetValue(neighborPos, out var neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Applies flow change to a water cell
        /// </summary>
        private void ApplyFlowChange(WaterCell cell)
        {
            // Calculate water transfer based on velocity
            var waterTransfer = cell.FlowVelocity * physicsConfig.UpdateIntervalMs / 1000f;
            
            // Update water level
            var newWaterLevel = cell.WaterLevel + waterTransfer.Y;
            
            // Clamp to reasonable bounds
            newWaterLevel = Math.Max(cell.Position.Y, 
                Math.Min(cell.Position.Y + physicsConfig.MaxWaterDepth, newWaterLevel));
            
            cell.WaterLevel = newWaterLevel;
            
            // Update neighboring cells if significant flow
            if (Math.Abs(waterTransfer.X) > 0.01f || Math.Abs(waterTransfer.Z) > 0.01f)
            {
                QueueFlowUpdate(cell.Position);
            }
        }
        
        /// <summary>
        /// Updates water pressure for all cells
        /// </summary>
        private void UpdateWaterPressure()
        {
            foreach (var cell in waterCells.Values)
            {
                var newPressure = CalculatePressure(cell.Position, cell.WaterLevel);
                
                // Update pressure if significant change
                if (Math.Abs(newPressure - cell.Pressure) > physicsConfig.MinPressureChange)
                {
                    cell.Pressure = newPressure;
                    processedCellsPerTick++;
                }
            }
        }
        
        /// <summary>
        /// Processes water evaporation
        /// </summary>
        private void ProcessWaterEvaporation()
        {
            if (!physicsConfig.EnableEvaporation) return;
            
            var cellsToEvaporate = new List<WaterCell>();
            
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsSource || cell.WaterLevel <= cell.Position.Y) continue;
                
                // Calculate evaporation rate based on temperature and exposure
                var evaporationRate = CalculateEvaporationRate(cell);
                
                if (evaporationRate > 0)
                {
                    cell.WaterLevel -= evaporationRate;
                    cellsToEvaporate.Add(cell);
                    processedCellsPerTick++;
                }
            }
            
            logger.LogDebug("[WaterPhysicsSystem] Evaporated water from {Count} cells", cellsToEvaporate.Count);
        }
        
        /// <summary>
        /// Calculates evaporation rate for a water cell
        /// </summary>
        private float CalculateEvaporationRate(WaterCell cell)
        {
            // Base evaporation rate
            var baseRate = physicsConfig.BaseEvaporationRate;
            
            // Temperature factor
            var tempFactor = Math.Max(0, (cell.Temperature - 20) / 20f); // Above 20°C
            
            // Exposure factor (simplified)
            var exposureFactor = cell.WaterLevel - cell.Position.Y > 2 ? 1.0f : 0.5f;
            
            return baseRate * tempFactor * exposureFactor;
        }
        
        /// <summary>
        /// Displaces water to neighboring cells
        /// </summary>
        private void DisplaceWater(Vector3Int position, float waterAmount)
        {
            var directions = new[]
            {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
            };
            
            var remainingWater = waterAmount;
            var waterPerDirection = waterAmount / directions.Length;
            
            foreach (var dir in directions)
            {
                if (remainingWater <= 0) break;
                
                var targetPos = position + dir;
                if (waterCells.TryGetValue(targetPos, out var targetCell))
                {
                    var transferAmount = Math.Min(waterPerDirection, remainingWater);
                    targetCell.WaterLevel += transferAmount;
                    targetCell.IsStatic = false;
                    remainingWater -= transferAmount;
                }
            }
        }
        
        /// <summary>
        /// Queues a flow update for a position
        /// </summary>
        private void QueueFlowUpdate(Vector3Int position)
        {
            var neighbors = GetNeighborPositions(position);
            
            foreach (var neighborPos in neighbors)
            {
                if (waterCells.ContainsKey(neighborPos))
                {
                    updateQueue.Enqueue(new WaterUpdateRequest
                    {
                        Position = neighborPos,
                        Type = WaterUpdateType.FlowUpdate
                    });
                }
            }
        }
        
        /// <summary>
        /// Gets neighboring positions
        /// </summary>
        private List<Vector3Int> GetNeighborPositions(Vector3Int position)
        {
            return new List<Vector3Int>
            {
                position + new Vector3Int(1, 0, 0),
                position + new Vector3Int(-1, 0, 0),
                position + new Vector3Int(0, 0, 1),
                position + new Vector3Int(0, 0, -1),
                position + new Vector3Int(0, 1, 0),
                position + new Vector3Int(0, -1, 0)
            };
        }
        
        /// <summary>
        /// Adds a block change to the update queue
        /// </summary>
        public void QueueBlockChange(Vector3Int position, BlockType oldBlock, BlockType newBlock)
        {
            updateQueue.Enqueue(new WaterUpdateRequest
            {
                Position = position,
                Type = WaterUpdateType.BlockAdded,
                BlockType = newBlock
            });
            
            if (IsWaterDisplacingBlock(oldBlock) && IsWaterDisplacingBlock(newBlock))
            {
                // No water impact
                return;
            }
            
            if (IsWaterDisplacingBlock(oldBlock) && !IsWaterDisplacingBlock(newBlock))
            {
                // Block removed - water can flow
                updateQueue.Enqueue(new WaterUpdateRequest
                {
                    Position = position,
                    Type = WaterUpdateType.BlockRemoved,
                    BlockType = oldBlock
                });
            }
        }
        
        /// <summary>
        /// Gets water level at a position
        /// </summary>
        public float GetWaterLevel(Vector3Int position)
        {
            return waterCells.TryGetValue(position, out var cell) ? cell.WaterLevel : float.MinValue;
        }
        
        /// <summary>
        /// Checks if a position has water
        /// </summary>
        public bool HasWater(Vector3Int position)
        {
            return waterCells.ContainsKey(position) && 
                   waterCells[position].WaterLevel > position.Y;
        }
        
        /// <summary>
        /// Gets water physics statistics
        /// </summary>
        public WaterPhysicsStatistics GetStatistics()
        {
            lock (lockObject)
            {
                return new WaterPhysicsStatistics
                {
                    TotalWaterCells = waterCells.Count,
                    ActiveWaterCells = CountActiveCells(),
                    SourceWaterCells = CountSourceCells(),
                    StaticWaterCells = CountStaticCells(),
                    ProcessedCellsPerTick = processedCellsPerTick,
                    LastUpdateTime = lastUpdateTime,
                    AverageWaterLevel = CalculateAverageWaterLevel(),
                    AveragePressure = CalculateAveragePressure()
                };
            }
        }
        
        /// <summary>
        /// Counts active water cells
        /// </summary>
        private int CountActiveCells()
        {
            var count = 0;
            foreach (var cell in waterCells.Values)
            {
                if (!cell.IsStatic && cell.WaterLevel > cell.Position.Y)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Counts source water cells
        /// </summary>
        private int CountSourceCells()
        {
            var count = 0;
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsSource)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Counts static water cells
        /// </summary>
        private int CountStaticCells()
        {
            var count = 0;
            foreach (var cell in waterCells.Values)
            {
                if (cell.IsStatic)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Calculates average water level
        /// </summary>
        private float CalculateAverageWaterLevel()
        {
            var total = 0f;
            var count = 0;
            
            foreach (var cell in waterCells.Values)
            {
                total += cell.WaterLevel;
                count++;
            }
            
            return count > 0 ? total / count : 0f;
        }
        
        /// <summary>
        /// Calculates average pressure
        /// </summary>
        private float CalculateAveragePressure()
        {
            var total = 0f;
            var count = 0;
            
            foreach (var cell in waterCells.Values)
            {
                total += cell.Pressure;
                count++;
            }
            
            return count > 0 ? total / count : 0f;
        }
        
        /// <summary>
        /// Disposes the water physics system
        /// </summary>
        public void Dispose()
        {
            physicsTimer?.Dispose();
            
            lock (lockObject)
            {
                waterCells.Clear();
                updateQueue.Clear();
            }
            
            logger.LogInformation("[WaterPhysicsSystem] Disposed");
        }
    }
    
    /// <summary>
    /// Water cell data
    /// </summary>
    public class WaterCell
    {
        public Vector3Int Position { get; set; }
        public float WaterLevel { get; set; }
        public float Pressure { get; set; }
        public Vector3 FlowVelocity { get; set; }
        public bool IsSource { get; set; }
        public float Temperature { get; set; }
        public bool IsStatic { get; set; }
    }
    
    /// <summary>
    /// Water update request
    /// </summary>
    internal struct WaterUpdateRequest
    {
        public Vector3Int Position;
        public WaterUpdateType Type;
        public BlockType BlockType;
        public float NewValue;
    }
    
    /// <summary>
    /// Water update types
    /// </summary>
    internal enum WaterUpdateType
    {
        FlowUpdate,
        BlockAdded,
        BlockRemoved,
        TemperatureChange,
        SourceToggle
    }
    
    /// <summary>
    /// Water physics configuration
    /// </summary>
    public class WaterPhysicsConfig
    {
        public int UpdateIntervalMs { get; set; } = 100; // 10 FPS
        public int MaxUpdateTimeMs { get; set; } = 50;
        public int MaxQueuedUpdatesPerTick { get; set; } = 100;
        
        public float Gravity { get; set; } = 9.81f;
        public float WaterDensity { get; set; } = 1000f; // kg/m³
        public float AtmosphericPressure { get; set; } = 101325f; // Pa
        
        public float FlowRate { get; set; } = 0.1f;
        public float MaxFlowVelocity { get; set; } = 5f;
        public float MinFlowVelocity { get; set; } = 0.01f;
        public float MaxVerticalFlow { get; set; } = 2f;
        
        public float DefaultWaterDepth { get; set; } = 1f;
        public float MaxWaterDepth { get; set; } = 10f;
        public float DefaultWaterTemperature { get; set; } = 20f; // Celsius
        
        public bool EnableEvaporation { get; set; } = true;
        public float BaseEvaporationRate { get; set; } = 0.001f;
        public float MinPressureChange { get; set; } = 0.1f;
    }
    
    /// <summary>
    /// Water physics statistics
    /// </summary>
    public class WaterPhysicsStatistics
    {
        public int TotalWaterCells { get; set; }
        public int ActiveWaterCells { get; set; }
        public int SourceWaterCells { get; set; }
        public int StaticWaterCells { get; set; }
        public int ProcessedCellsPerTick { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public float AverageWaterLevel { get; set; }
        public float AveragePressure { get; set; }
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
        
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
        
        public static Vector3 operator *(Vector3 a, float scalar)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        public static Vector3 Normalize(Vector3 vector)
        {
            var length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            if (length < 0.0001f)
                return Vector3.Zero;
                
            return new Vector3(vector.X / length, vector.Y / length, vector.Z / length);
        }
        
        public static Vector3 Zero => new Vector3(0, 0, 0);
        
        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }
}
