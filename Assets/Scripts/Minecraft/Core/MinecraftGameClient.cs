using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using Networking.Core;
using SharedProtocol;

namespace Minecraft.Core
{
    /// <summary>
    /// Enhanced Minecraft game client that properly integrates with the server
    /// using SharedProtocol messages and the Session protocol
    /// </summary>
    public class MinecraftGameClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 9000;
        [SerializeField] private float connectionTimeout = 10f;
        
        [Header("Game Settings")]
        [SerializeField] private int renderDistance = 8;
        [SerializeField] private float networkTickRate = 20f;
        
        private INetworkTransport _transport;
        private bool _isConnected = false;
        private string _sessionToken;
        private float _lastNetworkUpdate;
        
        // Player and World State
        private PlayerStateInfo _playerState;
        private Dictionary<Vector2Int, ChunkDataResponseMessage> _loadedChunks = new();
        private Dictionary<string, EntityInfo> _entities = new();
        
        // Message queues for thread safety
        private Queue<object> _outgoingMessages = new();
        private Queue<object> _incomingMessages = new();
        
        // Events for UI and game systems
        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ErrorOccurred;
        public event Action<PlayerStateInfo> PlayerStateUpdated;
        public event Action<ChunkDataResponseMessage> ChunkLoaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        public event Action<EntityInfo> EntitySpawned;
        public event Action<string> EntityDespawned;
        public event Action<ChatMessage> ChatMessageReceived;
        
        // Public properties
        public bool IsConnected => _isConnected;
        public PlayerStateInfo PlayerState => _playerState;
        public string SessionToken => _sessionToken;
        public int LoadedChunkCount => _loadedChunks.Count;
        
        private void Awake()
        {
            InitializeClient();
        }
        
        private void Update()
        {
            ProcessMessageQueues();
            
            if (_isConnected && Time.time - _lastNetworkUpdate > 1f / networkTickRate)
            {
                SendHeartbeat();
                _lastNetworkUpdate = Time.time;
            }
        }
        
        private void InitializeClient()
        {
            _transport = new TcpNetworkTransport();
            _transport.ConnectionStatusChanged += OnTransportConnectionChanged;
            _transport.Received += OnDataReceived;
            
            Debug.Log("MinecraftGameClient initialized with SharedProtocol");
        }
        
        #region Connection Management
        
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
        
