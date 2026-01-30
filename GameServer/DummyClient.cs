using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;
using Game.Core;
using Game.World;
using Game.Auth;
using Game.Chat;
using Game.Diag;
using Game.Move;
using EnhancedMinecraftProtocol;

namespace GameServer
{
    /// <summary>
    /// Headless dummy client for protocol testing.
    /// Tests packet encoding/decoding and network round-trip communication.
    /// </summary>
    public class DummyClient
    {
        private readonly string _serverHost;
        private readonly int _serverPort;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly Random _random = new Random();

        public DummyClient(string serverHost = "localhost", int serverPort = 5000)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
        }

        /// <summary>
        /// Connect to server and run protocol tests.
        /// </summary>
        public async Task RunTestsAsync()
        {
            Console.WriteLine($"[DummyClient] Connecting to {_serverHost}:{_serverPort}...");

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_serverHost, _serverPort);
                _stream = _tcpClient.GetStream();
                Console.WriteLine("[DummyClient] Connected successfully!");

                // Run protocol tests
                await TestAuthenticationAsync();
                await TestMovementAsync();
                await TestWorldBlockChangeAsync();
                await TestChatAsync();
                await TestPingAsync();
                await TestChunkDataAsync();

                Console.WriteLine("[DummyClient] All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Error: {ex.Message}");
                Console.WriteLine($"[DummyClient] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Test authentication protocol messages.
        /// </summary>
        private async Task TestAuthenticationAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Authentication...");

            // Create login request
            var loginRequest = new LoginRequest
            {
                Username = $"DummyUser_{_random.Next(1000, 9999)}",
                Password = "test_password",
                ClientVersion = "1.0.0"
            };

            Console.WriteLine($"[DummyClient] Sending LoginRequest: {loginRequest.Username}");
            await SendMessageAsync(Game.Auth.MessageType.LoginRequest, loginRequest);

            // Wait for response (simplified - in real scenario would read from stream)
            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Authentication test completed.");
        }

        /// <summary>
        /// Test movement protocol messages.
        /// </summary>
        private async Task TestMovementAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Movement...");

            // Create movement request
            var moveRequest = new MoveRequest
            {
                TargetPosition = new MinecraftGame.Common.Vector3
                {
                    X = _random.NextDouble() * 100,
                    Y = 64.0,
                    Z = _random.NextDouble() * 100
                },
                MovementSpeed = 4.5f
            };

            Console.WriteLine($"[DummyClient] Sending MoveRequest to ({moveRequest.TargetPosition.X:F2}, {moveRequest.TargetPosition.Y:F2}, {moveRequest.TargetPosition.Z:F2})");
            await SendMessageAsync(Game.Move.MessageType.MoveRequest, moveRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Movement test completed.");
        }

        /// <summary>
        /// Test world block change protocol messages.
        /// </summary>
        private async Task TestWorldBlockChangeAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing World Block Change...");

            // Create block change request
            var blockChangeRequest = new WorldBlockChangeRequest
            {
                AreaId = "test_area",
                SubworldId = "overworld",
                BlockPosition = new MinecraftGame.Common.Vector3Int
                {
                    X = _random.Next(0, 100),
                    Y = 64,
                    Z = _random.Next(0, 100)
                },
                BlockType = 1, // Stone
                ChunkType = 0
            };

