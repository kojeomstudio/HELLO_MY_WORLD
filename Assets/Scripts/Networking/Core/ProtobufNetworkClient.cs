using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Google.Protobuf;
using GameProtocol;

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
        public event Action<MoveResponse> MoveResponseReceived;
        public event Action<ChatMessage> ChatMessageReceived;
        public event Action<WorldBlockChangeBroadcast> BlockChangeBroadcastReceived;
        public event Action<PingResponse> PingResponseReceived;

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
        /// Sends a login request.
        /// </summary>
        public void SendLogin(string username, string password, string clientVersion = "1.0.0")
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password,
                ClientVersion = clientVersion
            };
            
            SendMessage(request);
            Debug.Log($"Sent login request for user: {username}");
        }

        /// <summary>
        /// Sends a move request.
        /// </summary>
        public void SendMoveRequest(Vector3 targetPosition, float movementSpeed = 5.0f)
        {
            var request = new MoveRequest
            {
                TargetPosition = new GameProtocol.Vector3 
                { 
                    X = targetPosition.x, 
                    Y = targetPosition.y, 
                    Z = targetPosition.z 
                },
                MovementSpeed = movementSpeed
            };
            
            SendMessage(request);
            Debug.Log($"Sent move request to ({targetPosition.x:F2}, {targetPosition.y:F2}, {targetPosition.z:F2})");
        }

        /// <summary>
        /// Sends a chat message.
        /// </summary>
        public void SendChatMessage(string message, ChatType chatType = ChatType.Global, string targetPlayer = "")
        {
            var request = new ChatRequest
            {
                Message = message,
                Type = (int)chatType,
                TargetPlayer = targetPlayer
            };
            
            SendMessage(request);
            Debug.Log($"Sent chat message [{chatType}]: {message}");
        }

        /// <summary>
        /// Sends a block change request.
        /// </summary>
        public void SendBlockChangeRequest(string areaId, string subworldId, Vector3Int blockPosition, int blockType, int chunkType)
        {
            var request = new WorldBlockChangeRequest
            {
                AreaId = areaId,
                SubworldId = subworldId,
                BlockPosition = new GameProtocol.Vector3Int 
                { 
                    X = blockPosition.x, 
                    Y = blockPosition.y, 
                    Z = blockPosition.z 
                },
                BlockType = blockType,
                ChunkType = chunkType
            };
            
            SendMessage(request);
            Debug.Log($"Sent block change request at ({blockPosition.x}, {blockPosition.y}, {blockPosition.z})");
        }

        /// <summary>
        /// Sends a ping request.
        /// </summary>
        public void SendPing()
        {
            var request = new PingRequest
            {
                ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            SendMessage(request);
        }

        /// <summary>
        /// Sends a Protobuf message to the server.
        /// </summary>
        private void SendMessage(IMessage message)
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
                var data = memoryStream.ToArray();
                
                _transport.Send(new ArraySegment<byte>(data));
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
                using var memoryStream = new MemoryStream(data.Array, data.Offset, data.Count);
                
                // TODO: In actual implementation, message type headers are needed.
                // Currently using simple implementation that tries multiple message types.
                var messageBytes = memoryStream.ToArray();
                
                // Try parsing multiple message types (protocol headers needed in actual implementation)
                if (TryParseMessage<LoginResponse>(messageBytes, out var loginResponse))
                {
                    _messageDispatcher.Dispatch(loginResponse);
                }
                else if (TryParseMessage<MoveResponse>(messageBytes, out var moveResponse))
                {
                    _messageDispatcher.Dispatch(moveResponse);
                }
                else if (TryParseMessage<ChatMessage>(messageBytes, out var chatMessage))
                {
                    _messageDispatcher.Dispatch(chatMessage);
                }
                else if (TryParseMessage<WorldBlockChangeBroadcast>(messageBytes, out var blockBroadcast))
                {
                    _messageDispatcher.Dispatch(blockBroadcast);
                }
                else if (TryParseMessage<PingResponse>(messageBytes, out var pingResponse))
                {
                    _messageDispatcher.Dispatch(pingResponse);
                }
                else
                {
                    Debug.LogWarning("Received unknown message type");
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

        private void OnMoveResponse(MoveResponse response)
        {
            if (response.Success && response.NewPosition != null)
            {
                var pos = response.NewPosition;
                Debug.Log($"Move response: Success, New position=({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");
            }
            else
            {
                Debug.Log("Move response: Failed");
            }
            MoveResponseReceived?.Invoke(response);
        }

        private void OnChatMessage(ChatMessage message)
        {
            var chatType = (ChatType)message.Type;
            Debug.Log($"[{chatType}] {message.SenderName}: {message.Message}");
            ChatMessageReceived?.Invoke(message);
        }

        private void OnBlockChangeBroadcast(WorldBlockChangeBroadcast broadcast)
        {
            if (broadcast.BlockPosition != null)
            {
                var pos = broadcast.BlockPosition;
                Debug.Log($"Block changed by {broadcast.PlayerId}: ({pos.X}, {pos.Y}, {pos.Z}) -> Type {broadcast.BlockType}");
            }
            BlockChangeBroadcastReceived?.Invoke(broadcast);
        }

        private void OnPingResponse(PingResponse response)
        {
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.ClientTimestamp;
            Debug.Log($"Ping: {latency}ms");
            PingResponseReceived?.Invoke(response);
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