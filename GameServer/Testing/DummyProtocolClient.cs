using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.Testing
{
    /// <summary>
    /// Minimal EnhancedMinecraftProtocol round-trip client for packet generation/validation tests.
    /// </summary>
    public sealed record ProtocolRoundTripResult(
        MinecraftMessageType MessageType,
        int PayloadSize,
        string Descriptor,
        string Signature,
        byte[] Payload);

    public static class DummyProtocolClient
    {
        /// <summary>
        /// Builds a framed TimeUpdate packet and verifies deserialize/serialize paths locally.
        /// </summary>
        public static ProtocolRoundTripResult BuildTimeUpdateRoundTrip()
        {
            ProtocolRegistry.ValidateBindings();
            ProtoRuntime.EnsureInitialized();
            ProtoFingerprint.AssertDescriptorFingerprint();

            var messageType = MinecraftMessageType.TimeUpdate;
            var message = new TimeUpdateBroadcast
            {
                WorldTime = 12000,
                DayTime = 6000
            };

            ProtocolRegistry.EnsureRegistered(messageType);
            var payload = message.ToByteArray();
            var parsed = TimeUpdateBroadcast.Parser.ParseFrom(payload);

            if (parsed.WorldTime != message.WorldTime || parsed.DayTime != message.DayTime)
            {
                throw new InvalidOperationException("[DummyProtocolClient] Parsed payload does not match source message.");
            }

            return new ProtocolRoundTripResult(
                messageType,
                payload.Length,
                TimeUpdateBroadcast.Descriptor.FullName,
                SharedFeatureCatalog.HydrologySignature,
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Builds a framed ChunkLoadRequest packet covering multiple positions for registry validation.
        /// </summary>
        public static ProtocolRoundTripResult BuildChunkLoadRequestRoundTrip()
        {
            var messageType = MinecraftMessageType.ChunkDataRequest;
            var request = new ChunkLoadRequest
            {
                ViewDistance = 6
            };

            ProtocolRegistry.EnsureRegistered(messageType);
            var payload = request.ToByteArray();
            var parsed = ChunkLoadRequest.Parser.ParseFrom(payload);

            request.ChunkPositions.Add(new EnhancedMinecraftProtocol.ChunkPosition { ChunkX = 0, ChunkZ = 0 });
            request.ChunkPositions.Add(new EnhancedMinecraftProtocol.ChunkPosition { ChunkX = 1, ChunkZ = 0 });
            request.ChunkPositions.Add(new EnhancedMinecraftProtocol.ChunkPosition { ChunkX = -2, ChunkZ = 3 });

            if (parsed.ViewDistance != request.ViewDistance || parsed.ChunkPositions.Count != request.ChunkPositions.Count)
            {
                throw new InvalidOperationException("[DummyProtocolClient] Parsed chunk-load request does not match source message.");
            }

            return new ProtocolRoundTripResult(
                messageType,
                payload.Length,
                ChunkLoadRequest.Descriptor.FullName,
                SharedFeatureCatalog.HydrologySignature,
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Sends a framed payload to a running server. No response parsing is performed.
        /// </summary>
        public static async Task<ProtocolRoundTripResult> SendAsync(string host = "127.0.0.1", int port = 9000, CancellationToken token = default)
        {
            return await SendRoundTripAsync(BuildTimeUpdateRoundTrip, host, port, token);
        }

        /// <summary>
        /// Sends a chunk-load request frame to a running server.
        /// </summary>
        public static async Task<ProtocolRoundTripResult> SendChunkRequestAsync(string host = "127.0.0.1", int port = 9000, CancellationToken token = default)
        {
            return SendRoundTripAsync(BuildChunkLoadRequestRoundTrip, host, port, token);
        }

        /// <summary>
        /// Builds a 6-byte frame header: 4-byte big-endian length + 2-byte big-endian message type.
        /// </summary>
        private static byte[] BuildFrame(MinecraftMessageType messageType, byte[] payload)
        {
            Span<byte> header = stackalloc byte[6];
            BinaryPrimitives.WriteInt32BigEndian(header.Slice(0, 4), payload.Length);
            BinaryPrimitives.WriteInt16BigEndian(header.Slice(4, 2), (short)messageType);

            var frame = new byte[header.Length + payload.Length];
            header.CopyTo(frame.AsSpan(0, header.Length));
            payload.CopyTo(frame.AsSpan(header.Length));
            return frame;
        }

        /// <summary>
        /// Sends a round-trip frame to a running server and returns the result.
        /// </summary>
        private static async Task<ProtocolRoundTripResult> SendRoundTripAsync(Func<ProtocolRoundTripResult> builder, string host, int port, CancellationToken token)
        {
            var roundTrip = builder();

            using var client = new TcpClient();
            await client.ConnectAsync(host, port);

            using var stream = client.GetStream();
            await stream.WriteAsync(roundTrip.Payload.AsMemory(0, roundTrip.Payload.Length), token);
            await stream.FlushAsync(token);

            return roundTrip;
        }
    }
}
            byte[] lengthBuffer = new byte[4];

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    // Read packet length (4 bytes)
                    int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, _cts.Token);
                    if (bytesRead < 4)
                    {
                        Console.WriteLine("[DummyClient] Disconnected (incomplete length)");
                        break;
                    }

                    int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (packetLength <= 0 || packetLength > 1024 * 1024) // Max 1MB
                    {
                        Console.WriteLine($"[DummyClient] Invalid packet length: {packetLength}");
                        break;
                    }

                    // Read packet data
                    byte[] packetBuffer = new byte[packetLength];
                    bytesRead = 0;
                    while (bytesRead < packetLength)
                    {
                        int read = await _stream.ReadAsync(packetBuffer, bytesRead, packetLength - bytesRead, _cts.Token);
                        if (read == 0)
                        {
                            Console.WriteLine("[DummyClient] Disconnected (incomplete data)");
                            return;
                        }
                        bytesRead += read;
                    }

                    // Process packet
                    _ = Task.Run(() => ProcessPacket(packetBuffer), _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[DummyClient] Receive loop cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Receive error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a received packet.
        /// </summary>
        private void ProcessPacket(byte[] packetData)
        {
            try
            {
                using var stream = new System.IO.MemoryStream(packetData);
                var message = Google.Protobuf.MessageParser<EnhancedMinecraftProtocol.GameMessage>.Default.ParseFrom(stream);

                Console.WriteLine($"[DummyClient] Received packet type: {message.MessageCase}");

                switch (message.MessageCase)
                {
                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.PlayerInfo:
                        HandlePlayerInfo(message.PlayerInfo);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.ChunkLoadResponse:
                        HandleChunkLoadResponse(message.ChunkLoadResponse);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.PlayerActionResponse:
                        HandlePlayerActionResponse(message.PlayerActionResponse);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.BlockChangeBroadcast:
                        HandleBlockChange(message.BlockChangeBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.EntitySpawnBroadcast:
                        HandleEntitySpawn(message.EntitySpawnBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.EntityDespawnBroadcast:
                        HandleEntityDespawn(message.EntityDespawnBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.TimeUpdateBroadcast:
                        HandleTimeUpdate(message.TimeUpdateBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.WeatherUpdateBroadcast:
                        HandleWeatherUpdate(message.WeatherUpdateBroadcast);
                        break;

                    default:
                        Console.WriteLine($"[DummyClient] Unknown message type: {message.MessageCase}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Failed to process packet: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles player info packet.
        /// </summary>
        private void HandlePlayerInfo(EnhancedMinecraftProtocol.PlayerInfo? playerInfo)
        {
            if (playerInfo == null)
            {
                Console.WriteLine("[DummyClient] Received null PlayerInfo");
                return;
            }

            Console.WriteLine($"[DummyClient] PlayerInfo - ID: {playerInfo.PlayerId}, Name: {playerInfo.Username}, " +
                $"Pos: ({playerInfo.Position.X}, {playerInfo.Position.Y}, {playerInfo.Position.Z}), " +
                $"Health: {playerInfo.Health}/{playerInfo.MaxHealth}");
        }

        /// <summary>
        /// Handles chunk load response packet.
        /// </summary>
        private void HandleChunkLoadResponse(EnhancedMinecraftProtocol.ChunkLoadResponse? response)
        {
            if (response == null)
            {
                Console.WriteLine("[DummyClient] Received null ChunkLoadResponse");
                return;
            }

            Console.WriteLine($"[DummyClient] ChunkLoadResponse - Chunks: {response.Chunks.Count}, " +
                $"Total: {response.TotalRequested}, Sent: {response.TotalSent}");
        }

        /// <summary>
        /// Handles player action response packet.
        /// </summary>
        private void HandlePlayerActionResponse(EnhancedMinecraftProtocol.PlayerActionResponse? response)
        {
            if (response == null)
            {
                Console.WriteLine("[DummyClient] Received null PlayerActionResponse");
                return;
            }

            Console.WriteLine($"[DummyClient] PlayerActionResponse - Success: {response.Success}, " +
                $"Message: {response.Message}");
        }

        /// <summary>
        /// Handles block change broadcast packet.
        /// </summary>
        private void HandleBlockChange(EnhancedMinecraftProtocol.BlockChangeBroadcast? blockChange)
        {
            if (blockChange == null)
            {
                Console.WriteLine("[DummyClient] Received null BlockChangeBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] BlockChange - Pos: ({blockChange.Position.X}, {blockChange.Position.Y}, {blockChange.Position.Z}), " +
                $"Block: {blockChange.BlockId}");
        }

        /// <summary>
        /// Handles entity spawn broadcast packet.
        /// </summary>
        private void HandleEntitySpawn(EnhancedMinecraftProtocol.EntitySpawnBroadcast? entitySpawn)
        {
            if (entitySpawn == null)
            {
                Console.WriteLine("[DummyClient] Received null EntitySpawnBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] EntitySpawn - ID: {entitySpawn.Entity.EntityId}, " +
                $"Type: {entitySpawn.Entity.EntityType}");
        }

        /// <summary>
        /// Handles entity despawn broadcast packet.
        /// </summary>
        private void HandleEntityDespawn(EnhancedMinecraftProtocol.EntityDespawnBroadcast? entityDespawn)
        {
            if (entityDespawn == null)
            {
                Console.WriteLine("[DummyClient] Received null EntityDespawnBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] EntityDespawn - ID: {entityDespawn.EntityId}, " +
                $"Reason: {entityDespawn.Reason}");
        }

        /// <summary>
        /// Handles time update broadcast packet.
        /// </summary>
        private void HandleTimeUpdate(EnhancedMinecraftProtocol.TimeUpdateBroadcast? timeUpdate)
        {
            if (timeUpdate == null)
            {
                Console.WriteLine("[DummyClient] Received null TimeUpdateBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] TimeUpdate - World: {timeUpdate.WorldTime}, Day: {timeUpdate.DayTime}");
        }

        /// <summary>
        /// Handles weather update broadcast packet.
        /// </summary>
        private void HandleWeatherUpdate(EnhancedMinecraftProtocol.WeatherUpdateBroadcast? weatherUpdate)
        {
            if (weatherUpdate == null)
            {
                Console.WriteLine("[DummyClient] Received null WeatherUpdateBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] WeatherUpdate - Type: {weatherUpdate.Weather.WeatherType}, " +
                $"Duration: {weatherUpdate.Weather.DurationTicks}");
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            _cts.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("[DummyClient] Disconnected");
        }

        /// <summary>
        /// Runs a round-trip test: sends packets and waits for responses.
        /// </summary>
        public async Task RunRoundTripTestAsync(string username, string password)
        {
            Console.WriteLine("[DummyClient] Starting round-trip test...");

            await ConnectAsync();

            // Test 1: Login
            Console.WriteLine("[DummyClient] Test 1: Login");
            await SendLoginAsync(username, password);
            await Task.Delay(1000);

            // Test 2: Chunk Load Request
            Console.WriteLine("[DummyClient] Test 2: Chunk Load Request");
            await SendChunkLoadRequestAsync(0, 0, 5);
            await Task.Delay(1000);

            // Test 3: Player Action
            Console.WriteLine("[DummyClient] Test 3: Player Action");
            var position = new EnhancedMinecraftProtocol.Vector3 { X = 100, Y = 64, Z = 100 };
            await SendPlayerActionAsync(EnhancedMinecraftProtocol.PlayerAction.BreakBlock, position, 0, null);
            await Task.Delay(1000);

            Console.WriteLine("[DummyClient] Round-trip test complete. Waiting for responses...");
            
            // Wait for responses
            await Task.Delay(5000);

            Disconnect();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }

    /// <summary>
    /// Entry point for dummy protocol client.
    /// </summary>
    public static class DummyProtocolClientMain
    {
        public static async Task Main(string[] args)
        {
            string host = "localhost";
            int port = 7777;
            string username = "testuser";
            string password = "testpass";

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--host":
                        if (i + 1 < args.Length)
                        {
                            host = args[i + 1];
                        }
                        break;
                    case "--port":
                        if (i + 1 < args.Length)
                        {
                            int.TryParse(args[i + 1], out port);
                        }
                        break;
                    case "--username":
                        if (i + 1 < args.Length)
                        {
                            username = args[i + 1];
                        }
                        break;
                    case "--password":
                        if (i + 1 < args.Length)
                        {
                            password = args[i + 1];
                        }
                        break;
                    case "--help":
                        PrintUsage();
                        return;
                }
            }

            Console.WriteLine($"[DummyClient] Starting protocol test client...");
            Console.WriteLine($"[DummyClient] Host: {host}, Port: {port}");
            Console.WriteLine($"[DummyClient] Username: {username}");

            var client = new DummyProtocolClient(host, port);
            
            try
            {
                await client.RunRoundTripTestAsync(username, password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Error: {ex.Message}");
                Console.WriteLine($"[DummyClient] Stack: {ex.StackTrace}");
            }
            finally
            {
                client.Dispose();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: DummyProtocolClient [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --host <host>      Server host (default: localhost)");
            Console.WriteLine("  --port <port>      Server port (default: 7777)");
            Console.WriteLine("  --username <user>   Username (default: testuser)");
            Console.WriteLine("  --password <pass>   Password (default: testpass)");
            Console.WriteLine("  --help              Show this help message");
        }
    }
}
            }

            byte[] lengthBuffer = new byte[4];

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    // Read packet length (4 bytes)
                    int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, _cts.Token);
                    if (bytesRead < 4)
                    {
                        Console.WriteLine("[DummyClient] Disconnected (incomplete length)");
                        break;
                    }

                    int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (packetLength <= 0 || packetLength > 1024 * 1024) // Max 1MB
                    {
                        Console.WriteLine($"[DummyClient] Invalid packet length: {packetLength}");
                        break;
                    }

                    // Read packet data
                    byte[] packetBuffer = new byte[packetLength];
                    bytesRead = 0;
                    while (bytesRead < packetLength)
                    {
                        int read = await _stream.ReadAsync(packetBuffer, bytesRead, packetLength - bytesRead, _cts.Token);
                        if (read == 0)
                        {
                            Console.WriteLine("[DummyClient] Disconnected (incomplete data)");
                            return;
                        }
                        bytesRead += read;
                    }

                    // Process packet
                    _ = Task.Run(() => ProcessPacket(packetBuffer), _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[DummyClient] Receive loop cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Receive error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a received packet.
        /// </summary>
        private void ProcessPacket(byte[] packetData)
        {
            try
            {
                using var stream = new System.IO.MemoryStream(packetData);
                var message = Google.Protobuf.MessageParser<EnhancedMinecraftProtocol.GameMessage>.Default.ParseFrom(stream);

                Console.WriteLine($"[DummyClient] Received packet type: {message.MessageCase}");

                switch (message.MessageCase)
                {
                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.PlayerInfo:
                        HandlePlayerInfo(message.PlayerInfo);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.ChunkLoadResponse:
                        HandleChunkLoadResponse(message.ChunkLoadResponse);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.PlayerActionResponse:
                        HandlePlayerActionResponse(message.PlayerActionResponse);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.BlockChangeBroadcast:
                        HandleBlockChange(message.BlockChangeBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.EntitySpawnBroadcast:
                        HandleEntitySpawn(message.EntitySpawnBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.EntityDespawnBroadcast:
                        HandleEntityDespawn(message.EntityDespawnBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.TimeUpdateBroadcast:
                        HandleTimeUpdate(message.TimeUpdateBroadcast);
                        break;

                    case EnhancedMinecraftProtocol.GameMessage.MessageOneofCase.WeatherUpdateBroadcast:
                        HandleWeatherUpdate(message.WeatherUpdateBroadcast);
                        break;

                    default:
                        Console.WriteLine($"[DummyClient] Unknown message type: {message.MessageCase}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Failed to process packet: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles player info packet.
        /// </summary>
        private void HandlePlayerInfo(EnhancedMinecraftProtocol.PlayerInfo? playerInfo)
        {
            if (playerInfo == null)
            {
                Console.WriteLine("[DummyClient] Received null PlayerInfo");
                return;
            }

            Console.WriteLine($"[DummyClient] PlayerInfo - ID: {playerInfo.PlayerId}, Name: {playerInfo.Username}, " +
                $"Pos: ({playerInfo.Position.X}, {playerInfo.Position.Y}, {playerInfo.Position.Z}), " +
                $"Health: {playerInfo.Health}/{playerInfo.MaxHealth}");
        }

        /// <summary>
        /// Handles chunk load response packet.
        /// </summary>
        private void HandleChunkLoadResponse(EnhancedMinecraftProtocol.ChunkLoadResponse? response)
        {
            if (response == null)
            {
                Console.WriteLine("[DummyClient] Received null ChunkLoadResponse");
                return;
            }

            Console.WriteLine($"[DummyClient] ChunkLoadResponse - Chunks: {response.Chunks.Count}, " +
                $"Total: {response.TotalRequested}, Sent: {response.TotalSent}");
        }

        /// <summary>
        /// Handles player action response packet.
        /// </summary>
        private void HandlePlayerActionResponse(EnhancedMinecraftProtocol.PlayerActionResponse? response)
        {
            if (response == null)
            {
                Console.WriteLine("[DummyClient] Received null PlayerActionResponse");
                return;
            }

            Console.WriteLine($"[DummyClient] PlayerActionResponse - Success: {response.Success}, " +
                $"Message: {response.Message}");
        }

        /// <summary>
        /// Handles block change broadcast packet.
        /// </summary>
        private void HandleBlockChange(EnhancedMinecraftProtocol.BlockChangeBroadcast? blockChange)
        {
            if (blockChange == null)
            {
                Console.WriteLine("[DummyClient] Received null BlockChangeBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] BlockChange - Pos: ({blockChange.Position.X}, {blockChange.Position.Y}, {blockChange.Position.Z}), " +
                $"Block: {blockChange.BlockId}");
        }

        /// <summary>
        /// Handles entity spawn broadcast packet.
        /// </summary>
        private void HandleEntitySpawn(EnhancedMinecraftProtocol.EntitySpawnBroadcast? entitySpawn)
        {
            if (entitySpawn == null)
            {
                Console.WriteLine("[DummyClient] Received null EntitySpawnBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] EntitySpawn - ID: {entitySpawn.Entity.EntityId}, " +
                $"Type: {entitySpawn.Entity.EntityType}");
        }

        /// <summary>
        /// Handles entity despawn broadcast packet.
        /// </summary>
        private void HandleEntityDespawn(EnhancedMinecraftProtocol.EntityDespawnBroadcast? entityDespawn)
        {
            if (entityDespawn == null)
            {
                Console.WriteLine("[DummyClient] Received null EntityDespawnBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] EntityDespawn - ID: {entityDespawn.EntityId}, " +
                $"Reason: {entityDespawn.Reason}");
        }

        /// <summary>
        /// Handles time update broadcast packet.
        /// </summary>
        private void HandleTimeUpdate(EnhancedMinecraftProtocol.TimeUpdateBroadcast? timeUpdate)
        {
            if (timeUpdate == null)
            {
                Console.WriteLine("[DummyClient] Received null TimeUpdateBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] TimeUpdate - World: {timeUpdate.WorldTime}, Day: {timeUpdate.DayTime}");
        }

        /// <summary>
        /// Handles weather update broadcast packet.
        /// </summary>
        private void HandleWeatherUpdate(EnhancedMinecraftProtocol.WeatherUpdateBroadcast? weatherUpdate)
        {
            if (weatherUpdate == null)
            {
                Console.WriteLine("[DummyClient] Received null WeatherUpdateBroadcast");
                return;
            }

            Console.WriteLine($"[DummyClient] WeatherUpdate - Type: {weatherUpdate.Weather.WeatherType}, " +
                $"Duration: {weatherUpdate.Weather.DurationTicks}");
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            _cts.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
            Console.WriteLine("[DummyClient] Disconnected");
        }

        /// <summary>
        /// Runs a round-trip test: sends packets and waits for responses.
        /// </summary>
        public async Task RunRoundTripTestAsync(string username, string password)
        {
            Console.WriteLine("[DummyClient] Starting round-trip test...");

            await ConnectAsync();

            // Test 1: Login
            Console.WriteLine("[DummyClient] Test 1: Login");
            await SendLoginAsync(username, password);
            await Task.Delay(1000);

            // Test 2: Chunk Load Request
            Console.WriteLine("[DummyClient] Test 2: Chunk Load Request");
            await SendChunkLoadRequestAsync(0, 0, 5);
            await Task.Delay(1000);

            // Test 3: Player Action
            Console.WriteLine("[DummyClient] Test 3: Player Action");
            var position = new EnhancedMinecraftProtocol.Vector3 { X = 100, Y = 64, Z = 100 };
            await SendPlayerActionAsync(EnhancedMinecraftProtocol.PlayerAction.BreakBlock, position, 0, null);
            await Task.Delay(1000);

            Console.WriteLine("[DummyClient] Round-trip test complete. Waiting for responses...");
            
            // Wait for responses
            await Task.Delay(5000);

            Disconnect();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }

    /// <summary>
    /// Entry point for dummy protocol client.
    /// </summary>
    public static class DummyProtocolClientMain
    {
        public static async Task Main(string[] args)
        {
            string host = "localhost";
            int port = 7777;
            string username = "testuser";
            string password = "testpass";

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--host":
                        if (i + 1 < args.Length)
                        {
                            host = args[i + 1];
                        }
                        break;
                    case "--port":
                        if (i + 1 < args.Length)
                        {
                            int.TryParse(args[i + 1], out port);
                        }
                        break;
                    case "--username":
                        if (i + 1 < args.Length)
                        {
                            username = args[i + 1];
                        }
                        break;
                    case "--password":
                        if (i + 1 < args.Length)
                        {
                            password = args[i + 1];
                        }
                        break;
                    case "--help":
                        PrintUsage();
                        return;
                }
            }

            Console.WriteLine($"[DummyClient] Starting protocol test client...");
            Console.WriteLine($"[DummyClient] Host: {host}, Port: {port}");
            Console.WriteLine($"[DummyClient] Username: {username}");

            var client = new DummyProtocolClient(host, port);
            
            try
            {
                await client.RunRoundTripTestAsync(username, password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DummyClient] Error: {ex.Message}");
                Console.WriteLine($"[DummyClient] Stack: {ex.StackTrace}");
            }
            finally
            {
                client.Dispose();
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: DummyProtocolClient [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --host <host>      Server host (default: localhost)");
            Console.WriteLine("  --port <port>      Server port (default: 7777)");
            Console.WriteLine("  --username <user>   Username (default: testuser)");
            Console.WriteLine("  --password <pass>   Password (default: testpass)");
            Console.WriteLine("  --help              Show this help message");
        }
    }
}

