using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCommon.World
{
    public static class WorldMapControlProfileUtility
    {
        public static string ComputeHash(WorldMapControlProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.EnsureDefaults();
            var builder = new StringBuilder()
                .Append(profile.Version).Append('|')
                .Append(profile.HydrologySignature).Append('|')
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
                .Append(profile.HydrologySeamRelaxBlend).Append('|')
                .Append(profile.HydrologyEdgeFluxBlend).Append('|')
                .Append(profile.HydrologyEdgeVarianceClamp).Append('|')
                .Append(profile.HydrologySmoothBlend).Append('|')
                .Append(profile.HydrologySmoothIterations).Append('|')
                .Append(profile.HydrologyReservoirIterations).Append('|')
                .Append(profile.HydrologyReservoirBlend).Append('|')
                .Append(profile.HydrologyShorePush).Append('|')
                .Append(profile.HydrologySlopePenalty).Append('|')
                .Append(profile.HydrologyFlowGain).Append('|')
                .Append(profile.HydrologyFlowShadowWeight).Append('|')
                .Append(profile.HydrologyFlowShadowSlopeWeight).Append('|')
                .Append(profile.HydrologyEdgeNormalizationBlend).Append('|')
                .Append(profile.HydrologyEdgeNormalizationIterations).Append('|')
                .Append(profile.HydrologyFlowMemoryWeight).Append('|')
                .Append(profile.HydrologyContinuityWeight).Append('|')
                .Append(profile.HydrologyThalwegStabilityWeight).Append('|')
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
                .Append(profile.RiverTributaryCaptureWeight).Append('|')
                .Append(profile.RiverAvulsionResistance).Append('|')
                .Append(profile.RiverFlowAlignmentWeight).Append('|')
                .Append(profile.RiverGradientPenalty).Append('|')
                .Append(profile.RiverHeadwaterStabilityWeight).Append('|')
                .Append(profile.RiverAnisotropyWeight).Append('|')
                .Append(profile.RiverAnisotropyDamping).Append('|')
                .Append(profile.RiverMeanderJitter).Append('|')
                .Append(profile.RiverReliefPenaltyWeight).Append('|')
                .Append(profile.RiverBankStabilityClamp).Append('|')
                .Append(profile.RiverEdgeFeather).Append('|')
                .Append(profile.RiverEdgeContinuityWeight).Append('|')
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
                .Append(profile.LakeOutflowSealWeight).Append('|')
                .Append(profile.LakeFlowSeepageWeight).Append('|')
                .Append(profile.LakeVarianceWeight).Append('|')
                .Append(profile.LakeOutflowStabilityWeight).Append('|')
                .Append(profile.LakeOutflowTaper).Append('|')
                .Append(profile.LakeSpillRetentionWeight).Append('|')
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
                .Append(profile.CaveMoistureFlowClamp).Append('|')
                .Append(profile.CaveEntranceFlowDampening).Append('|')
                .Append(profile.CaveGroundwaterConnectivityWeight).Append('|')
                .Append(profile.CaveVentilationBias).Append('|')
                .Append(profile.CaveHydrologyWeight).Append('|')
                .Append(profile.CaveFlowWeight).Append('|')
                .Append(profile.CaveRoughnessWeight).Append('|')
                .Append(profile.CaveDepthWeight).Append('|')
                .Append(profile.CaveRiverSuppressionWeight).Append('|')
                .Append(profile.RiparianCaveGuardWeight).Append('|')
                .Append(profile.CaveCeilingMoistureClamp).Append('|')
                .Append(profile.EnableRivers).Append('|')
                .Append(profile.EnableLakes).Append('|')
                .Append(profile.EnableCaves).Append('|')
                .Append(profile.UseImprovedCaves).Append('|')
                .Append(profile.UseImprovedRivers).Append('|')
                .Append(profile.UseImprovedLakes);

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        public static void Save(WorldMapControlProfile profile, string path)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.EnsureDefaults();
            if (string.IsNullOrWhiteSpace(profile.ProfileHash))
            {
                profile.ProfileHash = ComputeHash(profile);
            }

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

        public static WorldMapControlProfile? Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var json = File.ReadAllText(path);
                var profile = JsonSerializer.Deserialize<WorldMapControlProfile>(json, options);
                if (profile != null)
                {
                    profile.EnsureDefaults();
                    if (string.IsNullOrWhiteSpace(profile.ProfileHash))
                    {
                        profile.ProfileHash = ComputeHash(profile);
                    }
                }

                return profile;
            }
            catch
            {
                return null;
            }
        }

        public static WorldMapControlProfile LoadOrCreate(
            string path,
            Func<WorldMapControlProfile> factory,
            Func<WorldMapControlProfile, string?>? hashSelector = null,
            int requiredVersion = 1)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var generated = factory();
            generated.EnsureDefaults();
            generated.ProfileHash = string.IsNullOrWhiteSpace(generated.ProfileHash)
                ? ComputeHash(generated)
                : generated.ProfileHash;

            var existing = Load(path);
            if (existing != null)
            {
                var expectedHash = hashSelector?.Invoke(generated) ?? generated.ProfileHash;
                var existingHash = string.IsNullOrWhiteSpace(existing.ProfileHash)
                    ? ComputeHash(existing)
                    : existing.ProfileHash;

                if (existing.Version >= Math.Max(1, requiredVersion) &&
                    string.Equals(existingHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }
            }

            Save(generated, path);
            return generated;
        }
    }
}
