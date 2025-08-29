using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Networking.Core;
using MinecraftProtocol;

namespace Minecraft.Core
{
    /// <summary>
    /// Network client for Minecraft-style game
    /// Provides advanced features like block management, chunk loading, and player actions.
    /// </summary>
    public class MinecraftNetworkClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 9000;
        [SerializeField] private float connectionTimeout = 10f;
        
        [Header("Game Settings")]
        [SerializeField] private int renderDistance = 8;
        [SerializeField] private float tickRate = 20f;
        
        private INetworkTransport _transport;
        private bool _isConnected = false;
        private PlayerInfo _playerInfo;
        private WorldInfo _worldInfo;
        
        private Dictionary<Vector2Int, ChunkInfo> _loadedChunks = new();
        private Dictionary<string, EntityInfo> _entities = new();
        
        private string _sessionToken;
        private float _lastHeartbeat;
        private Queue<IMessage> _outgoingMessages = new();
        private Queue<IMessage> _incomingMessages = new();
        
        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ErrorOccurred;
        public event Action<PlayerInfo> PlayerInfoUpdated;
        public event Action<ChunkInfo> ChunkLoaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        public event Action<EntityInfo> EntitySpawned;
        public event Action<string> EntityDespawned;
        public event Action<ChatMessage> ChatMessageReceived;
        
        public bool IsConnected => _isConnected;
        public PlayerInfo PlayerInfo => _playerInfo;
        public WorldInfo WorldInfo => _worldInfo;
        public string SessionToken => _sessionToken;
        public int LoadedChunkCount => _loadedChunks.Count;
        
        private void Awake()
        {
            InitializeClient();
        }
        
        private void Update()
        {
            ProcessIncomingMessages();
            ProcessOutgoingMessages();
            
            if (_isConnected && Time.time - _lastHeartbeat > 1f / tickRate)
            {
                SendHeartbeat();
                _lastHeartbeat = Time.time;
            }
        }
        
        private void InitializeClient()
        {
            _transport = new TcpNetworkTransport();
            _transport.ConnectionStatusChanged += OnConnectionStatusChanged;
            _transport.Received += OnDataReceived;
            
            Debug.Log("MinecraftNetworkClient initialized");
        }
        
