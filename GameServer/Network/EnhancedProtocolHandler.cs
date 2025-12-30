using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using EnhancedMinecraftGame;
using GameServerApp.Configuration;
using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.Network
{
    /// <summary>
    /// Enhanced protocol handler with improved packet management,
    /// validation, serialization, and error handling for Minecraft server.
    /// </summary>
    public class EnhancedProtocolHandler
    {
        private readonly DataDrivenConfigManager _configManager;
        private readonly Dictionary<int, PacketHandler> _packetHandlers;
        private readonly Dictionary<Type, PacketSerializer> _packetSerializers;
        private readonly Dictionary<string, Type> _packetTypes;
        private readonly Dictionary<int, Type> _packetIdToType;
        private readonly Queue<QueuedPacket> _packetQueue;
        private readonly object _lockObject = new object();
        
        // Configuration
        private readonly NetworkConfiguration _networkConfig;
        private readonly int _maxPacketSize;
        private readonly bool _enableCompression;
        private readonly int _compressionThreshold;
        private readonly bool _enableEncryption;
        
        // Performance tracking
        private int _packetsSent;
        private int _packetsReceived;
        private int _bytesTransmitted;
        private int _bytesReceived;
        private DateTime _lastStatsReset;
        
        public EnhancedProtocolHandler(DataDrivenConfigManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _packetHandlers = new Dictionary<int, PacketHandler>();
            _packetSerializers = new Dictionary<Type, PacketSerializer>();
            _packetTypes = new Dictionary<string, Type>();
            _packetIdToType = new Dictionary<int, Type>();
            _packetQueue = new Queue<QueuedPacket>();
            
            // Load network configuration
            _networkConfig = _configManager.GetConfiguration<NetworkConfiguration>("network") ?? new NetworkConfiguration();
            _maxPacketSize = _networkConfig.ConnectionSettings.MaxPacketSize;
            _enableCompression = _networkConfig.ConnectionSettings.EnableCompression;
            _compressionThreshold = _networkConfig.ConnectionSettings.CompressionThreshold;
            _enableEncryption = _networkConfig.ConnectionSettings.EnableEncryption;
            
            _packetsSent = 0;
            _packetsReceived = 0;
            _bytesTransmitted = 0;
            _bytesReceived = 0;
            _lastStatsReset = DateTime.UtcNow;
            
            // Initialize packet handlers and serializers
            InitializePacketHandlers();
            InitializePacketSerializers();
        }
        
        /// <summary>
        /// Initialize packet handlers for different packet types
        /// </summary>
        private void InitializePacketHandlers()
        {
            // Core packets
            RegisterPacketHandler(0x01, HandleKeepAlive);
            RegisterPacketHandler(0x02, HandleLoginRequest);
            RegisterPacketHandler(0x03, HandleChatMessage);
            RegisterPacketHandler(0x04, HandlePlayerPosition);
            RegisterPacketHandler(0x05, HandlePlayerLook);
            RegisterPacketHandler(0x06, HandlePlayerMovement);
            
            // Movement packets
            RegisterPacketHandler(0x10, HandlePlayerDigging);
            RegisterPacketHandler(0x11, HandlePlayerBlockPlacement);
            RegisterPacketHandler(0x12, HandleHeldItemChange);
            
            // World packets
            RegisterPacketHandler(0x20, HandleChunkRequest);
            RegisterPacketHandler(0x21, HandleChunkData);
            RegisterPacketHandler(0x22, HandleBlockChange);
            RegisterPacketHandler(0x23, HandleMultiBlockChange);
            
            // Entity packets
            RegisterPacketHandler(0x30, HandleSpawnPlayer);
            RegisterPacketHandler(0x31, HandleSpawnMob);
            RegisterPacketHandler(0x32, HandleSpawnObject);
            RegisterPacketHandler(0x33, HandleEntityMovement);
            RegisterPacketHandler(0x34, HandleEntityLook);
            
            // Inventory packets
            RegisterPacketHandler(0x40, HandleWindowClick);
            RegisterPacketHandler(0x41, HandleWindowClose);
            RegisterPacketHandler(0x42, HandleWindowItems);
            RegisterPacketHandler(0x43, HandleSetSlot);
            
            // Game packets
            RegisterPacketHandler(0x50, HandlePlayerHealth);
            RegisterPacketHandler(0x51, HandlePlayerRespawn);
            RegisterPacketHandler(0x52, HandlePlayerExperience);
            RegisterPacketHandler(0x53, HandlePlayerAbilities);
            
            // World map control packets
            RegisterPacketHandler(0x60, HandleWorldMapRequest);
            RegisterPacketHandler(0x61, HandleWorldMapUpdate);
            RegisterPacketHandler(0x62, HandleWorldMapProfileUpdate);
        }
        
        /// <summary>
        /// Initialize packet serializers for different packet types
        /// </summary>
        private void InitializePacketSerializers()
        {
            // Register packet types
            RegisterPacketType("KeepAlivePacket", typeof(KeepAlivePacket));
            RegisterPacketType("LoginRequestPacket", typeof(LoginRequestPacket));
            RegisterPacketType("ChatMessagePacket", typeof(ChatMessagePacket));
            RegisterPacketType("PlayerPositionPacket", typeof(PlayerPositionPacket));
            RegisterPacketType("PlayerLookPacket", typeof(PlayerLookPacket));
            RegisterPacketType("PlayerMovementPacket", typeof(PlayerMovementPacket));
            
            RegisterPacketType("PlayerDiggingPacket", typeof(PlayerDiggingPacket));
            RegisterPacketType("PlayerBlockPlacementPacket", typeof(PlayerBlockPlacementPacket));
            RegisterPacketType("HeldItemChangePacket", typeof(HeldItemChangePacket));
            
            RegisterPacketType("ChunkRequestPacket", typeof(ChunkRequestPacket));
            RegisterPacketType("ChunkDataPacket", typeof(ChunkDataPacket));
            RegisterPacketType("BlockChangePacket", typeof(BlockChangePacket));
            RegisterPacketType("MultiBlockChangePacket", typeof(MultiBlockChangePacket));
            
            RegisterPacketType("SpawnPlayerPacket", typeof(SpawnPlayerPacket));
            RegisterPacketType("SpawnMobPacket", typeof(SpawnMobPacket));
            RegisterPacketType("SpawnObjectPacket", typeof(SpawnObjectPacket));
            RegisterPacketType("EntityMovementPacket", typeof(EntityMovementPacket));
            RegisterPacketType("EntityLookPacket", typeof(EntityLookPacket));
            
            RegisterPacketType("WindowClickPacket", typeof(WindowClickPacket));
            RegisterPacketType("WindowClosePacket", typeof(WindowClosePacket));
            RegisterPacketType("WindowItemsPacket", typeof(WindowItemsPacket));
            RegisterPacketType("SetSlotPacket", typeof(SetSlotPacket));
            
            RegisterPacketType("PlayerHealthPacket", typeof(PlayerHealthPacket));
            RegisterPacketType("PlayerRespawnPacket", typeof(PlayerRespawnPacket));
            RegisterPacketType("PlayerExperiencePacket", typeof(PlayerExperiencePacket));
            RegisterPacketType("PlayerAbilitiesPacket", typeof(PlayerAbilitiesPacket));
            
            RegisterPacketType("WorldMapRequestPacket", typeof(WorldMapRequestPacket));
            RegisterPacketType("WorldMapUpdatePacket", typeof(WorldMapUpdatePacket));
            RegisterPacketType("WorldMapProfileUpdatePacket", typeof(WorldMapProfileUpdatePacket));
        }
        
        /// <summary>
        /// Register a packet handler for a specific packet ID
        /// </summary>
        private void RegisterPacketHandler(int packetId, PacketHandler handler)
        {
            _packetHandlers[packetId] = handler;
        }
        
        /// <summary>
        /// Register a packet type for serialization
        /// </summary>
        private void RegisterPacketType(string packetName, Type packetType)
        {
            _packetTypes[packetName] = packetType;
            
            // Extract packet ID from type if it has the attribute
            var packetIdAttr = packetType.GetCustomAttributes(typeof(PacketIdAttribute), false)
                .FirstOrDefault() as PacketIdAttribute;
                
            if (packetIdAttr != null)
            {
                _packetIdToType[packetIdAttr.Id] = packetType;
            }
        }
        
        /// <summary>
        /// Handle incoming packet data
        /// </summary>
        public async Task<bool> HandlePacketAsync(byte[] data, string sessionId)
        {
            try
            {
                // Validate packet size
                if (data.Length > _maxPacketSize)
                {
                    Console.WriteLine($"Packet too large: {data.Length} bytes (max: {_maxPacketSize})");
                    return false;
                }
                
                // Decompress if needed
                var decompressedData = data;
                if (_enableCompression && data.Length > _compressionThreshold)
                {
                    decompressedData = await DecompressPacketAsync(data);
                }
                
                // Decrypt if needed
                var decryptedData = decompressedData;
                if (_enableEncryption)
                {
                    decryptedData = await DecryptPacketAsync(decryptedData, sessionId);
                }
                
                // Parse packet header
                if (decryptedData.Length < 1)
                {
                    Console.WriteLine("Packet too short to contain header");
                    return false;
                }
                
                var packetId = decryptedData[0];
                var packetData = decryptedData.Skip(1).ToArray();
                
                // Update statistics
                lock (_lockObject)
                {
                    _packetsReceived++;
                    _bytesReceived += data.Length;
                }
                
                // Find and invoke handler
                if (_packetHandlers.TryGetValue(packetId, out var handler))
                {
                    return await handler(packetData, sessionId);
                }
                
                Console.WriteLine($"Unknown packet ID: {packetId}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling packet: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Send a packet to a client
        /// </summary>
        public async Task<bool> SendPacketAsync<T>(T packet, string sessionId) where T : class
        {
            try
            {
                // Validate packet
                if (!ValidatePacket(packet))
                {
                    return false;
                }
                
                // Serialize packet
                var packetData = await SerializePacketAsync(packet);
                if (packetData == null)
                {
                    return false;
                }
                
                // Compress if needed
                var finalData = packetData;
                if (_enableCompression && packetData.Length > _compressionThreshold)
                {
                    finalData = await CompressPacketAsync(packetData);
                }
                
                // Encrypt if needed
                if (_enableEncryption)
                {
                    finalData = await EncryptPacketAsync(finalData, sessionId);
                }
                
                // Update statistics
                lock (_lockObject)
                {
                    _packetsSent++;
                    _bytesTransmitted += finalData.Length;
                }
                
                // Queue for sending
                lock (_lockObject)
                {
                    _packetQueue.Enqueue(new QueuedPacket
                    {
                        Data = finalData,
                        SessionId = sessionId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending packet: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Process queued packets for sending
        /// </summary>
        public async Task ProcessPacketQueueAsync()
        {
            var packetsToSend = new List<QueuedPacket>();
            
            lock (_lockObject)
            {
                while (_packetQueue.Count > 0 && packetsToSend.Count < 100) // Limit batch size
                {
                    packetsToSend.Add(_packetQueue.Dequeue());
                }
            }
            
            foreach (var packet in packetsToSend)
            {
                await SendRawPacketAsync(packet.Data, packet.SessionId);
            }
        }
        
        /// <summary>
        /// Get protocol statistics
        /// </summary>
        public ProtocolStats GetStats()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var uptime = now - _lastStatsReset;
                
                return new ProtocolStats
                {
                    PacketsSent = _packetsSent,
                    PacketsReceived = _packetsReceived,
                    BytesTransmitted = _bytesTransmitted,
                    BytesReceived = _bytesReceived,
                    UptimeSeconds = (int)uptime.TotalSeconds,
                    PacketsPerSecond = uptime.TotalSeconds > 0 ? (int)(_packetsSent / uptime.TotalSeconds) : 0
                };
            }
        }
        
        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStats()
        {
            lock (_lockObject)
            {
                _packetsSent = 0;
                _packetsReceived = 0;
                _bytesTransmitted = 0;
                _bytesReceived = 0;
                _lastStatsReset = DateTime.UtcNow;
            }
        }
        
        #region Packet Handlers
        
        private async Task<bool> HandleKeepAlive(byte[] data, string sessionId)
        {
            try
            {
                var packet = KeepAlivePacket.Parser.ParseFrom(data);
                
                // Send keep alive response
                var response = new KeepAlivePacket { KeepAliveId = packet.KeepAliveId };
                await SendPacketAsync(response, sessionId);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling keep alive: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleLoginRequest(byte[] data, string sessionId)
        {
            try
            {
                var packet = LoginRequestPacket.Parser.ParseFrom(data);
                
                // Validate login request
                if (!ValidateLoginRequest(packet))
                {
                    return false;
                }
                
                // Process login logic here
                // This would typically involve checking credentials, loading player data, etc.
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling login request: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleChatMessage(byte[] data, string sessionId)
        {
            try
            {
                var packet = ChatMessagePacket.Parser.ParseFrom(data);
                
                // Validate chat message
                if (!ValidateChatMessage(packet))
                {
                    return false;
                }
                
                // Process chat message logic here
                // This would typically involve broadcasting to other players
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling chat message: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerPosition(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerPositionPacket.Parser.ParseFrom(data);
                
                // Validate player position
                if (!ValidatePlayerPosition(packet))
                {
                    return false;
                }
                
                // Process player position update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player position: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerLook(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerLookPacket.Parser.ParseFrom(data);
                
                // Validate player look
                if (!ValidatePlayerLook(packet))
                {
                    return false;
                }
                
                // Process player look update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player look: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerMovement(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerMovementPacket.Parser.ParseFrom(data);
                
                // Validate player movement
                if (!ValidatePlayerMovement(packet))
                {
                    return false;
                }
                
                // Process player movement update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player movement: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerDigging(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerDiggingPacket.Parser.ParseFrom(data);
                
                // Validate player digging
                if (!ValidatePlayerDigging(packet))
                {
                    return false;
                }
                
                // Process player digging here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player digging: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerBlockPlacement(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerBlockPlacementPacket.Parser.ParseFrom(data);
                
                // Validate block placement
                if (!ValidatePlayerBlockPlacement(packet))
                {
                    return false;
                }
                
                // Process block placement here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling block placement: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleHeldItemChange(byte[] data, string sessionId)
        {
            try
            {
                var packet = HeldItemChangePacket.Parser.ParseFrom(data);
                
                // Validate held item change
                if (!ValidateHeldItemChange(packet))
                {
                    return false;
                }
                
                // Process held item change here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling held item change: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleChunkRequest(byte[] data, string sessionId)
        {
            try
            {
                var packet = ChunkRequestPacket.Parser.ParseFrom(data);
                
                // Validate chunk request
                if (!ValidateChunkRequest(packet))
                {
                    return false;
                }
                
                // Process chunk request here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling chunk request: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleChunkData(byte[] data, string sessionId)
        {
            try
            {
                var packet = ChunkDataPacket.Parser.ParseFrom(data);
                
                // Validate chunk data
                if (!ValidateChunkData(packet))
                {
                    return false;
                }
                
                // Process chunk data here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling chunk data: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleBlockChange(byte[] data, string sessionId)
        {
            try
            {
                var packet = BlockChangePacket.Parser.ParseFrom(data);
                
                // Validate block change
                if (!ValidateBlockChange(packet))
                {
                    return false;
                }
                
                // Process block change here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling block change: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleMultiBlockChange(byte[] data, string sessionId)
        {
            try
            {
                var packet = MultiBlockChangePacket.Parser.ParseFrom(data);
                
                // Validate multi block change
                if (!ValidateMultiBlockChange(packet))
                {
                    return false;
                }
                
                // Process multi block change here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling multi block change: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSpawnPlayer(byte[] data, string sessionId)
        {
            try
            {
                var packet = SpawnPlayerPacket.Parser.ParseFrom(data);
                
                // Validate spawn player
                if (!ValidateSpawnPlayer(packet))
                {
                    return false;
                }
                
                // Process spawn player here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling spawn player: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSpawnMob(byte[] data, string sessionId)
        {
            try
            {
                var packet = SpawnMobPacket.Parser.ParseFrom(data);
                
                // Validate spawn mob
                if (!ValidateSpawnMob(packet))
                {
                    return false;
                }
                
                // Process spawn mob here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling spawn mob: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSpawnObject(byte[] data, string sessionId)
        {
            try
            {
                var packet = SpawnObjectPacket.Parser.ParseFrom(data);
                
                // Validate spawn object
                if (!ValidateSpawnObject(packet))
                {
                    return false;
                }
                
                // Process spawn object here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling spawn object: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleEntityMovement(byte[] data, string sessionId)
        {
            try
            {
                var packet = EntityMovementPacket.Parser.ParseFrom(data);
                
                // Validate entity movement
                if (!ValidateEntityMovement(packet))
                {
                    return false;
                }
                
                // Process entity movement here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling entity movement: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleEntityLook(byte[] data, string sessionId)
        {
            try
            {
                var packet = EntityLookPacket.Parser.ParseFrom(data);
                
                // Validate entity look
                if (!ValidateEntityLook(packet))
                {
                    return false;
                }
                
                // Process entity look here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling entity look: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWindowClick(byte[] data, string sessionId)
        {
            try
            {
                var packet = WindowClickPacket.Parser.ParseFrom(data);
                
                // Validate window click
                if (!ValidateWindowClick(packet))
                {
                    return false;
                }
                
                // Process window click here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window click: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWindowClose(byte[] data, string sessionId)
        {
            try
            {
                var packet = WindowClosePacket.Parser.ParseFrom(data);
                
                // Validate window close
                if (!ValidateWindowClose(packet))
                {
                    return false;
                }
                
                // Process window close here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window close: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWindowItems(byte[] data, string sessionId)
        {
            try
            {
                var packet = WindowItemsPacket.Parser.ParseFrom(data);
                
                // Validate window items
                if (!ValidateWindowItems(packet))
                {
                    return false;
                }
                
                // Process window items here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window items: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSetSlot(byte[] data, string sessionId)
        {
            try
            {
                var packet = SetSlotPacket.Parser.ParseFrom(data);
                
                // Validate set slot
                if (!ValidateSetSlot(packet))
                {
                    return false;
                }
                
                // Process set slot here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling set slot: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerHealth(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerHealthPacket.Parser.ParseFrom(data);
                
                // Validate player health
                if (!ValidatePlayerHealth(packet))
                {
                    return false;
                }
                
                // Process player health here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player health: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerRespawn(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerRespawnPacket.Parser.ParseFrom(data);
                
                // Validate player respawn
                if (!ValidatePlayerRespawn(packet))
                {
                    return false;
                }
                
                // Process player respawn here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player respawn: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerExperience(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerExperiencePacket.Parser.ParseFrom(data);
                
                // Validate player experience
                if (!ValidatePlayerExperience(packet))
                {
                    return false;
                }
                
                // Process player experience here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player experience: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerAbilities(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerAbilitiesPacket.Parser.ParseFrom(data);
                
                // Validate player abilities
                if (!ValidatePlayerAbilities(packet))
                {
                    return false;
                }
                
                // Process player abilities here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player abilities: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWorldMapRequest(byte[] data, string sessionId)
        {
            try
            {
                var packet = WorldMapRequestPacket.Parser.ParseFrom(data);
                
                // Validate world map request
                if (!ValidateWorldMapRequest(packet))
                {
                    return false;
                }
                
                // Process world map request here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling world map request: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWorldMapUpdate(byte[] data, string sessionId)
        {
            try
            {
                var packet = WorldMapUpdatePacket.Parser.ParseFrom(data);
                
                // Validate world map update
                if (!ValidateWorldMapUpdate(packet))
                {
                    return false;
                }
                
                // Process world map update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling world map update: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWorldMapProfileUpdate(byte[] data, string sessionId)
        {
            try
            {
                var packet = WorldMapProfileUpdatePacket.Parser.ParseFrom(data);
                
                // Validate world map profile update
                if (!ValidateWorldMapProfileUpdate(packet))
                {
                    return false;
                }
                
                // Process world map profile update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling world map profile update: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Packet Validation
        
        private bool ValidateLoginRequest(LoginRequestPacket packet)
        {
            // Validate username length
            if (string.IsNullOrEmpty(packet.Username) || packet.Username.Length > 16)
            {
                return false;
            }
            
            // Validate protocol version
            if (packet.ProtocolVersion != _networkConfig.ConnectionSettings.ProtocolVersion)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateChatMessage(ChatMessagePacket packet)
        {
            // Validate message length
            if (packet.Message.Length > _networkConfig.SecuritySettings.MaxChatLength)
            {
                return false;
            }
            
            // Validate message content
            if (_networkConfig.SecuritySettings.EnableChatFilter)
            {
                // Check for blocked words
                foreach (var word in _networkConfig.SecuritySettings.BlockedWords)
                {
                    if (packet.Message.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                
                // Check for profanity
                if (_networkConfig.SecuritySettings.EnableProfanityFilter && ContainsProfanity(packet.Message))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private bool ValidatePlayerPosition(PlayerPositionPacket packet)
        {
            // Validate position bounds
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Y) > 30000000 || Math.Abs(packet.Z) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerLook(PlayerLookPacket packet)
        {
            // Validate look angles
            if (packet.Yaw < -180 || packet.Yaw > 180 || packet.Pitch < -90 || packet.Pitch > 90)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerMovement(PlayerMovementPacket packet)
        {
            // Validate movement values
            if (Math.Abs(packet.MotionX) > 10 || Math.Abs(packet.MotionY) > 10 || Math.Abs(packet.MotionZ) > 10)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerDigging(PlayerDiggingPacket packet)
        {
            // Validate digging state
            if (packet.Status < 0 || packet.Status > 5)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerBlockPlacement(PlayerBlockPlacementPacket packet)
        {
            // Validate block placement
            if (packet.X < -30000000 || packet.X > 30000000 ||
                packet.Y < -30000000 || packet.Y > 30000000 ||
                packet.Z < -30000000 || packet.Z > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateHeldItemChange(HeldItemChangePacket packet)
        {
            // Validate held item change
            if (packet.Slot < 0 || packet.Slot > 8)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateChunkRequest(ChunkRequestPacket packet)
        {
            // Validate chunk coordinates
            if (Math.Abs(packet.ChunkX) > 30000000 || Math.Abs(packet.ChunkZ) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateChunkData(ChunkDataPacket packet)
        {
            // Validate chunk data size
            if (packet.Data.Length > _maxPacketSize)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateBlockChange(BlockChangePacket packet)
        {
            // Validate block change coordinates
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Y) > 30000000 || Math.Abs(packet.Z) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateMultiBlockChange(MultiBlockChangePacket packet)
        {
            // Validate multi block change size
            if (packet.Records.Count > 10000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSpawnPlayer(SpawnPlayerPacket packet)
        {
            // Validate spawn player data
            if (string.IsNullOrEmpty(packet.PlayerUuid) || packet.PlayerUuid.Length != 36)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSpawnMob(SpawnMobPacket packet)
        {
            // Validate spawn mob data
            if (string.IsNullOrEmpty(packet.EntityUuid) || packet.EntityUuid.Length != 36)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSpawnObject(SpawnObjectPacket packet)
        {
            // Validate spawn object data
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Y) > 30000000 || Math.Abs(packet.Z) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateEntityMovement(EntityMovementPacket packet)
        {
            // Validate entity movement
            if (Math.Abs(packet.DeltaX) > 10 || Math.Abs(packet.DeltaY) > 10 || Math.Abs(packet.DeltaZ) > 10)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateEntityLook(EntityLookPacket packet)
        {
            // Validate entity look
            if (packet.Yaw < -180 || packet.Yaw > 180 || packet.Pitch < -90 || packet.Pitch > 90)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateWindowClick(WindowClickPacket packet)
        {
            // Validate window click
            if (packet.Slot < -1 || packet.Slot > 54)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateWindowClose(WindowClosePacket packet)
        {
            // Validate window close
            if (packet.WindowId < 0 || packet.WindowId > 255)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateWindowItems(WindowItemsPacket packet)
        {
            // Validate window items
            if (packet.Items.Count > 54)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSetSlot(SetSlotPacket packet)
        {
            // Validate set slot
            if (packet.Slot < 0 || packet.Slot > 54)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerHealth(PlayerHealthPacket packet)
        {
            // Validate player health
            if (packet.Health < 0 || packet.Health > 20)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerRespawn(PlayerRespawnPacket packet)
        {
            // Validate player respawn
            return true; // No specific validation needed
        }
        
        private bool ValidatePlayerExperience(PlayerExperiencePacket packet)
        {
            // Validate player experience
            if (packet.Experience < 0 || packet.Experience > int.MaxValue)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerAbilities(PlayerAbilitiesPacket packet)
        {
            // Validate player abilities
            return true; // No specific validation needed
        }
        
        private bool ValidateWorldMapRequest(WorldMapRequestPacket packet)
        {
            // Validate world map request
            return true; // No specific validation needed
        }
        
        private bool ValidateWorldMapUpdate(WorldMapUpdatePacket packet)
        {
            // Validate world map update
            return true; // No specific validation needed
        }
        
        private bool ValidateWorldMapProfileUpdate(WorldMapProfileUpdatePacket packet)
        {
            // Validate world map profile update
            return true; // No specific validation needed
        }
        
        #endregion
        
        #region Packet Serialization
        
        private async Task<byte[]> SerializePacketAsync<T>(T packet) where T : class
        {
            try
            {
                // Get packet type
                var packetType = typeof(T);
                
                // Check if we have a custom serializer
                if (_packetSerializers.TryGetValue(packetType, out var serializer))
                {
                    return await serializer(packet);
                }
                
                // Default to protobuf serialization
                return packet.ToByteArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing packet {packetType.Name}: {ex.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Compression and Encryption
        
        private async Task<byte[]> CompressPacketAsync(byte[] data)
        {
            // Implement compression logic here
            // This would typically use a compression algorithm like GZIP or ZLIB
            return await Task.FromResult(data); // Placeholder
        }
        
        private async Task<byte[]> DecompressPacketAsync(byte[] data)
        {
            // Implement decompression logic here
            // This would typically use a decompression algorithm like GZIP or ZLIB
            return await Task.FromResult(data); // Placeholder
        }
        
        private async Task<byte[]> EncryptPacketAsync(byte[] data, string sessionId)
        {
            // Implement encryption logic here
            // This would typically use an encryption algorithm like AES
            return await Task.FromResult(data); // Placeholder
        }
        
        private async Task<byte[]> DecryptPacketAsync(byte[] data, string sessionId)
        {
            // Implement decryption logic here
            // This would typically use a decryption algorithm like AES
            return await Task.FromResult(data); // Placeholder
        }
        
        #endregion
        
        #region Utility Methods
        
        private bool ContainsProfanity(string message)
        {
            // Simple profanity filter implementation
            var profanityList = new[] { "badword1", "badword2", "badword3" }; // Would be loaded from config
            
            var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Any(word => profanityList.Contains(word.ToLowerInvariant()));
        }
        
        private async Task SendRawPacketAsync(byte[] data, string sessionId)
        {
            // This would send the raw packet data to the client
            // Implementation depends on the networking layer being used
            await Task.CompletedTask; // Placeholder
        }
        
        private bool ValidatePacket<T>(T packet) where T : class
        {
            // Generic packet validation
            return packet != null;
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Delegate for packet handlers
    /// </summary>
    public delegate Task<bool> PacketHandler(byte[] data, string sessionId);
    
    /// <summary>
    /// Delegate for packet serializers
    /// </summary>
    public delegate Task<byte[]> PacketSerializer<T>(T packet) where T : class;
    
    /// <summary>
    /// Attribute to specify packet ID for packet types
    /// </summary>
    public class PacketIdAttribute : Attribute
    {
        public int Id { get; }
        
        public PacketIdAttribute(int id)
        {
            Id = id;
        }
    }
    
    /// <summary>
    /// Queued packet for sending
    /// </summary>
    public class QueuedPacket
    {
        public byte[] Data { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Protocol statistics
    /// </summary>
    public class ProtocolStats
    {
        public int PacketsSent { get; set; }
        public int PacketsReceived { get; set; }
        public int BytesTransmitted { get; set; }
        public int BytesReceived { get; set; }
        public int UptimeSeconds { get; set; }
        public int PacketsPerSecond { get; set; }
    }
    
    #endregion
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using EnhancedMinecraftGame;
using GameServerApp.Configuration;
using GameServerApp.World;
using GameServerApp.World.Generation;

namespace GameServerApp.Network
{
    /// <summary>
    /// Enhanced protocol handler with improved packet management,
    /// validation, serialization, and error handling for Minecraft server.
    /// </summary>
    public class EnhancedProtocolHandler
    {
        private readonly DataDrivenConfigManager _configManager;
        private readonly Dictionary<int, PacketHandler> _packetHandlers;
        private readonly Dictionary<Type, PacketSerializer> _packetSerializers;
        private readonly Dictionary<string, Type> _packetTypes;
        private readonly Dictionary<int, Type> _packetIdToType;
        private readonly Queue<QueuedPacket> _packetQueue;
        private readonly object _lockObject = new object();
        
        // Configuration
        private readonly NetworkConfiguration _networkConfig;
        private readonly int _maxPacketSize;
        private readonly bool _enableCompression;
        private readonly int _compressionThreshold;
        private readonly bool _enableEncryption;
        
        // Performance tracking
        private int _packetsSent;
        private int _packetsReceived;
        private int _bytesTransmitted;
        private int _bytesReceived;
        private DateTime _lastStatsReset;
        
        public EnhancedProtocolHandler(DataDrivenConfigManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _packetHandlers = new Dictionary<int, PacketHandler>();
            _packetSerializers = new Dictionary<Type, PacketSerializer>();
            _packetTypes = new Dictionary<string, Type>();
            _packetIdToType = new Dictionary<int, Type>();
            _packetQueue = new Queue<QueuedPacket>();
            
            // Load network configuration
            _networkConfig = _configManager.GetConfiguration<NetworkConfiguration>("network") ?? new NetworkConfiguration();
            _maxPacketSize = _networkConfig.ConnectionSettings.MaxPacketSize;
            _enableCompression = _networkConfig.ConnectionSettings.EnableCompression;
            _compressionThreshold = _networkConfig.ConnectionSettings.CompressionThreshold;
            _enableEncryption = _networkConfig.ConnectionSettings.EnableEncryption;
            
            _packetsSent = 0;
            _packetsReceived = 0;
            _bytesTransmitted = 0;
            _bytesReceived = 0;
            _lastStatsReset = DateTime.UtcNow;
            
            // Initialize packet handlers and serializers
            InitializePacketHandlers();
            InitializePacketSerializers();
        }
        
        /// <summary>
        /// Initialize packet handlers for different packet types
        /// </summary>
        private void InitializePacketHandlers()
        {
            // Core packets
            RegisterPacketHandler(0x01, HandleKeepAlive);
            RegisterPacketHandler(0x02, HandleLoginRequest);
            RegisterPacketHandler(0x03, HandleChatMessage);
            RegisterPacketHandler(0x04, HandlePlayerPosition);
            RegisterPacketHandler(0x05, HandlePlayerLook);
            RegisterPacketHandler(0x06, HandlePlayerMovement);
            
            // Movement packets
            RegisterPacketHandler(0x10, HandlePlayerDigging);
            RegisterPacketHandler(0x11, HandlePlayerBlockPlacement);
            RegisterPacketHandler(0x12, HandleHeldItemChange);
            
            // World packets
            RegisterPacketHandler(0x20, HandleChunkRequest);
            RegisterPacketHandler(0x21, HandleChunkData);
            RegisterPacketHandler(0x22, HandleBlockChange);
            RegisterPacketHandler(0x23, HandleMultiBlockChange);
            
            // Entity packets
            RegisterPacketHandler(0x30, HandleSpawnPlayer);
            RegisterPacketHandler(0x31, HandleSpawnMob);
            RegisterPacketHandler(0x32, HandleSpawnObject);
            RegisterPacketHandler(0x33, HandleEntityMovement);
            RegisterPacketHandler(0x34, HandleEntityLook);
            
            // Inventory packets
            RegisterPacketHandler(0x40, HandleWindowClick);
            RegisterPacketHandler(0x41, HandleWindowClose);
            RegisterPacketHandler(0x42, HandleWindowItems);
            RegisterPacketHandler(0x43, HandleSetSlot);
            
            // Game packets
            RegisterPacketHandler(0x50, HandlePlayerHealth);
            RegisterPacketHandler(0x51, HandlePlayerRespawn);
            RegisterPacketHandler(0x52, HandlePlayerExperience);
            RegisterPacketHandler(0x53, HandlePlayerAbilities);
            
            // World map control packets
            RegisterPacketHandler(0x60, HandleWorldMapRequest);
            RegisterPacketHandler(0x61, HandleWorldMapUpdate);
            RegisterPacketHandler(0x62, HandleWorldMapProfileUpdate);
        }
        
        /// <summary>
        /// Initialize packet serializers for different packet types
        /// </summary>
        private void InitializePacketSerializers()
        {
            // Register packet types
            RegisterPacketType("KeepAlivePacket", typeof(KeepAlivePacket));
            RegisterPacketType("LoginRequestPacket", typeof(LoginRequestPacket));
            RegisterPacketType("ChatMessagePacket", typeof(ChatMessagePacket));
            RegisterPacketType("PlayerPositionPacket", typeof(PlayerPositionPacket));
            RegisterPacketType("PlayerLookPacket", typeof(PlayerLookPacket));
            RegisterPacketType("PlayerMovementPacket", typeof(PlayerMovementPacket));
            
            RegisterPacketType("PlayerDiggingPacket", typeof(PlayerDiggingPacket));
            RegisterPacketType("PlayerBlockPlacementPacket", typeof(PlayerBlockPlacementPacket));
            RegisterPacketType("HeldItemChangePacket", typeof(HeldItemChangePacket));
            
            RegisterPacketType("ChunkRequestPacket", typeof(ChunkRequestPacket));
            RegisterPacketType("ChunkDataPacket", typeof(ChunkDataPacket));
            RegisterPacketType("BlockChangePacket", typeof(BlockChangePacket));
            RegisterPacketType("MultiBlockChangePacket", typeof(MultiBlockChangePacket));
            
            RegisterPacketType("SpawnPlayerPacket", typeof(SpawnPlayerPacket));
            RegisterPacketType("SpawnMobPacket", typeof(SpawnMobPacket));
            RegisterPacketType("SpawnObjectPacket", typeof(SpawnObjectPacket));
            RegisterPacketType("EntityMovementPacket", typeof(EntityMovementPacket));
            RegisterPacketType("EntityLookPacket", typeof(EntityLookPacket));
            
            RegisterPacketType("WindowClickPacket", typeof(WindowClickPacket));
            RegisterPacketType("WindowClosePacket", typeof(WindowClosePacket));
            RegisterPacketType("WindowItemsPacket", typeof(WindowItemsPacket));
            RegisterPacketType("SetSlotPacket", typeof(SetSlotPacket));
            
            RegisterPacketType("PlayerHealthPacket", typeof(PlayerHealthPacket));
            RegisterPacketType("PlayerRespawnPacket", typeof(PlayerRespawnPacket));
            RegisterPacketType("PlayerExperiencePacket", typeof(PlayerExperiencePacket));
            RegisterPacketType("PlayerAbilitiesPacket", typeof(PlayerAbilitiesPacket));
            
            RegisterPacketType("WorldMapRequestPacket", typeof(WorldMapRequestPacket));
            RegisterPacketType("WorldMapUpdatePacket", typeof(WorldMapUpdatePacket));
            RegisterPacketType("WorldMapProfileUpdatePacket", typeof(WorldMapProfileUpdatePacket));
        }
        
        /// <summary>
        /// Register a packet handler for a specific packet ID
        /// </summary>
        private void RegisterPacketHandler(int packetId, PacketHandler handler)
        {
            _packetHandlers[packetId] = handler;
        }
        
        /// <summary>
        /// Register a packet type for serialization
        /// </summary>
        private void RegisterPacketType(string packetName, Type packetType)
        {
            _packetTypes[packetName] = packetType;
            
            // Extract packet ID from type if it has the attribute
            var packetIdAttr = packetType.GetCustomAttributes(typeof(PacketIdAttribute), false)
                .FirstOrDefault() as PacketIdAttribute;
                
            if (packetIdAttr != null)
            {
                _packetIdToType[packetIdAttr.Id] = packetType;
            }
        }
        
        /// <summary>
        /// Handle incoming packet data
        /// </summary>
        public async Task<bool> HandlePacketAsync(byte[] data, string sessionId)
        {
            try
            {
                // Validate packet size
                if (data.Length > _maxPacketSize)
                {
                    Console.WriteLine($"Packet too large: {data.Length} bytes (max: {_maxPacketSize})");
                    return false;
                }
                
                // Decompress if needed
                var decompressedData = data;
                if (_enableCompression && data.Length > _compressionThreshold)
                {
                    decompressedData = await DecompressPacketAsync(data);
                }
                
                // Decrypt if needed
                var decryptedData = decompressedData;
                if (_enableEncryption)
                {
                    decryptedData = await DecryptPacketAsync(decryptedData, sessionId);
                }
                
                // Parse packet header
                if (decryptedData.Length < 1)
                {
                    Console.WriteLine("Packet too short to contain header");
                    return false;
                }
                
                var packetId = decryptedData[0];
                var packetData = decryptedData.Skip(1).ToArray();
                
                // Update statistics
                lock (_lockObject)
                {
                    _packetsReceived++;
                    _bytesReceived += data.Length;
                }
                
                // Find and invoke handler
                if (_packetHandlers.TryGetValue(packetId, out var handler))
                {
                    return await handler(packetData, sessionId);
                }
                
                Console.WriteLine($"Unknown packet ID: {packetId}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling packet: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Send a packet to a client
        /// </summary>
        public async Task<bool> SendPacketAsync<T>(T packet, string sessionId) where T : class
        {
            try
            {
                // Validate packet
                if (!ValidatePacket(packet))
                {
                    return false;
                }
                
                // Serialize packet
                var packetData = await SerializePacketAsync(packet);
                if (packetData == null)
                {
                    return false;
                }
                
                // Compress if needed
                var finalData = packetData;
                if (_enableCompression && packetData.Length > _compressionThreshold)
                {
                    finalData = await CompressPacketAsync(packetData);
                }
                
                // Encrypt if needed
                if (_enableEncryption)
                {
                    finalData = await EncryptPacketAsync(finalData, sessionId);
                }
                
                // Update statistics
                lock (_lockObject)
                {
                    _packetsSent++;
                    _bytesTransmitted += finalData.Length;
                }
                
                // Queue for sending
                lock (_lockObject)
                {
                    _packetQueue.Enqueue(new QueuedPacket
                    {
                        Data = finalData,
                        SessionId = sessionId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending packet: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Process queued packets for sending
        /// </summary>
        public async Task ProcessPacketQueueAsync()
        {
            var packetsToSend = new List<QueuedPacket>();
            
            lock (_lockObject)
            {
                while (_packetQueue.Count > 0 && packetsToSend.Count < 100) // Limit batch size
                {
                    packetsToSend.Add(_packetQueue.Dequeue());
                }
            }
            
            foreach (var packet in packetsToSend)
            {
                await SendRawPacketAsync(packet.Data, packet.SessionId);
            }
        }
        
        /// <summary>
        /// Get protocol statistics
        /// </summary>
        public ProtocolStats GetStats()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var uptime = now - _lastStatsReset;
                
                return new ProtocolStats
                {
                    PacketsSent = _packetsSent,
                    PacketsReceived = _packetsReceived,
                    BytesTransmitted = _bytesTransmitted,
                    BytesReceived = _bytesReceived,
                    UptimeSeconds = (int)uptime.TotalSeconds,
                    PacketsPerSecond = uptime.TotalSeconds > 0 ? (int)(_packetsSent / uptime.TotalSeconds) : 0
                };
            }
        }
        
        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStats()
        {
            lock (_lockObject)
            {
                _packetsSent = 0;
                _packetsReceived = 0;
                _bytesTransmitted = 0;
                _bytesReceived = 0;
                _lastStatsReset = DateTime.UtcNow;
            }
        }
        
        #region Packet Handlers
        
        private async Task<bool> HandleKeepAlive(byte[] data, string sessionId)
        {
            try
            {
                var packet = KeepAlivePacket.Parser.ParseFrom(data);
                
                // Send keep alive response
                var response = new KeepAlivePacket { KeepAliveId = packet.KeepAliveId };
                await SendPacketAsync(response, sessionId);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling keep alive: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleLoginRequest(byte[] data, string sessionId)
        {
            try
            {
                var packet = LoginRequestPacket.Parser.ParseFrom(data);
                
                // Validate login request
                if (!ValidateLoginRequest(packet))
                {
                    return false;
                }
                
                // Process login logic here
                // This would typically involve checking credentials, loading player data, etc.
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling login request: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleChatMessage(byte[] data, string sessionId)
        {
            try
            {
                var packet = ChatMessagePacket.Parser.ParseFrom(data);
                
                // Validate chat message
                if (!ValidateChatMessage(packet))
                {
                    return false;
                }
                
                // Process chat message logic here
                // This would typically involve broadcasting to other players
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling chat message: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerPosition(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerPositionPacket.Parser.ParseFrom(data);
                
                // Validate player position
                if (!ValidatePlayerPosition(packet))
                {
                    return false;
                }
                
                // Process player position update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player position: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerLook(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerLookPacket.Parser.ParseFrom(data);
                
                // Validate player look
                if (!ValidatePlayerLook(packet))
                {
                    return false;
                }
                
                // Process player look update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player look: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerMovement(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerMovementPacket.Parser.ParseFrom(data);
                
                // Validate player movement
                if (!ValidatePlayerMovement(packet))
                {
                    return false;
                }
                
                // Process player movement update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player movement: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerDigging(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerDiggingPacket.Parser.ParseFrom(data);
                
                // Validate player digging
                if (!ValidatePlayerDigging(packet))
                {
                    return false;
                }
                
                // Process player digging here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player digging: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerBlockPlacement(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerBlockPlacementPacket.Parser.ParseFrom(data);
                
                // Validate block placement
                if (!ValidatePlayerBlockPlacement(packet))
                {
                    return false;
                }
                
                // Process block placement here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling block placement: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleHeldItemChange(byte[] data, string sessionId)
        {
            try
            {
                var packet = HeldItemChangePacket.Parser.ParseFrom(data);
                
                // Validate held item change
                if (!ValidateHeldItemChange(packet))
                {
                    return false;
                }
                
                // Process held item change here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling held item change: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleChunkRequest(byte[] data, string sessionId)
        {
            try
            {
                var packet = ChunkRequestPacket.Parser.ParseFrom(data);
                
                // Validate chunk request
                if (!ValidateChunkRequest(packet))
                {
                    return false;
                }
                
                // Process chunk request here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling chunk request: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleChunkData(byte[] data, string sessionId)
        {
            try
            {
                var packet = ChunkDataPacket.Parser.ParseFrom(data);
                
                // Validate chunk data
                if (!ValidateChunkData(packet))
                {
                    return false;
                }
                
                // Process chunk data here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling chunk data: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleBlockChange(byte[] data, string sessionId)
        {
            try
            {
                var packet = BlockChangePacket.Parser.ParseFrom(data);
                
                // Validate block change
                if (!ValidateBlockChange(packet))
                {
                    return false;
                }
                
                // Process block change here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling block change: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleMultiBlockChange(byte[] data, string sessionId)
        {
            try
            {
                var packet = MultiBlockChangePacket.Parser.ParseFrom(data);
                
                // Validate multi block change
                if (!ValidateMultiBlockChange(packet))
                {
                    return false;
                }
                
                // Process multi block change here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling multi block change: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSpawnPlayer(byte[] data, string sessionId)
        {
            try
            {
                var packet = SpawnPlayerPacket.Parser.ParseFrom(data);
                
                // Validate spawn player
                if (!ValidateSpawnPlayer(packet))
                {
                    return false;
                }
                
                // Process spawn player here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling spawn player: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSpawnMob(byte[] data, string sessionId)
        {
            try
            {
                var packet = SpawnMobPacket.Parser.ParseFrom(data);
                
                // Validate spawn mob
                if (!ValidateSpawnMob(packet))
                {
                    return false;
                }
                
                // Process spawn mob here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling spawn mob: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSpawnObject(byte[] data, string sessionId)
        {
            try
            {
                var packet = SpawnObjectPacket.Parser.ParseFrom(data);
                
                // Validate spawn object
                if (!ValidateSpawnObject(packet))
                {
                    return false;
                }
                
                // Process spawn object here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling spawn object: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleEntityMovement(byte[] data, string sessionId)
        {
            try
            {
                var packet = EntityMovementPacket.Parser.ParseFrom(data);
                
                // Validate entity movement
                if (!ValidateEntityMovement(packet))
                {
                    return false;
                }
                
                // Process entity movement here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling entity movement: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleEntityLook(byte[] data, string sessionId)
        {
            try
            {
                var packet = EntityLookPacket.Parser.ParseFrom(data);
                
                // Validate entity look
                if (!ValidateEntityLook(packet))
                {
                    return false;
                }
                
                // Process entity look here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling entity look: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWindowClick(byte[] data, string sessionId)
        {
            try
            {
                var packet = WindowClickPacket.Parser.ParseFrom(data);
                
                // Validate window click
                if (!ValidateWindowClick(packet))
                {
                    return false;
                }
                
                // Process window click here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window click: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWindowClose(byte[] data, string sessionId)
        {
            try
            {
                var packet = WindowClosePacket.Parser.ParseFrom(data);
                
                // Validate window close
                if (!ValidateWindowClose(packet))
                {
                    return false;
                }
                
                // Process window close here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window close: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWindowItems(byte[] data, string sessionId)
        {
            try
            {
                var packet = WindowItemsPacket.Parser.ParseFrom(data);
                
                // Validate window items
                if (!ValidateWindowItems(packet))
                {
                    return false;
                }
                
                // Process window items here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window items: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleSetSlot(byte[] data, string sessionId)
        {
            try
            {
                var packet = SetSlotPacket.Parser.ParseFrom(data);
                
                // Validate set slot
                if (!ValidateSetSlot(packet))
                {
                    return false;
                }
                
                // Process set slot here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling set slot: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerHealth(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerHealthPacket.Parser.ParseFrom(data);
                
                // Validate player health
                if (!ValidatePlayerHealth(packet))
                {
                    return false;
                }
                
                // Process player health here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player health: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerRespawn(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerRespawnPacket.Parser.ParseFrom(data);
                
                // Validate player respawn
                if (!ValidatePlayerRespawn(packet))
                {
                    return false;
                }
                
                // Process player respawn here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player respawn: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerExperience(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerExperiencePacket.Parser.ParseFrom(data);
                
                // Validate player experience
                if (!ValidatePlayerExperience(packet))
                {
                    return false;
                }
                
                // Process player experience here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player experience: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandlePlayerAbilities(byte[] data, string sessionId)
        {
            try
            {
                var packet = PlayerAbilitiesPacket.Parser.ParseFrom(data);
                
                // Validate player abilities
                if (!ValidatePlayerAbilities(packet))
                {
                    return false;
                }
                
                // Process player abilities here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling player abilities: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWorldMapRequest(byte[] data, string sessionId)
        {
            try
            {
                var packet = WorldMapRequestPacket.Parser.ParseFrom(data);
                
                // Validate world map request
                if (!ValidateWorldMapRequest(packet))
                {
                    return false;
                }
                
                // Process world map request here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling world map request: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWorldMapUpdate(byte[] data, string sessionId)
        {
            try
            {
                var packet = WorldMapUpdatePacket.Parser.ParseFrom(data);
                
                // Validate world map update
                if (!ValidateWorldMapUpdate(packet))
                {
                    return false;
                }
                
                // Process world map update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling world map update: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> HandleWorldMapProfileUpdate(byte[] data, string sessionId)
        {
            try
            {
                var packet = WorldMapProfileUpdatePacket.Parser.ParseFrom(data);
                
                // Validate world map profile update
                if (!ValidateWorldMapProfileUpdate(packet))
                {
                    return false;
                }
                
                // Process world map profile update here
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling world map profile update: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Packet Validation
        
        private bool ValidateLoginRequest(LoginRequestPacket packet)
        {
            // Validate username length
            if (string.IsNullOrEmpty(packet.Username) || packet.Username.Length > 16)
            {
                return false;
            }
            
            // Validate protocol version
            if (packet.ProtocolVersion != _networkConfig.ConnectionSettings.ProtocolVersion)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateChatMessage(ChatMessagePacket packet)
        {
            // Validate message length
            if (packet.Message.Length > _networkConfig.SecuritySettings.MaxChatLength)
            {
                return false;
            }
            
            // Validate message content
            if (_networkConfig.SecuritySettings.EnableChatFilter)
            {
                // Check for blocked words
                foreach (var word in _networkConfig.SecuritySettings.BlockedWords)
                {
                    if (packet.Message.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                
                // Check for profanity
                if (_networkConfig.SecuritySettings.EnableProfanityFilter && ContainsProfanity(packet.Message))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private bool ValidatePlayerPosition(PlayerPositionPacket packet)
        {
            // Validate position bounds
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Y) > 30000000 || Math.Abs(packet.Z) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerLook(PlayerLookPacket packet)
        {
            // Validate look angles
            if (packet.Yaw < -180 || packet.Yaw > 180 || packet.Pitch < -90 || packet.Pitch > 90)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerMovement(PlayerMovementPacket packet)
        {
            // Validate movement values
            if (Math.Abs(packet.MotionX) > 10 || Math.Abs(packet.MotionY) > 10 || Math.Abs(packet.MotionZ) > 10)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerDigging(PlayerDiggingPacket packet)
        {
            // Validate digging state
            if (packet.Status < 0 || packet.Status > 5)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerBlockPlacement(PlayerBlockPlacementPacket packet)
        {
            // Validate block placement
            if (packet.X < -30000000 || packet.X > 30000000 ||
                packet.Y < -30000000 || packet.Y > 30000000 ||
                packet.Z < -30000000 || packet.Z > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateHeldItemChange(HeldItemChangePacket packet)
        {
            // Validate held item change
            if (packet.Slot < 0 || packet.Slot > 8)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateChunkRequest(ChunkRequestPacket packet)
        {
            // Validate chunk coordinates
            if (Math.Abs(packet.ChunkX) > 30000000 || Math.Abs(packet.ChunkZ) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateChunkData(ChunkDataPacket packet)
        {
            // Validate chunk data size
            if (packet.Data.Length > _maxPacketSize)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateBlockChange(BlockChangePacket packet)
        {
            // Validate block change coordinates
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Y) > 30000000 || Math.Abs(packet.Z) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateMultiBlockChange(MultiBlockChangePacket packet)
        {
            // Validate multi block change size
            if (packet.Records.Count > 10000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSpawnPlayer(SpawnPlayerPacket packet)
        {
            // Validate spawn player data
            if (string.IsNullOrEmpty(packet.PlayerUuid) || packet.PlayerUuid.Length != 36)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSpawnMob(SpawnMobPacket packet)
        {
            // Validate spawn mob data
            if (string.IsNullOrEmpty(packet.EntityUuid) || packet.EntityUuid.Length != 36)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSpawnObject(SpawnObjectPacket packet)
        {
            // Validate spawn object data
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Y) > 30000000 || Math.Abs(packet.Z) > 30000000)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateEntityMovement(EntityMovementPacket packet)
        {
            // Validate entity movement
            if (Math.Abs(packet.DeltaX) > 10 || Math.Abs(packet.DeltaY) > 10 || Math.Abs(packet.DeltaZ) > 10)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateEntityLook(EntityLookPacket packet)
        {
            // Validate entity look
            if (packet.Yaw < -180 || packet.Yaw > 180 || packet.Pitch < -90 || packet.Pitch > 90)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateWindowClick(WindowClickPacket packet)
        {
            // Validate window click
            if (packet.Slot < -1 || packet.Slot > 54)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateWindowClose(WindowClosePacket packet)
        {
            // Validate window close
            if (packet.WindowId < 0 || packet.WindowId > 255)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateWindowItems(WindowItemsPacket packet)
        {
            // Validate window items
            if (packet.Items.Count > 54)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidateSetSlot(SetSlotPacket packet)
        {
            // Validate set slot
            if (packet.Slot < 0 || packet.Slot > 54)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerHealth(PlayerHealthPacket packet)
        {
            // Validate player health
            if (packet.Health < 0 || packet.Health > 20)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerRespawn(PlayerRespawnPacket packet)
        {
            // Validate player respawn
            return true; // No specific validation needed
        }
        
        private bool ValidatePlayerExperience(PlayerExperiencePacket packet)
        {
            // Validate player experience
            if (packet.Experience < 0 || packet.Experience > int.MaxValue)
            {
                return false;
            }
            
            return true;
        }
        
        private bool ValidatePlayerAbilities(PlayerAbilitiesPacket packet)
        {
            // Validate player abilities
            return true; // No specific validation needed
        }
        
        private bool ValidateWorldMapRequest(WorldMapRequestPacket packet)
        {
            // Validate world map request
            return true; // No specific validation needed
        }
        
        private bool ValidateWorldMapUpdate(WorldMapUpdatePacket packet)
        {
            // Validate world map update
            return true; // No specific validation needed
        }
        
        private bool ValidateWorldMapProfileUpdate(WorldMapProfileUpdatePacket packet)
        {
            // Validate world map profile update
            return true; // No specific validation needed
        }
        
        #endregion
        
        #region Packet Serialization
        
        private async Task<byte[]> SerializePacketAsync<T>(T packet) where T : class
        {
            try
            {
                // Get packet type
                var packetType = typeof(T);
                
                // Check if we have a custom serializer
                if (_packetSerializers.TryGetValue(packetType, out var serializer))
                {
                    return await serializer(packet);
                }
                
                // Default to protobuf serialization
                return packet.ToByteArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing packet {packetType.Name}: {ex.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region Compression and Encryption
        
        private async Task<byte[]> CompressPacketAsync(byte[] data)
        {
            // Implement compression logic here
            // This would typically use a compression algorithm like GZIP or ZLIB
            return await Task.FromResult(data); // Placeholder
        }
        
        private async Task<byte[]> DecompressPacketAsync(byte[] data)
        {
            // Implement decompression logic here
            // This would typically use a decompression algorithm like GZIP or ZLIB
            return await Task.FromResult(data); // Placeholder
        }
        
        private async Task<byte[]> EncryptPacketAsync(byte[] data, string sessionId)
        {
            // Implement encryption logic here
            // This would typically use an encryption algorithm like AES
            return await Task.FromResult(data); // Placeholder
        }
        
        private async Task<byte[]> DecryptPacketAsync(byte[] data, string sessionId)
        {
            // Implement decryption logic here
            // This would typically use a decryption algorithm like AES
            return await Task.FromResult(data); // Placeholder
        }
        
        #endregion
        
        #region Utility Methods
        
        private bool ContainsProfanity(string message)
        {
            // Simple profanity filter implementation
            var profanityList = new[] { "badword1", "badword2", "badword3" }; // Would be loaded from config
            
            var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Any(word => profanityList.Contains(word.ToLowerInvariant()));
        }
        
        private async Task SendRawPacketAsync(byte[] data, string sessionId)
        {
            // This would send the raw packet data to the client
            // Implementation depends on the networking layer being used
            await Task.CompletedTask; // Placeholder
        }
        
        private bool ValidatePacket<T>(T packet) where T : class
        {
            // Generic packet validation
            return packet != null;
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Delegate for packet handlers
    /// </summary>
    public delegate Task<bool> PacketHandler(byte[] data, string sessionId);
    
    /// <summary>
    /// Delegate for packet serializers
    /// </summary>
    public delegate Task<byte[]> PacketSerializer<T>(T packet) where T : class;
    
    /// <summary>
    /// Attribute to specify packet ID for packet types
    /// </summary>
    public class PacketIdAttribute : Attribute
    {
        public int Id { get; }
        
        public PacketIdAttribute(int id)
        {
            Id = id;
        }
    }
    
    /// <summary>
    /// Queued packet for sending
    /// </summary>
    public class QueuedPacket
    {
        public byte[] Data { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Protocol statistics
    /// </summary>
    public class ProtocolStats
    {
        public int PacketsSent { get; set; }
        public int PacketsReceived { get; set; }
        public int BytesTransmitted { get; set; }
        public int BytesReceived { get; set; }
        public int UptimeSeconds { get; set; }
        public int PacketsPerSecond { get; set; }
    }
    
    #endregion
}
}
                
                if (packetId == -1)
                {
                    throw new ArgumentException($"Unknown packet type: {packetType.Name}");
                }
                
                // Serialize packet data
                var packetData = packet.ToByteArray();
                
                // Apply compression if enabled and data is large enough
                if (EnableCompression && packetData.Length > CompressionThreshold)
                {
                    packetData = CompressData(packetData);
                }
                
                // Create packet header
                var header = CreatePacketHeader(packetId, packetData.Length);
                
                // Combine header and data
                var fullPacket = new byte[header.Length + packetData.Length];
                Buffer.BlockCopy(header, 0, fullPacket, 0, header.Length);
                Buffer.BlockCopy(packetData, 0, fullPacket, header.Length, packetData.Length);
                
                // Apply encryption if enabled
                if (EnableEncryption)
                {
                    fullPacket = EncryptData(fullPacket);
                }
                
                // Update statistics
                lock (_handlerLock)
                {
                    _totalPacketsSent++;
                    _totalBytesSent += fullPacket.Length;
                    _packetTypeCounts[packetId] = _packetTypeCounts.GetValueOrDefault(packetId, 0) + 1;
                }
                
                return fullPacket;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing packet {packet.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deserialize a packet from bytes
        /// </summary>
        public IMessage? DeserializePacket(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Invalid packet data");
                
            if (data.Length > MaxPacketSize)
                throw new ArgumentException($"Packet too large: {data.Length} bytes");
                
            try
            {
                // Apply decryption if enabled
                if (EnableEncryption)
                {
                    data = DecryptData(data);
                }
                
                // Parse packet header
                var (packetId, dataOffset, dataLength) = ParsePacketHeader(data);
                
                if (packetId == -1)
                {
                    throw new ArgumentException("Invalid packet header");
                }
                
                // Extract packet data
                var packetData = new byte[dataLength];
                Buffer.BlockCopy(data, dataOffset, packetData, 0, dataLength);
                
                // Apply decompression if needed
                if (EnableCompression && dataLength > CompressionThreshold)
                {
                    packetData = DecompressData(packetData);
                }
                
                // Get packet type
                if (!_packetTypes.TryGetValue(packetId, out var packetType))
                {
                    throw new ArgumentException($"Unknown packet ID: {packetId}");
                }
                
                // Deserialize packet
                var packet = Activator.CreateInstance(packetType) as IMessage;
                if (packet != null)
                {
                    packet.MergeFrom(packetData);
                }
                
                // Update statistics
                lock (_handlerLock)
                {
                    _totalPacketsReceived++;
                    _totalBytesReceived += data.Length;
                    _packetTypeCounts[packetId] = _packetTypeCounts.GetValueOrDefault(packetId, 0) + 1;
                }
                
                return packet;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing packet: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Handle a received packet
        /// </summary>
        public void HandlePacket(IMessage packet, NetworkSession session)
        {
            if (packet == null || session == null)
                return;
                
            try
            {
                var packetType = packet.GetType();
                var packetId = GetPacketId(packetType);
                
                if (packetId == -1)
                {
                    Console.WriteLine($"Unknown packet type: {packetType.Name}");
                    return;
                }
                
                if (_packetHandlers.TryGetValue(packetId, out var handler))
                {
                    handler.HandlePacket(packet, session);
                }
                else
                {
                    Console.WriteLine($"No handler registered for packet ID: {packetId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling packet {packet.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get packet ID by type
        /// </summary>
        private int GetPacketId(Type packetType)
        {
            lock (_handlerLock)
            {
                return _packetTypes.FirstOrDefault(kvp => kvp.Value == packetType).Key;
            }
        }
        
        /// <summary>
        /// Create packet header
        /// </summary>
        private byte[] CreatePacketHeader(int packetId, int dataLength)
        {
            var header = new byte[8];
            
            // Packet ID (4 bytes, big-endian)
            header[0] = (byte)(packetId >> 24);
            header[1] = (byte)(packetId >> 16);
            header[2] = (byte)(packetId >> 8);
            header[3] = (byte)packetId;
            
            // Data length (4 bytes, big-endian)
            header[4] = (byte)(dataLength >> 24);
            header[5] = (byte)(dataLength >> 16);
            header[6] = (byte)(dataLength >> 8);
            header[7] = (byte)dataLength;
            
            return header;
        }
        
        /// <summary>
        /// Parse packet header
        /// </summary>
        private (int packetId, int dataOffset, int dataLength) ParsePacketHeader(byte[] data)
        {
            if (data.Length < 8)
                return (-1, 0, 0);
                
            // Packet ID (4 bytes, big-endian)
            var packetId = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
            
            // Data length (4 bytes, big-endian)
            var dataLength = (data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7];
            
            return (packetId, 8, dataLength);
        }
        
        /// <summary>
        /// Compress data using zlib
        /// </summary>
        private byte[] CompressData(byte[] data)
        {
            // Implementation would use System.IO.Compression or similar
            // For now, return original data
            return data;
        }
        
        /// <summary>
        /// Decompress data using zlib
        /// </summary>
        private byte[] DecompressData(byte[] data)
        {
            // Implementation would use System.IO.Compression or similar
            // For now, return original data
            return data;
        }
        
        /// <summary>
        /// Encrypt data
        /// </summary>
        private byte[] EncryptData(byte[] data)
        {
            // Implementation would use appropriate encryption
            // For now, return original data
            return data;
        }
        
        /// <summary>
        /// Decrypt data
        /// </summary>
        private byte[] DecryptData(byte[] data)
        {
            // Implementation would use appropriate decryption
            // For now, return original data
            return data;
        }
        
        /// <summary>
        /// Get protocol statistics
        /// </summary>
        public ProtocolStatistics GetStatistics()
        {
            lock (_handlerLock)
            {
                return new ProtocolStatistics
                {
                    TotalPacketsReceived = _totalPacketsReceived,
                    TotalPacketsSent = _totalPacketsSent,
                    TotalBytesReceived = _totalBytesReceived,
                    TotalBytesSent = _totalBytesSent,
                    PacketTypeCounts = new Dictionary<int, long>(_packetTypeCounts),
                    ProtocolVersion = ProtocolVersion,
                    MaxPacketSize = MaxPacketSize,
                    CompressionEnabled = EnableCompression,
                    EncryptionEnabled = EnableEncryption
                };
            }
        }
        
        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStatistics()
        {
            lock (_handlerLock)
            {
                _totalPacketsReceived = 0;
                _totalPacketsSent = 0;
                _totalBytesReceived = 0;
                _totalBytesSent = 0;
                _packetTypeCounts.Clear();
            }
        }
        
        /// <summary>
        /// Validate packet integrity
        /// </summary>
        public bool ValidatePacket(IMessage packet)
        {
            if (packet == null)
                return false;
                
            try
            {
                // Check if packet is properly initialized
                if (packet.Descriptor == null)
                    return false;
                    
                // Check required fields
                foreach (var field in packet.Descriptor.Fields.InDeclarationOrder())
                {
                    if (field.IsRequired && !packet.HasField(field.FieldNumber))
                        return false;
                }
                
                // Validate packet-specific constraints
                return ValidatePacketConstraints(packet);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Validate packet-specific constraints
        /// </summary>
        private bool ValidatePacketConstraints(IMessage packet)
        {
            // Implement packet-specific validation logic
            // This would depend on the specific packet types
            
            switch (packet)
            {
                case PlayerPositionPacket positionPacket:
                    return ValidatePlayerPosition(positionPacket);
                    
                case ChatMessagePacket chatPacket:
                    return ValidateChatMessage(chatPacket);
                    
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Validate player position packet
        /// </summary>
        private bool ValidatePlayerPosition(PlayerPositionPacket packet)
        {
            // Check for valid coordinates
            if (Math.Abs(packet.X) > 30000000 || Math.Abs(packet.Z) > 30000000)
                return false;
                
            if (packet.Y < -64 || packet.Y > 320)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Validate chat message packet
        /// </summary>
        private bool ValidateChatMessage(ChatMessagePacket packet)
        {
            // Check message length
            if (string.IsNullOrEmpty(packet.Message) || packet.Message.Length > 256)
                return false;
                
            // Check for invalid characters
            if (packet.Message.Any(c => char.IsControl(c) && c != '\n' && c != '\r'))
                return false;
                
            return true;
        }
    }
    
    /// <summary>
    /// Protocol statistics
    /// </summary>
    public class ProtocolStatistics
    {
        public long TotalPacketsReceived { get; set; }
        public long TotalPacketsSent { get; set; }
        public long TotalBytesReceived { get; set; }
        public long TotalBytesSent { get; set; }
        public Dictionary<int, long> PacketTypeCounts { get; set; } = new();
        public int ProtocolVersion { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public bool EncryptionEnabled { get; set; }
    }
    
    /// <summary>
    /// Network session interface
    /// </summary>
    public interface NetworkSession
    {
        string SessionId { get; }
        string PlayerId { get; }
        bool IsAuthenticated { get; }
        DateTime LastActivity { get; set; }
        void SendPacket(IMessage packet);
        void Disconnect(string reason);
    }
    
    /// <summary>
    /// Packet handler interface
    /// </summary>
    public interface IPacketHandler
    {
        void HandlePacket(IMessage packet, NetworkSession session);
    }
}