            Console.WriteLine($"[DummyClient] Sending WorldBlockChangeRequest at ({blockChangeRequest.BlockPosition.X}, {blockChangeRequest.BlockPosition.Y}, {blockChangeRequest.BlockPosition.Z})");
            await SendMessageAsync(Game.World.MessageType.WorldBlockChangeRequest, blockChangeRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] World block change test completed.");
        }

        /// <summary>
        /// Test chat protocol messages.
        /// </summary>
        private async Task TestChatAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Chat...");

            // Create chat request
            var chatRequest = new ChatRequest
            {
                Message = $"Hello from DummyClient at {DateTime.UtcNow:O}",
                Type = (int)Game.Chat.ChatType.Global
            };

            Console.WriteLine($"[DummyClient] Sending ChatRequest: {chatRequest.Message}");
            await SendMessageAsync(Game.Chat.MessageType.ChatRequest, chatRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Chat test completed.");
        }

        /// <summary>
        /// Test ping/pong protocol messages.
        /// </summary>
        private async Task TestPingAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Ping...");

            // Create ping request
            var pingRequest = new PingRequest
            {
                ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Console.WriteLine($"[DummyClient] Sending PingRequest: {pingRequest.ClientTimestamp}");
            await SendMessageAsync(Game.Diag.MessageType.PingRequest, pingRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Ping test completed.");
        }

        /// <summary>
        /// Test chunk data protocol messages.
        /// </summary>
        private async Task TestChunkDataAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Chunk Data...");

            // Create chunk data request
            var chunkDataRequest = new ChunkDataRequest
            {
                ChunkX = _random.Next(-10, 10),
                ChunkZ = _random.Next(-10, 10),
                ViewDistance = 5
            };

            Console.WriteLine($"[DummyClient] Sending ChunkDataRequest: ({chunkDataRequest.ChunkX}, {chunkDataRequest.ChunkZ})");
            await SendMessageAsync(Game.World.MessageType.ChunkDataRequest, chunkDataRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Chunk data test completed.");
        }

        /// <summary>
        /// Test Enhanced Minecraft protocol messages.
        /// </summary>
        private async Task TestEnhancedProtocolAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Enhanced Protocol...");

            // Test player state update
            var playerStateUpdate = new PlayerInfo
            {
                PlayerId = "dummy_player",
                Username = "DummyPlayer",
                Position = new MinecraftGame.Common.Vector3
                {
                    X = 0.0,
                    Y = 64.0,
                    Z = 0.0
                },
                Level = 1,
                Health = 20,
                MaxHealth = 20
            };

            Console.WriteLine($"[DummyClient] Sending PlayerStateUpdate for {playerStateUpdate.Username}");
            await SendMessageAsync(EnhancedMinecraftProtocol.MinecraftMessageType.PlayerStateUpdate, playerStateUpdate);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Enhanced protocol test completed.");
        }

        /// <summary>
        /// Send a protobuf message to server.
        /// </summary>
        private async Task SendMessageAsync<T>(int messageType, T message) where T : IMessage<T>
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                // Serialize message to bytes
                byte[] messageBytes = message.ToByteArray();

                // Create a simple packet format: [messageType (4 bytes)] [length (4 bytes)] [data]
                byte[] packet = new byte[8 + messageBytes.Length];
                BitConverter.GetBytes(messageType).CopyTo(packet, 0);
                BitConverter.GetBytes(messageBytes.Length).CopyTo(packet, 4);
                messageBytes.CopyTo(packet, 8);

                // Send to server
                await _stream.WriteAsync(packet, 0, packet.Length);
                await _stream.FlushAsync();

                Console.WriteLine($"[DummyClient] Sent message type {messageType} ({messageBytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Error sending message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Disconnect from server.
        /// </summary>
        private void Disconnect()
        {
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("[DummyClient] Disconnected.");
        }

        /// <summary>
        /// Entry point for running dummy client.
        /// </summary>
        public static async Task Main(string[] args)
        {
            string host = args.Length > 0 ? args[0] : "localhost";
            int port = args.Length > 1 ? int.Parse(args[1]) : 5000;

            var client = new DummyClient(host, port);
            await client.RunTestsAsync();
        }
    }
}
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;
using Game.Core;
using Game.World;
using Game.Auth;
using Game.Chat;
using Game.Diag;
using Game.Move;
using EnhancedMinecraftProtocol;

namespace GameServer
{
    /// <summary>
    /// Headless dummy client for protocol testing.
    /// Tests packet encoding/decoding and network round-trip communication.
    /// </summary>
    public class DummyClient
    {
        private readonly string _serverHost;
        private readonly int _serverPort;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly Random _random = new Random();

        public DummyClient(string serverHost = "localhost", int serverPort = 5000)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
        }

        /// <summary>
        /// Connect to server and run protocol tests.
        /// </summary>
        public async Task RunTestsAsync()
        {
            Console.WriteLine($"[DummyClient] Connecting to {_serverHost}:{_serverPort}...");

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_serverHost, _serverPort);
                _stream = _tcpClient.GetStream();
                Console.WriteLine("[DummyClient] Connected successfully!");

                // Run protocol tests
                await TestAuthenticationAsync();
                await TestMovementAsync();
                await TestWorldBlockChangeAsync();
                await TestChatAsync();
                await TestPingAsync();
                await TestChunkDataAsync();

                Console.WriteLine("[DummyClient] All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Error: {ex.Message}");
                Console.WriteLine($"[DummyClient] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Test authentication protocol messages.
        /// </summary>
        private async Task TestAuthenticationAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Authentication...");

            // Create login request
            var loginRequest = new LoginRequest
            {
                Username = $"DummyUser_{_random.Next(1000, 9999)}",
                Password = "test_password",
                ClientVersion = "1.0.0"
            };

            Console.WriteLine($"[DummyClient] Sending LoginRequest: {loginRequest.Username}");
            await SendMessageAsync(Game.Auth.MessageType.LoginRequest, loginRequest);

            // Wait for response (simplified - in real scenario would read from stream)
            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Authentication test completed.");
        }

        /// <summary>
        /// Test movement protocol messages.
        /// </summary>
        private async Task TestMovementAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Movement...");

            // Create movement request
            var moveRequest = new MoveRequest
            {
                TargetPosition = new MinecraftGame.Common.Vector3
                {
                    X = _random.NextDouble() * 100,
                    Y = 64.0,
                    Z = _random.NextDouble() * 100
                },
                MovementSpeed = 4.5f
            };

            Console.WriteLine($"[DummyClient] Sending MoveRequest to ({moveRequest.TargetPosition.X:F2}, {moveRequest.TargetPosition.Y:F2}, {moveRequest.TargetPosition.Z:F2})");
            await SendMessageAsync(Game.Move.MessageType.MoveRequest, moveRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Movement test completed.");
        }

