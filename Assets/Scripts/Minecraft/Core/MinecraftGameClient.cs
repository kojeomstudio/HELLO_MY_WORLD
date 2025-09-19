using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Networking.Core;
using SharedProtocol;
using Minecraft.World;

namespace Minecraft.Core
{
    /// <summary>
    /// High level Minecraft-style game client that speaks the SharedProtocol framing
    /// and surfaces gameplay events (chunks, entities, chat, etc.) to Unity systems.
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
        private bool _isConnected;
        private string _sessionToken = string.Empty;
        private float _lastNetworkUpdate;

        private PlayerStateInfo _playerState = new();
        private readonly Dictionary<Vector2Int, ChunkSnapshot> _loadedChunks = new();
        private readonly Dictionary<string, EntityInfo> _entities = new();

        private readonly Queue<OutgoingMessage> _outgoingMessages = new();
        private readonly Queue<object> _incomingMessages = new();

        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ErrorOccurred;
        public event Action<PlayerStateInfo> PlayerStateUpdated;
        public event Action<ChunkSnapshot> ChunkLoaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        public event Action<EntityInfo> EntitySpawned;
        public event Action<string> EntityDespawned;
        public event Action<ChatMessage> ChatMessageReceived;

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
            ProcessOutgoingMessages();
            ProcessIncomingMessages();

