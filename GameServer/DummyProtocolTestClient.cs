using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using ProtoBuf;

namespace GameServer
{
    /// <summary>
    /// Dummy protocol test client for comprehensive packet testing
    /// This client can test all protocol message types and validate protobuf serialization/deserialization
    /// </summary>
    public class DummyProtocolTestClient
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly string _serverHost;
        private readonly int _serverPort;
        private bool _isConnected;

        public DummyProtocolTestClient(string serverHost = "127.0.0.1", int serverPort = 7777)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
            _isConnected = false;
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_serverHost, _serverPort);
                _stream = _tcpClient.GetStream();
                _isConnected = true;
                Console.WriteLine($"[DummyClient] Connected to {_serverHost}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            if (_stream != null)
            {
                _stream.Close();
            }
            if (_tcpClient != null)
            {
                _tcpClient.Close();
            }
            _isConnected = false;
            Console.WriteLine("[DummyClient] Disconnected");
        }

        /// <summary>
        /// Send a login request
        /// </summary>
        public async Task<bool> SendLoginAsync(string username, string password = "test")
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                await SendMessageAsync(MessageType.LoginRequest, loginRequest);
                Console.WriteLine($"[DummyClient] Sent login request for {username}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Login failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a move request
        /// </summary>
        public async Task<bool> SendMoveAsync(float x, float y, float z, float yaw = 0, float pitch = 0)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var moveRequest = new MoveRequest
                {
                    Position = new Vector3 { X = x, Y = y, Z = z },
                    Rotation = new Vector3 { X = yaw, Y = pitch, Z = 0 }
                };

                await SendMessageAsync(MessageType.MoveRequest, moveRequest);
                Console.WriteLine($"[DummyClient] Sent move request to ({x}, {y}, {z})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Move failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a world block change request
        /// </summary>
        public async Task<bool> SendBlockChangeAsync(int x, int y, int z, int blockType)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var blockChangeRequest = new WorldBlockChangeRequest
                {
                    BlockPosition = new Vector3Int { X = x, Y = y, Z = z },
                    BlockType = blockType
                };

                await SendMessageAsync(MessageType.WorldBlockChangeRequest, blockChangeRequest);
                Console.WriteLine($"[DummyClient] Sent block change request at ({x}, {y}, {z}) to type {blockType}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Block change failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a chat message
        /// </summary>
        public async Task<bool> SendChatAsync(string message)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var chatRequest = new ChatRequest
                {
                    Message = message
                };

                await SendMessageAsync(MessageType.ChatRequest, chatRequest);
                Console.WriteLine($"[DummyClient] Sent chat message: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chat failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send an inventory request
        /// </summary>
        public async Task<bool> SendInventoryRequestAsync()
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var inventoryRequest = new InventoryRequest();

                await SendMessageAsync(MessageType.InventoryRequest, inventoryRequest);
                Console.WriteLine("[DummyClient] Sent inventory request");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Inventory request failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a player action request
        /// </summary>
        public async Task<bool> SendPlayerActionAsync(PlayerActionType action, int targetX = 0, int targetY = 0, int targetZ = 0)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var actionRequest = new PlayerActionRequestMessage
                {
                    Action = action,
                    TargetPosition = new Vector3Int { X = targetX, Y = targetY, Z = targetZ }
                };

                await SendMessageAsync(MessageType.PlayerActionRequest, actionRequest);
                Console.WriteLine($"[DummyClient] Sent player action: {action}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Player action failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a chunk data request
        /// </summary>
        public async Task<bool> SendChunkDataRequestAsync(int chunkX, int chunkZ, int viewDistance = 8)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var chunkRequest = new ChunkDataRequestMessage
                {
                    ChunkX = chunkX,
                    ChunkZ = chunkZ,
                    ViewDistance = viewDistance
                };

                await SendMessageAsync(MessageType.ChunkDataRequest, chunkRequest);
                Console.WriteLine($"[DummyClient] Sent chunk data request for ({chunkX}, {chunkZ})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chunk data request failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a ping request
        /// </summary>
        public async Task<bool> SendPingAsync()
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var pingRequest = new PingRequest();

                await SendMessageAsync(MessageType.PingRequest, pingRequest);
                Console.WriteLine("[DummyClient] Sent ping request");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Ping failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a server status request
        /// </summary>
        public async Task<bool> SendServerStatusAsync()
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var statusRequest = new ServerStatusRequest();

                await SendMessageAsync(MessageType.ServerStatusRequest, statusRequest);
                Console.WriteLine("[DummyClient] Sent server status request");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Server status request failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a message using protobuf-net serialization
        /// </summary>
        private async Task SendMessageAsync(MessageType messageType, object payload)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Stream is not initialized");
            }

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, payload);
            byte[] payloadData = ms.ToArray();

            // Create message header
            byte[] header = CreateMessageHeader((int)messageType, payloadData.Length);

            // Send header and payload
            await _stream.WriteAsync(header, 0, header.Length);
            await _stream.WriteAsync(payloadData, 0, payloadData.Length);
            await _stream.FlushAsync();
        }

        /// <summary>
        /// Create message header with type and length
        /// </summary>
        private byte[] CreateMessageHeader(int messageType, int payloadLength)
        {
            byte[] header = new byte[4];
            header[0] = (byte)(messageType & 0xFF);
            header[1] = (byte)((messageType >> 8) & 0xFF);
            header[2] = (byte)(payloadLength & 0xFF);
            header[3] = (byte)((payloadLength >> 8) & 0xFF);
            return header;
        }

        /// <summary>
        /// Run comprehensive protocol test
        /// </summary>
        public async Task RunProtocolTestAsync()
        {
            Console.WriteLine("[DummyClient] Starting comprehensive protocol test...");
            Console.WriteLine("==========================================");

            // Test 1: Connection
            Console.WriteLine("\n[Test 1] Connection Test");
            bool connected = await ConnectAsync();
            if (!connected)
            {
                Console.WriteLine("[DummyClient] Connection test FAILED");
                return;
            }
            Console.WriteLine("[DummyClient] Connection test PASSED");

            // Test 2: Login
            Console.WriteLine("\n[Test 2] Login Test");
            bool loggedIn = await SendLoginAsync("TestUser");
            await Task.Delay(1000); // Wait for response

            // Test 3: Movement
            Console.WriteLine("\n[Test 3] Movement Test");
            await SendMoveAsync(10.5f, 64.0f, 10.5f, 90.0f, 0.0f);
            await Task.Delay(500);

            // Test 4: Block Change
            Console.WriteLine("\n[Test 4] Block Change Test");
            await SendBlockChangeAsync(10, 64, 10, 1); // Stone block
            await Task.Delay(500);

            // Test 5: Chat
            Console.WriteLine("\n[Test 5] Chat Test");
            await SendChatAsync("Hello from dummy client!");
            await Task.Delay(500);

            // Test 6: Player Action
            Console.WriteLine("\n[Test 6] Player Action Test");
            await SendPlayerActionAsync(PlayerActionType.Jump);
            await Task.Delay(500);

            // Test 7: Inventory
            Console.WriteLine("\n[Test 7] Inventory Request Test");
            await SendInventoryRequestAsync();
            await Task.Delay(500);

            // Test 8: Chunk Data
            Console.WriteLine("\n[Test 8] Chunk Data Request Test");
            await SendChunkDataRequestAsync(0, 0, 8);
            await Task.Delay(500);

            // Test 9: Ping
            Console.WriteLine("\n[Test 9] Ping Test");
            await SendPingAsync();
            await Task.Delay(500);

            // Test 10: Server Status
            Console.WriteLine("\n[Test 10] Server Status Test");
            await SendServerStatusAsync();
            await Task.Delay(500);

            Console.WriteLine("\n==========================================");
            Console.WriteLine("[DummyClient] Protocol test completed");

            // Disconnect
            await Task.Delay(1000);
            Disconnect();
        }

        /// <summary>
        /// Test Enhanced Minecraft protocol messages (Google.Protobuf)
        /// </summary>
        public void TestEnhancedMinecraftProtocol()
        {
            Console.WriteLine("[DummyClient] Testing Enhanced Minecraft protocol messages...");

            // Test PlayerInfo
            var playerInfo = new PlayerInfo
            {
                PlayerId = "test_player",
                Username = "TestPlayer",
                Level = 1,
                Experience = 100,
                Health = 20.0f,
                MaxHealth = 20.0f,
                Hunger = 20.0f,
                MaxHunger = 20.0f,
                GameMode = MinecraftGame.Common.GameMode.Survival
            };

            Console.WriteLine($"[DummyClient] Created PlayerInfo: {playerInfo.Username}");

            // Test ItemStack
            var itemStack = new ItemStack
            {
                ItemId = 1,
                ItemName = "Stone",
                Count = 64,
                Durability = 100,
                MaxDurability = 100,
                ItemType = ItemType.BLOCK,
                Rarity = ItemRarity.COMMON
            };

            Console.WriteLine($"[DummyClient] Created ItemStack: {itemStack.ItemName} x{itemStack.Count}");

            // Test BlockBreakStartRequest
            var blockBreakRequest = new BlockBreakStartRequest
            {
                BlockPosition = new MinecraftGame.Common.Vector3Int { X = 10, Y = 64, Z = 10 },
                ToolItemId = 1,
                SequenceId = 1
            };

            Console.WriteLine($"[DummyClient] Created BlockBreakStartRequest at ({blockBreakRequest.BlockPosition.X}, {blockBreakRequest.BlockPosition.Y}, {blockBreakRequest.BlockPosition.Z})");

            // Test BlockPlaceRequest
            var blockPlaceRequest = new BlockPlaceRequest
            {
                BlockPosition = new MinecraftGame.Common.Vector3Int { X = 11, Y = 64, Z = 10 },
                BlockId = 2,
                BlockMetadata = 0,
                Face = 1
            };

            Console.WriteLine($"[DummyClient] Created BlockPlaceRequest at ({blockPlaceRequest.BlockPosition.X}, {blockPlaceRequest.BlockPosition.Y}, {blockPlaceRequest.BlockPosition.Z})");

            // Test ChunkLoadRequest
            var chunkLoadRequest = new ChunkLoadRequest
            {
                ViewDistance = 8
            };

            Console.WriteLine($"[DummyClient] Created ChunkLoadRequest with view distance {chunkLoadRequest.ViewDistance}");

            // Test ChatMessage
            var chatMessage = new EnhancedMinecraftProtocol.ChatMessage
            {
                SenderId = "test_player",
                SenderName = "TestPlayer",
                MessageContent = "Test chat message",
                ChatType = EnhancedMinecraftProtocol.ChatType.CHAT_GLOBAL,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            Console.WriteLine($"[DummyClient] Created ChatMessage: {chatMessage.MessageContent}");

            Console.WriteLine("[DummyClient] Enhanced Minecraft protocol test completed");
        }

        /// <summary>
        /// Validate protobuf serialization/deserialization
        /// </summary>
        public bool ValidateProtobufSerialization()
        {
            Console.WriteLine("[DummyClient] Validating protobuf serialization...");

            try
            {
                // Test LoginRequest serialization
                var loginRequest = new LoginRequest
                {
                    Username = "TestUser",
                    Password = "TestPass"
                };

                using var ms = new MemoryStream();
                Serializer.Serialize(ms, loginRequest);
                byte[] serialized = ms.ToArray();

                ms.Position = 0;
                var deserialized = Serializer.Deserialize<LoginRequest>(ms);

                if (deserialized.Username == loginRequest.Username &&
                    deserialized.Password == loginRequest.Password)
                {
                    Console.WriteLine("[DummyClient] LoginRequest serialization/deserialization PASSED");
                }
                else
                {
                    Console.WriteLine("[DummyClient] LoginRequest serialization/deserialization FAILED");
                    return false;
                }

                // Test MoveRequest serialization
                var moveRequest = new MoveRequest
                {
                    Position = new Vector3 { X = 10.5f, Y = 64.0f, Z = 10.5f },
                    Rotation = new Vector3 { X = 90.0f, Y = 0.0f, Z = 0.0f }
                };

                ms.Position = 0;
                Serializer.Serialize(ms, moveRequest);
                serialized = ms.ToArray();

                ms.Position = 0;
                var deserializedMove = Serializer.Deserialize<MoveRequest>(ms);

                if (Math.Abs(deserializedMove.Position.X - moveRequest.Position.X) < 0.001f &&
                    Math.Abs(deserializedMove.Position.Y - moveRequest.Position.Y) < 0.001f &&
                    Math.Abs(deserializedMove.Position.Z - moveRequest.Position.Z) < 0.001f)
                {
                    Console.WriteLine("[DummyClient] MoveRequest serialization/deserialization PASSED");
                }
                else
                {
                    Console.WriteLine("[DummyClient] MoveRequest serialization/deserialization FAILED");
                    return false;
                }

                Console.WriteLine("[DummyClient] All protobuf serialization tests PASSED");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Protobuf validation FAILED: {ex.Message}");
                return false;
            }
        }
    }
}
using System.Net.Sockets;
using System.Threading.Tasks;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using ProtoBuf;

namespace GameServer
{
    /// <summary>
    /// Dummy protocol test client for comprehensive packet testing
    /// This client can test all protocol message types and validate protobuf serialization/deserialization
    /// </summary>
    public class DummyProtocolTestClient
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly string _serverHost;
        private readonly int _serverPort;
        private bool _isConnected;

        public DummyProtocolTestClient(string serverHost = "127.0.0.1", int serverPort = 7777)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
            _isConnected = false;
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_serverHost, _serverPort);
                _stream = _tcpClient.GetStream();
                _isConnected = true;
                Console.WriteLine($"[DummyClient] Connected to {_serverHost}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            if (_stream != null)
            {
                _stream.Close();
            }
            if (_tcpClient != null)
            {
                _tcpClient.Close();
            }
            _isConnected = false;
            Console.WriteLine("[DummyClient] Disconnected");
        }

        /// <summary>
        /// Send a login request
        /// </summary>
        public async Task<bool> SendLoginAsync(string username, string password = "test")
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                await SendMessageAsync(MessageType.LoginRequest, loginRequest);
                Console.WriteLine($"[DummyClient] Sent login request for {username}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Login failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a move request
        /// </summary>
        public async Task<bool> SendMoveAsync(float x, float y, float z, float yaw = 0, float pitch = 0)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var moveRequest = new MoveRequest
                {
                    Position = new Vector3 { X = x, Y = y, Z = z },
                    Rotation = new Vector3 { X = yaw, Y = pitch, Z = 0 }
                };

                await SendMessageAsync(MessageType.MoveRequest, moveRequest);
                Console.WriteLine($"[DummyClient] Sent move request to ({x}, {y}, {z})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Move failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a world block change request
        /// </summary>
        public async Task<bool> SendBlockChangeAsync(int x, int y, int z, int blockType)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var blockChangeRequest = new WorldBlockChangeRequest
                {
                    BlockPosition = new Vector3Int { X = x, Y = y, Z = z },
                    BlockType = blockType
                };

                await SendMessageAsync(MessageType.WorldBlockChangeRequest, blockChangeRequest);
                Console.WriteLine($"[DummyClient] Sent block change request at ({x}, {y}, {z}) to type {blockType}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Block change failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a chat message
        /// </summary>
        public async Task<bool> SendChatAsync(string message)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var chatRequest = new ChatRequest
                {
                    Message = message
                };

                await SendMessageAsync(MessageType.ChatRequest, chatRequest);
                Console.WriteLine($"[DummyClient] Sent chat message: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chat failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send an inventory request
        /// </summary>
        public async Task<bool> SendInventoryRequestAsync()
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var inventoryRequest = new InventoryRequest();

                await SendMessageAsync(MessageType.InventoryRequest, inventoryRequest);
                Console.WriteLine("[DummyClient] Sent inventory request");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Inventory request failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a player action request
        /// </summary>
        public async Task<bool> SendPlayerActionAsync(PlayerActionType action, int targetX = 0, int targetY = 0, int targetZ = 0)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var actionRequest = new PlayerActionRequestMessage
                {
                    Action = action,
                    TargetPosition = new Vector3Int { X = targetX, Y = targetY, Z = targetZ }
                };

                await SendMessageAsync(MessageType.PlayerActionRequest, actionRequest);
                Console.WriteLine($"[DummyClient] Sent player action: {action}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Player action failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a chunk data request
        /// </summary>
        public async Task<bool> SendChunkDataRequestAsync(int chunkX, int chunkZ, int viewDistance = 8)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var chunkRequest = new ChunkDataRequestMessage
                {
                    ChunkX = chunkX,
                    ChunkZ = chunkZ,
                    ViewDistance = viewDistance
                };

                await SendMessageAsync(MessageType.ChunkDataRequest, chunkRequest);
                Console.WriteLine($"[DummyClient] Sent chunk data request for ({chunkX}, {chunkZ})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Chunk data request failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a ping request
        /// </summary>
        public async Task<bool> SendPingAsync()
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var pingRequest = new PingRequest();

                await SendMessageAsync(MessageType.PingRequest, pingRequest);
                Console.WriteLine("[DummyClient] Sent ping request");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Ping failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a server status request
        /// </summary>
        public async Task<bool> SendServerStatusAsync()
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[DummyClient] Not connected");
                return false;
            }

            try
            {
                var statusRequest = new ServerStatusRequest();

                await SendMessageAsync(MessageType.ServerStatusRequest, statusRequest);
                Console.WriteLine("[DummyClient] Sent server status request");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Server status request failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a message using protobuf-net serialization
        /// </summary>
        private async Task SendMessageAsync(MessageType messageType, object payload)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Stream is not initialized");
            }

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, payload);
            byte[] payloadData = ms.ToArray();

            // Create message header
            byte[] header = CreateMessageHeader((int)messageType, payloadData.Length);

            // Send header and payload
            await _stream.WriteAsync(header, 0, header.Length);
            await _stream.WriteAsync(payloadData, 0, payloadData.Length);
            await _stream.FlushAsync();
        }

        /// <summary>
        /// Create message header with type and length
        /// </summary>
        private byte[] CreateMessageHeader(int messageType, int payloadLength)
        {
            byte[] header = new byte[4];
            header[0] = (byte)(messageType & 0xFF);
            header[1] = (byte)((messageType >> 8) & 0xFF);
            header[2] = (byte)(payloadLength & 0xFF);
            header[3] = (byte)((payloadLength >> 8) & 0xFF);
            return header;
        }

        /// <summary>
        /// Run comprehensive protocol test
        /// </summary>
        public async Task RunProtocolTestAsync()
        {
            Console.WriteLine("[DummyClient] Starting comprehensive protocol test...");
            Console.WriteLine("==========================================");

            // Test 1: Connection
            Console.WriteLine("\n[Test 1] Connection Test");
            bool connected = await ConnectAsync();
            if (!connected)
            {
                Console.WriteLine("[DummyClient] Connection test FAILED");
                return;
            }
            Console.WriteLine("[DummyClient] Connection test PASSED");

            // Test 2: Login
            Console.WriteLine("\n[Test 2] Login Test");
            bool loggedIn = await SendLoginAsync("TestUser");
            await Task.Delay(1000); // Wait for response

            // Test 3: Movement
            Console.WriteLine("\n[Test 3] Movement Test");
            await SendMoveAsync(10.5f, 64.0f, 10.5f, 90.0f, 0.0f);
            await Task.Delay(500);

            // Test 4: Block Change
            Console.WriteLine("\n[Test 4] Block Change Test");
            await SendBlockChangeAsync(10, 64, 10, 1); // Stone block
            await Task.Delay(500);

            // Test 5: Chat
            Console.WriteLine("\n[Test 5] Chat Test");
            await SendChatAsync("Hello from dummy client!");
            await Task.Delay(500);

            // Test 6: Player Action
            Console.WriteLine("\n[Test 6] Player Action Test");
            await SendPlayerActionAsync(PlayerActionType.Jump);
            await Task.Delay(500);

            // Test 7: Inventory
            Console.WriteLine("\n[Test 7] Inventory Request Test");
            await SendInventoryRequestAsync();
            await Task.Delay(500);

            // Test 8: Chunk Data
            Console.WriteLine("\n[Test 8] Chunk Data Request Test");
            await SendChunkDataRequestAsync(0, 0, 8);
            await Task.Delay(500);

            // Test 9: Ping
            Console.WriteLine("\n[Test 9] Ping Test");
            await SendPingAsync();
            await Task.Delay(500);

            // Test 10: Server Status
            Console.WriteLine("\n[Test 10] Server Status Test");
            await SendServerStatusAsync();
            await Task.Delay(500);

            Console.WriteLine("\n==========================================");
            Console.WriteLine("[DummyClient] Protocol test completed");

            // Disconnect
            await Task.Delay(1000);
            Disconnect();
        }

        /// <summary>
        /// Test Enhanced Minecraft protocol messages (Google.Protobuf)
        /// </summary>
        public void TestEnhancedMinecraftProtocol()
        {
            Console.WriteLine("[DummyClient] Testing Enhanced Minecraft protocol messages...");

            // Test PlayerInfo
            var playerInfo = new PlayerInfo
            {
                PlayerId = "test_player",
                Username = "TestPlayer",
                Level = 1,
                Experience = 100,
                Health = 20.0f,
                MaxHealth = 20.0f,
                Hunger = 20.0f,
                MaxHunger = 20.0f,
                GameMode = MinecraftGame.Common.GameMode.Survival
            };

            Console.WriteLine($"[DummyClient] Created PlayerInfo: {playerInfo.Username}");

            // Test ItemStack
            var itemStack = new ItemStack
            {
                ItemId = 1,
                ItemName = "Stone",
                Count = 64,
                Durability = 100,
                MaxDurability = 100,
                ItemType = ItemType.BLOCK,
                Rarity = ItemRarity.COMMON
            };

            Console.WriteLine($"[DummyClient] Created ItemStack: {itemStack.ItemName} x{itemStack.Count}");

            // Test BlockBreakStartRequest
            var blockBreakRequest = new BlockBreakStartRequest
            {
                BlockPosition = new MinecraftGame.Common.Vector3Int { X = 10, Y = 64, Z = 10 },
                ToolItemId = 1,
                SequenceId = 1
            };

            Console.WriteLine($"[DummyClient] Created BlockBreakStartRequest at ({blockBreakRequest.BlockPosition.X}, {blockBreakRequest.BlockPosition.Y}, {blockBreakRequest.BlockPosition.Z})");

            // Test BlockPlaceRequest
            var blockPlaceRequest = new BlockPlaceRequest
            {
                BlockPosition = new MinecraftGame.Common.Vector3Int { X = 11, Y = 64, Z = 10 },
                BlockId = 2,
                BlockMetadata = 0,
                Face = 1
            };

            Console.WriteLine($"[DummyClient] Created BlockPlaceRequest at ({blockPlaceRequest.BlockPosition.X}, {blockPlaceRequest.BlockPosition.Y}, {blockPlaceRequest.BlockPosition.Z})");

            // Test ChunkLoadRequest
            var chunkLoadRequest = new ChunkLoadRequest
            {
                ViewDistance = 8
            };

            Console.WriteLine($"[DummyClient] Created ChunkLoadRequest with view distance {chunkLoadRequest.ViewDistance}");

            // Test ChatMessage
            var chatMessage = new EnhancedMinecraftProtocol.ChatMessage
            {
                SenderId = "test_player",
                SenderName = "TestPlayer",
                MessageContent = "Test chat message",
                ChatType = EnhancedMinecraftProtocol.ChatType.CHAT_GLOBAL,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            Console.WriteLine($"[DummyClient] Created ChatMessage: {chatMessage.MessageContent}");

            Console.WriteLine("[DummyClient] Enhanced Minecraft protocol test completed");
        }

        /// <summary>
        /// Validate protobuf serialization/deserialization
        /// </summary>
        public bool ValidateProtobufSerialization()
        {
            Console.WriteLine("[DummyClient] Validating protobuf serialization...");

            try
            {
                // Test LoginRequest serialization
                var loginRequest = new LoginRequest
                {
                    Username = "TestUser",
                    Password = "TestPass"
                };

                using var ms = new MemoryStream();
                Serializer.Serialize(ms, loginRequest);
                byte[] serialized = ms.ToArray();

                ms.Position = 0;
                var deserialized = Serializer.Deserialize<LoginRequest>(ms);

                if (deserialized.Username == loginRequest.Username &&
                    deserialized.Password == loginRequest.Password)
                {
                    Console.WriteLine("[DummyClient] LoginRequest serialization/deserialization PASSED");
                }
                else
                {
                    Console.WriteLine("[DummyClient] LoginRequest serialization/deserialization FAILED");
                    return false;
                }

                // Test MoveRequest serialization
                var moveRequest = new MoveRequest
                {
                    Position = new Vector3 { X = 10.5f, Y = 64.0f, Z = 10.5f },
                    Rotation = new Vector3 { X = 90.0f, Y = 0.0f, Z = 0.0f }
                };

                ms.Position = 0;
                Serializer.Serialize(ms, moveRequest);
                serialized = ms.ToArray();

                ms.Position = 0;
                var deserializedMove = Serializer.Deserialize<MoveRequest>(ms);

                if (Math.Abs(deserializedMove.Position.X - moveRequest.Position.X) < 0.001f &&
                    Math.Abs(deserializedMove.Position.Y - moveRequest.Position.Y) < 0.001f &&
                    Math.Abs(deserializedMove.Position.Z - moveRequest.Position.Z) < 0.001f)
                {
                    Console.WriteLine("[DummyClient] MoveRequest serialization/deserialization PASSED");
                }
                else
                {
                    Console.WriteLine("[DummyClient] MoveRequest serialization/deserialization FAILED");
                    return false;
                }

                Console.WriteLine("[DummyClient] All protobuf serialization tests PASSED");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Protobuf validation FAILED: {ex.Message}");
                return false;
            }
        }
    }
}

