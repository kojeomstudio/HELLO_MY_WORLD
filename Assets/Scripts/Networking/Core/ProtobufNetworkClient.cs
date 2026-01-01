using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Google.Protobuf;
using Game.Auth;
using GameProtocol;
using EnhancedMinecraftProtocol.Manifest;
using SharedProtocol.EnhancedMinecraft;
#if HMW_PROTO
using Game.Move;
#endif

namespace Networking.Core
{
    /// <summary>
    /// Protobuf-based network client
    /// Handles Protobuf message serialization/deserialization for server communication.
    /// </summary>
    public class ProtobufNetworkClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 9000;
        [SerializeField] private float connectionTimeout = 10f;
        
        private INetworkTransport _transport;
        private MessageDispatcher _messageDispatcher;
        private bool _isInitialized = false;
        
        // Connection status events
        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ConnectionError;
        
        // Message handler events
        public event Action<LoginResponse> LoginResponseReceived;
        #if HMW_PROTO
        public event Action<Game.Move.MoveResponse> MoveResponseReceived;
        public event Action<Game.Chat.ChatMessage> ChatMessageReceived;
        public event Action<Game.World.WorldBlockChangeBroadcast> BlockChangeBroadcastReceived;
        public event Action<Game.Diag.PingResponse> PingResponseReceived;
        #endif

        // AI System events (Server-Authoritative)
        public event Action<AIStateSyncBroadcast> AIStateSyncReceived;
        public event Action<AIAttackEventBroadcast> AIAttackEventReceived;
        public event Action<AIDeathEventBroadcast> AIDeathEventReceived;
        public event Action<AISpawnResponse> AISpawnResponseReceived;
        public event Action<AIDebugInfoResponse> AIDebugInfoResponseReceived;

        public bool IsConnected => _transport?.IsConnected ?? false;
        public string ServerAddress => serverAddress;
        public int ServerPort => serverPort;

        private void Awake()
        {
            InitializeClient();
        }

        private void InitializeClient()
        {
            if (_isInitialized) return;

            ValidateProtocolContracts();
            _transport = new TcpNetworkTransport();
            _transport.ConnectionStatusChanged += OnConnectionStatusChanged;
            _transport.Received += OnDataReceived;
            
            _messageDispatcher = new MessageDispatcher();
            
            _messageDispatcher.RegisterHandler<LoginResponse>(OnLoginResponse);
            _messageDispatcher.RegisterHandler<MoveResponse>(OnMoveResponse);
            _messageDispatcher.RegisterHandler<ChatMessage>(OnChatMessage);
            _messageDispatcher.RegisterHandler<WorldBlockChangeBroadcast>(OnBlockChangeBroadcast);
            _messageDispatcher.RegisterHandler<PingResponse>(OnPingResponse);
            
            // Register enhanced protocol handlers
            _messageDispatcher.RegisterHandler<EnhancedMinecraftProtocol.EntitySpawnNotification>(OnEntitySpawnNotification);
            _messageDispatcher.RegisterHandler<EnhancedMinecraftProtocol.EntityDespawnNotification>(OnEntityDespawnNotification);
            _messageDispatcher.RegisterHandler<EnhancedMinecraftProtocol.EntityStateUpdate>(OnEntityStateUpdate);
            _messageDispatcher.RegisterHandler<EnhancedMinecraftProtocol.WorldTimeUpdate>(OnWorldTimeUpdate);
            _messageDispatcher.RegisterHandler<EnhancedMinecraftProtocol.WeatherChangeNotification>(OnWeatherChangeNotification);
            
            _isInitialized = true;
            Debug.Log("ProtobufNetworkClient initialized");
        }

