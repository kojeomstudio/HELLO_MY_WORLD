using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Google.Protobuf;
using Game.Auth;
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
            
            _transport = new TcpNetworkTransport();
            _transport.ConnectionStatusChanged += OnConnectionStatusChanged;
            _transport.Received += OnDataReceived;
            
            _messageDispatcher = new MessageDispatcher();
            
            _messageDispatcher.RegisterHandler<LoginResponse>(OnLoginResponse);
            _messageDispatcher.RegisterHandler<MoveResponse>(OnMoveResponse);
            _messageDispatcher.RegisterHandler<ChatMessage>(OnChatMessage);
            _messageDispatcher.RegisterHandler<WorldBlockChangeBroadcast>(OnBlockChangeBroadcast);
            _messageDispatcher.RegisterHandler<PingResponse>(OnPingResponse);
            
            _isInitialized = true;
            Debug.Log("ProtobufNetworkClient initialized");
        }

        /// <summary>
        /// Connects to the server asynchronously.
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
        /// Disconnects from the server.
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
        /// Called when data is received from the server.
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
        /// Attempts to parse a message.
        /// </summary>
        private bool TryParseMessage<T>(byte[] data, out T message) where T : IMessage, new()
        {
            try
            {
                message = new T();
                message.MergeFrom(data);
                return true;
            }
            catch
            {
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

        // Placeholder handlers for future messages (Move/Chat/Block/Ping) will be implemented when .proto is ready.

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

/// <summary>
/// Client-side mirror of server MessageType enum for framing.
/// Keep values in sync with SharedProtocol.MessageType.
/// </summary>
public enum ClientMessageType
{
    // 인증 관련
    LoginRequest = 1,
    LoginResponse = 2,
    LogoutRequest = 3,
    LogoutResponse = 4,

    // 이동 관련
    MoveRequest = 10,
    MoveResponse = 11,

    // 월드/블록 관련
    WorldBlockChangeRequest = 20,
    WorldBlockChangeResponse = 21,
    WorldBlockChangeBroadcast = 22,

    // 채팅 관련
    ChatRequest = 30,
    ChatResponse = 31,
    ChatMessage = 32,

    // 서버 상태/진단
    PingRequest = 40,
    PingResponse = 41,
    ServerStatusRequest = 42,
    ServerStatusResponse = 43,

    // 플레이어 정보 업데이트
    PlayerInfoUpdate = 50,
}
