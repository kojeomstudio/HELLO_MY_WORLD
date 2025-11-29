using System;
using System.IO;
using System.Text.Json;

namespace GameServerApp.World
{
    /// <summary>
    /// Data-driven world generation settings shared across terrain, caves, rivers, and lakes.
    /// Values are loaded from config/world.json so both server and client can stay in sync.
    /// </summary>
    public sealed class WorldGenerationConfig
    {
        public string SourcePath { get; set; } = "config/world.json";
        public WaterConfig Water { get; set; } = new();
        public CaveConfig Caves { get; set; } = new();
        public LakeConfig Lakes { get; set; } = new();

        public static WorldGenerationConfig Load(string? configPath)
        {
            var resolvedPath = string.IsNullOrWhiteSpace(configPath) ? "config/world.json" : configPath!;
            var defaults = new WorldGenerationConfig { SourcePath = resolvedPath };

            if (!File.Exists(resolvedPath))
            {
                Console.WriteLine($"[WorldGenConfig] Missing config at '{resolvedPath}', using defaults.");
                return defaults;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                var json = File.ReadAllText(resolvedPath);
                var loaded = JsonSerializer.Deserialize<WorldGenerationConfig>(json, options);
                if (loaded != null)
                {
                    loaded.SourcePath = resolvedPath;
                    loaded.Normalize();
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldGenConfig] Failed to parse '{resolvedPath}': {ex.Message}");
            }

            defaults.Normalize();
            return defaults;
        }

        private void Normalize()
        {
            Water ??= new WaterConfig();
            Caves ??= new CaveConfig();
            Lakes ??= new LakeConfig();
        }
    }

    public sealed class WaterConfig
    {
        public int GlobalWaterLevel { get; set; } = 62;
        public double RiverCenterThreshold { get; set; } = 0.0125;
        public double RiverBankThreshold { get; set; } = 0.028;
        public double RiverNoiseScale { get; set; } = 0.015;
        public int RiverDepth { get; set; } = 5;
        public int HydrologySmoothIterations { get; set; } = 1;
        public double HydrologySmoothBlend { get; set; } = 0.55;
        public double HydrologyShorePush { get; set; } = 5.0;
        public double HydrologySlopePenalty { get; set; } = 6.0;
        public double HydrologyFlowGain { get; set; } = 0.5;
        public double HydrologyContinuityWeight { get; set; } = 0.35;
        public int HydrologyEdgeBlendRadius { get; set; } = 2;
        public double HydrologyFlowPersistence { get; set; } = 0.55;
        public int HydrologySeamRelaxIterations { get; set; } = 2;
        public double HydrologySeamRelaxBlend { get; set; } = 0.45;
        public double RiverBankErosionWeight { get; set; } = 0.18;
        public double LakeRimErosionWeight { get; set; } = 0.25;
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
    }

    public sealed class CaveConfig
    {
        public bool EnableCaves { get; set; } = true;
        public double HorizontalFrequency { get; set; } = 0.0026;
        public double VerticalFrequency { get; set; } = 0.018;
        public double Threshold { get; set; } = 0.42;
        public double LavaThreshold { get; set; } = 0.28;
        public double WaterThreshold { get; set; } = 0.34;
        public double FloodedCaveNoiseFrequency { get; set; } = 0.0031;
        public double FloodedCaveProximityToWaterTableWeight { get; set; } = 0.6;
        public double FloodedCaveThreshold { get; set; } = 0.75;
        public int StabilitySmoothIterations { get; set; } = 1;
        public double StabilitySmoothBlend { get; set; } = 0.55;
    }

    public sealed class LakeConfig
    {
        public int MinDepth { get; set; } = 3;
        public int MaxDepth { get; set; } = 9;
        public int MaxRadius { get; set; } = 9;
        public double SpawnWeightBias { get; set; } = 0.3;
        public double ShorelineBlend { get; set; } = 0.6;
    }
}
