using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Client configuration data structure
    /// </summary>
    [Serializable]
    public class ClientConfigData
    {
        public ClientSettings Client = new();
        public NetworkSettings Network = new();
        public WorldSettings World = new();
        public GraphicsSettings Graphics = new();
        public AudioSettings Audio = new();
        public InputSettings Input = new();
        public UISettings UI = new();
        public PlayerSettings Player = new();
        public PerformanceSettings Performance = new();
        public DebugSettings Debug = new();
    }

    [Serializable]
    public class ClientSettings
    {
        public string Version = "1.0.0";
        public string GameTitle = "Enhanced Minecraft";
        public string LogFilePath = "logs/client.log";
        public string LogLevel = "Info";
        public bool EnableDebugMode = false;
        public bool EnableProfiler = false;
    }

    [Serializable]
    public class NetworkSettings
    {
        public string ServerAddress = "127.0.0.1";
        public int ServerPort = 8080;
        public int ConnectionTimeout = 10000;
        public int ReconnectDelay = 5000;
        public int MaxReconnectAttempts = 3;
        public int HeartbeatInterval = 30000;
        public int NetworkTickRate = 20;
        public bool EnableCompression = true;
        public bool EnableEncryption = false;
        public int MaxPacketSize = 65536;
    }

    [Serializable]
    public class WorldSettings
    {
        public string WorldName = "PlayerWorld";
        public int Seed = 0;
        public string GameMode = "survival";
        public int WorldHeight = 256;
        public int ChunkSize = 16;
        public int RenderDistance = 10;
        public int SimulationDistance = 8;
        public int MaxLoadedChunks = 1000;
        public float ChunkUpdateInterval = 0.1f;
        public bool EnableChunkCaching = true;
        public int ChunkCacheSize = 100;
        public int AutoSaveInterval = 300;
    }

    [Serializable]
    public class GraphicsSettings
    {
        public float RenderScale = 1.0f;
        public string ShadowQuality = "Medium";
        public int ViewDistance = 10;
        public string ParticleQuality = "Medium";
        public string TerrainQuality = "High";
        public string WaterQuality = "High";
        public string FoliageQuality = "Medium";
        public bool EnableVSync = true;
        public int TargetFrameRate = 60;
        public int MaxFrameRate = 120;
        public bool EnableAntiAliasing = true;
        public int AntiAliasingQuality = 4;
    }

    [Serializable]
    public class AudioSettings
    {
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 0.9f;
        public float AmbientVolume = 0.7f;
        public bool EnableAudio = true;
        public string AudioDevice = "Default";
    }

    [Serializable]
    public class InputSettings
    {
        public float MouseSensitivity = 1.0f;
        public bool InvertMouseY = false;
        public bool EnableAutoJump = false;
        public Dictionary<string, string> KeyBindings = new()
        {
            ["MoveForward"] = "W",
            ["MoveBackward"] = "S",
            ["MoveLeft"] = "A",
            ["MoveRight"] = "D",
            ["Jump"] = "Space",
            ["Sprint"] = "LeftShift",
            ["Sneak"] = "LeftControl",
            ["Inventory"] = "E",
            ["Chat"] = "T",
            ["Attack"] = "Mouse0",
            ["Use"] = "Mouse1",
            ["Drop"] = "Q",
            ["Pause"] = "Escape"
        };
    }

    [Serializable]
    public class UISettings
    {
        public float UIScale = 1.0f;
        public bool ShowFPS = false;
        public bool ShowCoordinates = true;
        public bool ShowDebugInfo = false;
        public int ChatHistorySize = 100;
        public bool EnableTooltips = true;
        public float TooltipDelay = 0.5f;
        public float FontScale = 1.0f;
        public bool EnableNotifications = true;
        public float NotificationDuration = 3.0f;
    }

    [Serializable]
    public class PlayerSettings
    {
        public Vector3 DefaultSpawnPoint = new(0, 64, 0);
        public float ReachDistance = 5.0f;
        public float BreakSpeed = 1.0f;
        public bool CreativeFlight = true;
        public bool AutoRespawn = true;
        public bool KeepInventoryOnDeath = false;
    }

    [Serializable]
    public class PerformanceSettings
    {
        public bool EnableMultithreading = true;
        public int WorkerThreadCount = 0;
        public string GarbageCollectionMode = "Incremental";
        public bool EnableObjectPooling = true;
        public int PoolInitialSize = 100;
        public int PoolMaxSize = 1000;
        public bool EnableLOD = true;
        public int LODDistance = 50;
        public bool EnableOcclusionCulling = true;
    }

    [Serializable]
    public class DebugSettings
    {
        public bool EnableDebugLogs = false;
        public bool EnableNetworkLogs = false;
        public bool EnableChunkDebug = false;
        public bool EnablePerformanceMetrics = false;
        public bool LogToFile = true;
        public int MaxLogFileSize = 10485760;
        public int MaxLogFiles = 5;
    }

    /// <summary>
    /// Client configuration manager that loads and provides access to client settings
    /// </summary>
    public static class ClientConfig
    {
        private static ClientConfigData _config;
        private static readonly string ConfigPath = Path.Combine(Application.streamingAssetsPath, "client-config.json");

        /// <summary>
        /// Gets the loaded client configuration
        /// </summary>
        public static ClientConfigData Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }
                return _config;
            }
        }

        /// <summary>
        /// Loads the client configuration from JSON file
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string jsonContent = File.ReadAllText(ConfigPath);
                    _config = JsonUtility.FromJson<ClientConfigData>(jsonContent);
                    Debug.Log($"[ClientConfig] Loaded configuration from {ConfigPath}");
                }
                else
                {
                    Debug.LogWarning($"[ClientConfig] Configuration file not found at {ConfigPath}, using defaults");
                    _config = new ClientConfigData();
                    SaveConfig(); // Save default configuration
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientConfig] Failed to load configuration: {ex.Message}");
                _config = new ClientConfigData();
            }
        }

        /// <summary>
        /// Saves the current configuration to JSON file
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(_config, true);
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, jsonContent);
                Debug.Log($"[ClientConfig] Saved configuration to {ConfigPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientConfig] Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Reloads the configuration from file
        /// </summary>
        public static void ReloadConfig()
        {
            _config = null;
            LoadConfig();
        }

        /// <summary>
        /// Applies graphics settings to Unity quality settings
        /// </summary>
        public static void ApplyGraphicsSettings()
        {
            var graphics = Config.Graphics;
            
            QualitySettings.vSyncCount = graphics.EnableVSync ? 1 : 0;
            Application.targetFrameRate = graphics.TargetFrameRate;
            
            // Set shadow quality
            graphics.ShadowQuality = graphics.ShadowQuality.ToLower() switch
            {
                "low" => Quality.ShadowQuality.Low,
                "medium" => Quality.ShadowQuality.Medium,
                "high" => Quality.ShadowQuality.High,
                "veryhigh" => Quality.ShadowQuality.VeryHigh,
                _ => Quality.ShadowQuality.Medium
            };
            
            // Set anti-aliasing
            QualitySettings.antiAliasing = graphics.EnableAntiAliasing ? graphics.AntiAliasingQuality : 0;
            
            Debug.Log("[ClientConfig] Applied graphics settings");
        }

        /// <summary>
        /// Applies performance settings
        /// </summary>
        public static void ApplyPerformanceSettings()
        {
            var performance = Config.Performance;
            
            // Set LOD bias
            QualitySettings.lodBias = performance.EnableLOD ? 1.0f : 0.0f;
            
            // Enable/disable occlusion culling
            QualitySettings.occlusionCulling = performance.EnableOcclusionCulling;
            
            Debug.Log("[ClientConfig] Applied performance settings");
        }
    }
}using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Client configuration data structure
    /// </summary>
    [Serializable]
    public class ClientConfigData
    {
        public ClientSettings Client = new();
        public NetworkSettings Network = new();
        public WorldSettings World = new();
        public GraphicsSettings Graphics = new();
        public AudioSettings Audio = new();
        public InputSettings Input = new();
        public UISettings UI = new();
        public PlayerSettings Player = new();
        public PerformanceSettings Performance = new();
        public DebugSettings Debug = new();
    }

    [Serializable]
    public class ClientSettings
    {
        public string Version = "1.0.0";
        public string GameTitle = "Enhanced Minecraft";
        public string LogFilePath = "logs/client.log";
        public string LogLevel = "Info";
        public bool EnableDebugMode = false;
        public bool EnableProfiler = false;
    }

    [Serializable]
    public class NetworkSettings
    {
        public string ServerAddress = "127.0.0.1";
        public int ServerPort = 8080;
        public int ConnectionTimeout = 10000;
        public int ReconnectDelay = 5000;
        public int MaxReconnectAttempts = 3;
        public int HeartbeatInterval = 30000;
        public int NetworkTickRate = 20;
        public bool EnableCompression = true;
        public bool EnableEncryption = false;
        public int MaxPacketSize = 65536;
    }

    [Serializable]
    public class WorldSettings
    {
        public string WorldName = "PlayerWorld";
        public int Seed = 0;
        public string GameMode = "survival";
        public int WorldHeight = 256;
        public int ChunkSize = 16;
        public int RenderDistance = 10;
        public int SimulationDistance = 8;
        public int MaxLoadedChunks = 1000;
        public float ChunkUpdateInterval = 0.1f;
        public bool EnableChunkCaching = true;
        public int ChunkCacheSize = 100;
        public int AutoSaveInterval = 300;
    }

    [Serializable]
    public class GraphicsSettings
    {
        public float RenderScale = 1.0f;
        public string ShadowQuality = "Medium";
        public int ViewDistance = 10;
        public string ParticleQuality = "Medium";
        public string TerrainQuality = "High";
        public string WaterQuality = "High";
        public string FoliageQuality = "Medium";
        public bool EnableVSync = true;
        public int TargetFrameRate = 60;
        public int MaxFrameRate = 120;
        public bool EnableAntiAliasing = true;
        public int AntiAliasingQuality = 4;
    }

    [Serializable]
    public class AudioSettings
    {
        public float MasterVolume = 1.0f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 0.9f;
        public float AmbientVolume = 0.7f;
        public bool EnableAudio = true;
        public string AudioDevice = "Default";
    }

    [Serializable]
    public class InputSettings
    {
        public float MouseSensitivity = 1.0f;
        public bool InvertMouseY = false;
        public bool EnableAutoJump = false;
        public Dictionary<string, string> KeyBindings = new()
        {
            ["MoveForward"] = "W",
            ["MoveBackward"] = "S",
            ["MoveLeft"] = "A",
            ["MoveRight"] = "D",
            ["Jump"] = "Space",
            ["Sprint"] = "LeftShift",
            ["Sneak"] = "LeftControl",
            ["Inventory"] = "E",
            ["Chat"] = "T",
            ["Attack"] = "Mouse0",
            ["Use"] = "Mouse1",
            ["Drop"] = "Q",
            ["Pause"] = "Escape"
        };
    }

    [Serializable]
    public class UISettings
    {
        public float UIScale = 1.0f;
        public bool ShowFPS = false;
        public bool ShowCoordinates = true;
        public bool ShowDebugInfo = false;
        public int ChatHistorySize = 100;
        public bool EnableTooltips = true;
        public float TooltipDelay = 0.5f;
        public float FontScale = 1.0f;
        public bool EnableNotifications = true;
        public float NotificationDuration = 3.0f;
    }

    [Serializable]
    public class PlayerSettings
    {
        public Vector3 DefaultSpawnPoint = new(0, 64, 0);
        public float ReachDistance = 5.0f;
        public float BreakSpeed = 1.0f;
        public bool CreativeFlight = true;
        public bool AutoRespawn = true;
        public bool KeepInventoryOnDeath = false;
    }

    [Serializable]
    public class PerformanceSettings
    {
        public bool EnableMultithreading = true;
        public int WorkerThreadCount = 0;
        public string GarbageCollectionMode = "Incremental";
        public bool EnableObjectPooling = true;
        public int PoolInitialSize = 100;
        public int PoolMaxSize = 1000;
        public bool EnableLOD = true;
        public int LODDistance = 50;
        public bool EnableOcclusionCulling = true;
    }

    [Serializable]
    public class DebugSettings
    {
        public bool EnableDebugLogs = false;
        public bool EnableNetworkLogs = false;
        public bool EnableChunkDebug = false;
        public bool EnablePerformanceMetrics = false;
        public bool LogToFile = true;
        public int MaxLogFileSize = 10485760;
        public int MaxLogFiles = 5;
    }

    /// <summary>
    /// Client configuration manager that loads and provides access to client settings
    /// </summary>
    public static class ClientConfig
    {
        private static ClientConfigData _config;
        private static readonly string ConfigPath = Path.Combine(Application.streamingAssetsPath, "client-config.json");

        /// <summary>
        /// Gets the loaded client configuration
        /// </summary>
        public static ClientConfigData Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }
                return _config;
            }
        }

        /// <summary>
        /// Loads the client configuration from JSON file
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string jsonContent = File.ReadAllText(ConfigPath);
                    _config = JsonUtility.FromJson<ClientConfigData>(jsonContent);
                    Debug.Log($"[ClientConfig] Loaded configuration from {ConfigPath}");
                }
                else
                {
                    Debug.LogWarning($"[ClientConfig] Configuration file not found at {ConfigPath}, using defaults");
                    _config = new ClientConfigData();
                    SaveConfig(); // Save default configuration
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientConfig] Failed to load configuration: {ex.Message}");
                _config = new ClientConfigData();
            }
        }

        /// <summary>
        /// Saves the current configuration to JSON file
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(_config, true);
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, jsonContent);
                Debug.Log($"[ClientConfig] Saved configuration to {ConfigPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ClientConfig] Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Reloads the configuration from file
        /// </summary>
        public static void ReloadConfig()
        {
            _config = null;
            LoadConfig();
        }

        /// <summary>
        /// Applies graphics settings to Unity quality settings
        /// </summary>
        public static void ApplyGraphicsSettings()
        {
            var graphics = Config.Graphics;
            
            QualitySettings.vSyncCount = graphics.EnableVSync ? 1 : 0;
            Application.targetFrameRate = graphics.TargetFrameRate;
            
            // Set shadow quality
            graphics.ShadowQuality = graphics.ShadowQuality.ToLower() switch
            {
                "low" => Quality.ShadowQuality.Low,
                "medium" => Quality.ShadowQuality.Medium,
                "high" => Quality.ShadowQuality.High,
                "veryhigh" => Quality.ShadowQuality.VeryHigh,
                _ => Quality.ShadowQuality.Medium
            };
            
            // Set anti-aliasing
            QualitySettings.antiAliasing = graphics.EnableAntiAliasing ? graphics.AntiAliasingQuality : 0;
            
            Debug.Log("[ClientConfig] Applied graphics settings");
        }

        /// <summary>
        /// Applies performance settings
        /// </summary>
        public static void ApplyPerformanceSettings()
        {
            var performance = Config.Performance;
            
            // Set LOD bias
            QualitySettings.lodBias = performance.EnableLOD ? 1.0f : 0.0f;
            
            // Enable/disable occlusion culling
            QualitySettings.occlusionCulling = performance.EnableOcclusionCulling;
            
            Debug.Log("[ClientConfig] Applied performance settings");
        }
    }
}
}
