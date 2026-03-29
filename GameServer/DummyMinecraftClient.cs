using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MinecraftGame.Common;
using EnhancedMinecraftProtocol;

namespace GameServerApp
{
    /// <summary>
    /// Dummy Minecraft client for protocol testing.
    /// This client can connect to the server and test various protocol messages.
    /// </summary>
    public class DummyMinecraftClient : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private NetworkStream? _networkStream;
        private readonly string _serverHost;
        private readonly int _serverPort;
        private readonly string _playerName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<DummyMinecraftClient> _logger;
        private bool _isConnected;
        private string? _playerId;
        private PlayerInfo? _playerInfo;
        private int _sequenceId;

        public bool IsConnected => _isConnected;
        public string? PlayerId => _playerId;
        public PlayerInfo? PlayerInfo => _playerInfo;

        public DummyMinecraftClient(
            string serverHost,
            int serverPort,
            string playerName,
            ILogger<DummyMinecraftClient> logger)
        {
            _serverHost = serverHost ?? throw new ArgumentNullException(nameof(serverHost));
            _serverPort = serverPort;
            _playerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tcpClient = new TcpClient();
            _cancellationTokenSource = new CancellationTokenSource();
            _sequenceId = 0;
        }

        public async Task ConnectAsync()
        {
            try
            {
                _logger.LogInformation("[DummyClient] Connecting to {Host}:{Port}...", _serverHost, _serverPort);
                await _tcpClient.ConnectAsync(_serverHost, _serverPort).ConfigureAwait(false);
                _networkStream = _tcpClient.GetStream();
                _isConnected = true;
                _logger.LogInformation("[DummyClient] Connected successfully");

                _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to connect");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _isConnected = false;
                _cancellationTokenSource.Cancel();
                _networkStream?.Close();
                _tcpClient.Close();
                _logger.LogInformation("[DummyClient] Disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Error during disconnect");
            }
        }

        public async Task SendAuthRequestAsync()
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var authRequest = new AuthRequest
            {
                Username = _playerName,
                ProtocolVersion = 1,
                ClientVersion = "1.0.0"
            };

