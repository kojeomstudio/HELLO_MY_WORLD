using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Google.Protobuf;
using GameProtocol;

namespace Networking.Core
{
    /// <summary>
    /// Protobuf 기반 네트워크 클라이언트
    /// 서버와의 통신에서 Protobuf 메시지 직렬화/역직렬화를 담당합니다.
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
        
        // 연결 상태 이벤트
        public event Action<bool> ConnectionStatusChanged;
        public event Action<string> ConnectionError;
        
        // 메시지 핸들러 이벤트들
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
            
            // TCP 전송 계층 생성
            _transport = new TcpNetworkTransport();
            _transport.ConnectionStatusChanged += OnConnectionStatusChanged;
            _transport.Received += OnDataReceived;
            
            // 메시지 디스패처 생성 및 핸들러 등록
            _messageDispatcher = new MessageDispatcher();
            RegisterMessageHandlers();
            
            _isInitialized = true;
            Debug.Log("ProtobufNetworkClient initialized");
        }

        private void RegisterMessageHandlers()
        {
            // 각 메시지 타입에 대한 핸들러 등록
            _messageDispatcher.Register<LoginResponse>(OnLoginResponse);
            _messageDispatcher.Register<MoveResponse>(OnMoveResponse);
            _messageDispatcher.Register<ChatMessage>(OnChatMessage);
            _messageDispatcher.Register<WorldBlockChangeBroadcast>(OnBlockChangeBroadcast);
            _messageDispatcher.Register<PingResponse>(OnPingResponse);
        }

        /// <summary>
        /// 서버에 연결합니다.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (!_isInitialized)
            {
                Debug.LogError("Client not initialized");
                return false;
            }

            if (IsConnected)
            {
                Debug.LogWarning("Already connected to server");
                return true;
            }

            try
            {
                Debug.Log($"Connecting to server at {serverAddress}:{serverPort}...");
                
                var connectTask = _transport.ConnectAsync(serverAddress, serverPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(connectionTimeout));
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    var error = "Connection timed out";
                    Debug.LogError(error);
                    ConnectionError?.Invoke(error);
                    return false;
                }
                
                await connectTask; // 예외가 있다면 여기서 발생
                
                Debug.Log("Successfully connected to server");
                return true;
            }
            catch (Exception ex)
            {
                var error = $"Failed to connect: {ex.Message}";
                Debug.LogError(error);
                ConnectionError?.Invoke(error);
                return false;
            }
        }

        /// <summary>
        /// 서버와의 연결을 끊습니다.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_transport != null && IsConnected)
            {
                await ((TcpNetworkTransport)_transport).DisconnectAsync();
            }
        }

        /// <summary>
        /// 로그인 요청을 전송합니다.
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
        /// 이동 요청을 전송합니다.
        /// </summary>
        public void SendMoveRequest(Vector3 targetPosition, float movementSpeed = 5.0f)
        {
            var request = new MoveRequest
            {\n                TargetPosition = new GameProtocol.Vector3 \n                { \n                    X = targetPosition.x, \n                    Y = targetPosition.y, \n                    Z = targetPosition.z \n                },\n                MovementSpeed = movementSpeed\n            };\n            \n            SendMessage(request);\n            Debug.Log($\"Sent move request to ({targetPosition.x:F2}, {targetPosition.y:F2}, {targetPosition.z:F2})\");\n        }\n\n        /// <summary>\n        /// 채팅 메시지를 전송합니다.\n        /// </summary>\n        public void SendChatMessage(string message, ChatType chatType = ChatType.Global, string targetPlayer = \"\")\n        {\n            var request = new ChatRequest\n            {\n                Message = message,\n                Type = (int)chatType,\n                TargetPlayer = targetPlayer\n            };\n            \n            SendMessage(request);\n            Debug.Log($\"Sent chat message [{chatType}]: {message}\");\n        }\n\n        /// <summary>\n        /// 블록 변경 요청을 전송합니다.\n        /// </summary>\n        public void SendBlockChangeRequest(string areaId, string subworldId, Vector3Int blockPosition, int blockType, int chunkType)\n        {\n            var request = new WorldBlockChangeRequest\n            {\n                AreaId = areaId,\n                SubworldId = subworldId,\n                BlockPosition = new GameProtocol.Vector3Int \n                { \n                    X = blockPosition.x, \n                    Y = blockPosition.y, \n                    Z = blockPosition.z \n                },\n                BlockType = blockType,\n                ChunkType = chunkType\n            };\n            \n            SendMessage(request);\n            Debug.Log($\"Sent block change request at ({blockPosition.x}, {blockPosition.y}, {blockPosition.z})\");\n        }\n\n        /// <summary>\n        /// 핑을 전송합니다.\n        /// </summary>\n        public void SendPing()\n        {\n            var request = new PingRequest\n            {\n                ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()\n            };\n            \n            SendMessage(request);\n        }\n\n        /// <summary>\n        /// Protobuf 메시지를 서버로 전송합니다.\n        /// </summary>\n        private void SendMessage(IMessage message)\n        {\n            if (!IsConnected)\n            {\n                Debug.LogWarning($\"Cannot send {message.GetType().Name}: not connected to server\");\n                return;\n            }\n\n            try\n            {\n                using var memoryStream = new MemoryStream();\n                message.WriteTo(memoryStream);\n                var data = memoryStream.ToArray();\n                \n                _transport.Send(new ArraySegment<byte>(data));\n            }\n            catch (Exception ex)\n            {\n                Debug.LogError($\"Failed to send {message.GetType().Name}: {ex.Message}\");\n            }\n        }\n\n        /// <summary>\n        /// 서버로부터 데이터를 수신했을 때 호출됩니다.\n        /// </summary>\n        private void OnDataReceived(ArraySegment<byte> data)\n        {\n            try\n            {\n                using var memoryStream = new MemoryStream(data.Array, data.Offset, data.Count);\n                \n                // TODO: 실제로는 메시지 타입을 구분하는 헤더가 필요합니다.\n                // 현재는 간단한 구현으로 메시지 타입을 추측합니다.\n                var messageBytes = memoryStream.ToArray();\n                \n                // 여러 메시지 타입을 시도해서 파싱 (실제로는 프로토콜 헤더 필요)\n                if (TryParseMessage<LoginResponse>(messageBytes, out var loginResponse))\n                {\n                    _messageDispatcher.Dispatch(loginResponse);\n                }\n                else if (TryParseMessage<MoveResponse>(messageBytes, out var moveResponse))\n                {\n                    _messageDispatcher.Dispatch(moveResponse);\n                }\n                else if (TryParseMessage<ChatMessage>(messageBytes, out var chatMessage))\n                {\n                    _messageDispatcher.Dispatch(chatMessage);\n                }\n                else if (TryParseMessage<WorldBlockChangeBroadcast>(messageBytes, out var blockBroadcast))\n                {\n                    _messageDispatcher.Dispatch(blockBroadcast);\n                }\n                else if (TryParseMessage<PingResponse>(messageBytes, out var pingResponse))\n                {\n                    _messageDispatcher.Dispatch(pingResponse);\n                }\n                else\n                {\n                    Debug.LogWarning(\"Received unknown message type\");\n                }\n            }\n            catch (Exception ex)\n            {\n                Debug.LogError($\"Failed to process received data: {ex.Message}\");\n            }\n        }\n\n        /// <summary>\n        /// 메시지 파싱을 시도합니다.\n        /// </summary>\n        private bool TryParseMessage<T>(byte[] data, out T message) where T : IMessage, new()\n        {\n            try\n            {\n                message = new T();\n                message.MergeFrom(data);\n                return true;\n            }\n            catch\n            {\n                message = default(T);\n                return false;\n            }\n        }\n\n        // 메시지 핸들러들\n        private void OnLoginResponse(LoginResponse response)\n        {\n            Debug.Log($\"Login response: Success={response.Success}, Message={response.Message}\");\n            LoginResponseReceived?.Invoke(response);\n        }\n\n        private void OnMoveResponse(MoveResponse response)\n        {\n            if (response.Success && response.NewPosition != null)\n            {\n                var pos = response.NewPosition;\n                Debug.Log($\"Move response: Success, New position=({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})\");\n            }\n            else\n            {\n                Debug.Log(\"Move response: Failed\");\n            }\n            MoveResponseReceived?.Invoke(response);\n        }\n\n        private void OnChatMessage(ChatMessage message)\n        {\n            var chatType = (ChatType)message.Type;\n            Debug.Log($\"[{chatType}] {message.SenderName}: {message.Message}\");\n            ChatMessageReceived?.Invoke(message);\n        }\n\n        private void OnBlockChangeBroadcast(WorldBlockChangeBroadcast broadcast)\n        {\n            if (broadcast.BlockPosition != null)\n            {\n                var pos = broadcast.BlockPosition;\n                Debug.Log($\"Block changed by {broadcast.PlayerId}: ({pos.X}, {pos.Y}, {pos.Z}) -> Type {broadcast.BlockType}\");\n            }\n            BlockChangeBroadcastReceived?.Invoke(broadcast);\n        }\n\n        private void OnPingResponse(PingResponse response)\n        {\n            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.ClientTimestamp;\n            Debug.Log($\"Ping: {latency}ms\");\n            PingResponseReceived?.Invoke(response);\n        }\n\n        private void OnConnectionStatusChanged(bool isConnected)\n        {\n            Debug.Log($\"Connection status changed: {isConnected}\");\n            ConnectionStatusChanged?.Invoke(isConnected);\n        }\n\n        private void OnDestroy()\n        {\n            if (_transport != null)\n            {\n                _transport.ConnectionStatusChanged -= OnConnectionStatusChanged;\n                _transport.Received -= OnDataReceived;\n                \n                if (_transport is IDisposable disposable)\n                {\n                    disposable.Dispose();\n                }\n            }\n        }\n    }\n\n    /// <summary>\n    /// 채팅 메시지 타입\n    /// </summary>\n    public enum ChatType\n    {\n        Global = 0,\n        Local = 1,\n        Whisper = 2,\n        System = 3\n    }\n}