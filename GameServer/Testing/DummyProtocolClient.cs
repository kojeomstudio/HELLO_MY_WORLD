using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;
using GameProtocol;

namespace GameServer.Testing
{
    /// <summary>
    /// Dummy client for testing Minecraft protocol packets
    /// Supports both protobuf-net and Google.Protobuf message types
    /// </summary>
    public class DummyProtocolClient : IDisposable
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _isConnected;
        private string _sessionId;
        private CancellationTokenSource _cancellationTokenSource;
        
        // Configuration
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private readonly int _connectionTimeoutMs;
        
        // Statistics
        private int _messagesSent;
        private int _messagesReceived;
        private int _errors;
        
        public bool IsConnected => _isConnected;
        public string SessionId => _sessionId;
        public int MessagesSent => _messagesSent;
        public int MessagesReceived => _messagesReceived;
        public int Errors => _errors;
        
        public event Action<string> OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        public event Action<LoginResponse> OnLoginResponse;
        public event Action<ChunkLoadResponse> OnChunkLoadResponse;
        public event Action<PlayerInfo> OnPlayerInfoUpdate;
        public event Action<BlockChangeBroadcast> OnBlockChangeBroadcast;
        public event Action<EntitySpawnBroadcast> OnEntitySpawnBroadcast;
        public event Action<EntityDespawnBroadcast> OnEntityDespawnBroadcast;
        public event Action<TimeUpdateBroadcast> OnTimeUpdateBroadcast;
        public event Action<WeatherUpdateBroadcast> OnWeatherUpdateBroadcast;
        public event Action<PingResponse> OnPingResponse;
        public event Action<ChatMessage> OnChatMessage;
        
        public DummyProtocolClient(string serverAddress = "127.0.0.1", int serverPort = 9000, int connectionTimeoutMs = 10000)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
            _connectionTimeoutMs = connectionTimeoutMs;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Connect to the server
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                Console.WriteLine($"[DummyClient] Connecting to {_serverAddress}:{_serverPort}...");
                
