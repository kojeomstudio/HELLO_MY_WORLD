using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Networking.Core;
using SharedProtocol;
using System.IO;

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
        private PlayerStateInfo _playerInfo;
        private Dictionary<Vector2Int, ChunkDataResponseMessage> _loadedChunks = new();
        private Dictionary<string, EntityInfo> _entities = new();
        
        private string _sessionToken;
        private float _lastHeartbeat;
        private Queue<object> _outgoingMessages = new();
        private Queue<object> _incomingMessages = new();
        
        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ErrorOccurred;
        public event Action<PlayerStateInfo> PlayerInfoUpdated;
        public event Action<ChunkDataResponseMessage> ChunkLoaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        public event Action<EntityInfo> EntitySpawned;
        public event Action<string> EntityDespawned;
        public event Action<ChatMessage> ChatMessageReceived;
        
        public bool IsConnected => _isConnected;
        public PlayerStateInfo PlayerInfo => _playerInfo;
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
            var loginRequest = new LoginMessage
            {
                UserName = username,
                Password = password
            };
            
            SendMessage(loginRequest);
            Debug.Log($"Sent login request for {username}");
        }
        
        public void RequestChunk(int chunkX, int chunkZ)
        {
            var chunkKey = new Vector2Int(chunkX, chunkZ);
            if (_loadedChunks.ContainsKey(chunkKey)) return;
            
            var request = new ChunkDataRequestMessage
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ
            };
            
            SendMessage(request);
        }
        
        public void SendPlayerMove(Vector3 position, Vector3 rotation, bool isOnGround = true, 
            bool isSneaking = false, bool isSprinting = false, bool isFlying = false)
        {
            var stateUpdate = new PlayerStateUpdateMessage
            {
                Position = new Vector3F { X = position.x, Y = position.y, Z = position.z },
                Rotation = new Vector3F { X = rotation.x, Y = rotation.y, Z = rotation.z },
                IsOnGround = isOnGround,
                IsSneaking = isSneaking,
                IsSprinting = isSprinting,
                IsFlying = isFlying
            };
            
            SendMessage(stateUpdate);
        }
        
        public void SendPlayerAction(PlayerActionType action, Vector3Int targetPos, int face, Vector3 cursorPos, ItemInfo selectedItem = null)
        {
            var request = new PlayerActionRequestMessage
            {
                Action = action,
                TargetPosition = new Vector3I { X = targetPos.x, Y = targetPos.y, Z = targetPos.z },
                Face = face,
                CursorPosition = new Vector3F { X = cursorPos.x, Y = cursorPos.y, Z = cursorPos.z },
                SelectedItem = selectedItem
            };
            
            SendMessage(request);
            Debug.Log($"Sent player action: {action} at {targetPos}");
        }
        
        private void SendMessage(object message)
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
            var ping = new PingMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ClientId = _sessionToken ?? "unknown"
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
        
        private byte[] SerializeMessage(object message)
        {
            using var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, message);
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
        
        private object DeserializeMessage(ArraySegment<byte> data)
        {
            using var stream = new MemoryStream(data.Array, data.Offset, data.Count);
            
            try
            {
                stream.Position = 0;
                return ProtoBuf.Serializer.Deserialize<LoginResponseMessage>(stream);
            }
            catch { }
            
            try
            {
                stream.Position = 0;
                return ProtoBuf.Serializer.Deserialize<ChunkDataResponseMessage>(stream);
            }
            catch { }
            
            try
            {
                stream.Position = 0;
                return ProtoBuf.Serializer.Deserialize<PlayerStateUpdateMessage>(stream);
            }
            catch { }
            
            return null;
        }
        
        private void HandleIncomingMessage(object message)
        {
            switch (message)
            {
                case LoginResponseMessage loginResponse:
                    HandleLoginResponse(loginResponse);
                    break;
                    
                case ChunkDataResponseMessage chunkResponse:
                    HandleChunkResponse(chunkResponse);
                    break;
                    
                case PlayerStateUpdateMessage stateUpdate:
                    HandlePlayerStateUpdate(stateUpdate);
                    break;
                    
                case PingMessage pingResponse:
                    HandlePingResponse(pingResponse);
                    break;
                    
                default:
                    Debug.LogWarning($"Unhandled message type: {message.GetType().Name}");
                    break;
            }
        }
        
        private void HandleLoginResponse(LoginResponseMessage response)
        {
            if (response.Success)
            {
                _sessionToken = response.SessionToken;
                _playerInfo = response.PlayerState;
                
                PlayerInfoUpdated?.Invoke(_playerInfo);
                
                Debug.Log($"Login successful: {response.Message}");
            }
            else
            {
                ErrorOccurred?.Invoke($"Login failed: {response.Message}");
            }
        }
        
        private void HandleChunkResponse(ChunkDataResponseMessage response)
        {
            if (response.Success)
            {
                var chunkKey = new Vector2Int(response.ChunkX, response.ChunkZ);
                _loadedChunks[chunkKey] = response;
                
                ChunkLoaded?.Invoke(response);
                Debug.Log($"Loaded chunk ({response.ChunkX}, {response.ChunkZ})");
            }
        }
        
        private void HandlePingResponse(PingMessage response)
        {
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.Timestamp;
            Debug.Log($"Network latency: {latency}ms");
        }
        
        private void HandlePlayerStateUpdate(PlayerStateUpdateMessage stateUpdate)
        {
            if (stateUpdate.PlayerId != _playerInfo?.PlayerId)
            {
                Debug.Log($"Received state update for other player: {stateUpdate.PlayerId}");
                return;
            }
            
            if (_playerInfo != null)
            {
                _playerInfo.Position = stateUpdate.Position;
                _playerInfo.Health = stateUpdate.Health;
                _playerInfo.Hunger = stateUpdate.Hunger;
                _playerInfo.IsOnGround = stateUpdate.IsOnGround;
                _playerInfo.IsSneaking = stateUpdate.IsSneaking;
                _playerInfo.IsSprinting = stateUpdate.IsSprinting;
                _playerInfo.IsFlying = stateUpdate.IsFlying;
                
                PlayerInfoUpdated?.Invoke(_playerInfo);
            }
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
            }
        }
        
        public bool IsChunkLoaded(int chunkX, int chunkZ)
        {
            return _loadedChunks.ContainsKey(new Vector2Int(chunkX, chunkZ));
        }
        
        public ChunkDataResponseMessage GetChunk(int chunkX, int chunkZ)
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