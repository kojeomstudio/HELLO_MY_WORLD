using System;
using System.Collections.Generic;

namespace GameServerApp.Configuration
{
    public sealed class ServerConfiguration
    {
        public string ServerName { get; set; } = "HELLO_MY_WORLD Server";
        public string ServerVersion { get; set; } = "1.0.0";
        public int MaxPlayers { get; set; } = 100;
        public int Port { get; set; } = 9000;
        public string BindAddress { get; set; } = "0.0.0.0";
        public string Motd { get; set; } = "Welcome to HELLO_MY_WORLD!";
        public bool EnableWhitelist { get; set; } = false;
        public bool EnablePvp { get; set; } = true;
        public bool EnableCommandBlocks { get; set; } = false;
    }

    public sealed class WorldConfiguration
    {
        public string WorldName { get; set; } = "world";
        public long Seed { get; set; } = 0;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;
        public int SeaLevel { get; set; } = 62;
        public bool EnableCaves { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public string MapControlProfilePath { get; set; } = "config/world_map_control_profile.json";
    }

    public sealed class GameplayConfiguration
    {
        public bool EnableSurvival { get; set; } = true;
        public bool EnableCreative { get; set; } = false;
        public int RespawnCooldownSeconds { get; set; } = 5;
        public int MaxInventorySlots { get; set; } = 36;
    }

    public sealed class NetworkConfiguration
    {
        public ConnectionSettings ConnectionSettings { get; set; } = new();
        public RateLimitSettings RateLimit { get; set; } = new();
    }

    public sealed class ConnectionSettings
    {
        public int ProtocolVersion { get; set; } = 1;
        public int MaxPacketSize { get; set; } = 262_144;
        public bool EnableCompression { get; set; } = true;
        public int CompressionThreshold { get; set; } = 512;
        public bool EnableEncryption { get; set; } = false;
    }

    public sealed class RateLimitSettings
    {
        public int MaxPacketsPerSecond { get; set; } = 120;
        public int BurstSize { get; set; } = 24;
        public int MaxQueuedPackets { get; set; } = 1024;
    }

    public sealed class PerformanceConfiguration
    {
        public int MaxConcurrentChunkGenerations { get; set; } = 4;
        public int MaxWorldThreads { get; set; } = 2;
        public int ChunkSaveIntervalSeconds { get; set; } = 30;
        public int MetricsFlushIntervalSeconds { get; set; } = 60;
    }

    public sealed class SecurityConfiguration
    {
        public bool RequireAuthentication { get; set; } = true;
        public bool EnableCommandValidation { get; set; } = true;
        public bool EnableAntiCheat { get; set; } = true;
    }

    public sealed class DatabaseConfiguration
    {
        public string Type { get; set; } = "sqlite";
        public string ConnectionString { get; set; } = "Data Source=minecraft_game.db";
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 50;
        public int MinPoolSize { get; set; } = 2;
    }

    public sealed class LoggingConfiguration
    {
        public string LogLevel { get; set; } = "info";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "logs";
        public string LogFileName { get; set; } = "server-{Date}.log";
        public int MaxLogFiles { get; set; } = 10;
    }

    public sealed class TerrainGenerationSettings
    {
        public int Seed { get; set; } = 1337;
        public double Scale { get; set; } = 0.005;
        public double Persistence { get; set; } = 0.5;
        public double Lacunarity { get; set; } = 2.0;
        public int Octaves { get; set; } = 4;
        public double Offset { get; set; } = 0.0;
        public double Exponent { get; set; } = 1.15;
        public int WaterLevel { get; set; } = 62;
        public int BeachLevel { get; set; } = 64;
        public int GrassLevel { get; set; } = 68;
        public int StoneLevel { get; set; } = 80;
        public int MinCaveHeight { get; set; } = 12;
        public int MinRiverHeight { get; set; } = 50;
        public int MinLakeHeight { get; set; } = 60;
        public double RiverThreshold { get; set; } = 0.0125;
        public double LakeThreshold { get; set; } = 0.55;
        public double CaveThreshold { get; set; } = 0.42;
        public double RiverPriority { get; set; } = 1.0;
        public double LakePriority { get; set; } = 0.85;
        public double CavePriority { get; set; } = 0.75;
    }

    public sealed class CaveGenerationSettings
    {
        public int Seed { get; set; } = 424242;
        public int MinCavesPerChunk { get; set; } = 1;
        public int MaxCavesPerChunk { get; set; } = 3;
        public int MinCaveDepth { get; set; } = 8;
        public int MaxCaveDepth { get; set; } = 56;
        public int MinCaveRadius { get; set; } = 2;
        public int MaxCaveRadius { get; set; } = 5;
        public double CaveComplexityFactor { get; set; } = 0.6;
        public int MinChambersPerChunk { get; set; } = 0;
        public int MaxChambersPerChunk { get; set; } = 2;
        public int MinChamberRadius { get; set; } = 3;
        public int MaxChamberRadius { get; set; } = 6;
        public double CaveConnectionDistance { get; set; } = 18.0;
        public double CaveConnectionProbability { get; set; } = 0.35;
        public double CaveDecorationDensity { get; set; } = 0.08;
    }

