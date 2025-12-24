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
        
        // Player position tracking for movement synchronization
        private readonly ConcurrentDictionary<string, PlayerPositionState> _playerPositions = new();
        
        // World change queue for batch processing
        private readonly Queue<WorldChangeRecord> _worldChangeQueue = new();
        private readonly object _queueLock = new object();

        // Configuration
        private readonly int _syncBatchSize = 50;
        private readonly int _syncIntervalMs = 100;
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
                    OriginPlayerId = originSession.UserName
                });
            }

            // Process the block change immediately for the origin player
            await ProcessImmediateBlockChange(request, originSession);
        }

        /// <summary>
        /// Updates player position and broadcasts to nearby players
        /// </summary>
        public async Task UpdatePlayerPositionAsync(string playerId, Vector3 position, Vector3 rotation)
        {
            var oldState = _playerPositions.GetOrAdd(playerId, _ => new PlayerPositionState());
            var oldChunkKey = GetChunkKey(
                (int)oldState.Position.X / 16, 
                (int)oldState.Position.Z / 16);
            var newChunkKey = GetChunkKey(
                (int)position.X / 16, 
                (int)position.Z / 16);

            // Update player state
            oldState.Position = position;
            oldState.Rotation = rotation;
            oldState.LastUpdate = DateTimeOffset.UtcNow;

            // Handle chunk transitions
            if (oldChunkKey != newChunkKey)
            {
                await HandlePlayerChunkTransition(playerId, oldChunkKey, newChunkKey);
            }

            // Broadcast to nearby players
            await BroadcastPlayerPosition(playerId, position, rotation, newChunkKey);
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
                .Select(c => (WorldBlockChangeRequest)c.Data)
                .ToList();

            if (blockChanges.Count > 0)
            {
                await BroadcastBlockChanges(blockChanges);
            }

            // Clean up old chunk trackers
            await CleanupOldChunkTrackers();
        }

        /// <summary>
        /// Sends initial world data to a newly connected player
        /// </summary>
        public async Task SendInitialWorldDataAsync(Session session, Vector3 spawnPosition)
        {
            var playerChunkX = (int)spawnPosition.X / 16;
            var playerChunkZ = (int)spawnPosition.Z / 16;
            var loadRadius = 8; // Load 8x8 chunks around spawn

            var chunkTasks = new List<Task>();
            
            for (int x = playerChunkX - loadRadius; x <= playerChunkX + loadRadius; x++)
            {
                for (int z = playerChunkZ - loadRadius; z <= playerChunkZ + loadRadius; z++)
                {
                    chunkTasks.Add(SendChunkToPlayer(session, x, z));
                }
            }

            await Task.WhenAll(chunkTasks);

            // Send initial player position
            await UpdatePlayerPositionAsync(session.UserName!, spawnPosition, Vector3.Zero);
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

        private async Task BroadcastBlockChanges(List<WorldBlockChangeRequest> changes)
        {
            // Group changes by chunk for efficient broadcasting
            var chunkGroups = changes.GroupBy(c => $"{c.BlockPosition.X / 16}_{c.BlockPosition.Z / 16}");

            foreach (var group in chunkGroups)
            {
                var chunkX = group.First().BlockPosition.X / 16;
                var chunkZ = group.First().BlockPosition.Z / 16;
                var roomId = _roomManager.GetRoomIdForChunk(chunkX, chunkZ);

                if (!string.IsNullOrEmpty(roomId))
                {
                    var broadcast = new WorldBlockChangeBatchBroadcast
                    {
                        AreaId = "world",
                        SubworldId = "overworld",
                        Changes = group.Select(c => new WorldBlockChangeData
                        {
                            Position = c.BlockPosition,
                            BlockType = c.BlockType,
                            ChunkType = c.ChunkType
                        }).ToList(),
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    await _roomManager.BroadcastToRoomAsync(roomId, MessageType.WorldBlockChangeBatchBroadcast, broadcast);
                }
            }
        }

        private async Task BroadcastPlayerPosition(string playerId, Vector3 position, Vector3 rotation, string chunkKey)
        {
            var positionUpdate = new PlayerPositionUpdate
            {
                PlayerId = playerId,
                Position = position,
                Rotation = rotation,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // Find players in nearby chunks
            var nearbyChunks = GetNearbyChunkKeys(chunkKey, 2); // 2 chunk radius
            var nearbyPlayers = _playerPositions
                .Where(p => nearbyChunks.Contains(GetChunkKey(
                    (int)p.Value.Position.X / 16, 
                    (int)p.Value.Position.Z / 16)) && p.Key != playerId)
                .Select(p => p.Key)
                .ToList();

            foreach (var nearbyPlayerId in nearbyPlayers)
            {
                var session = _sessionManager.GetSession(nearbyPlayerId);
                if (session != null)
                {
                    await session.SendAsync(MessageType.PlayerPositionUpdate, positionUpdate);
                }
            }
        }

        private async Task HandlePlayerChunkTransition(string playerId, string oldChunkKey, string newChunkKey)
        {
            // Unload old chunks
            var oldNearbyChunks = GetNearbyChunkKeys(oldChunkKey, 8);
            var newNearbyChunks = GetNearbyChunkKeys(newChunkKey, 8);
            var chunksToUnload = oldNearbyChunks.Except(newNearbyChunks);

            foreach (var chunkToUnload in chunksToUnload)
            {
                var session = _sessionManager.GetSession(playerId);
                if (session != null)
                {
                    var coords = ParseChunkKey(chunkToUnload);
                    var unloadMessage = new ChunkUnloadMessage
                    {
                        ChunkX = coords.x,
                        ChunkZ = coords.z,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    await session.SendAsync(MessageType.ChunkUnload, unloadMessage);
                }
            }

            // Load new chunks
            var chunksToLoad = newNearbyChunks.Except(oldNearbyChunks);
            foreach (var chunkToLoad in chunksToLoad)
            {
                var coords = ParseChunkKey(chunkToLoad);
                var session = _sessionManager.GetSession(playerId);
                if (session != null)
                {
                    await SendChunkToPlayer(session, coords.x, coords.z);
                }
            }
        }

        private async Task SendChunkToPlayer(Session session, int chunkX, int chunkZ)
        {
            try
            {
                var chunkData = await _worldManager.GetChunkAsync(chunkX, chunkZ);
                if (chunkData != null)
                {
                    var chunkMessage = new ChunkDataMessage
                    {
                        ChunkX = chunkX,
                        ChunkZ = chunkZ,
                        BlockData = chunkData.ToBytes().blockData,
                        BiomeData = chunkData.ToBytes().biomeData,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    await session.SendAsync(MessageType.ChunkData, chunkMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending chunk ({chunkX}, {chunkZ}) to player {session.UserName}: {ex.Message}");
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
        private readonly HashSet<Vector3> _changedBlocks = new();

        public ChunkUpdateTracker(int chunkX, int chunkZ)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            LastUpdate = DateTimeOffset.UtcNow;
        }

        public void RecordBlockChange(Vector3 position, BlockType blockType)
        {
            _changedBlocks.Add(position);
            LastUpdate = DateTimeOffset.UtcNow;
        }

        public bool HasChanges() => _changedBlocks.Count > 0;
        public void ClearChanges() => _changedBlocks.Clear();
    }

    /// <summary>
    /// Tracks player position state for movement synchronization
    /// </summary>
    internal class PlayerPositionState
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public DateTimeOffset LastUpdate { get; set; }
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
