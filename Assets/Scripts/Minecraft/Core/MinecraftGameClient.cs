using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using Networking.Core;
using SharedProtocol;
using Minecraft.Inventory;
using Minecraft.Player;
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

        [Header("Server Status")]
        [SerializeField] private bool autoRefreshServerStatus = true;
        [SerializeField] private float serverStatusRefreshInterval = 15f;

        private INetworkTransport _transport;
        private bool _isConnected;
        private string _sessionToken = string.Empty;
        private float _lastNetworkUpdate;

        private PlayerStateInfo _playerState = new();
        private readonly Dictionary<Vector2Int, ChunkSnapshot> _loadedChunks = new();
        private readonly HashSet<Vector2Int> _pendingChunkRequests = new();
        private readonly List<Vector2Int> _chunksToUnload = new();
        private readonly Dictionary<string, EntityInfo> _entities = new();
        private readonly Dictionary<string, RecipeData> _knownRecipes = new();
        private readonly Dictionary<string, RoomInfo> _knownRooms = new();
        private ClientInventorySnapshot _inventorySnapshot = ClientInventorySnapshot.Empty;
        private readonly List<ItemInfo> _inventoryItems = new();
        private readonly Dictionary<string, int> _itemIdLookup = new(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<OutgoingMessage> _outgoingMessages = new();
        private readonly Queue<object> _incomingMessages = new();

        private float _nextServerStatusRequestTime;
        private ServerStatusResponse _latestServerStatus;
        private TimeUpdateMessage _lastTimeUpdate;
        private WeatherChangeMessage _lastWeatherChange;

        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ErrorOccurred;
        public event Action<PlayerStateInfo> PlayerStateUpdated;
        public event Action<PlayerRespawnBroadcast> PlayerRespawned;
        public event Action<PlayerDeathMessage> PlayerDeathNotified;

        public event Action<CombatEventMessage> CombatEventReceived;
        public event Action<ChunkSnapshot> ChunkLoaded;
        public event Action<Vector2Int, ChunkSnapshot> ChunkUnloaded;
        public event Action<Vector3Int, int, int> BlockChanged;
        public event Action<Vector3Int, IReadOnlyList<ItemDropInfo>> BlockDropsReceived;
        public event Action<EntityInfo> EntitySpawned;
        public event Action<EntityInfo> EntityUpdated;
        public event Action<string> EntityDespawned;
        public event Action<ChatMessage> ChatMessageReceived;
        public event Action<IReadOnlyList<RecipeData>> RecipeListReceived;
        public event Action<CraftingResponse> CraftingCompleted;
        public event Action<IReadOnlyList<RoomInfo>> RoomListReceived;
        public event Action<RoomEnterResponse> RoomEntered;
        public event Action<RoomLeaveResponse> RoomLeft;
        public event Action<RoomQueueUpdateMessage> RoomQueueUpdated;
        public event Action<RoomPromotionMessage> RoomPromotionReceived;
        public event Action<ServerStatusResponse> ServerStatusReceived;
        public event Action<IReadOnlyList<ItemInfo>> InventoryItemsUpdated;
        public event Action<TimeUpdateMessage> TimeUpdated;
        public event Action<WeatherChangeMessage> WeatherChanged;
        public event Action<ContainerOpenResponseMessage> ContainerOpened;
        public event Action<ContainerUpdateBroadcastMessage> ContainerUpdated;
        public event Action<ContainerCloseNotificationMessage> ContainerClosed;

        public bool IsConnected => _isConnected;
        public PlayerStateInfo PlayerState => _playerState;
        public string SessionToken => _sessionToken;
        public ServerStatusResponse LatestServerStatus => _latestServerStatus;
        public int LoadedChunkCount => _loadedChunks.Count;
        public long CurrentWorldTime => _lastTimeUpdate != null ? _lastTimeUpdate.WorldTime : 0;
        public long CurrentDayTime => _lastTimeUpdate != null ? _lastTimeUpdate.DayTime : 0;
        public WeatherType CurrentWeather => _lastWeatherChange != null ? _lastWeatherChange.WeatherType : WeatherType.Clear;

        public bool TryGetLastTimeSnapshot(out TimeUpdateMessage snapshot)
        {
            snapshot = _lastTimeUpdate;
            return snapshot != null;
        }

        public bool TryGetLastWeatherSnapshot(out WeatherChangeMessage snapshot)
        {
            snapshot = _lastWeatherChange;
            return snapshot != null;
        }

        public bool TryGetEntity(string entityId, out EntityInfo entity)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                entity = null;
                return false;
            }

            if (_entities.TryGetValue(entityId, out var stored))
            {
                entity = CloneEntity(stored);
                return true;
            }

            entity = null;
            return false;
        }

        public IReadOnlyCollection<EntityInfo> GetEntitySnapshot()
        {
            if (_entities.Count == 0)
            {
                return Array.Empty<EntityInfo>();
            }

            return _entities.Values.Select(CloneEntity).ToArray();
        }

        private void Awake()
        {
            InitializeClient();
        }

        private void Update()
        {
            ProcessOutgoingMessages();
            ProcessIncomingMessages();

            if (_isConnected)
            {
                EvaluateChunkResidency();
            }

            if (_isConnected && !string.IsNullOrEmpty(_sessionToken) && autoRefreshServerStatus && serverStatusRefreshInterval > 0f)
            {
                if (Time.time >= _nextServerStatusRequestTime)
                {
                    RequestServerStatus();
                }
            }

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
                _pendingChunkRequests.Clear();
                _chunksToUnload.Clear();
                _entities.Clear();
                _outgoingMessages.Clear();
                _incomingMessages.Clear();
                _nextServerStatusRequestTime = 0f;
                _latestServerStatus = null;
                _lastTimeUpdate = null;
                _lastWeatherChange = null;
                ServerStatusReceived?.Invoke(null);
                TimeUpdated?.Invoke(null);
                WeatherChanged?.Invoke(null);
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
            var chunkKey = new Vector2Int(chunkX, chunkZ);
            if (_loadedChunks.ContainsKey(chunkKey) || _pendingChunkRequests.Contains(chunkKey))
            {
                return;
            }

            _pendingChunkRequests.Add(chunkKey);

            var request = new ChunkDataRequestMessage
            {
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                ViewDistance = renderDistance
            };

            EnqueueMessage((int)MinecraftMessageType.ChunkDataRequest, request);
        }

        public void RequestServerStatus()
        {
            if (!_isConnected)
            {
                Debug.LogWarning("Cannot request server status while disconnected.");
                return;
            }

            if (string.IsNullOrEmpty(_sessionToken))
            {
                Debug.LogWarning("Cannot request server status before login.");
                return;
            }

            var request = new ServerStatusRequest
            {
                SessionToken = _sessionToken
            };

            EnqueueMessage((int)MessageType.ServerStatusRequest, request);

            if (autoRefreshServerStatus && serverStatusRefreshInterval > 0f)
            {
                _nextServerStatusRequestTime = Time.time + serverStatusRefreshInterval;
            }
        }

        #region Crafting API
        public void RequestAllRecipes()
        {
            var request = new RecipeListRequest { CraftingType = -1 };
            EnqueueMessage((int)MessageType.RecipeListRequest, request);
        }

        public void RequestRecipes(CraftingType craftingType)
        {
            var request = new RecipeListRequest { CraftingType = (int)craftingType };
            EnqueueMessage((int)MessageType.RecipeListRequest, request);
        }

        public void SendCraftingRequest(string recipeId, int amount, CraftingType craftingType)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                Debug.LogWarning("Cannot craft: recipe id is empty");
                return;
            }

            var request = new CraftingRequest
            {
                RecipeId = recipeId,
                CraftingAmount = Mathf.Max(1, amount),
                CraftingType = (int)craftingType
            };

            EnqueueMessage((int)MessageType.CraftingRequest, request);
        }

        public bool TryGetKnownRecipe(string recipeId, out RecipeData recipe)
        {
            return _knownRecipes.TryGetValue(recipeId, out recipe);
        }

        public IReadOnlyCollection<RecipeData> GetKnownRecipes()
        {
            return _knownRecipes.Values;
        }

        #endregion

        #region Room / Lobby API

        public void RequestRoomList(bool includeMembers = false, int worldFilter = -1)
        {
            var request = new RoomListRequest
            {
                IncludeMembers = includeMembers,
                WorldIdFilter = worldFilter
            };
            EnqueueMessage((int)MessageType.RoomListRequest, request);
        }

        public void EnterRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                Debug.LogWarning("Cannot enter room: roomId is empty");
                return;
            }

            var request = new RoomEnterRequest { RoomId = roomId };
            EnqueueMessage((int)MessageType.RoomEnterRequest, request);
        }

        public void LeaveCurrentRoom(string? roomIdHint = null)
        {
            var request = new RoomLeaveRequest { RoomId = roomIdHint ?? string.Empty };
            EnqueueMessage((int)MessageType.RoomLeaveRequest, request);
        }

        public IReadOnlyDictionary<string, RoomInfo> GetKnownRooms() => _knownRooms;

        public void RequestContainerOpen(Vector3Int position, ContainerType containerType)
        {
            var request = new ContainerOpenRequestMessage
            {
                Position = new Vector3I { X = position.x, Y = position.y, Z = position.z },
                ContainerType = containerType
            };

            EnqueueMessage((int)MinecraftMessageType.ContainerOpen, request);
        }

        public void RequestContainerClose(int containerId)
        {
            var request = new ContainerCloseRequestMessage
            {
                ContainerId = containerId
            };

            EnqueueMessage((int)MinecraftMessageType.ContainerClose, request);
        }

        public void SendContainerUpdate(int containerId, IEnumerable<SlotUpdate> updates, bool forceFullSync, string? clientSnapshotHash = null)
        {
            var normalizedUpdates = (updates ?? Array.Empty<SlotUpdate>())
                .Where(update => update != null)
                .Select(CloneSlotUpdate)
                .ToList();

            if (normalizedUpdates.Count == 0 && !forceFullSync)
            {
                return;
            }

            var request = new ContainerUpdateRequestMessage
            {
                ContainerId = containerId,
                SlotUpdates = normalizedUpdates,
                ForceFullSync = forceFullSync,
                ClientSnapshotHash = clientSnapshotHash ?? string.Empty
            };

            EnqueueMessage((int)MinecraftMessageType.ContainerUpdate, request);
        }

        #endregion

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
                        MessageType.ServerStatusResponse => ProtoBuf.Serializer.Deserialize<ServerStatusResponse>(stream),
                        MessageType.WorldBlockChangeBroadcast => ProtoBuf.Serializer.Deserialize<WorldBlockChangeBroadcast>(stream),
                        MessageType.WorldBlockChangeResponse => ProtoBuf.Serializer.Deserialize<WorldBlockChangeResponse>(stream),
                        MessageType.CraftingResponse => ProtoBuf.Serializer.Deserialize<CraftingResponse>(stream),
                        MessageType.RecipeListResponse => ProtoBuf.Serializer.Deserialize<RecipeListResponse>(stream),
                        MessageType.RoomListResponse => ProtoBuf.Serializer.Deserialize<RoomListResponse>(stream),
                        MessageType.RoomEnterResponse => ProtoBuf.Serializer.Deserialize<RoomEnterResponse>(stream),
                        MessageType.RoomLeaveResponse => ProtoBuf.Serializer.Deserialize<RoomLeaveResponse>(stream),
                        MessageType.RoomQueueUpdate => ProtoBuf.Serializer.Deserialize<RoomQueueUpdateMessage>(stream),
                        MessageType.RoomPromotionNotice => ProtoBuf.Serializer.Deserialize<RoomPromotionMessage>(stream),
                        MessageType.PlayerInfoUpdate => ProtoBuf.Serializer.Deserialize<PlayerInfoUpdate>(stream),
                        MessageType.PlayerDeath => ProtoBuf.Serializer.Deserialize<PlayerDeathMessage>(stream),

                        MessageType.PlayerRespawnBroadcast => ProtoBuf.Serializer.Deserialize<PlayerRespawnBroadcast>(stream),

                        MessageType.CombatEvent => ProtoBuf.Serializer.Deserialize<CombatEventMessage>(stream),
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
                        MinecraftMessageType.ChunkUnloadAcknowledge => ProtoBuf.Serializer.Deserialize<ChunkUnloadAcknowledgeMessage>(stream),
                        MinecraftMessageType.BlockChangeNotification => ProtoBuf.Serializer.Deserialize<BlockChangeNotificationMessage>(stream),
                        MinecraftMessageType.EntitySpawn => ProtoBuf.Serializer.Deserialize<EntitySpawnMessage>(stream),
                        MinecraftMessageType.EntityUpdate => ProtoBuf.Serializer.Deserialize<EntityUpdateMessage>(stream),
                        MinecraftMessageType.EntityDespawn => ProtoBuf.Serializer.Deserialize<EntityDespawnMessage>(stream),
                        MinecraftMessageType.TimeUpdate => ProtoBuf.Serializer.Deserialize<TimeUpdateMessage>(stream),
                        MinecraftMessageType.WeatherChange => ProtoBuf.Serializer.Deserialize<WeatherChangeMessage>(stream),
                        MinecraftMessageType.ContainerOpen => ProtoBuf.Serializer.Deserialize<ContainerOpenResponseMessage>(stream),
                        MinecraftMessageType.ContainerUpdate => ProtoBuf.Serializer.Deserialize<ContainerUpdateBroadcastMessage>(stream),
                        MinecraftMessageType.ContainerClose => ProtoBuf.Serializer.Deserialize<ContainerCloseNotificationMessage>(stream),
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
                case ChunkUnloadAcknowledgeMessage unloadAck:
                    HandleChunkUnloadAcknowledge(unloadAck);
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
                case RecipeListResponse recipeList:
                    HandleRecipeListResponse(recipeList);
                    break;
                case CraftingResponse craftingResponse:
                    HandleCraftingResponse(craftingResponse);
                    break;
                case RoomListResponse roomList:
                    HandleRoomListResponse(roomList);
                    break;
                case RoomEnterResponse roomEnter:
                    HandleRoomEnterResponse(roomEnter);
                    break;
                case RoomLeaveResponse roomLeave:
                    HandleRoomLeaveResponse(roomLeave);
                    break;
                case RoomQueueUpdateMessage queueUpdate:
                    HandleRoomQueueUpdate(queueUpdate);
                    break;
                case RoomPromotionMessage promotion:
                    HandleRoomPromotion(promotion);
                    break;
                case EntitySpawnMessage spawnMessage:
                    HandleEntitySpawn(spawnMessage);
                    break;
                case EntityUpdateMessage updateMessage:
                    HandleEntityUpdate(updateMessage);
                    break;
                case EntityDespawnMessage despawnMessage:
                    HandleEntityDespawn(despawnMessage);
                    break;
                case PingResponse pingResponse:
                    HandlePingResponse(pingResponse);
                    break;
                case ServerStatusResponse serverStatus:
                    HandleServerStatusResponse(serverStatus);
                    break;
                case TimeUpdateMessage timeUpdate:
                    HandleTimeUpdate(timeUpdate);
                    break;
                case ContainerOpenResponseMessage containerOpen:
                    ContainerOpened?.Invoke(containerOpen);
                    break;
                case ContainerUpdateBroadcastMessage containerUpdate:
                    ContainerUpdated?.Invoke(containerUpdate);
                    break;
                case ContainerCloseNotificationMessage containerClose:
                    ContainerClosed?.Invoke(containerClose);
                    break;
                case WeatherChangeMessage weatherChange:
                    HandleWeatherChange(weatherChange);
                    break;
                case PlayerDeathMessage deathMessage:
                    PlayerDeathNotified?.Invoke(deathMessage);
                    break;
                case CombatEventMessage combatEvent:
                    CombatEventReceived?.Invoke(combatEvent);
                    break;
                case PlayerRespawnBroadcast respawnBroadcast:
                    HandlePlayerRespawn(respawnBroadcast);
                    break;
                default:
                    Debug.LogWarning($"Unhandled message type: {message.GetType().Name}");
                    break;
            }
        }

        private void HandleTimeUpdate(TimeUpdateMessage message)
        {
            if (message == null)
            {
                return;
            }

            _lastTimeUpdate = new TimeUpdateMessage
            {
                WorldTime = message.WorldTime,
                DayTime = message.DayTime
            };

            TimeUpdated?.Invoke(_lastTimeUpdate);
        }

        private void HandleWeatherChange(WeatherChangeMessage message)
        {
            if (message == null)
            {
                return;
            }

            _lastWeatherChange = new WeatherChangeMessage
            {
                WeatherType = message.WeatherType,
                Duration = message.Duration,
                Intensity = message.Intensity
            };

            WeatherChanged?.Invoke(_lastWeatherChange);
        }

        private void HandlePlayerRespawn(PlayerRespawnBroadcast message)
        {
            if (message == null)
            {
                return;
            }

            var playerName = message.PlayerName;
            var isLocalPlayer = !string.IsNullOrWhiteSpace(playerName)
                && _playerState != null
                && !string.IsNullOrWhiteSpace(_playerState.Username)
                && string.Equals(_playerState.Username, playerName, StringComparison.OrdinalIgnoreCase);

            if (isLocalPlayer)
            {
                if (message.RespawnPosition != null)
                {
                    var respawn = message.RespawnPosition;
                    _playerState.Position = new Vector3D
                    {
                        X = respawn.X,
                        Y = respawn.Y,
                        Z = respawn.Z
                    };
                }

                PlayerStateUpdated?.Invoke(_playerState);
            }
            else
            {
                UpdateRemoteRespawnState(playerName, message.RespawnPosition);
            }

            PlayerRespawned?.Invoke(message);
        }

        private void UpdateRemoteRespawnState(string playerName, SharedProtocol.Vector3? respawnPosition)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            if (!_entities.TryGetValue(playerName, out var stored))
            {
                return;
            }

            var snapshot = CloneEntity(stored);

            if (respawnPosition != null)
            {
                snapshot.Position = new Vector3D
                {
                    X = respawnPosition.X,
                    Y = respawnPosition.Y,
                    Z = respawnPosition.Z
                };
            }

            snapshot.Velocity = new Vector3D();

            if (snapshot.MaxHealth > 0f)
            {
                snapshot.Health = snapshot.MaxHealth;
            }
            else
            {
                const float defaultHealth = 20f;
                snapshot.MaxHealth = defaultHealth;
                if (snapshot.Health <= 0f)
                {
                    snapshot.Health = defaultHealth;
                }
            }

            snapshot.EntityType = EntityType.Player;

            _entities[playerName] = snapshot;
            EntityUpdated?.Invoke(CloneEntity(snapshot));
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

            RequestServerStatus();
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
            _pendingChunkRequests.Remove(chunkKey);

            if (!response.Success)
            {
                ErrorOccurred?.Invoke($"Failed to load chunk {response.ChunkX},{response.ChunkZ} from server.");
                return;
            }

            var blocks = ChunkCompression.DecodeBlocks(response.CompressedBlockData);
            var entities = response.Entities ?? new List<EntityInfo>();
            var metadata = EnhancedChunkPayloadBridge.Decode(response, warning => Debug.LogWarning($"[Chunks] {warning}"));
            var snapshot = new ChunkSnapshot(response.ChunkX, response.ChunkZ, blocks, response.BiomeData, entities, response.IsFromCache, metadata);

            _loadedChunks[chunkKey] = snapshot;

            if (response.IsFromCache)
            {
                Debug.Log($"Chunk {response.ChunkX},{response.ChunkZ} served from cache.");
            }

            ChunkLoaded?.Invoke(snapshot);
        }

        private void EvaluateChunkResidency()
        {
            if (!_isConnected || _loadedChunks.Count == 0)
            {
                return;
            }

            var position = _playerState?.Position;
            if (position == null)
            {
                return;
            }

            var chunkSize = ChunkSnapshot.ChunkSize;
            var playerChunkX = Mathf.FloorToInt((float)position.X / chunkSize);
            var playerChunkZ = Mathf.FloorToInt((float)position.Z / chunkSize);
            var radius = Mathf.Max(1, renderDistance);

            if (_chunksToUnload.Count > 0)
            {
                _chunksToUnload.Clear();
            }

            foreach (var chunkKey in _loadedChunks.Keys)
            {
                var distance = Mathf.Max(Mathf.Abs(chunkKey.x - playerChunkX), Mathf.Abs(chunkKey.y - playerChunkZ));
                if (distance > radius)
                {
                    _chunksToUnload.Add(chunkKey);
                }
            }

            if (_chunksToUnload.Count == 0)
            {
                return;
            }

            foreach (var chunkKey in _chunksToUnload)
            {
                UnloadChunk(chunkKey, ChunkUnloadReason.ViewDistance);
            }

            _chunksToUnload.Clear();
        }

        private void UnloadChunk(Vector2Int chunkKey, ChunkUnloadReason reason)
        {
            if (!_loadedChunks.TryGetValue(chunkKey, out var chunk))
            {
                return;
            }

            _loadedChunks.Remove(chunkKey);
            ChunkUnloaded?.Invoke(chunkKey, chunk);
            SendChunkUnloadNotification(chunkKey, reason);
        }

        private void SendChunkUnloadNotification(Vector2Int chunkKey, ChunkUnloadReason reason)
        {
            if (!_isConnected)
            {
                return;
            }

            var notification = new ChunkUnloadNotificationMessage
            {
                PlayerId = _playerState?.PlayerId ?? string.Empty,
                ChunkX = chunkKey.x,
                ChunkZ = chunkKey.y,
                Reason = reason,
                ViewDistance = renderDistance,
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            EnqueueMessage((int)MinecraftMessageType.ChunkUnloadNotification, notification);
        }

        private void HandleChunkUnloadAcknowledge(ChunkUnloadAcknowledgeMessage ack)
        {
            if (ack == null)
            {
                return;
            }

            if (!ack.Accepted)
            {
                Debug.LogWarning($"Server rejected chunk unload {ack.ChunkX},{ack.ChunkZ}: {ack.Note}");
                return;
            }

            Debug.Log($"Server acknowledged chunk unload {ack.ChunkX},{ack.ChunkZ} (remaining tracked: {ack.RemainingChunks})");
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

            if (message.Drops != null && message.Drops.Count > 0)
            {
                BlockDropsReceived?.Invoke(position, message.Drops);
            }
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

        private void HandleRecipeListResponse(RecipeListResponse response)
        {
            if (!response.Success)
            {
                ErrorOccurred?.Invoke("Failed to fetch crafting recipes from server.");
                return;
            }

            foreach (var recipe in response.Recipes)
            {
                if (!string.IsNullOrEmpty(recipe.RecipeId))
                {
                    _knownRecipes[recipe.RecipeId] = recipe;
                }
            }

            RecipeListReceived?.Invoke(response.Recipes);
            Debug.Log($"Received {response.Recipes.Count} crafting recipes (total cached: {_knownRecipes.Count}).");
        }

        private void HandleCraftingResponse(CraftingResponse response)
        {
            if (!response.Success)
            {
                if (!string.IsNullOrEmpty(response.Message))
                {
                    ErrorOccurred?.Invoke(response.Message);
                }
                CraftingCompleted?.Invoke(response);
                return;
            }

            if (!string.IsNullOrWhiteSpace(response.UpdatedInventory))
            {
                ApplyInventorySnapshot(response.UpdatedInventory);
            }

            if (!string.IsNullOrEmpty(response.RecipeId) && _knownRecipes.TryGetValue(response.RecipeId, out var recipe))
            {
                var craftedSummary = response.CraftedItems?.Count > 0
                    ? string.Join(", ", response.CraftedItems.Select(item => $"{item.Amount}x {item.ItemId}"))
                    : "(no items reported)";
                Debug.Log($"Crafted {craftedSummary} via recipe '{recipe.Name}'.");
            }

            CraftingCompleted?.Invoke(response);
        }
        private void ApplyInventorySnapshot(string snapshotJson)
        {
            if (!ClientInventorySnapshot.TryParse(snapshotJson, out var snapshot, out var error))
            {
                Debug.LogWarning($"Failed to parse inventory snapshot: {error}");
                return;
            }

            var changedSlots = snapshot.GetChangedSlots(_inventorySnapshot);
            if (changedSlots.Count == 0)
            {
                return;
            }

            _inventorySnapshot = snapshot;

            var orderedSlots = snapshot.GetOrderedSlots();
            const int TotalInventorySlots = 41;

            _inventoryItems.Clear();
            for (int i = 0; i < TotalInventorySlots; i++)
            {
                _inventoryItems.Add(new ItemInfo());
            }

            if (orderedSlots.Count > 0)
            {
                foreach (var slot in orderedSlots)
                {
                    if (slot.SlotIndex >= 0 && slot.SlotIndex < TotalInventorySlots)
                    {
                        _inventoryItems[slot.SlotIndex] = ConvertSlotToItemInfo(slot);
                    }
                }
            }

            UpdatePlayerStateInventory(snapshot);

            InventoryItemsUpdated?.Invoke(_inventoryItems
                .Select(item => item.Clone())
                .ToArray());

            PlayerStateUpdated?.Invoke(_playerState);

            Debug.Log($"Inventory snapshot applied with {changedSlots.Count} changed slot(s).");
        }

        private void UpdatePlayerStateInventory(ClientInventorySnapshot snapshot)
        {
            if (_playerState == null)
            {
                _playerState = new PlayerStateInfo();
            }

            var protocolItems = new List<InventoryItemInfo>();
            foreach (var slot in snapshot.GetOrderedSlots())
            {
                if (slot.IsEmpty)
                {
                    continue;
                }

                protocolItems.Add(ConvertSlotToProtocolItem(slot));
            }

            _playerState.Inventory = protocolItems;

            if (_playerState.Inventory.Count > 0)
            {
                var selectedIndex = _playerState.SelectedSlot;
                if (selectedIndex < 0 || selectedIndex >= _playerState.Inventory.Count)
                {
                    selectedIndex = 0;
                    _playerState.SelectedSlot = 0;
                }

                _playerState.HeldItem = _playerState.Inventory[selectedIndex];
            }
            else
            {
                _playerState.HeldItem = new InventoryItemInfo();
            }
        }

        private ItemInfo ConvertSlotToItemInfo(ClientInventorySlot slot)
        {
            if (slot.IsEmpty)
            {
                return new ItemInfo();
            }

            return new ItemInfo
            {
                Id = ResolveItemNumericId(slot.ItemId),
                Name = FormatItemName(slot.ItemId),
                Quantity = slot.Amount,
                Type = GuessItemType(slot.ItemId),
                CustomData = slot.ItemData
            };
        }

        private InventoryItemInfo ConvertSlotToProtocolItem(ClientInventorySlot slot)
        {
            return new InventoryItemInfo
            {
                ItemId = ResolveItemNumericId(slot.ItemId),
                ItemName = FormatItemName(slot.ItemId),
                Quantity = slot.Amount,
                Durability = 0,
                MaxDurability = 0,
                ItemType = GuessItemType(slot.ItemId),
                CustomData = slot.ItemData ?? string.Empty
            };
        }

        private int ResolveItemNumericId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            if (_itemIdLookup.TryGetValue(itemId, out var numericId))
            {
                return numericId;
            }

            var computed = ComputeStableItemId(itemId);
            _itemIdLookup[itemId] = computed;
            return computed;
        }

        private static int ComputeStableItemId(string value)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToInt32(hash, 0) & int.MaxValue;
        }

        private static string FormatItemName(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return "Empty";
            }

            var core = itemId.Contains(':') ? itemId.Split(':')[1] : itemId;
            core = core.Replace('_', ' ');
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(core);
        }

        private static ItemType GuessItemType(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return ItemType.Material;
            }

            var normalized = itemId.ToLowerInvariant();

            if (normalized.Contains("sword") || normalized.Contains("bow") || normalized.Contains("trident"))
            {
                return ItemType.Weapon;
            }

            if (normalized.Contains("pickaxe") || normalized.Contains("axe") || normalized.Contains("shovel") || normalized.Contains("hoe"))
            {
                return ItemType.Tool;
            }

            if (normalized.Contains("helmet") || normalized.Contains("chestplate") || normalized.Contains("leggings") || normalized.Contains("boots"))
            {
                return ItemType.Armor;
            }

            if (normalized.Contains("bread") || normalized.Contains("apple") || normalized.Contains("stew") || normalized.Contains("food"))
            {
                return ItemType.Food;
            }

            if (normalized.Contains("log") || normalized.Contains("plank") || normalized.Contains("stone") || normalized.Contains("block"))
            {
                return ItemType.Block;
            }

            return ItemType.Material;
        }

        private void HandleRoomListResponse(RoomListResponse response)
        {
            if (!response.Success)
            {
                if (!string.IsNullOrEmpty(response.Message))
                {
                    ErrorOccurred?.Invoke(response.Message);
                }
                RoomListReceived?.Invoke(Array.Empty<RoomInfo>());
                return;
            }

            _knownRooms.Clear();
            foreach (var room in response.Rooms)
            {
                if (!string.IsNullOrEmpty(room.RoomId))
                {
                    _knownRooms[room.RoomId] = room;
                }
            }

            if (response.LobbySummaries?.Count > 0)
            {
                foreach (var lobby in response.LobbySummaries)
                {
                    Debug.Log($"Lobby {lobby.LobbyId}: rooms={lobby.RoomCount}, players={lobby.PlayerCount}, queue={lobby.QueueCount}");
                }
            }

            RoomListReceived?.Invoke(response.Rooms);
        }

        private void HandleRoomEnterResponse(RoomEnterResponse response)
        {
            if (!response.Success)
            {
                if (!string.IsNullOrEmpty(response.Message))
                {
                    ErrorOccurred?.Invoke(response.Message);
                }
                RoomEntered?.Invoke(response);
                return;
            }

            if (response.Room != null && !string.IsNullOrEmpty(response.Room.RoomId))
            {
                _knownRooms[response.Room.RoomId] = response.Room;
            }

            if (response.Room != null)
            {
                Debug.Log($"Entered room {response.Room.DisplayName} ({response.Room.RoomId}). Members: {response.Members.Count}");
                if (response.IsQueued)
                {
                    Debug.Log($"Waiting in queue position {response.QueuePosition} (est. {response.EstimatedWaitMs} ms).");
                    response.Room.QueueCount = Math.Max(response.Room.QueueCount, response.QueuePosition);
                }
            }

            RoomEntered?.Invoke(response);
        }

        private void HandleRoomLeaveResponse(RoomLeaveResponse response)
        {
            if (!response.Success)
            {
                if (!string.IsNullOrEmpty(response.Message))
                {
                    ErrorOccurred?.Invoke(response.Message);
                }
            }
            else
            {
                var promotionSuffix = response.PromotedFromQueue && !string.IsNullOrEmpty(response.PromotedUser)
                    ? $" (promoted {response.PromotedUser} from queue)"
                    : string.Empty;
                var destination = response.ReturnedToLobby ? "returned to lobby" : "left room";
                Debug.Log($"Left room {response.PreviousRoomId}; {destination}.{promotionSuffix}");
                if (_knownRooms.TryGetValue(response.PreviousRoomId, out var info))
                {
                    if (info.PlayerCount > 0 && response.ReturnedToLobby)
                    {
                        info.PlayerCount = Math.Max(0, info.PlayerCount - 1);
                    }
                    if (response.PromotedFromQueue && info.QueueCount > 0)
                    {
                        info.QueueCount = Math.Max(0, info.QueueCount - 1);
                    }
                }
            }

            RoomLeft?.Invoke(response);
        }

        private void HandleRoomQueueUpdate(RoomQueueUpdateMessage message)
        {
            var waiting = message.Queue?.Count ?? 0;
            Debug.Log($"Queue update for room {message.RoomId}: {waiting} players waiting.");
            if (_knownRooms.TryGetValue(message.RoomId, out var info))
            {
                info.QueueCount = waiting;
            }
            RoomQueueUpdated?.Invoke(message);
        }

        private void HandleRoomPromotion(RoomPromotionMessage message)
        {
            if (message.Member != null)
            {
                Debug.Log($"Player {message.Member.UserName} promoted into room {message.RoomId}.");
            }
            else
            {
                Debug.Log($"Received room promotion notice for {message.RoomId}.");
            }

            if (message.Room != null && !string.IsNullOrEmpty(message.Room.RoomId))
            {
                _knownRooms[message.Room.RoomId] = message.Room;
            }
            else if (_knownRooms.TryGetValue(message.RoomId, out var info))
            {
                info.QueueCount = Math.Max(0, info.QueueCount - 1);
                if (message.Member != null && !message.Member.IsSpectator)
                {
                    info.PlayerCount += 1;
                }
            }

            RoomPromotionReceived?.Invoke(message);
        }

        private void HandleEntitySpawn(EntitySpawnMessage message)
        {
            if (message?.Entity == null || string.IsNullOrEmpty(message.Entity.EntityId))
            {
                return;
            }

            var snapshot = CloneEntity(message.Entity);
            _entities[message.Entity.EntityId] = snapshot;
            EntitySpawned?.Invoke(CloneEntity(snapshot));
        }

        private void HandleEntityUpdate(EntityUpdateMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.EntityId))
            {
                return;
            }

            EntityInfo entity;
            if (_entities.TryGetValue(message.EntityId, out var existing))
            {
                entity = CloneEntity(existing);
            }
            else
            {
                entity = new EntityInfo
                {
                    EntityId = message.EntityId,
                    EntityType = EntityType.Player
                };
            }

            if (message.UpdateFlags?.PositionUpdated == true && message.Position != null)
            {
                entity.Position = CloneVector(message.Position);
            }

            if (message.UpdateFlags?.RotationUpdated == true && message.Rotation != null)
            {
                entity.Rotation = CloneVector(message.Rotation);
            }

            if (message.UpdateFlags?.VelocityUpdated == true && message.Velocity != null)
            {
                entity.Velocity = CloneVector(message.Velocity);
            }

            if (message.UpdateFlags?.HealthUpdated == true)
            {
                entity.Health = message.Health;
            }

            _entities[message.EntityId] = entity;
            EntityUpdated?.Invoke(CloneEntity(entity));
        }

        private void HandleEntityDespawn(EntityDespawnMessage message)
        {
            if (string.IsNullOrEmpty(message.EntityId))
            {
                return;
            }

            _entities.Remove(message.EntityId);
            EntityDespawned?.Invoke(message.EntityId);
        }

        private static EntityInfo CloneEntity(EntityInfo source)
        {
            if (source == null)
            {
                return new EntityInfo();
            }

            return new EntityInfo
            {
                EntityId = source.EntityId,
                EntityType = source.EntityType,
                Position = CloneVector(source.Position),
                Rotation = CloneVector(source.Rotation),
                Velocity = CloneVector(source.Velocity),
                Health = source.Health,
                MaxHealth = source.MaxHealth,
                CustomData = source.CustomData
            };
        }

        private static Vector3D CloneVector(Vector3D source)
        {
            if (source == null)
            {
                return new Vector3D();
            }

            return new Vector3D
            {
                X = source.X,
                Y = source.Y,
                Z = source.Z
            };
        }

        private void HandlePingResponse(PingResponse response)
        {
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.ClientTimestamp;
            Debug.Log($"Ping: {latency} ms");
        }

        private void HandleServerStatusResponse(ServerStatusResponse response)
        {
            if (response == null)
            {
                return;
            }

            _latestServerStatus = response;
            ServerStatusReceived?.Invoke(response);
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

        private static SlotUpdate CloneSlotUpdate(SlotUpdate update)
        {
            if (update == null)
            {
                return new SlotUpdate();
            }

            return new SlotUpdate
            {
                Slot = update.Slot,
                ItemIdentifier = update.ItemIdentifier ?? string.Empty,
                Item = CloneInventoryItem(update.Item)
            };
        }

        private static InventoryItemInfo CloneInventoryItem(InventoryItemInfo item)
        {
            if (item == null)
            {
                return new InventoryItemInfo();
            }

            return new InventoryItemInfo
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName ?? string.Empty,
                Quantity = item.Quantity,
                Durability = item.Durability,
                MaxDurability = item.MaxDurability,
                ItemType = item.ItemType,
                CustomData = item.CustomData ?? string.Empty,
                Enchantments = item.Enchantments != null
                    ? item.Enchantments.Select(e => new EnchantmentInfo { EnchantId = e.EnchantId, Level = e.Level }).ToList()
                    : new List<EnchantmentInfo>()
            };
        }

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