        public async Task DisconnectAsync()
        {
            try
            {
                if (_transport != null)
                {
                    await _transport.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during disconnect: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Message Sending
        
        public void SendLogin(string username, string password)
        {
            var loginMessage = new LoginMessage
            {
                UserName = username,
                Password = password
            };
            
            SendMessage(MessageType.Login, loginMessage);
            Debug.Log($"Sent login request for {username}");
        }
        
        public void SendPlayerStateUpdate(Vector3 position, Vector3 rotation, bool isOnGround = true, 
            bool isSneaking = false, bool isSprinting = false, bool isFlying = false)
        {
            var stateUpdate = new PlayerStateUpdateMessage
            {
                Position = new Vector3F { X = position.x, Y = position.y, Z = position.z },
                Rotation = new Vector3F { X = rotation.x, Y = rotation.y, Z = rotation.z },
                IsOnGround = isOnGround,
                IsSneaking = isSneaking,
                IsSprinting = isSprinting,
                IsFlying = isFlying,
                PlayerId = _sessionToken ?? "unknown"
            };
            
            SendMinecraftMessage(MinecraftMessageType.PlayerStateUpdate, stateUpdate);
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
            
            SendMinecraftMessage(MinecraftMessageType.PlayerActionRequest, request);
            Debug.Log($"Sent player action: {action} at {targetPos}");
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
            
            SendMinecraftMessage(MinecraftMessageType.ChunkDataRequest, request);
            Debug.Log($"Requested chunk ({chunkX}, {chunkZ})");
        }
        
        public void SendChatMessage(string message)
        {
            var chatMessage = new ChatMessage
            {
                Sender = _playerState?.PlayerId ?? "unknown",
                Content = message,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            SendMessage(MessageType.Chat, chatMessage);
        }
        
        private void SendHeartbeat()
        {
            var ping = new PingMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ClientId = _sessionToken ?? "unknown"
            };
            
            SendMessage(MessageType.Ping, ping);
        }
        
        private void SendMessage(MessageType messageType, object message)
        {
            if (!_isConnected)
            {
                Debug.LogWarning($"Cannot send {message.GetType().Name}: not connected");
                return;
            }
            
            _outgoingMessages.Enqueue(new { MessageType = messageType, Message = message });
        }
        
        private void SendMinecraftMessage(MinecraftMessageType messageType, object message)
        {
            if (!_isConnected)
            {
                Debug.LogWarning($"Cannot send {message.GetType().Name}: not connected");
                return;
            }
            
            _outgoingMessages.Enqueue(new { MessageType = messageType, Message = message, IsMinecraftMessage = true });
        }
        
        #endregion
        
        #region Message Processing
        
        private void ProcessMessageQueues()
        {
            // Process outgoing messages
            while (_outgoingMessages.Count > 0 && _isConnected)
            {
                var messageWrapper = _outgoingMessages.Dequeue();
                try
                {
                    SendMessageToTransport(messageWrapper);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send message: {ex.Message}");
                }
            }
            
            // Process incoming messages
            while (_incomingMessages.Count > 0)
            {
                var message = _incomingMessages.Dequeue();
                try
                {
                    HandleIncomingMessage(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to handle message {message?.GetType().Name}: {ex.Message}");
                }
            }
        }
        
        private void SendMessageToTransport(object messageWrapper)
        {
            using var stream = new MemoryStream();
            
            // Use the Session protocol format: [MessageType][MessageData]
            var wrapper = messageWrapper as dynamic;
            var messageType = wrapper.MessageType;
            var message = wrapper.Message;
            var isMinecraftMessage = wrapper.IsMinecraftMessage ?? false;
            
            // Write message type
            if (isMinecraftMessage)
            {
                stream.WriteByte((byte)messageType); // MinecraftMessageType
            }
            else
            {
                stream.WriteByte((byte)messageType); // MessageType
            }
            
            // Serialize and write message data
            using var messageStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(messageStream, message);
            var messageBytes = messageStream.ToArray();
            
            stream.Write(messageBytes, 0, messageBytes.Length);
            
            var data = stream.ToArray();
            _transport.Send(new ArraySegment<byte>(data));
        }
        
        private void OnDataReceived(ArraySegment<byte> data)
        {
            try
            {
                using var stream = new MemoryStream(data.Array, data.Offset, data.Count);
                
                // Read message type
                var messageTypeByte = stream.ReadByte();
                if (messageTypeByte == -1) return;
                
                // Determine if it's a regular message or Minecraft message
                var remainingBytes = new byte[data.Count - 1];
                stream.Read(remainingBytes, 0, remainingBytes.Length);
                
                var message = DeserializeMessage((MessageType)messageTypeByte, remainingBytes);
                if (message != null)
                {
                    _incomingMessages.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process received data: {ex.Message}");
            }
        }
        
        private object DeserializeMessage(MessageType messageType, byte[] data)
        {
            using var stream = new MemoryStream(data);
            
            try
            {
                return messageType switch
                {
                    MessageType.Login => ProtoBuf.Serializer.Deserialize<LoginResponseMessage>(stream),
                    MessageType.Chat => ProtoBuf.Serializer.Deserialize<ChatMessage>(stream),
                    MessageType.Ping => ProtoBuf.Serializer.Deserialize<PingMessage>(stream),
                    
                    // Try to deserialize as Minecraft messages
                    _ => TryDeserializeMinecraftMessage((MinecraftMessageType)messageType, data)
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize message type {messageType}: {ex.Message}");
                return null;
            }
        }
        
        private object TryDeserializeMinecraftMessage(MinecraftMessageType messageType, byte[] data)
        {
            using var stream = new MemoryStream(data);
            
            try
            {
                return messageType switch
                {
                    MinecraftMessageType.PlayerStateUpdate => ProtoBuf.Serializer.Deserialize<PlayerStateUpdateMessage>(stream),
                    MinecraftMessageType.PlayerActionResponse => ProtoBuf.Serializer.Deserialize<PlayerActionResponseMessage>(stream),
                    MinecraftMessageType.ChunkDataResponse => ProtoBuf.Serializer.Deserialize<ChunkDataResponseMessage>(stream),
                    MinecraftMessageType.BlockChangeNotification => ProtoBuf.Serializer.Deserialize<BlockChangeNotificationMessage>(stream),
                    MinecraftMessageType.EntitySpawn => ProtoBuf.Serializer.Deserialize<EntitySpawnMessage>(stream),
                    MinecraftMessageType.EntityDespawn => ProtoBuf.Serializer.Deserialize<EntityDespawnMessage>(stream),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize Minecraft message type {messageType}: {ex.Message}");
                return null;
            }
        }
        
        private void HandleIncomingMessage(object message)
        {
            switch (message)
            {
                case LoginResponseMessage loginResponse:
                    HandleLoginResponse(loginResponse);
                    break;
                    
                case PlayerStateUpdateMessage stateUpdate:
                    HandlePlayerStateUpdate(stateUpdate);
                    break;
                    
                case ChunkDataResponseMessage chunkResponse:
                    HandleChunkResponse(chunkResponse);
                    break;
                    
                case PlayerActionResponseMessage actionResponse:
                    HandlePlayerActionResponse(actionResponse);
                    break;
                    
                case BlockChangeNotificationMessage blockChange:
                    HandleBlockChange(blockChange);
                    break;
                    
                case EntitySpawnMessage entitySpawn:
                    HandleEntitySpawn(entitySpawn);
                    break;
                    
                case EntityDespawnMessage entityDespawn:
                    HandleEntityDespawn(entityDespawn);
                    break;
                    
                case ChatMessage chatMessage:
                    HandleChatMessage(chatMessage);
                    break;
                    
                case PingMessage pingResponse:
                    HandlePingResponse(pingResponse);
                    break;
                    
                default:
                    Debug.LogWarning($"Unhandled message type: {message?.GetType().Name}");
                    break;
            }
        }
        
        #endregion
        
        #region Message Handlers
        
        private void HandleLoginResponse(LoginResponseMessage response)
        {
            if (response.Success)
            {
                _sessionToken = response.SessionToken;
                _playerState = response.PlayerState;
                
                PlayerStateUpdated?.Invoke(_playerState);
                Debug.Log($"Login successful: {response.Message}");
            }
            else
            {
                ErrorOccurred?.Invoke($"Login failed: {response.Message}");
            }
        }
        
        private void HandlePlayerStateUpdate(PlayerStateUpdateMessage stateUpdate)
        {
            if (_playerState != null && stateUpdate.PlayerId == _playerState.PlayerId)
            {
                // Update our own player state
                _playerState.Position = stateUpdate.Position;
                _playerState.Health = stateUpdate.Health;
                _playerState.Hunger = stateUpdate.Hunger;
                _playerState.IsOnGround = stateUpdate.IsOnGround;
                _playerState.IsSneaking = stateUpdate.IsSneaking;
                _playerState.IsSprinting = stateUpdate.IsSprinting;
                _playerState.IsFlying = stateUpdate.IsFlying;
                
                PlayerStateUpdated?.Invoke(_playerState);
            }
        }
        
        private void HandleChunkResponse(ChunkDataResponseMessage response)
        {
            if (response.Success)
            {
                var chunkKey = new Vector2Int(response.ChunkX, response.ChunkZ);
                _loadedChunks[chunkKey] = response;
                
                ChunkLoaded?.Invoke(response);
                Debug.Log($"Loaded chunk ({response.ChunkX}, {response.ChunkZ}) with {response.Blocks?.Count ?? 0} blocks");
            }
            else
            {
                Debug.LogWarning($"Failed to load chunk: {response.Message}");
            }
        }
        
        private void HandlePlayerActionResponse(PlayerActionResponseMessage response)
        {
            if (response.Success)
            {
                Debug.Log($"Player action {response.Action} completed successfully");
            }
            else
            {
                Debug.LogWarning($"Player action {response.Action} failed: {response.Message}");
            }
        }
        
        private void HandleBlockChange(BlockChangeNotificationMessage blockChange)
        {
            if (blockChange.Blocks != null)
            {
                foreach (var block in blockChange.Blocks)
                {
                    var pos = new Vector3Int(block.Position.X, block.Position.Y, block.Position.Z);
                    BlockChanged?.Invoke(pos, 0, block.BlockId); // oldBlockId not available
                }
            }
        }
        
        private void HandleEntitySpawn(EntitySpawnMessage entitySpawn)
        {
            if (entitySpawn.Entity != null)
            {
                _entities[entitySpawn.Entity.EntityId] = entitySpawn.Entity;
                EntitySpawned?.Invoke(entitySpawn.Entity);
            }
        }
        
        private void HandleEntityDespawn(EntityDespawnMessage entityDespawn)
        {
            _entities.Remove(entityDespawn.EntityId);
            EntityDespawned?.Invoke(entityDespawn.EntityId);
        }
        
        private void HandleChatMessage(ChatMessage chatMessage)
        {
            ChatMessageReceived?.Invoke(chatMessage);
        }
        
        private void HandlePingResponse(PingMessage pingResponse)
        {
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pingResponse.Timestamp;
            Debug.Log($"Network latency: {latency}ms");
        }
        
        #endregion
        
        #region Public API
        
        public bool IsChunkLoaded(int chunkX, int chunkZ)
        {
            return _loadedChunks.ContainsKey(new Vector2Int(chunkX, chunkZ));
        }
        
        public ChunkDataResponseMessage GetChunk(int chunkX, int chunkZ)
        {
            _loadedChunks.TryGetValue(new Vector2Int(chunkX, chunkZ), out var chunk);
            return chunk;
        }
        
        public IEnumerable<ChunkDataResponseMessage> GetLoadedChunks()
        {
            return _loadedChunks.Values;
        }
        
        public EntityInfo GetEntity(string entityId)
        {
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnTransportConnectionChanged(bool isConnected)
        {
            _isConnected = isConnected;
            ConnectionStatusChanged?.Invoke(isConnected);
            
            if (!isConnected)
            {
                // Clear state on disconnect
                _loadedChunks.Clear();
                _entities.Clear();
                _sessionToken = null;
                _playerState = null;
                
                Debug.Log("Disconnected from server - state cleared");
            }
        }
        
        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.ConnectionStatusChanged -= OnTransportConnectionChanged;
                _transport.Received -= OnDataReceived;
                
                if (_transport is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        
        #endregion
    }
}