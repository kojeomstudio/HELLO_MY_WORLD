using System;
using System.Collections.Generic;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// Server configuration model
    /// </summary>
    public class ServerConfiguration
    {
        public string ServerName { get; set; } = "HELLO_MY_WORLD Server";
        public string ServerVersion { get; set; } = "1.0.0";
        public int MaxPlayers { get; set; } = 100;
        public int Port { get; set; } = 8080;
        public string BindAddress { get; set; } = "0.0.0.0";
        public bool EnableWhitelist { get; set; } = false;
        public bool EnablePvP { get; set; } = true;
        public bool EnableNether { get; set; } = false;
        public bool EnableEnd { get; set; } = false;
        public string Motd { get; set; } = "Welcome to HELLO_MY_WORLD!";
        public int ViewDistance { get; set; } = 10;
        public string Difficulty { get; set; } = "normal";
        public string GameMode { get; set; } = "survival";
        public bool EnableCommandBlocks { get; set; } = false;
        public bool AllowFlight { get; set; } = false;
        public bool SpawnProtection { get; set; } = true;
        public int SpawnRadius { get; set; } = 16;
        public bool KeepSpawnLoaded { get; set; } = true;
        public bool EnableRcon { get; set; } = false;
        public int RconPort { get; set; } = 25575;
        public string RconPassword { get; set; } = "";
    }
    
    /// <summary>
    /// World configuration model
    /// </summary>
    public class WorldConfiguration
    {
        public string WorldName { get; set; } = "world";
        public string WorldType { get; set; } = "default";
        public long? Seed { get; set; }
        public bool GenerateStructures { get; set; } = true;
        public bool AllowCheats { get; set; } = false;
        public bool Hardcore { get; set; } = false;
        public WorldBorderConfig WorldBorder { get; set; } = new();
        public WorldMapControlConfig WorldMapControl { get; set; } = new();
        public EnvironmentConfig Environment { get; set; } = new();
    }
    
    /// <summary>
    /// World border configuration
    /// </summary>
    public class WorldBorderConfig
    {
        public bool Enabled { get; set; } = false;
        public int CenterX { get; set; } = 0;
        public int CenterZ { get; set; } = 0;
        public int Size { get; set; } = 60000000;
        public double DamageBuffer { get; set; } = 5.0;
        public int WarningTime { get; set; } = 15;
        public int WarningDistance { get; set; } = 5;
    }
    
    /// <summary>
    /// World map control configuration
    /// </summary>
    public class WorldMapControlConfig
    {
        public string ProfileName { get; set; } = "default";
        public double TerrainScale { get; set; } = 1.0;
        public double TerrainHeightMultiplier { get; set; } = 1.0;
        public double TerrainRoughness { get; set; } = 0.5;
        public bool CaveEnabled { get; set; } = true;
        public double CaveDensity { get; set; } = 0.5;
        public bool RiverEnabled { get; set; } = true;
        public double RiverDensity { get; set; } = 0.3;
        public bool LakeEnabled { get; set; } = true;
        public double LakeDensity { get; set; } = 0.2;
        public double BiomeTemperatureScale { get; set; } = 0.002;
        public double BiomeMoistureScale { get; set; } = 0.003;
        public double VegetationDensity { get; set; } = 0.5;
        public double TreeDensity { get; set; } = 0.1;
        public double GrassDensity { get; set; } = 0.3;
    }
    
    /// <summary>
    /// Environment configuration
    /// </summary>
    public class EnvironmentConfig
    {
        public int DayDuration { get; set; } = 12000;
        public int NightDuration { get; set; } = 12000;
        public bool WeatherCycle { get; set; } = true;
        public bool ThunderCycle { get; set; } = true;
        public int SeaLevel { get; set; } = 64;
        public int MaxBuildHeight { get; set; } = 256;
        public int MinBuildHeight { get; set; } = -64;
    }
    
    /// <summary>
    /// Gameplay configuration model
    /// </summary>
    public class GameplayConfiguration
    {
        public PlayerSettingsConfig PlayerSettings { get; set; } = new();
        public MobSettingsConfig MobSettings { get; set; } = new();
        public ItemSettingsConfig ItemSettings { get; set; } = new();
        public BlockSettingsConfig BlockSettings { get; set; } = new();
        public EconomySettingsConfig EconomySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Player settings configuration
    /// </summary>
    public class PlayerSettingsConfig
    {
        public int MaxHealth { get; set; } = 20;
        public int MaxHunger { get; set; } = 20;
        public int MaxExperience { get; set; } = 2147483647;
        public int RespawnCooldown { get; set; } = 5;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool KeepExperienceOnDeath { get; set; } = false;
        public bool EnableSpectatorMode { get; set; } = true;
        public bool EnableFlying { get; set; } = false;
        public bool EnableCreativeMode { get; set; } = true;
    }
    
    /// <summary>
    /// Mob settings configuration
    /// </summary>
    public class MobSettingsConfig
    {
        public bool EnableMobs { get; set; } = true;
        public bool EnableHostileMobs { get; set; } = true;
        public bool EnablePassiveMobs { get; set; } = true;
        public bool EnableNeutralMobs { get; set; } = true;
        public double MobSpawningRate { get; set; } = 1.0;
        public int MaxMobsPerChunk { get; set; } = 70;
        public int MaxHostileMobsPerChunk { get; set; } = 40;
        public int DespawnDistance { get; set; } = 128;
        public bool PersistentMobs { get; set; } = false;
    }
    
    /// <summary>
    /// Item settings configuration
    /// </summary>
    public class ItemSettingsConfig
    {
        public bool EnableItemDrops { get; set; } = true;
        public bool EnableItemDespawning { get; set; } = true;
        public int ItemDespawnTime { get; set; } = 6000;
        public int MaxItemsPerChunk { get; set; } = 200;
        public bool EnableEnchanting { get; set; } = true;
        public bool EnableBrewing { get; set; } = true;
        public bool EnableAnvil { get; set; } = true;
        public bool EnableEnchantingTable { get; set; } = true;
        public int MaxEnchantmentLevel { get; set; } = 30;
    }
    
    /// <summary>
    /// Block settings configuration
    /// </summary>
    public class BlockSettingsConfig
    {
        public bool EnableBlockBreaking { get; set; } = true;
        public bool EnableBlockPlacing { get; set; } = true;
        public bool EnableRedstone { get; set; } = true;
        public bool EnablePistons { get; set; } = true;
        public bool EnableHoppers { get; set; } = true;
        public int MaxBlockUpdateDistance { get; set; } = 64;
        public bool EnableTileEntities { get; set; } = true;
        public bool EnableCommandBlocks { get; set; } = false;
    }
    
    /// <summary>
    /// Economy settings configuration
    /// </summary>
    public class EconomySettingsConfig
    {
        public bool EnableEconomy { get; set; } = false;
        public decimal StartingBalance { get; set; } = 0;
        public string CurrencySymbol { get; set; } = "$";
        public bool EnablePlayerShops { get; set; } = false;
        public bool EnableAdminShops { get; set; } = false;
        public decimal TaxRate { get; set; } = 0.0m;
        public bool EnableBanking { get; set; } = false;
    }
    
    /// <summary>
    /// Network configuration model
    /// </summary>
    public class NetworkConfiguration
    {
        public ConnectionSettingsConfig ConnectionSettings { get; set; } = new();
        public BandwidthSettingsConfig BandwidthSettings { get; set; } = new();
        public NetworkSecurityConfig SecuritySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Connection settings configuration
    /// </summary>
    public class ConnectionSettingsConfig
    {
        public int MaxConnections { get; set; } = 1000;
        public int ConnectionTimeout { get; set; } = 30000;
        public int KeepAliveInterval { get; set; } = 15000;
        public int MaxPacketSize { get; set; } = 2097152;
        public bool EnableCompression { get; set; } = true;
        public int CompressionThreshold { get; set; } = 256;
        public bool EnableEncryption { get; set; } = true;
        public int ProtocolVersion { get; set; } = 757;
    }
    
    /// <summary>
    /// Bandwidth settings configuration
    /// </summary>
    public class BandwidthSettingsConfig
    {
        public int MaxUploadBandwidth { get; set; } = 1048576;
        public int MaxDownloadBandwidth { get; set; } = 1048576;
        public bool EnableThrottling { get; set; } = false;
        public int ThrottleThreshold { get; set; } = 10485760;
        public bool EnableQoS { get; set; } = false;
    }
    
    /// <summary>
    /// Network security configuration
    /// </summary>
    public class NetworkSecurityConfig
    {
        public bool EnableDDoSProtection { get; set; } = true;
        public int MaxConnectionsPerIP { get; set; } = 5;
        public int ConnectionRateLimit { get; set; } = 10;
        public bool EnableIPWhitelist { get; set; } = false;
        public bool EnableIPBlacklist { get; set; } = false;
        public List<string> WhitelistIPs { get; set; } = new();
        public List<string> BlacklistIPs { get; set; } = new();
        public bool EnableProxyDetection { get; set; } = true;
    }
    
    /// <summary>
    /// Performance configuration model
    /// </summary>
    public class PerformanceConfiguration
    {
        public ChunkPerformanceConfig ChunkSettings { get; set; } = new();
        public EntityPerformanceConfig EntitySettings { get; set; } = new();
        public MemoryPerformanceConfig MemorySettings { get; set; } = new();
        public ThreadPerformanceConfig ThreadSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Chunk performance configuration
    /// </summary>
    public class ChunkPerformanceConfig
    {
        public int MaxLoadedChunks { get; set; } = 10000;
        public int ChunkGenerationThreads { get; set; } = 4;
        public int ChunkSaveInterval { get; set; } = 600;
        public bool EnableChunkCompression { get; set; } = true;
        public bool EnableChunkCaching { get; set; } = true;
        public int MaxCachedChunks { get; set; } = 1000;
        public int ChunkUnloadDistance { get; set; } = 192;
        public bool EnableAsyncChunkLoading { get; set; } = true;
    }
    
    /// <summary>
    /// Entity performance configuration
    /// </summary>
    public class EntityPerformanceConfig
    {
        public int MaxLoadedEntities { get; set; } = 10000;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableEntityCulling { get; set; } = true;
        public bool EnableLazyEntityLoading { get; set; } = true;
        public int MaxEntityUpdatesPerTick { get; set; } = 100;
        public bool EnableAsyncEntityProcessing { get; set; } = true;
    }
    
    /// <summary>
    /// Memory performance configuration
    /// </summary>
    public class MemoryPerformanceConfig
    {
        public int MaxMemoryUsage { get; set; } = 4096;
        public bool EnableMemoryMonitoring { get; set; } = true;
        public int GarbageCollectionInterval { get; set; } = 60;
        public bool EnableMemoryPooling { get; set; } = true;
        public int MaxPooledObjects { get; set; } = 10000;
        public double MemoryWarningThreshold { get; set; } = 0.8;
    }
    
    /// <summary>
    /// Thread performance configuration
    /// </summary>
    public class ThreadPerformanceConfig
    {
        public int WorkerThreads { get; set; } = Environment.ProcessorCount;
        public int IoThreads { get; set; } = 4;
        public bool EnableThreadPool { get; set; } = true;
        public int MaxThreadPoolSize { get; set; } = 100;
        public bool EnableWorkStealing { get; set; } = true;
        public string ThreadPriority { get; set; } = "normal";
    }
    
    /// <summary>
    /// Security configuration model
    /// </summary>
    public class SecurityConfiguration
    {
        public AuthenticationConfig AuthenticationSettings { get; set; } = new();
        public PermissionConfig PermissionSettings { get; set; } = new();
        public ValidationConfig ValidationSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Authentication configuration
    /// </summary>
    public class AuthenticationConfig
    {
        public bool EnableAuthentication { get; set; } = true;
        public bool RequirePassword { get; set; } = false;
        public int MinPasswordLength { get; set; } = 8;
        public bool EnableTwoFactor { get; set; } = false;
        public int SessionTimeout { get; set; } = 3600;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDuration { get; set; } = 300;
        public bool EnableBruteForceProtection { get; set; } = true;
    }
    
    /// <summary>
    /// Permission configuration
    /// </summary>
    public class PermissionConfig
    {
        public bool EnablePermissions { get; set; } = true;
        public string DefaultPermissionLevel { get; set; } = "player";
        public List<string> PermissionLevels { get; set; } = new();
        public bool EnableInheritance { get; set; } = true;
        public bool EnableWildcardPermissions { get; set; } = true;
    }
    
    /// <summary>
    /// Validation configuration
    /// </summary>
    public class ValidationConfig
    {
        public bool EnableInputValidation { get; set; } = true;
        public bool EnableCommandValidation { get; set; } = true;
        public bool EnableChatFilter { get; set; } = false;
        public int MaxChatLength { get; set; } = 256;
        public int MaxCommandLength { get; set; } = 256;
        public List<string> BlockedWords { get; set; } = new();
        public bool EnableProfanityFilter { get; set; } = false;
    }
    
    /// <summary>
    /// Database configuration model
    /// </summary>
    public class DatabaseConfiguration
    {
        public string Type { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=world.db";
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableMigrations { get; set; } = true;
        public int BackupInterval { get; set; } = 3600;
        public int BackupRetentionDays { get; set; } = 7;
        public bool EnableCompression { get; set; } = false;
        public bool EnableEncryption { get; set; } = false;
    }
    
    /// <summary>
    /// Logging configuration model
    /// </summary>
    public class LoggingConfiguration
    {
        public string LogLevel { get; set; } = "info";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public string LogFileName { get; set; } = "server-{Date}.log";
        public int MaxLogFileSize { get; set; } = 10485760;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnableJsonLogging { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = false;
        public Dictionary<string, string> Loggers { get; set; } = new();
    }
    
    // Terrain generation settings
    public class TerrainGenerationSettings
    {
        public bool UseImprovedTerrain { get; set; } = true;
        public bool EnableCaves { get; set; } = true;
        public bool UseImprovedCaves { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
        public bool UseImprovedRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool UseImprovedLakes { get; set; } = true;
        public int GlobalWaterLevel { get; set; } = 64;
        public int DayLength { get; set; } = 24000;
        public int CurrentTime { get; set; } = 0;
    }
    
    public class CaveGenerationSettings
    {
        public int CaveSystemMinSize { get; set; } = 50;
        public int CaveSystemMaxSize { get; set; } = 200;
        public double CaveTunnelMinWidth { get; set; } = 2.0;
        public double CaveTunnelMaxWidth { get; set; } = 8.0;
        public double CaveChamberMinRadius { get; set; } = 4.0;
        public double CaveChamberMaxRadius { get; set; } = 12.0;
        public double CaveVerticalVariation { get; set; } = 0.3;
        public double CaveHorizontalVariation { get; set; } = 0.4;
        public int CaveMaxDepth { get; set; } = 80;
        public double CaveRoughness { get; set; } = 0.15;
    }
    
    public class RiverGenerationSettings
    {
        public int RiverMinLength { get; set; } = 100;
        public int RiverMaxLength { get; set; } = 500;
        public double RiverMinWidth { get; set; } = 3.0;
        public double RiverMaxWidth { get; set; } = 12.0;
        public double RiverMeanderStrength { get; set; } = 0.7;
        public double RiverSlopeFactor { get; set; } = 0.02;
        public int RiverTributaryChance { get; set; } = 35;
        public int RiverMaxTributaries { get; set; } = 3;
        public double RiverDepthFactor { get; set; } = 0.3;
        public double RiverBankSteepness { get; set; } = 2.5;
    }
    
    public class LakeGenerationSettings
    {
        public int LakeMinRadius { get; set; } = 15;
        public int LakeMaxRadius { get; set; } = 80;
        public double LakeMinDepth { get; set; } = 3.0;
        public double LakeMaxDepth { get; set; } = 20.0;
        public double LakeShoreSteepness { get; set; } = 0.3;
        public int LakeIslandChance { get; set; } = 25;
        public int LakeMaxIslands { get; set; } = 3;
        public double LakeDepthVariation { get; set; } = 0.4;
        public double LakeShapeComplexity { get; set; } = 0.7;
        public double LakeTerrainFactor { get; set; } = 0.8;
    }
    
    // World map control settings
    public class WorldMapControlSettings
    {
        public int ViewDistance { get; set; } = 10;
        public int MaxConcurrentChunkGenerations { get; set; } = 4;
        public int UpdateBatchSize { get; set; } = 20;
        public int UpdateIntervalMs { get; set; } = 100;
        public int DefaultTerrainQuality { get; set; } = 2;
        public int DefaultWaterQuality { get; set; } = 2;
        public int DefaultVegetationQuality { get; set; } = 2;
        public bool DefaultFogEnabled { get; set; } = true;
        public bool DefaultShadowEnabled { get; set; } = true;
        public int DefaultMaxChunkUpdatesPerFrame { get; set; } = 10;
        public int DefaultChunkLOD { get; set; } = 2;
        public int DefaultUnloadDistance { get; set; } = 12;
    }
    
    // World seed configuration
    public class WorldSeedConfig
    {
        public int ContinentalSeed { get; set; } = 12345;
        public int MountainSeed { get; set; } = 23456;
        public int HillSeed { get; set; } = 34567;
        public int DetailSeed { get; set; } = 45678;
    }
}
using System.Collections.Generic;

namespace GameServerApp.Configuration
{
    /// <summary>
    /// Server configuration model
    /// </summary>
    public class ServerConfiguration
    {
        public string ServerName { get; set; } = "HELLO_MY_WORLD Server";
        public string ServerVersion { get; set; } = "1.0.0";
        public int MaxPlayers { get; set; } = 100;
        public int Port { get; set; } = 8080;
        public string BindAddress { get; set; } = "0.0.0.0";
        public bool EnableWhitelist { get; set; } = false;
        public bool EnablePvP { get; set; } = true;
        public bool EnableNether { get; set; } = false;
        public bool EnableEnd { get; set; } = false;
        public string Motd { get; set; } = "Welcome to HELLO_MY_WORLD!";
        public int ViewDistance { get; set; } = 10;
        public string Difficulty { get; set; } = "normal";
        public string GameMode { get; set; } = "survival";
        public bool EnableCommandBlocks { get; set; } = false;
        public bool AllowFlight { get; set; } = false;
        public bool SpawnProtection { get; set; } = true;
        public int SpawnRadius { get; set; } = 16;
        public bool KeepSpawnLoaded { get; set; } = true;
        public bool EnableRcon { get; set; } = false;
        public int RconPort { get; set; } = 25575;
        public string RconPassword { get; set; } = "";
    }
    
    /// <summary>
    /// World configuration model
    /// </summary>
    public class WorldConfiguration
    {
        public string WorldName { get; set; } = "world";
        public string WorldType { get; set; } = "default";
        public long? Seed { get; set; }
        public bool GenerateStructures { get; set; } = true;
        public bool AllowCheats { get; set; } = false;
        public bool Hardcore { get; set; } = false;
        public WorldBorderConfig WorldBorder { get; set; } = new();
        public WorldMapControlConfig WorldMapControl { get; set; } = new();
        public EnvironmentConfig Environment { get; set; } = new();
    }
    
    /// <summary>
    /// World border configuration
    /// </summary>
    public class WorldBorderConfig
    {
        public bool Enabled { get; set; } = false;
        public int CenterX { get; set; } = 0;
        public int CenterZ { get; set; } = 0;
        public int Size { get; set; } = 60000000;
        public double DamageBuffer { get; set; } = 5.0;
        public int WarningTime { get; set; } = 15;
        public int WarningDistance { get; set; } = 5;
    }
    
    /// <summary>
    /// World map control configuration
    /// </summary>
    public class WorldMapControlConfig
    {
        public string ProfileName { get; set; } = "default";
        public double TerrainScale { get; set; } = 1.0;
        public double TerrainHeightMultiplier { get; set; } = 1.0;
        public double TerrainRoughness { get; set; } = 0.5;
        public bool CaveEnabled { get; set; } = true;
        public double CaveDensity { get; set; } = 0.5;
        public bool RiverEnabled { get; set; } = true;
        public double RiverDensity { get; set; } = 0.3;
        public bool LakeEnabled { get; set; } = true;
        public double LakeDensity { get; set; } = 0.2;
        public double BiomeTemperatureScale { get; set; } = 0.002;
        public double BiomeMoistureScale { get; set; } = 0.003;
        public double VegetationDensity { get; set; } = 0.5;
        public double TreeDensity { get; set; } = 0.1;
        public double GrassDensity { get; set; } = 0.3;
    }
    
    /// <summary>
    /// Environment configuration
    /// </summary>
    public class EnvironmentConfig
    {
        public int DayDuration { get; set; } = 12000;
        public int NightDuration { get; set; } = 12000;
        public bool WeatherCycle { get; set; } = true;
        public bool ThunderCycle { get; set; } = true;
        public int SeaLevel { get; set; } = 64;
        public int MaxBuildHeight { get; set; } = 256;
        public int MinBuildHeight { get; set; } = -64;
    }
    
    /// <summary>
    /// Gameplay configuration model
    /// </summary>
    public class GameplayConfiguration
    {
        public PlayerSettingsConfig PlayerSettings { get; set; } = new();
        public MobSettingsConfig MobSettings { get; set; } = new();
        public ItemSettingsConfig ItemSettings { get; set; } = new();
        public BlockSettingsConfig BlockSettings { get; set; } = new();
        public EconomySettingsConfig EconomySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Player settings configuration
    /// </summary>
    public class PlayerSettingsConfig
    {
        public int MaxHealth { get; set; } = 20;
        public int MaxHunger { get; set; } = 20;
        public int MaxExperience { get; set; } = 2147483647;
        public int RespawnCooldown { get; set; } = 5;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool KeepExperienceOnDeath { get; set; } = false;
        public bool EnableSpectatorMode { get; set; } = true;
        public bool EnableFlying { get; set; } = false;
        public bool EnableCreativeMode { get; set; } = true;
    }
    
    /// <summary>
    /// Mob settings configuration
    /// </summary>
    public class MobSettingsConfig
    {
        public bool EnableMobs { get; set; } = true;
        public bool EnableHostileMobs { get; set; } = true;
        public bool EnablePassiveMobs { get; set; } = true;
        public bool EnableNeutralMobs { get; set; } = true;
        public double MobSpawningRate { get; set; } = 1.0;
        public int MaxMobsPerChunk { get; set; } = 70;
        public int MaxHostileMobsPerChunk { get; set; } = 40;
        public int DespawnDistance { get; set; } = 128;
        public bool PersistentMobs { get; set; } = false;
    }
    
    /// <summary>
    /// Item settings configuration
    /// </summary>
    public class ItemSettingsConfig
    {
        public bool EnableItemDrops { get; set; } = true;
        public bool EnableItemDespawning { get; set; } = true;
        public int ItemDespawnTime { get; set; } = 6000;
        public int MaxItemsPerChunk { get; set; } = 200;
        public bool EnableEnchanting { get; set; } = true;
        public bool EnableBrewing { get; set; } = true;
        public bool EnableAnvil { get; set; } = true;
        public bool EnableEnchantingTable { get; set; } = true;
        public int MaxEnchantmentLevel { get; set; } = 30;
    }
    
    /// <summary>
    /// Block settings configuration
    /// </summary>
    public class BlockSettingsConfig
    {
        public bool EnableBlockBreaking { get; set; } = true;
        public bool EnableBlockPlacing { get; set; } = true;
        public bool EnableRedstone { get; set; } = true;
        public bool EnablePistons { get; set; } = true;
        public bool EnableHoppers { get; set; } = true;
        public int MaxBlockUpdateDistance { get; set; } = 64;
        public bool EnableTileEntities { get; set; } = true;
        public bool EnableCommandBlocks { get; set; } = false;
    }
    
    /// <summary>
    /// Economy settings configuration
    /// </summary>
    public class EconomySettingsConfig
    {
        public bool EnableEconomy { get; set; } = false;
        public decimal StartingBalance { get; set; } = 0;
        public string CurrencySymbol { get; set; } = "$";
        public bool EnablePlayerShops { get; set; } = false;
        public bool EnableAdminShops { get; set; } = false;
        public decimal TaxRate { get; set; } = 0.0m;
        public bool EnableBanking { get; set; } = false;
    }
    
    /// <summary>
    /// Network configuration model
    /// </summary>
    public class NetworkConfiguration
    {
        public ConnectionSettingsConfig ConnectionSettings { get; set; } = new();
        public BandwidthSettingsConfig BandwidthSettings { get; set; } = new();
        public NetworkSecurityConfig SecuritySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Connection settings configuration
    /// </summary>
    public class ConnectionSettingsConfig
    {
        public int MaxConnections { get; set; } = 1000;
        public int ConnectionTimeout { get; set; } = 30000;
        public int KeepAliveInterval { get; set; } = 15000;
        public int MaxPacketSize { get; set; } = 2097152;
        public bool EnableCompression { get; set; } = true;
        public int CompressionThreshold { get; set; } = 256;
        public bool EnableEncryption { get; set; } = true;
        public int ProtocolVersion { get; set; } = 757;
    }
    
    /// <summary>
    /// Bandwidth settings configuration
    /// </summary>
    public class BandwidthSettingsConfig
    {
        public int MaxUploadBandwidth { get; set; } = 1048576;
        public int MaxDownloadBandwidth { get; set; } = 1048576;
        public bool EnableThrottling { get; set; } = false;
        public int ThrottleThreshold { get; set; } = 10485760;
        public bool EnableQoS { get; set; } = false;
    }
    
    /// <summary>
    /// Network security configuration
    /// </summary>
    public class NetworkSecurityConfig
    {
        public bool EnableDDoSProtection { get; set; } = true;
        public int MaxConnectionsPerIP { get; set; } = 5;
        public int ConnectionRateLimit { get; set; } = 10;
        public bool EnableIPWhitelist { get; set; } = false;
        public bool EnableIPBlacklist { get; set; } = false;
        public List<string> WhitelistIPs { get; set; } = new();
        public List<string> BlacklistIPs { get; set; } = new();
        public bool EnableProxyDetection { get; set; } = true;
    }
    
    /// <summary>
    /// Performance configuration model
    /// </summary>
    public class PerformanceConfiguration
    {
        public ChunkPerformanceConfig ChunkSettings { get; set; } = new();
        public EntityPerformanceConfig EntitySettings { get; set; } = new();
        public MemoryPerformanceConfig MemorySettings { get; set; } = new();
        public ThreadPerformanceConfig ThreadSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Chunk performance configuration
    /// </summary>
    public class ChunkPerformanceConfig
    {
        public int MaxLoadedChunks { get; set; } = 10000;
        public int ChunkGenerationThreads { get; set; } = 4;
        public int ChunkSaveInterval { get; set; } = 600;
        public bool EnableChunkCompression { get; set; } = true;
        public bool EnableChunkCaching { get; set; } = true;
        public int MaxCachedChunks { get; set; } = 1000;
        public int ChunkUnloadDistance { get; set; } = 192;
        public bool EnableAsyncChunkLoading { get; set; } = true;
    }
    
    /// <summary>
    /// Entity performance configuration
    /// </summary>
    public class EntityPerformanceConfig
    {
        public int MaxLoadedEntities { get; set; } = 10000;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableEntityCulling { get; set; } = true;
        public bool EnableLazyEntityLoading { get; set; } = true;
        public int MaxEntityUpdatesPerTick { get; set; } = 100;
        public bool EnableAsyncEntityProcessing { get; set; } = true;
    }
    
    /// <summary>
    /// Memory performance configuration
    /// </summary>
    public class MemoryPerformanceConfig
    {
        public int MaxMemoryUsage { get; set; } = 4096;
        public bool EnableMemoryMonitoring { get; set; } = true;
        public int GarbageCollectionInterval { get; set; } = 60;
        public bool EnableMemoryPooling { get; set; } = true;
        public int MaxPooledObjects { get; set; } = 10000;
        public double MemoryWarningThreshold { get; set; } = 0.8;
    }
    
    /// <summary>
    /// Thread performance configuration
    /// </summary>
    public class ThreadPerformanceConfig
    {
        public int WorkerThreads { get; set; } = Environment.ProcessorCount;
        public int IoThreads { get; set; } = 4;
        public bool EnableThreadPool { get; set; } = true;
        public int MaxThreadPoolSize { get; set; } = 100;
        public bool EnableWorkStealing { get; set; } = true;
        public string ThreadPriority { get; set; } = "normal";
    }
    
    /// <summary>
    /// Security configuration model
    /// </summary>
    public class SecurityConfiguration
    {
        public AuthenticationConfig AuthenticationSettings { get; set; } = new();
        public PermissionConfig PermissionSettings { get; set; } = new();
        public ValidationConfig ValidationSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Authentication configuration
    /// </summary>
    public class AuthenticationConfig
    {
        public bool EnableAuthentication { get; set; } = true;
        public bool RequirePassword { get; set; } = false;
        public int MinPasswordLength { get; set; } = 8;
        public bool EnableTwoFactor { get; set; } = false;
        public int SessionTimeout { get; set; } = 3600;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDuration { get; set; } = 300;
        public bool EnableBruteForceProtection { get; set; } = true;
    }
    
    /// <summary>
    /// Permission configuration
    /// </summary>
    public class PermissionConfig
    {
        public bool EnablePermissions { get; set; } = true;
        public string DefaultPermissionLevel { get; set; } = "player";
        public List<string> PermissionLevels { get; set; } = new();
        public bool EnableInheritance { get; set; } = true;
        public bool EnableWildcardPermissions { get; set; } = true;
    }
    
    /// <summary>
    /// Validation configuration
    /// </summary>
    public class ValidationConfig
    {
        public bool EnableInputValidation { get; set; } = true;
        public bool EnableCommandValidation { get; set; } = true;
        public bool EnableChatFilter { get; set; } = false;
        public int MaxChatLength { get; set; } = 256;
        public int MaxCommandLength { get; set; } = 256;
        public List<string> BlockedWords { get; set; } = new();
        public bool EnableProfanityFilter { get; set; } = false;
    }
    
    /// <summary>
    /// Database configuration model
    /// </summary>
    public class DatabaseConfiguration
    {
        public string Type { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=world.db";
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableMigrations { get; set; } = true;
        public int BackupInterval { get; set; } = 3600;
        public int BackupRetentionDays { get; set; } = 7;
        public bool EnableCompression { get; set; } = false;
        public bool EnableEncryption { get; set; } = false;
    }
    
    /// <summary>
    /// Logging configuration model
    /// </summary>
    public class LoggingConfiguration
    {
        public string LogLevel { get; set; } = "info";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public string LogFileName { get; set; } = "server-{Date}.log";
        public int MaxLogFileSize { get; set; } = 10485760;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnableJsonLogging { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = false;
        public Dictionary<string, string> Loggers { get; set; } = new();
    }
    
    // Terrain generation settings
    public class TerrainGenerationSettings
    {
        public bool UseImprovedTerrain { get; set; } = true;
        public bool EnableCaves { get; set; } = true;
        public bool UseImprovedCaves { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
        public bool UseImprovedRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool UseImprovedLakes { get; set; } = true;
        public int GlobalWaterLevel { get; set; } = 64;
        public int DayLength { get; set; } = 24000;
        public int CurrentTime { get; set; } = 0;
    }
    
    public class CaveGenerationSettings
    {
        public int CaveSystemMinSize { get; set; } = 50;
        public int CaveSystemMaxSize { get; set; } = 200;
        public double CaveTunnelMinWidth { get; set; } = 2.0;
        public double CaveTunnelMaxWidth { get; set; } = 8.0;
        public double CaveChamberMinRadius { get; set; } = 4.0;
        public double CaveChamberMaxRadius { get; set; } = 12.0;
        public double CaveVerticalVariation { get; set; } = 0.3;
        public double CaveHorizontalVariation { get; set; } = 0.4;
        public int CaveMaxDepth { get; set; } = 80;
        public double CaveRoughness { get; set; } = 0.15;
    }
    
    public class RiverGenerationSettings
    {
        public int RiverMinLength { get; set; } = 100;
        public int RiverMaxLength { get; set; } = 500;
        public double RiverMinWidth { get; set; } = 3.0;
        public double RiverMaxWidth { get; set; } = 12.0;
        public double RiverMeanderStrength { get; set; } = 0.7;
        public double RiverSlopeFactor { get; set; } = 0.02;
        public int RiverTributaryChance { get; set; } = 35;
        public int RiverMaxTributaries { get; set; } = 3;
        public double RiverDepthFactor { get; set; } = 0.3;
        public double RiverBankSteepness { get; set; } = 2.5;
    }
    
    public class LakeGenerationSettings
    {
        public int LakeMinRadius { get; set; } = 15;
        public int LakeMaxRadius { get; set; } = 80;
        public double LakeMinDepth { get; set; } = 3.0;
        public double LakeMaxDepth { get; set; } = 20.0;
        public double LakeShoreSteepness { get; set; } = 0.3;
        public int LakeIslandChance { get; set; } = 25;
        public int LakeMaxIslands { get; set; } = 3;
        public double LakeDepthVariation { get; set; } = 0.4;
        public double LakeShapeComplexity { get; set; } = 0.7;
        public double LakeTerrainFactor { get; set; } = 0.8;
    }
    
    // World map control settings
    public class WorldMapControlSettings
    {
        public int ViewDistance { get; set; } = 10;
        public int MaxConcurrentChunkGenerations { get; set; } = 4;
        public int UpdateBatchSize { get; set; } = 20;
        public int UpdateIntervalMs { get; set; } = 100;
        public int DefaultTerrainQuality { get; set; } = 2;
        public int DefaultWaterQuality { get; set; } = 2;
        public int DefaultVegetationQuality { get; set; } = 2;
        public bool DefaultFogEnabled { get; set; } = true;
        public bool DefaultShadowEnabled { get; set; } = true;
        public int DefaultMaxChunkUpdatesPerFrame { get; set; } = 10;
        public int DefaultChunkLOD { get; set; } = 2;
        public int DefaultUnloadDistance { get; set; } = 12;
    }
    
    // World seed configuration
    public class WorldSeedConfig
    {
        public int ContinentalSeed { get; set; } = 12345;
        public int MountainSeed { get; set; } = 23456;
        public int HillSeed { get; set; } = 34567;
        public int DetailSeed { get; set; } = 45678;
    }
}
    
    /// <summary>
    /// Environment configuration
    /// </summary>
    public class EnvironmentConfig
    {
        public int DayDuration { get; set; } = 12000;
        public int NightDuration { get; set; } = 12000;
        public bool WeatherCycle { get; set; } = true;
        public bool ThunderCycle { get; set; } = true;
        public int SeaLevel { get; set; } = 64;
        public int MaxBuildHeight { get; set; } = 256;
        public int MinBuildHeight { get; set; } = -64;
    }
    
    /// <summary>
    /// Gameplay configuration model
    /// </summary>
    public class GameplayConfiguration
    {
        public PlayerSettingsConfig PlayerSettings { get; set; } = new();
        public MobSettingsConfig MobSettings { get; set; } = new();
        public ItemSettingsConfig ItemSettings { get; set; } = new();
        public BlockSettingsConfig BlockSettings { get; set; } = new();
        public EconomySettingsConfig EconomySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Player settings configuration
    /// </summary>
    public class PlayerSettingsConfig
    {
        public int MaxHealth { get; set; } = 20;
        public int MaxHunger { get; set; } = 20;
        public int MaxExperience { get; set; } = 2147483647;
        public int RespawnCooldown { get; set; } = 5;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool KeepExperienceOnDeath { get; set; } = false;
        public bool EnableSpectatorMode { get; set; } = true;
        public bool EnableFlying { get; set; } = false;
        public bool EnableCreativeMode { get; set; } = true;
    }
    
    /// <summary>
    /// Mob settings configuration
    /// </summary>
    public class MobSettingsConfig
    {
        public bool EnableMobs { get; set; } = true;
        public bool EnableHostileMobs { get; set; } = true;
        public bool EnablePassiveMobs { get; set; } = true;
        public bool EnableNeutralMobs { get; set; } = true;
        public double MobSpawningRate { get; set; } = 1.0;
        public int MaxMobsPerChunk { get; set; } = 70;
        public int MaxHostileMobsPerChunk { get; set; } = 40;
        public int DespawnDistance { get; set; } = 128;
        public bool PersistentMobs { get; set; } = false;
    }
    
    /// <summary>
    /// Item settings configuration
    /// </summary>
    public class ItemSettingsConfig
    {
        public bool EnableItemDrops { get; set; } = true;
        public bool EnableItemDespawning { get; set; } = true;
        public int ItemDespawnTime { get; set; } = 6000;
        public int MaxItemsPerChunk { get; set; } = 200;
        public bool EnableEnchanting { get; set; } = true;
        public bool EnableBrewing { get; set; } = true;
        public bool EnableAnvil { get; set; } = true;
        public bool EnableEnchantingTable { get; set; } = true;
        public int MaxEnchantmentLevel { get; set; } = 30;
    }
    
    /// <summary>
    /// Block settings configuration
    /// </summary>
    public class BlockSettingsConfig
    {
        public bool EnableBlockBreaking { get; set; } = true;
        public bool EnableBlockPlacing { get; set; } = true;
        public bool EnableRedstone { get; set; } = true;
        public bool EnablePistons { get; set; } = true;
        public bool EnableHoppers { get; set; } = true;
        public int MaxBlockUpdateDistance { get; set; } = 64;
        public bool EnableTileEntities { get; set; } = true;
        public bool EnableCommandBlocks { get; set; } = false;
    }
    
    /// <summary>
    /// Economy settings configuration
    /// </summary>
    public class EconomySettingsConfig
    {
        public bool EnableEconomy { get; set; } = false;
        public decimal StartingBalance { get; set; } = 0;
        public string CurrencySymbol { get; set; } = "$";
        public bool EnablePlayerShops { get; set; } = false;
        public bool EnableAdminShops { get; set; } = false;
        public decimal TaxRate { get; set; } = 0.0m;
        public bool EnableBanking { get; set; } = false;
    }
    
    /// <summary>
    /// Network configuration model
    /// </summary>
    public class NetworkConfiguration
    {
        public ConnectionSettingsConfig ConnectionSettings { get; set; } = new();
        public BandwidthSettingsConfig BandwidthSettings { get; set; } = new();
        public NetworkSecurityConfig SecuritySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Connection settings configuration
    /// </summary>
    public class ConnectionSettingsConfig
    {
        public int MaxConnections { get; set; } = 1000;
        public int ConnectionTimeout { get; set; } = 30000;
        public int KeepAliveInterval { get; set; } = 15000;
        public int MaxPacketSize { get; set; } = 2097152;
        public bool EnableCompression { get; set; } = true;
        public int CompressionThreshold { get; set; } = 256;
        public bool EnableEncryption { get; set; } = true;
        public int ProtocolVersion { get; set; } = 757;
    }
    
    /// <summary>
    /// Bandwidth settings configuration
    /// </summary>
    public class BandwidthSettingsConfig
    {
        public int MaxUploadBandwidth { get; set; } = 1048576;
        public int MaxDownloadBandwidth { get; set; } = 1048576;
        public bool EnableThrottling { get; set; } = false;
        public int ThrottleThreshold { get; set; } = 10485760;
        public bool EnableQoS { get; set; } = false;
    }
    
    /// <summary>
    /// Network security configuration
    /// </summary>
    public class NetworkSecurityConfig
    {
        public bool EnableDDoSProtection { get; set; } = true;
        public int MaxConnectionsPerIP { get; set; } = 5;
        public int ConnectionRateLimit { get; set; } = 10;
        public bool EnableIPWhitelist { get; set; } = false;
        public bool EnableIPBlacklist { get; set; } = false;
        public List<string> WhitelistIPs { get; set; } = new();
        public List<string> BlacklistIPs { get; set; } = new();
        public bool EnableProxyDetection { get; set; } = true;
    }
    
    /// <summary>
    /// Performance configuration model
    /// </summary>
    public class PerformanceConfiguration
    {
        public ChunkPerformanceConfig ChunkSettings { get; set; } = new();
        public EntityPerformanceConfig EntitySettings { get; set; } = new();
        public MemoryPerformanceConfig MemorySettings { get; set; } = new();
        public ThreadPerformanceConfig ThreadSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Chunk performance configuration
    /// </summary>
    public class ChunkPerformanceConfig
    {
        public int MaxLoadedChunks { get; set; } = 10000;
        public int ChunkGenerationThreads { get; set; } = 4;
        public int ChunkSaveInterval { get; set; } = 600;
        public bool EnableChunkCompression { get; set; } = true;
        public bool EnableChunkCaching { get; set; } = true;
        public int MaxCachedChunks { get; set; } = 1000;
        public int ChunkUnloadDistance { get; set; } = 192;
        public bool EnableAsyncChunkLoading { get; set; } = true;
    }
    
    /// <summary>
    /// Entity performance configuration
    /// </summary>
    public class EntityPerformanceConfig
    {
        public int MaxLoadedEntities { get; set; } = 10000;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableEntityCulling { get; set; } = true;
        public bool EnableLazyEntityLoading { get; set; } = true;
        public int MaxEntityUpdatesPerTick { get; set; } = 100;
        public bool EnableAsyncEntityProcessing { get; set; } = true;
    }
    
    /// <summary>
    /// Memory performance configuration
    /// </summary>
    public class MemoryPerformanceConfig
    {
        public int MaxMemoryUsage { get; set; } = 4096;
        public bool EnableMemoryMonitoring { get; set; } = true;
        public int GarbageCollectionInterval { get; set; } = 60;
        public bool EnableMemoryPooling { get; set; } = true;
        public int MaxPooledObjects { get; set; } = 10000;
        public double MemoryWarningThreshold { get; set; } = 0.8;
    }
    
    /// <summary>
    /// Thread performance configuration
    /// </summary>
    public class ThreadPerformanceConfig
    {
        public int WorkerThreads { get; set; } = Environment.ProcessorCount;
        public int IoThreads { get; set; } = 4;
        public bool EnableThreadPool { get; set; } = true;
        public int MaxThreadPoolSize { get; set; } = 100;
        public bool EnableWorkStealing { get; set; } = true;
        public string ThreadPriority { get; set; } = "normal";
    }
    
    /// <summary>
    /// Security configuration model
    /// </summary>
    public class SecurityConfiguration
    {
        public AuthenticationConfig AuthenticationSettings { get; set; } = new();
        public PermissionConfig PermissionSettings { get; set; } = new();
        public ValidationConfig ValidationSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Authentication configuration
    /// </summary>
    public class AuthenticationConfig
    {
        public bool EnableAuthentication { get; set; } = true;
        public bool RequirePassword { get; set; } = false;
        public int MinPasswordLength { get; set; } = 8;
        public bool EnableTwoFactor { get; set; } = false;
        public int SessionTimeout { get; set; } = 3600;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDuration { get; set; } = 300;
        public bool EnableBruteForceProtection { get; set; } = true;
    }
    
    /// <summary>
    /// Permission configuration
    /// </summary>
    public class PermissionConfig
    {
        public bool EnablePermissions { get; set; } = true;
        public string DefaultPermissionLevel { get; set; } = "player";
        public List<string> PermissionLevels { get; set; } = new();
        public bool EnableInheritance { get; set; } = true;
        public bool EnableWildcardPermissions { get; set; } = true;
    }
    
    /// <summary>
    /// Validation configuration
    /// </summary>
    public class ValidationConfig
    {
        public bool EnableInputValidation { get; set; } = true;
        public bool EnableCommandValidation { get; set; } = true;
        public bool EnableChatFilter { get; set; } = false;
        public int MaxChatLength { get; set; } = 256;
        public int MaxCommandLength { get; set; } = 256;
        public List<string> BlockedWords { get; set; } = new();
        public bool EnableProfanityFilter { get; set; } = false;
    }
    
    /// <summary>
    /// Database configuration model
    /// </summary>
    public class DatabaseConfiguration
    {
        public string Type { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=world.db";
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableMigrations { get; set; } = true;
        public int BackupInterval { get; set; } = 3600;
        public int BackupRetentionDays { get; set; } = 7;
        public bool EnableCompression { get; set; } = false;
        public bool EnableEncryption { get; set; } = false;
    }
    
    /// <summary>
    /// Logging configuration model
    /// </summary>
    public class LoggingConfiguration
    {
        public string LogLevel { get; set; } = "info";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public string LogFileName { get; set; } = "server-{Date}.log";
        public int MaxLogFileSize { get; set; } = 10485760;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnableJsonLogging { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = false;
        public Dictionary<string, string> Loggers { get; set; } = new();
    }
}
}
namespace GameServerApp.Configuration
{
    /// <summary>
    /// Server configuration model
    /// </summary>
    public class ServerConfiguration
    {
        public string ServerName { get; set; } = "HELLO_MY_WORLD Server";
        public string ServerVersion { get; set; } = "1.0.0";
        public int MaxPlayers { get; set; } = 100;
        public int Port { get; set; } = 8080;
        public string BindAddress { get; set; } = "0.0.0.0";
        public bool EnableWhitelist { get; set; } = false;
        public bool EnablePvP { get; set; } = true;
        public bool EnableNether { get; set; } = false;
        public bool EnableEnd { get; set; } = false;
        public string Motd { get; set; } = "Welcome to HELLO_MY_WORLD!";
        public int ViewDistance { get; set; } = 10;
        public string Difficulty { get; set; } = "normal";
        public string GameMode { get; set; } = "survival";
        public bool EnableCommandBlocks { get; set; } = false;
        public bool AllowFlight { get; set; } = false;
        public bool SpawnProtection { get; set; } = true;
        public int SpawnRadius { get; set; } = 16;
        public bool KeepSpawnLoaded { get; set; } = true;
        public bool EnableRcon { get; set; } = false;
        public int RconPort { get; set; } = 25575;
        public string RconPassword { get; set; } = "";
    }
    
    /// <summary>
    /// World configuration model
    /// </summary>
    public class WorldConfiguration
    {
        public string WorldName { get; set; } = "world";
        public string WorldType { get; set; } = "default";
        public long? Seed { get; set; }
        public bool GenerateStructures { get; set; } = true;
        public bool AllowCheats { get; set; } = false;
        public bool Hardcore { get; set; } = false;
        public WorldBorderConfig WorldBorder { get; set; } = new();
        public WorldMapControlConfig WorldMapControl { get; set; } = new();
        public EnvironmentConfig Environment { get; set; } = new();
    }
    
    /// <summary>
    /// World border configuration
    /// </summary>
    public class WorldBorderConfig
    {
        public bool Enabled { get; set; } = false;
        public int CenterX { get; set; } = 0;
        public int CenterZ { get; set; } = 0;
        public int Size { get; set; } = 60000000;
        public double DamageBuffer { get; set; } = 5.0;
        public int WarningTime { get; set; } = 15;
        public int WarningDistance { get; set; } = 5;
    }
    
    /// <summary>
    /// World map control configuration
    /// </summary>
    public class WorldMapControlConfig
    {
        public string ProfileName { get; set; } = "default";
        public double TerrainScale { get; set; } = 1.0;
        public double TerrainHeightMultiplier { get; set; } = 1.0;
        public double TerrainRoughness { get; set; } = 0.5;
        public bool CaveEnabled { get; set; } = true;
        public double CaveDensity { get; set; } = 0.5;
        public bool RiverEnabled { get; set; } = true;
        public double RiverDensity { get; set; } = 0.3;
        public bool LakeEnabled { get; set; } = true;
        public double LakeDensity { get; set; } = 0.2;
        public double BiomeTemperatureScale { get; set; } = 0.002;
        public double BiomeMoistureScale { get; set; } = 0.003;
        public double VegetationDensity { get; set; } = 0.5;
        public double TreeDensity { get; set; } = 0.1;
        public double GrassDensity { get; set; } = 0.3;
    }
    
    /// <summary>
    /// Environment configuration
    /// </summary>
    public class EnvironmentConfig
    {
        public int DayDuration { get; set; } = 12000;
        public int NightDuration { get; set; } = 12000;
        public bool WeatherCycle { get; set; } = true;
        public bool ThunderCycle { get; set; } = true;
        public int SeaLevel { get; set; } = 64;
        public int MaxBuildHeight { get; set; } = 256;
        public int MinBuildHeight { get; set; } = -64;
    }
    
    /// <summary>
    /// Gameplay configuration model
    /// </summary>
    public class GameplayConfiguration
    {
        public PlayerSettingsConfig PlayerSettings { get; set; } = new();
        public MobSettingsConfig MobSettings { get; set; } = new();
        public ItemSettingsConfig ItemSettings { get; set; } = new();
        public BlockSettingsConfig BlockSettings { get; set; } = new();
        public EconomySettingsConfig EconomySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Player settings configuration
    /// </summary>
    public class PlayerSettingsConfig
    {
        public int MaxHealth { get; set; } = 20;
        public int MaxHunger { get; set; } = 20;
        public int MaxExperience { get; set; } = 2147483647;
        public int RespawnCooldown { get; set; } = 5;
        public bool KeepInventoryOnDeath { get; set; } = false;
        public bool KeepExperienceOnDeath { get; set; } = false;
        public bool EnableSpectatorMode { get; set; } = true;
        public bool EnableFlying { get; set; } = false;
        public bool EnableCreativeMode { get; set; } = true;
    }
    
    /// <summary>
    /// Mob settings configuration
    /// </summary>
    public class MobSettingsConfig
    {
        public bool EnableMobs { get; set; } = true;
        public bool EnableHostileMobs { get; set; } = true;
        public bool EnablePassiveMobs { get; set; } = true;
        public bool EnableNeutralMobs { get; set; } = true;
        public double MobSpawningRate { get; set; } = 1.0;
        public int MaxMobsPerChunk { get; set; } = 70;
        public int MaxHostileMobsPerChunk { get; set; } = 40;
        public int DespawnDistance { get; set; } = 128;
        public bool PersistentMobs { get; set; } = false;
    }
    
    /// <summary>
    /// Item settings configuration
    /// </summary>
    public class ItemSettingsConfig
    {
        public bool EnableItemDrops { get; set; } = true;
        public bool EnableItemDespawning { get; set; } = true;
        public int ItemDespawnTime { get; set; } = 6000;
        public int MaxItemsPerChunk { get; set; } = 200;
        public bool EnableEnchanting { get; set; } = true;
        public bool EnableBrewing { get; set; } = true;
        public bool EnableAnvil { get; set; } = true;
        public bool EnableEnchantingTable { get; set; } = true;
        public int MaxEnchantmentLevel { get; set; } = 30;
    }
    
    /// <summary>
    /// Block settings configuration
    /// </summary>
    public class BlockSettingsConfig
    {
        public bool EnableBlockBreaking { get; set; } = true;
        public bool EnableBlockPlacing { get; set; } = true;
        public bool EnableRedstone { get; set; } = true;
        public bool EnablePistons { get; set; } = true;
        public bool EnableHoppers { get; set; } = true;
        public int MaxBlockUpdateDistance { get; set; } = 64;
        public bool EnableTileEntities { get; set; } = true;
        public bool EnableCommandBlocks { get; set; } = false;
    }
    
    /// <summary>
    /// Economy settings configuration
    /// </summary>
    public class EconomySettingsConfig
    {
        public bool EnableEconomy { get; set; } = false;
        public decimal StartingBalance { get; set; } = 0;
        public string CurrencySymbol { get; set; } = "$";
        public bool EnablePlayerShops { get; set; } = false;
        public bool EnableAdminShops { get; set; } = false;
        public decimal TaxRate { get; set; } = 0.0m;
        public bool EnableBanking { get; set; } = false;
    }
    
    /// <summary>
    /// Network configuration model
    /// </summary>
    public class NetworkConfiguration
    {
        public ConnectionSettingsConfig ConnectionSettings { get; set; } = new();
        public BandwidthSettingsConfig BandwidthSettings { get; set; } = new();
        public NetworkSecurityConfig SecuritySettings { get; set; } = new();
    }
    
    /// <summary>
    /// Connection settings configuration
    /// </summary>
    public class ConnectionSettingsConfig
    {
        public int MaxConnections { get; set; } = 1000;
        public int ConnectionTimeout { get; set; } = 30000;
        public int KeepAliveInterval { get; set; } = 15000;
        public int MaxPacketSize { get; set; } = 2097152;
        public bool EnableCompression { get; set; } = true;
        public int CompressionThreshold { get; set; } = 256;
        public bool EnableEncryption { get; set; } = true;
        public int ProtocolVersion { get; set; } = 757;
    }
    
    /// <summary>
    /// Bandwidth settings configuration
    /// </summary>
    public class BandwidthSettingsConfig
    {
        public int MaxUploadBandwidth { get; set; } = 1048576;
        public int MaxDownloadBandwidth { get; set; } = 1048576;
        public bool EnableThrottling { get; set; } = false;
        public int ThrottleThreshold { get; set; } = 10485760;
        public bool EnableQoS { get; set; } = false;
    }
    
    /// <summary>
    /// Network security configuration
    /// </summary>
    public class NetworkSecurityConfig
    {
        public bool EnableDDoSProtection { get; set; } = true;
        public int MaxConnectionsPerIP { get; set; } = 5;
        public int ConnectionRateLimit { get; set; } = 10;
        public bool EnableIPWhitelist { get; set; } = false;
        public bool EnableIPBlacklist { get; set; } = false;
        public List<string> WhitelistIPs { get; set; } = new();
        public List<string> BlacklistIPs { get; set; } = new();
        public bool EnableProxyDetection { get; set; } = true;
    }
    
    /// <summary>
    /// Performance configuration model
    /// </summary>
    public class PerformanceConfiguration
    {
        public ChunkPerformanceConfig ChunkSettings { get; set; } = new();
        public EntityPerformanceConfig EntitySettings { get; set; } = new();
        public MemoryPerformanceConfig MemorySettings { get; set; } = new();
        public ThreadPerformanceConfig ThreadSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Chunk performance configuration
    /// </summary>
    public class ChunkPerformanceConfig
    {
        public int MaxLoadedChunks { get; set; } = 10000;
        public int ChunkGenerationThreads { get; set; } = 4;
        public int ChunkSaveInterval { get; set; } = 600;
        public bool EnableChunkCompression { get; set; } = true;
        public bool EnableChunkCaching { get; set; } = true;
        public int MaxCachedChunks { get; set; } = 1000;
        public int ChunkUnloadDistance { get; set; } = 192;
        public bool EnableAsyncChunkLoading { get; set; } = true;
    }
    
    /// <summary>
    /// Entity performance configuration
    /// </summary>
    public class EntityPerformanceConfig
    {
        public int MaxLoadedEntities { get; set; } = 10000;
        public int EntityUpdateDistance { get; set; } = 128;
        public bool EnableEntityCulling { get; set; } = true;
        public bool EnableLazyEntityLoading { get; set; } = true;
        public int MaxEntityUpdatesPerTick { get; set; } = 100;
        public bool EnableAsyncEntityProcessing { get; set; } = true;
    }
    
    /// <summary>
    /// Memory performance configuration
    /// </summary>
    public class MemoryPerformanceConfig
    {
        public int MaxMemoryUsage { get; set; } = 4096;
        public bool EnableMemoryMonitoring { get; set; } = true;
        public int GarbageCollectionInterval { get; set; } = 60;
        public bool EnableMemoryPooling { get; set; } = true;
        public int MaxPooledObjects { get; set; } = 10000;
        public double MemoryWarningThreshold { get; set; } = 0.8;
    }
    
    /// <summary>
    /// Thread performance configuration
    /// </summary>
    public class ThreadPerformanceConfig
    {
        public int WorkerThreads { get; set; } = Environment.ProcessorCount;
        public int IoThreads { get; set; } = 4;
        public bool EnableThreadPool { get; set; } = true;
        public int MaxThreadPoolSize { get; set; } = 100;
        public bool EnableWorkStealing { get; set; } = true;
        public string ThreadPriority { get; set; } = "normal";
    }
    
    /// <summary>
    /// Security configuration model
    /// </summary>
    public class SecurityConfiguration
    {
        public AuthenticationConfig AuthenticationSettings { get; set; } = new();
        public PermissionConfig PermissionSettings { get; set; } = new();
        public ValidationConfig ValidationSettings { get; set; } = new();
    }
    
    /// <summary>
    /// Authentication configuration
    /// </summary>
    public class AuthenticationConfig
    {
        public bool EnableAuthentication { get; set; } = true;
        public bool RequirePassword { get; set; } = false;
        public int MinPasswordLength { get; set; } = 8;
        public bool EnableTwoFactor { get; set; } = false;
        public int SessionTimeout { get; set; } = 3600;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDuration { get; set; } = 300;
        public bool EnableBruteForceProtection { get; set; } = true;
    }
    
    /// <summary>
    /// Permission configuration
    /// </summary>
    public class PermissionConfig
    {
        public bool EnablePermissions { get; set; } = true;
        public string DefaultPermissionLevel { get; set; } = "player";
        public List<string> PermissionLevels { get; set; } = new();
        public bool EnableInheritance { get; set; } = true;
        public bool EnableWildcardPermissions { get; set; } = true;
    }
    
    /// <summary>
    /// Validation configuration
    /// </summary>
    public class ValidationConfig
    {
        public bool EnableInputValidation { get; set; } = true;
        public bool EnableCommandValidation { get; set; } = true;
        public bool EnableChatFilter { get; set; } = false;
        public int MaxChatLength { get; set; } = 256;
        public int MaxCommandLength { get; set; } = 256;
        public List<string> BlockedWords { get; set; } = new();
        public bool EnableProfanityFilter { get; set; } = false;
    }
    
    /// <summary>
    /// Database configuration model
    /// </summary>
    public class DatabaseConfiguration
    {
        public string Type { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=world.db";
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 5;
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableMigrations { get; set; } = true;
        public int BackupInterval { get; set; } = 3600;
        public int BackupRetentionDays { get; set; } = 7;
        public bool EnableCompression { get; set; } = false;
        public bool EnableEncryption { get; set; } = false;
    }
    
    /// <summary>
    /// Logging configuration model
    /// </summary>
    public class LoggingConfiguration
    {
        public string LogLevel { get; set; } = "info";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public string LogFileName { get; set; } = "server-{Date}.log";
        public int MaxLogFileSize { get; set; } = 10485760;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnableJsonLogging { get; set; } = false;
        public bool EnableStructuredLogging { get; set; } = false;
        public Dictionary<string, string> Loggers { get; set; } = new();
    }
}