            if (_isConnected && networkTickRate > 0f && Time.time - _lastNetworkUpdate >= 1f / networkTickRate)
            {
                SendHeartbeat();
                _lastNetworkUpdate = Time.time;
            }
        }

        private void OnDestroy()
        {
            if (_transport != null)
            {
                _transport.ConnectionStatusChanged -= OnTransportConnectionChanged;
                _transport.Received -= OnDataReceived;
                _transport.Dispose();
            }
        }

        private void InitializeClient()
        {
            _transport = new TcpNetworkTransport();
            _transport.ConnectionStatusChanged += OnTransportConnectionChanged;
            _transport.Received += OnDataReceived;

            Debug.Log("MinecraftGameClient ready (SharedProtocol framing)");
        }

        #region Connection Management

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var connectTask = _transport.ConnectAsync(serverAddress, serverPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(connectionTimeout));

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    ErrorOccurred?.Invoke("Connection timed out");
                    return false;
                }

                await connectTask;
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
                await _transport.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Disconnect error: {ex.Message}");
            }
        }

        private void OnTransportConnectionChanged(bool isConnected)
        {
            _isConnected = isConnected;

            if (!isConnected)
            {
                _sessionToken = string.Empty;
                _playerState = new PlayerStateInfo();
                _loadedChunks.Clear();
                _entities.Clear();
                _outgoingMessages.Clear();
                _incomingMessages.Clear();
            }

            ConnectionStatusChanged?.Invoke(isConnected);
        }

        #endregion

        #region Message Sending

        public void SendLogin(string username, string password, string clientVersion = "1.0.0")
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password,
                ClientVersion = clientVersion
            };

            EnqueueMessage((int)MessageType.LoginRequest, request);
            Debug.Log($"Sent login request for {username}");
        }

        public void SendPlayerStateUpdate(Vector3 position, Vector3 rotation, float movementSpeed,
            bool isOnGround = true, bool isSneaking = false, bool isSprinting = false, bool isFlying = false)
        {
            if (!_isConnected) return;

            var clampedSpeed = Mathf.Clamp(movementSpeed, 0.1f, 10f);
            var moveRequest = new MoveRequest
            {
                TargetPosition = new SharedProtocol.Vector3(position.x, position.y, position.z),
                MovementSpeed = clampedSpeed
            };

            EnqueueMessage((int)MessageType.MoveRequest, moveRequest);

            UpdateLocalPlayerState(position, rotation, isOnGround, isSneaking, isSprinting, isFlying);
        }

        public void SendPlayerAction(PlayerActionType action, Vector3Int targetPos, int face, Vector3 cursorPos, ItemInfo selectedItem = null)
        {
            var request = new PlayerActionRequestMessage
            {
                Action = action,
                TargetPosition = new Vector3I { X = targetPos.x, Y = targetPos.y, Z = targetPos.z },
                Face = face,
                CursorPosition = new Vector3D { X = cursorPos.x, Y = cursorPos.y, Z = cursorPos.z },
                SelectedItem = selectedItem
            };

            EnqueueMessage((int)MinecraftMessageType.PlayerActionRequest, request);
        }

        public void RequestChunk(int chunkX, int chunkZ)
        {
            var request = new ChunkDataRequestMessage
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                ViewDistance = renderDistance
            };

            EnqueueMessage((int)MinecraftMessageType.ChunkDataRequest, request);
        }

        public void SendChatMessage(string message, ChatType chatType = ChatType.Global, string targetPlayer = "")
        {
            var request = new ChatRequest
            {
                Message = message,
                Type = (int)chatType,
                TargetPlayer = targetPlayer
            };

            EnqueueMessage((int)MessageType.ChatRequest, request);
        }

        private void SendHeartbeat()
        {
            if (!_isConnected) return;

            var ping = new PingRequest
            {
                ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            EnqueueMessage((int)MessageType.PingRequest, ping);
        }

        private void EnqueueMessage(int typeCode, object payload)
        {
            if (!_isConnected)
            {
                Debug.LogWarning($"Cannot send {payload.GetType().Name}: not connected");
                return;
            }

            _outgoingMessages.Enqueue(new OutgoingMessage(typeCode, payload));
        }

        #endregion

        #region Message Queues

        private void ProcessOutgoingMessages()
        {
            while (_isConnected && _outgoingMessages.Count > 0)
            {
                var message = _outgoingMessages.Dequeue();
                try
                {
                    SendMessageToTransport(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send message ({message.Payload.GetType().Name}): {ex.Message}");
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
                    Debug.LogError($"Failed to handle message {message?.GetType().Name}: {ex.Message}");
                }
            }
        }

        private void SendMessageToTransport(OutgoingMessage message)
        {
            using var payloadStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(payloadStream, message.Payload);
            var payload = payloadStream.ToArray();

            var typeBytes = BitConverter.GetBytes(message.TypeCode);
            var framed = new byte[typeBytes.Length + payload.Length];
            Buffer.BlockCopy(typeBytes, 0, framed, 0, typeBytes.Length);
            Buffer.BlockCopy(payload, 0, framed, typeBytes.Length, payload.Length);

            _transport.Send(new ArraySegment<byte>(framed));
        }

        private void OnDataReceived(ArraySegment<byte> data)
        {
            try
            {
                var buffer = new byte[data.Count];
                Buffer.BlockCopy(data.Array!, data.Offset, buffer, 0, data.Count);

                if (buffer.Length < sizeof(int))
                {
                    Debug.LogWarning("Received payload smaller than header");
                    return;
                }

                var typeCode = BitConverter.ToInt32(buffer, 0);
                var payload = new byte[buffer.Length - sizeof(int)];
                if (payload.Length > 0)
                {
                    Buffer.BlockCopy(buffer, sizeof(int), payload, 0, payload.Length);
                }

                var message = DeserializeMessage(typeCode, payload);
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

        private object DeserializeMessage(int typeCode, byte[] payload)
        {
            using var stream = new MemoryStream(payload);

            try
            {
                if (Enum.IsDefined(typeof(MessageType), typeCode))
                {
                    var messageType = (MessageType)typeCode;
                    return messageType switch
                    {
                        MessageType.LoginResponse => ProtoBuf.Serializer.Deserialize<LoginResponse>(stream),
                        MessageType.MoveResponse => ProtoBuf.Serializer.Deserialize<MoveResponse>(stream),
                        MessageType.ChatResponse => ProtoBuf.Serializer.Deserialize<ChatResponse>(stream),
                        MessageType.ChatMessage => ProtoBuf.Serializer.Deserialize<ChatMessage>(stream),
                        MessageType.PingResponse => ProtoBuf.Serializer.Deserialize<PingResponse>(stream),
                        MessageType.WorldBlockChangeBroadcast => ProtoBuf.Serializer.Deserialize<WorldBlockChangeBroadcast>(stream),
                        MessageType.WorldBlockChangeResponse => ProtoBuf.Serializer.Deserialize<WorldBlockChangeResponse>(stream),
                        MessageType.PlayerInfoUpdate => ProtoBuf.Serializer.Deserialize<PlayerInfoUpdate>(stream),
                        _ => null
                    };
                }

                if (Enum.IsDefined(typeof(MinecraftMessageType), typeCode))
                {
                    var minecraftType = (MinecraftMessageType)typeCode;
                    return minecraftType switch
                    {
                        MinecraftMessageType.PlayerActionResponse => ProtoBuf.Serializer.Deserialize<PlayerActionResponseMessage>(stream),
                        MinecraftMessageType.ChunkDataResponse => ProtoBuf.Serializer.Deserialize<ChunkDataResponseMessage>(stream),
                        MinecraftMessageType.BlockChangeNotification => ProtoBuf.Serializer.Deserialize<BlockChangeNotificationMessage>(stream),
                        MinecraftMessageType.EntitySpawn => ProtoBuf.Serializer.Deserialize<EntitySpawnMessage>(stream),
                        MinecraftMessageType.EntityDespawn => ProtoBuf.Serializer.Deserialize<EntityDespawnMessage>(stream),
                        _ => null
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize message type {typeCode}: {ex.Message}");
                return null;
            }

            Debug.LogWarning($"Unknown message type received: {typeCode}");
            return null;
        }

        #endregion

        #region Message Handlers

        private void HandleIncomingMessage(object message)
        {
            switch (message)
            {
                case LoginResponse loginResponse:
                    HandleLoginResponse(loginResponse);
                    break;
                case MoveResponse moveResponse:
                    HandleMoveResponse(moveResponse);
                    break;
                case PlayerInfoUpdate infoUpdate:
                    HandlePlayerInfoUpdate(infoUpdate);
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
                case WorldBlockChangeBroadcast worldBlockChange:
                    HandleWorldBlockBroadcast(worldBlockChange);
                    break;
                case ChatMessage chatMessage:
                    ChatMessageReceived?.Invoke(chatMessage);
                    break;
                case ChatResponse chatResponse:
                    HandleChatResponse(chatResponse);
                    break;
                case EntitySpawnMessage spawnMessage:
                    HandleEntitySpawn(spawnMessage);
                    break;
                case EntityDespawnMessage despawnMessage:
                    HandleEntityDespawn(despawnMessage);
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
            if (!response.Success)
            {
                ErrorOccurred?.Invoke(string.IsNullOrWhiteSpace(response.Message) ? "Login failed" : response.Message);
                return;
            }

            _sessionToken = response.SessionToken ?? string.Empty;
            if (response.PlayerInfo != null)
            {
                _playerState = ConvertToPlayerStateInfo(response.PlayerInfo);
                PlayerStateUpdated?.Invoke(_playerState);
            }
        }

        private void HandleMoveResponse(MoveResponse response)
        {
            if (!response.Success || response.NewPosition == null) return;

            _playerState.Position = new Vector3D
            {
                X = response.NewPosition.X,
                Y = response.NewPosition.Y,
                Z = response.NewPosition.Z
            };
        }

        private void HandlePlayerInfoUpdate(PlayerInfoUpdate update)
        {
            if (update.PlayerInfo == null) return;
            _playerState = ConvertToPlayerStateInfo(update.PlayerInfo);
            PlayerStateUpdated?.Invoke(_playerState);
        }

        private void HandleChunkResponse(ChunkDataResponseMessage response)
        {
            var chunkKey = new Vector2Int(response.ChunkX, response.ChunkZ);
            var blocks = ChunkCompression.DecodeBlocks(response.CompressedBlockData);
            var entities = response.Entities ?? new List<EntityInfo>();
            var snapshot = new ChunkSnapshot(response.ChunkX, response.ChunkZ, blocks, response.BiomeData, entities, response.IsFromCache);

            _loadedChunks[chunkKey] = snapshot;
            ChunkLoaded?.Invoke(snapshot);
        }

        private void HandlePlayerActionResponse(PlayerActionResponseMessage response)
        {
            if (!response.Success && !string.IsNullOrEmpty(response.Message))
            {
                ErrorOccurred?.Invoke(response.Message);
            }
        }

        private void HandleBlockChange(BlockChangeNotificationMessage message)
        {
            var position = new Vector3Int(message.Position.X, message.Position.Y, message.Position.Z);
            var previousBlockId = UpdateLocalChunkCache(position, message.NewBlockId);
            var oldId = message.OldBlockId != 0 ? message.OldBlockId : previousBlockId;
            BlockChanged?.Invoke(position, oldId, message.NewBlockId);
        }

        private void HandleWorldBlockBroadcast(WorldBlockChangeBroadcast message)
        {
            if (message.BlockPosition == null) return;
            var pos = new Vector3Int(message.BlockPosition.X, message.BlockPosition.Y, message.BlockPosition.Z);
            var previous = UpdateLocalChunkCache(pos, message.BlockType);
            BlockChanged?.Invoke(pos, previous, message.BlockType);
        }

        private void HandleChatResponse(ChatResponse response)
        {
            if (!response.Success && !string.IsNullOrEmpty(response.ErrorMessage))
            {
                ErrorOccurred?.Invoke(response.ErrorMessage);
            }
        }

        private void HandleEntitySpawn(EntitySpawnMessage message)
        {
            if (message.Entity == null || string.IsNullOrEmpty(message.Entity.EntityId)) return;
            _entities[message.Entity.EntityId] = message.Entity;
            EntitySpawned?.Invoke(message.Entity);
        }

        private void HandleEntityDespawn(EntityDespawnMessage message)
        {
            if (string.IsNullOrEmpty(message.EntityId)) return;
            _entities.Remove(message.EntityId);
            EntityDespawned?.Invoke(message.EntityId);
        }

        private void HandlePingResponse(PingResponse response)
        {
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.ClientTimestamp;
            Debug.Log($"Ping: {latency} ms");
        }

        private void UpdateLocalPlayerState(Vector3 position, Vector3 rotation, bool isOnGround,
            bool isSneaking, bool isSprinting, bool isFlying)
        {
            _playerState ??= new PlayerStateInfo();
            _playerState.Position = new Vector3D { X = position.x, Y = position.y, Z = position.z };
            _playerState.Rotation = new Vector3D { X = rotation.x, Y = rotation.y, Z = rotation.z };
            _playerState.IsOnGround = isOnGround;
            _playerState.IsSneaking = isSneaking;
            _playerState.IsSprinting = isSprinting;
            _playerState.IsFlying = isFlying;
        }

        private PlayerStateInfo ConvertToPlayerStateInfo(PlayerInfo info)
        {
            var state = new PlayerStateInfo
            {
                PlayerId = info.PlayerId,
                Username = info.Username,
                Position = info.Position != null ? new Vector3D { X = info.Position.X, Y = info.Position.Y, Z = info.Position.Z } : new Vector3D(),
                Rotation = new Vector3D(),
                Level = info.Level,
                Experience = 0,
                Health = info.Health,
                MaxHealth = info.MaxHealth,
                Hunger = 20f,
                MaxHunger = 20f,
                GameMode = GameMode.Survival,
                SelectedSlot = 0
            };

            if (info.Inventory != null)
            {
                state.Inventory = info.Inventory.Select(ConvertInventoryItem).ToList();
            }

            if (info.Inventory != null && info.Inventory.Count > 0)
            {
                state.HeldItem = ConvertInventoryItem(info.Inventory[0]);
            }

            return state;
        }

        private InventoryItemInfo ConvertInventoryItem(InventoryItem item)
        {
            return new InventoryItemInfo
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                Durability = item.Durability,
                MaxDurability = item.MaxDurability,
                ItemType = ItemType.Block
            };
        }

        #endregion

        #region Public Accessors

        public ChunkSnapshot GetChunk(int chunkX, int chunkZ)
        {
            var key = new Vector2Int(chunkX, chunkZ);
            return _loadedChunks.TryGetValue(key, out var chunk) ? chunk : null;
        }

        public IEnumerable<ChunkSnapshot> GetLoadedChunks()
        {
            return _loadedChunks.Values;
        }

        #endregion

        private readonly struct OutgoingMessage
        {
            public OutgoingMessage(int typeCode, object payload)
            {
                TypeCode = typeCode;
                Payload = payload;
            }

            public int TypeCode { get; }
            public object Payload { get; }
        }

        private int UpdateLocalChunkCache(Vector3Int worldPosition, int newBlockId)
        {
            var chunkSize = ChunkSnapshot.ChunkSize;
            int chunkX = Mathf.FloorToInt(worldPosition.x / (float)chunkSize);
            int chunkZ = Mathf.FloorToInt(worldPosition.z / (float)chunkSize);
            var key = new Vector2Int(chunkX, chunkZ);

            if (!_loadedChunks.TryGetValue(key, out var chunk))
            {
                return 0;
            }

            int localX = worldPosition.x - chunkX * chunkSize;
            int localZ = worldPosition.z - chunkZ * chunkSize;

            var previous = chunk.GetBlockId(localX, worldPosition.y, localZ);
            chunk.SetBlockId(localX, worldPosition.y, localZ, newBlockId);
            return previous;
        }
    }
}
