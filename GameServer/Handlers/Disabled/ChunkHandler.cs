using GameServerApp.Database;
using GameServerApp.World;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// Handles chunk loading requests and manages chunk data distribution to clients.
/// </summary>
public class ChunkHandler : MessageHandler<ChunkRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly WorldManager _worldManager;

    public ChunkHandler(DatabaseHelper database, SessionManager sessions, WorldManager worldManager) 
        : base(MessageType.ChunkRequest)
    {
        _database = database;
        _sessions = sessions;
        _worldManager = worldManager;
    }

    protected override async Task HandleAsync(Session session, ChunkRequest message)
    {
        try
        {
            if (!_sessions.ValidateSession(session))
            {
                await SendErrorResponse(session, "Invalid session");
                return;
            }

            var playerState = _sessions.GetPlayerState(session.UserName!);
            if (playerState == null)
            {
                await SendErrorResponse(session, "Player state not found");
                return;
            }

            var chunkData = await _worldManager.GetChunkAsync(message.ChunkX, message.ChunkZ);
            if (chunkData == null)
            {
                await SendErrorResponse(session, "Failed to generate chunk");
                return;
            }

            _sessions.UpdatePlayerWorld(session.UserName!, playerState.CurrentWorldId, 
                message.ChunkX, message.ChunkZ);

            var response = new ChunkResponse
            {
                ChunkX = message.ChunkX,
                ChunkZ = message.ChunkZ,
                BlockData = ConvertBlockData(chunkData),
                BiomeData = ConvertBiomeData(chunkData),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.ChunkResponse, response);
            Console.WriteLine($"Sent chunk ({message.ChunkX}, {message.ChunkZ}) to {session.UserName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling chunk request from {session.UserName}: {ex.Message}");
            await SendErrorResponse(session, "Internal server error");
        }
    }

    private async Task SendErrorResponse(Session session, string errorMessage)
    {
        var response = new ChunkResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.ChunkResponse, response);
    }

    private List<BlockInfo> ConvertBlockData(ChunkData chunkData)
    {
        var blocks = new List<BlockInfo>();
        
        for (int y = 0; y < 256; y++)
        {
            for (int z = 0; z < 16; z++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var blockType = chunkData.GetBlock(x, y, z);
                    if (blockType != World.BlockType.Air)
                    {
                        blocks.Add(new BlockInfo
                        {
                            Position = new Position { X = x, Y = y, Z = z },
                            BlockType = (int)blockType
                        });
                    }
                }
            }
        }
        
        return blocks;
    }

    private List<int> ConvertBiomeData(ChunkData chunkData)
    {
        var biomes = new List<int>();
        
        for (int z = 0; z < 16; z++)
        {
            for (int x = 0; x < 16; x++)
            {
                biomes.Add((int)chunkData.GetBiome(x, z));
            }
        }
        
        return biomes;
    }
}

/// <summary>
/// Handles chunk unload requests to optimize server memory usage.
/// </summary>
public class ChunkUnloadHandler : MessageHandler<ChunkUnloadRequest>
{
    private readonly SessionManager _sessions;
    private readonly WorldManager _worldManager;

    public ChunkUnloadHandler(SessionManager sessions, WorldManager worldManager) 
        : base(MessageType.ChunkUnloadRequest)
    {
        _sessions = sessions;
        _worldManager = worldManager;
    }

    protected override async Task HandleAsync(Session session, ChunkUnloadRequest message)
    {
        try
        {
            if (!_sessions.ValidateSession(session))
            {
                return;
            }

            Console.WriteLine($"Player {session.UserName} requested unload of chunk ({message.ChunkX}, {message.ChunkZ})");
            
            var response = new ChunkUnloadResponse
            {
                ChunkX = message.ChunkX,
                ChunkZ = message.ChunkZ,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.ChunkUnloadResponse, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling chunk unload request: {ex.Message}");
        }
    }
}