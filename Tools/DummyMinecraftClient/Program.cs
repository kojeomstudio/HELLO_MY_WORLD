using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using EnhancedMinecraftProtocol;
using MinecraftGame.Common;

namespace DummyMinecraftClient
{
    /// <summary>
    /// Dummy Minecraft client for protocol testing
    /// Supports both legacy and enhanced protobuf protocols
    /// </summary>
    public class Program
    {
        private const string DEFAULT_HOST = "127.0.0.1";
        private const int DEFAULT_PORT = 7777;
        private const int BUFFER_SIZE = 65536;
        private const int TIMEOUT_MS = 30000;
        private const int KEEPALIVE_INTERVAL_MS = 15000;

        private static string? _sessionId;
        private static string? _username;
        private static bool _useEnhancedProtocol = true;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Dummy Minecraft Client - Protocol Testing Tool ===");
            Console.WriteLine($"Version: 1.0.0");
            Console.WriteLine($"Protocol: Enhanced (supports legacy fallback)");
            Console.WriteLine();

            string host = DEFAULT_HOST;
            int port = DEFAULT_PORT;
            string username = "TestUser";

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--host":
                    case "-h":
                        if (i + 1 < args.Length)
                            host = args[++i];
                        break;
                        goto default;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        break;
                }
            }

            try
            {
                await RunClientAsync(host, port, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        private static async Task RunClientAsync(string host, int port, string username)
        {
            Console.WriteLine($"Connecting to {host}:{port} as '{username}'...");

            using var tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(host, port);
                var stream = tcpClient.GetStream();

                Console.WriteLine("Connected! Starting protocol handshake...");

                // Send login request
                await SendLoginRequestAsync(stream, username);

                // Start keepalive
                var keepaliveTask = Task.Run(async () =>
                {
                    while (tcpClient.Connected)
                    {
                        await Task.Delay(KEEPALIVE_INTERVAL_MS);
                        await SendPingAsync(stream);
                    }
                });

                // Start message receiving loop
                await MessageLoopAsync(stream, tcpClient);

                keepaliveTask.Wait();
            }
        }

        private static async Task SendLoginRequestAsync(NetworkStream stream, string username)
        {
            Console.WriteLine("Sending LoginRequest...");

            // Try enhanced protocol first
            if (_useEnhancedProtocol)
            {
                try
                {
                    var loginRequest = new EnhancedMinecraftProtocol.PlayerInfo
                    {
                        Username = username,
                        Position = new Vector3 { X = 0, Y = 64, Z = 0 },
                        Rotation = new Vector3 { X = 0, Y = 0, Z = 0 },
                        Level = 1,
                        Experience = 0,
                        ExperienceProgress = 0f,
                        Health = 20f,
                        MaxHealth = 20f,
                        Hunger = 20f,
                        MaxHunger = 20f,
                        Saturation = 5f,
                        GameMode = GameMode.Survival,
                        SelectedSlot = 0
                    };

                    var messageBytes = loginRequest.ToByteArray();
                    await SendMessageAsync(stream, (int)MinecraftMessageType.PlayerStateUpdate, messageBytes, true);
                    Console.WriteLine($"Enhanced LoginRequest sent ({messageBytes.Length} bytes)");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Enhanced protocol failed, falling back to legacy: {ex.Message}");
                }
            }

            // Fallback to legacy protocol
            var legacyLogin = new SharedProtocol.LoginRequest
            {
                Username = username,
                Password = "test",
                ClientVersion = "1.0.0"
            };

            var legacyBytes = SerializeLegacyMessage(legacyLogin);
            await SendMessageAsync(stream, (int)SharedProtocol.MessageType.LoginRequest, legacyBytes, false);
            Console.WriteLine($"Legacy LoginRequest sent ({legacyBytes.Length} bytes)");
        }

        private static async Task SendPingAsync(NetworkStream stream)
        {
            var ping = new EnhancedMinecraftProtocol.SoundEffect
            {
                SoundType = SoundType.FootstepStone,
                Position = new Vector3 { X = 0, Y = 64, Z = 0 },
                Volume = 0.5f,
                Pitch = 1.0f,
                Category = SoundCategory.SndPlayer
            };

            var bytes = ping.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.SoundEffect, bytes, true);
            Console.WriteLine("Ping sent");
        }

        private static async Task SendChunkLoadRequestAsync(NetworkStream stream)
        {
            Console.WriteLine("Sending ChunkLoadRequest...");

            var request = new EnhancedMinecraftProtocol.ChunkLoadRequest();
            request.ChunkPositions.Add(new Vector3Int { X = 0, Y = 0, Z = 0 });
            request.ChunkPositions.Add(new Vector3Int { X = 1, Y = 0, Z = 0 });
            request.ChunkPositions.Add(new Vector3Int { X = 0, Y = 0, Z = 1 });
            request.ChunkPositions.Add(new Vector3Int { X = 1, Y = 0, Z = 1 });
            request.ViewDistance = 4;

            var bytes = request.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.ChunkDataRequest, bytes, true);
            Console.WriteLine($"ChunkLoadRequest sent ({request.ChunkPositions.Count} chunks, {bytes.Length} bytes)");
        }

        private static async Task SendBlockPlaceRequestAsync(NetworkStream stream)
        {
            Console.WriteLine("Sending BlockPlaceRequest...");

            var request = new EnhancedMinecraftProtocol.PlayerActionRequest
            {
                Action = EnhancedMinecraftProtocol.PlayerAction.PlaceBlock,
                TargetPosition = new Vector3Int { X = 10, Y = 65, Z = 10 },
                Face = 1, // Top face
                CursorPosition = new Vector3 { X = 0.5f, Y = 0.5f, Z = 0.5f },
                UsedItem = new EnhancedMinecraftProtocol.ItemStack
                {
                    ItemId = 1,
                    ItemName = "Stone",
                    Count = 64,
                    Durability = 100,
                    MaxDurability = 100,
                    ItemType = EnhancedMinecraftProtocol.ItemType.Block,
                    Rarity = EnhancedMinecraftProtocol.ItemRarity.Common
                },
                Sequence = 1
            };

            var bytes = request.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.PlayerActionRequest, bytes, true);
            Console.WriteLine($"BlockPlaceRequest sent ({bytes.Length} bytes)");
        }

        private static async Task SendBlockBreakRequestAsync(NetworkStream stream)
        {
            Console.WriteLine("Sending BlockBreakStartRequest...");

            var request = new EnhancedMinecraftProtocol.PlayerActionRequest
            {
                Action = EnhancedMinecraftProtocol.PlayerAction.StartDestroyBlock,
                TargetPosition = new Vector3Int { X = 10, Y = 65, Z = 10 },
                Face = 0,
                CursorPosition = new Vector3 { X = 0.5f, Y = 0.5f, Z = 0.5f },
                UsedItem = new EnhancedMinecraftProtocol.ItemStack
                {
                    ItemId = 2,
                    ItemName = "Pickaxe",
                    Count = 1,
                    Durability = 100,
                    MaxDurability = 100,
                    ItemType = EnhancedMinecraftProtocol.ItemType.Tool,
                    Rarity = EnhancedMinecraftProtocol.ItemRarity.Common
                },
                Sequence = 1
            };

            var bytes = request.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.PlayerActionRequest, bytes, true);
            Console.WriteLine($"BlockBreakStartRequest sent ({bytes.Length} bytes)");
        }

        private static async Task SendChatMessageAsync(NetworkStream stream, string message)
        {
            Console.WriteLine($"Sending chat: {message}");

            var chat = new EnhancedMinecraftProtocol.ChatMessage
            {
                SenderId = _username ?? "Unknown",
                SenderName = _username ?? "TestUser",
                MessageContent = message,
                ChatType = EnhancedMinecraftProtocol.ChatType.ChatGlobal,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                FormattedMessage = $"<{_username ?? "TestUser"}> {message}",
                Style = new EnhancedMinecraftProtocol.ChatStyle
                {
                    Color = "#FFFFFF",
                    Bold = false,
                    Italic = false,
                    Underlined = false,
                    Strikethrough = false,
                    Obfuscated = false
                }
            };

            var bytes = chat.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.ChatMessage, bytes, true);
            }

        private static async Task MessageLoopAsync(NetworkStream stream, TcpClient tcpClient)
        {
            var buffer = new byte[BUFFER_SIZE];
            int messageCount = 0;

            while (tcpClient.Connected)
            {
                try
                {
                    // Read message type (4 bytes)
                    var typeBytes = new byte[4];
                    var read = await stream.ReadAsync(typeBytes, 0, 4);
                    if (read != 4)
                    {
                        Console.WriteLine($"Connection closed (expected 4 bytes, got {read})");
                        break;
                    }

                    var messageType = BitConverter.ToInt32(typeBytes, 0);

                    // Read message length (4 bytes)
                    var lengthBytes = new byte[4];
                    read = await stream.ReadAsync(lengthBytes, 0, 4);
                    if (read != 4)
                    {
                        Console.WriteLine($"Connection closed (expected 4 bytes, got {read})");
                        break;
                    }

                    var messageLength = BitConverter.ToInt32(lengthBytes, 0);

                    // Read message body
                    var messageBytes = new byte[messageLength];
                    read = await stream.ReadAsync(messageBytes, 0, messageLength);
                    if (read != messageLength)
                    {
                        Console.WriteLine($"Connection closed (expected {messageLength} bytes, got {read})");
                        break;
                    }

                    messageCount++;
                    await ProcessMessageAsync(messageType, messageBytes, messageCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"Connection closed. Total messages received: {messageCount}");
        }

        private static async Task ProcessMessageAsync(int messageType, byte[] messageBytes, int messageNumber)
        {
            try
            {
                Console.WriteLine($"[{messageNumber}] Received message type: {messageType} ({messageBytes.Length} bytes)");

                switch (messageType)
                {
                    case (int)MinecraftMessageType.PlayerActionResponse:
                        await HandlePlayerActionResponseAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.ChunkDataResponse:
                        await HandleChunkDataResponseAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.BlockChangeNotification:
                        await HandleBlockChangeNotificationAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.ChatMessage:
                        await HandleChatMessageAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.TimeUpdateBroadcast:
                        await HandleTimeUpdateAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.WeatherUpdateBroadcast:
                        await HandleWeatherUpdateAsync(messageBytes);
                        break;

                    default:
                        Console.WriteLine($"  Unknown message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static async Task HandlePlayerActionResponseAsync(byte[] messageBytes)
        {
            try
            {
                var response = EnhancedMinecraftProtocol.PlayerActionResponse.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Action: {response.Action}");
                Console.WriteLine($"  Success: {response.Success}");
                Console.WriteLine($"  Message: {response.Message}");
                Console.WriteLine($"  Sequence: {response.Sequence}");

                if (response.Result != null)
                {
                    Console.WriteLine($"  Updated Items: {response.Result.UpdatedItems.Count}");
                    Console.WriteLine($"  Health Change: {response.Result.HealthChange}");
                    Console.WriteLine($"  Hunger Change: {response.Result.HungerChange}");
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse as enhanced: {ex.Message}");
                Console.WriteLine("Attempting legacy protocol parse...");
                // Could try legacy fallback here
            }
        }

        private static async Task HandleChunkDataResponseAsync(byte[] messageBytes)
        {
            try
            {
                var response = EnhancedMinecraftProtocol.ChunkLoadResponse.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Total Requested: {response.TotalRequested}");
                Console.WriteLine($"  Total Sent: {response.TotalSent}");
                Console.WriteLine($"  Chunks: {response.Chunks.Count}");

                foreach (var chunk in response.Chunks)
                {
                    Console.WriteLine($"    Chunk [{chunk.ChunkX}, {chunk.ChunkZ}]:");
                    Console.WriteLine($"      Block Data: {chunk.BlockData.Length} bytes");
                    Console.WriteLine($"      Biome Data: {chunk.BiomeData.Length} bytes");
                    Console.WriteLine($"      Entities: {chunk.Entities.Count}");
                    Console.WriteLine($"      Tile Entities: {chunk.TileEntities.Count}");
                    Console.WriteLine($"      Timestamp: {chunk.GenerationTimestamp}");
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse chunk data: {ex.Message}");
            }
        }

        private static async Task HandleBlockChangeNotificationAsync(byte[] messageBytes)
        {
            try
            {
                var notification = EnhancedMinecraftProtocol.BlockChangeBroadcast.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Position: [{notification.Position.X}, {notification.Position.Y}, {notification.Position.Z}]");
                Console.WriteLine($"  Old Block: {notification.OldBlockId}");
                Console.WriteLine($"  New Block: {notification.NewBlockId}");
                Console.WriteLine($"  Player: {notification.PlayerId}");
                Console.WriteLine($"  Reason: {notification.Reason}");
                Console.WriteLine($"  Timestamp: {notification.Timestamp}");
                Console.WriteLine($"  Drops: {notification.Drops.Count}");

                if (notification.ParticleEffect != null)
                {
                    Console.WriteLine($"  Particle: {notification.ParticleEffect.ParticleType}");
                }

                if (notification.SoundEffect != null)
                {
                    Console.WriteLine($"  Sound: {notification.SoundEffect.SoundType}");
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse block change: {ex.Message}");
            }
        }

        private static async Task HandleChatMessageAsync(byte[] messageBytes)
        {
            try
            {
                var chat = EnhancedMinecraftProtocol.ChatMessage.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  From: {chat.SenderName}");
                Console.WriteLine($"  Message: {chat.MessageContent}");
                Console.WriteLine($"  Type: {chat.ChatType}");
                Console.WriteLine($"  Timestamp: {chat.Timestamp}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse chat: {ex.Message}");
            }
        }

        private static async Task HandleTimeUpdateAsync(byte[] messageBytes)
        {
            try
            {
                var timeUpdate = EnhancedMinecraftProtocol.TimeUpdateBroadcast.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  World Time: {timeUpdate.WorldTime}");
                Console.WriteLine($"  Day Time: {timeUpdate.DayTime}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse time update: {ex.Message}");
            }
        }

        private static async Task HandleWeatherUpdateAsync(byte[] messageBytes)
        {
            try
            {
                var weatherUpdate = EnhancedMinecraftProtocol.WeatherUpdateBroadcast.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Weather: {weatherUpdate.Weather.WeatherType}");
                Console.WriteLine($"  Duration: {weatherUpdate.Weather.DurationTicks}");
                Console.WriteLine($"  Intensity: {weatherUpdate.Weather.Intensity}");
                Console.WriteLine($"  Thundering: {weatherUpdate.Weather.Thundering}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse weather update: {ex.Message}");
            }
        }

        private static async Task SendMessageAsync(NetworkStream stream, int messageType, byte[] messageBytes, bool isEnhanced)
        {
            // Build message: [messageType (4 bytes)][messageLength (4 bytes)][messageBody (variable)]
            var header = BitConverter.GetBytes(messageType);
            var length = BitConverter.GetBytes(messageBytes.Length);

            var fullMessage = new byte[8 + messageBytes.Length];
            Buffer.BlockCopy(header, 0, fullMessage, 0);
            Buffer.BlockCopy(length, 0, fullMessage, 4);
            Buffer.BlockCopy(messageBytes, 0, fullMessage, 8);

            await stream.WriteAsync(fullMessage, 0, fullMessage.Length);

            var protocolName = isEnhanced ? "Enhanced" : "Legacy";
            Console.WriteLine($"Sent {protocolName} message type {messageType} ({fullMessage.Length} bytes)");
        }

        private static byte[] SerializeLegacyMessage(object message)
        {
            // Simple serialization for legacy protocol
            using var stream = new System.IO.MemoryStream();
            var writer = new ProtoBuf.Serializer(stream);
            ProtoBuf.Serializer.Serialize(stream, message);
            return stream.ToArray();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: DummyMinecraftClient [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --host <address>    Server host (default: 127.0.0.1)");
            Console.WriteLine("  -h <address>         Server host (default: 127.0.0.1)");
            Console.WriteLine();
            Console.WriteLine("Commands (after connection):");
            Console.WriteLine("  /chunk              Request chunks");
            Console.WriteLine("  /place              Place a block");
            Console.WriteLine("  /break              Start breaking a block");
            Console.WriteLine("  /chat <message>     Send chat message");
            Console.WriteLine("  /help               Show this help");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to disconnect");
        }
    }
}
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using EnhancedMinecraftProtocol;
using MinecraftGame.Common;

namespace DummyMinecraftClient
{
    /// <summary>
    /// Dummy Minecraft client for protocol testing
    /// Supports both legacy and enhanced protobuf protocols
    /// </summary>
    public class Program
    {
        private const string DEFAULT_HOST = "127.0.0.1";
        private const int DEFAULT_PORT = 7777;
        private const int BUFFER_SIZE = 65536;
        private const int TIMEOUT_MS = 30000;
        private const int KEEPALIVE_INTERVAL_MS = 15000;

        private static string? _sessionId;
        private static string? _username;
        private static bool _useEnhancedProtocol = true;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Dummy Minecraft Client - Protocol Testing Tool ===");
            Console.WriteLine($"Version: 1.0.0");
            Console.WriteLine($"Protocol: Enhanced (supports legacy fallback)");
            Console.WriteLine();

            string host = DEFAULT_HOST;
            int port = DEFAULT_PORT;
            string username = "TestUser";

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--host":
                    case "-h":
                        if (i + 1 < args.Length)
                            host = args[++i];
                        break;
                        goto default;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        break;
                }
            }

            try
            {
                await RunClientAsync(host, port, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        private static async Task RunClientAsync(string host, int port, string username)
        {
            Console.WriteLine($"Connecting to {host}:{port} as '{username}'...");

            using var tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(host, port);
                var stream = tcpClient.GetStream();

                Console.WriteLine("Connected! Starting protocol handshake...");

                // Send login request
                await SendLoginRequestAsync(stream, username);

                // Start keepalive
                var keepaliveTask = Task.Run(async () =>
                {
                    while (tcpClient.Connected)
                    {
                        await Task.Delay(KEEPALIVE_INTERVAL_MS);
                        await SendPingAsync(stream);
                    }
                });

                // Start message receiving loop
                await MessageLoopAsync(stream, tcpClient);

                keepaliveTask.Wait();
            }
        }

        private static async Task SendLoginRequestAsync(NetworkStream stream, string username)
        {
            Console.WriteLine("Sending LoginRequest...");

            // Try enhanced protocol first
            if (_useEnhancedProtocol)
            {
                try
                {
                    var loginRequest = new EnhancedMinecraftProtocol.PlayerInfo
                    {
                        Username = username,
                        Position = new Vector3 { X = 0, Y = 64, Z = 0 },
                        Rotation = new Vector3 { X = 0, Y = 0, Z = 0 },
                        Level = 1,
                        Experience = 0,
                        ExperienceProgress = 0f,
                        Health = 20f,
                        MaxHealth = 20f,
                        Hunger = 20f,
                        MaxHunger = 20f,
                        Saturation = 5f,
                        GameMode = GameMode.Survival,
                        SelectedSlot = 0
                    };

                    var messageBytes = loginRequest.ToByteArray();
                    await SendMessageAsync(stream, (int)MinecraftMessageType.PlayerStateUpdate, messageBytes, true);
                    Console.WriteLine($"Enhanced LoginRequest sent ({messageBytes.Length} bytes)");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Enhanced protocol failed, falling back to legacy: {ex.Message}");
                }
            }

            // Fallback to legacy protocol
            var legacyLogin = new SharedProtocol.LoginRequest
            {
                Username = username,
                Password = "test",
                ClientVersion = "1.0.0"
            };

            var legacyBytes = SerializeLegacyMessage(legacyLogin);
            await SendMessageAsync(stream, (int)SharedProtocol.MessageType.LoginRequest, legacyBytes, false);
            Console.WriteLine($"Legacy LoginRequest sent ({legacyBytes.Length} bytes)");
        }

        private static async Task SendPingAsync(NetworkStream stream)
        {
            var ping = new EnhancedMinecraftProtocol.SoundEffect
            {
                SoundType = SoundType.FootstepStone,
                Position = new Vector3 { X = 0, Y = 64, Z = 0 },
                Volume = 0.5f,
                Pitch = 1.0f,
                Category = SoundCategory.SndPlayer
            };

            var bytes = ping.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.SoundEffect, bytes, true);
            Console.WriteLine("Ping sent");
        }

        private static async Task SendChunkLoadRequestAsync(NetworkStream stream)
        {
            Console.WriteLine("Sending ChunkLoadRequest...");

            var request = new EnhancedMinecraftProtocol.ChunkLoadRequest();
            request.ChunkPositions.Add(new Vector3Int { X = 0, Y = 0, Z = 0 });
            request.ChunkPositions.Add(new Vector3Int { X = 1, Y = 0, Z = 0 });
            request.ChunkPositions.Add(new Vector3Int { X = 0, Y = 0, Z = 1 });
            request.ChunkPositions.Add(new Vector3Int { X = 1, Y = 0, Z = 1 });
            request.ViewDistance = 4;

            var bytes = request.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.ChunkDataRequest, bytes, true);
            Console.WriteLine($"ChunkLoadRequest sent ({request.ChunkPositions.Count} chunks, {bytes.Length} bytes)");
        }

        private static async Task SendBlockPlaceRequestAsync(NetworkStream stream)
        {
            Console.WriteLine("Sending BlockPlaceRequest...");

            var request = new EnhancedMinecraftProtocol.PlayerActionRequest
            {
                Action = EnhancedMinecraftProtocol.PlayerAction.PlaceBlock,
                TargetPosition = new Vector3Int { X = 10, Y = 65, Z = 10 },
                Face = 1, // Top face
                CursorPosition = new Vector3 { X = 0.5f, Y = 0.5f, Z = 0.5f },
                UsedItem = new EnhancedMinecraftProtocol.ItemStack
                {
                    ItemId = 1,
                    ItemName = "Stone",
                    Count = 64,
                    Durability = 100,
                    MaxDurability = 100,
                    ItemType = EnhancedMinecraftProtocol.ItemType.Block,
                    Rarity = EnhancedMinecraftProtocol.ItemRarity.Common
                },
                Sequence = 1
            };

            var bytes = request.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.PlayerActionRequest, bytes, true);
            Console.WriteLine($"BlockPlaceRequest sent ({bytes.Length} bytes)");
        }

        private static async Task SendBlockBreakRequestAsync(NetworkStream stream)
        {
            Console.WriteLine("Sending BlockBreakStartRequest...");

            var request = new EnhancedMinecraftProtocol.PlayerActionRequest
            {
                Action = EnhancedMinecraftProtocol.PlayerAction.StartDestroyBlock,
                TargetPosition = new Vector3Int { X = 10, Y = 65, Z = 10 },
                Face = 0,
                CursorPosition = new Vector3 { X = 0.5f, Y = 0.5f, Z = 0.5f },
                UsedItem = new EnhancedMinecraftProtocol.ItemStack
                {
                    ItemId = 2,
                    ItemName = "Pickaxe",
                    Count = 1,
                    Durability = 100,
                    MaxDurability = 100,
                    ItemType = EnhancedMinecraftProtocol.ItemType.Tool,
                    Rarity = EnhancedMinecraftProtocol.ItemRarity.Common
                },
                Sequence = 1
            };

            var bytes = request.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.PlayerActionRequest, bytes, true);
            Console.WriteLine($"BlockBreakStartRequest sent ({bytes.Length} bytes)");
        }

        private static async Task SendChatMessageAsync(NetworkStream stream, string message)
        {
            Console.WriteLine($"Sending chat: {message}");

            var chat = new EnhancedMinecraftProtocol.ChatMessage
            {
                SenderId = _username ?? "Unknown",
                SenderName = _username ?? "TestUser",
                MessageContent = message,
                ChatType = EnhancedMinecraftProtocol.ChatType.ChatGlobal,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                FormattedMessage = $"<{_username ?? "TestUser"}> {message}",
                Style = new EnhancedMinecraftProtocol.ChatStyle
                {
                    Color = "#FFFFFF",
                    Bold = false,
                    Italic = false,
                    Underlined = false,
                    Strikethrough = false,
                    Obfuscated = false
                }
            };

            var bytes = chat.ToByteArray();
            await SendMessageAsync(stream, (int)MinecraftMessageType.ChatMessage, bytes, true);
            }

        private static async Task MessageLoopAsync(NetworkStream stream, TcpClient tcpClient)
        {
            var buffer = new byte[BUFFER_SIZE];
            int messageCount = 0;

            while (tcpClient.Connected)
            {
                try
                {
                    // Read message type (4 bytes)
                    var typeBytes = new byte[4];
                    var read = await stream.ReadAsync(typeBytes, 0, 4);
                    if (read != 4)
                    {
                        Console.WriteLine($"Connection closed (expected 4 bytes, got {read})");
                        break;
                    }

                    var messageType = BitConverter.ToInt32(typeBytes, 0);

                    // Read message length (4 bytes)
                    var lengthBytes = new byte[4];
                    read = await stream.ReadAsync(lengthBytes, 0, 4);
                    if (read != 4)
                    {
                        Console.WriteLine($"Connection closed (expected 4 bytes, got {read})");
                        break;
                    }

                    var messageLength = BitConverter.ToInt32(lengthBytes, 0);

                    // Read message body
                    var messageBytes = new byte[messageLength];
                    read = await stream.ReadAsync(messageBytes, 0, messageLength);
                    if (read != messageLength)
                    {
                        Console.WriteLine($"Connection closed (expected {messageLength} bytes, got {read})");
                        break;
                    }

                    messageCount++;
                    await ProcessMessageAsync(messageType, messageBytes, messageCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"Connection closed. Total messages received: {messageCount}");
        }

        private static async Task ProcessMessageAsync(int messageType, byte[] messageBytes, int messageNumber)
        {
            try
            {
                Console.WriteLine($"[{messageNumber}] Received message type: {messageType} ({messageBytes.Length} bytes)");

                switch (messageType)
                {
                    case (int)MinecraftMessageType.PlayerActionResponse:
                        await HandlePlayerActionResponseAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.ChunkDataResponse:
                        await HandleChunkDataResponseAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.BlockChangeNotification:
                        await HandleBlockChangeNotificationAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.ChatMessage:
                        await HandleChatMessageAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.TimeUpdateBroadcast:
                        await HandleTimeUpdateAsync(messageBytes);
                        break;

                    case (int)MinecraftMessageType.WeatherUpdateBroadcast:
                        await HandleWeatherUpdateAsync(messageBytes);
                        break;

                    default:
                        Console.WriteLine($"  Unknown message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static async Task HandlePlayerActionResponseAsync(byte[] messageBytes)
        {
            try
            {
                var response = EnhancedMinecraftProtocol.PlayerActionResponse.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Action: {response.Action}");
                Console.WriteLine($"  Success: {response.Success}");
                Console.WriteLine($"  Message: {response.Message}");
                Console.WriteLine($"  Sequence: {response.Sequence}");

                if (response.Result != null)
                {
                    Console.WriteLine($"  Updated Items: {response.Result.UpdatedItems.Count}");
                    Console.WriteLine($"  Health Change: {response.Result.HealthChange}");
                    Console.WriteLine($"  Hunger Change: {response.Result.HungerChange}");
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse as enhanced: {ex.Message}");
                Console.WriteLine("Attempting legacy protocol parse...");
                // Could try legacy fallback here
            }
        }

        private static async Task HandleChunkDataResponseAsync(byte[] messageBytes)
        {
            try
            {
                var response = EnhancedMinecraftProtocol.ChunkLoadResponse.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Total Requested: {response.TotalRequested}");
                Console.WriteLine($"  Total Sent: {response.TotalSent}");
                Console.WriteLine($"  Chunks: {response.Chunks.Count}");

                foreach (var chunk in response.Chunks)
                {
                    Console.WriteLine($"    Chunk [{chunk.ChunkX}, {chunk.ChunkZ}]:");
                    Console.WriteLine($"      Block Data: {chunk.BlockData.Length} bytes");
                    Console.WriteLine($"      Biome Data: {chunk.BiomeData.Length} bytes");
                    Console.WriteLine($"      Entities: {chunk.Entities.Count}");
                    Console.WriteLine($"      Tile Entities: {chunk.TileEntities.Count}");
                    Console.WriteLine($"      Timestamp: {chunk.GenerationTimestamp}");
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse chunk data: {ex.Message}");
            }
        }

        private static async Task HandleBlockChangeNotificationAsync(byte[] messageBytes)
        {
            try
            {
                var notification = EnhancedMinecraftProtocol.BlockChangeBroadcast.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Position: [{notification.Position.X}, {notification.Position.Y}, {notification.Position.Z}]");
                Console.WriteLine($"  Old Block: {notification.OldBlockId}");
                Console.WriteLine($"  New Block: {notification.NewBlockId}");
                Console.WriteLine($"  Player: {notification.PlayerId}");
                Console.WriteLine($"  Reason: {notification.Reason}");
                Console.WriteLine($"  Timestamp: {notification.Timestamp}");
                Console.WriteLine($"  Drops: {notification.Drops.Count}");

                if (notification.ParticleEffect != null)
                {
                    Console.WriteLine($"  Particle: {notification.ParticleEffect.ParticleType}");
                }

                if (notification.SoundEffect != null)
                {
                    Console.WriteLine($"  Sound: {notification.SoundEffect.SoundType}");
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse block change: {ex.Message}");
            }
        }

        private static async Task HandleChatMessageAsync(byte[] messageBytes)
        {
            try
            {
                var chat = EnhancedMinecraftProtocol.ChatMessage.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  From: {chat.SenderName}");
                Console.WriteLine($"  Message: {chat.MessageContent}");
                Console.WriteLine($"  Type: {chat.ChatType}");
                Console.WriteLine($"  Timestamp: {chat.Timestamp}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse chat: {ex.Message}");
            }
        }

        private static async Task HandleTimeUpdateAsync(byte[] messageBytes)
        {
            try
            {
                var timeUpdate = EnhancedMinecraftProtocol.TimeUpdateBroadcast.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  World Time: {timeUpdate.WorldTime}");
                Console.WriteLine($"  Day Time: {timeUpdate.DayTime}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse time update: {ex.Message}");
            }
        }

        private static async Task HandleWeatherUpdateAsync(byte[] messageBytes)
        {
            try
            {
                var weatherUpdate = EnhancedMinecraftProtocol.WeatherUpdateBroadcast.Parser.ParseFrom(messageBytes);
                Console.WriteLine($"  Weather: {weatherUpdate.Weather.WeatherType}");
                Console.WriteLine($"  Duration: {weatherUpdate.Weather.DurationTicks}");
                Console.WriteLine($"  Intensity: {weatherUpdate.Weather.Intensity}");
                Console.WriteLine($"  Thundering: {weatherUpdate.Weather.Thundering}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Failed to parse weather update: {ex.Message}");
            }
        }

        private static async Task SendMessageAsync(NetworkStream stream, int messageType, byte[] messageBytes, bool isEnhanced)
        {
            // Build message: [messageType (4 bytes)][messageLength (4 bytes)][messageBody (variable)]
            var header = BitConverter.GetBytes(messageType);
            var length = BitConverter.GetBytes(messageBytes.Length);

            var fullMessage = new byte[8 + messageBytes.Length];
            Buffer.BlockCopy(header, 0, fullMessage, 0);
            Buffer.BlockCopy(length, 0, fullMessage, 4);
            Buffer.BlockCopy(messageBytes, 0, fullMessage, 8);

            await stream.WriteAsync(fullMessage, 0, fullMessage.Length);

            var protocolName = isEnhanced ? "Enhanced" : "Legacy";
            Console.WriteLine($"Sent {protocolName} message type {messageType} ({fullMessage.Length} bytes)");
        }

        private static byte[] SerializeLegacyMessage(object message)
        {
            // Simple serialization for legacy protocol
            using var stream = new System.IO.MemoryStream();
            var writer = new ProtoBuf.Serializer(stream);
            ProtoBuf.Serializer.Serialize(stream, message);
            return stream.ToArray();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: DummyMinecraftClient [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --host <address>    Server host (default: 127.0.0.1)");
            Console.WriteLine("  -h <address>         Server host (default: 127.0.0.1)");
            Console.WriteLine();
            Console.WriteLine("Commands (after connection):");
            Console.WriteLine("  /chunk              Request chunks");
            Console.WriteLine("  /place              Place a block");
            Console.WriteLine("  /break              Start breaking a block");
            Console.WriteLine("  /chat <message>     Send chat message");
            Console.WriteLine("  /help               Show this help");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to disconnect");
        }
    }
}

