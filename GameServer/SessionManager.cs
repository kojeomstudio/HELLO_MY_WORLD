using System.Collections.Concurrent;
using SharedProtocol;
using GameServerApp.Models;

namespace GameServerApp;

/// <summary>
/// Thread-safe helper to manage active sessions, user authentication, and player state.
/// </summary>
public class SessionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, PlayerState> _playerStates = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastHeartbeat = new();
    private readonly Timer _heartbeatTimer;

    public SessionManager()
    {
        _heartbeatTimer = new Timer(CheckHeartbeats, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Registers a session after successful authentication with enhanced player state tracking.
    /// </summary>
    public void Add(Session session)
    {
        if (string.IsNullOrEmpty(session.UserName))
            throw new InvalidOperationException("Session must have a user name before registration.");
        
        _sessions[session.UserName] = session;
        _lastHeartbeat[session.UserName] = DateTime.UtcNow;
        
        if (!_playerStates.ContainsKey(session.UserName))
        {
            _playerStates[session.UserName] = new PlayerState
            {
                UserName = session.UserName,
                LoginTime = DateTime.UtcNow,
                IsOnline = true,
                CurrentWorldId = 1,
                Position = new Vector3 { X = 0, Y = 100, Z = 0 }
            };
        }
        else
        {
            _playerStates[session.UserName].IsOnline = true;
            _playerStates[session.UserName].LoginTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes the session and updates player state when client disconnects.
    /// </summary>
    public void Remove(Session session)
    {
        if (!string.IsNullOrEmpty(session.UserName))
        {
            _sessions.TryRemove(session.UserName, out _);
            _lastHeartbeat.TryRemove(session.UserName, out _);
            
            if (_playerStates.TryGetValue(session.UserName, out var playerState))
            {
                playerState.IsOnline = false;
                playerState.LastSeenTime = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Retrieves the session by user name if connected and authenticated.
    /// </summary>
    public Session? GetSession(string name) => _sessions.TryGetValue(name, out var session) ? session : null;

    /// <summary>
    /// Gets player state information for active players.
    /// </summary>
    public PlayerState? GetPlayerState(string userName) => 
        _playerStates.TryGetValue(userName, out var state) ? state : null;

    /// <summary>
    /// Updates player position and state information.
    /// </summary>
    public void UpdatePlayerState(string userName, Vector3 position, float rotationY = 0f, float rotationX = 0f)
    {
        if (_playerStates.TryGetValue(userName, out var state))
        {
            state.Position = position;
            state.RotationY = rotationY;
            state.RotationX = rotationX;
            state.LastMoveTime = DateTime.UtcNow;
        }
        
        _lastHeartbeat[userName] = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates player's current world/chunk information.
    /// </summary>
    public void UpdatePlayerWorld(string userName, int worldId, int chunkX, int chunkZ)
    {
        if (_playerStates.TryGetValue(userName, out var state))
        {
            state.CurrentWorldId = worldId;
            state.CurrentChunkX = chunkX;
            state.CurrentChunkZ = chunkZ;
        }
    }

    /// <summary>
    /// Gets all players in a specific chunk for proximity-based operations.
    /// </summary>
    public IEnumerable<string> GetPlayersInChunk(int worldId, int chunkX, int chunkZ)
    {
        return _playerStates
            .Where(kvp => kvp.Value.IsOnline && 
                         kvp.Value.CurrentWorldId == worldId &&
                         kvp.Value.CurrentChunkX == chunkX && 
                         kvp.Value.CurrentChunkZ == chunkZ)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets all players within a certain distance from a position.
    /// </summary>
    public IEnumerable<string> GetPlayersInRange(int worldId, Vector3 position, double range)
    {
        return _playerStates
            .Where(kvp => kvp.Value.IsOnline && 
                         kvp.Value.CurrentWorldId == worldId &&
                         CalculateDistance(kvp.Value.Position, position) <= range)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Broadcasts a message to all players in a specific area.
    /// </summary>
    public async Task BroadcastToAreaAsync<T>(int worldId, int chunkX, int chunkZ, 
        MessageType messageType, T message) where T : class
    {
        var playersInArea = GetPlayersInChunk(worldId, chunkX, chunkZ);
        var tasks = new List<Task>();
        
        foreach (var playerName in playersInArea)
        {
            var session = GetSession(playerName);
            if (session != null)
            {
                tasks.Add(session.SendAsync(messageType, message));
            }
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Broadcasts a message to all connected players.
    /// </summary>
    public async Task BroadcastToAllAsync<T>(MessageType messageType, T message) where T : class
    {
        var tasks = new List<Task>();
        
        foreach (var session in _sessions.Values)
        {
            tasks.Add(session.SendAsync(messageType, message));
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Validates session token and ensures player is authenticated.
    /// </summary>
    public bool ValidateSession(Session session)
    {
        return !string.IsNullOrEmpty(session.SessionToken) && 
               !string.IsNullOrEmpty(session.UserName) &&
               _sessions.ContainsKey(session.UserName);
    }

    /// <summary>
    /// Updates heartbeat timestamp for connection monitoring.
    /// </summary>
    public void UpdateHeartbeat(string userName)
    {
        _lastHeartbeat[userName] = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a snapshot of currently connected user names.
    /// </summary>
    public IReadOnlyCollection<string> ConnectedUsers => _sessions.Keys.ToList();

    /// <summary>
    /// Gets total count of online players.
    /// </summary>
    public int OnlinePlayerCount => _sessions.Count;

    private void CheckHeartbeats(object? state)
    {
        var timeout = TimeSpan.FromMinutes(5);
        var cutoffTime = DateTime.UtcNow - timeout;
        var disconnectedPlayers = new List<string>();
        
        foreach (var kvp in _lastHeartbeat)
        {
            if (kvp.Value < cutoffTime)
            {
                disconnectedPlayers.Add(kvp.Key);
            }
        }
        
        foreach (var playerName in disconnectedPlayers)
        {
            if (_sessions.TryGetValue(playerName, out var session))
            {
                Console.WriteLine($"Player {playerName} timed out due to inactivity");
                Remove(session);
                session.Disconnect();
            }
        }
    }

    private double CalculateDistance(Vector3 pos1, Vector3 pos2)
    {
        var dx = pos1.X - pos2.X;
        var dy = pos1.Y - pos2.Y;
        var dz = pos1.Z - pos2.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public void Dispose()
    {
        _heartbeatTimer?.Dispose();
    }
}

/// <summary>
/// Represents the current state of a player in the game world.
/// </summary>
public class PlayerState
{
    public string UserName { get; set; } = string.Empty;
    public Vector3 Position { get; set; } = new();
    public float RotationY { get; set; }
    public float RotationX { get; set; }
    public int CurrentWorldId { get; set; } = 1;
    public int CurrentChunkX { get; set; }
    public int CurrentChunkZ { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastMoveTime { get; set; }
    public DateTime LastSeenTime { get; set; }
    public bool IsFlying { get; set; }
    public bool IsSneaking { get; set; }
    public bool IsSprinting { get; set; }
    public int GameMode { get; set; }
    public int Health { get; set; } = 100;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
}

/// <summary>
/// Simple 3D vector for position tracking.
/// </summary>
public class Vector3
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    
    public Vector3() { }
    
    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
