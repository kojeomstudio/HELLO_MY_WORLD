using GameServerApp.Database;
using GameServerApp.World;
using SharedProtocol;

namespace GameServerApp.Handlers;

/// <summary>
/// Enhanced player movement handler with Minecraft-style movement and synchronization.
/// </summary>
public class PlayerMoveHandler : MessageHandler<PlayerMoveRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly WorldManager _worldManager;

    public PlayerMoveHandler(DatabaseHelper database, SessionManager sessions, WorldManager worldManager) 
        : base(MessageType.PlayerMoveRequest)
    {
        _database = database;
        _sessions = sessions;
        _worldManager = worldManager;
    }

    protected override async Task HandleAsync(Session session, PlayerMoveRequest message)
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

            if (!ValidateMovement(message))
            {
                await SendErrorResponse(session, "Invalid movement data");
                return;
            }

            var newPosition = new Vector3(message.Position.X, message.Position.Y, message.Position.Z);
            var rotationY = message.Rotation?.Y ?? 0f;
            var rotationX = message.Rotation?.X ?? 0f;

            _sessions.UpdatePlayerState(session.UserName!, newPosition, rotationY, rotationX);

            var chunkX = (int)Math.Floor(message.Position.X / 16);
            var chunkZ = (int)Math.Floor(message.Position.Z / 16);
            
            if (chunkX != playerState.CurrentChunkX || chunkZ != playerState.CurrentChunkZ)
            {
                _sessions.UpdatePlayerWorld(session.UserName!, playerState.CurrentWorldId, chunkX, chunkZ);
                
                await NotifyPlayersInArea(session.UserName!, message, chunkX, chunkZ);
            }
            else
            {
                await BroadcastMovementToNearbyPlayers(session.UserName!, message, chunkX, chunkZ);
            }

            var response = new PlayerMoveResponse
            {
                Success = true,
                Position = message.Position,
                Rotation = message.Rotation,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.PlayerMoveResponse, response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling player move for {session.UserName}: {ex.Message}");
            await SendErrorResponse(session, "Internal server error");
        }
    }

    private bool ValidateMovement(PlayerMoveRequest message)
    {
        if (message.Position == null)
            return false;

        if (Math.Abs(message.Position.X) > 30000000 || 
            Math.Abs(message.Position.Z) > 30000000 ||
            message.Position.Y < -64 || message.Position.Y > 320)
            return false;

        return true;
    }

    private async Task NotifyPlayersInArea(string playerName, PlayerMoveRequest message, int chunkX, int chunkZ)
    {
        var nearbyPlayers = _sessions.GetPlayersInRange(1, 
            new Vector3(message.Position.X, message.Position.Y, message.Position.Z), 100.0);

        var notification = new PlayerEnterAreaNotification
        {
            PlayerName = playerName,
            Position = message.Position,
            Rotation = message.Rotation,
            ChunkX = chunkX,
            ChunkZ = chunkZ,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var tasks = new List<Task>();
        foreach (var nearbyPlayerName in nearbyPlayers)
        {
            if (nearbyPlayerName != playerName)
            {
                var nearbySession = _sessions.GetSession(nearbyPlayerName);
                if (nearbySession != null)
                {
                    tasks.Add(nearbySession.SendAsync(MessageType.PlayerEnterAreaNotification, notification));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task BroadcastMovementToNearbyPlayers(string playerName, PlayerMoveRequest message, int chunkX, int chunkZ)
    {
        var nearbyPlayers = _sessions.GetPlayersInRange(1,
            new Vector3(message.Position.X, message.Position.Y, message.Position.Z), 50.0);

        var broadcast = new PlayerMoveBroadcast
        {
            PlayerName = playerName,
            Position = message.Position,
            Rotation = message.Rotation,
            MovementType = message.MovementType,
            IsFlying = message.IsFlying,
            IsSneaking = message.IsSneaking,
            IsSprinting = message.IsSprinting,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var tasks = new List<Task>();
        foreach (var nearbyPlayerName in nearbyPlayers)
        {
            if (nearbyPlayerName != playerName)
            {
                var nearbySession = _sessions.GetSession(nearbyPlayerName);
                if (nearbySession != null)
                {
                    tasks.Add(nearbySession.SendAsync(MessageType.PlayerMoveBroadcast, broadcast));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendErrorResponse(Session session, string errorMessage)
    {
        var response = new PlayerMoveResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.PlayerMoveResponse, response);
    }
}

/// <summary>
/// Handles block change requests with full server-client synchronization.
/// </summary>
public class BlockChangeHandler : MessageHandler<BlockChangeRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly WorldManager _worldManager;

    public BlockChangeHandler(DatabaseHelper database, SessionManager sessions, WorldManager worldManager)
        : base(MessageType.BlockChangeRequest)
    {
        _database = database;
        _sessions = sessions;
        _worldManager = worldManager;
    }

    protected override async Task HandleAsync(Session session, BlockChangeRequest message)
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

            if (!ValidateBlockChange(message, playerState))
            {
                await SendErrorResponse(session, "Invalid block change");
                return;
            }

            var chunkX = message.Position.X / 16;
            var chunkZ = message.Position.Z / 16;
            if (message.Position.X < 0) chunkX--;
            if (message.Position.Z < 0) chunkZ--;

            var blockType = (World.BlockType)message.BlockType;
            await _worldManager.UpdateBlockAsync(chunkX, chunkZ,
                message.Position.X, message.Position.Y, message.Position.Z,
                blockType, 1);

            var response = new BlockChangeResponse
            {
                Success = true,
                Position = message.Position,
                BlockType = message.BlockType,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.BlockChangeResponse, response);

            await BroadcastBlockChange(session.UserName!, message, chunkX, chunkZ);

            Console.WriteLine($"Block changed by {session.UserName} at ({message.Position.X}, {message.Position.Y}, {message.Position.Z}) to type {blockType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling block change for {session.UserName}: {ex.Message}");
            await SendErrorResponse(session, "Internal server error");
        }
    }

    private bool ValidateBlockChange(BlockChangeRequest message, PlayerState playerState)
    {
        if (message.Position == null)
            return false;

        var distance = Math.Sqrt(
            Math.Pow(message.Position.X - playerState.Position.X, 2) +
            Math.Pow(message.Position.Y - playerState.Position.Y, 2) +
            Math.Pow(message.Position.Z - playerState.Position.Z, 2));

        return distance <= 10.0;
    }

    private async Task BroadcastBlockChange(string playerName, BlockChangeRequest message, int chunkX, int chunkZ)
    {
        var playersInChunk = _sessions.GetPlayersInChunk(1, chunkX, chunkZ);

        var broadcast = new BlockChangeBroadcast
        {
            PlayerName = playerName,
            Position = message.Position,
            BlockType = message.BlockType,
            ChangeType = message.ChangeType,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var tasks = new List<Task>();
        foreach (var playerInChunk in playersInChunk)
        {
            if (playerInChunk != playerName)
            {
                var playerSession = _sessions.GetSession(playerInChunk);
                if (playerSession != null)
                {
                    tasks.Add(playerSession.SendAsync(MessageType.BlockChangeBroadcast, broadcast));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendErrorResponse(Session session, string errorMessage)
    {
        var response = new BlockChangeResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendAsync(MessageType.BlockChangeResponse, response);
    }
}