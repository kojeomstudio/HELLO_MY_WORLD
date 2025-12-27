using System;
using System.Collections.Generic;

namespace GameCommon.Configuration
{
    // Server configuration model
    public class ServerConfig
    {
        public NetworkSettings Network { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class NetworkSettings
    {
        public string Host { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 9000;
        public int MaxPlayers { get; set; } = 20;
        public int MaxConnectionsPerIP { get; set; } = 3;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int KeepAliveIntervalSeconds { get; set; } = 5;
        public int PacketCompressionThreshold { get; set; } = 256;
    }

    public class DatabaseSettings
    {
        public string Provider { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=gameserver.db";
        public bool EnableAutoMigration { get; set; } = true;
        public int CommandTimeoutSeconds { get; set; } = 30;
        public int MaxPoolSize { get; set; } = 100;
    }

    public class PerformanceSettings
    {
        public int TickRate { get; set; } = 20;
        public int ChunkLoadThreads { get; set; } = 4;
        public int MaxChunkLoadsPerTick { get; set; } = 10;
        public int ChunkUnloadDelay { get; set; } = 30;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableAsyncChunkGeneration { get; set; } = true;
        public int ChunkCacheSize { get; set; } = 1000;
        public bool EnableGarbageCollection { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool EnableWhitelist { get; set; } = false;
        public bool EnableAuthentication { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
        public int MaxPacketSize { get; set; } = 2097152;
        public int RateLimitPacketsPerSecond { get; set; } = 100;
        public bool EnableAntiCheat { get; set; } = true;
        public float MaxPlayerSpeed { get; set; } = 10.0f;
        public float MaxFlySpeed { get; set; } = 20.0f;
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public bool EnableConsoleLogging { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnablePerformanceLogging { get; set; } = false;
        public bool EnableNetworkLogging { get; set; } = false;
    }

    // Client configuration model
    public class ClientConfig
    {
        public ClientNetworkSettings Network { get; set; } = new();
        public GraphicsSettings Graphics { get; set; } = new();
        public AudioSettings Audio { get; set; } = new();
        public ControlSettings Controls { get; set; } = new();
    }

    public class ClientNetworkSettings
    {
        public int ConnectionTimeoutMs { get; set; } = 10000;
        public int ReconnectAttempts { get; set; } = 3;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxPacketSize { get; set; } = 1048576;
        public bool CompressionEnabled { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
    }

    public class GraphicsSettings
    {
        public int RenderDistance { get; set; } = 8;
        public int MaxRenderDistance { get; set; } = 16;
        public int Fov { get; set; } = 75;
        public int MaxFov { get; set; } = 110;
        public float Brightness { get; set; } = 0.7f;
        public float Gamma { get; set; } = 1.0f;
        public bool VsyncEnabled { get; set; } = true;
        public int MaxFps { get; set; } = 60;
        public int AntiAliasing { get; set; } = 2;
        public bool AnisotropicFiltering { get; set; } = true;
        public string TextureQuality { get; set; } = "high";
        public string ShadowQuality { get; set; } = "medium";
        public string ParticleQuality { get; set; } = "high";
        public string WaterQuality { get; set; } = "high";
    }

    public class AudioSettings
    {
        public float MasterVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SoundVolume { get; set; } = 0.8f;
        public float AmbientVolume { get; set; } = 0.6f;
        public float VoiceChatVolume { get; set; } = 0.9f;
        public int MaxSoundDistance { get; set; } = 32;
        public bool DopplerEnabled { get; set; } = true;
        public bool ReverbEnabled { get; set; } = true;
        public string AudioDevice { get; set; } = "default";
    }

    public class ControlSettings
    {
        public float MouseSensitivity { get; set; } = 1.0f;
        public bool InvertMouseY { get; set; } = false;
        public bool SmoothMouse { get; set; } = true;
        public float MouseSmoothing { get; set; } = 0.5f;
        public Dictionary<string, string> KeyBindings { get; set; } = new()
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
        };
    }

    // World configuration model
    public class WorldConfig
    {
        public string WorldName { get; set; } = "New World";
        public int Seed { get; set; } = 0;
        public string GameMode { get; set; } = "survival";
        public int WorldHeight { get; set; } = 256;
        public int ChunkSize { get; set; } = 16;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;
        public TerrainGenerationSettings TerrainGeneration { get; set; } = new();
        public WaterSettings Water { get; set; } = new();
        public CaveSettings Caves { get; set; } = new();
        public OreSettings Ores { get; set; } = new();
        public StructureSettings Structures { get; set; } = new();
        public List<BiomeData> Biomes { get; set; } = new();
    }

    public class TerrainGenerationSettings
    {
        public int SeaLevel { get; set; } = 62;
        public int BedrockLevel { get; set; } = 5;
        public float NoiseScale { get; set; } = 100.0f;
        public float NoiseAmplitude { get; set; } = 50.0f;
        public int Octaves { get; set; } = 4;
        public float Persistence { get; set; } = 0.5f;
        public float Lacunarity { get; set; } = 2.0f;
        public float BiomeScale { get; set; } = 0.005f;
        public float TemperatureScale { get; set; } = 0.003f;
        public float HumidityScale { get; set; } = 0.004f;
        public float MountainThreshold { get; set; } = 0.6f;
        public int MountainMaxHeight { get; set; } = 200;
        public int PlainBaseHeight { get; set; } = 64;
    }

    public class WaterSettings
    {
        public int GlobalWaterLevel { get; set; } = 62;
        public float RiverCenterThreshold { get; set; } = 0.0125f;
        public float RiverBankThreshold { get; set; } = 0.028f;
        public bool EnableOceans { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool UseImprovedRivers { get; set; } = true;
        public bool UseImprovedLakes { get; set; } = true;
    }

    public class CaveSettings
    {
        public bool EnableCaves { get; set; } = true;
        public bool UseImprovedCaves { get; set; } = true;
        public float CaveDensity { get; set; } = 0.3f;
        public float CaveNoiseScale { get; set; } = 0.05f;
        public float Threshold { get; set; } = 0.45f;
        public int MinCaveHeight { get; set; } = 5;
        public int MaxCaveHeight { get; set; } = 128;
        public float HorizontalFrequency { get; set; } = 0.0026f;
        public float VerticalFrequency { get; set; } = 0.018f;
        public float NoiseThreshold { get; set; } = 0.45f;
    }

    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; } = true;
        public OreVeinSettings Coal { get; set; } = new() { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 };
        public OreVeinSettings Iron { get; set; } = new() { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 };
        public OreVeinSettings Gold { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 };
        public OreVeinSettings Diamond { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 };
        public OreVeinSettings Redstone { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 };
        public OreVeinSettings Lapis { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 };
    }

    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }

    public class StructureSettings
    {
        public bool EnableTrees { get; set; } = true;
        public float TreeDensity { get; set; } = 0.05f;
        public bool EnableVillages { get; set; } = false;
        public bool EnableMineshafts { get; set; } = false;
        public bool EnableDungeons { get; set; } = true;
        public float DungeonChance { get; set; } = 0.01f;
    }

    // Gameplay configuration model
    public class GameplayConfig
    {
        public string Difficulty { get; set; } = "normal";
        public string GameMode { get; set; } = "survival";
        public bool AllowCheats { get; set; } = false;
        public bool AllowFlight { get; set; } = false;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool NaturalRegeneration { get; set; } = true;
        public bool PvpEnabled { get; set; } = true;
        public bool FireSpread { get; set; } = true;
        public bool MobSpawning { get; set; } = true;
        public bool DaylightCycle { get; set; } = true;
        public bool WeatherCycle { get; set; } = true;
        public int MaxHealth { get; set; } = 100;
        public HungerSettings Hunger { get; set; } = new();
    }

    public class HungerSettings
    {
        public bool Enabled { get; set; } = true;
        public float DepletionRate { get; set; } = 0.5f;
        public float StarvationDamage { get; set; } = 1.0f;
        public float RegenerationThreshold { get; set; } = 80.0f;
    }

    // Network configuration model
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; } = 10000;
        public int ReconnectAttempts { get; set; } = 3;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxPacketSize { get; set; } = 1048576;
        public bool CompressionEnabled { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
        public string ProtocolVersion { get; set; } = "1.0.0";
        public bool EnableProtobuf { get; set; } = true;
        public bool RateLimitEnabled { get; set; } = true;
        public int MaxPacketsPerSecond { get; set; } = 20;
        public int MaxBytesPerSecond { get; set; } = 32768;
    }

    // Biome data model (shared with DataDriven namespace)
    public class BiomeData
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Rainfall { get; set; }
        public string[] TopBlocks { get; set; } = Array.Empty<string>();
        public string[] FillBlocks { get; set; } = Array.Empty<string>();
        public string[] UnderwaterBlocks { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> Features { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
using System.Collections.Generic;

namespace GameCommon.Configuration
{
    // Server configuration model
    public class ServerConfig
    {
        public NetworkSettings Network { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class NetworkSettings
    {
        public string Host { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 9000;
        public int MaxPlayers { get; set; } = 20;
        public int MaxConnectionsPerIP { get; set; } = 3;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int KeepAliveIntervalSeconds { get; set; } = 5;
        public int PacketCompressionThreshold { get; set; } = 256;
    }

    public class DatabaseSettings
    {
        public string Provider { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=gameserver.db";
        public bool EnableAutoMigration { get; set; } = true;
        public int CommandTimeoutSeconds { get; set; } = 30;
        public int MaxPoolSize { get; set; } = 100;
    }

    public class PerformanceSettings
    {
        public int TickRate { get; set; } = 20;
        public int ChunkLoadThreads { get; set; } = 4;
        public int MaxChunkLoadsPerTick { get; set; } = 10;
        public int ChunkUnloadDelay { get; set; } = 30;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableAsyncChunkGeneration { get; set; } = true;
        public int ChunkCacheSize { get; set; } = 1000;
        public bool EnableGarbageCollection { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool EnableWhitelist { get; set; } = false;
        public bool EnableAuthentication { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
        public int MaxPacketSize { get; set; } = 2097152;
        public int RateLimitPacketsPerSecond { get; set; } = 100;
        public bool EnableAntiCheat { get; set; } = true;
        public float MaxPlayerSpeed { get; set; } = 10.0f;
        public float MaxFlySpeed { get; set; } = 20.0f;
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public bool EnableConsoleLogging { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnablePerformanceLogging { get; set; } = false;
        public bool EnableNetworkLogging { get; set; } = false;
    }

    // Client configuration model
    public class ClientConfig
    {
        public ClientNetworkSettings Network { get; set; } = new();
        public GraphicsSettings Graphics { get; set; } = new();
        public AudioSettings Audio { get; set; } = new();
        public ControlSettings Controls { get; set; } = new();
    }

    public class ClientNetworkSettings
    {
        public int ConnectionTimeoutMs { get; set; } = 10000;
        public int ReconnectAttempts { get; set; } = 3;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxPacketSize { get; set; } = 1048576;
        public bool CompressionEnabled { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
    }

    public class GraphicsSettings
    {
        public int RenderDistance { get; set; } = 8;
        public int MaxRenderDistance { get; set; } = 16;
        public int Fov { get; set; } = 75;
        public int MaxFov { get; set; } = 110;
        public float Brightness { get; set; } = 0.7f;
        public float Gamma { get; set; } = 1.0f;
        public bool VsyncEnabled { get; set; } = true;
        public int MaxFps { get; set; } = 60;
        public int AntiAliasing { get; set; } = 2;
        public bool AnisotropicFiltering { get; set; } = true;
        public string TextureQuality { get; set; } = "high";
        public string ShadowQuality { get; set; } = "medium";
        public string ParticleQuality { get; set; } = "high";
        public string WaterQuality { get; set; } = "high";
    }

    public class AudioSettings
    {
        public float MasterVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SoundVolume { get; set; } = 0.8f;
        public float AmbientVolume { get; set; } = 0.6f;
        public float VoiceChatVolume { get; set; } = 0.9f;
        public int MaxSoundDistance { get; set; } = 32;
        public bool DopplerEnabled { get; set; } = true;
        public bool ReverbEnabled { get; set; } = true;
        public string AudioDevice { get; set; } = "default";
    }

    public class ControlSettings
    {
        public float MouseSensitivity { get; set; } = 1.0f;
        public bool InvertMouseY { get; set; } = false;
        public bool SmoothMouse { get; set; } = true;
        public float MouseSmoothing { get; set; } = 0.5f;
        public Dictionary<string, string> KeyBindings { get; set; } = new()
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
        };
    }

    // World configuration model
    public class WorldConfig
    {
        public string WorldName { get; set; } = "New World";
        public int Seed { get; set; } = 0;
        public string GameMode { get; set; } = "survival";
        public int WorldHeight { get; set; } = 256;
        public int ChunkSize { get; set; } = 16;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;
        public TerrainGenerationSettings TerrainGeneration { get; set; } = new();
        public WaterSettings Water { get; set; } = new();
        public CaveSettings Caves { get; set; } = new();
        public OreSettings Ores { get; set; } = new();
        public StructureSettings Structures { get; set; } = new();
        public List<BiomeData> Biomes { get; set; } = new();
    }

    public class TerrainGenerationSettings
    {
        public int SeaLevel { get; set; } = 62;
        public int BedrockLevel { get; set; } = 5;
        public float NoiseScale { get; set; } = 100.0f;
        public float NoiseAmplitude { get; set; } = 50.0f;
        public int Octaves { get; set; } = 4;
        public float Persistence { get; set; } = 0.5f;
        public float Lacunarity { get; set; } = 2.0f;
        public float BiomeScale { get; set; } = 0.005f;
        public float TemperatureScale { get; set; } = 0.003f;
        public float HumidityScale { get; set; } = 0.004f;
        public float MountainThreshold { get; set; } = 0.6f;
        public int MountainMaxHeight { get; set; } = 200;
        public int PlainBaseHeight { get; set; } = 64;
    }

    public class WaterSettings
    {
        public int GlobalWaterLevel { get; set; } = 62;
        public float RiverCenterThreshold { get; set; } = 0.0125f;
        public float RiverBankThreshold { get; set; } = 0.028f;
        public bool EnableOceans { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool UseImprovedRivers { get; set; } = true;
        public bool UseImprovedLakes { get; set; } = true;
    }

    public class CaveSettings
    {
        public bool EnableCaves { get; set; } = true;
        public bool UseImprovedCaves { get; set; } = true;
        public float CaveDensity { get; set; } = 0.3f;
        public float CaveNoiseScale { get; set; } = 0.05f;
        public float Threshold { get; set; } = 0.45f;
        public int MinCaveHeight { get; set; } = 5;
        public int MaxCaveHeight { get; set; } = 128;
        public float HorizontalFrequency { get; set; } = 0.0026f;
        public float VerticalFrequency { get; set; } = 0.018f;
        public float NoiseThreshold { get; set; } = 0.45f;
    }

    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; } = true;
        public OreVeinSettings Coal { get; set; } = new() { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 };
        public OreVeinSettings Iron { get; set; } = new() { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 };
        public OreVeinSettings Gold { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 };
        public OreVeinSettings Diamond { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 };
        public OreVeinSettings Redstone { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 };
        public OreVeinSettings Lapis { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 };
    }

    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }

    public class StructureSettings
    {
        public bool EnableTrees { get; set; } = true;
        public float TreeDensity { get; set; } = 0.05f;
        public bool EnableVillages { get; set; } = false;
        public bool EnableMineshafts { get; set; } = false;
        public bool EnableDungeons { get; set; } = true;
        public float DungeonChance { get; set; } = 0.01f;
    }

    // Gameplay configuration model
    public class GameplayConfig
    {
        public string Difficulty { get; set; } = "normal";
        public string GameMode { get; set; } = "survival";
        public bool AllowCheats { get; set; } = false;
        public bool AllowFlight { get; set; } = false;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool NaturalRegeneration { get; set; } = true;
        public bool PvpEnabled { get; set; } = true;
        public bool FireSpread { get; set; } = true;
        public bool MobSpawning { get; set; } = true;
        public bool DaylightCycle { get; set; } = true;
        public bool WeatherCycle { get; set; } = true;
        public int MaxHealth { get; set; } = 100;
        public HungerSettings Hunger { get; set; } = new();
    }

    public class HungerSettings
    {
        public bool Enabled { get; set; } = true;
        public float DepletionRate { get; set; } = 0.5f;
        public float StarvationDamage { get; set; } = 1.0f;
        public float RegenerationThreshold { get; set; } = 80.0f;
    }

    // Network configuration model
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; } = 10000;
        public int ReconnectAttempts { get; set; } = 3;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxPacketSize { get; set; } = 1048576;
        public bool CompressionEnabled { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
        public string ProtocolVersion { get; set; } = "1.0.0";
        public bool EnableProtobuf { get; set; } = true;
        public bool RateLimitEnabled { get; set; } = true;
        public int MaxPacketsPerSecond { get; set; } = 20;
        public int MaxBytesPerSecond { get; set; } = 32768;
    }

    // Biome data model (shared with DataDriven namespace)
    public class BiomeData
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Rainfall { get; set; }
        public string[] TopBlocks { get; set; } = Array.Empty<string>();
        public string[] FillBlocks { get; set; } = Array.Empty<string>();
        public string[] UnderwaterBlocks { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> Features { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
using System.Collections.Generic;

namespace GameCommon.Configuration
{
    // Server configuration model
    public class ServerConfig
    {
        public NetworkSettings Network { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class NetworkSettings
    {
        public string Host { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 9000;
        public int MaxPlayers { get; set; } = 20;
        public int MaxConnectionsPerIP { get; set; } = 3;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int KeepAliveIntervalSeconds { get; set; } = 5;
        public int PacketCompressionThreshold { get; set; } = 256;
    }

    public class DatabaseSettings
    {
        public string Provider { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=gameserver.db";
        public bool EnableAutoMigration { get; set; } = true;
        public int CommandTimeoutSeconds { get; set; } = 30;
        public int MaxPoolSize { get; set; } = 100;
    }

    public class PerformanceSettings
    {
        public int TickRate { get; set; } = 20;
        public int ChunkLoadThreads { get; set; } = 4;
        public int MaxChunkLoadsPerTick { get; set; } = 10;
        public int ChunkUnloadDelay { get; set; } = 30;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableAsyncChunkGeneration { get; set; } = true;
        public int ChunkCacheSize { get; set; } = 1000;
        public bool EnableGarbageCollection { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool EnableWhitelist { get; set; } = false;
        public bool EnableAuthentication { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
        public int MaxPacketSize { get; set; } = 2097152;
        public int RateLimitPacketsPerSecond { get; set; } = 100;
        public bool EnableAntiCheat { get; set; } = true;
        public float MaxPlayerSpeed { get; set; } = 10.0f;
        public float MaxFlySpeed { get; set; } = 20.0f;
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public bool EnableConsoleLogging { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnablePerformanceLogging { get; set; } = false;
        public bool EnableNetworkLogging { get; set; } = false;
    }

    // Client configuration model
    public class ClientConfig
    {
        public ClientNetworkSettings Network { get; set; } = new();
        public GraphicsSettings Graphics { get; set; } = new();
        public AudioSettings Audio { get; set; } = new();
        public ControlSettings Controls { get; set; } = new();
    }

    public class ClientNetworkSettings
    {
        public int ConnectionTimeoutMs { get; set; } = 10000;
        public int ReconnectAttempts { get; set; } = 3;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxPacketSize { get; set; } = 1048576;
        public bool CompressionEnabled { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
    }

    public class GraphicsSettings
    {
        public int RenderDistance { get; set; } = 8;
        public int MaxRenderDistance { get; set; } = 16;
        public int Fov { get; set; } = 75;
        public int MaxFov { get; set; } = 110;
        public float Brightness { get; set; } = 0.7f;
        public float Gamma { get; set; } = 1.0f;
        public bool VsyncEnabled { get; set; } = true;
        public int MaxFps { get; set; } = 60;
        public int AntiAliasing { get; set; } = 2;
        public bool AnisotropicFiltering { get; set; } = true;
        public string TextureQuality { get; set; } = "high";
        public string ShadowQuality { get; set; } = "medium";
        public string ParticleQuality { get; set; } = "high";
        public string WaterQuality { get; set; } = "high";
    }

    public class AudioSettings
    {
        public float MasterVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SoundVolume { get; set; } = 0.8f;
        public float AmbientVolume { get; set; } = 0.6f;
        public float VoiceChatVolume { get; set; } = 0.9f;
        public int MaxSoundDistance { get; set; } = 32;
        public bool DopplerEnabled { get; set; } = true;
        public bool ReverbEnabled { get; set; } = true;
        public string AudioDevice { get; set; } = "default";
    }

    public class ControlSettings
    {
        public float MouseSensitivity { get; set; } = 1.0f;
        public bool InvertMouseY { get; set; } = false;
        public bool SmoothMouse { get; set; } = true;
        public float MouseSmoothing { get; set; } = 0.5f;
        public Dictionary<string, string> KeyBindings { get; set; } = new()
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
        };
    }

    // World configuration model
    public class WorldConfig
    {
        public string WorldName { get; set; } = "New World";
        public int Seed { get; set; } = 0;
        public string GameMode { get; set; } = "survival";
        public int WorldHeight { get; set; } = 256;
        public int ChunkSize { get; set; } = 16;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;
        public TerrainGenerationSettings TerrainGeneration { get; set; } = new();
        public WaterSettings Water { get; set; } = new();
        public CaveSettings Caves { get; set; } = new();
        public OreSettings Ores { get; set; } = new();
        public StructureSettings Structures { get; set; } = new();
        public List<BiomeData> Biomes { get; set; } = new();
    }

    public class TerrainGenerationSettings
    {
        public int SeaLevel { get; set; } = 62;
        public int BedrockLevel { get; set; } = 5;
        public float NoiseScale { get; set; } = 100.0f;
        public float NoiseAmplitude { get; set; } = 50.0f;
        public int Octaves { get; set; } = 4;
        public float Persistence { get; set; } = 0.5f;
        public float Lacunarity { get; set; } = 2.0f;
        public float BiomeScale { get; set; } = 0.005f;
        public float TemperatureScale { get; set; } = 0.003f;
        public float HumidityScale { get; set; } = 0.004f;
        public float MountainThreshold { get; set; } = 0.6f;
        public int MountainMaxHeight { get; set; } = 200;
        public int PlainBaseHeight { get; set; } = 64;
    }

    public class WaterSettings
    {
        public int GlobalWaterLevel { get; set; } = 62;
        public float RiverCenterThreshold { get; set; } = 0.0125f;
        public float RiverBankThreshold { get; set; } = 0.028f;
        public bool EnableOceans { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool UseImprovedRivers { get; set; } = true;
        public bool UseImprovedLakes { get; set; } = true;
    }

    public class CaveSettings
    {
        public bool EnableCaves { get; set; } = true;
        public bool UseImprovedCaves { get; set; } = true;
        public float CaveDensity { get; set; } = 0.3f;
        public float CaveNoiseScale { get; set; } = 0.05f;
        public float Threshold { get; set; } = 0.45f;
        public int MinCaveHeight { get; set; } = 5;
        public int MaxCaveHeight { get; set; } = 128;
        public float HorizontalFrequency { get; set; } = 0.0026f;
        public float VerticalFrequency { get; set; } = 0.018f;
        public float NoiseThreshold { get; set; } = 0.45f;
    }

    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; } = true;
        public OreVeinSettings Coal { get; set; } = new() { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 };
        public OreVeinSettings Iron { get; set; } = new() { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 };
        public OreVeinSettings Gold { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 };
        public OreVeinSettings Diamond { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 };
        public OreVeinSettings Redstone { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 };
        public OreVeinSettings Lapis { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 };
    }

    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }

    public class StructureSettings
    {
        public bool EnableTrees { get; set; } = true;
        public float TreeDensity { get; set; } = 0.05f;
        public bool EnableVillages { get; set; } = false;
        public bool EnableMineshafts { get; set; } = false;
        public bool EnableDungeons { get; set; } = true;
        public float DungeonChance { get; set; } = 0.01f;
    }

    // Gameplay configuration model
    public class GameplayConfig
    {
        public string Difficulty { get; set; } = "normal";
        public string GameMode { get; set; } = "survival";
        public bool AllowCheats { get; set; } = false;
        public bool AllowFlight { get; set; } = false;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool NaturalRegeneration { get; set; } = true;
        public bool PvpEnabled { get; set; } = true;
        public bool FireSpread { get; set; } = true;
        public bool MobSpawning { get; set; } = true;
        public bool DaylightCycle { get; set; } = true;
        public bool WeatherCycle { get; set; } = true;
        public int MaxHealth { get; set; } = 100;
        public HungerSettings Hunger { get; set; } = new();
    }

    public class HungerSettings
    {
        public bool Enabled { get; set; } = true;
        public float DepletionRate { get; set; } = 0.5f;
        public float StarvationDamage { get; set; } = 1.0f;
        public float RegenerationThreshold { get; set; } = 80.0f;
    }

    // Network configuration model
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; } = 10000;
        public int ReconnectAttempts { get; set; } = 3;
        public int ReconnectDelayMs { get; set; } = 5000;
        public int MaxPacketSize { get; set; } = 1048576;
        public bool CompressionEnabled { get; set; } = true;
        public int CompressionThreshold { get; set; } = 1024;
        public string ProtocolVersion { get; set; } = "1.0.0";
        public bool EnableProtobuf { get; set; } = true;
        public bool RateLimitEnabled { get; set; } = true;
        public int MaxPacketsPerSecond { get; set; } = 20;
        public int MaxBytesPerSecond { get; set; } = 32768;
    }

    // Biome data model (shared with DataDriven namespace)
    public class BiomeData
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Rainfall { get; set; }
        public string[] TopBlocks { get; set; } = Array.Empty<string>();
        public string[] FillBlocks { get; set; } = Array.Empty<string>();
        public string[] UnderwaterBlocks { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> Features { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
        public float FloodedCaveNoiseFrequency { get; set; }
        public float FloodedCaveProximityToWaterTableWeight { get; set; }
        public float FloodedCaveThreshold { get; set; }
        public int StabilitySmoothIterations { get; set; }
        public float StabilitySmoothBlend { get; set; }
        public float SupportDensity { get; set; }
        public float SupportHydrationBias { get; set; }
        public float SupportFlowBias { get; set; }
        public float HydrologyStabilityWeight { get; set; }
        public float FlowStabilityWeight { get; set; }
        public float RoughnessStabilityWeight { get; set; }
        public float RiverSuppressionWeight { get; set; }
        public float MoistureRetentionWeight { get; set; }
        public float EdgeSealStrength { get; set; }
        public float SupportPillarChance { get; set; }
        public int RiparianPlugDepth { get; set; }
    }
    
    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; }
        public OreVeinSettings Coal { get; set; }
        public OreVeinSettings Iron { get; set; }
        public OreVeinSettings Gold { get; set; }
        public OreVeinSettings Diamond { get; set; }
        public OreVeinSettings Redstone { get; set; }
        public OreVeinSettings Lapis { get; set; }
    }
    
    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }
    
    public class StructureSettings
    {
        public bool EnableTrees { get; set; }
        public float TreeDensity { get; set; }
        public bool EnableVillages { get; set; }
        public bool EnableMineshafts { get; set; }
        public bool EnableDungeons { get; set; }
        public float DungeonChance { get; set; }
    }
    
    public class LakeSettings
    {
        public int MinDepth { get; set; }
        public int MaxDepth { get; set; }
        public int MaxRadius { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int ShelfDepth { get; set; }
        public float SpawnWeightBias { get; set; }
        public float ShorelineBlend { get; set; }
        public float RiverProximitySuppression { get; set; }
        public float WetlandSaturationThreshold { get; set; }
        public int OutflowCarveDepth { get; set; }
    }
    #endregion
    
    #region Gameplay Configuration
    public class GameplayConfig
    {
        public string Difficulty { get; set; }
        public string GameMode { get; set; }
        public bool AllowCheats { get; set; }
        public bool AllowFlight { get; set; }
        public bool KeepInventoryOnDeath { get; set; }
        public bool NaturalRegeneration { get; set; }
        public bool PvpEnabled { get; set; }
        public bool FireSpread { get; set; }
        public bool MobSpawning { get; set; }
        public bool DaylightCycle { get; set; }
        public bool WeatherCycle { get; set; }
        public float MaxHealth { get; set; }
        public HungerSettings Hunger { get; set; }
    }
    
    public class HungerSettings
    {
        public bool Enabled { get; set; }
        public float DepletionRate { get; set; }
        public float StarvationDamage { get; set; }
        public float RegenerationThreshold { get; set; }
    }
    #endregion
    
    #region Network Configuration
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
        public string ProtocolVersion { get; set; }
        public bool EnableProtobuf { get; set; }
        public bool RateLimitEnabled { get; set; }
        public int MaxPacketsPerSecond { get; set; }
        public int MaxBytesPerSecond { get; set; }
    }
    #endregion
}

namespace GameCommon.Configuration
{
    #region Server Configuration
    public class ServerConfig
    {
        public NetworkSettings Network { get; set; }
        public DatabaseSettings Database { get; set; }
        public PerformanceSettings Performance { get; set; }
        public SecuritySettings Security { get; set; }
        public LoggingSettings Logging { get; set; }
    }
    
    public class NetworkSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int MaxPlayers { get; set; }
        public int MaxConnectionsPerIP { get; set; }
        public int ConnectionTimeoutSeconds { get; set; }
        public int KeepAliveIntervalSeconds { get; set; }
        public int PacketCompressionThreshold { get; set; }
    }
    
    public class DatabaseSettings
    {
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public bool EnableAutoMigration { get; set; }
        public int CommandTimeoutSeconds { get; set; }
        public int MaxPoolSize { get; set; }
    }
    
    public class PerformanceSettings
    {
        public int TickRate { get; set; }
        public int ChunkLoadThreads { get; set; }
        public int MaxChunkLoadsPerTick { get; set; }
        public int ChunkUnloadDelay { get; set; }
        public int EntityUpdateDistance { get; set; }
        public bool EnableAsyncChunkGeneration { get; set; }
        public int ChunkCacheSize { get; set; }
        public bool EnableGarbageCollection { get; set; }
    }
    
    public class SecuritySettings
    {
        public bool EnableWhitelist { get; set; }
        public bool EnableAuthentication { get; set; }
        public bool EnableEncryption { get; set; }
        public int MaxPacketSize { get; set; }
        public int RateLimitPacketsPerSecond { get; set; }
        public bool EnableAntiCheat { get; set; }
        public float MaxPlayerSpeed { get; set; }
        public float MaxFlySpeed { get; set; }
    }
    
    public class LoggingSettings
    {
        public string LogLevel { get; set; }
        public bool EnableFileLogging { get; set; }
        public string LogDirectory { get; set; }
        public bool EnableConsoleLogging { get; set; }
        public int MaxLogFileSizeMB { get; set; }
        public int MaxLogFiles { get; set; }
        public bool EnablePerformanceLogging { get; set; }
        public bool EnableNetworkLogging { get; set; }
    }
    #endregion
    
    #region Client Configuration
    public class ClientConfig
    {
        public ClientNetworkSettings Network { get; set; }
        public GraphicsSettings Graphics { get; set; }
        public AudioSettings Audio { get; set; }
        public ControlSettings Controls { get; set; }
        public UISettings UI { get; set; }
        public DebugSettings Debug { get; set; }
    }
    
    public class ClientNetworkSettings
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
    }
    
    public class GraphicsSettings
    {
        public int RenderDistance { get; set; }
        public int MaxRenderDistance { get; set; }
        public int Fov { get; set; }
        public int MaxFov { get; set; }
        public float Brightness { get; set; }
        public float Gamma { get; set; }
        public bool VsyncEnabled { get; set; }
        public int MaxFps { get; set; }
        public int AntiAliasing { get; set; }
        public bool AnisotropicFiltering { get; set; }
        public string TextureQuality { get; set; }
        public string ShadowQuality { get; set; }
        public string ParticleQuality { get; set; }
        public string WaterQuality { get; set; }
    }
    
    public class AudioSettings
    {
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float SoundVolume { get; set; }
        public float AmbientVolume { get; set; }
        public float VoiceChatVolume { get; set; }
        public int MaxSoundDistance { get; set; }
        public bool DopplerEnabled { get; set; }
        public bool ReverbEnabled { get; set; }
        public string AudioDevice { get; set; }
    }
    
    public class ControlSettings
    {
        public float MouseSensitivity { get; set; }
        public bool InvertMouseY { get; set; }
        public bool SmoothMouse { get; set; }
        public float MouseSmoothing { get; set; }
        public Dictionary<string, string> KeyBindings { get; set; }
    }
    
    public class UISettings
    {
        public bool ShowCoordinates { get; set; }
        public bool ShowFps { get; set; }
        public bool ShowPing { get; set; }
        public bool ShowCrosshair { get; set; }
        public bool ShowHotbar { get; set; }
        public bool ShowInventory { get; set; }
        public bool ShowChatHistory { get; set; }
        public int MaxChatHistory { get; set; }
        public int FontSize { get; set; }
        public float UiScale { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
        public bool MinimapEnabled { get; set; }
        public int MinimapSize { get; set; }
        public float MinimapOpacity { get; set; }
    }
    
    public class DebugSettings
    {
        public bool Enabled { get; set; }
        public bool ShowCollisionBoxes { get; set; }
        public bool ShowChunkBorders { get; set; }
        public bool ShowLightLevels { get; set; }
        public bool ShowBiomeBorders { get; set; }
        public bool LogNetworkPackets { get; set; }
        public bool LogPerformanceMetrics { get; set; }
        public bool DebugRendering { get; set; }
        public bool DebugPhysics { get; set; }
        public bool DebugAI { get; set; }
        public bool DebugWorldGen { get; set; }
    }
    #endregion
    
    #region World Configuration
    public class WorldConfig
    {
        public string WorldName { get; set; }
        public int Seed { get; set; }
        public string GameMode { get; set; }
        public int WorldHeight { get; set; }
        public int ChunkSize { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public string MapControlProfilePath { get; set; }
        public int MapControlProfileVersion { get; set; }
        public TerrainGenerationSettings TerrainGeneration { get; set; }
        public WaterSettings Water { get; set; }
        public CaveSettings Caves { get; set; }
        public OreSettings Ores { get; set; }
        public StructureSettings Structures { get; set; }
        public LakeSettings Lakes { get; set; }
    }
    
    public class TerrainGenerationSettings
    {
        public int SeaLevel { get; set; }
        public int BedrockLevel { get; set; }
        public float NoiseScale { get; set; }
        public float NoiseAmplitude { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }
        public float Lacunarity { get; set; }
        public float BiomeScale { get; set; }
        public float TemperatureScale { get; set; }
        public float HumidityScale { get; set; }
        public float MountainThreshold { get; set; }
        public int MountainMaxHeight { get; set; }
        public int PlainBaseHeight { get; set; }
    }
    
    public class WaterSettings
    {
        public int GlobalWaterLevel { get; set; }
        public float RiverCenterThreshold { get; set; }
        public float RiverBankThreshold { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public float HydrologySmoothBlend { get; set; }
        public float HydrologyShorePush { get; set; }
        public float HydrologySlopePenalty { get; set; }
        public float HydrologyFlowGain { get; set; }
        public float HydrologyContinuityWeight { get; set; }
        public float HydrologyEdgeFlowBias { get; set; }
        public float HydrologyEdgeTangentWeight { get; set; }
        public float HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public float HydrologyEdgeStabilityWeight { get; set; }
        public float HydrologyEdgeVarianceClamp { get; set; }
        public float HydrologyEdgeFluxBlend { get; set; }
        public float HydrologyVarianceBlend { get; set; }
        public float HydrologyVarianceClamp { get; set; }
        public float HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public float HydrologyWaterTableSlopeWeight { get; set; }
        public float HydrologyFlowPersistence { get; set; }
        public float HydrologyGradientWeight { get; set; }
        public float HydrologyGradientSlopeWeight { get; set; }
        public float HydrologyGradientClamp { get; set; }
        public int HydrologyGradientStabilityIterations { get; set; }
        public float HydrologyGradientStabilityBlend { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public float HydrologyDirectionalBlend { get; set; }
        public float HydrologyFlowDivergenceClamp { get; set; }
        public float HydrologyCurvatureWeight { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public float HydrologySeamRelaxBlend { get; set; }
        public int RiparianSmoothIterations { get; set; }
        public float RiparianSmoothBlend { get; set; }
        public float RiparianSaturationBoost { get; set; }
        public float RiverReliefPenaltyWeight { get; set; }
        public float HydrologyWarpFrequency { get; set; }
        public float HydrologyWarpAmplitude { get; set; }
        public float RiverFlowAlignmentWeight { get; set; }
        public float RiverGradientPenalty { get; set; }
        public float RiverHeadwaterStabilityWeight { get; set; }
        public float RiverAnisotropyWeight { get; set; }
        public float RiverBankErosionWeight { get; set; }
        public float LakeRimErosionWeight { get; set; }
        public float LakeInflowBlendWeight { get; set; }
        public float RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public float RiverDeltaWetlandStrength { get; set; }
        public float RiverNoiseScale { get; set; }
        public int RiverDepth { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public float RiverIntensitySmoothBlend { get; set; }
        public float RiverConfluenceBoost { get; set; }
        public bool EnableOceans { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
    }
    
    public class CaveSettings
    {
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseRegionalMainCaves { get; set; }
        public int RegionalMainCaveRegionSizeChunks { get; set; }
        public int RegionalMainCaveWormCountMin { get; set; }
        public int RegionalMainCaveWormCountMax { get; set; }
        public int RegionalMainCaveStepsMin { get; set; }
        public int RegionalMainCaveStepsMax { get; set; }
        public int RegionalMainCaveMinY { get; set; }
        public int RegionalMainCaveMaxY { get; set; }
        public float RegionalMainCaveRadiusMin { get; set; }
        public float RegionalMainCaveRadiusMax { get; set; }
        public float CaveDensity { get; set; }
        public float CaveNoiseScale { get; set; }
        public float Threshold { get; set; }
        public float CaveThreshold { get; set; }
        public int MinCaveHeight { get; set; }
        public int MaxCaveHeight { get; set; }
        public float HorizontalFrequency { get; set; }
        public float VerticalFrequency { get; set; }
        public float NoiseThreshold { get; set; }
        public float LavaThreshold { get; set; }
        public float WaterThreshold { get; set; }
        public float FloodedCaveNoiseFrequency { get; set; }
        public float FloodedCaveProximityToWaterTableWeight { get; set; }
        public float FloodedCaveThreshold { get; set; }
        public int StabilitySmoothIterations { get; set; }
        public float StabilitySmoothBlend { get; set; }
        public float SupportDensity { get; set; }
        public float SupportHydrationBias { get; set; }
        public float SupportFlowBias { get; set; }
        public float HydrologyStabilityWeight { get; set; }
        public float FlowStabilityWeight { get; set; }
        public float RoughnessStabilityWeight { get; set; }
        public float RiverSuppressionWeight { get; set; }
        public float MoistureRetentionWeight { get; set; }
        public float EdgeSealStrength { get; set; }
        public float SupportPillarChance { get; set; }
        public int RiparianPlugDepth { get; set; }
    }
    
    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; }
        public OreVeinSettings Coal { get; set; }
        public OreVeinSettings Iron { get; set; }
        public OreVeinSettings Gold { get; set; }
        public OreVeinSettings Diamond { get; set; }
        public OreVeinSettings Redstone { get; set; }
        public OreVeinSettings Lapis { get; set; }
    }
    
    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }
    
    public class StructureSettings
    {
        public bool EnableTrees { get; set; }
        public float TreeDensity { get; set; }
        public bool EnableVillages { get; set; }
        public bool EnableMineshafts { get; set; }
        public bool EnableDungeons { get; set; }
        public float DungeonChance { get; set; }
    }
    
    public class LakeSettings
    {
        public int MinDepth { get; set; }
        public int MaxDepth { get; set; }
        public int MaxRadius { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int ShelfDepth { get; set; }
        public float SpawnWeightBias { get; set; }
        public float ShorelineBlend { get; set; }
        public float RiverProximitySuppression { get; set; }
        public float WetlandSaturationThreshold { get; set; }
        public int OutflowCarveDepth { get; set; }
    }
    #endregion
    
    #region Gameplay Configuration
    public class GameplayConfig
    {
        public string Difficulty { get; set; }
        public string GameMode { get; set; }
        public bool AllowCheats { get; set; }
        public bool AllowFlight { get; set; }
        public bool KeepInventoryOnDeath { get; set; }
        public bool NaturalRegeneration { get; set; }
        public bool PvpEnabled { get; set; }
        public bool FireSpread { get; set; }
        public bool MobSpawning { get; set; }
        public bool DaylightCycle { get; set; }
        public bool WeatherCycle { get; set; }
        public float MaxHealth { get; set; }
        public HungerSettings Hunger { get; set; }
    }
    
    public class HungerSettings
    {
        public bool Enabled { get; set; }
        public float DepletionRate { get; set; }
        public float StarvationDamage { get; set; }
        public float RegenerationThreshold { get; set; }
    }
    #endregion
    
    #region Network Configuration
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
        public string ProtocolVersion { get; set; }
        public bool EnableProtobuf { get; set; }
        public bool RateLimitEnabled { get; set; }
        public int MaxPacketsPerSecond { get; set; }
        public int MaxBytesPerSecond { get; set; }
    }
    #endregion
}
}
namespace GameCommon.Configuration
{
    #region Server Configuration
    public class ServerConfig
    {
        public NetworkSettings Network { get; set; }
        public DatabaseSettings Database { get; set; }
        public PerformanceSettings Performance { get; set; }
        public SecuritySettings Security { get; set; }
        public LoggingSettings Logging { get; set; }
    }
    
    public class NetworkSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int MaxPlayers { get; set; }
        public int MaxConnectionsPerIP { get; set; }
        public int ConnectionTimeoutSeconds { get; set; }
        public int KeepAliveIntervalSeconds { get; set; }
        public int PacketCompressionThreshold { get; set; }
    }
    
    public class DatabaseSettings
    {
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public bool EnableAutoMigration { get; set; }
        public int CommandTimeoutSeconds { get; set; }
        public int MaxPoolSize { get; set; }
    }
    
    public class PerformanceSettings
    {
        public int TickRate { get; set; }
        public int ChunkLoadThreads { get; set; }
        public int MaxChunkLoadsPerTick { get; set; }
        public int ChunkUnloadDelay { get; set; }
        public int EntityUpdateDistance { get; set; }
        public bool EnableAsyncChunkGeneration { get; set; }
        public int ChunkCacheSize { get; set; }
        public bool EnableGarbageCollection { get; set; }
    }
    
    public class SecuritySettings
    {
        public bool EnableWhitelist { get; set; }
        public bool EnableAuthentication { get; set; }
        public bool EnableEncryption { get; set; }
        public int MaxPacketSize { get; set; }
        public int RateLimitPacketsPerSecond { get; set; }
        public bool EnableAntiCheat { get; set; }
        public float MaxPlayerSpeed { get; set; }
        public float MaxFlySpeed { get; set; }
    }
    
    public class LoggingSettings
    {
        public string LogLevel { get; set; }
        public bool EnableFileLogging { get; set; }
        public string LogDirectory { get; set; }
        public bool EnableConsoleLogging { get; set; }
        public int MaxLogFileSizeMB { get; set; }
        public int MaxLogFiles { get; set; }
        public bool EnablePerformanceLogging { get; set; }
        public bool EnableNetworkLogging { get; set; }
    }
    #endregion
    
    #region Client Configuration
    public class ClientConfig
    {
        public ClientNetworkSettings Network { get; set; }
        public GraphicsSettings Graphics { get; set; }
        public AudioSettings Audio { get; set; }
        public ControlSettings Controls { get; set; }
        public UISettings UI { get; set; }
        public DebugSettings Debug { get; set; }
    }
    
    public class ClientNetworkSettings
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
    }
    
    public class GraphicsSettings
    {
        public int RenderDistance { get; set; }
        public int MaxRenderDistance { get; set; }
        public int Fov { get; set; }
        public int MaxFov { get; set; }
        public float Brightness { get; set; }
        public float Gamma { get; set; }
        public bool VsyncEnabled { get; set; }
        public int MaxFps { get; set; }
        public int AntiAliasing { get; set; }
        public bool AnisotropicFiltering { get; set; }
        public string TextureQuality { get; set; }
        public string ShadowQuality { get; set; }
        public string ParticleQuality { get; set; }
        public string WaterQuality { get; set; }
    }
    
    public class AudioSettings
    {
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float SoundVolume { get; set; }
        public float AmbientVolume { get; set; }
        public float VoiceChatVolume { get; set; }
        public int MaxSoundDistance { get; set; }
        public bool DopplerEnabled { get; set; }
        public bool ReverbEnabled { get; set; }
        public string AudioDevice { get; set; }
    }
    
    public class ControlSettings
    {
        public float MouseSensitivity { get; set; }
        public bool InvertMouseY { get; set; }
        public bool SmoothMouse { get; set; }
        public float MouseSmoothing { get; set; }
        public Dictionary<string, string> KeyBindings { get; set; }
    }
    
    public class UISettings
    {
        public bool ShowCoordinates { get; set; }
        public bool ShowFps { get; set; }
        public bool ShowPing { get; set; }
        public bool ShowCrosshair { get; set; }
        public bool ShowHotbar { get; set; }
        public bool ShowInventory { get; set; }
        public bool ShowChatHistory { get; set; }
        public int MaxChatHistory { get; set; }
        public int FontSize { get; set; }
        public float UiScale { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
        public bool MinimapEnabled { get; set; }
        public int MinimapSize { get; set; }
        public float MinimapOpacity { get; set; }
    }
    
    public class DebugSettings
    {
        public bool Enabled { get; set; }
        public bool ShowCollisionBoxes { get; set; }
        public bool ShowChunkBorders { get; set; }
        public bool ShowLightLevels { get; set; }
        public bool ShowBiomeBorders { get; set; }
        public bool LogNetworkPackets { get; set; }
        public bool LogPerformanceMetrics { get; set; }
        public bool DebugRendering { get; set; }
        public bool DebugPhysics { get; set; }
        public bool DebugAI { get; set; }
        public bool DebugWorldGen { get; set; }
    }
    #endregion
    
    #region World Configuration
    public class WorldConfig
    {
        public string WorldName { get; set; }
        public int Seed { get; set; }
        public string GameMode { get; set; }
        public int WorldHeight { get; set; }
        public int ChunkSize { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public string MapControlProfilePath { get; set; }
        public int MapControlProfileVersion { get; set; }
        public TerrainGenerationSettings TerrainGeneration { get; set; }
        public WaterSettings Water { get; set; }
        public CaveSettings Caves { get; set; }
        public OreSettings Ores { get; set; }
        public StructureSettings Structures { get; set; }
        public LakeSettings Lakes { get; set; }
    }
    
    public class TerrainGenerationSettings
    {
        public int SeaLevel { get; set; }
        public int BedrockLevel { get; set; }
        public float NoiseScale { get; set; }
        public float NoiseAmplitude { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }
        public float Lacunarity { get; set; }
        public float BiomeScale { get; set; }
        public float TemperatureScale { get; set; }
        public float HumidityScale { get; set; }
        public float MountainThreshold { get; set; }
        public int MountainMaxHeight { get; set; }
        public int PlainBaseHeight { get; set; }
    }
    
    public class WaterSettings
    {
        public int GlobalWaterLevel { get; set; }
        public float RiverCenterThreshold { get; set; }
        public float RiverBankThreshold { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public float HydrologySmoothBlend { get; set; }
        public float HydrologyShorePush { get; set; }
        public float HydrologySlopePenalty { get; set; }
        public float HydrologyFlowGain { get; set; }
        public float HydrologyContinuityWeight { get; set; }
        public float HydrologyEdgeFlowBias { get; set; }
        public float HydrologyEdgeTangentWeight { get; set; }
        public float HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public float HydrologyEdgeStabilityWeight { get; set; }
        public float HydrologyEdgeVarianceClamp { get; set; }
        public float HydrologyEdgeFluxBlend { get; set; }
        public float HydrologyVarianceBlend { get; set; }
        public float HydrologyVarianceClamp { get; set; }
        public float HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public float HydrologyWaterTableSlopeWeight { get; set; }
        public float HydrologyFlowPersistence { get; set; }
        public float HydrologyGradientWeight { get; set; }
        public float HydrologyGradientSlopeWeight { get; set; }
        public float HydrologyGradientClamp { get; set; }
        public int HydrologyGradientStabilityIterations { get; set; }
        public float HydrologyGradientStabilityBlend { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public float HydrologyDirectionalBlend { get; set; }
        public float HydrologyFlowDivergenceClamp { get; set; }
        public float HydrologyCurvatureWeight { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public float HydrologySeamRelaxBlend { get; set; }
        public int RiparianSmoothIterations { get; set; }
        public float RiparianSmoothBlend { get; set; }
        public float RiparianSaturationBoost { get; set; }
        public float RiverReliefPenaltyWeight { get; set; }
        public float HydrologyWarpFrequency { get; set; }
        public float HydrologyWarpAmplitude { get; set; }
        public float RiverFlowAlignmentWeight { get; set; }
        public float RiverGradientPenalty { get; set; }
        public float RiverHeadwaterStabilityWeight { get; set; }
        public float RiverAnisotropyWeight { get; set; }
        public float RiverBankErosionWeight { get; set; }
        public float LakeRimErosionWeight { get; set; }
        public float LakeInflowBlendWeight { get; set; }
        public float RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public float RiverDeltaWetlandStrength { get; set; }
        public float RiverNoiseScale { get; set; }
        public int RiverDepth { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public float RiverIntensitySmoothBlend { get; set; }
        public float RiverConfluenceBoost { get; set; }
        public bool EnableOceans { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
    }
    
    public class CaveSettings
    {
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseRegionalMainCaves { get; set; }
        public int RegionalMainCaveRegionSizeChunks { get; set; }
        public int RegionalMainCaveWormCountMin { get; set; }
        public int RegionalMainCaveWormCountMax { get; set; }
        public int RegionalMainCaveStepsMin { get; set; }
        public int RegionalMainCaveStepsMax { get; set; }
        public int RegionalMainCaveMinY { get; set; }
        public int RegionalMainCaveMaxY { get; set; }
        public float RegionalMainCaveRadiusMin { get; set; }
        public float RegionalMainCaveRadiusMax { get; set; }
        public float CaveDensity { get; set; }
        public float CaveNoiseScale { get; set; }
        public float Threshold { get; set; }
        public float CaveThreshold { get; set; }
        public int MinCaveHeight { get; set; }
        public int MaxCaveHeight { get; set; }
        public float HorizontalFrequency { get; set; }
        public float VerticalFrequency { get; set; }
        public float NoiseThreshold { get; set; }
        public float LavaThreshold { get; set; }
        public float WaterThreshold { get; set; }
        public float FloodedCaveNoiseFrequency { get; set; }
        public float FloodedCaveProximityToWaterTableWeight { get; set; }
        public float FloodedCaveThreshold { get; set; }
        public int StabilitySmoothIterations { get; set; }
        public float StabilitySmoothBlend { get; set; }
        public float SupportDensity { get; set; }
        public float SupportHydrationBias { get; set; }
        public float SupportFlowBias { get; set; }
        public float HydrologyStabilityWeight { get; set; }
        public float FlowStabilityWeight { get; set; }
        public float RoughnessStabilityWeight { get; set; }
        public float RiverSuppressionWeight { get; set; }
        public float MoistureRetentionWeight { get; set; }
        public float EdgeSealStrength { get; set; }
        public float SupportPillarChance { get; set; }
        public int RiparianPlugDepth { get; set; }
    }
    
    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; }
        public OreVeinSettings Coal { get; set; }
        public OreVeinSettings Iron { get; set; }
        public OreVeinSettings Gold { get; set; }
        public OreVeinSettings Diamond { get; set; }
        public OreVeinSettings Redstone { get; set; }
        public OreVeinSettings Lapis { get; set; }
    }
    
    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }
    
    public class StructureSettings
    {
        public bool EnableTrees { get; set; }
        public float TreeDensity { get; set; }
        public bool EnableVillages { get; set; }
        public bool EnableMineshafts { get; set; }
        public bool EnableDungeons { get; set; }
        public float DungeonChance { get; set; }
    }
    
    public class LakeSettings
    {
        public int MinDepth { get; set; }
        public int MaxDepth { get; set; }
        public int MaxRadius { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int ShelfDepth { get; set; }
        public float SpawnWeightBias { get; set; }
        public float ShorelineBlend { get; set; }
        public float RiverProximitySuppression { get; set; }
        public float WetlandSaturationThreshold { get; set; }
        public int OutflowCarveDepth { get; set; }
    }
    #endregion
    
    #region Gameplay Configuration
    public class GameplayConfig
    {
        public string Difficulty { get; set; }
        public string GameMode { get; set; }
        public bool AllowCheats { get; set; }
        public bool AllowFlight { get; set; }
        public bool KeepInventoryOnDeath { get; set; }
        public bool NaturalRegeneration { get; set; }
        public bool PvpEnabled { get; set; }
        public bool FireSpread { get; set; }
        public bool MobSpawning { get; set; }
        public bool DaylightCycle { get; set; }
        public bool WeatherCycle { get; set; }
        public float MaxHealth { get; set; }
        public HungerSettings Hunger { get; set; }
    }
    
    public class HungerSettings
    {
        public bool Enabled { get; set; }
        public float DepletionRate { get; set; }
        public float StarvationDamage { get; set; }
        public float RegenerationThreshold { get; set; }
    }
    #endregion
    
    #region Network Configuration
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
        public string ProtocolVersion { get; set; }
        public bool EnableProtobuf { get; set; }
        public bool RateLimitEnabled { get; set; }
        public int MaxPacketsPerSecond { get; set; }
        public int MaxBytesPerSecond { get; set; }
    }
    #endregion
}
namespace GameCommon.Configuration
{
    #region Server Configuration
    public class ServerConfig
    {
        public NetworkSettings Network { get; set; }
        public DatabaseSettings Database { get; set; }
        public PerformanceSettings Performance { get; set; }
        public SecuritySettings Security { get; set; }
        public LoggingSettings Logging { get; set; }
    }
    
    public class NetworkSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int MaxPlayers { get; set; }
        public int MaxConnectionsPerIP { get; set; }
        public int ConnectionTimeoutSeconds { get; set; }
        public int KeepAliveIntervalSeconds { get; set; }
        public int PacketCompressionThreshold { get; set; }
    }
    
    public class DatabaseSettings
    {
        public string Provider { get; set; }
        public string ConnectionString { get; set; }
        public bool EnableAutoMigration { get; set; }
        public int CommandTimeoutSeconds { get; set; }
        public int MaxPoolSize { get; set; }
    }
    
    public class PerformanceSettings
    {
        public int TickRate { get; set; }
        public int ChunkLoadThreads { get; set; }
        public int MaxChunkLoadsPerTick { get; set; }
        public int ChunkUnloadDelay { get; set; }
        public int EntityUpdateDistance { get; set; }
        public bool EnableAsyncChunkGeneration { get; set; }
        public int ChunkCacheSize { get; set; }
        public bool EnableGarbageCollection { get; set; }
    }
    
    public class SecuritySettings
    {
        public bool EnableWhitelist { get; set; }
        public bool EnableAuthentication { get; set; }
        public bool EnableEncryption { get; set; }
        public int MaxPacketSize { get; set; }
        public int RateLimitPacketsPerSecond { get; set; }
        public bool EnableAntiCheat { get; set; }
        public float MaxPlayerSpeed { get; set; }
        public float MaxFlySpeed { get; set; }
    }
    
    public class LoggingSettings
    {
        public string LogLevel { get; set; }
        public bool EnableFileLogging { get; set; }
        public string LogDirectory { get; set; }
        public bool EnableConsoleLogging { get; set; }
        public int MaxLogFileSizeMB { get; set; }
        public int MaxLogFiles { get; set; }
        public bool EnablePerformanceLogging { get; set; }
        public bool EnableNetworkLogging { get; set; }
    }
    #endregion
    
    #region Client Configuration
    public class ClientConfig
    {
        public ClientNetworkSettings Network { get; set; }
        public GraphicsSettings Graphics { get; set; }
        public AudioSettings Audio { get; set; }
        public ControlSettings Controls { get; set; }
        public UISettings UI { get; set; }
        public DebugSettings Debug { get; set; }
    }
    
    public class ClientNetworkSettings
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
    }
    
    public class GraphicsSettings
    {
        public int RenderDistance { get; set; }
        public int MaxRenderDistance { get; set; }
        public int Fov { get; set; }
        public int MaxFov { get; set; }
        public float Brightness { get; set; }
        public float Gamma { get; set; }
        public bool VsyncEnabled { get; set; }
        public int MaxFps { get; set; }
        public int AntiAliasing { get; set; }
        public bool AnisotropicFiltering { get; set; }
        public string TextureQuality { get; set; }
        public string ShadowQuality { get; set; }
        public string ParticleQuality { get; set; }
        public string WaterQuality { get; set; }
    }
    
    public class AudioSettings
    {
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float SoundVolume { get; set; }
        public float AmbientVolume { get; set; }
        public float VoiceChatVolume { get; set; }
        public int MaxSoundDistance { get; set; }
        public bool DopplerEnabled { get; set; }
        public bool ReverbEnabled { get; set; }
        public string AudioDevice { get; set; }
    }
    
    public class ControlSettings
    {
        public float MouseSensitivity { get; set; }
        public bool InvertMouseY { get; set; }
        public bool SmoothMouse { get; set; }
        public float MouseSmoothing { get; set; }
        public Dictionary<string, string> KeyBindings { get; set; }
    }
    
    public class UISettings
    {
        public bool ShowCoordinates { get; set; }
        public bool ShowFps { get; set; }
        public bool ShowPing { get; set; }
        public bool ShowCrosshair { get; set; }
        public bool ShowHotbar { get; set; }
        public bool ShowInventory { get; set; }
        public bool ShowChatHistory { get; set; }
        public int MaxChatHistory { get; set; }
        public int FontSize { get; set; }
        public float UiScale { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
        public bool MinimapEnabled { get; set; }
        public int MinimapSize { get; set; }
        public float MinimapOpacity { get; set; }
    }
    
    public class DebugSettings
    {
        public bool Enabled { get; set; }
        public bool ShowCollisionBoxes { get; set; }
        public bool ShowChunkBorders { get; set; }
        public bool ShowLightLevels { get; set; }
        public bool ShowBiomeBorders { get; set; }
        public bool LogNetworkPackets { get; set; }
        public bool LogPerformanceMetrics { get; set; }
        public bool DebugRendering { get; set; }
        public bool DebugPhysics { get; set; }
        public bool DebugAI { get; set; }
        public bool DebugWorldGen { get; set; }
    }
    #endregion
    
    #region World Configuration
    public class WorldConfig
    {
        public string WorldName { get; set; }
        public int Seed { get; set; }
        public string GameMode { get; set; }
        public int WorldHeight { get; set; }
        public int ChunkSize { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public string MapControlProfilePath { get; set; }
        public int MapControlProfileVersion { get; set; }
        public TerrainGenerationSettings TerrainGeneration { get; set; }
        public WaterSettings Water { get; set; }
        public CaveSettings Caves { get; set; }
        public OreSettings Ores { get; set; }
        public StructureSettings Structures { get; set; }
        public LakeSettings Lakes { get; set; }
    }
    
    public class TerrainGenerationSettings
    {
        public int SeaLevel { get; set; }
        public int BedrockLevel { get; set; }
        public float NoiseScale { get; set; }
        public float NoiseAmplitude { get; set; }
        public int Octaves { get; set; }
        public float Persistence { get; set; }
        public float Lacunarity { get; set; }
        public float BiomeScale { get; set; }
        public float TemperatureScale { get; set; }
        public float HumidityScale { get; set; }
        public float MountainThreshold { get; set; }
        public int MountainMaxHeight { get; set; }
        public int PlainBaseHeight { get; set; }
    }
    
    public class WaterSettings
    {
        public int GlobalWaterLevel { get; set; }
        public float RiverCenterThreshold { get; set; }
        public float RiverBankThreshold { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public float HydrologySmoothBlend { get; set; }
        public float HydrologyShorePush { get; set; }
        public float HydrologySlopePenalty { get; set; }
        public float HydrologyFlowGain { get; set; }
        public float HydrologyContinuityWeight { get; set; }
        public float HydrologyEdgeFlowBias { get; set; }
        public float HydrologyEdgeTangentWeight { get; set; }
        public float HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public float HydrologyEdgeStabilityWeight { get; set; }
        public float HydrologyEdgeVarianceClamp { get; set; }
        public float HydrologyEdgeFluxBlend { get; set; }
        public float HydrologyVarianceBlend { get; set; }
        public float HydrologyVarianceClamp { get; set; }
        public float HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public float HydrologyWaterTableSlopeWeight { get; set; }
        public float HydrologyFlowPersistence { get; set; }
        public float HydrologyGradientWeight { get; set; }
        public float HydrologyGradientSlopeWeight { get; set; }
        public float HydrologyGradientClamp { get; set; }
        public int HydrologyGradientStabilityIterations { get; set; }
        public float HydrologyGradientStabilityBlend { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public float HydrologyDirectionalBlend { get; set; }
        public float HydrologyFlowDivergenceClamp { get; set; }
        public float HydrologyCurvatureWeight { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public float HydrologySeamRelaxBlend { get; set; }
        public int RiparianSmoothIterations { get; set; }
        public float RiparianSmoothBlend { get; set; }
        public float RiparianSaturationBoost { get; set; }
        public float RiverReliefPenaltyWeight { get; set; }
        public float HydrologyWarpFrequency { get; set; }
        public float HydrologyWarpAmplitude { get; set; }
        public float RiverFlowAlignmentWeight { get; set; }
        public float RiverGradientPenalty { get; set; }
        public float RiverHeadwaterStabilityWeight { get; set; }
        public float RiverAnisotropyWeight { get; set; }
        public float RiverBankErosionWeight { get; set; }
        public float LakeRimErosionWeight { get; set; }
        public float LakeInflowBlendWeight { get; set; }
        public float RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public float RiverDeltaWetlandStrength { get; set; }
        public float RiverNoiseScale { get; set; }
        public int RiverDepth { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public float RiverIntensitySmoothBlend { get; set; }
        public float RiverConfluenceBoost { get; set; }
        public bool EnableOceans { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
    }
    
    public class CaveSettings
    {
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseRegionalMainCaves { get; set; }
        public int RegionalMainCaveRegionSizeChunks { get; set; }
        public int RegionalMainCaveWormCountMin { get; set; }
        public int RegionalMainCaveWormCountMax { get; set; }
        public int RegionalMainCaveStepsMin { get; set; }
        public int RegionalMainCaveStepsMax { get; set; }
        public int RegionalMainCaveMinY { get; set; }
        public int RegionalMainCaveMaxY { get; set; }
        public float RegionalMainCaveRadiusMin { get; set; }
        public float RegionalMainCaveRadiusMax { get; set; }
        public float CaveDensity { get; set; }
        public float CaveNoiseScale { get; set; }
        public float Threshold { get; set; }
        public float CaveThreshold { get; set; }
        public int MinCaveHeight { get; set; }
        public int MaxCaveHeight { get; set; }
        public float HorizontalFrequency { get; set; }
        public float VerticalFrequency { get; set; }
        public float NoiseThreshold { get; set; }
        public float LavaThreshold { get; set; }
        public float WaterThreshold { get; set; }
        public float FloodedCaveNoiseFrequency { get; set; }
        public float FloodedCaveProximityToWaterTableWeight { get; set; }
        public float FloodedCaveThreshold { get; set; }
        public int StabilitySmoothIterations { get; set; }
        public float StabilitySmoothBlend { get; set; }
        public float SupportDensity { get; set; }
        public float SupportHydrationBias { get; set; }
        public float SupportFlowBias { get; set; }
        public float HydrologyStabilityWeight { get; set; }
        public float FlowStabilityWeight { get; set; }
        public float RoughnessStabilityWeight { get; set; }
        public float RiverSuppressionWeight { get; set; }
        public float MoistureRetentionWeight { get; set; }
        public float EdgeSealStrength { get; set; }
        public float SupportPillarChance { get; set; }
        public int RiparianPlugDepth { get; set; }
    }
    
    public class OreSettings
    {
        public bool EnableOreGeneration { get; set; }
        public OreVeinSettings Coal { get; set; }
        public OreVeinSettings Iron { get; set; }
        public OreVeinSettings Gold { get; set; }
        public OreVeinSettings Diamond { get; set; }
        public OreVeinSettings Redstone { get; set; }
        public OreVeinSettings Lapis { get; set; }
    }
    
    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }
    
    public class StructureSettings
    {
        public bool EnableTrees { get; set; }
        public float TreeDensity { get; set; }
        public bool EnableVillages { get; set; }
        public bool EnableMineshafts { get; set; }
        public bool EnableDungeons { get; set; }
        public float DungeonChance { get; set; }
    }
    
    public class LakeSettings
    {
        public int MinDepth { get; set; }
        public int MaxDepth { get; set; }
        public int MaxRadius { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int ShelfDepth { get; set; }
        public float SpawnWeightBias { get; set; }
        public float ShorelineBlend { get; set; }
        public float RiverProximitySuppression { get; set; }
        public float WetlandSaturationThreshold { get; set; }
        public int OutflowCarveDepth { get; set; }
    }
    #endregion
    
    #region Gameplay Configuration
    public class GameplayConfig
    {
        public string Difficulty { get; set; }
        public string GameMode { get; set; }
        public bool AllowCheats { get; set; }
        public bool AllowFlight { get; set; }
        public bool KeepInventoryOnDeath { get; set; }
        public bool NaturalRegeneration { get; set; }
        public bool PvpEnabled { get; set; }
        public bool FireSpread { get; set; }
        public bool MobSpawning { get; set; }
        public bool DaylightCycle { get; set; }
        public bool WeatherCycle { get; set; }
        public float MaxHealth { get; set; }
        public HungerSettings Hunger { get; set; }
    }
    
    public class HungerSettings
    {
        public bool Enabled { get; set; }
        public float DepletionRate { get; set; }
        public float StarvationDamage { get; set; }
        public float RegenerationThreshold { get; set; }
    }
    #endregion
    
    #region Network Configuration
    public class NetworkConfig
    {
        public int ConnectionTimeoutMs { get; set; }
        public int ReconnectAttempts { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxPacketSize { get; set; }
        public bool CompressionEnabled { get; set; }
        public int CompressionThreshold { get; set; }
        public string ProtocolVersion { get; set; }
        public bool EnableProtobuf { get; set; }
        public bool RateLimitEnabled { get; set; }
        public int MaxPacketsPerSecond { get; set; }
        public int MaxBytesPerSecond { get; set; }
    }
    #endregion
}