    public sealed class RiverGenerationSettings
    {
        public int Seed { get; set; } = 7777;
        public int MinRiverWidth { get; set; } = 3;
        public int MaxRiverWidth { get; set; } = 8;
        public int MinFlowRate { get; set; } = 8;
        public int MaxFlowRate { get; set; } = 18;
        public double MeanderIntensity { get; set; } = 0.35;
        public double TributaryProbability { get; set; } = 0.15;
        public int MaxRiverLength { get; set; } = 960;
        public int RiverSegmentLength { get; set; } = 8;
        public double RiverConnectionDistance { get; set; } = 32.0;
        public double RiverConnectionProbability { get; set; } = 0.2;
        public double RiverSourceThreshold { get; set; } = 0.62;
    }

    public sealed class LakeGenerationSettings
    {
        public int Seed { get; set; } = 9898;
        public int MinLakeRadius { get; set; } = 6;
        public int MaxLakeRadius { get; set; } = 14;
        public int MinLakeDepth { get; set; } = 2;
        public int MaxLakeDepth { get; set; } = 7;
        public double IslandThreshold { get; set; } = 11.0;
        public int MaxIslandsPerLake { get; set; } = 2;
        public int MinIslandRadius { get; set; } = 2;
        public int MaxIslandRadius { get; set; } = 5;
        public int MinLilyPadsPerLake { get; set; } = 6;
        public int MaxLilyPadsPerLake { get; set; } = 14;
        public int MinReedsPerLake { get; set; } = 4;
        public int MaxReedsPerLake { get; set; } = 9;
        public int MinReedHeight { get; set; } = 2;
        public int MaxReedHeight { get; set; } = 4;
        public double RiverConnectionProbability { get; set; } = 0.25;
        public int MinRiverWidth { get; set; } = 2;
        public int MaxRiverWidth { get; set; } = 4;
        public double LakeGenerationThreshold { get; set; } = 0.6;
    }

    public sealed class WorldMapControlSettings
    {
        public int Seed { get; set; } = 13371337;
        public int ViewDistance { get; set; } = 10;
        public int MaxConcurrentChunkGenerations { get; set; } = 4;
        public int UpdateBatchSize { get; set; } = 24;
        public int UpdateIntervalMs { get; set; } = 100;
        public int DefaultRenderDistance { get; set; } = 10;
        public double DefaultMapScale { get; set; } = 1.0;
        public bool DefaultShowCoordinates { get; set; } = true;
        public bool DefaultShowBiomeInfo { get; set; } = true;
        public int DefaultTerrainQuality { get; set; } = 2;
        public int DefaultWaterQuality { get; set; } = 2;
        public int DefaultVegetationQuality { get; set; } = 2;
        public bool DefaultFogEnabled { get; set; } = true;
        public bool DefaultShadowEnabled { get; set; } = true;
        public int DefaultMaxChunkUpdatesPerFrame { get; set; } = 12;
        public int DefaultChunkLOD { get; set; } = 2;
        public int DefaultUnloadDistance { get; set; } = 12;
        public int MaxCachedChunks { get; set; } = 0;
        public int MaxQueuedChunkRequests { get; set; } = 2048;
        public int QueuePressureFactor { get; set; } = 2;
        public double QueueSlackRatio { get; set; } = 2.0;
        public double QueueBurstSlackMultiplier { get; set; } = 1.15;
        public double QueueLoadSheddingThreshold { get; set; } = 0.88;
        public double QueueEmergencyBrakeThreshold { get; set; } = 1.15;
        public double QueueLoadEmaBlend { get; set; } = 0.28;
        public double QueueEmergencyReleaseRatio { get; set; } = 0.84;
        public double QueueTrendBoostWeight { get; set; } = 0.22;
        public double QueueShockAbsorberWeight { get; set; } = 0.24;
        public int QueueOverloadDrainFactor { get; set; } = 2;
        public int QueueBackoffDelayMs { get; set; } = 4;
        public int QueueEmergencyHoldTicks { get; set; } = 8;
        public int QueueRecoveryRampTicks { get; set; } = 10;
        public int QueueNearChunkKeepCount { get; set; } = 24;
        public int QueueStalePruneMax { get; set; } = 48;
        public double QueueStalePruneEmergencyMultiplier { get; set; } = 1.35;
        public double QueueAlluvialRelayWeight { get; set; } = 0.82;
        public double QueueKarstSpillwayWeight { get; set; } = 0.92;
        public double QueueHyporheicExchangeWeight { get; set; } = 0.94;
        public double QueueHotspotBias { get; set; } = 0.42;
        public double QueueHotspotEmergencyPenalty { get; set; } = 1.0;
        public int QueueHotspotRetentionSeconds { get; set; } = 18;
        public int InflightChunkTimeoutSeconds { get; set; } = 45;
        public int InflightPruneIntervalSeconds { get; set; } = 2;
    }
}
