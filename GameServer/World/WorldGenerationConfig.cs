using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApp.World
{
    /// <summary>
    /// Data-driven world generation settings shared across terrain, caves, rivers, and lakes.
    /// Values are loaded from config/world.json so both server and client can stay in sync.
    /// </summary>
    public sealed class WorldGenerationConfig
    {
        public string SourcePath { get; set; } = "config/world.json";
        public string MapControlProfilePath { get; set; } = "config/world_map_control_profile.json";
        public int MapControlProfileVersion { get; set; } = 49;
        public string WorldName { get; set; } = "HELLO_MY_WORLD";
        public long Seed { get; set; } = 0;
        public TerrainGenerationConfig TerrainGeneration { get; set; } = new();
        public int ChunkSize { get; set; } = 16;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;
        public int WorldHeight { get; set; } = 256;
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
            TerrainGeneration ??= new TerrainGenerationConfig();
            ChunkSize = Math.Max(ChunkSize, 1);
            RenderDistance = Math.Max(RenderDistance, 1);
            SimulationDistance = Math.Max(SimulationDistance, 1);
            WorldHeight = Math.Max(WorldHeight, 1);
            MapControlProfilePath = string.IsNullOrWhiteSpace(MapControlProfilePath)
                ? "config/world_map_control_profile.json"
                : MapControlProfilePath;
            MapControlProfileVersion = Math.Max(1, MapControlProfileVersion);
        }
    }

    public sealed class TerrainGenerationConfig
    {
        public int SeaLevel { get; set; } = 62;
        public int BedrockLevel { get; set; } = 5;
        public double NoiseScale { get; set; } = 100.0;
        public double NoiseAmplitude { get; set; } = 50.0;
        public int Octaves { get; set; } = 4;
        public double Persistence { get; set; } = 0.5;
        public double Lacunarity { get; set; } = 2.0;
        public double BiomeScale { get; set; } = 0.005;
        public double TemperatureScale { get; set; } = 0.003;
        public double HumidityScale { get; set; } = 0.004;
        public double MountainThreshold { get; set; } = 0.6;
        public int MountainMaxHeight { get; set; } = 200;
        public int PlainBaseHeight { get; set; } = 64;
    }

    public sealed class WaterConfig
    {
        public int GlobalWaterLevel { get; set; } = 62;
        public double RiverCenterThreshold { get; set; } = 0.0118;
        public double RiverBankThreshold { get; set; } = 0.0245;
        public double RiverNoiseScale { get; set; } = 0.0145;
        public int RiverDepth { get; set; } = 9;
        public int RiverIntensitySmoothIterations { get; set; } = 5;
        public double RiverIntensitySmoothBlend { get; set; } = 0.66;
        public int HydrologyReservoirIterations { get; set; } = 6;
        public double HydrologyReservoirBlend { get; set; } = 0.5;
        public int HydrologySmoothIterations { get; set; } = 6;
        public double HydrologySmoothBlend { get; set; } = 0.68;
        public double HydrologyShorePush { get; set; } = 5.6;
        public double HydrologySlopePenalty { get; set; } = 6.5;
        public double HydrologyFlowGain { get; set; } = 0.68;
        public double HydrologyFlowShadowWeight { get; set; } = 0.64;
        public double HydrologyFlowShadowSlopeWeight { get; set; } = 0.52;
        public double HydrologyContinuityWeight { get; set; } = 0.42;
        public double HydrologyPressureBlend { get; set; } = 0.48;
        public double HydrologyPressureGradientClamp { get; set; } = 0.26;
        public double HydrologyEdgeFlowBias { get; set; } = 0.5;
        public double HydrologyEdgeTangentWeight { get; set; } = 0.54;
        public double HydrologyEdgeFlowLockWeight { get; set; } = 0.56;
        public int HydrologyEdgeBlendRadius { get; set; } = 8;
        public int HydrologyWatershedStitchRadius { get; set; } = 3;
        public double HydrologyWatershedStitchWeight { get; set; } = 0.5;
        public int HydrologyEdgeStabilityIterations { get; set; } = 6;
        public double HydrologyEdgeStabilityWeight { get; set; } = 0.52;
        public double HydrologyEdgeVarianceClamp { get; set; } = 0.22;
        public double HydrologyEdgeFluxBlend { get; set; } = 0.66;
        public double HydrologyVarianceBlend { get; set; } = 0.68;
        public double HydrologyVarianceClamp { get; set; } = 0.58;
        public double HydrologyEdgeNormalizationBlend { get; set; } = 0.58;
        public int HydrologyEdgeNormalizationIterations { get; set; } = 4;
        public double HydrologyFlowMemoryWeight { get; set; } = 0.60;
        public double HydrologyWaterTableClampWeight { get; set; } = 0.69;
        public int HydrologyWaterTableClampRange { get; set; } = 26;
        public double HydrologyWaterTableSlopeWeight { get; set; } = 0.7;
        public double HydrologyFlowPersistence { get; set; } = 0.94;
        public double HydrologyCatchmentWeight { get; set; } = 0.46;
        public double HydrologyGradientWeight { get; set; } = 0.38;
        public double HydrologyGradientSlopeWeight { get; set; } = 0.5;
        public double HydrologyGradientClamp { get; set; } = 1.52;
        public int HydrologyGradientStabilityIterations { get; set; } = 3;
        public double HydrologyGradientStabilityBlend { get; set; } = 0.56;
        public int HydrologyDirectionalIterations { get; set; } = 3;
        public double HydrologyDirectionalBlend { get; set; } = 0.48;
        public double HydrologyFlowDivergenceClamp { get; set; } = 0.48;
        public double HydrologyCurvatureWeight { get; set; } = 0.42;
        public int HydrologySeamRelaxIterations { get; set; } = 6;
        public double HydrologySeamRelaxBlend { get; set; } = 0.64;
        public double RiverBankErosionWeight { get; set; } = 0.22;
        public double LakeRimErosionWeight { get; set; } = 0.54;
        public double RiverReliefPenaltyWeight { get; set; } = 0.4;
        public double HydrologyWarpFrequency { get; set; } = 0.0011;
        public double HydrologyWarpAmplitude { get; set; } = 10.5;
        public int RiparianSmoothIterations { get; set; } = 4;
        public double RiparianSmoothBlend { get; set; } = 0.7;
        public double RiparianSaturationBoost { get; set; } = 0.24;
        public int RiparianBufferRadius { get; set; } = 4;
        public double RiverFlowAlignmentWeight { get; set; } = 0.38;
        public double RiverGradientPenalty { get; set; } = 0.46;
        public double RiverHeadwaterStabilityWeight { get; set; } = 0.42;
        public double RiverAnisotropyWeight { get; set; } = 0.38;
        public double RiverAnisotropyDamping { get; set; } = 0.4;
        public double RiverMeanderJitter { get; set; } = 0.3;
        public double RiverBankStabilityClamp { get; set; } = 0.52;
        public double LakeInflowBlendWeight { get; set; } = 0.64;
        public double RiverConfluenceBoost { get; set; } = 0.78;
        public double RiverTributaryCaptureWeight { get; set; } = 0.46;
        public double RiverAvulsionResistance { get; set; } = 0.52;
        public double RiverBraidingWeight { get; set; } = 0.53;
        public double RiverEdgeFeather { get; set; } = 0.66;
        public double RiverEdgeContinuityWeight { get; set; } = 0.94;
        public int RiverMouthSmoothRadius { get; set; } = 10;
        public double RiverDeltaWetlandStrength { get; set; } = 0.70;
        public double RiverSeamFillStrength { get; set; } = 0.80;
        public bool EnableRivers { get; set; } = true;
        public bool EnableLakes { get; set; } = true;
        public bool UseImprovedRivers { get; set; } = true;
        public bool UseImprovedLakes { get; set; } = true;
    }

    public sealed class CaveConfig
    {
        public bool EnableCaves { get; set; } = true;
        public bool UseImprovedCaves { get; set; } = true;
        public bool UseRegionalMainCaves { get; set; } = true;
        public int RegionalMainCaveRegionSizeChunks { get; set; } = 4;
        public int RegionalMainCaveWormCountMin { get; set; } = 4;
        public int RegionalMainCaveWormCountMax { get; set; } = 9;
        public int RegionalMainCaveStepsMin { get; set; } = 180;
        public int RegionalMainCaveStepsMax { get; set; } = 320;
        public int RegionalMainCaveMinY { get; set; } = 14;
        public int RegionalMainCaveMaxY { get; set; } = 72;
        public double RegionalMainCaveRadiusMin { get; set; } = 1.8;
        public double RegionalMainCaveRadiusMax { get; set; } = 3.2;
        public double HorizontalFrequency { get; set; } = 0.0026;
        public double VerticalFrequency { get; set; } = 0.018;
        public double Threshold { get; set; } = 0.42;

        [JsonPropertyName("NoiseThreshold")]
        public double NoiseThreshold
        {
            get => Threshold;
            set => Threshold = value;
        }

        [JsonPropertyName("CaveThreshold")]
        public double CaveThreshold
        {
            get => Threshold;
            set => Threshold = value;
        }
        public double LavaThreshold { get; set; } = 0.28;
        public double WaterThreshold { get; set; } = 0.34;
        public double FloodedCaveNoiseFrequency { get; set; } = 0.0031;
        public double FloodedCaveProximityToWaterTableWeight { get; set; } = 0.6;
        public double FloodedCaveThreshold { get; set; } = 0.75;
        public int StabilitySmoothIterations { get; set; } = 4;
        public double StabilitySmoothBlend { get; set; } = 0.55;
        public double SupportDensity { get; set; } = 0.62;
        public double HydrologyStabilityWeight { get; set; } = 0.45;
        public double FlowStabilityWeight { get; set; } = 0.25;
        public double RoughnessStabilityWeight { get; set; } = 0.1;
        public double RiverSuppressionWeight { get; set; } = 0.42;
        public double SupportHydrationBias { get; set; } = 0.42;
        public double SupportFlowBias { get; set; } = 0.20;
        public double MoistureRetentionWeight { get; set; } = 0.55;
        public double MoistureFlowClamp { get; set; } = 0.48;
        public double RiparianCaveGuardWeight { get; set; } = 0.64;
        public double AquiferBarrierWeight { get; set; } = 0.72;
        public double EdgeSealStrength { get; set; } = 0.82;
        public double SupportPillarChance { get; set; } = 0.3;
        public int RiparianPlugDepth { get; set; } = 5;
        public double CeilingStabilityWeight { get; set; } = 0.46;
        public double CeilingMoistureWeight { get; set; } = 0.46;
        public double CeilingMoistureClamp { get; set; } = 0.42;
        public double CaveEntranceFlowDampening { get; set; } = 0.80;
        public double GroundwaterConnectivityWeight { get; set; } = 0.58;
        public double CaveVentilationBias { get; set; } = 0.42;
    }

    public sealed class LakeConfig
    {
        public int MinDepth { get; set; } = 3;
        public int MaxDepth { get; set; } = 9;
        public int MaxRadius { get; set; } = 9;
        public int LakeBasinSmoothIterations { get; set; } = 7;
        public double SpawnWeightBias { get; set; } = 0.38;
        public double ShorelineBlend { get; set; } = 0.75;
        public double RiverProximitySuppression { get; set; } = 0.42;
        public double WetlandSaturationThreshold { get; set; } = 0.6;
        public int OutflowCarveDepth { get; set; } = 4;
        public double OutflowSealWeight { get; set; } = 0.56;
        public int ShelfDepth { get; set; } = 3;
        public int WetlandBufferRadius { get; set; } = 6;
        public double FlowSeepageWeight { get; set; } = 0.70;
        public double VarianceWeight { get; set; } = 0.46;
        public double OutflowStabilityWeight { get; set; } = 0.95;
        public double LakeOutflowTaper { get; set; } = 0.74;
        public double SpillwayContinuityWeight { get; set; } = 0.88;
        public double TerraceBiasWeight { get; set; } = 0.4;
        public double SpillRetentionWeight { get; set; } = 0.58;
    }
}
