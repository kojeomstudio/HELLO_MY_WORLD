using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApp.World
{
    /// <summary>
    /// Data-driven snapshot for world map control so server and client hydrology/cave previews stay aligned.
    /// Serialized to JSON for parity with Unity StreamingAssets.
    /// </summary>
    public sealed class WorldMapControlProfile
    {
        public int Version { get; set; }
        public string ProfileHash { get; set; } = string.Empty;
        public string SourceConfig { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }

        public int ChunkSize { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public int GlobalWaterLevel { get; set; }
        public int HydrologyGradientStabilityIterations { get; set; }
        public double HydrologyGradientStabilityBlend { get; set; }
        public double HydrologyCurvatureWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public double HydrologyVarianceBlend { get; set; }
        public double HydrologyVarianceClamp { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public double HydrologyEdgeFluxBlend { get; set; }
        public double HydrologySmoothBlend { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public double RiverCenterThreshold { get; set; }
        public double RiverBankThreshold { get; set; }
        public int RiverDepth { get; set; }
        public double RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public double RiverDeltaWetlandStrength { get; set; }
        public double LakeSpawnWeightBias { get; set; }
        public double LakeShorelineBlend { get; set; }
        public double LakeWetlandSaturationThreshold { get; set; }
        public int LakeOutflowCarveDepth { get; set; }
        public double CaveEdgeSealStrength { get; set; }
        public double SupportPillarChance { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
        public int LakeBasinSmoothIterations { get; set; }

        public static WorldMapControlProfile Create(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            int chunkSize = Math.Max(1, config.ChunkSize);
            int renderDistance = Math.Max(config.RenderDistance, Math.Max(1, worldSettings.ChunkLoadRadius));
            int simulationDistance = Math.Max(config.SimulationDistance, Math.Max(1, renderDistance - 2));

            var profile = new WorldMapControlProfile
            {
                Version = Math.Max(1, config.MapControlProfileVersion),
                SourceConfig = config.SourcePath,
                GeneratedAtUtc = DateTime.UtcNow,
                ChunkSize = chunkSize,
                RenderDistance = renderDistance,
                SimulationDistance = simulationDistance,
                GlobalWaterLevel = Math.Max(0, config.Water.GlobalWaterLevel),
                HydrologyGradientStabilityIterations = Math.Max(0, config.Water.HydrologyGradientStabilityIterations),
                HydrologyGradientStabilityBlend = config.Water.HydrologyGradientStabilityBlend,
                HydrologyCurvatureWeight = config.Water.HydrologyCurvatureWeight,
                HydrologyEdgeBlendRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius),
                HydrologyVarianceBlend = config.Water.HydrologyVarianceBlend,
                HydrologyVarianceClamp = config.Water.HydrologyVarianceClamp,
                HydrologySeamRelaxIterations = Math.Max(0, config.Water.HydrologySeamRelaxIterations),
                HydrologyEdgeFluxBlend = config.Water.HydrologyEdgeFluxBlend,
                HydrologySmoothBlend = config.Water.HydrologySmoothBlend,
                HydrologySmoothIterations = Math.Max(0, config.Water.HydrologySmoothIterations),
                RiverCenterThreshold = config.Water.RiverCenterThreshold,
                RiverBankThreshold = config.Water.RiverBankThreshold,
                RiverDepth = Math.Max(1, config.Water.RiverDepth),
                RiverEdgeFeather = config.Water.RiverEdgeFeather,
                RiverMouthSmoothRadius = Math.Max(1, config.Water.RiverMouthSmoothRadius),
                RiverDeltaWetlandStrength = config.Water.RiverDeltaWetlandStrength,
                LakeSpawnWeightBias = config.Lakes.SpawnWeightBias,
                LakeShorelineBlend = config.Lakes.ShorelineBlend,
                LakeWetlandSaturationThreshold = config.Lakes.WetlandSaturationThreshold,
                LakeOutflowCarveDepth = Math.Max(1, config.Lakes.OutflowCarveDepth),
                CaveEdgeSealStrength = config.Caves.EdgeSealStrength,
                SupportPillarChance = config.Caves.SupportPillarChance,
                EnableRivers = config.Water.EnableRivers,
                EnableLakes = config.Water.EnableLakes,
                EnableCaves = config.Caves.EnableCaves,
                UseImprovedCaves = config.Caves.UseImprovedCaves,
                UseImprovedRivers = config.Water.UseImprovedRivers,
                UseImprovedLakes = config.Water.UseImprovedLakes,
                LakeBasinSmoothIterations = Math.Max(0, config.Lakes.LakeBasinSmoothIterations)
            };

            profile.ProfileHash = WorldMapControlProfileUtility.ComputeHash(profile);
            return profile;
        }
    }

    public static class WorldMapControlProfileUtility
    {
        public static string ComputeHash(WorldMapControlProfile profile)
        {
            var builder = new StringBuilder();
            builder
                .Append(profile.Version).Append('|')
                .Append(profile.ChunkSize).Append('|')
                .Append(profile.RenderDistance).Append('|')
                .Append(profile.SimulationDistance).Append('|')
                .Append(profile.GlobalWaterLevel).Append('|')
                .Append(profile.HydrologyGradientStabilityIterations).Append('|')
                .Append(profile.HydrologyGradientStabilityBlend).Append('|')
                .Append(profile.HydrologyCurvatureWeight).Append('|')
                .Append(profile.HydrologyEdgeBlendRadius).Append('|')
                .Append(profile.HydrologyVarianceBlend).Append('|')
                .Append(profile.HydrologyVarianceClamp).Append('|')
                .Append(profile.HydrologySeamRelaxIterations).Append('|')
                .Append(profile.HydrologyEdgeFluxBlend).Append('|')
                .Append(profile.HydrologySmoothBlend).Append('|')
                .Append(profile.HydrologySmoothIterations).Append('|')
                .Append(profile.RiverCenterThreshold).Append('|')
                .Append(profile.RiverBankThreshold).Append('|')
                .Append(profile.RiverDepth).Append('|')
                .Append(profile.RiverEdgeFeather).Append('|')
                .Append(profile.RiverMouthSmoothRadius).Append('|')
                .Append(profile.RiverDeltaWetlandStrength).Append('|')
                .Append(profile.LakeSpawnWeightBias).Append('|')
                .Append(profile.LakeShorelineBlend).Append('|')
                .Append(profile.LakeWetlandSaturationThreshold).Append('|')
                .Append(profile.LakeOutflowCarveDepth).Append('|')
                .Append(profile.CaveEdgeSealStrength).Append('|')
                .Append(profile.SupportPillarChance).Append('|')
                .Append(profile.EnableRivers).Append('|')
                .Append(profile.EnableLakes).Append('|')
                .Append(profile.EnableCaves).Append('|')
                .Append(profile.UseImprovedCaves).Append('|')
                .Append(profile.UseImprovedRivers).Append('|')
                .Append(profile.UseImprovedLakes).Append('|')
                .Append(profile.LakeBasinSmoothIterations);

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public static void Save(WorldMapControlProfile profile, string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlProfile] Failed to write profile to '{path}': {ex.Message}");
            }
        }
    }
}
