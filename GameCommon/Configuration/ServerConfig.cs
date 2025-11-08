namespace GameCommon.Configuration
{
    /// <summary>
    /// 서버 설정
    /// config/server.json에서 로드
    /// </summary>
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
        public int Port { get; set; } = 25565;
        public int MaxPlayers { get; set; } = 20;
        public int MaxConnectionsPerIP { get; set; } = 3;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public int KeepAliveIntervalSeconds { get; set; } = 5;
        public int PacketCompressionThreshold { get; set; } = 256;
    }

    public class DatabaseSettings
    {
        public string Provider { get; set; } = "sqlite"; // sqlite, postgresql, mysql
        public string ConnectionString { get; set; } = "Data Source=gameserver.db";
        public bool EnableAutoMigration { get; set; } = true;
        public int CommandTimeoutSeconds { get; set; } = 30;
        public int MaxPoolSize { get; set; } = 100;
    }

    public class PerformanceSettings
    {
        public int TickRate { get; set; } = 20; // TPS (ticks per second)
        public int ChunkLoadThreads { get; set; } = 4;
        public int MaxChunkLoadsPerTick { get; set; } = 10;
        public int ChunkUnloadDelay { get; set; } = 30; // seconds
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
        public int MaxPacketSize { get; set; } = 2097152; // 2MB
        public int RateLimitPacketsPerSecond { get; set; } = 100;
        public bool EnableAntiCheat { get; set; } = true;
        public double MaxPlayerSpeed { get; set; } = 10.0;
        public double MaxFlySpeed { get; set; } = 20.0;
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information"; // Trace, Debug, Information, Warning, Error, Critical
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public bool EnableConsoleLogging { get; set; } = true;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnablePerformanceLogging { get; set; } = false;
        public bool EnableNetworkLogging { get; set; } = false;
    }
}
