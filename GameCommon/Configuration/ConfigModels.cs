namespace GameCommon.Configuration
{
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
        public int SaveIntervalSeconds { get; set; } = 60;
    }

    public class SecuritySettings
    {
        public bool EnableSsl { get; set; }
        public string SslCertificatePath { get; set; } = string.Empty;
        public string SslCertificatePassword { get; set; } = string.Empty;
        public bool EnableRateLimiting { get; set; } = true;
        public int MaxPacketsPerSecond { get; set; } = 200;
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public string LogFilePath { get; set; } = "logs/server.log";
        public bool EnableStructuredLogging { get; set; } = true;
    }

    public class ClientConfig
    {
        public InputSettings Input { get; set; } = new();
        public VideoSettings Video { get; set; } = new();
        public AudioSettings Audio { get; set; } = new();
    }

    public class InputSettings
    {
        public float MouseSensitivity { get; set; } = 0.5f;
        public bool InvertMouse { get; set; }
    }

    public class VideoSettings
    {
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public bool Fullscreen { get; set; } = true;
        public int VSyncCount { get; set; } = 1;
        public int TargetFps { get; set; } = 60;
    }

    public class AudioSettings
    {
        public float MasterVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.6f;
        public float SfxVolume { get; set; } = 0.8f;
    }

    public class GameplayConfig
    {
        public bool EnableHunger { get; set; } = true;
        public bool EnableWeather { get; set; } = true;
        public int MaxHealth { get; set; } = 100;
        public float PlayerMoveSpeed { get; set; } = 6.0f;
    }

    public class NetworkConfig
    {
        public string MatchmakingEndpoint { get; set; } = string.Empty;
        public string TelemetryEndpoint { get; set; } = string.Empty;
        public int HeartbeatSeconds { get; set; } = 5;
    }

    public class WorldConfig
    {
        public string WorldName { get; set; } = "HELLO_MY_WORLD";
        public int Seed { get; set; }
        public int WorldHeight { get; set; } = 256;
        public int ChunkSize { get; set; } = 16;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;
        public string MapControlProfilePath { get; set; } = "config/world_map_control_profile.json";
    }
}