            await SendMessageAsync(MessageType.AuthRequest, authRequest).ConfigureAwait(false);
            _logger.LogInformation("[DummyClient] Sent auth request for {Username}", _playerName);
        }

        public async Task SendPlayerMoveAsync(Vector3 position, Vector3 rotation)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var moveRequest = new PlayerMoveRequest
            {
                Position = position,
                Rotation = rotation,
                OnGround = true
            };

            await SendMessageAsync(MessageType.PlayerMove, moveRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent player move to {Position}", position);
        }

        public async Task SendBlockBreakStartAsync(Vector3Int blockPosition, int toolItemId)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var breakStartRequest = new BlockBreakStartRequest
            {
                BlockPosition = blockPosition,
                ToolItemId = toolItemId,
                SequenceId = ++_sequenceId
            };

            await SendMessageAsync(MessageType.BlockBreakStart, breakStartRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent block break start for {Position}", blockPosition);
        }

        public async Task SendBlockBreakCompleteAsync(Vector3Int blockPosition)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var breakCompleteRequest = new BlockBreakCompleteRequest
            {
                BlockPosition = blockPosition,
                SequenceId = _sequenceId
            };

            await SendMessageAsync(MessageType.BlockBreakComplete, breakCompleteRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent block break complete for {Position}", blockPosition);
        }

        public async Task SendBlockPlaceAsync(Vector3Int blockPosition, int blockId, int face)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var placeRequest = new BlockPlaceRequest
            {
                BlockPosition = blockPosition,
                BlockId = blockId,
                BlockMetadata = 0,
                Face = face,
                CursorPosition = new Vector3 { X = 0.5, Y = 0.5, Z = 0.5 },
                UsedItem = new ItemStack
                {
                    ItemId = blockId,
                    ItemName = $"block_{blockId}",
                    Count = 1
                }
            };

            await SendMessageAsync(MessageType.BlockPlace, placeRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent block place at {Position}", blockPosition);
        }

        public async Task SendChunkLoadRequestAsync(int chunkX, int chunkZ, int viewDistance)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var chunkLoadRequest = new ChunkLoadRequest
            {
                ChunkPositions = { new Vector3Int { X = chunkX, Y = 0, Z = chunkZ } },
                ViewDistance = viewDistance
            };

            await SendMessageAsync(MessageType.ChunkLoad, chunkLoadRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent chunk load request for ({X}, {Z})", chunkX, chunkZ);
        }

        public async Task SendChatMessageAsync(string message)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var chatMessage = new ChatMessage
            {
                SenderId = _playerId ?? "unknown",
                SenderName = _playerName,
                MessageContent = message,
                ChatType = ChatType.ChatGlobal,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                FormattedMessage = $"<{_playerName}> {message}"
            };

            await SendMessageAsync(MessageType.Chat, chatMessage).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent chat message: {Message}", message);
        }

        public async Task SendPlayerActionAsync(PlayerAction action, Vector3Int? targetPosition = null)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var actionRequest = new PlayerActionRequest
            {
                Action = action,
                TargetPosition = targetPosition ?? new Vector3Int { X = 0, Y = 0, Z = 0 },
                Face = 0,
                CursorPosition = new Vector3 { X = 0.5, Y = 0.5, Z = 0.5 },
                UsedItem = new ItemStack { ItemId = 0, Count = 0 },
                Sequence = ++_sequenceId,
                ActionData = new ActionData()
            };

            await SendMessageAsync(MessageType.PlayerAction, actionRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent player action: {Action}", action);
        }

        public async Task SendInventoryRequestAsync()
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var inventoryRequest = new InventoryRequest
            {
                RequestType = InventoryRequestType.GetInventory
            };

            await SendMessageAsync(MessageType.Inventory, inventoryRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent inventory request");
        }

        public async Task SendCraftingRequestAsync(int recipeId)
        {
            if (!_isConnected || _networkStream == null)
            {
                throw new InvalidOperationException("Not connected to server");
            }

            var craftingRequest = new CraftingRequest
            {
                RecipeId = recipeId,
                Ingredients = new ItemStack[0],
                CraftingType = CraftingType.CraftingPlayer2X2,
                CraftAmount = 1
            };

            await SendMessageAsync(MessageType.Crafting, craftingRequest).ConfigureAwait(false);
            _logger.LogDebug("[DummyClient] Sent crafting request for recipe {RecipeId}", recipeId);
        }

        private async Task SendMessageAsync(MessageType messageType, IMessage message)
        {
            if (_networkStream == null)
            {
                throw new InvalidOperationException("Network stream is null");
            }

            try
            {
                byte[] messageData = message.ToByteArray();
                byte[] lengthPrefix = BitConverter.GetBytes(messageData.Length);
                
                await _networkStream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length).ConfigureAwait(false);
                await _networkStream.WriteByteAsync((byte)messageType).ConfigureAwait(false);
                await _networkStream.WriteAsync(messageData, 0, messageData.Length).ConfigureAwait(false);
                await _networkStream.FlushAsync().ConfigureAwait(false);
                
                _logger.LogDebug("[DummyClient] Sent message: {Type}, Size: {Size} bytes", messageType, messageData.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to send message");
                throw;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] lengthBuffer = new byte[4];
            byte[] typeBuffer = new byte[1];

            while (!_cancellationTokenSource.Token.IsCancellationRequested && _isConnected)
            {
                try
                {
                    int bytesRead = await _networkStream.ReadAsync(lengthBuffer, 0, 4, _cancellationTokenSource.Token).ConfigureAwait(false);
                    if (bytesRead != 4)
                    {
                        _logger.LogWarning("[DummyClient] Failed to read message length");
                        break;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    
                    bytesRead = await _networkStream.ReadAsync(typeBuffer, 0, 1, _cancellationTokenSource.Token).ConfigureAwait(false);
                    if (bytesRead != 1)
                    {
                        _logger.LogWarning("[DummyClient] Failed to read message type");
                        break;
                    }

                    MessageType messageType = (MessageType)typeBuffer[0];
                    
                    byte[] messageData = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        bytesRead = await _networkStream.ReadAsync(
                            messageData,
                            totalBytesRead,
                            messageLength - totalBytesRead,
                            _cancellationTokenSource.Token).ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            _logger.LogWarning("[DummyClient] Connection closed while reading message");
                            break;
                        }
                        totalBytesRead += bytesRead;
                    }

                    await ProcessMessageAsync(messageType, messageData).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("[DummyClient] Receive loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DummyClient] Error receiving message");
                    break;
                }
            }
        }

        private async Task ProcessMessageAsync(MessageType messageType, byte[] messageData)
        {
            try
            {
                switch (messageType)
                {
                    case MessageType.AuthResponse:
                        await HandleAuthResponseAsync(messageData).ConfigureAwait(false);
                        break;
                    
                    case MessageType.PlayerInfo:
                        await HandlePlayerInfoAsync(messageData).ConfigureAwait(false);
                        break;
                    
                    case MessageType.ChunkData:
                        await HandleChunkDataAsync(messageData).ConfigureAwait(false);
                        break;
                    
                    case MessageType.Chat:
                        await HandleChatMessageAsync(messageData).ConfigureAwait(false);
                        break;
                    
                    case MessageType.BlockChangeBroadcast:
                        await HandleBlockChangeBroadcastAsync(messageData).ConfigureAwait(false);
                        break;
                    
                    case MessageType.Inventory:
                        await HandleInventoryResponseAsync(messageData).ConfigureAwait(false);
                        break;
                    
                    default:
                        _logger.LogDebug("[DummyClient] Received unhandled message type: {Type}", messageType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Error processing message type: {Type}", messageType);
            }
        }

        private async Task HandleAuthResponseAsync(byte[] data)
        {
            try
            {
                var authResponse = AuthResponse.Parser.ParseFrom(data);
                _playerId = authResponse.PlayerId;
                _logger.LogInformation("[DummyClient] Auth response: Success={Success}, PlayerId={PlayerId}", 
                    authResponse.Success, authResponse.PlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to parse auth response");
            }
            await Task.CompletedTask;
        }

        private async Task HandlePlayerInfoAsync(byte[] data)
        {
            try
            {
                var playerInfo = PlayerInfo.Parser.ParseFrom(data);
                _playerInfo = playerInfo;
                _logger.LogInformation("[DummyClient] Player info: Level={Level}, Health={Health}/{MaxHealth}, Hunger={Hunger}/{MaxHunger}",
                    playerInfo.Level, playerInfo.Health, playerInfo.MaxHealth, 
                    playerInfo.Hunger, playerInfo.MaxHunger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to parse player info");
            }
            await Task.CompletedTask;
        }

        private async Task HandleChunkDataAsync(byte[] data)
        {
            try
            {
                var chunkData = ChunkData.Parser.ParseFrom(data);
                _logger.LogInformation("[DummyClient] Received chunk data: ({X}, {Z}), Entities={EntityCount}, TileEntities={TileEntityCount}",
                    chunkData.ChunkX, chunkData.ChunkZ, chunkData.Entities.Count, chunkData.TileEntities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to parse chunk data");
            }
            await Task.CompletedTask;
        }

        private async Task HandleChatMessageAsync(byte[] data)
        {
            try
            {
                var chatMessage = ChatMessage.Parser.ParseFrom(data);
                _logger.LogInformation("[Chat] {Sender}: {Message}", chatMessage.SenderName, chatMessage.MessageContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to parse chat message");
            }
            await Task.CompletedTask;
        }

        private async Task HandleBlockChangeBroadcastAsync(byte[] data)
        {
            try
            {
                var blockChange = BlockChangeBroadcast.Parser.ParseFrom(data);
                _logger.LogInformation("[BlockChange] {Position}: {OldBlock} -> {NewBlock} by {Player}",
                    blockChange.Position, blockChange.OldBlockId, blockChange.NewBlockId, blockChange.PlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to parse block change broadcast");
            }
            await Task.CompletedTask;
        }

        private async Task HandleInventoryResponseAsync(byte[] data)
        {
            try
            {
                var inventoryResponse = InventoryResponse.Parser.ParseFrom(data);
                _logger.LogInformation("[Inventory] Received {SlotCount} inventory slots", 
                    inventoryResponse.Slots.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Failed to parse inventory response");
            }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            _cancellationTokenSource.Dispose();
            _tcpClient.Dispose();
        }

        public async Task RunTestSequenceAsync()
        {
            try
            {
                _logger.LogInformation("[DummyClient] Starting test sequence...");

                await ConnectAsync().ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);

                await SendAuthRequestAsync().ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);

                await SendPlayerMoveAsync(
                    new Vector3 { X = 0, Y = 64, Z = 0 },
                    new Vector3 { X = 0, Y = 0, Z = 0 }).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                await SendBlockBreakStartAsync(new Vector3Int { X = 0, Y = 64, Z = 0 }, 1).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                await SendBlockBreakCompleteAsync(new Vector3Int { X = 0, Y = 64, Z = 0 }).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                await SendBlockPlaceAsync(new Vector3Int { X = 0, Y = 64, Z = 1 }, 5, 0).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                await SendChatMessageAsync("Hello from dummy client!").ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                await SendChunkLoadRequestAsync(0, 0, 4).ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                await SendInventoryRequestAsync().ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);

                _logger.LogInformation("[DummyClient] Test sequence completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DummyClient] Test sequence failed");
            }
        }
    }

    public enum MessageType : byte
    {
        AuthRequest = 1,
        AuthResponse = 2,
        PlayerMove = 3,
        PlayerInfo = 4,
        ChunkLoad = 5,
        ChunkData = 6,
        BlockBreakStart = 7,
        BlockBreakComplete = 8,
        BlockPlace = 9,
        BlockChangeBroadcast = 10,
        Chat = 11,
        PlayerAction = 12,
        Inventory = 13,
        Crafting = 14,
        InventoryResponse = 15
    }

    public class AuthRequest : IMessage
    {
        public string Username { get; set; } = string.Empty;
        public int ProtocolVersion { get; set; }
        public string ClientVersion { get; set; } = string.Empty;

        public void MergeFrom(CodedInputStream input) { }
        public void WriteTo(CodedOutputStream output) { }
        public int CalculateSize() => 0;
        public bool IsInitialized() => !string.IsNullOrEmpty(Username);
    }

    public class AuthResponse : IMessage
    {
        public bool Success { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public void MergeFrom(CodedInputStream input) { }
        public void WriteTo(CodedOutputStream output) { }
        public int CalculateSize() => 0;
        public bool IsInitialized() => true;
    }

    public class PlayerMoveRequest : IMessage
    {
        public Vector3 Position { get; set; } = new Vector3();
        public Vector3 Rotation { get; set; } = new Vector3();
        public bool OnGround { get; set; }

        public void MergeFrom(CodedInputStream input) { }
        public void WriteTo(CodedOutputStream output) { }
        public int CalculateSize() => 0;
        public bool IsInitialized() => true;
    }

    public class InventoryRequest : IMessage
    {
        public InventoryRequestType RequestType { get; set; }

        public void MergeFrom(CodedInputStream input) { }
        public void WriteTo(CodedOutputStream output) { }
        public int CalculateSize() => 0;
        public bool IsInitialized() => true;
    }

    public class InventoryResponse : IMessage
    {
        public List<InventorySlot> Slots { get; set; } = new List<InventorySlot>();

        public void MergeFrom(CodedInputStream input) { }
        public void WriteTo(CodedOutputStream output) { }
        public int CalculateSize() => 0;
        public bool IsInitialized() => true;
    }

    public enum InventoryRequestType
    {
        GetInventory,
        UpdateSlot,
        SwapSlots
    }
}