        public async Task<bool> ConnectAsync()
        {
            try
            {
                Debug.Log($"Connecting to Minecraft server at {serverAddress}:{serverPort}...");
                
                var connectTask = _transport.ConnectAsync(serverAddress, serverPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(connectionTimeout));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    ErrorOccurred?.Invoke("Connection timed out");
                    return false;
                }
                
                await connectTask;
                
                Debug.Log("Successfully connected to Minecraft server");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to connect: {ex.Message}");
                return false;
            }
        }
        
        public void SendLogin(string username, string password)
        {
            var loginRequest = new LoginRequest
            {
                Username = username,
                Password = password,
                ClientVersion = Application.version
            };
            
            SendMessage(loginRequest);
            Debug.Log($"Sent login request for {username}");
        }
        
        public void RequestChunk(int chunkX, int chunkZ)
        {
            var chunkKey = new Vector2Int(chunkX, chunkZ);
            if (_loadedChunks.ContainsKey(chunkKey)) return;
            
            var request = new ChunkRequest
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                ViewDistance = renderDistance
            };
            
            SendMessage(request);
        }
        
        public void SendPlayerMove(Vector3 position, Vector3 rotation, bool isOnGround = true, 
            bool isSneaking = false, bool isSprinting = false, bool isFlying = false)
        {
            var moveRequest = new PlayerMoveRequest
            {
                Position = new MinecraftProtocol.Vector3 { X = position.x, Y = position.y, Z = position.z },
                Rotation = new MinecraftProtocol.Vector3 { X = rotation.x, Y = rotation.y, Z = rotation.z },
                IsOnGround = isOnGround,
                IsSneaking = isSneaking,
                IsSprinting = isSprinting,
                IsFlying = isFlying,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            SendMessage(moveRequest);
        }
        
        public void SendBlockChange(Vector3Int position, int newBlockId, int metadata = 0, 
            string blockEntityData = null, PlayerAction actionType = PlayerAction.PlaceBlock)
        {
            var request = new BlockChangeRequest
            {
                Position = new Vector3Int { X = position.x, Y = position.y, Z = position.z },
                NewBlockId = newBlockId,
                Metadata = metadata,
                BlockEntityData = blockEntityData ?? "",
                ActionType = actionType,
                Sequence = UnityEngine.Random.Range(1000, 9999)
            };
            
            SendMessage(request);
            Debug.Log($"Sent block change: {position} -> Block ID {newBlockId}");
        }
        
        private void SendMessage(IMessage message)
        {
            if (!_isConnected)
            {
                Debug.LogWarning($"Cannot send {message.GetType().Name}: not connected");
                return;
            }
            
            _outgoingMessages.Enqueue(message);
        }
        
        private void SendHeartbeat()
        {
            var ping = new PingRequest
            {
                ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            SendMessage(ping);
        }
        
        private void ProcessOutgoingMessages()
        {
            while (_outgoingMessages.Count > 0 && _isConnected)
            {
                var message = _outgoingMessages.Dequeue();
                try
                {
                    var data = SerializeMessage(message);
                    _transport.Send(new ArraySegment<byte>(data));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send message {message.GetType().Name}: {ex.Message}");
                }
            }
        }
        
        private void ProcessIncomingMessages()
        {
            while (_incomingMessages.Count > 0)
            {
                var message = _incomingMessages.Dequeue();
                try
                {
                    HandleIncomingMessage(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to handle message {message.GetType().Name}: {ex.Message}");
                }
            }
        }
        
        private byte[] SerializeMessage(IMessage message)
        {
            using var stream = new System.IO.MemoryStream();
            message.WriteTo(stream);
            return stream.ToArray();
        }
        
        private void OnDataReceived(ArraySegment<byte> data)
        {
            try
            {
                var message = DeserializeMessage(data);
                if (message != null)
                {
                    _incomingMessages.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize message: {ex.Message}");
            }
        }
        
        private IMessage DeserializeMessage(ArraySegment<byte> data)
        {
            using var stream = new System.IO.MemoryStream(data.Array, data.Offset, data.Count);
            
            try
            {
                stream.Position = 0;
                return LoginResponse.Parser.ParseFrom(stream);
            }
            catch { }
            
            try
            {
                stream.Position = 0;
                return ChunkResponse.Parser.ParseFrom(stream);
            }
            catch { }
            
            return null;
        }
        
        private void HandleIncomingMessage(IMessage message)
        {
            switch (message)
            {
                case LoginResponse loginResponse:
                    HandleLoginResponse(loginResponse);
                    break;
                    
                case ChunkResponse chunkResponse:
                    HandleChunkResponse(chunkResponse);
                    break;
                    
                case PingResponse pingResponse:
                    HandlePingResponse(pingResponse);
                    break;
                    
                default:
                    Debug.LogWarning($"Unhandled message type: {message.GetType().Name}");
                    break;
            }
        }
        
        private void HandleLoginResponse(LoginResponse response)
        {
            if (response.Success)
            {
                _sessionToken = response.SessionToken;
                _playerInfo = response.PlayerInfo;
                _worldInfo = response.WorldInfo;
                
                PlayerInfoUpdated?.Invoke(_playerInfo);
                
                Debug.Log($"Login successful: {response.Message}");
            }
            else
            {
                ErrorOccurred?.Invoke($"Login failed: {response.Message}");
            }
        }
        
        private void HandleChunkResponse(ChunkResponse response)
        {
            if (response.Success && response.ChunkData != null)
            {
                var chunkKey = new Vector2Int(response.ChunkData.ChunkX, response.ChunkData.ChunkZ);
                _loadedChunks[chunkKey] = response.ChunkData;
                
                ChunkLoaded?.Invoke(response.ChunkData);
                Debug.Log($"Loaded chunk ({response.ChunkData.ChunkX}, {response.ChunkData.ChunkZ})");
            }
        }
        
        private void HandlePingResponse(PingResponse response)
        {
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.ClientTimestamp;
        }
        
        private void OnConnectionStatusChanged(bool isConnected)
        {
            _isConnected = isConnected;
            ConnectionStatusChanged?.Invoke(isConnected);
            
            if (!isConnected)
            {
                _loadedChunks.Clear();
                _entities.Clear();
                _sessionToken = null;
                _playerInfo = null;
                _worldInfo = null;
            }
        }
        
        public bool IsChunkLoaded(int chunkX, int chunkZ)
        {
            return _loadedChunks.ContainsKey(new Vector2Int(chunkX, chunkZ));
        }
        
        public ChunkInfo GetChunk(int chunkX, int chunkZ)
        {
            _loadedChunks.TryGetValue(new Vector2Int(chunkX, chunkZ), out var chunk);
            return chunk;
        }
        
        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _transport.Received -= OnDataReceived;
                
                if (_transport is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}