                _tcpClient = new TcpClient();
                var connectTask = _tcpClient.ConnectAsync(_serverAddress, _serverPort);
                var timeoutTask = Task.Delay(_connectionTimeoutMs, _cancellationTokenSource.Token);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("[DummyClient] Connection timed out");
                    OnError?.Invoke("Connection timeout");
                    return false;
                }
                
                await connectTask;
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;
                
                Console.WriteLine("[DummyClient] Connected successfully");
                OnConnected?.Invoke(_sessionId);
                
                // Start receiving messages
                _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Connection error: {ex.Message}");
                OnError?.Invoke($"Connection error: {ex.Message}");
                _errors++;
                return false;
            }
        }
        
        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (_isConnected)
                {
                    Console.WriteLine("[DummyClient] Disconnecting...");
                    _cancellationTokenSource.Cancel();
                    _networkStream?.Close();
                    _tcpClient?.Close();
                    _isConnected = false;
                    OnDisconnected?.Invoke(_sessionId);
                    Console.WriteLine("[DummyClient] Disconnected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Disconnect error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send login request
        /// </summary>
        public void SendLogin(string username, string password)
        {
            try
            {
                var request = new LoginRequest
                {
                    Username = username,
                    Password = password,
                    ClientVersion = "1.0.0"
                };
                
                SendMessage(MessageType.LoginRequest, request);
                Console.WriteLine($"[DummyClient] Sent login request for user: {username}");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Login error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send chunk load request
        /// </summary>
        public void SendChunkLoadRequest(int chunkX, int chunkZ, int viewDistance = 8)
        {
            try
            {
                var request = new ChunkLoadRequest
                {
                    ChunkPositions = { new MinecraftGame.Common.Vector3Int { X = chunkX, Y = 0, Z = chunkZ } },
                    ViewDistance = viewDistance
                };
                
                SendMessage(MinecraftMessageType.ChunkDataRequest, request);
                Console.WriteLine($"[DummyClient] Sent chunk load request: ({chunkX}, {chunkZ}), view distance: {viewDistance}");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chunk load request error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send chunk unload acknowledge
        /// </summary>
        public void SendChunkUnloadAck(int chunkX, int chunkZ, bool accepted = true, string note = "")
        {
            try
            {
                var ack = new ChunkUnloadAck
                {
                    ChunkX = chunkX,
                    ChunkZ = chunkZ,
                    Accepted = accepted,
                    RemainingChunks = 0,
                    Note = note
                };
                
                SendMessage(MinecraftMessageType.ChunkUnloadAcknowledge, ack);
                Console.WriteLine($"[DummyClient] Sent chunk unload ack: ({chunkX}, {chunkZ}), accepted: {accepted}");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chunk unload ack error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send player action request
        /// </summary>
        public void SendPlayerAction(PlayerAction action, int x, int y, int z, int face = 0, string itemId = "")
        {
            try
            {
                var request = new PlayerActionRequest
                {
                    Action = action,
                    TargetPosition = new MinecraftGame.Common.Vector3Int { X = x, Y = y, Z = z },
                    Face = face,
                    CursorPosition = new MinecraftGame.Common.Vector3 { X = 0.5f, Y = 0.5f, Z = 0.5f },
                    Sequence = 0,
                    ActionData = new ActionData
                    {
                        TargetEntityId = "",
                        ChargeProgress = 0.0f,
                        HeldTicks = 0
                    }
                };
                
                SendMessage(MinecraftMessageType.PlayerActionRequest, request);
                Console.WriteLine($"[DummyClient] Sent player action: {action} at ({x}, {y}, {z})");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Player action error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send ping request
        /// </summary>
        public void SendPing()
        {
            try
            {
                var request = new PingRequest
                {
                    ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                
                SendMessage(MinecraftMessageType.PingRequest, request);
                Console.WriteLine($"[DummyClient] Sent ping request");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Ping error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send chat message
        /// </summary>
        public void SendChatMessage(string message, ChatType chatType = ChatType.Global)
        {
            try
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = _sessionId ?? "unknown",
                    SenderName = "DummyClient",
                    MessageContent = message,
                    ChatType = (int)chatType,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    FormattedMessage = message,
                    Style = new ChatStyle
                    {
                        Color = "#FFFFFF",
                        Bold = false,
                        Italic = false,
                        Underlined = false,
                        Strikethrough = false,
                        Obfuscated = false
                    }
                };
                
                SendMessage(MessageType.ChatMessage, chatMessage);
                Console.WriteLine($"[DummyClient] Sent chat message: {message}");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chat message error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send server status request
        /// </summary>
        public void SendServerStatusRequest()
        {
            try
            {
                var request = new ServerStatusRequest
                {
                    SessionToken = _sessionId ?? ""
                };
                
                SendMessage(MessageType.ServerStatusRequest, request);
                Console.WriteLine("[DummyClient] Sent server status request");
                _messagesSent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Server status request error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send message using protobuf-net serialization
        /// </summary>
        private void SendMessage(MessageType messageType, object message)
        {
            if (!_isConnected || _networkStream == null)
            {
                Console.WriteLine("[DummyClient] Not connected, cannot send message");
                _errors++;
                return;
            }
            
            try
            {
                using var memoryStream = new MemoryStream();
                ProtoBuf.Serializer.Serialize(memoryStream, message);
                
                var messageData = memoryStream.ToArray();
                var packet = BuildPacket(messageType, messageData);
                
                _networkStream.Write(packet, 0, packet.Length);
                _networkStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Send message error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Send message using Google.Protobuf serialization
        /// </summary>
        private void SendMessage(MinecraftMessageType messageType, IMessage message)
        {
            if (!_isConnected || _networkStream == null)
            {
                Console.WriteLine("[DummyClient] Not connected, cannot send message");
                _errors++;
                return;
            }
            
            try
            {
                using var memoryStream = new MemoryStream();
                message.WriteTo(memoryStream);
                
                var messageData = memoryStream.ToArray();
                var packet = BuildPacket(messageType, messageData);
                
                _networkStream.Write(packet, 0, packet.Length);
                _networkStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Send message error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Build packet with length and type header
        /// </summary>
        private byte[] BuildPacket(MessageType messageType, byte[] messageData)
        {
            var lengthBytes = BitConverter.GetBytes(messageData.Length + 4); // +4 for message type
            var typeBytes = BitConverter.GetBytes((int)messageType);
            
            var packet = new byte[lengthBytes.Length + typeBytes.Length + messageData.Length];
            Buffer.BlockCopy(lengthBytes, 0, packet, 0, lengthBytes.Length);
            Buffer.BlockCopy(typeBytes, 0, packet, lengthBytes.Length, typeBytes.Length);
            Buffer.BlockCopy(messageData, 0, packet, lengthBytes.Length + typeBytes.Length, messageData.Length);
            
            return packet;
        }
        
        /// <summary>
        /// Receive messages from server
        /// </summary>
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[65536];
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _isConnected)
            {
                try
                {
                    var bytesRead = await _networkStream.ReadAsync(buffer, 0, 4, _cancellationTokenSource.Token);
                    
                    if (bytesRead < 4)
                    {
                        Console.WriteLine("[DummyClient] Incomplete packet header");
                        continue;
                    }
                    
                    var messageLength = BitConverter.ToInt32(buffer, 0);
                    var totalLength = messageLength + 4;
                    
                    if (messageLength > buffer.Length)
                    {
                        Console.WriteLine($"[DummyClient] Message too large: {messageLength}");
                        continue;
                    }
                    
                    var totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        var read = await _networkStream.ReadAsync(buffer, 4 + totalRead, messageLength - totalRead, _cancellationTokenSource.Token);
                        if (read == 0)
                        {
                            Console.WriteLine("[DummyClient] Connection closed");
                            break;
                        }
                        totalRead += read;
                    }
                    
                    if (totalRead < messageLength)
                    {
                        continue;
                    }
                    
                    var messageData = new byte[messageLength];
                    Buffer.BlockCopy(buffer, 4, messageData, 0, messageLength);
                    
                    var messageTypeValue = BitConverter.ToInt32(messageData, 0);
                    var messageType = (MessageType)messageTypeValue;
                    
                    var payload = new byte[messageLength - 4];
                    Buffer.BlockCopy(messageData, 4, payload, 0, payload.Length);
                    
                    await HandleMessageAsync(messageType, payload);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DummyClient] Receive error: {ex.Message}");
                    _errors++;
                }
            }
        }
        
        /// <summary>
        /// Handle received message
        /// </summary>
        private async Task HandleMessageAsync(MessageType messageType, byte[] payload)
        {
            try
            {
                switch (messageType)
                {
                    case MessageType.LoginResponse:
                        var loginResponse = ProtoBuf.Serializer.Deserialize<LoginResponse>(new MemoryStream(payload));
                        OnLoginResponse?.Invoke(loginResponse);
                        Console.WriteLine($"[DummyClient] Received login response: Success={loginResponse.Success}");
                        break;
                        
                    case MessageType.PingResponse:
                        var pingResponse = ProtoBuf.Serializer.Deserialize<PingResponse>(new MemoryStream(payload));
                        OnPingResponse?.Invoke(pingResponse);
                        var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pingResponse.ClientTimestamp;
                        Console.WriteLine($"[DummyClient] Received ping response: Latency={latency}ms");
                        break;
                        
                    case MessageType.ChatMessage:
                        var chatMessage = ProtoBuf.Serializer.Deserialize<ChatMessage>(new MemoryStream(payload));
                        OnChatMessage?.Invoke(chatMessage);
                        Console.WriteLine($"[DummyClient] Received chat: {chatMessage.SenderName}: {chatMessage.MessageContent}");
                        break;
                        
                    case MessageType.ServerStatusResponse:
                        var statusResponse = ProtoBuf.Serializer.Deserialize<ServerStatusResponse>(new MemoryStream(payload));
                        Console.WriteLine($"[DummyClient] Received server status: Online={statusResponse.OnlinePlayers}");
                        break;
                        
                    case MessageType.PlayerInfoUpdate:
                        var playerInfoUpdate = ProtoBuf.Serializer.Deserialize<PlayerInfoUpdate>(new MemoryStream(payload));
                        OnPlayerInfoUpdate?.Invoke(playerInfoUpdate.PlayerInfo);
                        Console.WriteLine($"[DummyClient] Received player info update");
                        break;
                        
                    case MessageType.WorldBlockChangeBroadcast:
                        var blockChange = ProtoBuf.Serializer.Deserialize<WorldBlockChangeBroadcast>(new MemoryStream(payload));
                        Console.WriteLine($"[DummyClient] Received block change: ({blockChange.BlockPosition.X}, {blockChange.BlockPosition.Y}, {blockChange.BlockPosition.Z}) -> {blockChange.BlockType}");
                        break;
                        
                    case MinecraftMessageType.ChunkDataResponse:
                        var chunkResponse = ChunkLoadResponse.Parser.ParseFrom(payload);
                        OnChunkLoadResponse?.Invoke(chunkResponse);
                        Console.WriteLine($"[DummyClient] Received chunk response: {chunkResponse.Chunks.Count} chunks");
                        break;
                        
                    case MinecraftMessageType.BlockChangeNotification:
                        var blockBroadcast = BlockChangeBroadcast.Parser.ParseFrom(payload);
                        OnBlockChangeBroadcast?.Invoke(blockBroadcast);
                        Console.WriteLine($"[DummyClient] Received block change broadcast");
                        break;
                        
                    case MinecraftMessageType.PlayerStateUpdate:
                        var playerInfo = PlayerInfo.Parser.ParseFrom(payload);
                        OnPlayerInfoUpdate?.Invoke(playerInfo);
                        Console.WriteLine($"[DummyClient] Received player state update");
                        break;
                        
                    case MinecraftMessageType.EntitySpawn:
                        var entitySpawn = EntitySpawnBroadcast.Parser.ParseFrom(payload);
                        OnEntitySpawnBroadcast?.Invoke(entitySpawn);
                        Console.WriteLine($"[DummyClient] Received entity spawn: {entitySpawn.Entity?.EntityId}");
                        break;
                        
                    case MinecraftMessageType.EntityDespawn:
                        var entityDespawn = EntityDespawnBroadcast.Parser.ParseFrom(payload);
                        OnEntityDespawnBroadcast?.Invoke(entityDespawn);
                        Console.WriteLine($"[DummyClient] Received entity despawn: {entityDespawn.EntityId}");
                        break;
                        
                    case MinecraftMessageType.TimeUpdate:
                        var timeUpdate = TimeUpdateBroadcast.Parser.ParseFrom(payload);
                        OnTimeUpdateBroadcast?.Invoke(timeUpdate);
                        Console.WriteLine($"[DummyClient] Received time update: {timeUpdate.WorldTime}");
                        break;
                        
                    case MinecraftMessageType.WeatherChange:
                        var weatherUpdate = WeatherUpdateBroadcast.Parser.ParseFrom(payload);
                        OnWeatherUpdateBroadcast?.Invoke(weatherUpdate);
                        Console.WriteLine($"[DummyClient] Received weather update: {weatherUpdate.Weather.WeatherType}");
                        break;
                        
                    case MinecraftMessageType.ParticleEffect:
                        var particleEffect = ParticleEffect.Parser.ParseFrom(payload);
                        Console.WriteLine($"[DummyClient] Received particle effect: {particleEffect.ParticleType}");
                        break;
                        
                    case MinecraftMessageType.SoundEffect:
                        var soundEffect = SoundEffect.Parser.ParseFrom(payload);
                        Console.WriteLine($"[DummyClient] Received sound effect: {soundEffect.SoundType}");
                        break;
                        
                    default:
                        Console.WriteLine($"[DummyClient] Unknown message type: {messageType}");
                        break;
                }
                
                _messagesReceived++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Handle message error: {ex.Message}");
                _errors++;
            }
        }
        
        /// <summary>
        /// Print statistics
        /// </summary>
        public void PrintStatistics()
        {
            Console.WriteLine("\n=== Dummy Client Statistics ===");
            Console.WriteLine($"Connected: {_isConnected}");
            Console.WriteLine($"Session ID: {_sessionId ?? "N/A"}");
            Console.WriteLine($"Messages Sent: {_messagesSent}");
            Console.WriteLine($"Messages Received: {_messagesReceived}");
            Console.WriteLine($"Errors: {_errors}");
            Console.WriteLine("===============================\n");
        }
        
        /// <summary>
        /// Run automated test sequence
        /// </summary>
        public async Task RunTestSequenceAsync()
        {
            Console.WriteLine("\n=== Starting Test Sequence ===\n");
            
            // Test 1: Login
            Console.WriteLine("Test 1: Login");
            SendLogin("testuser", "testpass");
            await Task.Delay(1000);
            
            // Test 2: Request chunks
            Console.WriteLine("\nTest 2: Request chunks");
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    SendChunkLoadRequest(x, z, 8);
                    await Task.Delay(100);
                }
            }
            await Task.Delay(2000);
            
            // Test 3: Player actions
            Console.WriteLine("\nTest 3: Player actions");
            SendPlayerAction(PlayerAction.PLACE_BLOCK, 100, 64, 100);
            await Task.Delay(500);
            SendPlayerAction(PlayerAction.START_DESTROY_BLOCK, 100, 64, 100);
            await Task.Delay(500);
            SendPlayerAction(PlayerAction.FINISH_DESTROY_BLOCK, 100, 64, 100);
            await Task.Delay(500);
            
            // Test 4: Chat
            Console.WriteLine("\nTest 4: Chat");
            SendChatMessage("Hello from dummy client!", ChatType.Global);
            await Task.Delay(1000);
            
            // Test 5: Ping
            Console.WriteLine("\nTest 5: Ping");
            SendPing();
            await Task.Delay(1000);
            
            // Test 6: Server status
            Console.WriteLine("\nTest 6: Server status");
            SendServerStatusRequest();
            await Task.Delay(1000);
            
            Console.WriteLine("\n=== Test Sequence Complete ===\n");
            PrintStatistics();
        }
        
        public void Dispose()
        {
            Disconnect();
            _cancellationTokenSource?.Dispose();
        }
    }
}
