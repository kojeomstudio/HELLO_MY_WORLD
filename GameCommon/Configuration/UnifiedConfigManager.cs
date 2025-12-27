using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.Configuration
{
    /// <summary>
    /// Unified configuration manager that handles all game configuration in a centralized way
    /// Supports server, client, and world configurations with validation and default values
    /// </summary>
    public class UnifiedConfigManager
    {
        private static UnifiedConfigManager _instance;
        private static readonly object _lock = new object();
        
        public static UnifiedConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UnifiedConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private ServerConfig _serverConfig;
        private ClientConfig _clientConfig;
        private WorldConfig _worldConfig;
        private GameplayConfig _gameplayConfig;
        private NetworkConfig _networkConfig;
        private Dictionary<string, object> _runtimeConfigs;
        
        private readonly Dictionary<string, string> _configPaths = new()
        {
            { "server", "config/server.json" },
            { "client", "config/client_config.json" },
            { "world", "config/world.json" },
            { "gameplay", "config/gameplay.json" },
            { "network", "config/network.json" },
            { "runtime", "config/runtime.json" }
        };
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        private UnifiedConfigManager()
        {
            InitializeConfigs();
        }
        
        private void InitializeConfigs()
        {
            LoadAllConfigs();
            ValidateAllConfigs();
        }
        
        #region Public Properties
        public ServerConfig ServerConfig => _serverConfig;
        public ClientConfig ClientConfig => _clientConfig;
        public WorldConfig WorldConfig => _worldConfig;
        public GameplayConfig GameplayConfig => _gameplayConfig;
        public NetworkConfig NetworkConfig => _networkConfig;
        #endregion
        
        #region Configuration Loading
        public void LoadAllConfigs()
        {
            try
            {
                _serverConfig = LoadConfig<ServerConfig>(_configPaths["server"], GetDefaultServerConfig());
                _clientConfig = LoadConfig<ClientConfig>(_configPaths["client"], GetDefaultClientConfig());
                _worldConfig = LoadConfig<WorldConfig>(_configPaths["world"], GetDefaultWorldConfig());
                _gameplayConfig = LoadConfig<GameplayConfig>(_configPaths["gameplay"], GetDefaultGameplayConfig());
                _networkConfig = LoadConfig<NetworkConfig>(_configPaths["network"], GetDefaultNetworkConfig());
                _runtimeConfigs = LoadConfig<Dictionary<string, object>>(_configPaths["runtime"], new Dictionary<string, object>());
                
                Debug.Log("[ConfigManager] All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to load configurations: {ex.Message}");
                throw;
            }
        }
        
        public T LoadConfig<T>(string path, T defaultValue = default)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ConfigManager] Config file not found: {path}, creating default");
                    SaveConfig(path, defaultValue);
                    return defaultValue;
                }
                
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                if (config == null)
                {
                    Debug.LogWarning($"[ConfigManager] Failed to deserialize config from {path}, using default");
                    return defaultValue;
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error loading config from {path}: {ex.Message}");
                return defaultValue;
            }
        }
        
        public void SaveConfig<T>(string path, T config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(path, json);
                
                Debug.Log($"[ConfigManager] Config saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error saving config to {path}: {ex.Message}");
            }
        }
        
        public void SaveAllConfigs()
        {
            SaveConfig(_configPaths["server"], _serverConfig);
            SaveConfig(_configPaths["client"], _clientConfig);
            SaveConfig(_configPaths["world"], _worldConfig);
            SaveConfig(_configPaths["gameplay"], _gameplayConfig);
            SaveConfig(_configPaths["network"], _networkConfig);
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Configuration Validation
        public void ValidateAllConfigs()
        {
            ValidateServerConfig();
            ValidateClientConfig();
            ValidateWorldConfig();
            ValidateGameplayConfig();
            ValidateNetworkConfig();
            
            Debug.Log("[ConfigManager] All configurations validated");
        }
        
        private void ValidateServerConfig()
        {
            if (_serverConfig.Network.Port <= 0 || _serverConfig.Network.Port > 65535)
            {
                Debug.LogWarning("[ConfigManager] Invalid server port, using default 9000");
                _serverConfig.Network.Port = 9000;
            }
            
            if (_serverConfig.Network.MaxPlayers <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max players, using default 20");
                _serverConfig.Network.MaxPlayers = 20;
            }
        }
        
        private void ValidateClientConfig()
        {
            if (_clientConfig.Graphics.RenderDistance < 2 || _clientConfig.Graphics.RenderDistance > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid render distance, clamping to range [2, 32]");
                _clientConfig.Graphics.RenderDistance = Mathf.Clamp(_clientConfig.Graphics.RenderDistance, 2, 32);
            }
            
            if (_clientConfig.Audio.MasterVolume < 0 || _clientConfig.Audio.MasterVolume > 1)
            {
                Debug.LogWarning("[ConfigManager] Invalid master volume, clamping to [0, 1]");
                _clientConfig.Audio.MasterVolume = Mathf.Clamp01(_clientConfig.Audio.MasterVolume);
            }
        }
        
        private void ValidateWorldConfig()
        {
            if (_worldConfig.WorldHeight < 64 || _worldConfig.WorldHeight > 512)
            {
                Debug.LogWarning("[ConfigManager] Invalid world height, using default 256");
                _worldConfig.WorldHeight = 256;
            }
            
            if (_worldConfig.ChunkSize <= 0 || _worldConfig.ChunkSize > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid chunk size, using default 16");
                _worldConfig.ChunkSize = 16;
            }
        }
        
        private void ValidateGameplayConfig()
        {
            // Validate gameplay settings
            if (_gameplayConfig.MaxHealth <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max health, using default 100");
                _gameplayConfig.MaxHealth = 100;
            }
        }
        
        private void ValidateNetworkConfig()
        {
            if (_networkConfig.ConnectionTimeoutMs <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid connection timeout, using default 10000");
                _networkConfig.ConnectionTimeoutMs = 10000;
            }
        }
        #endregion
        
        #region Runtime Configuration
        public T GetRuntimeConfig<T>(string key, T defaultValue = default)
        {
            if (_runtimeConfigs.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public void SetRuntimeConfig<T>(string key, T value)
        {
            _runtimeConfigs[key] = value;
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Default Configurations
        private ServerConfig GetDefaultServerConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkSettings
                {
                    Host = "0.0.0.0",
                    Port = 9000,
                    MaxPlayers = 20,
                    MaxConnectionsPerIP = 3,
                    ConnectionTimeoutSeconds = 30,
                    KeepAliveIntervalSeconds = 5,
                    PacketCompressionThreshold = 256
                },
                Database = new DatabaseSettings
                {
                    Provider = "sqlite",
                    ConnectionString = "Data Source=gameserver.db",
                    EnableAutoMigration = true,
                    CommandTimeoutSeconds = 30,
                    MaxPoolSize = 100
                },
                Performance = new PerformanceSettings
                {
                    TickRate = 20,
                    ChunkLoadThreads = 4,
                    MaxChunkLoadsPerTick = 10,
                    ChunkUnloadDelay = 30,
                    EntityUpdateDistance = 128,
                    EnableAsyncChunkGeneration = true,
                    ChunkCacheSize = 1000,
                    EnableGarbageCollection = true
                },
                Security = new SecuritySettings
                {
                    EnableWhitelist = false,
                    EnableAuthentication = true,
                    EnableEncryption = true,
                    MaxPacketSize = 2097152,
                    RateLimitPacketsPerSecond = 100,
                    EnableAntiCheat = true,
                    MaxPlayerSpeed = 10.0f,
                    MaxFlySpeed = 20.0f
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Information",
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    EnableConsoleLogging = true,
                    MaxLogFileSizeMB = 10,
                    MaxLogFiles = 10,
                    EnablePerformanceLogging = false,
                    EnableNetworkLogging = false
                }
            };
        }
        
        private ClientConfig GetDefaultClientConfig()
        {
            return new ClientConfig
            {
                Network = new ClientNetworkSettings
                {
                    ConnectionTimeoutMs = 10000,
                    ReconnectAttempts = 3,
                    ReconnectDelayMs = 5000,
                    MaxPacketSize = 1048576,
                    CompressionEnabled = true,
                    CompressionThreshold = 1024
                },
                Graphics = new GraphicsSettings
                {
                    RenderDistance = 8,
                    MaxRenderDistance = 16,
                    Fov = 75,
                    MaxFov = 110,
                    Brightness = 0.7f,
                    Gamma = 1.0f,
                    VsyncEnabled = true,
                    MaxFps = 60,
                    AntiAliasing = 2,
                    AnisotropicFiltering = true,
                    TextureQuality = "high",
                    ShadowQuality = "medium",
                    ParticleQuality = "high",
                    WaterQuality = "high"
                },
                Audio = new AudioSettings
                {
                    MasterVolume = 0.8f,
                    MusicVolume = 0.7f,
                    SoundVolume = 0.8f,
                    AmbientVolume = 0.6f,
                    VoiceChatVolume = 0.9f,
                    MaxSoundDistance = 32,
                    DopplerEnabled = true,
                    ReverbEnabled = true,
                    AudioDevice = "default"
                },
                Controls = new ControlSettings
                {
                    MouseSensitivity = 1.0f,
                    InvertMouseY = false,
                    SmoothMouse = true,
                    MouseSmoothing = 0.5f,
                    KeyBindings = new Dictionary<string, string>
                    {
                        { "forward", "W" },
                        { "backward", "S" },
                        { "left", "A" },
                        { "right", "D" },
                        { "jump", "Space" },
                        { "sneak", "LeftShift" },
                        { "sprint", "LeftControl" },
                        { "inventory", "E" },
                        { "drop", "Q" },
                        { "use", "RightClick" },
                        { "attack", "LeftClick" },
                        { "chat", "T" },
                        { "pause", "Escape" },
                        { "screenshot", "F2" }
                    }
                }
            };
        }
        
        private WorldConfig GetDefaultWorldConfig()
        {
            return new WorldConfig
            {
                WorldName = "New World",
                Seed = 0,
                GameMode = "survival",
                WorldHeight = 256,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 8,
                TerrainGeneration = new TerrainGenerationSettings
                {
                    SeaLevel = 62,
                    BedrockLevel = 5,
                    NoiseScale = 100.0f,
                    NoiseAmplitude = 50.0f,
                    Octaves = 4,
                    Persistence = 0.5f,
                    Lacunarity = 2.0f,
                    BiomeScale = 0.005f,
                    TemperatureScale = 0.003f,
                    HumidityScale = 0.004f,
                    MountainThreshold = 0.6f,
                    MountainMaxHeight = 200,
                    PlainBaseHeight = 64
                },
                Water = new WaterSettings
                {
                    GlobalWaterLevel = 62,
                    RiverCenterThreshold = 0.0125f,
                    RiverBankThreshold = 0.028f,
                    EnableOceans = true,
                    EnableRivers = true,
                    EnableLakes = true,
                    UseImprovedRivers = true,
                    UseImprovedLakes = true
                },
                Caves = new CaveSettings
                {
                    EnableCaves = true,
                    UseImprovedCaves = true,
                    CaveDensity = 0.3f,
                    CaveNoiseScale = 0.05f,
                    Threshold = 0.45f,
                    MinCaveHeight = 5,
                    MaxCaveHeight = 128,
                    HorizontalFrequency = 0.0026f,
                    VerticalFrequency = 0.018f,
                    NoiseThreshold = 0.45f
                },
                Ores = new OreSettings
                {
                    EnableOreGeneration = true,
                    Coal = new OreVeinSettings { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 },
                    Iron = new OreVeinSettings { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 },
                    Gold = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 },
                    Diamond = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 },
                    Redstone = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 },
                    Lapis = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 }
                },
                Structures = new StructureSettings
                {
                    EnableTrees = true,
                    TreeDensity = 0.05f,
                    EnableVillages = false,
                    EnableMineshafts = false,
                    EnableDungeons = true,
                    DungeonChance = 0.01f
                }
            };
        }
        
        private GameplayConfig GetDefaultGameplayConfig()
        {
            return new GameplayConfig
            {
                Difficulty = "normal",
                GameMode = "survival",
                AllowCheats = false,
                AllowFlight = false,
                KeepInventoryOnDeath = false,
                NaturalRegeneration = true,
                PvpEnabled = true,
                FireSpread = true,
                MobSpawning = true,
                DaylightCycle = true,
                WeatherCycle = true,
                MaxHealth = 100,
                Hunger = new HungerSettings
                {
                    Enabled = true,
                    DepletionRate = 0.5f,
                    StarvationDamage = 1.0f,
                    RegenerationThreshold = 80.0f
                }
            };
        }
        
        private NetworkConfig GetDefaultNetworkConfig()
        {
            return new NetworkConfig
            {
                ConnectionTimeoutMs = 10000,
                ReconnectAttempts = 3,
                ReconnectDelayMs = 5000,
                MaxPacketSize = 1048576,
                CompressionEnabled = true,
                CompressionThreshold = 1024,
                ProtocolVersion = "1.0.0",
                EnableProtobuf = true,
                RateLimitEnabled = true,
                MaxPacketsPerSecond = 20,
                MaxBytesPerSecond = 32768
            };
        }
        #endregion
        
        #region Configuration Merging
        public void MergeConfigs(string basePath)
        {
            try
            {
                var baseConfig = LoadConfig<Dictionary<string, object>>(basePath, new Dictionary<string, object>());
                
                // Merge base config into specific configs
                if (baseConfig.ContainsKey("server"))
                {
                    var serverJson = JsonSerializer.Serialize(baseConfig["server"], _jsonOptions);
                    var mergedServer = JsonSerializer.Deserialize<ServerConfig>(serverJson, _jsonOptions);
                    _serverConfig = mergedServer ?? _serverConfig;
                }
                
                if (baseConfig.ContainsKey("client"))
                {
                    var clientJson = JsonSerializer.Serialize(baseConfig["client"], _jsonOptions);
                    var mergedClient = JsonSerializer.Deserialize<ClientConfig>(clientJson, _jsonOptions);
                    _clientConfig = mergedClient ?? _clientConfig;
                }
                
                // Validate after merging
                ValidateAllConfigs();
                
                Debug.Log($"[ConfigManager] Merged configuration from {basePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to merge configs: {ex.Message}");
            }
        }
        #endregion
        
        #region Configuration Export/Import
        public void ExportConfig(string configType, string outputPath)
        {
            try
            {
                object configToExport = configType.ToLower() switch
                {
                    "server" => _serverConfig,
                    "client" => _clientConfig,
                    "world" => _worldConfig,
                    "gameplay" => _gameplayConfig,
                    "network" => _networkConfig,
                    "runtime" => _runtimeConfigs,
                    "all" => new
                    {
                        server = _serverConfig,
                        client = _clientConfig,
                        world = _worldConfig,
                        gameplay = _gameplayConfig,
                        network = _networkConfig,
                        runtime = _runtimeConfigs
                    },
                    _ => throw new ArgumentException($"Unknown config type: {configType}")
                };
                
                SaveConfig(outputPath, configToExport);
                Debug.Log($"[ConfigManager] Exported {configType} config to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to export config: {ex.Message}");
            }
        }
        
        public void ImportConfig(string configType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Config file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (configType.ToLower())
                {
                    case "server":
                        _serverConfig = JsonSerializer.Deserialize<ServerConfig>(json, _jsonOptions);
                        break;
                    case "client":
                        _clientConfig = JsonSerializer.Deserialize<ClientConfig>(json, _jsonOptions);
                        break;
                    case "world":
                        _worldConfig = JsonSerializer.Deserialize<WorldConfig>(json, _jsonOptions);
                        break;
                    case "gameplay":
                        _gameplayConfig = JsonSerializer.Deserialize<GameplayConfig>(json, _jsonOptions);
                        break;
                    case "network":
                        _networkConfig = JsonSerializer.Deserialize<NetworkConfig>(json, _jsonOptions);
                        break;
                    case "runtime":
                        _runtimeConfigs = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                        break;
                    default:
                        throw new ArgumentException($"Unknown config type: {configType}");
                }
                
                ValidateAllConfigs();
                Debug.Log($"[ConfigManager] Imported {configType} config from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to import config: {ex.Message}");
            }
        }
        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.Configuration
{
    /// <summary>
    /// Unified configuration manager that handles all game configuration in a centralized way
    /// Supports server, client, and world configurations with validation and default values
    /// </summary>
    public class UnifiedConfigManager
    {
        private static UnifiedConfigManager _instance;
        private static readonly object _lock = new object();
        
        public static UnifiedConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UnifiedConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private ServerConfig _serverConfig;
        private ClientConfig _clientConfig;
        private WorldConfig _worldConfig;
        private GameplayConfig _gameplayConfig;
        private NetworkConfig _networkConfig;
        private Dictionary<string, object> _runtimeConfigs;
        
        private readonly Dictionary<string, string> _configPaths = new()
        {
            { "server", "config/server.json" },
            { "client", "config/client_config.json" },
            { "world", "config/world.json" },
            { "gameplay", "config/gameplay.json" },
            { "network", "config/network.json" },
            { "runtime", "config/runtime.json" }
        };
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        private UnifiedConfigManager()
        {
            InitializeConfigs();
        }
        
        private void InitializeConfigs()
        {
            LoadAllConfigs();
            ValidateAllConfigs();
        }
        
        #region Public Properties
        public ServerConfig ServerConfig => _serverConfig;
        public ClientConfig ClientConfig => _clientConfig;
        public WorldConfig WorldConfig => _worldConfig;
        public GameplayConfig GameplayConfig => _gameplayConfig;
        public NetworkConfig NetworkConfig => _networkConfig;
        #endregion
        
        #region Configuration Loading
        public void LoadAllConfigs()
        {
            try
            {
                _serverConfig = LoadConfig<ServerConfig>(_configPaths["server"], GetDefaultServerConfig());
                _clientConfig = LoadConfig<ClientConfig>(_configPaths["client"], GetDefaultClientConfig());
                _worldConfig = LoadConfig<WorldConfig>(_configPaths["world"], GetDefaultWorldConfig());
                _gameplayConfig = LoadConfig<GameplayConfig>(_configPaths["gameplay"], GetDefaultGameplayConfig());
                _networkConfig = LoadConfig<NetworkConfig>(_configPaths["network"], GetDefaultNetworkConfig());
                _runtimeConfigs = LoadConfig<Dictionary<string, object>>(_configPaths["runtime"], new Dictionary<string, object>());
                
                Debug.Log("[ConfigManager] All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to load configurations: {ex.Message}");
                throw;
            }
        }
        
        public T LoadConfig<T>(string path, T defaultValue = default)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ConfigManager] Config file not found: {path}, creating default");
                    SaveConfig(path, defaultValue);
                    return defaultValue;
                }
                
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                if (config == null)
                {
                    Debug.LogWarning($"[ConfigManager] Failed to deserialize config from {path}, using default");
                    return defaultValue;
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error loading config from {path}: {ex.Message}");
                return defaultValue;
            }
        }
        
        public void SaveConfig<T>(string path, T config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(path, json);
                
                Debug.Log($"[ConfigManager] Config saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error saving config to {path}: {ex.Message}");
            }
        }
        
        public void SaveAllConfigs()
        {
            SaveConfig(_configPaths["server"], _serverConfig);
            SaveConfig(_configPaths["client"], _clientConfig);
            SaveConfig(_configPaths["world"], _worldConfig);
            SaveConfig(_configPaths["gameplay"], _gameplayConfig);
            SaveConfig(_configPaths["network"], _networkConfig);
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Configuration Validation
        public void ValidateAllConfigs()
        {
            ValidateServerConfig();
            ValidateClientConfig();
            ValidateWorldConfig();
            ValidateGameplayConfig();
            ValidateNetworkConfig();
            
            Debug.Log("[ConfigManager] All configurations validated");
        }
        
        private void ValidateServerConfig()
        {
            if (_serverConfig.Network.Port <= 0 || _serverConfig.Network.Port > 65535)
            {
                Debug.LogWarning("[ConfigManager] Invalid server port, using default 9000");
                _serverConfig.Network.Port = 9000;
            }
            
            if (_serverConfig.Network.MaxPlayers <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max players, using default 20");
                _serverConfig.Network.MaxPlayers = 20;
            }
        }
        
        private void ValidateClientConfig()
        {
            if (_clientConfig.Graphics.RenderDistance < 2 || _clientConfig.Graphics.RenderDistance > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid render distance, clamping to range [2, 32]");
                _clientConfig.Graphics.RenderDistance = Mathf.Clamp(_clientConfig.Graphics.RenderDistance, 2, 32);
            }
            
            if (_clientConfig.Audio.MasterVolume < 0 || _clientConfig.Audio.MasterVolume > 1)
            {
                Debug.LogWarning("[ConfigManager] Invalid master volume, clamping to [0, 1]");
                _clientConfig.Audio.MasterVolume = Mathf.Clamp01(_clientConfig.Audio.MasterVolume);
            }
        }
        
        private void ValidateWorldConfig()
        {
            if (_worldConfig.WorldHeight < 64 || _worldConfig.WorldHeight > 512)
            {
                Debug.LogWarning("[ConfigManager] Invalid world height, using default 256");
                _worldConfig.WorldHeight = 256;
            }
            
            if (_worldConfig.ChunkSize <= 0 || _worldConfig.ChunkSize > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid chunk size, using default 16");
                _worldConfig.ChunkSize = 16;
            }
        }
        
        private void ValidateGameplayConfig()
        {
            // Validate gameplay settings
            if (_gameplayConfig.MaxHealth <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max health, using default 100");
                _gameplayConfig.MaxHealth = 100;
            }
        }
        
        private void ValidateNetworkConfig()
        {
            if (_networkConfig.ConnectionTimeoutMs <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid connection timeout, using default 10000");
                _networkConfig.ConnectionTimeoutMs = 10000;
            }
        }
        #endregion
        
        #region Runtime Configuration
        public T GetRuntimeConfig<T>(string key, T defaultValue = default)
        {
            if (_runtimeConfigs.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public void SetRuntimeConfig<T>(string key, T value)
        {
            _runtimeConfigs[key] = value;
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Default Configurations
        private ServerConfig GetDefaultServerConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkSettings
                {
                    Host = "0.0.0.0",
                    Port = 9000,
                    MaxPlayers = 20,
                    MaxConnectionsPerIP = 3,
                    ConnectionTimeoutSeconds = 30,
                    KeepAliveIntervalSeconds = 5,
                    PacketCompressionThreshold = 256
                },
                Database = new DatabaseSettings
                {
                    Provider = "sqlite",
                    ConnectionString = "Data Source=gameserver.db",
                    EnableAutoMigration = true,
                    CommandTimeoutSeconds = 30,
                    MaxPoolSize = 100
                },
                Performance = new PerformanceSettings
                {
                    TickRate = 20,
                    ChunkLoadThreads = 4,
                    MaxChunkLoadsPerTick = 10,
                    ChunkUnloadDelay = 30,
                    EntityUpdateDistance = 128,
                    EnableAsyncChunkGeneration = true,
                    ChunkCacheSize = 1000,
                    EnableGarbageCollection = true
                },
                Security = new SecuritySettings
                {
                    EnableWhitelist = false,
                    EnableAuthentication = true,
                    EnableEncryption = true,
                    MaxPacketSize = 2097152,
                    RateLimitPacketsPerSecond = 100,
                    EnableAntiCheat = true,
                    MaxPlayerSpeed = 10.0f,
                    MaxFlySpeed = 20.0f
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Information",
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    EnableConsoleLogging = true,
                    MaxLogFileSizeMB = 10,
                    MaxLogFiles = 10,
                    EnablePerformanceLogging = false,
                    EnableNetworkLogging = false
                }
            };
        }
        
        private ClientConfig GetDefaultClientConfig()
        {
            return new ClientConfig
            {
                Network = new ClientNetworkSettings
                {
                    ConnectionTimeoutMs = 10000,
                    ReconnectAttempts = 3,
                    ReconnectDelayMs = 5000,
                    MaxPacketSize = 1048576,
                    CompressionEnabled = true,
                    CompressionThreshold = 1024
                },
                Graphics = new GraphicsSettings
                {
                    RenderDistance = 8,
                    MaxRenderDistance = 16,
                    Fov = 75,
                    MaxFov = 110,
                    Brightness = 0.7f,
                    Gamma = 1.0f,
                    VsyncEnabled = true,
                    MaxFps = 60,
                    AntiAliasing = 2,
                    AnisotropicFiltering = true,
                    TextureQuality = "high",
                    ShadowQuality = "medium",
                    ParticleQuality = "high",
                    WaterQuality = "high"
                },
                Audio = new AudioSettings
                {
                    MasterVolume = 0.8f,
                    MusicVolume = 0.7f,
                    SoundVolume = 0.8f,
                    AmbientVolume = 0.6f,
                    VoiceChatVolume = 0.9f,
                    MaxSoundDistance = 32,
                    DopplerEnabled = true,
                    ReverbEnabled = true,
                    AudioDevice = "default"
                },
                Controls = new ControlSettings
                {
                    MouseSensitivity = 1.0f,
                    InvertMouseY = false,
                    SmoothMouse = true,
                    MouseSmoothing = 0.5f,
                    KeyBindings = new Dictionary<string, string>
                    {
                        { "forward", "W" },
                        { "backward", "S" },
                        { "left", "A" },
                        { "right", "D" },
                        { "jump", "Space" },
                        { "sneak", "LeftShift" },
                        { "sprint", "LeftControl" },
                        { "inventory", "E" },
                        { "drop", "Q" },
                        { "use", "RightClick" },
                        { "attack", "LeftClick" },
                        { "chat", "T" },
                        { "pause", "Escape" },
                        { "screenshot", "F2" }
                    }
                }
            };
        }
        
        private WorldConfig GetDefaultWorldConfig()
        {
            return new WorldConfig
            {
                WorldName = "New World",
                Seed = 0,
                GameMode = "survival",
                WorldHeight = 256,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 8,
                TerrainGeneration = new TerrainGenerationSettings
                {
                    SeaLevel = 62,
                    BedrockLevel = 5,
                    NoiseScale = 100.0f,
                    NoiseAmplitude = 50.0f,
                    Octaves = 4,
                    Persistence = 0.5f,
                    Lacunarity = 2.0f,
                    BiomeScale = 0.005f,
                    TemperatureScale = 0.003f,
                    HumidityScale = 0.004f,
                    MountainThreshold = 0.6f,
                    MountainMaxHeight = 200,
                    PlainBaseHeight = 64
                },
                Water = new WaterSettings
                {
                    GlobalWaterLevel = 62,
                    RiverCenterThreshold = 0.0125f,
                    RiverBankThreshold = 0.028f,
                    EnableOceans = true,
                    EnableRivers = true,
                    EnableLakes = true,
                    UseImprovedRivers = true,
                    UseImprovedLakes = true
                },
                Caves = new CaveSettings
                {
                    EnableCaves = true,
                    UseImprovedCaves = true,
                    CaveDensity = 0.3f,
                    CaveNoiseScale = 0.05f,
                    Threshold = 0.45f,
                    MinCaveHeight = 5,
                    MaxCaveHeight = 128,
                    HorizontalFrequency = 0.0026f,
                    VerticalFrequency = 0.018f,
                    NoiseThreshold = 0.45f
                },
                Ores = new OreSettings
                {
                    EnableOreGeneration = true,
                    Coal = new OreVeinSettings { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 },
                    Iron = new OreVeinSettings { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 },
                    Gold = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 },
                    Diamond = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 },
                    Redstone = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 },
                    Lapis = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 }
                },
                Structures = new StructureSettings
                {
                    EnableTrees = true,
                    TreeDensity = 0.05f,
                    EnableVillages = false,
                    EnableMineshafts = false,
                    EnableDungeons = true,
                    DungeonChance = 0.01f
                }
            };
        }
        
        private GameplayConfig GetDefaultGameplayConfig()
        {
            return new GameplayConfig
            {
                Difficulty = "normal",
                GameMode = "survival",
                AllowCheats = false,
                AllowFlight = false,
                KeepInventoryOnDeath = false,
                NaturalRegeneration = true,
                PvpEnabled = true,
                FireSpread = true,
                MobSpawning = true,
                DaylightCycle = true,
                WeatherCycle = true,
                MaxHealth = 100,
                Hunger = new HungerSettings
                {
                    Enabled = true,
                    DepletionRate = 0.5f,
                    StarvationDamage = 1.0f,
                    RegenerationThreshold = 80.0f
                }
            };
        }
        
        private NetworkConfig GetDefaultNetworkConfig()
        {
            return new NetworkConfig
            {
                ConnectionTimeoutMs = 10000,
                ReconnectAttempts = 3,
                ReconnectDelayMs = 5000,
                MaxPacketSize = 1048576,
                CompressionEnabled = true,
                CompressionThreshold = 1024,
                ProtocolVersion = "1.0.0",
                EnableProtobuf = true,
                RateLimitEnabled = true,
                MaxPacketsPerSecond = 20,
                MaxBytesPerSecond = 32768
            };
        }
        #endregion
        
        #region Configuration Merging
        public void MergeConfigs(string basePath)
        {
            try
            {
                var baseConfig = LoadConfig<Dictionary<string, object>>(basePath, new Dictionary<string, object>());
                
                // Merge base config into specific configs
                if (baseConfig.ContainsKey("server"))
                {
                    var serverJson = JsonSerializer.Serialize(baseConfig["server"], _jsonOptions);
                    var mergedServer = JsonSerializer.Deserialize<ServerConfig>(serverJson, _jsonOptions);
                    _serverConfig = mergedServer ?? _serverConfig;
                }
                
                if (baseConfig.ContainsKey("client"))
                {
                    var clientJson = JsonSerializer.Serialize(baseConfig["client"], _jsonOptions);
                    var mergedClient = JsonSerializer.Deserialize<ClientConfig>(clientJson, _jsonOptions);
                    _clientConfig = mergedClient ?? _clientConfig;
                }
                
                // Validate after merging
                ValidateAllConfigs();
                
                Debug.Log($"[ConfigManager] Merged configuration from {basePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to merge configs: {ex.Message}");
            }
        }
        #endregion
        
        #region Configuration Export/Import
        public void ExportConfig(string configType, string outputPath)
        {
            try
            {
                object configToExport = configType.ToLower() switch
                {
                    "server" => _serverConfig,
                    "client" => _clientConfig,
                    "world" => _worldConfig,
                    "gameplay" => _gameplayConfig,
                    "network" => _networkConfig,
                    "runtime" => _runtimeConfigs,
                    "all" => new
                    {
                        server = _serverConfig,
                        client = _clientConfig,
                        world = _worldConfig,
                        gameplay = _gameplayConfig,
                        network = _networkConfig,
                        runtime = _runtimeConfigs
                    },
                    _ => throw new ArgumentException($"Unknown config type: {configType}")
                };
                
                SaveConfig(outputPath, configToExport);
                Debug.Log($"[ConfigManager] Exported {configType} config to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to export config: {ex.Message}");
            }
        }
        
        public void ImportConfig(string configType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Config file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (configType.ToLower())
                {
                    case "server":
                        _serverConfig = JsonSerializer.Deserialize<ServerConfig>(json, _jsonOptions);
                        break;
                    case "client":
                        _clientConfig = JsonSerializer.Deserialize<ClientConfig>(json, _jsonOptions);
                        break;
                    case "world":
                        _worldConfig = JsonSerializer.Deserialize<WorldConfig>(json, _jsonOptions);
                        break;
                    case "gameplay":
                        _gameplayConfig = JsonSerializer.Deserialize<GameplayConfig>(json, _jsonOptions);
                        break;
                    case "network":
                        _networkConfig = JsonSerializer.Deserialize<NetworkConfig>(json, _jsonOptions);
                        break;
                    case "runtime":
                        _runtimeConfigs = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                        break;
                    default:
                        throw new ArgumentException($"Unknown config type: {configType}");
                }
                
                ValidateAllConfigs();
                Debug.Log($"[ConfigManager] Imported {configType} config from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to import config: {ex.Message}");
            }
        }
        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.Configuration
{
    /// <summary>
    /// Unified configuration manager that handles all game configuration in a centralized way
    /// Supports server, client, and world configurations with validation and default values
    /// </summary>
    public class UnifiedConfigManager
    {
        private static UnifiedConfigManager _instance;
        private static readonly object _lock = new object();
        
        public static UnifiedConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UnifiedConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private ServerConfig _serverConfig;
        private ClientConfig _clientConfig;
        private WorldConfig _worldConfig;
        private GameplayConfig _gameplayConfig;
        private NetworkConfig _networkConfig;
        private Dictionary<string, object> _runtimeConfigs;
        
        private readonly Dictionary<string, string> _configPaths = new()
        {
            { "server", "config/server.json" },
            { "client", "config/client_config.json" },
            { "world", "config/world.json" },
            { "gameplay", "config/gameplay.json" },
            { "network", "config/network.json" },
            { "runtime", "config/runtime.json" }
        };
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        private UnifiedConfigManager()
        {
            InitializeConfigs();
        }
        
        private void InitializeConfigs()
        {
            LoadAllConfigs();
            ValidateAllConfigs();
        }
        
        #region Public Properties
        public ServerConfig ServerConfig => _serverConfig;
        public ClientConfig ClientConfig => _clientConfig;
        public WorldConfig WorldConfig => _worldConfig;
        public GameplayConfig GameplayConfig => _gameplayConfig;
        public NetworkConfig NetworkConfig => _networkConfig;
        #endregion
        
        #region Configuration Loading
        public void LoadAllConfigs()
        {
            try
            {
                _serverConfig = LoadConfig<ServerConfig>(_configPaths["server"], GetDefaultServerConfig());
                _clientConfig = LoadConfig<ClientConfig>(_configPaths["client"], GetDefaultClientConfig());
                _worldConfig = LoadConfig<WorldConfig>(_configPaths["world"], GetDefaultWorldConfig());
                _gameplayConfig = LoadConfig<GameplayConfig>(_configPaths["gameplay"], GetDefaultGameplayConfig());
                _networkConfig = LoadConfig<NetworkConfig>(_configPaths["network"], GetDefaultNetworkConfig());
                _runtimeConfigs = LoadConfig<Dictionary<string, object>>(_configPaths["runtime"], new Dictionary<string, object>());
                
                Debug.Log("[ConfigManager] All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to load configurations: {ex.Message}");
                throw;
            }
        }
        
        public T LoadConfig<T>(string path, T defaultValue = default)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ConfigManager] Config file not found: {path}, creating default");
                    SaveConfig(path, defaultValue);
                    return defaultValue;
                }
                
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                if (config == null)
                {
                    Debug.LogWarning($"[ConfigManager] Failed to deserialize config from {path}, using default");
                    return defaultValue;
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error loading config from {path}: {ex.Message}");
                return defaultValue;
            }
        }
        
        public void SaveConfig<T>(string path, T config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(path, json);
                
                Debug.Log($"[ConfigManager] Config saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error saving config to {path}: {ex.Message}");
            }
        }
        
        public void SaveAllConfigs()
        {
            SaveConfig(_configPaths["server"], _serverConfig);
            SaveConfig(_configPaths["client"], _clientConfig);
            SaveConfig(_configPaths["world"], _worldConfig);
            SaveConfig(_configPaths["gameplay"], _gameplayConfig);
            SaveConfig(_configPaths["network"], _networkConfig);
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Configuration Validation
        public void ValidateAllConfigs()
        {
            ValidateServerConfig();
            ValidateClientConfig();
            ValidateWorldConfig();
            ValidateGameplayConfig();
            ValidateNetworkConfig();
            
            Debug.Log("[ConfigManager] All configurations validated");
        }
        
        private void ValidateServerConfig()
        {
            if (_serverConfig.Network.Port <= 0 || _serverConfig.Network.Port > 65535)
            {
                Debug.LogWarning("[ConfigManager] Invalid server port, using default 9000");
                _serverConfig.Network.Port = 9000;
            }
            
            if (_serverConfig.Network.MaxPlayers <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max players, using default 20");
                _serverConfig.Network.MaxPlayers = 20;
            }
        }
        
        private void ValidateClientConfig()
        {
            if (_clientConfig.Graphics.RenderDistance < 2 || _clientConfig.Graphics.RenderDistance > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid render distance, clamping to range [2, 32]");
                _clientConfig.Graphics.RenderDistance = Mathf.Clamp(_clientConfig.Graphics.RenderDistance, 2, 32);
            }
            
            if (_clientConfig.Audio.MasterVolume < 0 || _clientConfig.Audio.MasterVolume > 1)
            {
                Debug.LogWarning("[ConfigManager] Invalid master volume, clamping to [0, 1]");
                _clientConfig.Audio.MasterVolume = Mathf.Clamp01(_clientConfig.Audio.MasterVolume);
            }
        }
        
        private void ValidateWorldConfig()
        {
            if (_worldConfig.WorldHeight < 64 || _worldConfig.WorldHeight > 512)
            {
                Debug.LogWarning("[ConfigManager] Invalid world height, using default 256");
                _worldConfig.WorldHeight = 256;
            }
            
            if (_worldConfig.ChunkSize <= 0 || _worldConfig.ChunkSize > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid chunk size, using default 16");
                _worldConfig.ChunkSize = 16;
            }
        }
        
        private void ValidateGameplayConfig()
        {
            // Validate gameplay settings
            if (_gameplayConfig.MaxHealth <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max health, using default 100");
                _gameplayConfig.MaxHealth = 100;
            }
        }
        
        private void ValidateNetworkConfig()
        {
            if (_networkConfig.ConnectionTimeoutMs <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid connection timeout, using default 10000");
                _networkConfig.ConnectionTimeoutMs = 10000;
            }
        }
        #endregion
        
        #region Runtime Configuration
        public T GetRuntimeConfig<T>(string key, T defaultValue = default)
        {
            if (_runtimeConfigs.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public void SetRuntimeConfig<T>(string key, T value)
        {
            _runtimeConfigs[key] = value;
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Default Configurations
        private ServerConfig GetDefaultServerConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkSettings
                {
                    Host = "0.0.0.0",
                    Port = 9000,
                    MaxPlayers = 20,
                    MaxConnectionsPerIP = 3,
                    ConnectionTimeoutSeconds = 30,
                    KeepAliveIntervalSeconds = 5,
                    PacketCompressionThreshold = 256
                },
                Database = new DatabaseSettings
                {
                    Provider = "sqlite",
                    ConnectionString = "Data Source=gameserver.db",
                    EnableAutoMigration = true,
                    CommandTimeoutSeconds = 30,
                    MaxPoolSize = 100
                },
                Performance = new PerformanceSettings
                {
                    TickRate = 20,
                    ChunkLoadThreads = 4,
                    MaxChunkLoadsPerTick = 10,
                    ChunkUnloadDelay = 30,
                    EntityUpdateDistance = 128,
                    EnableAsyncChunkGeneration = true,
                    ChunkCacheSize = 1000,
                    EnableGarbageCollection = true
                },
                Security = new SecuritySettings
                {
                    EnableWhitelist = false,
                    EnableAuthentication = true,
                    EnableEncryption = true,
                    MaxPacketSize = 2097152,
                    RateLimitPacketsPerSecond = 100,
                    EnableAntiCheat = true,
                    MaxPlayerSpeed = 10.0f,
                    MaxFlySpeed = 20.0f
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Information",
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    EnableConsoleLogging = true,
                    MaxLogFileSizeMB = 10,
                    MaxLogFiles = 10,
                    EnablePerformanceLogging = false,
                    EnableNetworkLogging = false
                }
            };
        }
        
        private ClientConfig GetDefaultClientConfig()
        {
            return new ClientConfig
            {
                Network = new ClientNetworkSettings
                {
                    ConnectionTimeoutMs = 10000,
                    ReconnectAttempts = 3,
                    ReconnectDelayMs = 5000,
                    MaxPacketSize = 1048576,
                    CompressionEnabled = true,
                    CompressionThreshold = 1024
                },
                Graphics = new GraphicsSettings
                {
                    RenderDistance = 8,
                    MaxRenderDistance = 16,
                    Fov = 75,
                    MaxFov = 110,
                    Brightness = 0.7f,
                    Gamma = 1.0f,
                    VsyncEnabled = true,
                    MaxFps = 60,
                    AntiAliasing = 2,
                    AnisotropicFiltering = true,
                    TextureQuality = "high",
                    ShadowQuality = "medium",
                    ParticleQuality = "high",
                    WaterQuality = "high"
                },
                Audio = new AudioSettings
                {
                    MasterVolume = 0.8f,
                    MusicVolume = 0.7f,
                    SoundVolume = 0.8f,
                    AmbientVolume = 0.6f,
                    VoiceChatVolume = 0.9f,
                    MaxSoundDistance = 32,
                    DopplerEnabled = true,
                    ReverbEnabled = true,
                    AudioDevice = "default"
                },
                Controls = new ControlSettings
                {
                    MouseSensitivity = 1.0f,
                    InvertMouseY = false,
                    SmoothMouse = true,
                    MouseSmoothing = 0.5f,
                    KeyBindings = new Dictionary<string, string>
                    {
                        { "forward", "W" },
                        { "backward", "S" },
                        { "left", "A" },
                        { "right", "D" },
                        { "jump", "Space" },
                        { "sneak", "LeftShift" },
                        { "sprint", "LeftControl" },
                        { "inventory", "E" },
                        { "drop", "Q" },
                        { "use", "RightClick" },
                        { "attack", "LeftClick" },
                        { "chat", "T" },
                        { "pause", "Escape" },
                        { "screenshot", "F2" }
                    }
                }
            };
        }
        
        private WorldConfig GetDefaultWorldConfig()
        {
            return new WorldConfig
            {
                WorldName = "New World",
                Seed = 0,
                GameMode = "survival",
                WorldHeight = 256,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 8,
                TerrainGeneration = new TerrainGenerationSettings
                {
                    SeaLevel = 62,
                    BedrockLevel = 5,
                    NoiseScale = 100.0f,
                    NoiseAmplitude = 50.0f,
                    Octaves = 4,
                    Persistence = 0.5f,
                    Lacunarity = 2.0f,
                    BiomeScale = 0.005f,
                    TemperatureScale = 0.003f,
                    HumidityScale = 0.004f,
                    MountainThreshold = 0.6f,
                    MountainMaxHeight = 200,
                    PlainBaseHeight = 64
                },
                Water = new WaterSettings
                {
                    GlobalWaterLevel = 62,
                    RiverCenterThreshold = 0.0125f,
                    RiverBankThreshold = 0.028f,
                    EnableOceans = true,
                    EnableRivers = true,
                    EnableLakes = true,
                    UseImprovedRivers = true,
                    UseImprovedLakes = true
                },
                Caves = new CaveSettings
                {
                    EnableCaves = true,
                    UseImprovedCaves = true,
                    CaveDensity = 0.3f,
                    CaveNoiseScale = 0.05f,
                    Threshold = 0.45f,
                    MinCaveHeight = 5,
                    MaxCaveHeight = 128,
                    HorizontalFrequency = 0.0026f,
                    VerticalFrequency = 0.018f,
                    NoiseThreshold = 0.45f
                },
                Ores = new OreSettings
                {
                    EnableOreGeneration = true,
                    Coal = new OreVeinSettings { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 },
                    Iron = new OreVeinSettings { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 },
                    Gold = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 },
                    Diamond = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 },
                    Redstone = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 },
                    Lapis = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 }
                },
                Structures = new StructureSettings
                {
                    EnableTrees = true,
                    TreeDensity = 0.05f,
                    EnableVillages = false,
                    EnableMineshafts = false,
                    EnableDungeons = true,
                    DungeonChance = 0.01f
                }
            };
        }
        
        private GameplayConfig GetDefaultGameplayConfig()
        {
            return new GameplayConfig
            {
                Difficulty = "normal",
                GameMode = "survival",
                AllowCheats = false,
                AllowFlight = false,
                KeepInventoryOnDeath = false,
                NaturalRegeneration = true,
                PvpEnabled = true,
                FireSpread = true,
                MobSpawning = true,
                DaylightCycle = true,
                WeatherCycle = true,
                MaxHealth = 100,
                Hunger = new HungerSettings
                {
                    Enabled = true,
                    DepletionRate = 0.5f,
                    StarvationDamage = 1.0f,
                    RegenerationThreshold = 80.0f
                }
            };
        }
        
        private NetworkConfig GetDefaultNetworkConfig()
        {
            return new NetworkConfig
            {
                ConnectionTimeoutMs = 10000,
                ReconnectAttempts = 3,
                ReconnectDelayMs = 5000,
                MaxPacketSize = 1048576,
                CompressionEnabled = true,
                CompressionThreshold = 1024,
                ProtocolVersion = "1.0.0",
                EnableProtobuf = true,
                RateLimitEnabled = true,
                MaxPacketsPerSecond = 20,
                MaxBytesPerSecond = 32768
            };
        }
        #endregion
        
        #region Configuration Merging
        public void MergeConfigs(string basePath)
        {
            try
            {
                var baseConfig = LoadConfig<Dictionary<string, object>>(basePath, new Dictionary<string, object>());
                
                // Merge base config into specific configs
                if (baseConfig.ContainsKey("server"))
                {
                    var serverJson = JsonSerializer.Serialize(baseConfig["server"], _jsonOptions);
                    var mergedServer = JsonSerializer.Deserialize<ServerConfig>(serverJson, _jsonOptions);
                    _serverConfig = mergedServer ?? _serverConfig;
                }
                
                if (baseConfig.ContainsKey("client"))
                {
                    var clientJson = JsonSerializer.Serialize(baseConfig["client"], _jsonOptions);
                    var mergedClient = JsonSerializer.Deserialize<ClientConfig>(clientJson, _jsonOptions);
                    _clientConfig = mergedClient ?? _clientConfig;
                }
                
                // Validate after merging
                ValidateAllConfigs();
                
                Debug.Log($"[ConfigManager] Merged configuration from {basePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to merge configs: {ex.Message}");
            }
        }
        #endregion
        
        #region Configuration Export/Import
        public void ExportConfig(string configType, string outputPath)
        {
            try
            {
                object configToExport = configType.ToLower() switch
                {
                    "server" => _serverConfig,
                    "client" => _clientConfig,
                    "world" => _worldConfig,
                    "gameplay" => _gameplayConfig,
                    "network" => _networkConfig,
                    "runtime" => _runtimeConfigs,
                    "all" => new
                    {
                        server = _serverConfig,
                        client = _clientConfig,
                        world = _worldConfig,
                        gameplay = _gameplayConfig,
                        network = _networkConfig,
                        runtime = _runtimeConfigs
                    },
                    _ => throw new ArgumentException($"Unknown config type: {configType}")
                };
                
                SaveConfig(outputPath, configToExport);
                Debug.Log($"[ConfigManager] Exported {configType} config to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to export config: {ex.Message}");
            }
        }
        
        public void ImportConfig(string configType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Config file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (configType.ToLower())
                {
                    case "server":
                        _serverConfig = JsonSerializer.Deserialize<ServerConfig>(json, _jsonOptions);
                        break;
                    case "client":
                        _clientConfig = JsonSerializer.Deserialize<ClientConfig>(json, _jsonOptions);
                        break;
                    case "world":
                        _worldConfig = JsonSerializer.Deserialize<WorldConfig>(json, _jsonOptions);
                        break;
                    case "gameplay":
                        _gameplayConfig = JsonSerializer.Deserialize<GameplayConfig>(json, _jsonOptions);
                        break;
                    case "network":
                        _networkConfig = JsonSerializer.Deserialize<NetworkConfig>(json, _jsonOptions);
                        break;
                    case "runtime":
                        _runtimeConfigs = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                        break;
                    default:
                        throw new ArgumentException($"Unknown config type: {configType}");
                }
                
                ValidateAllConfigs();
                Debug.Log($"[ConfigManager] Imported {configType} config from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to import config: {ex.Message}");
            }
        }
        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.Configuration
{
    /// <summary>
    /// Unified configuration manager that handles all game configuration in a centralized way
    /// Supports server, client, and world configurations with validation and default values
    /// </summary>
    public class UnifiedConfigManager
    {
        private static UnifiedConfigManager _instance;
        private static readonly object _lock = new object();
        
        public static UnifiedConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UnifiedConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private ServerConfig _serverConfig;
        private ClientConfig _clientConfig;
        private WorldConfig _worldConfig;
        private GameplayConfig _gameplayConfig;
        private NetworkConfig _networkConfig;
        private Dictionary<string, object> _runtimeConfigs;
        
        private readonly Dictionary<string, string> _configPaths = new()
        {
            { "server", "config/server.json" },
            { "client", "config/client_config.json" },
            { "world", "config/world.json" },
            { "gameplay", "config/gameplay.json" },
            { "network", "config/network.json" },
            { "runtime", "config/runtime.json" }
        };
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        private UnifiedConfigManager()
        {
            InitializeConfigs();
        }
        
        private void InitializeConfigs()
        {
            LoadAllConfigs();
            ValidateAllConfigs();
        }
        
        #region Public Properties
        public ServerConfig ServerConfig => _serverConfig;
        public ClientConfig ClientConfig => _clientConfig;
        public WorldConfig WorldConfig => _worldConfig;
        public GameplayConfig GameplayConfig => _gameplayConfig;
        public NetworkConfig NetworkConfig => _networkConfig;
        #endregion
        
        #region Configuration Loading
        public void LoadAllConfigs()
        {
            try
            {
                _serverConfig = LoadConfig<ServerConfig>(_configPaths["server"], GetDefaultServerConfig());
                _clientConfig = LoadConfig<ClientConfig>(_configPaths["client"], GetDefaultClientConfig());
                _worldConfig = LoadConfig<WorldConfig>(_configPaths["world"], GetDefaultWorldConfig());
                _gameplayConfig = LoadConfig<GameplayConfig>(_configPaths["gameplay"], GetDefaultGameplayConfig());
                _networkConfig = LoadConfig<NetworkConfig>(_configPaths["network"], GetDefaultNetworkConfig());
                _runtimeConfigs = LoadConfig<Dictionary<string, object>>(_configPaths["runtime"], new Dictionary<string, object>());
                
                Debug.Log("[ConfigManager] All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to load configurations: {ex.Message}");
                throw;
            }
        }
        
        public T LoadConfig<T>(string path, T defaultValue = default)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ConfigManager] Config file not found: {path}, creating default");
                    SaveConfig(path, defaultValue);
                    return defaultValue;
                }
                
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                if (config == null)
                {
                    Debug.LogWarning($"[ConfigManager] Failed to deserialize config from {path}, using default");
                    return defaultValue;
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error loading config from {path}: {ex.Message}");
                return defaultValue;
            }
        }
        
        public void SaveConfig<T>(string path, T config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(path, json);
                
                Debug.Log($"[ConfigManager] Config saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error saving config to {path}: {ex.Message}");
            }
        }
        
        public void SaveAllConfigs()
        {
            SaveConfig(_configPaths["server"], _serverConfig);
            SaveConfig(_configPaths["client"], _clientConfig);
            SaveConfig(_configPaths["world"], _worldConfig);
            SaveConfig(_configPaths["gameplay"], _gameplayConfig);
            SaveConfig(_configPaths["network"], _networkConfig);
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Configuration Validation
        public void ValidateAllConfigs()
        {
            ValidateServerConfig();
            ValidateClientConfig();
            ValidateWorldConfig();
            ValidateGameplayConfig();
            ValidateNetworkConfig();
            
            Debug.Log("[ConfigManager] All configurations validated");
        }
        
        private void ValidateServerConfig()
        {
            if (_serverConfig.Network.Port <= 0 || _serverConfig.Network.Port > 65535)
            {
                Debug.LogWarning("[ConfigManager] Invalid server port, using default 9000");
                _serverConfig.Network.Port = 9000;
            }
            
            if (_serverConfig.Network.MaxPlayers <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max players, using default 20");
                _serverConfig.Network.MaxPlayers = 20;
            }
        }
        
        private void ValidateClientConfig()
        {
            if (_clientConfig.Graphics.RenderDistance < 2 || _clientConfig.Graphics.RenderDistance > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid render distance, clamping to range [2, 32]");
                _clientConfig.Graphics.RenderDistance = Mathf.Clamp(_clientConfig.Graphics.RenderDistance, 2, 32);
            }
            
            if (_clientConfig.Audio.MasterVolume < 0 || _clientConfig.Audio.MasterVolume > 1)
            {
                Debug.LogWarning("[ConfigManager] Invalid master volume, clamping to [0, 1]");
                _clientConfig.Audio.MasterVolume = Mathf.Clamp01(_clientConfig.Audio.MasterVolume);
            }
        }
        
        private void ValidateWorldConfig()
        {
            if (_worldConfig.WorldHeight < 64 || _worldConfig.WorldHeight > 512)
            {
                Debug.LogWarning("[ConfigManager] Invalid world height, using default 256");
                _worldConfig.WorldHeight = 256;
            }
            
            if (_worldConfig.ChunkSize <= 0 || _worldConfig.ChunkSize > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid chunk size, using default 16");
                _worldConfig.ChunkSize = 16;
            }
        }
        
        private void ValidateGameplayConfig()
        {
            // Validate gameplay settings
            if (_gameplayConfig.MaxHealth <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max health, using default 100");
                _gameplayConfig.MaxHealth = 100;
            }
        }
        
        private void ValidateNetworkConfig()
        {
            if (_networkConfig.ConnectionTimeoutMs <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid connection timeout, using default 10000");
                _networkConfig.ConnectionTimeoutMs = 10000;
            }
        }
        #endregion
        
        #region Runtime Configuration
        public T GetRuntimeConfig<T>(string key, T defaultValue = default)
        {
            if (_runtimeConfigs.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public void SetRuntimeConfig<T>(string key, T value)
        {
            _runtimeConfigs[key] = value;
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Default Configurations
        private ServerConfig GetDefaultServerConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkSettings
                {
                    Host = "0.0.0.0",
                    Port = 9000,
                    MaxPlayers = 20,
                    MaxConnectionsPerIP = 3,
                    ConnectionTimeoutSeconds = 30,
                    KeepAliveIntervalSeconds = 5,
                    PacketCompressionThreshold = 256
                },
                Database = new DatabaseSettings
                {
                    Provider = "sqlite",
                    ConnectionString = "Data Source=gameserver.db",
                    EnableAutoMigration = true,
                    CommandTimeoutSeconds = 30,
                    MaxPoolSize = 100
                },
                Performance = new PerformanceSettings
                {
                    TickRate = 20,
                    ChunkLoadThreads = 4,
                    MaxChunkLoadsPerTick = 10,
                    ChunkUnloadDelay = 30,
                    EntityUpdateDistance = 128,
                    EnableAsyncChunkGeneration = true,
                    ChunkCacheSize = 1000,
                    EnableGarbageCollection = true
                },
                Security = new SecuritySettings
                {
                    EnableWhitelist = false,
                    EnableAuthentication = true,
                    EnableEncryption = true,
                    MaxPacketSize = 2097152,
                    RateLimitPacketsPerSecond = 100,
                    EnableAntiCheat = true,
                    MaxPlayerSpeed = 10.0f,
                    MaxFlySpeed = 20.0f
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Information",
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    EnableConsoleLogging = true,
                    MaxLogFileSizeMB = 10,
                    MaxLogFiles = 10,
                    EnablePerformanceLogging = false,
                    EnableNetworkLogging = false
                }
            };
        }
        
        private ClientConfig GetDefaultClientConfig()
        {
            return new ClientConfig
            {
                Network = new ClientNetworkSettings
                {
                    ConnectionTimeoutMs = 10000,
                    ReconnectAttempts = 3,
                    ReconnectDelayMs = 5000,
                    MaxPacketSize = 1048576,
                    CompressionEnabled = true,
                    CompressionThreshold = 1024
                },
                Graphics = new GraphicsSettings
                {
                    RenderDistance = 8,
                    MaxRenderDistance = 16,
                    Fov = 75,
                    MaxFov = 110,
                    Brightness = 0.7f,
                    Gamma = 1.0f,
                    VsyncEnabled = true,
                    MaxFps = 60,
                    AntiAliasing = 2,
                    AnisotropicFiltering = true,
                    TextureQuality = "high",
                    ShadowQuality = "medium",
                    ParticleQuality = "high",
                    WaterQuality = "high"
                },
                Audio = new AudioSettings
                {
                    MasterVolume = 0.8f,
                    MusicVolume = 0.7f,
                    SoundVolume = 0.8f,
                    AmbientVolume = 0.6f,
                    VoiceChatVolume = 0.9f,
                    MaxSoundDistance = 32,
                    DopplerEnabled = true,
                    ReverbEnabled = true,
                    AudioDevice = "default"
                },
                Controls = new ControlSettings
                {
                    MouseSensitivity = 1.0f,
                    InvertMouseY = false,
                    SmoothMouse = true,
                    MouseSmoothing = 0.5f,
                    KeyBindings = new Dictionary<string, string>
                    {
                        { "forward", "W" },
                        { "backward", "S" },
                        { "left", "A" },
                        { "right", "D" },
                        { "jump", "Space" },
                        { "sneak", "LeftShift" },
                        { "sprint", "LeftControl" },
                        { "inventory", "E" },
                        { "drop", "Q" },
                        { "use", "RightClick" },
                        { "attack", "LeftClick" },
                        { "chat", "T" },
                        { "pause", "Escape" },
                        { "screenshot", "F2" }
                    }
                }
            };
        }
        
        private WorldConfig GetDefaultWorldConfig()
        {
            return new WorldConfig
            {
                WorldName = "New World",
                Seed = 0,
                GameMode = "survival",
                WorldHeight = 256,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 8,
                TerrainGeneration = new TerrainGenerationSettings
                {
                    SeaLevel = 62,
                    BedrockLevel = 5,
                    NoiseScale = 100.0f,
                    NoiseAmplitude = 50.0f,
                    Octaves = 4,
                    Persistence = 0.5f,
                    Lacunarity = 2.0f,
                    BiomeScale = 0.005f,
                    TemperatureScale = 0.003f,
                    HumidityScale = 0.004f,
                    MountainThreshold = 0.6f,
                    MountainMaxHeight = 200,
                    PlainBaseHeight = 64
                },
                Water = new WaterSettings
                {
                    GlobalWaterLevel = 62,
                    RiverCenterThreshold = 0.0125f,
                    RiverBankThreshold = 0.028f,
                    EnableOceans = true,
                    EnableRivers = true,
                    EnableLakes = true,
                    UseImprovedRivers = true,
                    UseImprovedLakes = true
                },
                Caves = new CaveSettings
                {
                    EnableCaves = true,
                    UseImprovedCaves = true,
                    CaveDensity = 0.3f,
                    CaveNoiseScale = 0.05f,
                    Threshold = 0.45f,
                    MinCaveHeight = 5,
                    MaxCaveHeight = 128,
                    HorizontalFrequency = 0.0026f,
                    VerticalFrequency = 0.018f,
                    NoiseThreshold = 0.45f
                },
                Ores = new OreSettings
                {
                    EnableOreGeneration = true,
                    Coal = new OreVeinSettings { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 },
                    Iron = new OreVeinSettings { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 },
                    Gold = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 },
                    Diamond = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 },
                    Redstone = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 },
                    Lapis = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 }
                },
                Structures = new StructureSettings
                {
                    EnableTrees = true,
                    TreeDensity = 0.05f,
                    EnableVillages = false,
                    EnableMineshafts = false,
                    EnableDungeons = true,
                    DungeonChance = 0.01f
                }
            };
        }
        
        private GameplayConfig GetDefaultGameplayConfig()
        {
            return new GameplayConfig
            {
                Difficulty = "normal",
                GameMode = "survival",
                AllowCheats = false,
                AllowFlight = false,
                KeepInventoryOnDeath = false,
                NaturalRegeneration = true,
                PvpEnabled = true,
                FireSpread = true,
                MobSpawning = true,
                DaylightCycle = true,
                WeatherCycle = true,
                MaxHealth = 100,
                Hunger = new HungerSettings
                {
                    Enabled = true,
                    DepletionRate = 0.5f,
                    StarvationDamage = 1.0f,
                    RegenerationThreshold = 80.0f
                }
            };
        }
        
        private NetworkConfig GetDefaultNetworkConfig()
        {
            return new NetworkConfig
            {
                ConnectionTimeoutMs = 10000,
                ReconnectAttempts = 3,
                ReconnectDelayMs = 5000,
                MaxPacketSize = 1048576,
                CompressionEnabled = true,
                CompressionThreshold = 1024,
                ProtocolVersion = "1.0.0",
                EnableProtobuf = true,
                RateLimitEnabled = true,
                MaxPacketsPerSecond = 20,
                MaxBytesPerSecond = 32768
            };
        }
        #endregion
        
        #region Configuration Merging
        public void MergeConfigs(string basePath)
        {
            try
            {
                var baseConfig = LoadConfig<Dictionary<string, object>>(basePath, new Dictionary<string, object>());
                
                // Merge base config into specific configs
                if (baseConfig.ContainsKey("server"))
                {
                    var serverJson = JsonSerializer.Serialize(baseConfig["server"], _jsonOptions);
                    var mergedServer = JsonSerializer.Deserialize<ServerConfig>(serverJson, _jsonOptions);
                    _serverConfig = mergedServer ?? _serverConfig;
                }
                
                if (baseConfig.ContainsKey("client"))
                {
                    var clientJson = JsonSerializer.Serialize(baseConfig["client"], _jsonOptions);
                    var mergedClient = JsonSerializer.Deserialize<ClientConfig>(clientJson, _jsonOptions);
                    _clientConfig = mergedClient ?? _clientConfig;
                }
                
                // Validate after merging
                ValidateAllConfigs();
                
                Debug.Log($"[ConfigManager] Merged configuration from {basePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to merge configs: {ex.Message}");
            }
        }
        #endregion
        
        #region Configuration Export/Import
        public void ExportConfig(string configType, string outputPath)
        {
            try
            {
                object configToExport = configType.ToLower() switch
                {
                    "server" => _serverConfig,
                    "client" => _clientConfig,
                    "world" => _worldConfig,
                    "gameplay" => _gameplayConfig,
                    "network" => _networkConfig,
                    "runtime" => _runtimeConfigs,
                    "all" => new
                    {
                        server = _serverConfig,
                        client = _clientConfig,
                        world = _worldConfig,
                        gameplay = _gameplayConfig,
                        network = _networkConfig,
                        runtime = _runtimeConfigs
                    },
                    _ => throw new ArgumentException($"Unknown config type: {configType}")
                };
                
                SaveConfig(outputPath, configToExport);
                Debug.Log($"[ConfigManager] Exported {configType} config to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to export config: {ex.Message}");
            }
        }
        
        public void ImportConfig(string configType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Config file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (configType.ToLower())
                {
                    case "server":
                        _serverConfig = JsonSerializer.Deserialize<ServerConfig>(json, _jsonOptions);
                        break;
                    case "client":
                        _clientConfig = JsonSerializer.Deserialize<ClientConfig>(json, _jsonOptions);
                        break;
                    case "world":
                        _worldConfig = JsonSerializer.Deserialize<WorldConfig>(json, _jsonOptions);
                        break;
                    case "gameplay":
                        _gameplayConfig = JsonSerializer.Deserialize<GameplayConfig>(json, _jsonOptions);
                        break;
                    case "network":
                        _networkConfig = JsonSerializer.Deserialize<NetworkConfig>(json, _jsonOptions);
                        break;
                    case "runtime":
                        _runtimeConfigs = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                        break;
                    default:
                        throw new ArgumentException($"Unknown config type: {configType}");
                }
                
                ValidateAllConfigs();
                Debug.Log($"[ConfigManager] Imported {configType} config from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to import config: {ex.Message}");
            }
        }
        #endregion
    }
}
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace GameCommon.Configuration
{
    /// <summary>
    /// Unified configuration manager that handles all game configuration in a centralized way
    /// Supports server, client, and world configurations with validation and default values
    /// </summary>
    public class UnifiedConfigManager
    {
        private static UnifiedConfigManager _instance;
        private static readonly object _lock = new object();
        
        public static UnifiedConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UnifiedConfigManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private ServerConfig _serverConfig;
        private ClientConfig _clientConfig;
        private WorldConfig _worldConfig;
        private GameplayConfig _gameplayConfig;
        private NetworkConfig _networkConfig;
        private Dictionary<string, object> _runtimeConfigs;
        
        private readonly Dictionary<string, string> _configPaths = new()
        {
            { "server", "config/server.json" },
            { "client", "config/client_config.json" },
            { "world", "config/world.json" },
            { "gameplay", "config/gameplay.json" },
            { "network", "config/network.json" },
            { "runtime", "config/runtime.json" }
        };
        
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        private UnifiedConfigManager()
        {
            InitializeConfigs();
        }
        
        private void InitializeConfigs()
        {
            LoadAllConfigs();
            ValidateAllConfigs();
        }
        
        #region Public Properties
        public ServerConfig ServerConfig => _serverConfig;
        public ClientConfig ClientConfig => _clientConfig;
        public WorldConfig WorldConfig => _worldConfig;
        public GameplayConfig GameplayConfig => _gameplayConfig;
        public NetworkConfig NetworkConfig => _networkConfig;
        #endregion
        
        #region Configuration Loading
        public void LoadAllConfigs()
        {
            try
            {
                _serverConfig = LoadConfig<ServerConfig>(_configPaths["server"], GetDefaultServerConfig());
                _clientConfig = LoadConfig<ClientConfig>(_configPaths["client"], GetDefaultClientConfig());
                _worldConfig = LoadConfig<WorldConfig>(_configPaths["world"], GetDefaultWorldConfig());
                _gameplayConfig = LoadConfig<GameplayConfig>(_configPaths["gameplay"], GetDefaultGameplayConfig());
                _networkConfig = LoadConfig<NetworkConfig>(_configPaths["network"], GetDefaultNetworkConfig());
                _runtimeConfigs = LoadConfig<Dictionary<string, object>>(_configPaths["runtime"], new Dictionary<string, object>());
                
                Debug.Log("[ConfigManager] All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to load configurations: {ex.Message}");
                throw;
            }
        }
        
        public T LoadConfig<T>(string path, T defaultValue = default)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[ConfigManager] Config file not found: {path}, creating default");
                    SaveConfig(path, defaultValue);
                    return defaultValue;
                }
                
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                if (config == null)
                {
                    Debug.LogWarning($"[ConfigManager] Failed to deserialize config from {path}, using default");
                    return defaultValue;
                }
                
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error loading config from {path}: {ex.Message}");
                return defaultValue;
            }
        }
        
        public void SaveConfig<T>(string path, T config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(path, json);
                
                Debug.Log($"[ConfigManager] Config saved to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Error saving config to {path}: {ex.Message}");
            }
        }
        
        public void SaveAllConfigs()
        {
            SaveConfig(_configPaths["server"], _serverConfig);
            SaveConfig(_configPaths["client"], _clientConfig);
            SaveConfig(_configPaths["world"], _worldConfig);
            SaveConfig(_configPaths["gameplay"], _gameplayConfig);
            SaveConfig(_configPaths["network"], _networkConfig);
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Configuration Validation
        public void ValidateAllConfigs()
        {
            ValidateServerConfig();
            ValidateClientConfig();
            ValidateWorldConfig();
            ValidateGameplayConfig();
            ValidateNetworkConfig();
            
            Debug.Log("[ConfigManager] All configurations validated");
        }
        
        private void ValidateServerConfig()
        {
            if (_serverConfig.Network.Port <= 0 || _serverConfig.Network.Port > 65535)
            {
                Debug.LogWarning("[ConfigManager] Invalid server port, using default 9000");
                _serverConfig.Network.Port = 9000;
            }
            
            if (_serverConfig.Network.MaxPlayers <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max players, using default 20");
                _serverConfig.Network.MaxPlayers = 20;
            }
        }
        
        private void ValidateClientConfig()
        {
            if (_clientConfig.Graphics.RenderDistance < 2 || _clientConfig.Graphics.RenderDistance > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid render distance, clamping to range [2, 32]");
                _clientConfig.Graphics.RenderDistance = Mathf.Clamp(_clientConfig.Graphics.RenderDistance, 2, 32);
            }
            
            if (_clientConfig.Audio.MasterVolume < 0 || _clientConfig.Audio.MasterVolume > 1)
            {
                Debug.LogWarning("[ConfigManager] Invalid master volume, clamping to [0, 1]");
                _clientConfig.Audio.MasterVolume = Mathf.Clamp01(_clientConfig.Audio.MasterVolume);
            }
        }
        
        private void ValidateWorldConfig()
        {
            if (_worldConfig.WorldHeight < 64 || _worldConfig.WorldHeight > 512)
            {
                Debug.LogWarning("[ConfigManager] Invalid world height, using default 256");
                _worldConfig.WorldHeight = 256;
            }
            
            if (_worldConfig.ChunkSize <= 0 || _worldConfig.ChunkSize > 32)
            {
                Debug.LogWarning("[ConfigManager] Invalid chunk size, using default 16");
                _worldConfig.ChunkSize = 16;
            }
        }
        
        private void ValidateGameplayConfig()
        {
            // Validate gameplay settings
            if (_gameplayConfig.MaxHealth <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid max health, using default 100");
                _gameplayConfig.MaxHealth = 100;
            }
        }
        
        private void ValidateNetworkConfig()
        {
            if (_networkConfig.ConnectionTimeoutMs <= 0)
            {
                Debug.LogWarning("[ConfigManager] Invalid connection timeout, using default 10000");
                _networkConfig.ConnectionTimeoutMs = 10000;
            }
        }
        #endregion
        
        #region Runtime Configuration
        public T GetRuntimeConfig<T>(string key, T defaultValue = default)
        {
            if (_runtimeConfigs.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public void SetRuntimeConfig<T>(string key, T value)
        {
            _runtimeConfigs[key] = value;
            SaveConfig(_configPaths["runtime"], _runtimeConfigs);
        }
        #endregion
        
        #region Default Configurations
        private ServerConfig GetDefaultServerConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkSettings
                {
                    Host = "0.0.0.0",
                    Port = 9000,
                    MaxPlayers = 20,
                    MaxConnectionsPerIP = 3,
                    ConnectionTimeoutSeconds = 30,
                    KeepAliveIntervalSeconds = 5,
                    PacketCompressionThreshold = 256
                },
                Database = new DatabaseSettings
                {
                    Provider = "sqlite",
                    ConnectionString = "Data Source=gameserver.db",
                    EnableAutoMigration = true,
                    CommandTimeoutSeconds = 30,
                    MaxPoolSize = 100
                },
                Performance = new PerformanceSettings
                {
                    TickRate = 20,
                    ChunkLoadThreads = 4,
                    MaxChunkLoadsPerTick = 10,
                    ChunkUnloadDelay = 30,
                    EntityUpdateDistance = 128,
                    EnableAsyncChunkGeneration = true,
                    ChunkCacheSize = 1000,
                    EnableGarbageCollection = true
                },
                Security = new SecuritySettings
                {
                    EnableWhitelist = false,
                    EnableAuthentication = true,
                    EnableEncryption = true,
                    MaxPacketSize = 2097152,
                    RateLimitPacketsPerSecond = 100,
                    EnableAntiCheat = true,
                    MaxPlayerSpeed = 10.0f,
                    MaxFlySpeed = 20.0f
                },
                Logging = new LoggingSettings
                {
                    LogLevel = "Information",
                    EnableFileLogging = true,
                    LogDirectory = "logs",
                    EnableConsoleLogging = true,
                    MaxLogFileSizeMB = 10,
                    MaxLogFiles = 10,
                    EnablePerformanceLogging = false,
                    EnableNetworkLogging = false
                }
            };
        }
        
        private ClientConfig GetDefaultClientConfig()
        {
            return new ClientConfig
            {
                Network = new ClientNetworkSettings
                {
                    ConnectionTimeoutMs = 10000,
                    ReconnectAttempts = 3,
                    ReconnectDelayMs = 5000,
                    MaxPacketSize = 1048576,
                    CompressionEnabled = true,
                    CompressionThreshold = 1024
                },
                Graphics = new GraphicsSettings
                {
                    RenderDistance = 8,
                    MaxRenderDistance = 16,
                    Fov = 75,
                    MaxFov = 110,
                    Brightness = 0.7f,
                    Gamma = 1.0f,
                    VsyncEnabled = true,
                    MaxFps = 60,
                    AntiAliasing = 2,
                    AnisotropicFiltering = true,
                    TextureQuality = "high",
                    ShadowQuality = "medium",
                    ParticleQuality = "high",
                    WaterQuality = "high"
                },
                Audio = new AudioSettings
                {
                    MasterVolume = 0.8f,
                    MusicVolume = 0.7f,
                    SoundVolume = 0.8f,
                    AmbientVolume = 0.6f,
                    VoiceChatVolume = 0.9f,
                    MaxSoundDistance = 32,
                    DopplerEnabled = true,
                    ReverbEnabled = true,
                    AudioDevice = "default"
                },
                Controls = new ControlSettings
                {
                    MouseSensitivity = 1.0f,
                    InvertMouseY = false,
                    SmoothMouse = true,
                    MouseSmoothing = 0.5f,
                    KeyBindings = new Dictionary<string, string>
                    {
                        { "forward", "W" },
                        { "backward", "S" },
                        { "left", "A" },
                        { "right", "D" },
                        { "jump", "Space" },
                        { "sneak", "LeftShift" },
                        { "sprint", "LeftControl" },
                        { "inventory", "E" },
                        { "drop", "Q" },
                        { "use", "RightClick" },
                        { "attack", "LeftClick" },
                        { "chat", "T" },
                        { "pause", "Escape" },
                        { "screenshot", "F2" }
                    }
                }
            };
        }
        
        private WorldConfig GetDefaultWorldConfig()
        {
            return new WorldConfig
            {
                WorldName = "New World",
                Seed = 0,
                GameMode = "survival",
                WorldHeight = 256,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 8,
                TerrainGeneration = new TerrainGenerationSettings
                {
                    SeaLevel = 62,
                    BedrockLevel = 5,
                    NoiseScale = 100.0f,
                    NoiseAmplitude = 50.0f,
                    Octaves = 4,
                    Persistence = 0.5f,
                    Lacunarity = 2.0f,
                    BiomeScale = 0.005f,
                    TemperatureScale = 0.003f,
                    HumidityScale = 0.004f,
                    MountainThreshold = 0.6f,
                    MountainMaxHeight = 200,
                    PlainBaseHeight = 64
                },
                Water = new WaterSettings
                {
                    GlobalWaterLevel = 62,
                    RiverCenterThreshold = 0.0125f,
                    RiverBankThreshold = 0.028f,
                    EnableOceans = true,
                    EnableRivers = true,
                    EnableLakes = true,
                    UseImprovedRivers = true,
                    UseImprovedLakes = true
                },
                Caves = new CaveSettings
                {
                    EnableCaves = true,
                    UseImprovedCaves = true,
                    CaveDensity = 0.3f,
                    CaveNoiseScale = 0.05f,
                    Threshold = 0.45f,
                    MinCaveHeight = 5,
                    MaxCaveHeight = 128,
                    HorizontalFrequency = 0.0026f,
                    VerticalFrequency = 0.018f,
                    NoiseThreshold = 0.45f
                },
                Ores = new OreSettings
                {
                    EnableOreGeneration = true,
                    Coal = new OreVeinSettings { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 },
                    Iron = new OreVeinSettings { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 },
                    Gold = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 },
                    Diamond = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 },
                    Redstone = new OreVeinSettings { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 },
                    Lapis = new OreVeinSettings { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 }
                },
                Structures = new StructureSettings
                {
                    EnableTrees = true,
                    TreeDensity = 0.05f,
                    EnableVillages = false,
                    EnableMineshafts = false,
                    EnableDungeons = true,
                    DungeonChance = 0.01f
                }
            };
        }
        
        private GameplayConfig GetDefaultGameplayConfig()
        {
            return new GameplayConfig
            {
                Difficulty = "normal",
                GameMode = "survival",
                AllowCheats = false,
                AllowFlight = false,
                KeepInventoryOnDeath = false,
                NaturalRegeneration = true,
                PvpEnabled = true,
                FireSpread = true,
                MobSpawning = true,
                DaylightCycle = true,
                WeatherCycle = true,
                MaxHealth = 100,
                Hunger = new HungerSettings
                {
                    Enabled = true,
                    DepletionRate = 0.5f,
                    StarvationDamage = 1.0f,
                    RegenerationThreshold = 80.0f
                }
            };
        }
        
        private NetworkConfig GetDefaultNetworkConfig()
        {
            return new NetworkConfig
            {
                ConnectionTimeoutMs = 10000,
                ReconnectAttempts = 3,
                ReconnectDelayMs = 5000,
                MaxPacketSize = 1048576,
                CompressionEnabled = true,
                CompressionThreshold = 1024,
                ProtocolVersion = "1.0.0",
                EnableProtobuf = true,
                RateLimitEnabled = true,
                MaxPacketsPerSecond = 20,
                MaxBytesPerSecond = 32768
            };
        }
        #endregion
        
        #region Configuration Merging
        public void MergeConfigs(string basePath)
        {
            try
            {
                var baseConfig = LoadConfig<Dictionary<string, object>>(basePath, new Dictionary<string, object>());
                
                // Merge base config into specific configs
                if (baseConfig.ContainsKey("server"))
                {
                    var serverJson = JsonSerializer.Serialize(baseConfig["server"], _jsonOptions);
                    var mergedServer = JsonSerializer.Deserialize<ServerConfig>(serverJson, _jsonOptions);
                    _serverConfig = mergedServer ?? _serverConfig;
                }
                
                if (baseConfig.ContainsKey("client"))
                {
                    var clientJson = JsonSerializer.Serialize(baseConfig["client"], _jsonOptions);
                    var mergedClient = JsonSerializer.Deserialize<ClientConfig>(clientJson, _jsonOptions);
                    _clientConfig = mergedClient ?? _clientConfig;
                }
                
                // Validate after merging
                ValidateAllConfigs();
                
                Debug.Log($"[ConfigManager] Merged configuration from {basePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to merge configs: {ex.Message}");
            }
        }
        #endregion
        
        #region Configuration Export/Import
        public void ExportConfig(string configType, string outputPath)
        {
            try
            {
                object configToExport = configType.ToLower() switch
                {
                    "server" => _serverConfig,
                    "client" => _clientConfig,
                    "world" => _worldConfig,
                    "gameplay" => _gameplayConfig,
                    "network" => _networkConfig,
                    "runtime" => _runtimeConfigs,
                    "all" => new
                    {
                        server = _serverConfig,
                        client = _clientConfig,
                        world = _worldConfig,
                        gameplay = _gameplayConfig,
                        network = _networkConfig,
                        runtime = _runtimeConfigs
                    },
                    _ => throw new ArgumentException($"Unknown config type: {configType}")
                };
                
                SaveConfig(outputPath, configToExport);
                Debug.Log($"[ConfigManager] Exported {configType} config to {outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to export config: {ex.Message}");
            }
        }
        
        public void ImportConfig(string configType, string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Config file not found: {inputPath}");
                }
                
                var json = File.ReadAllText(inputPath);
                
                switch (configType.ToLower())
                {
                    case "server":
                        _serverConfig = JsonSerializer.Deserialize<ServerConfig>(json, _jsonOptions);
                        break;
                    case "client":
                        _clientConfig = JsonSerializer.Deserialize<ClientConfig>(json, _jsonOptions);
                        break;
                    case "world":
                        _worldConfig = JsonSerializer.Deserialize<WorldConfig>(json, _jsonOptions);
                        break;
                    case "gameplay":
                        _gameplayConfig = JsonSerializer.Deserialize<GameplayConfig>(json, _jsonOptions);
                        break;
                    case "network":
                        _networkConfig = JsonSerializer.Deserialize<NetworkConfig>(json, _jsonOptions);
                        break;
                    case "runtime":
                        _runtimeConfigs = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                        break;
                    default:
                        throw new ArgumentException($"Unknown config type: {configType}");
                }
                
                ValidateAllConfigs();
                Debug.Log($"[ConfigManager] Imported {configType} config from {inputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] Failed to import config: {ex.Message}");
            }
        }
        #endregion
    }
}
