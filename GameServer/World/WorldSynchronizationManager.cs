using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameServerApp.Database;
using GameServerApp.Models;
using GameServerApp.World;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.World
{
    /// <summary>
    /// Enhanced world synchronization manager that handles chunk updates, player positions,
    /// and world state synchronization between server and clients.
    /// </summary>
    public class WorldSynchronizationManager
    {
        private readonly WorldManager _worldManager;
        private readonly SessionManager _sessionManager;
        private readonly DatabaseHelper _database;
        private readonly Rooms.RoomManager _roomManager;

        // Chunk update tracking for efficient synchronization
        private readonly ConcurrentDictionary<string, ChunkUpdateTracker> _chunkUpdateTrackers = new();
        
        // World change queue for batch processing
        private readonly Queue<WorldChangeRecord> _worldChangeQueue = new();
        private readonly object _queueLock = new object();

        // Configuration
        private readonly int _syncBatchSize = 50;
        private readonly int _chunkUnloadDelayMs = 30000;

        public WorldSynchronizationManager(
            WorldManager worldManager, 
            SessionManager sessionManager, 
            DatabaseHelper database,
            Rooms.RoomManager roomManager)
        {
            _worldManager = worldManager;
            _sessionManager = sessionManager;
            _database = database;
            _roomManager = roomManager;
        }

        /// <summary>
        /// Processes a block change and queues it for synchronization
        /// </summary>
        public async Task ProcessBlockChangeAsync(WorldBlockChangeRequest request, Session originSession)
        {
            var chunkX = request.BlockPosition.X / 16;
            var chunkZ = request.BlockPosition.Z / 16;
            var chunkKey = GetChunkKey(chunkX, chunkZ);

            // Track the chunk update
            var tracker = _chunkUpdateTrackers.GetOrAdd(chunkKey, _ => new ChunkUpdateTracker(chunkX, chunkZ));
            tracker.RecordBlockChange(request.BlockPosition, (BlockType)request.BlockType);

            // Queue the world change
            lock (_queueLock)
            {
                _worldChangeQueue.Enqueue(new WorldChangeRecord
                {
                    Type = WorldChangeType.BlockChange,
                    Data = request,
                    Timestamp = DateTimeOffset.UtcNow,
                    OriginPlayerId = originSession.UserName ?? originSession.SessionToken ?? string.Empty
                });
            }

            // Process the block change immediately for the origin player
            await ProcessImmediateBlockChange(request, originSession);
        }

        /// <summary>
        /// Processes queued world changes in batches
        /// </summary>
        public async Task ProcessWorldChangeQueueAsync()
        {
            List<WorldChangeRecord> changesToProcess;
            
            lock (_queueLock)
            {
                changesToProcess = new List<WorldChangeRecord>();
                while (changesToProcess.Count < _syncBatchSize && _worldChangeQueue.Count > 0)
                {
                    changesToProcess.Add(_worldChangeQueue.Dequeue());
                }
            }

            if (changesToProcess.Count == 0) return;

            // Group changes by type for efficient processing
            var blockChanges = changesToProcess
                .Where(c => c.Type == WorldChangeType.BlockChange)
                .ToList();

            if (blockChanges.Count > 0)
            {
                await BroadcastBlockChanges(blockChanges);
            }

            // Clean up old chunk trackers
            await CleanupOldChunkTrackers();
        }

        private async Task ProcessImmediateBlockChange(WorldBlockChangeRequest request, Session originSession)
        {
            var chunkX = request.BlockPosition.X / 16;
            var chunkZ = request.BlockPosition.Z / 16;
            
            if (request.BlockPosition.X < 0) chunkX--;
            if (request.BlockPosition.Z < 0) chunkZ--;

            var playerId = 1; // TODO: Get actual player ID
            var blockType = (BlockType)request.BlockType;
            
            await _worldManager.UpdateBlockAsync(chunkX, chunkZ, 
                request.BlockPosition.X, request.BlockPosition.Y, request.BlockPosition.Z,
                blockType, playerId);
        }

        private async Task BroadcastBlockChanges(IEnumerable<WorldChangeRecord> changes)
        {
            foreach (var record in changes)
            {
                if (record.Data is not WorldBlockChangeRequest request)
                {
                    continue;
                }
                var roomId = _roomManager.GetPlayerRoomId(record.OriginPlayerId ?? string.Empty);
                if (string.IsNullOrEmpty(roomId))
                {
                    continue;
                }

                var broadcast = new WorldBlockChangeBroadcast
                {
                    AreaId = request.AreaId,
                    SubworldId = request.SubworldId,
                    BlockPosition = request.BlockPosition,
                    BlockType = request.BlockType,
                    ChunkType = request.ChunkType,
                    PlayerId = record.OriginPlayerId ?? "Unknown",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await _roomManager.BroadcastToRoomAsync(roomId, MessageType.WorldBlockChangeBroadcast, broadcast);
            }
        }

        private async Task CleanupOldChunkTrackers()
        {
            var cutoffTime = DateTimeOffset.UtcNow.AddMilliseconds(-_chunkUnloadDelayMs);
            var keysToRemove = _chunkUpdateTrackers
                .Where(kvp => kvp.Value.LastUpdate < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _chunkUpdateTrackers.TryRemove(key, out _);
            }
        }

        private static string GetChunkKey(int x, int z) => $"{x}_{z}";
        private static (int x, int z) ParseChunkKey(string key)
        {
            var parts = key.Split('_');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private static List<string> GetNearbyChunkKeys(string centerKey, int radius)
        {
            var (centerX, centerZ) = ParseChunkKey(centerKey);
            var nearbyKeys = new List<string>();

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    nearbyKeys.Add(GetChunkKey(x, z));
                }
            }

            return nearbyKeys;
        }
    }

    /// <summary>
    /// Tracks changes to a specific chunk for efficient synchronization
    /// </summary>
    internal class ChunkUpdateTracker
    {
        public int ChunkX { get; }
        public int ChunkZ { get; }
        public DateTimeOffset LastUpdate { get; private set; }
        private readonly HashSet<Vector3Int> _changedBlocks = new();

        public ChunkUpdateTracker(int chunkX, int chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            LastUpdate = DateTimeOffset.UtcNow;
        }

        public void RecordBlockChange(Vector3Int position, BlockType blockType)
        {
            _changedBlocks.Add(position);
            LastUpdate = DateTimeOffset.UtcNow;
        }

        public bool HasChanges() => _changedBlocks.Count > 0;
        public void ClearChanges() => _changedBlocks.Clear();
    }

    /// <summary>
    /// Represents a world change record
    /// </summary>
    internal class WorldChangeRecord
    {
        public WorldChangeType Type { get; set; }
        public object Data { get; set; } = null!;
        public DateTimeOffset Timestamp { get; set; }
        public string OriginPlayerId { get; set; } = null!;
    }

    /// <summary>
    /// Types of world changes
    /// </summary>
    internal enum WorldChangeType
    {
        BlockChange,
        EntitySpawn,
        EntityDespawn,
        WeatherChange,
        TimeChange
    }
}