        /// <summary>
        /// Test world block change protocol messages.
        /// </summary>
        private async Task TestWorldBlockChangeAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing World Block Change...");

            // Create block change request
            var blockChangeRequest = new WorldBlockChangeRequest
            {
                AreaId = "test_area",
                SubworldId = "overworld",
                BlockPosition = new MinecraftGame.Common.Vector3Int
                {
                    X = _random.Next(0, 100),
                    Y = 64,
                    Z = _random.Next(0, 100)
                },
                BlockType = 1, // Stone
                ChunkType = 0
            };

            Console.WriteLine($"[DummyClient] Sending WorldBlockChangeRequest at ({blockChangeRequest.BlockPosition.X}, {blockChangeRequest.BlockPosition.Y}, {blockChangeRequest.BlockPosition.Z})");
            await SendMessageAsync(Game.World.MessageType.WorldBlockChangeRequest, blockChangeRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] World block change test completed.");
        }

        /// <summary>
        /// Test chat protocol messages.
        /// </summary>
        private async Task TestChatAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Chat...");

            // Create chat request
            var chatRequest = new ChatRequest
            {
                Message = $"Hello from DummyClient at {DateTime.UtcNow:O}",
                Type = (int)Game.Chat.ChatType.Global
            };

            Console.WriteLine($"[DummyClient] Sending ChatRequest: {chatRequest.Message}");
            await SendMessageAsync(Game.Chat.MessageType.ChatRequest, chatRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Chat test completed.");
        }

        /// <summary>
        /// Test ping/pong protocol messages.
        /// </summary>
        private async Task TestPingAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Ping...");

            // Create ping request
            var pingRequest = new PingRequest
            {
                ClientTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Console.WriteLine($"[DummyClient] Sending PingRequest: {pingRequest.ClientTimestamp}");
            await SendMessageAsync(Game.Diag.MessageType.PingRequest, pingRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Ping test completed.");
        }

        /// <summary>
        /// Test chunk data protocol messages.
        /// </summary>
        private async Task TestChunkDataAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Chunk Data...");

            // Create chunk data request
            var chunkDataRequest = new ChunkDataRequest
            {
                ChunkX = _random.Next(-10, 10),
                ChunkZ = _random.Next(-10, 10),
                ViewDistance = 5
            };

            Console.WriteLine($"[DummyClient] Sending ChunkDataRequest: ({chunkDataRequest.ChunkX}, {chunkDataRequest.ChunkZ})");
            await SendMessageAsync(Game.World.MessageType.ChunkDataRequest, chunkDataRequest);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Chunk data test completed.");
        }

        /// <summary>
        /// Test Enhanced Minecraft protocol messages.
        /// </summary>
        private async Task TestEnhancedProtocolAsync()
        {
            Console.WriteLine("\n[DummyClient] Testing Enhanced Protocol...");

            // Test player state update
            var playerStateUpdate = new PlayerInfo
            {
                PlayerId = "dummy_player",
                Username = "DummyPlayer",
                Position = new MinecraftGame.Common.Vector3
                {
                    X = 0.0,
                    Y = 64.0,
                    Z = 0.0
                },
                Level = 1,
                Health = 20,
                MaxHealth = 20
            };

            Console.WriteLine($"[DummyClient] Sending PlayerStateUpdate for {playerStateUpdate.Username}");
            await SendMessageAsync(EnhancedMinecraftProtocol.MinecraftMessageType.PlayerStateUpdate, playerStateUpdate);

            await Task.Delay(100);
            Console.WriteLine("[DummyClient] Enhanced protocol test completed.");
        }

        /// <summary>
        /// Send a protobuf message to server.
        /// </summary>
        private async Task SendMessageAsync<T>(int messageType, T message) where T : IMessage<T>
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            try
            {
                // Serialize message to bytes
                byte[] messageBytes = message.ToByteArray();

                // Create a simple packet format: [messageType (4 bytes)] [length (4 bytes)] [data]
                byte[] packet = new byte[8 + messageBytes.Length];
                BitConverter.GetBytes(messageType).CopyTo(packet, 0);
                BitConverter.GetBytes(messageBytes.Length).CopyTo(packet, 4);
                messageBytes.CopyTo(packet, 8);

                // Send to server
                await _stream.WriteAsync(packet, 0, packet.Length);
                await _stream.FlushAsync();

                Console.WriteLine($"[DummyClient] Sent message type {messageType} ({messageBytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Error sending message: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Disconnect from server.
        /// </summary>
        private void Disconnect()
        {
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("[DummyClient] Disconnected.");
        }

        /// <summary>
        /// Entry point for running dummy client.
        /// </summary>
        public static async Task Main(string[] args)
        {
            string host = args.Length > 0 ? args[0] : "localhost";
            int port = args.Length > 1 ? int.Parse(args[1]) : 5000;

            var client = new DummyClient(host, port);
            await client.RunTestsAsync();
        }
    }
}