        private void ValidateProtocolContracts()
        {
            try
            {
                ProtocolStandardization.ValidateProtocolImplementation();
                EnhancedProtoManifest.AssertFingerprint();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProtobufNetworkClient] Protobuf contract validation warning: {ex.Message}");
            }
        }

        /// <summary>
        /// Connects to server asynchronously.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                Debug.Log($"Connecting to server at {serverAddress}:{serverPort}...");
                
                var connectTask = _transport.ConnectAsync(serverAddress, serverPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(connectionTimeout));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    ConnectionError?.Invoke("Connection timed out");
                    return false;
                }
                
                await connectTask;
                Debug.Log("Successfully connected to server");
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to connect: {ex.Message}";
                Debug.LogError(errorMsg);
                ConnectionError?.Invoke(errorMsg);
                return false;
            }
        }

        /// <summary>
        /// Disconnects from server.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_transport != null)
            {
                await _transport.DisconnectAsync();
            }
        }

        /// <summary>
        /// Sends a login request with protocol header (length + type + payload).
        /// </summary>
        public void SendLogin(string username, string password, string clientVersion = "1.0.0")
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };
            // Note: Server-side allows missing ClientVersion (defaults). Our .proto lacks this optional field.
            SendMessageWithHeader(request, ClientMessageType.LoginRequest);
            Debug.Log($"Sent login request for user: {username}");
        }

        // TODO: Implement additional message senders (Move, Chat, BlockChange) after adding matching .proto definitions.

        /// <summary>
        /// Sends a chat message.
        /// </summary>
        public void SendChatMessage(string message, ChatType chatType = ChatType.Global, string targetPlayer = "")
        {
            #if HMW_PROTO
            var request = new Game.Chat.ChatRequest
            {
                Message = message,
                Type = (int)chatType,
                TargetPlayer = targetPlayer
            };
            SendMessageWithHeader(request, ClientMessageType.ChatRequest);
            #else
            Debug.LogWarning("Chat proto not generated yet. See docs/networking-protocol.md to generate C#.");
            #endif
        }

        /// <summary>
        /// Sends a block change request.
        /// </summary>
        public void SendBlockChangeRequest(string areaId, string subworldId, Vector3Int blockPosition, int blockType, int chunkType)
        {
            #if HMW_PROTO
            var request = new Game.World.WorldBlockChangeRequest
            {
                AreaId = areaId,
                SubworldId = subworldId,
                BlockPosition = new Game.Core.Vector3Int 
                { 
                    X = blockPosition.x, 
                    Y = blockPosition.y, 
                    Z = blockPosition.z 
                },
                BlockType = blockType,
                ChunkType = chunkType
            };
            SendMessageWithHeader(request, ClientMessageType.WorldBlockChangeRequest);
            #else
            Debug.LogWarning("World proto not generated yet. See docs/networking-protocol.md to generate C#.");
            #endif
        }

        /// <summary>
        /// Sends a ping request.
        /// </summary>
        public void SendPing()
        {
            #if HMW_PROTO
            var request = new Game.Diag.PingRequest { ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            SendMessageWithHeader(request, ClientMessageType.PingRequest);
            #else
            Debug.LogWarning("Diag proto not generated yet. See docs/networking-protocol.md to generate C#.");
            #endif
        }

        /// <summary>
        /// Sends a movement request to the server so it can validate and echo back an authoritative position.
        /// Keeps signature available even when proto types are not compiled.
        /// </summary>
        public void SendMoveRequest(Vector3 targetPosition, float movementSpeed)
        {
#if HMW_PROTO
            var req = new Game.Move.MoveRequest
            {
                TargetPosition = new Game.Core.Vector3
                {
                    X = targetPosition.x,
                    Y = targetPosition.y,
                    Z = targetPosition.z
                },
                MovementSpeed = movementSpeed
            };
            SendMessageWithHeader(req, ClientMessageType.MoveRequest);
#else
            Debug.LogWarning("Move proto not generated yet. Define HMW_PROTO and generate C# from proto/game_move.proto.");
#endif
        }

        /// <summary>
        /// Sends an AI spawn request (GM command).
        /// </summary>
        public void SendAISpawnRequest(string aiType, UnityEngine.Vector3 spawnPosition, string worldId = "main_world")
        {
            var request = new AISpawnRequest
            {
                AIType = aiType,
                SpawnPosition = new GameProtocol.Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z),
                WorldId = worldId
            };
            SendJsonMessageWithHeader(request, ClientMessageType.AISpawnRequest);
            Debug.Log($"Sent AI spawn request: Type={aiType}, Position={spawnPosition}");
        }

        /// <summary>
        /// Sends an AI debug info request.
        /// </summary>
        public void SendAIDebugInfoRequest(int actorId = 0)
        {
            var request = new AIDebugInfoRequest
            {
                ActorId = actorId // 0 = all AI actors
            };
            SendJsonMessageWithHeader(request, ClientMessageType.AIDebugInfoRequest);
            Debug.Log($"Sent AI debug info request: ActorId={actorId}");
        }

        /// <summary>
        /// Serialize protobuf and send with header (length set by transport, type prepended here).
        /// </summary>
        private void SendMessageWithHeader(IMessage message, ClientMessageType type)
        {
            if (!IsConnected)
            {
                Debug.LogWarning($"Cannot send {message.GetType().Name}: not connected to server");
                return;
            }

            try
            {
                using var memoryStream = new MemoryStream();
                message.WriteTo(memoryStream);
                var payload = memoryStream.ToArray();

                // Build [type:int][payload]
                var typeBytes = BitConverter.GetBytes((int)type);
                var framed = new byte[typeBytes.Length + payload.Length];
                Buffer.BlockCopy(typeBytes, 0, framed, 0, typeBytes.Length);
                Buffer.BlockCopy(payload, 0, framed, typeBytes.Length, payload.Length);

                _transport.Send(new ArraySegment<byte>(framed));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send {message.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Serialize JSON and send with header (for GameProtocol classes).
        /// </summary>
        private void SendJsonMessageWithHeader(object message, ClientMessageType type)
        {
            if (!IsConnected)
            {
                Debug.LogWarning($"Cannot send {message.GetType().Name}: not connected to server");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(message);
                var payload = System.Text.Encoding.UTF8.GetBytes(json);

                // Build [type:int][payload]
                var typeBytes = BitConverter.GetBytes((int)type);
                var framed = new byte[typeBytes.Length + payload.Length];
                Buffer.BlockCopy(typeBytes, 0, framed, 0, typeBytes.Length);
                Buffer.BlockCopy(payload, 0, framed, typeBytes.Length, payload.Length);

                _transport.Send(new ArraySegment<byte>(framed));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send JSON message {message.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when data is received from server.
        /// </summary>
        private void OnDataReceived(ArraySegment<byte> data)
        {
            try
            {
                // Parse [type:int][payload]
                var buffer = new byte[data.Count];
                Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);

                if (buffer.Length < 4)
                {
                    Debug.LogWarning("Received too small packet");
                    return;
                }

                int type = BitConverter.ToInt32(buffer, 0);
                var payload = new byte[buffer.Length - 4];
                Buffer.BlockCopy(buffer, 4, payload, 0, payload.Length);

                switch ((ClientMessageType)type)
                {
                    case ClientMessageType.LoginResponse:
                        if (TryParseMessage<LoginResponse>(payload, out var loginResponse))
                            _messageDispatcher.Dispatch(loginResponse);
                        break;
                    #if HMW_PROTO
                    case ClientMessageType.MoveResponse:
                        if (TryParseMessage<Game.Move.MoveResponse>(payload, out var moveResponse))
                        {
                            _messageDispatcher.Dispatch(moveResponse);
                            MoveResponseReceived?.Invoke(moveResponse);
                        }
                        break;
                    case ClientMessageType.ChatMessage:
                        if (TryParseMessage<Game.Chat.ChatMessage>(payload, out var chatMessage))
                        {
                            _messageDispatcher.Dispatch(chatMessage);
                            ChatMessageReceived?.Invoke(chatMessage);
                        }
                        break;
                    case ClientMessageType.WorldBlockChangeBroadcast:
                        if (TryParseMessage<Game.World.WorldBlockChangeBroadcast>(payload, out var blockBroadcast))
                        {
                            _messageDispatcher.Dispatch(blockBroadcast);
                            BlockChangeBroadcastReceived?.Invoke(blockBroadcast);
                        }
                        break;
                    case ClientMessageType.PingResponse:
                        if (TryParseMessage<Game.Diag.PingResponse>(payload, out var pingResponse))
                        {
                            _messageDispatcher.Dispatch(pingResponse);
                            PingResponseReceived?.Invoke(pingResponse);
                        }
                        break;
                    #endif

                    // AI System messages (JSON serialization)
                    case ClientMessageType.AIStateSyncBroadcast:
                        if (TryParseJsonMessage<AIStateSyncBroadcast>(payload, out var aiStateSync))
                        {
                            AIStateSyncReceived?.Invoke(aiStateSync);
                        }
                        break;
                    case ClientMessageType.AIAttackEventBroadcast:
                        if (TryParseJsonMessage<AIAttackEventBroadcast>(payload, out var aiAttack))
                        {
                            AIAttackEventReceived?.Invoke(aiAttack);
                        }
                        break;
                    case ClientMessageType.AIDeathEventBroadcast:
                        if (TryParseJsonMessage<AIDeathEventBroadcast>(payload, out var aiDeath))
                        {
                            AIDeathEventReceived?.Invoke(aiDeath);
                        }
                        break;
                    case ClientMessageType.AISpawnResponse:
                        if (TryParseJsonMessage<AISpawnResponse>(payload, out var aiSpawnResp))
                        {
                            AISpawnResponseReceived?.Invoke(aiSpawnResp);
                        }
                        break;
                    case ClientMessageType.AIDebugInfoResponse:
                        if (TryParseJsonMessage<AIDebugInfoResponse>(payload, out var aiDebugResp))
                        {
                            AIDebugInfoResponseReceived?.Invoke(aiDebugResp);
                        }
                        break;

                    default:
                        Debug.LogWarning($"Unknown or unhandled message type: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process received data: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to parse a protobuf message.
        /// </summary>
        private bool TryParseMessage<T>(byte[] data, out T message) where T : IMessage, new()
        {
            try
            {
                message = new T();
                message.MergeFrom(data);
                return true;
            }
            catch (Google.Protobuf.InvalidProtocolBufferException)
            {
                message = default(T);
                return false;
            }
            catch
            {
                message = default(T);
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse a JSON message (for GameProtocol classes).
        /// Uses Unity JsonUtility for simple serialization.
        /// </summary>
        private bool TryParseJsonMessage<T>(byte[] data, out T message)
        {
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(data);
                message = JsonUtility.FromJson<T>(json);
                return message != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse JSON message of type {typeof(T).Name}: {ex.Message}");
                message = default(T);
                return false;
            }
        }

        // Message handlers
        private void OnLoginResponse(LoginResponse response)
        {
            Debug.Log($"Login response: Success={response.Success}, Message={response.Message}");
            LoginResponseReceived?.Invoke(response);
        }

#if HMW_PROTO
        private void OnMoveResponse(Game.Move.MoveResponse response)
        {
            Debug.Log($"Move response: Status={response.Status}, Position=({response.NewPosition.X}, {response.NewPosition.Y}, {response.NewPosition.Z})");
            MoveResponseReceived?.Invoke(response);
        }

        private void OnChatMessage(Game.Chat.ChatMessage message)
        {
            Debug.Log($"Chat message: {message.Sender}: {message.Message}");
            ChatMessageReceived?.Invoke(message);
        }

        private void OnBlockChangeBroadcast(Game.World.WorldBlockChangeBroadcast broadcast)
        {
            Debug.Log($"Block change: ({broadcast.BlockPosition.X}, {broadcast.BlockPosition.Y}, {broadcast.BlockPosition.Z}) -> {broadcast.BlockType}");
            BlockChangeBroadcastReceived?.Invoke(broadcast);
        }

        private void OnPingResponse(Game.Diag.PingResponse response)
        {
            Debug.Log($"Ping response: {response.ClientTimestamp} -> {response.ServerTimestamp}");
            PingResponseReceived?.Invoke(response);
        }
#endif

        // Enhanced protocol handlers
        private void OnEntitySpawnNotification(EnhancedMinecraftProtocol.EntitySpawnNotification notification)
        {
            Debug.Log($"Entity spawned: {notification.EntityType} at ({notification.Position.X}, {notification.Position.Y}, {notification.Position.Z})");
            // TODO: Implement entity spawn handling
        }

        private void OnEntityDespawnNotification(EnhancedMinecraftProtocol.EntityDespawnNotification notification)
        {
            Debug.Log($"Entity despawned: {notification.EntityId}");
            // TODO: Implement entity despawn handling
        }

        private void OnEntityStateUpdate(EnhancedMinecraftProtocol.EntityStateUpdate update)
        {
            Debug.Log($"Entity state update: {update.EntityId}");
            // TODO: Implement entity state update handling
        }

        private void OnWorldTimeUpdate(EnhancedMinecraftProtocol.WorldTimeUpdate timeUpdate)
        {
            Debug.Log($"World time updated: {timeUpdate.WorldTime}");
            // TODO: Implement world time handling
        }

        private void OnWeatherChangeNotification(EnhancedMinecraftProtocol.WeatherChangeNotification weatherUpdate)
        {
            Debug.Log($"Weather changed: {weatherUpdate.WeatherType}");
            // TODO: Implement weather change handling
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            Debug.Log($"Connection status changed: {isConnected}");
            ConnectionStatusChanged?.Invoke(isConnected);
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

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3
    }
}
                _transport.Received -= OnDataReceived;
                
                if (_transport is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3
    }
}
}
                
                if (_transport is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3
    }
}
}
                
                if (_transport is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType
    {
        Global = 0,
        Local = 1,
        Whisper = 2,
        System = 3
    }
}

