using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameCommon.World;

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
        public string HydrologySignature { get; set; } = SharedFeatureCatalog.HydrologySignature;

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
        public double HydrologySeamRelaxBlend { get; set; }
        public double HydrologyEdgeFluxBlend { get; set; }
        public double HydrologyEdgeVarianceClamp { get; set; }
        public double HydrologySmoothBlend { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public double HydrologyShorePush { get; set; }
        public double HydrologySlopePenalty { get; set; }
        public double HydrologyFlowGain { get; set; }
        public double HydrologyFlowShadowWeight { get; set; }
        public double HydrologyFlowShadowSlopeWeight { get; set; }
        public double HydrologyEdgeNormalizationBlend { get; set; }
        public int HydrologyEdgeNormalizationIterations { get; set; }
        public double HydrologyFlowMemoryWeight { get; set; }
        public double HydrologyContinuityWeight { get; set; }
        public double HydrologyPressureBlend { get; set; }
        public double HydrologyPressureGradientClamp { get; set; }
        public double HydrologyEdgeFlowBias { get; set; }
        public double HydrologyEdgeTangentWeight { get; set; }
        public double HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public double HydrologyEdgeStabilityWeight { get; set; }
        public double HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public double HydrologyWaterTableSlopeWeight { get; set; }
        public double HydrologyFlowPersistence { get; set; }
        public double HydrologyGradientWeight { get; set; }
        public double HydrologyGradientSlopeWeight { get; set; }
        public double HydrologyGradientClamp { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public double HydrologyDirectionalBlend { get; set; }
        public double HydrologyFlowDivergenceClamp { get; set; }
        public double HydrologyWarpFrequency { get; set; }
        public double HydrologyWarpAmplitude { get; set; }
        public int RiparianSmoothIterations { get; set; }
        public double RiparianSmoothBlend { get; set; }
        public double RiparianSaturationBoost { get; set; }
        public int RiparianBufferRadius { get; set; }
        public double RiverCenterThreshold { get; set; }
        public double RiverBankThreshold { get; set; }
        public int RiverDepth { get; set; }
        public double RiverNoiseScale { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public double RiverIntensitySmoothBlend { get; set; }
        public double RiverConfluenceBoost { get; set; }
        public double RiverFlowAlignmentWeight { get; set; }
        public double RiverGradientPenalty { get; set; }
        public double RiverHeadwaterStabilityWeight { get; set; }
        public double RiverAnisotropyWeight { get; set; }
        public double RiverMeanderJitter { get; set; }
        public double RiverReliefPenaltyWeight { get; set; }
        public double RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public double RiverDeltaWetlandStrength { get; set; }
        public double RiverSeamFillStrength { get; set; }
        public double RiverBankErosionWeight { get; set; }
        public double LakeSpawnWeightBias { get; set; }
        public double LakeShorelineBlend { get; set; }
        public double LakeWetlandSaturationThreshold { get; set; }
        public int LakeOutflowCarveDepth { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int LakeShelfDepth { get; set; }
        public int LakeMaxRadius { get; set; }
        public int LakeWetlandBufferRadius { get; set; }
        public double LakeRiverProximitySuppression { get; set; }
        public double LakeInflowBlendWeight { get; set; }
        public double LakeRimErosionWeight { get; set; }
        public double LakeFlowSeepageWeight { get; set; }
        public double LakeVarianceWeight { get; set; }
        public double LakeOutflowStabilityWeight { get; set; }
        public double CaveEdgeSealStrength { get; set; }
        public double SupportPillarChance { get; set; }
        public int CaveStabilitySmoothIterations { get; set; }
        public double CaveStabilitySmoothBlend { get; set; }
        public double CaveSupportDensity { get; set; }
        public double CaveSupportHydrationBias { get; set; }
        public double CaveSupportFlowBias { get; set; }
        public double CaveMoistureRetentionWeight { get; set; }
        public int CaveRiparianPlugDepth { get; set; }
        public double CaveCeilingStabilityWeight { get; set; }
        public double CaveHydrologyWeight { get; set; }
        public double CaveFlowWeight { get; set; }
        public double CaveRoughnessWeight { get; set; }
        public double CaveDepthWeight { get; set; }
        public double CaveRiverSuppressionWeight { get; set; }
        public double CaveCeilingMoistureClamp { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }

        public static WorldMapControlProfile Create(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            int chunkSize = Math.Max(1, config.ChunkSize);
            int renderDistance = Math.Max(config.RenderDistance, Math.Max(1, worldSettings.ChunkLoadRadius));
            int simulationDistance = Math.Max(config.SimulationDistance, Math.Max(1, renderDistance - 2));
            double caveWeightTotal = Math.Clamp(
                config.Caves.HydrologyStabilityWeight + config.Caves.FlowStabilityWeight + config.Caves.RoughnessStabilityWeight,
                0.0,
                1.0);
            double caveDepthWeight = Math.Clamp(1.0 - caveWeightTotal, 0.05, 0.45);

            var profile = new WorldMapControlProfile
            {
                Version = Math.Max(1, config.MapControlProfileVersion),
                SourceConfig = config.SourcePath,
                GeneratedAtUtc = DateTime.UtcNow,
                ChunkSize = chunkSize,
                RenderDistance = renderDistance,
                SimulationDistance = simulationDistance,
                HydrologySignature = SharedFeatureCatalog.HydrologySignature,
                GlobalWaterLevel = Math.Max(0, config.Water.GlobalWaterLevel),
                HydrologyGradientStabilityIterations = Math.Max(0, config.Water.HydrologyGradientStabilityIterations),
                HydrologyGradientStabilityBlend = config.Water.HydrologyGradientStabilityBlend,
                HydrologyCurvatureWeight = config.Water.HydrologyCurvatureWeight,
                HydrologyEdgeBlendRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius),
                HydrologyVarianceBlend = config.Water.HydrologyVarianceBlend,
                HydrologyVarianceClamp = config.Water.HydrologyVarianceClamp,
                HydrologySeamRelaxIterations = Math.Max(0, config.Water.HydrologySeamRelaxIterations),
                HydrologySeamRelaxBlend = config.Water.HydrologySeamRelaxBlend,
                HydrologyEdgeFluxBlend = config.Water.HydrologyEdgeFluxBlend,
                HydrologyEdgeVarianceClamp = config.Water.HydrologyEdgeVarianceClamp,
                HydrologySmoothBlend = config.Water.HydrologySmoothBlend,
                HydrologySmoothIterations = Math.Max(0, config.Water.HydrologySmoothIterations),
                HydrologyShorePush = config.Water.HydrologyShorePush,
                HydrologySlopePenalty = config.Water.HydrologySlopePenalty,
                HydrologyFlowGain = config.Water.HydrologyFlowGain,
                HydrologyFlowShadowWeight = config.Water.HydrologyFlowShadowWeight,
                HydrologyFlowShadowSlopeWeight = config.Water.HydrologyFlowShadowSlopeWeight,
                HydrologyEdgeNormalizationBlend = config.Water.HydrologyEdgeNormalizationBlend,
                HydrologyEdgeNormalizationIterations = Math.Max(0, config.Water.HydrologyEdgeNormalizationIterations),
                HydrologyFlowMemoryWeight = config.Water.HydrologyFlowMemoryWeight,
                HydrologyContinuityWeight = config.Water.HydrologyContinuityWeight,
                HydrologyPressureBlend = config.Water.HydrologyPressureBlend,
                HydrologyPressureGradientClamp = config.Water.HydrologyPressureGradientClamp,
                HydrologyEdgeFlowBias = config.Water.HydrologyEdgeFlowBias,
                HydrologyEdgeTangentWeight = config.Water.HydrologyEdgeTangentWeight,
                HydrologyEdgeFlowLockWeight = config.Water.HydrologyEdgeFlowLockWeight,
                HydrologyEdgeStabilityIterations = Math.Max(0, config.Water.HydrologyEdgeStabilityIterations),
                HydrologyEdgeStabilityWeight = config.Water.HydrologyEdgeStabilityWeight,
                HydrologyWaterTableClampWeight = config.Water.HydrologyWaterTableClampWeight,
                HydrologyWaterTableClampRange = Math.Max(1, config.Water.HydrologyWaterTableClampRange),
                HydrologyWaterTableSlopeWeight = config.Water.HydrologyWaterTableSlopeWeight,
                HydrologyFlowPersistence = config.Water.HydrologyFlowPersistence,
                HydrologyGradientWeight = config.Water.HydrologyGradientWeight,
                HydrologyGradientSlopeWeight = config.Water.HydrologyGradientSlopeWeight,
                HydrologyGradientClamp = config.Water.HydrologyGradientClamp,
                HydrologyDirectionalIterations = Math.Max(0, config.Water.HydrologyDirectionalIterations),
                HydrologyDirectionalBlend = config.Water.HydrologyDirectionalBlend,
                HydrologyFlowDivergenceClamp = config.Water.HydrologyFlowDivergenceClamp,
                HydrologyWarpFrequency = config.Water.HydrologyWarpFrequency,
                HydrologyWarpAmplitude = config.Water.HydrologyWarpAmplitude,
                RiparianSmoothIterations = Math.Max(0, config.Water.RiparianSmoothIterations),
                RiparianSmoothBlend = config.Water.RiparianSmoothBlend,
                RiparianSaturationBoost = config.Water.RiparianSaturationBoost,
                RiparianBufferRadius = Math.Max(0, config.Water.RiparianBufferRadius),
                RiverCenterThreshold = config.Water.RiverCenterThreshold,
                RiverBankThreshold = config.Water.RiverBankThreshold,
                RiverDepth = Math.Max(1, config.Water.RiverDepth),
                RiverNoiseScale = config.Water.RiverNoiseScale,
                RiverIntensitySmoothIterations = Math.Max(1, config.Water.RiverIntensitySmoothIterations),
                RiverIntensitySmoothBlend = config.Water.RiverIntensitySmoothBlend,
                RiverConfluenceBoost = config.Water.RiverConfluenceBoost,
                RiverFlowAlignmentWeight = config.Water.RiverFlowAlignmentWeight,
                RiverGradientPenalty = config.Water.RiverGradientPenalty,
                RiverHeadwaterStabilityWeight = config.Water.RiverHeadwaterStabilityWeight,
                RiverAnisotropyWeight = config.Water.RiverAnisotropyWeight,
                RiverMeanderJitter = config.Water.RiverMeanderJitter,
                RiverReliefPenaltyWeight = config.Water.RiverReliefPenaltyWeight,
                RiverEdgeFeather = config.Water.RiverEdgeFeather,
                RiverMouthSmoothRadius = Math.Max(1, config.Water.RiverMouthSmoothRadius),
                RiverDeltaWetlandStrength = config.Water.RiverDeltaWetlandStrength,
                RiverSeamFillStrength = config.Water.RiverSeamFillStrength,
                RiverBankErosionWeight = config.Water.RiverBankErosionWeight,
                LakeSpawnWeightBias = config.Lakes.SpawnWeightBias,
                LakeShorelineBlend = config.Lakes.ShorelineBlend,
                LakeWetlandSaturationThreshold = config.Lakes.WetlandSaturationThreshold,
                LakeOutflowCarveDepth = Math.Max(1, config.Lakes.OutflowCarveDepth),
                LakeBasinSmoothIterations = Math.Max(0, config.Lakes.LakeBasinSmoothIterations),
                LakeShelfDepth = Math.Max(0, config.Lakes.ShelfDepth),
                LakeMaxRadius = Math.Max(1, config.Lakes.MaxRadius),
                LakeWetlandBufferRadius = Math.Max(0, config.Lakes.WetlandBufferRadius),
                LakeRiverProximitySuppression = config.Lakes.RiverProximitySuppression,
                LakeInflowBlendWeight = config.Water.LakeInflowBlendWeight,
                LakeRimErosionWeight = config.Water.LakeRimErosionWeight,
                LakeFlowSeepageWeight = config.Lakes.FlowSeepageWeight,
                LakeVarianceWeight = config.Lakes.VarianceWeight,
                LakeOutflowStabilityWeight = config.Lakes.OutflowStabilityWeight,
                CaveEdgeSealStrength = config.Caves.EdgeSealStrength,
                SupportPillarChance = config.Caves.SupportPillarChance,
                CaveStabilitySmoothIterations = Math.Max(0, config.Caves.StabilitySmoothIterations),
                CaveStabilitySmoothBlend = config.Caves.StabilitySmoothBlend,
                CaveSupportDensity = config.Caves.SupportDensity,
                CaveSupportHydrationBias = config.Caves.SupportHydrationBias,
                CaveSupportFlowBias = config.Caves.SupportFlowBias,
                CaveMoistureRetentionWeight = config.Caves.MoistureRetentionWeight,
                CaveRiparianPlugDepth = Math.Max(0, config.Caves.RiparianPlugDepth),
                CaveCeilingStabilityWeight = config.Caves.CeilingStabilityWeight,
                CaveHydrologyWeight = config.Caves.HydrologyStabilityWeight,
                CaveFlowWeight = config.Caves.FlowStabilityWeight,
                CaveRoughnessWeight = config.Caves.RoughnessStabilityWeight,
                CaveDepthWeight = caveDepthWeight,
                CaveRiverSuppressionWeight = config.Caves.RiverSuppressionWeight,
                CaveCeilingMoistureClamp = config.Caves.CeilingMoistureClamp,
                EnableRivers = config.Water.EnableRivers,
                EnableLakes = config.Water.EnableLakes,
                EnableCaves = config.Caves.EnableCaves,
                UseImprovedCaves = config.Caves.UseImprovedCaves,
                UseImprovedRivers = config.Water.UseImprovedRivers,
                UseImprovedLakes = config.Water.UseImprovedLakes
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
                .Append(string.IsNullOrWhiteSpace(profile.HydrologySignature) ? "default" : profile.HydrologySignature).Append('|')
                .Append(profile.GlobalWaterLevel).Append('|')
                .Append(profile.HydrologyGradientStabilityIterations).Append('|')
                .Append(profile.HydrologyGradientStabilityBlend).Append('|')
                .Append(profile.HydrologyCurvatureWeight).Append('|')
                .Append(profile.HydrologyEdgeBlendRadius).Append('|')
                .Append(profile.HydrologyVarianceBlend).Append('|')
                .Append(profile.HydrologyVarianceClamp).Append('|')
                .Append(profile.HydrologySeamRelaxIterations).Append('|')
                .Append(profile.HydrologySeamRelaxBlend).Append('|')
                .Append(profile.HydrologyEdgeFluxBlend).Append('|')
                .Append(profile.HydrologyEdgeVarianceClamp).Append('|')
                .Append(profile.HydrologySmoothBlend).Append('|')
                .Append(profile.HydrologySmoothIterations).Append('|')
                .Append(profile.HydrologyShorePush).Append('|')
                .Append(profile.HydrologySlopePenalty).Append('|')
                .Append(profile.HydrologyFlowGain).Append('|')
                .Append(profile.HydrologyFlowShadowWeight).Append('|')
                .Append(profile.HydrologyFlowShadowSlopeWeight).Append('|')
                .Append(profile.HydrologyEdgeNormalizationBlend).Append('|')
                .Append(profile.HydrologyEdgeNormalizationIterations).Append('|')
                .Append(profile.HydrologyFlowMemoryWeight).Append('|')
                .Append(profile.HydrologyContinuityWeight).Append('|')
                .Append(profile.HydrologyPressureBlend).Append('|')
                .Append(profile.HydrologyPressureGradientClamp).Append('|')
                .Append(profile.HydrologyEdgeFlowBias).Append('|')
                .Append(profile.HydrologyEdgeTangentWeight).Append('|')
                .Append(profile.HydrologyEdgeFlowLockWeight).Append('|')
                .Append(profile.HydrologyEdgeStabilityIterations).Append('|')
                .Append(profile.HydrologyEdgeStabilityWeight).Append('|')
                .Append(profile.HydrologyWaterTableClampWeight).Append('|')
                .Append(profile.HydrologyWaterTableClampRange).Append('|')
                .Append(profile.HydrologyWaterTableSlopeWeight).Append('|')
                .Append(profile.HydrologyFlowPersistence).Append('|')
                .Append(profile.HydrologyGradientWeight).Append('|')
                .Append(profile.HydrologyGradientSlopeWeight).Append('|')
                .Append(profile.HydrologyGradientClamp).Append('|')
                .Append(profile.HydrologyDirectionalIterations).Append('|')
                .Append(profile.HydrologyDirectionalBlend).Append('|')
                .Append(profile.HydrologyFlowDivergenceClamp).Append('|')
                .Append(profile.HydrologyWarpFrequency).Append('|')
                .Append(profile.HydrologyWarpAmplitude).Append('|')
                .Append(profile.RiparianSmoothIterations).Append('|')
                .Append(profile.RiparianSmoothBlend).Append('|')
                .Append(profile.RiparianSaturationBoost).Append('|')
                .Append(profile.RiparianBufferRadius).Append('|')
                .Append(profile.RiverCenterThreshold).Append('|')
                .Append(profile.RiverBankThreshold).Append('|')
                .Append(profile.RiverDepth).Append('|')
                .Append(profile.RiverNoiseScale).Append('|')
                .Append(profile.RiverIntensitySmoothIterations).Append('|')
                .Append(profile.RiverIntensitySmoothBlend).Append('|')
                .Append(profile.RiverConfluenceBoost).Append('|')
                .Append(profile.RiverFlowAlignmentWeight).Append('|')
                .Append(profile.RiverGradientPenalty).Append('|')
                .Append(profile.RiverHeadwaterStabilityWeight).Append('|')
                .Append(profile.RiverAnisotropyWeight).Append('|')
                .Append(profile.RiverMeanderJitter).Append('|')
                .Append(profile.RiverReliefPenaltyWeight).Append('|')
                .Append(profile.RiverEdgeFeather).Append('|')
                .Append(profile.RiverMouthSmoothRadius).Append('|')
                .Append(profile.RiverDeltaWetlandStrength).Append('|')
                .Append(profile.RiverSeamFillStrength).Append('|')
                .Append(profile.RiverBankErosionWeight).Append('|')
                .Append(profile.LakeSpawnWeightBias).Append('|')
                .Append(profile.LakeShorelineBlend).Append('|')
                .Append(profile.LakeWetlandSaturationThreshold).Append('|')
                .Append(profile.LakeOutflowCarveDepth).Append('|')
                .Append(profile.LakeBasinSmoothIterations).Append('|')
                .Append(profile.LakeShelfDepth).Append('|')
                .Append(profile.LakeMaxRadius).Append('|')
                .Append(profile.LakeWetlandBufferRadius).Append('|')
                .Append(profile.LakeRiverProximitySuppression).Append('|')
                .Append(profile.LakeInflowBlendWeight).Append('|')
                .Append(profile.LakeRimErosionWeight).Append('|')
                .Append(profile.LakeFlowSeepageWeight).Append('|')
                .Append(profile.LakeVarianceWeight).Append('|')
                .Append(profile.LakeOutflowStabilityWeight).Append('|')
                .Append(profile.CaveEdgeSealStrength).Append('|')
                .Append(profile.SupportPillarChance).Append('|')
                .Append(profile.CaveStabilitySmoothIterations).Append('|')
                .Append(profile.CaveStabilitySmoothBlend).Append('|')
                .Append(profile.CaveSupportDensity).Append('|')
                .Append(profile.CaveSupportHydrationBias).Append('|')
                .Append(profile.CaveSupportFlowBias).Append('|')
                .Append(profile.CaveRiparianPlugDepth).Append('|')
                .Append(profile.CaveCeilingStabilityWeight).Append('|')
                .Append(profile.CaveMoistureRetentionWeight).Append('|')
                .Append(profile.CaveHydrologyWeight).Append('|')
                .Append(profile.CaveFlowWeight).Append('|')
                .Append(profile.CaveRoughnessWeight).Append('|')
                .Append(profile.CaveDepthWeight).Append('|')
                .Append(profile.CaveRiverSuppressionWeight).Append('|')
                .Append(profile.CaveCeilingMoistureClamp).Append('|')
                .Append(profile.EnableRivers).Append('|')
                .Append(profile.EnableLakes).Append('|')
                .Append(profile.EnableCaves).Append('|')
                .Append(profile.UseImprovedCaves).Append('|')
                .Append(profile.UseImprovedRivers).Append('|')
                .Append(profile.UseImprovedLakes);

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

        public static WorldMapControlProfile? Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var json = File.ReadAllText(path);
                var profile = JsonSerializer.Deserialize<WorldMapControlProfile>(json, options);
                if (profile != null)
                {
                    if (string.IsNullOrWhiteSpace(profile.HydrologySignature))
                    {
                        profile.HydrologySignature = SharedFeatureCatalog.HydrologySignature;
                    }

                    if (string.IsNullOrWhiteSpace(profile.ProfileHash))
                    {
                        profile.ProfileHash = ComputeHash(profile);
                    }
                }

                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlProfile] Failed to read '{path}': {ex.Message}");
                return null;
            }
        }

        public static WorldMapControlProfile LoadOrCreate(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            var generated = WorldMapControlProfile.Create(config, worldSettings);
            var existing = Load(config.MapControlProfilePath);

            if (existing != null &&
                string.Equals(existing.ProfileHash, generated.ProfileHash, StringComparison.OrdinalIgnoreCase) &&
                existing.Version >= generated.Version)
            {
                return existing;
            }

            Save(generated, config.MapControlProfilePath);
            return generated;
        }
    }
}
