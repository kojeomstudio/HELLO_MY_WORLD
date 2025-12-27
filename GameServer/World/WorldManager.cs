using System;
using System.Collections.Concurrent;
using GameServerApp;
using GameServerApp.Database;
using GameServerApp.Models;
using GameServerApp.World.Generation;
using GameServerApp.World.Generation.Stages;
using System.Numerics;

namespace GameServerApp.World
{
    public partial class WorldManager
    {
        private readonly DatabaseHelper _database;
        private readonly ConcurrentDictionary<string, LoadedChunk> _loadedChunks = new();
        private readonly Random _random;
        private int _worldId;
        private readonly WorldSeedConfig _worldSeed;
        private readonly WorldSettings _worldSettings;
        private readonly WorldGenerationConfig _worldGenConfig;
        private readonly bool _enableCaves;
        private readonly bool _enableRivers;
        private readonly bool _enableLakes;
        private readonly bool _useImprovedCaves;
        private readonly bool _useImprovedRivers;
        private readonly bool _useImprovedLakes;
        private readonly TerrainGenerationPipeline _terrainPipeline;
        private readonly WorldMapControlProfile _mapControlProfile;
        private readonly int _hydrologySmoothIterations;
        private readonly double _hydrologySmoothBlend;
        private readonly double _hydrologyShorePush;
        private readonly double _hydrologySlopePenalty;
        private readonly double _hydrologyFlowGain;
        private readonly double _hydrologyContinuityWeight;
        private readonly double _hydrologyEdgeFlowBias;
        private readonly double _hydrologyEdgeTangentWeight;
        private readonly double _hydrologyEdgeFlowLockWeight;
        private readonly int _hydrologyEdgeBlendRadius;
        private readonly int _hydrologyEdgeStabilityIterations;
        private readonly double _hydrologyEdgeStabilityWeight;
        private readonly double _hydrologyEdgeVarianceClamp;
        private readonly double _hydrologyEdgeFluxBlend;
        private readonly double _hydrologyVarianceBlend;
        private readonly double _hydrologyVarianceClamp;
        private readonly double _hydrologyWaterTableClampWeight;
        private readonly int _hydrologyWaterTableClampRange;
        private readonly double _hydrologyWaterTableSlopeWeight;
        private readonly double _hydrologyFlowPersistence;
        private readonly double _hydrologyGradientWeight;
        private readonly double _hydrologyGradientSlopeWeight;
        private readonly double _hydrologyGradientClamp;
        private readonly int _hydrologyGradientStabilityIterations;
        private readonly double _hydrologyGradientStabilityBlend;
        private readonly int _hydrologyDirectionalIterations;
        private readonly double _hydrologyDirectionalBlend;
        private readonly double _hydrologyFlowDivergenceClamp;
        private readonly double _hydrologyCurvatureWeight;
        private readonly int _hydrologySeamRelaxIterations;
        private readonly double _hydrologySeamRelaxBlend;
        private readonly int _riparianSmoothIterations;
        private readonly double _riparianSmoothBlend;
        private readonly double _riparianSaturationBoost;
        private readonly int _riparianBufferRadius;
        private readonly double _riverBankErosionWeight;
        private readonly double _lakeRimErosionWeight;
        private readonly double _riverNoiseScale;
        private readonly int _riverDepth;
        private readonly int _riverIntensitySmoothIterations;
        private readonly double _riverIntensitySmoothBlend;
        private readonly double _riverConfluenceBoost;
        private readonly double _riverEdgeFeather;
        private readonly int _riverMouthSmoothRadius;
        private readonly double _riverDeltaWetlandStrength;
        private readonly int _caveStabilitySmoothIterations;
        private readonly double _caveStabilitySmoothBlend;
        private readonly double _caveSupportDensity;
        private readonly double _supportPillarChance;
        private readonly double _hydrologyWarpFrequency;
        private readonly double _hydrologyWarpAmplitude;
        private readonly double _caveHydrologyWeight;
        private readonly double _caveFlowWeight;
        private readonly double _caveRoughnessWeight;
        private readonly double _caveDepthWeight;
        private readonly double _caveRiverSuppressionWeight;
        private readonly double _riverFlowAlignmentWeight;
        private readonly double _riverAnisotropyWeight;
        private readonly double _riverGradientPenalty;
        private readonly double _riverHeadwaterStabilityWeight;
        private readonly double _riverReliefPenaltyWeight;
        private readonly double _riverSeamFillStrength;
        private readonly double _caveSupportHydrationBias;
        private readonly double _caveSupportFlowBias;
        private readonly int _caveRiparianPlugDepth;
        private readonly double _lakeRiverProximitySuppression;
        private readonly int _lakeBasinSmoothIterations;
        private readonly int _lakeShelfDepth;
        private readonly double _lakeInflowBlendWeight;
        private readonly double _lakeWetlandSaturationThreshold;
        private readonly int _lakeOutflowCarveDepth;
        private readonly int _lakeWetlandBufferRadius;
        private readonly double _caveMoistureRetentionWeight;
        private readonly double _caveEdgeSealStrength;
        private readonly double _caveCeilingStabilityWeight;

        private static int GlobalWaterLevel = 62;
        private static double RiverCenterThreshold = 0.0125;
        private static double RiverBankThreshold = 0.028;
        private const int CloudBaseAltitude = 200;
        private const double OceanThreshold = 0.36;
        private const double BeachThreshold = 0.42;
        private const int MinSurfaceHeight = 45;
        private const int MaxSurfaceHeight = 150;
        private const double CliffThreshold = 0.55;
        private const double DomainWarpSimplexFrequency = 0.00065;
        private const double DomainWarpPerlinFrequency = 0.0011;
        private const double DomainWarpSimplexAmplitude = 32.0;
        private const double DomainWarpPerlinAmplitude = 18.0;
        private const double ValleyDepthMultiplier = 12.0;
        private const string TerrainProfilesKey = "terrain.profiles";
        private const string RiverFieldCacheKey = "terrain.riverField";
        private const string HydrologyFieldCacheKey = "terrain.hydrology";
        private const string RiparianSaturationCacheKey = "terrain.riparianSaturation";
        private const string RiparianSaturationWithRiverCacheKey = "terrain.riparianSaturation.river";
        private static double NoiseCaveHorizontalFrequency = 0.0026;
        private static double NoiseCaveVerticalFrequency = 0.018;
        private static double NoiseCaveThreshold = 0.42;
        private static double NoiseCaveLavaThreshold = 0.28;
        private static double NoiseCaveWaterThreshold = 0.34;
        private const int SaltCaveMain = 0x6CA5E001;
        private const int SaltCaveRegionalMain = 0x6CA5E021;
        private const int SaltCaveHydro = 0x6CA5E00B;
        private const int SaltCaveDrip = 0x6CA5E00D;
        private const int SaltCaveKarst = 0x6CA5E00F;
        private const int SaltDungeon = 0x6D00D001;
        private const int SaltLake = 0x1A2E0001;
        private const int SaltRiverTributary = 0x5A7B1001;
        private const int SaltRiverSediment = 0x5A7B2001;
        private const int SaltRiverWetland = 0x5A7B3001;
        private const int SaltOre = 0x0F0E0D0C;
        private const int SaltVegetation = 0x0A0B0C0D;

        private struct TerrainProfile
        {
            public int SurfaceHeight;
            public bool HasWater;
            public int WaterLevel;
            public BiomeType Biome;
            public BlockType SurfaceBlock;
            public BlockType SubSurfaceBlock;
            public BlockType FillerBlock;
            public bool UseCliffFace;
        }

        private sealed class HydrologyFieldCache
        {
            public double[,] HydrologyMask { get; set; } = default!;
            public double[,] FlowAccumulation { get; set; } = default!;
            public double[,] ErosionRisk { get; set; } = default!;
            public double[,] HydrologyCurvature { get; set; } = default!;
            public Vector2[,] HydrologyGradient { get; set; } = default!;
        }

        private sealed class RiverFieldCache
        {
            public RiverFieldCache()
            {
                Intensity = new double[16, 16];
                Flow = new Vector2[16, 16];
            }

            public double[,] Intensity { get; }
            public Vector2[,] Flow { get; }
            public bool IsInitialized { get; set; }
        }

        public WorldManager(DatabaseHelper database, WorldSettings? worldSettings = null, WorldGenerationConfig? generationConfig = null, int worldId = 1, WorldSeedConfig? worldSeed = null)
        {
            _database = database;
            _worldSettings = worldSettings ?? new WorldSettings();
            _worldGenConfig = generationConfig ?? WorldGenerationConfig.Load(_worldSettings.WorldConfigPath);
            _worldId = worldId;

            // 월드 시드 초기화: 제공된 시드 또는 데이터베이스에서 로드, 또는 새로 생성
            _worldSeed = worldSeed
                ?? WorldSeedConfig.FromSeed((int)_worldSettings.WorldSeed)
                ?? LoadWorldSeedFromDatabase()
                ?? WorldSeedConfig.Random();
            SaveWorldSeedToDatabase();

            // 시드를 사용하여 Random 초기화 (결정적 생성을 위함)
            _random = new Random(_worldSeed.Seed);
            _caveSettings = new CaveGenerationSettings
            {
                HorizontalFrequency = _worldGenConfig.Caves.HorizontalFrequency,
                VerticalFrequency = _worldGenConfig.Caves.VerticalFrequency,
                Threshold = _worldGenConfig.Caves.Threshold,
                LavaThreshold = _worldGenConfig.Caves.LavaThreshold,
                WaterThreshold = _worldGenConfig.Caves.WaterThreshold,
                FloodedCaveNoiseFrequency = _worldGenConfig.Caves.FloodedCaveNoiseFrequency,
                FloodedCaveProximityToWaterTableWeight = _worldGenConfig.Caves.FloodedCaveProximityToWaterTableWeight,
                FloodedCaveThreshold = _worldGenConfig.Caves.FloodedCaveThreshold,
                HydrologyStabilityWeight = _worldGenConfig.Caves.HydrologyStabilityWeight,
                FlowStabilityWeight = _worldGenConfig.Caves.FlowStabilityWeight,
                RoughnessStabilityWeight = _worldGenConfig.Caves.RoughnessStabilityWeight,
                RiverSuppressionWeight = _worldGenConfig.Caves.RiverSuppressionWeight
            };

            _enableCaves = _worldSettings.EnableCaves && _worldGenConfig.Caves.EnableCaves;
            _enableRivers = _worldSettings.EnableRivers && _worldGenConfig.Water.EnableRivers;
            _enableLakes = _worldSettings.EnableLakes && _worldGenConfig.Water.EnableLakes;
            _useImprovedCaves = _enableCaves && _worldGenConfig.Caves.UseImprovedCaves;
            _useImprovedRivers = _enableRivers && _worldGenConfig.Water.UseImprovedRivers;
            _useImprovedLakes = _enableLakes && _worldGenConfig.Water.UseImprovedLakes;

            GlobalWaterLevel = _worldGenConfig.Water.GlobalWaterLevel;
            RiverCenterThreshold = _worldGenConfig.Water.RiverCenterThreshold;
            RiverBankThreshold = _worldGenConfig.Water.RiverBankThreshold;
            NoiseCaveHorizontalFrequency = _caveSettings.HorizontalFrequency;
            NoiseCaveVerticalFrequency = _caveSettings.VerticalFrequency;
            NoiseCaveThreshold = _caveSettings.Threshold;
            NoiseCaveLavaThreshold = _caveSettings.LavaThreshold;
            NoiseCaveWaterThreshold = _caveSettings.WaterThreshold;
            _hydrologySmoothIterations = Math.Clamp(_worldGenConfig.Water.HydrologySmoothIterations, 0, 6);
            _hydrologySmoothBlend = Math.Clamp(_worldGenConfig.Water.HydrologySmoothBlend, 0.0, 1.0);
            _hydrologyShorePush = Math.Clamp(_worldGenConfig.Water.HydrologyShorePush, 1.0, 24.0);
            _hydrologySlopePenalty = Math.Clamp(_worldGenConfig.Water.HydrologySlopePenalty, 2.0, 18.0);
            _hydrologyFlowGain = Math.Clamp(_worldGenConfig.Water.HydrologyFlowGain, 0.0, 1.5);
            _hydrologyContinuityWeight = Math.Clamp(_worldGenConfig.Water.HydrologyContinuityWeight, 0.0, 1.0);
            _hydrologyEdgeFlowBias = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeFlowBias, 0.0, 1.25);
            _hydrologyEdgeTangentWeight = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeTangentWeight, 0.0, 1.5);
            _hydrologyEdgeFlowLockWeight = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.5);
            _hydrologyEdgeBlendRadius = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeBlendRadius, 1, 6);
            _hydrologyEdgeStabilityIterations = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeStabilityIterations, 0, 4);
            _hydrologyEdgeStabilityWeight = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            _hydrologyEdgeVarianceClamp = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeVarianceClamp, 0.0, 1.0);
            _hydrologyEdgeFluxBlend = Math.Clamp(_worldGenConfig.Water.HydrologyEdgeFluxBlend, 0.0, 1.0);
            _hydrologyVarianceBlend = Math.Clamp(_worldGenConfig.Water.HydrologyVarianceBlend, 0.0, 1.0);
            _hydrologyVarianceClamp = Math.Clamp(_worldGenConfig.Water.HydrologyVarianceClamp, 0.0, 1.25);
            _hydrologyWaterTableClampWeight = Math.Clamp(_worldGenConfig.Water.HydrologyWaterTableClampWeight, 0.0, 1.0);
            _hydrologyWaterTableClampRange = Math.Clamp(_worldGenConfig.Water.HydrologyWaterTableClampRange, 1, 64);
            _hydrologyWaterTableSlopeWeight = Math.Clamp(_worldGenConfig.Water.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            _hydrologyFlowPersistence = Math.Clamp(_worldGenConfig.Water.HydrologyFlowPersistence, 0.0, 1.0);
            _hydrologyGradientWeight = Math.Clamp(_worldGenConfig.Water.HydrologyGradientWeight, 0.0, 1.5);
            _hydrologyGradientSlopeWeight = Math.Clamp(_worldGenConfig.Water.HydrologyGradientSlopeWeight, 0.0, 1.0);
            _hydrologyGradientClamp = Math.Clamp(_worldGenConfig.Water.HydrologyGradientClamp, 0.1, 3.5);
            _hydrologyGradientStabilityIterations = Math.Clamp(_worldGenConfig.Water.HydrologyGradientStabilityIterations, 0, 6);
            _hydrologyGradientStabilityBlend = Math.Clamp(_worldGenConfig.Water.HydrologyGradientStabilityBlend, 0.0, 1.0);
            _hydrologyDirectionalIterations = Math.Clamp(_worldGenConfig.Water.HydrologyDirectionalIterations, 0, 4);
            _hydrologyDirectionalBlend = Math.Clamp(_worldGenConfig.Water.HydrologyDirectionalBlend, 0.0, 1.0);
            _hydrologyFlowDivergenceClamp = Math.Clamp(_worldGenConfig.Water.HydrologyFlowDivergenceClamp, 0.0, 1.5);
            _hydrologyCurvatureWeight = Math.Clamp(_worldGenConfig.Water.HydrologyCurvatureWeight, 0.0, 1.5);
            _hydrologySeamRelaxIterations = Math.Clamp(_worldGenConfig.Water.HydrologySeamRelaxIterations, 0, 4);
            _hydrologySeamRelaxBlend = Math.Clamp(_worldGenConfig.Water.HydrologySeamRelaxBlend, 0.0, 1.0);
            _riparianSmoothIterations = Math.Clamp(_worldGenConfig.Water.RiparianSmoothIterations, 0, 6);
            _riparianSmoothBlend = Math.Clamp(_worldGenConfig.Water.RiparianSmoothBlend, 0.0, 1.0);
            _riparianSaturationBoost = Math.Clamp(_worldGenConfig.Water.RiparianSaturationBoost, 0.0, 1.0);
            _riparianBufferRadius = Math.Clamp(_worldGenConfig.Water.RiparianBufferRadius, 0, 6);
            _riverBankErosionWeight = Math.Clamp(_worldGenConfig.Water.RiverBankErosionWeight, 0.0, 1.0);
            _lakeRimErosionWeight = Math.Clamp(_worldGenConfig.Water.LakeRimErosionWeight, 0.0, 1.0);
            _riverNoiseScale = Math.Clamp(_worldGenConfig.Water.RiverNoiseScale, 0.0001, 0.05);
            _riverDepth = Math.Clamp(_worldGenConfig.Water.RiverDepth, 2, 24);
            _riverIntensitySmoothIterations = Math.Clamp(_worldGenConfig.Water.RiverIntensitySmoothIterations, 1, 6);
            _riverIntensitySmoothBlend = Math.Clamp(_worldGenConfig.Water.RiverIntensitySmoothBlend, 0.0, 1.0);
            _riverConfluenceBoost = Math.Clamp(_worldGenConfig.Water.RiverConfluenceBoost, 0.0, 2.0);
            _riverEdgeFeather = Math.Clamp(_worldGenConfig.Water.RiverEdgeFeather, 0.0, 1.0);
            _riverMouthSmoothRadius = Math.Clamp(_worldGenConfig.Water.RiverMouthSmoothRadius, 1, 8);
            _riverDeltaWetlandStrength = Math.Clamp(_worldGenConfig.Water.RiverDeltaWetlandStrength, 0.0, 1.0);
            _caveStabilitySmoothIterations = Math.Clamp(_worldGenConfig.Caves.StabilitySmoothIterations, 0, 6);
            _caveStabilitySmoothBlend = Math.Clamp(_worldGenConfig.Caves.StabilitySmoothBlend, 0.0, 1.0);
            _caveSupportDensity = Math.Clamp(_worldGenConfig.Caves.SupportDensity, 0.0, 1.0);
            _supportPillarChance = Math.Clamp(_worldGenConfig.Caves.SupportPillarChance, 0.0, 1.0);
            _hydrologyWarpFrequency = Math.Clamp(_worldGenConfig.Water.HydrologyWarpFrequency, 0.0001, 0.01);
            _hydrologyWarpAmplitude = Math.Clamp(_worldGenConfig.Water.HydrologyWarpAmplitude, 0.0, 48.0);
            _caveHydrologyWeight = Math.Clamp(_worldGenConfig.Caves.HydrologyStabilityWeight, 0.0, 1.0);
            _caveFlowWeight = Math.Clamp(_worldGenConfig.Caves.FlowStabilityWeight, 0.0, 1.0);
            _caveRoughnessWeight = Math.Clamp(_worldGenConfig.Caves.RoughnessStabilityWeight, 0.0, 1.0);
            double caveWeightTotal = _caveHydrologyWeight + _caveFlowWeight + _caveRoughnessWeight;
            _caveDepthWeight = Math.Clamp(1.0 - caveWeightTotal, 0.05, 0.45);
            _caveRiverSuppressionWeight = Math.Clamp(_worldGenConfig.Caves.RiverSuppressionWeight, 0.0, 1.0);
            _riverFlowAlignmentWeight = Math.Clamp(_worldGenConfig.Water.RiverFlowAlignmentWeight, 0.0, 2.0);
            _riverAnisotropyWeight = Math.Clamp(_worldGenConfig.Water.RiverAnisotropyWeight, 0.0, 2.0);
            _riverGradientPenalty = Math.Clamp(_worldGenConfig.Water.RiverGradientPenalty, 0.0, 1.0);
            _riverHeadwaterStabilityWeight = Math.Clamp(_worldGenConfig.Water.RiverHeadwaterStabilityWeight, 0.0, 1.0);
            _riverReliefPenaltyWeight = Math.Clamp(_worldGenConfig.Water.RiverReliefPenaltyWeight, 0.0, 1.0);
            _riverSeamFillStrength = Math.Clamp(_worldGenConfig.Water.RiverSeamFillStrength, 0.0, 1.5);
            _caveSupportHydrationBias = Math.Clamp(_worldGenConfig.Caves.SupportHydrationBias, 0.0, 1.0);
            _caveSupportFlowBias = Math.Clamp(_worldGenConfig.Caves.SupportFlowBias, 0.0, 1.0);
            _caveRiparianPlugDepth = Math.Clamp(_worldGenConfig.Caves.RiparianPlugDepth, 0, 8);
            _lakeRiverProximitySuppression = Math.Clamp(_worldGenConfig.Lakes.RiverProximitySuppression, 0.0, 1.0);
            _lakeBasinSmoothIterations = Math.Clamp(_worldGenConfig.Lakes.LakeBasinSmoothIterations, 0, 6);
            _lakeShelfDepth = Math.Clamp(_worldGenConfig.Lakes.ShelfDepth, 0, 6);
            _lakeInflowBlendWeight = Math.Clamp(_worldGenConfig.Water.LakeInflowBlendWeight, 0.0, 1.0);
            _lakeWetlandSaturationThreshold = Math.Clamp(_worldGenConfig.Lakes.WetlandSaturationThreshold, 0.0, 1.25);
            _lakeOutflowCarveDepth = Math.Clamp(_worldGenConfig.Lakes.OutflowCarveDepth, 1, 12);
            _lakeWetlandBufferRadius = Math.Clamp(_worldGenConfig.Lakes.WetlandBufferRadius, 0, 8);
            _caveMoistureRetentionWeight = Math.Clamp(_worldGenConfig.Caves.MoistureRetentionWeight, 0.0, 1.0);
            _caveEdgeSealStrength = Math.Clamp(_worldGenConfig.Caves.EdgeSealStrength, 0.0, 1.0);
            _caveCeilingStabilityWeight = Math.Clamp(_worldGenConfig.Caves.CeilingStabilityWeight, 0.0, 1.0);

            _mapControlProfile = WorldMapControlProfile.Create(_worldGenConfig, _worldSettings);
            WorldMapControlProfileUtility.Save(_mapControlProfile, _worldGenConfig.MapControlProfilePath);

            Console.WriteLine($"[WorldManager] {_worldSeed} (config: {_worldGenConfig.SourcePath}, rivers: {_enableRivers}, lakes: {_enableLakes}, caves: {_enableCaves})");
            Console.WriteLine($"[WorldManager] hydrology: smooth={_hydrologySmoothIterations}/{_hydrologySmoothBlend:0.##}, shorePush={_hydrologyShorePush:0.##}, slopePenalty={_hydrologySlopePenalty:0.##}, flowGain={_hydrologyFlowGain:0.##}, continuity={_hydrologyContinuityWeight:0.##}, edgeFlowBias={_hydrologyEdgeFlowBias:0.##}, edgeTangent={_hydrologyEdgeTangentWeight:0.##}, edgeFlowLock={_hydrologyEdgeFlowLockWeight:0.##}, edgeStability={_hydrologyEdgeStabilityIterations}/{_hydrologyEdgeStabilityWeight:0.##}, variance={_hydrologyVarianceBlend:0.##}/{_hydrologyVarianceClamp:0.##}, waterTableClamp={_hydrologyWaterTableClampWeight:0.##}/{_hydrologyWaterTableClampRange} slope={_hydrologyWaterTableSlopeWeight:0.##}, seamRelax={_hydrologySeamRelaxIterations}/{_hydrologySeamRelaxBlend:0.##}, riparian={_riparianSmoothIterations}/{_riparianSmoothBlend:0.##}/boost={_riparianSaturationBoost:0.##}, grad={_hydrologyGradientWeight:0.##}/slope={_hydrologyGradientSlopeWeight:0.##}/clamp={_hydrologyGradientClamp:0.##}/stab={_hydrologyGradientStabilityIterations}/{_hydrologyGradientStabilityBlend:0.##}/dir={_hydrologyDirectionalIterations}/{_hydrologyDirectionalBlend:0.##}/divClamp={_hydrologyFlowDivergenceClamp:0.##}/curv={_hydrologyCurvatureWeight:0.##}, riverNoiseScale={_riverNoiseScale:0.#####}, riverDepth={_riverDepth}, riverSmooth={_riverIntensitySmoothIterations}/{_riverIntensitySmoothBlend:0.##}, riverAniso={_riverFlowAlignmentWeight:0.##}/{_riverGradientPenalty:0.##}, headwater={_riverHeadwaterStabilityWeight:0.##}, confluence={_riverConfluenceBoost:0.##}, lakeInflow={_lakeInflowBlendWeight:0.##}, lakeBasinSmooth={_lakeBasinSmoothIterations}/shelf={_lakeShelfDepth}, caveSupport={_caveSupportDensity:0.##}, supportBias=H{_caveSupportHydrationBias:0.##}/F{_caveSupportFlowBias:0.##}/plug={_caveRiparianPlugDepth}, hydroWarp={_hydrologyWarpFrequency:0.#####}/{_hydrologyWarpAmplitude:0.##}, caveWeights=H{_caveHydrologyWeight:0.##}/F{_caveFlowWeight:0.##}/R{_caveRoughnessWeight:0.##}, caveMoistureRet={_caveMoistureRetentionWeight:0.##}");
            Console.WriteLine($"[WorldManager] map control: chunk={_mapControlProfile.ChunkSize}, render={_mapControlProfile.RenderDistance}, sim={_mapControlProfile.SimulationDistance}, water={_mapControlProfile.GlobalWaterLevel}, curv={_mapControlProfile.HydrologyCurvatureWeight:0.##}, hash={_mapControlProfile.ProfileHash[..Math.Min(12, _mapControlProfile.ProfileHash.Length)]}");
            Console.WriteLine($"[WorldManager] riparianBuffer={_riparianBufferRadius}, riverSeamFill={_riverSeamFillStrength:0.##}, lakeWetlandBuffer={_lakeWetlandBufferRadius}, caveCeilingStability={_caveCeilingStabilityWeight:0.##}");
            Console.WriteLine($"[WorldManager] map control profile written to '{_worldGenConfig.MapControlProfilePath}' (v{_mapControlProfile.Version})");

            var pipeline = new TerrainGenerationPipeline()
                .AddStage(new BaseTerrainStage(this));

            if (_worldSettings.EnableOreGeneration)
            {
                pipeline.AddStage(new OreGenerationStage(this));
            }

            if (_enableCaves)
            {
                pipeline.AddStage(_useImprovedCaves
                    ? new ImprovedCaveGenerationStage(this)
                    : new CaveGenerationStage(this));
            }

            pipeline.AddStage(new DungeonGenerationStage(this));

            if (_enableRivers)
            {
                pipeline.AddStage(_useImprovedRivers
                    ? new ImprovedRiverGenerationStage(this)
                    : new RiverGenerationStage(this));
            }

            if (_enableLakes)
            {
                pipeline.AddStage(_useImprovedLakes
                    ? new ImprovedLakeGenerationStage(this)
                    : new LakeGenerationStage(this));
            }

            if (_worldSettings.EnableVegetationGeneration)
            {
                pipeline.AddStage(new VegetationGenerationStage(this));
            }

            pipeline.AddStage(new CloudGenerationStage(this));
            _terrainPipeline = pipeline;
        }

        public WorldMapControlProfile MapControlProfile => _mapControlProfile;

        public async Task<ChunkData?> GetChunkAsync(int chunkX, int chunkZ)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            if (_loadedChunks.TryGetValue(chunkKey, out var loadedChunk))
            {
                loadedChunk.LastAccessed = DateTime.UtcNow;
                return loadedChunk.Data;
            }

            var chunkData = await LoadChunkFromDatabase(chunkX, chunkZ);
            if (chunkData == null)
            {
                chunkData = await GenerateChunk(chunkX, chunkZ);
                await SaveChunkToDatabase(chunkX, chunkZ, chunkData);
            }

            _loadedChunks[chunkKey] = new LoadedChunk
            {
                Data = chunkData,
                LastAccessed = DateTime.UtcNow,
                IsModified = false
            };

            return chunkData;
        }

        public async Task UpdateBlockAsync(int chunkX, int chunkZ, int blockX, int blockY, int blockZ, 
            BlockType blockType, int playerId)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            if (!_loadedChunks.TryGetValue(chunkKey, out var loadedChunk))
            {
                loadedChunk = new LoadedChunk
                {
                    Data = await GetChunkAsync(chunkX, chunkZ),
                    LastAccessed = DateTime.UtcNow,
                    IsModified = false
                };
                _loadedChunks[chunkKey] = loadedChunk;
            }

            if (loadedChunk.Data != null)
            {
                var localX = blockX % 16;
                var localZ = blockZ % 16;
                
                if (localX >= 0 && localX < 16 && localZ >= 0 && localZ < 16 && 
                    blockY >= 0 && blockY < 256)
                {
                    loadedChunk.Data.SetBlock(localX, blockY, localZ, blockType);
                    loadedChunk.IsModified = true;
                    loadedChunk.LastAccessed = DateTime.UtcNow;
                    
                    await _database.SaveBlockChangeAsync(_worldId, chunkX, chunkZ, 
                        blockX, blockY, blockZ, (int)blockType, playerId);
                }
            }
        }

        public async Task SaveModifiedChunksAsync()
        {
            var tasks = new List<Task>();
            
            foreach (var kvp in _loadedChunks)
            {
                if (kvp.Value.IsModified)
                {
                    var coords = ParseChunkKey(kvp.Key);
                    tasks.Add(SaveChunkToDatabase(coords.x, coords.z, kvp.Value.Data));
                    kvp.Value.IsModified = false;
                }
            }
            
            await Task.WhenAll(tasks);
        }

        public void UnloadOldChunks(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var chunksToUnload = new List<string>();
            
            foreach (var kvp in _loadedChunks)
            {
                if (kvp.Value.LastAccessed < cutoffTime)
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var chunkKey in chunksToUnload)
            {
                if (_loadedChunks.TryRemove(chunkKey, out var chunk) && chunk.IsModified)
                {
                    var coords = ParseChunkKey(chunkKey);
                    _ = SaveChunkToDatabase(coords.x, coords.z, chunk.Data);
                }
            }
        }

        // ==================== World Seed Management ====================

        private WorldSeedConfig? LoadWorldSeedFromDatabase() => null;

        private void SaveWorldSeedToDatabase()
        {
            // TODO: wire up DatabaseHelper persistence when raw query helpers are available.
        }

        public Random GetChunkRandom(int chunkX, int chunkZ)
        {
            int chunkSeed = _worldSeed.GetChunkSeed(chunkX, chunkZ);
            return new Random(chunkSeed);
        }

        private Random GetChunkRandom(int chunkX, int chunkZ, int salt)
        {
            unchecked
            {
                int baseSeed = _worldSeed.GetChunkSeed(chunkX, chunkZ);
                int mixedSeed = baseSeed ^ (salt * 397) ^ (chunkX * 1013) ^ (chunkZ * 9173);
                return new Random(mixedSeed);
            }
        }

        public WorldSeedConfig GetWorldSeed() => _worldSeed;

        private async Task<ChunkData?> LoadChunkFromDatabase(int chunkX, int chunkZ)
        {
            var result = await _database.LoadChunkAsync(_worldId, chunkX, chunkZ);
            if (result != null)
            {
                return ChunkData.FromBytes(result.Value.blockData, result.Value.biomeData);
            }
            return null;
        }

        private async Task SaveChunkToDatabase(int chunkX, int chunkZ, ChunkData chunkData)
        {
            var (blockData, biomeData) = chunkData.ToBytes();
            await _database.SaveChunkAsync(_worldId, chunkX, chunkZ, blockData, biomeData);
        }

        private async Task<ChunkData> GenerateChunk(int chunkX, int chunkZ)
        {
            var chunk = new ChunkData(chunkX, chunkZ);
            var context = new TerrainGenerationContext(this, chunk, chunkX, chunkZ);
            _terrainPipeline.Execute(context);
            return chunk;
        }

        private TerrainProfile CalculateTerrainProfile(int worldX, int worldZ)
        {
            var warp = SimplexNoise.DomainWarp(
                worldX,
                worldZ,
                DomainWarpSimplexFrequency,
                DomainWarpPerlinFrequency,
                DomainWarpSimplexAmplitude,
                DomainWarpPerlinAmplitude,
                925113);

            double sampleX = worldX + warp.dx;
            double sampleZ = worldZ + warp.dz;

            var continentalness = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.00045, 5, 1.0, 0.52, 934113));
            var macroPerlin = NormalizeNoise(PerlinNoise.Generate(sampleX, sampleZ, 0.00038, 4, 1.0, 0.5, 420031));
            var erosion = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.0012, 4, 1.0, 0.58, 811223));
            var peaks = SampleRidgedNoise(sampleX, sampleZ, 0.0018, 4, 1.0, 0.48, 51277);
            var hills = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.0028, 4, 1.0, 0.54, 22119));
            var valleyNoise = NormalizeNoise(PerlinNoise.Generate(sampleX, sampleZ, 0.0036, 3, 1.0, 0.45, 20317));
            var humidity = NormalizeNoise(SimplexNoise.Generate(sampleX, sampleZ, 0.00095, 3, 1.0, 0.58, 6711));
            var temperature = NormalizeNoise(PerlinNoise.Generate(sampleX, sampleZ, 0.00075, 3, 1.0, 0.6, 9987));

            double blendedContinental = Math.Clamp(continentalness * 0.65 + macroPerlin * 0.35, 0.0, 1.0);

            if (blendedContinental < OceanThreshold)
            {
                double oceanDepthFactor = Math.Clamp((OceanThreshold - blendedContinental) / OceanThreshold, 0.0, 1.0);
                int depth = 14 + (int)(oceanDepthFactor * 28);
                int seafloor = Math.Max(6, GlobalWaterLevel - depth);

                return new TerrainProfile
                {
                    SurfaceHeight = seafloor,
                    HasWater = true,
                    WaterLevel = GlobalWaterLevel,
                    Biome = BiomeType.Ocean,
                    SurfaceBlock = BlockType.Sand,
                    SubSurfaceBlock = BlockType.Sand,
                    FillerBlock = BlockType.Stone,
                    UseCliffFace = false
                };
            }

            bool isBeach = blendedContinental < BeachThreshold;

            var profile = new TerrainProfile
            {
                HasWater = false,
                WaterLevel = GlobalWaterLevel,
                SurfaceBlock = BlockType.Grass,
                SubSurfaceBlock = BlockType.Dirt,
                FillerBlock = BlockType.Stone,
                Biome = BiomeType.Plains,
                UseCliffFace = false
            };

            int baseHeight = (int)(MinSurfaceHeight + blendedContinental * 62);
            baseHeight -= (int)(erosion * 10);
            baseHeight -= (int)(Math.Max(0.0, 0.55 - valleyNoise) * ValleyDepthMultiplier);
            baseHeight = Math.Clamp(baseHeight, MinSurfaceHeight, MaxSurfaceHeight);

            double hillWeight = Math.Max(0.0, hills - 0.32);
            double mountainWeight = Math.Max(0.0, peaks - 0.46) * (1.0 - erosion * 0.6);
            mountainWeight += Math.Max(0.0, valleyNoise - 0.7) * 0.25;

            int hillContribution = (int)(hillWeight * 20);
            int mountainContribution = (int)(mountainWeight * 64);

            int surfaceHeight = baseHeight + hillContribution + mountainContribution;
            surfaceHeight = Math.Clamp(surfaceHeight, MinSurfaceHeight, MaxSurfaceHeight);
            profile.SurfaceHeight = surfaceHeight;

            bool steep = mountainWeight > CliffThreshold && erosion < 0.35;
            bool mountainous = surfaceHeight > 108 || mountainContribution > 28;

            if (steep)
            {
                profile.Biome = BiomeType.Cliffs;
                profile.SurfaceBlock = BlockType.Cobblestone;
                profile.SubSurfaceBlock = BlockType.Stone;
                profile.UseCliffFace = true;
            }
            else if (mountainous)
            {
                profile.Biome = BiomeType.Mountains;
                profile.SurfaceBlock = BlockType.Stone;
                profile.SubSurfaceBlock = BlockType.Stone;
            }
            else if (hillContribution > 8)
            {
                profile.Biome = BiomeType.Hills;
            }
            else
            {
                profile.Biome = DetermineLandBiome(temperature, humidity);
            }

            if (profile.Biome == BiomeType.Desert)
            {
                profile.SurfaceBlock = BlockType.Sand;
                profile.SubSurfaceBlock = BlockType.Sand;
            }

            if (profile.Biome == BiomeType.Tundra)
            {
                profile.SurfaceBlock = BlockType.Grass;
                profile.SubSurfaceBlock = BlockType.Dirt;
            }

            if (isBeach)
            {
                profile.Biome = BiomeType.Beach;
                profile.SurfaceBlock = BlockType.Sand;
                profile.SubSurfaceBlock = BlockType.Sand;
                profile.HasWater = true;
                profile.WaterLevel = Math.Max(GlobalWaterLevel - 1, profile.SurfaceHeight + 1);
            }

            return profile;
        }

        public void GenerateBaseTerrainInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var profiles = context.GetOrAddMetadata(TerrainProfilesKey, () => new TerrainProfile[16, 16]);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = context.ChunkX * 16 + x;
                    var worldZ = context.ChunkZ * 16 + z;

                    var profile = CalculateTerrainProfile(worldX, worldZ);
                    profiles[x, z] = profile;
                    chunk.SetBiome(x, z, profile.Biome);
                    ApplyTerrainColumn(chunk, x, z, profile);

                    var clearanceTop = profile.HasWater
                        ? Math.Max(profile.WaterLevel, profile.SurfaceHeight)
                        : profile.SurfaceHeight;
                    clearanceTop = Math.Clamp(clearanceTop, 0, 255);

                    for (int y = clearanceTop + 1; y < 256; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        private void ApplyTerrainColumn(ChunkData chunk, int x, int z, TerrainProfile profile)
        {
            int surfaceHeight = Math.Clamp(profile.SurfaceHeight, 1, 255);

            for (int y = 0; y <= surfaceHeight && y < 256; y++)
            {
                BlockType block;

                if (y == 0)
                {
                    block = BlockType.Bedrock;
                }
                else if (y < surfaceHeight - 3)
                {
                    block = profile.FillerBlock;
                }
                else if (profile.UseCliffFace && y >= surfaceHeight - 4)
                {
                    block = BlockType.Cobblestone;
                }
                else if (y < surfaceHeight)
                {
                    block = profile.SubSurfaceBlock;
                }
                else
                {
                    block = profile.SurfaceBlock;
                }

                chunk.SetBlock(x, y, z, block);
            }

            if (profile.HasWater)
            {
                int waterTop = Math.Clamp(profile.WaterLevel, surfaceHeight, 255);
                for (int y = surfaceHeight + 1; y <= waterTop && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }
            }
        }

        private BiomeType DetermineLandBiome(double temperature, double humidity)
        {
            if (temperature > 0.7 && humidity < 0.35)
                return BiomeType.Desert;
            if (temperature < 0.3)
                return BiomeType.Tundra;
            if (humidity > 0.6)
                return BiomeType.Forest;
            return BiomeType.Plains;
        }

        private static double NormalizeNoise(double value)
        {
            return Math.Clamp((value + 1.0) * 0.5, 0.0, 1.0);
        }

        private static double SampleField(double[,] field, double x, double z)
        {
            if (field == null)
            {
                return 0.0;
            }

            int width = field.GetLength(0);
            int depth = field.GetLength(1);
            double clampedX = Math.Clamp(x, 0.0, width - 1);
            double clampedZ = Math.Clamp(z, 0.0, depth - 1);

            int x0 = (int)Math.Floor(clampedX);
            int z0 = (int)Math.Floor(clampedZ);
            int x1 = Math.Min(x0 + 1, width - 1);
            int z1 = Math.Min(z0 + 1, depth - 1);
            double tx = clampedX - x0;
            double tz = clampedZ - z0;

            double v00 = field[x0, z0];
            double v10 = field[x1, z0];
            double v01 = field[x0, z1];
            double v11 = field[x1, z1];

            double vx0 = v00 + (v10 - v00) * tx;
            double vx1 = v01 + (v11 - v01) * tx;
            return vx0 + (vx1 - vx0) * tz;
        }

        private double SampleRidgedNoise(double worldX, double worldZ, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var noise = SimplexNoise.Generate(worldX, worldZ, frequency, octaves, amplitude, persistence, seed);
            noise = Math.Clamp(noise, -1.0, 1.0);
            return 1.0 - Math.Abs(noise);
        }

        private static double SampleDeterministicNoise(int x, int z, int salt)
        {
            unchecked
            {
                int hash = x * 73428767 ^ z * 91228541 ^ salt * 19997;
                hash ^= (hash << 13);
                hash ^= (hash >> 9);
                hash = hash * 60493 + 19990303;
                hash ^= (hash << 11);
                return (hash & int.MaxValue) / (double)int.MaxValue;
            }
        }

        /// <summary>
        /// 개선된 3D 동굴 생성 시스템 - 더 자연스럽고 다양한 동굴 구조
        /// </summary>
        public void GenerateCavesInternal(TerrainGenerationContext context)
        {
            if (!_enableCaves)
            {
                return;
            }

            var chunk = context.Chunk;
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltCaveMain);
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyField = GetHydrologyField(context, surfaceCache);
            var hydrologyMask = hydrologyField.HydrologyMask;
            var flowAccumulation = hydrologyField.FlowAccumulation;
            var erosionRiskField = hydrologyField.ErosionRisk;
            var hydrologyGradient = hydrologyField.HydrologyGradient;
            var riverField = GetRiverFieldCache(context);
            
            // 메인 동굴 시스템 (기존 웜 방식 개선)
            var caveStabilityField = BuildCaveStabilityField(context, surfaceCache, hydrologyMask, flowAccumulation, hydrologyGradient);
            SmoothScalarField(caveStabilityField, _caveStabilitySmoothIterations, _caveStabilitySmoothBlend);

              GenerateMainCaveSystem(context, chunk, rand, caveStabilityField);

            // 소형 동굴방 추가
            GenerateSmallCaveRooms(chunk, rand);

            // 수직 동굴 (수직갱)
            GenerateVerticalShafts(chunk, rand);

            // 노이즈 기반 동굴층 추가 (연속성 보장)
            GenerateNoiseCavePass(context, chunk, surfaceCache, caveStabilityField, erosionRiskField, hydrologyMask, flowAccumulation, hydrologyGradient);
            ApplyCaveHydrologyFeatures(context, chunk, surfaceCache, hydrologyMask);
            IntegrateKarstInlets(context, chunk, surfaceCache, hydrologyMask, flowAccumulation, riverField);
            AddCaveColumnSupports(chunk, surfaceCache, caveStabilityField, hydrologyMask, flowAccumulation, rand);
            AddCaveShelfBands(chunk, surfaceCache, caveStabilityField, hydrologyMask);
            AddCaveDripstoneFeatures(context, chunk);
            AddCaveVentShafts(chunk, surfaceCache, hydrologyMask, caveStabilityField);
            AddCaveAquiferChannels(chunk, surfaceCache, hydrologyMask, flowAccumulation, riverField);
            AddCaveRibbonTerraces(chunk, surfaceCache, hydrologyMask, caveStabilityField, flowAccumulation);
            ApplyCaveHydrologyErosion(chunk, surfaceCache, hydrologyMask, flowAccumulation);
            StabilizeMoistCaveCeilings(chunk, surfaceCache, hydrologyMask, flowAccumulation);
            SealCaveChunkEdges(context, chunk, surfaceCache, hydrologyMask);
        }
        
        private double[,] BuildCaveStabilityField(
            TerrainGenerationContext context,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            Vector2[,] hydrologyGradient)
        {
            var stability = new double[16, 16];
            int originX = context.ChunkX * 16;
            int originZ = context.ChunkZ * 16;

            double surfaceRange = Math.Max(1, MaxSurfaceHeight - MinSurfaceHeight);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        stability[x, z] = 0.0;
                        continue;
                    }

                    double depthFactor = Math.Clamp((surface - MinSurfaceHeight) / surfaceRange, 0.0, 1.0);
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double gradientStrength = Math.Clamp(hydrologyGradient[x, z].Length(), 0.0, _hydrologyGradientClamp);
                    double gradientBias = gradientStrength * Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5);
                    double gradientPenalty = gradientStrength * Math.Clamp(_hydrologyGradientSlopeWeight, 0.0, 1.0);
                    double roughness = NormalizeNoise(SimplexNoise.Generate(originX + x * 0.85, originZ + z * 0.85, 0.012, 3, 1.0, 0.6, 91517));
                    double warp = NormalizeNoise(PerlinNoise.Generate(originX + x * 0.5, originZ + z * 0.5, 0.018, 2, 1.0, 0.55, 52301));
                    double riverIntensity = Math.Abs(SampleRiverField(originX + x, originZ + z));
                    double riverPressure = Math.Clamp(1.0 - riverIntensity / Math.Max(RiverBankThreshold, 1e-5), 0.0, 1.0);

                    double waterTableBias = Math.Clamp((GlobalWaterLevel - surface) / 48.0, 0.0, 1.0);
                    double moisturePenalty = Math.Clamp(hydrology * 0.35 + flow * 0.22, 0.0, 0.75);
                    double roughnessBlend = (roughness * 0.7 + warp * 0.3) * _caveRoughnessWeight;
                    double moistureRetention = 1.0 - Math.Clamp(hydrology * 0.55 + flow * 0.35, 0.0, 1.0) * _caveMoistureRetentionWeight;
                    moistureRetention *= 1.0 - Math.Clamp(gradientPenalty * 0.15, 0.0, 0.25);
                    double saturation = hydrology * _caveHydrologyWeight
                        + flow * _caveFlowWeight
                        + (1.0 - depthFactor) * _caveDepthWeight
                        + roughnessBlend
                        + gradientBias * 0.08;
                    double suppression = 1.0 - _caveRiverSuppressionWeight * (1.0 - riverPressure);
                    double supportBoost = 1.0 + waterTableBias * 0.35 + gradientStrength * 0.05;
                    double stabilityValue = saturation * supportBoost * suppression * Math.Clamp(moistureRetention, 0.25, 1.15);
                    stabilityValue *= 1.0 - (moisturePenalty + gradientPenalty * 0.15) * 0.35;
                    stability[x, z] = Math.Clamp(stabilityValue, 0.0, 1.0);
                }
            }

            FeatherScalarFieldEdges(stability);
            return stability;
        }

        private void SealCaveChunkEdges(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask)
        {
            if (_caveEdgeSealStrength <= 0.0)
            {
                return;
            }

            int radius = Math.Max(1, (int)Math.Round(3 + _caveEdgeSealStrength * 4));
            double sealWeight = Math.Clamp(_caveEdgeSealStrength, 0.0, 1.0);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, 15 - x), Math.Min(z, 15 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[x, z] = surface;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double sealFactor = sealWeight * (1.0 - edgeDistance / (double)radius);
                    sealFactor = Math.Clamp(sealFactor * (0.65 + hydrology * 0.45), 0.0, 1.0);

                    int sealDepth = Math.Max(4, (int)Math.Round(10 * sealFactor));
                    int minY = Math.Max(2, surface - sealDepth);

                    for (int y = minY; y < surface; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block != BlockType.Air)
                        {
                            continue;
                        }

                        double jitter = SampleDeterministicNoise(context.ChunkX * 16 + x, context.ChunkZ * 16 + z, y + SaltCaveMain);
                        if (jitter <= sealFactor)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        }
                    }
                }
            }
        }

        private void FeatherScalarFieldEdges(double[,] field)
        {
            int width = field.GetLength(0);
            int depth = field.GetLength(1);
            int radius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double clampWeight = Math.Clamp(_hydrologyEdgeVarianceClamp, 0.0, 1.0);

            if (clampWeight <= 0.0)
            {
                return;
            }

            var original = (double[,])field.Clone();
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    double sum = 0.0;
                    int samples = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int sampleX = x + dx;
                            int sampleZ = z + dz;
                            if (sampleX < 0 || sampleX >= width || sampleZ < 0 || sampleZ >= depth)
                            {
                                continue;
                            }

                            sum += original[sampleX, sampleZ];
                            samples++;
                        }
                    }

                    if (samples == 0)
                    {
                        continue;
                    }

                    double average = sum / samples;
                    double blend = clampWeight * (1.0 - edgeDistance / (double)radius);
                    field[x, z] = Math.Clamp(original[x, z] * (1.0 - blend) + average * blend, 0.0, 1.0);
                }
            }
        }

        private void AddCaveColumnSupports(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] caveStabilityField,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            Random rand)
        {
            double supportThreshold = _caveSupportDensity <= 0.0 ? 0.58 : _caveSupportDensity;

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double moisture = Math.Max(hydrology, flow);
                    double stability = caveStabilityField[x, z] * (1.0 + moisture * _caveSupportHydrationBias + flow * _caveSupportFlowBias);
                    double adaptiveThreshold = Math.Clamp(supportThreshold * (0.85 + moisture * 0.4), 0.0, 1.0);
                    if (stability < adaptiveThreshold)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 12)
                    {
                        continue;
                    }

                    int top = -1;
                    int bottom = -1;
                    bool insideAir = false;
                    int scanStart = Math.Min(surface - 2, 140);

                    for (int y = scanStart; y >= 8; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air || block == BlockType.Water)
                        {
                            if (!insideAir)
                            {
                                insideAir = true;
                                top = y;
                            }
                        }
                        else if (insideAir)
                        {
                            bottom = y + 1;
                            break;
                        }
                    }

                    if (!insideAir || top == -1 || bottom == -1)
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom + 1;
                    if (cavityHeight < 6)
                    {
                        continue;
                    }

                    double densityFactor = Math.Clamp(stability * 0.55 + adaptiveThreshold * 0.45 + moisture * 0.25, 0.0, 1.0);
                    int supportSpan = Math.Clamp((int)Math.Round(cavityHeight * (0.18 + densityFactor * 0.45 + flow * 0.15)), 3, cavityHeight - 1);
                    int baseOffset = rand.Next(0, Math.Max(1, cavityHeight - supportSpan));
                    int columnBase = bottom + baseOffset;
                    int columnTop = Math.Min(top - 1, columnBase + supportSpan);
                    int radius = (stability > 0.82 || densityFactor > 0.75 || moisture > 0.55) ? 2 : 1;
                    int step = (densityFactor > 0.65 || flow > 0.35) ? 1 : 2;

                    for (int y = columnBase; y <= columnTop; y++)
                    {
                        if ((y - columnBase) % step == 0 || y == columnTop)
                        {
                            PlaceSupportNode(chunk, x, y, z, radius);
                        }
                    }
                }
            }
        }

        private void StabilizeMoistCaveCeilings(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double moisture = Math.Max(hydrologyMask[x, z], Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0));
                    if (moisture < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 5)
                    {
                        continue;
                    }

                    int scanStart = Math.Max(1, surface - 6);
                    bool insideAir = false;
                    int airTop = -1;
                    int airBottom = -1;

                    for (int y = surface; y >= scanStart; y--)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        bool isEmpty = block == BlockType.Air || block == BlockType.Water;
                        if (isEmpty && !insideAir)
                        {
                            insideAir = true;
                            airTop = y;
                        }
                        else if (!isEmpty && insideAir)
                        {
                            airBottom = y + 1;
                            break;
                        }
                    }

                    if (!insideAir || airTop == -1)
                    {
                        continue;
                    }

                    if (airBottom == -1)
                    {
                        airBottom = scanStart;
                    }

                    int roofThickness = surface - airTop;
                    if (roofThickness >= 3)
                    {
                        continue;
                    }

                    int fillTop = Math.Clamp(surface - 1, 2, 254);
                    int fillBottom = Math.Max(airBottom, fillTop - 2);
                    double sealStrength = Math.Clamp((0.75 - roofThickness * 0.25) + moisture * 0.35, 0.0, 1.0);

                    for (int y = fillTop; y >= fillBottom; y--)
                    {
                        double selector = SampleDeterministicNoise(x * 31 + y, z * 37 + surface, 9127);
                        if (selector <= sealStrength)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                            surfaceCache[x, z] = Math.Max(surfaceCache[x, z], y);
                        }
                    }
                }
            }
        }

        private void AddCaveShelfBands(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] stabilityField,
            double[,] hydrologyMask)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    double stability = stabilityField[x, z];
                    if (stability < 0.42)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 6)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double wetness = Math.Clamp(hydrology * 0.7 + stability * 0.25, 0.0, 1.0);
                    int shelfThickness = Math.Clamp((int)Math.Round(1 + wetness * 3.0), 1, 4);
                    int shelfOffset = Math.Clamp((int)Math.Round(cavityHeight * (0.35 + wetness * 0.2)), 2, cavityHeight - 2);
                    int shelfY = Math.Clamp(bottom + shelfOffset, bottom + 2, top - 2);
                    int radius = wetness > 0.62 ? 2 : 1;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                            {
                                continue;
                            }

                            double falloff = 1.0 - (Math.Abs(dx) + Math.Abs(dz)) * 0.35;
                            if (falloff <= 0.0)
                            {
                                continue;
                            }

                            for (int y = shelfY; y > shelfY - shelfThickness && y > bottom + 1; y--)
                            {
                                var block = chunk.GetBlock(nx, y, nz);
                                if (block == BlockType.Air || block == BlockType.Water)
                                {
                                    chunk.SetBlock(nx, y, nz, wetness > 0.6 ? BlockType.Cobblestone : BlockType.Stone);
                                }
                            }

                            if (shelfY + 1 < 256)
                            {
                                var above = chunk.GetBlock(nx, shelfY + 1, nz);
                                if (above != BlockType.Air)
                                {
                                    chunk.SetBlock(nx, shelfY + 1, nz, BlockType.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddCaveVentShafts(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] stabilityField)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 6)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surface, x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    if (surface - top < 4)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double instability = 1.0 - Math.Clamp(stabilityField[x, z], 0.0, 1.0);
                    if (instability < 0.08)
                    {
                        continue;
                    }

                    double spawnWeight = Math.Clamp(hydrology * 0.55 + instability * 0.6, 0.0, 1.0);
                    double selector = SampleDeterministicNoise(x, z, 173);
                    if (spawnWeight < 0.35 || selector > spawnWeight)
                    {
                        continue;
                    }

                    int ventTop = Math.Min(surface - 1, 254);
                    int ventBottom = Math.Clamp(top, bottom + 1, ventTop - 1);

                    for (int y = ventTop; y >= ventBottom; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    surfaceCache[x, z] = Math.Max(ventBottom - 1, 1);

                    if (hydrology > 0.68)
                    {
                        int poolY = Math.Max(ventBottom - 1, 1);
                        chunk.SetBlock(x, poolY, z, BlockType.Water);
                    }

                    HardenVentLip(chunk, surfaceCache, x, z);
                }
            }
        }

        private void HardenVentLip(ChunkData chunk, int[,] surfaceCache, int centerX, int centerZ)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[x, z] = surface;
                    }

                    if (chunk.GetBlock(x, surface, z) == BlockType.Air)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Cobblestone);
                    }
                }
            }
        }

        private void AddCaveAquiferChannels(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            RiverFieldCache riverField)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double catchment = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    if (hydrology < 0.7 && catchment < 0.35)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    if (top - bottom < 6)
                    {
                        continue;
                    }

                    double pressure = Math.Clamp((hydrology - 0.65) * 1.4 + catchment * 0.85, 0.0, 1.0);
                    int channelY = bottom + Math.Max(2, (int)Math.Round((top - bottom) * (0.25 + pressure * 0.35)));
                    if (surfaceCache[x, z] - channelY < 6)
                    {
                        continue;
                    }

                    var flowDir = riverField.Flow[x, z];
                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    if (slopeDir.LengthSquared() > 1e-4f)
                    {
                        flowDir = Vector2.Normalize(flowDir * 0.55f + slopeDir * 0.45f);
                    }

                    if (flowDir.LengthSquared() < 1e-4f)
                    {
                        flowDir = new Vector2(1, 0);
                    }

                    int steps = Math.Clamp((int)Math.Round(3 + pressure * 5 + catchment * 4), 3, 9);
                    int radius = pressure > 0.7 ? 2 : 1;
                    bool floodChannel = hydrology > 0.8 || catchment > 0.6;

                    int cx = x;
                    int cz = z;
                    for (int step = 0; step < steps; step++)
                    {
                        if (cx < 1 || cx >= 15 || cz < 1 || cz >= 15)
                        {
                            break;
                        }

                        CarveAquiferChannelNode(chunk, cx, channelY, cz, radius, floodChannel);
                        var delta = GetAquiferStep(flowDir, step);
                        cx += delta.dx;
                        cz += delta.dz;
                    }
                }
            }
        }

        private void AddCaveRibbonTerraces(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] caveStabilityField,
            double[,] flowAccumulation)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double stability = Math.Clamp(caveStabilityField[x, z], 0.0, 1.0);
                    double catchment = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double ribbonWeight = hydrology * 0.6 + catchment * 0.25 + (1.0 - Math.Abs(stability - 0.55)) * 0.35;
                    if (ribbonWeight < 0.55)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 7)
                    {
                        continue;
                    }

                    int ribbonY = bottom + Math.Clamp((int)Math.Round(cavityHeight * (0.3 + hydrology * 0.35)), 2, cavityHeight - 2);
                    int ribbonThickness = Math.Clamp((int)Math.Round(1 + ribbonWeight * 2), 1, 3);

                    var tangent = ComputeHydrologyTangent(hydrologyMask, x, z);
                    foreach (var (dx, dz) in BuildRibbonOffsets(tangent, ribbonWeight, hydrology))
                    {
                        int worldX = x + dx;
                        int worldZ = z + dz;
                        if (worldX < 1 || worldX >= 15 || worldZ < 1 || worldZ >= 15)
                        {
                            continue;
                        }

                        if (!TryResolveSurface(chunk, surfaceCache, worldX, worldZ, out int columnSurface))
                        {
                            continue;
                        }

                        if (columnSurface - ribbonY < 3)
                        {
                            continue;
                        }

                        int floorY = Math.Max(bottom + 1, ribbonY - ribbonThickness);
                        for (int y = ribbonY; y >= floorY && y > bottom + 1; y--)
                        {
                            chunk.SetBlock(worldX, y, worldZ, BlockType.Air);
                        }

                        int supportY = Math.Max(floorY - 1, 1);
                        var supportMaterial = stability > 0.72 ? BlockType.Cobblestone : BlockType.Stone;
                        chunk.SetBlock(worldX, supportY, worldZ, supportMaterial);

                        var walkwayMaterial = hydrology > 0.78 ? BlockType.Clay : BlockType.Cobblestone;
                        chunk.SetBlock(worldX, floorY, worldZ, walkwayMaterial);

                        int clearanceTop = Math.Min(floorY + 2, 254);
                        for (int y = floorY + 1; y <= clearanceTop; y++)
                        {
                            chunk.SetBlock(worldX, y, worldZ, BlockType.Air);
                        }
                    }
                }
            }
        }

        private void ApplyCaveHydrologyErosion(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation)
        {
            int[,] neighborOffsets =
            {
                { 1, 0 },
                { -1, 0 },
                { 0, 1 },
                { 0, -1 }
            };

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double moisture = Math.Max(hydrology, flow);
                    if (moisture < 0.55)
                    {
                        continue;
                    }

                    if (!TryFindCaveSpan(chunk, surfaceCache[x, z], x, z, out int top, out int bottom))
                    {
                        continue;
                    }

                    int cavityHeight = top - bottom;
                    if (cavityHeight < 5)
                    {
                        continue;
                    }

                    int streamHeight = Math.Clamp((int)Math.Round(cavityHeight * (0.25 + flow * 0.35)), 2, cavityHeight - 1);
                    int streamTop = Math.Min(top - 1, bottom + streamHeight);
                    int thickness = Math.Clamp(
                        (int)Math.Round(1 + (moisture - 0.45) * 6.0),
                        1,
                        Math.Max(2, streamHeight));
                    int streamBottom = Math.Max(bottom + 1, streamTop - thickness);
                    bool fillWithWater = hydrology > 0.68;
                    BlockType fillBlock = fillWithWater ? BlockType.Water : BlockType.Air;
                    BlockType floorMaterial = fillWithWater ? BlockType.Clay : BlockType.Cobblestone;
                    int floor = Math.Max(streamBottom - 1, 1);

                    for (int y = streamBottom; y <= streamTop && y < 255; y++)
                    {
                        chunk.SetBlock(x, y, z, fillBlock);
                    }

                    chunk.SetBlock(x, floor, z, floorMaterial);

                    for (int i = 0; i < neighborOffsets.GetLength(0); i++)
                    {
                        int nx = x + neighborOffsets[i, 0];
                        int nz = z + neighborOffsets[i, 1];
                        if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                        {
                            continue;
                        }

                        double cue = SampleDeterministicNoise(nx + floor, nz + floor, 9731);
                        if (cue < moisture * 0.45)
                        {
                            continue;
                        }

                        int linkBottom = streamBottom;
                        int linkTop = Math.Min(streamTop, linkBottom + 2 + (int)Math.Round(flow * 3.0));
                        for (int y = linkBottom; y <= linkTop && y < 255; y++)
                        {
                            chunk.SetBlock(nx, y, nz, fillBlock);
                        }

                        int guardY = Math.Min(linkTop + 1, 254);
                        if (chunk.GetBlock(nx, guardY, nz) != BlockType.Air)
                        {
                            chunk.SetBlock(nx, guardY, nz, BlockType.Air);
                        }
                    }

                    ExtendCaveHydrologyRunoff(
                        chunk,
                        surfaceCache,
                        hydrologyMask,
                        flowAccumulation,
                        x,
                        z,
                        streamBottom,
                        streamTop,
                        fillBlock);
                }
            }
        }

        private void ExtendCaveHydrologyRunoff(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            int originX,
            int originZ,
            int streamBottom,
            int streamTop,
            BlockType fillBlock)
        {
            var gradient = ComputeHydrologyGradientVector(hydrologyMask, originX, originZ);
            if (gradient.LengthSquared() < 1e-5f)
            {
                return;
            }

            double baseHydrology = Math.Clamp(hydrologyMask[originX, originZ], 0.0, 1.0);
            double baseFlow = Math.Clamp(flowAccumulation[originX, originZ] / 6.0, 0.0, 1.0);
            double pressure = Math.Max(baseHydrology, baseFlow);
            int steps = Math.Clamp((int)Math.Round(1 + pressure * 3.0), 1, 4);

            double cursorX = originX;
            double cursorZ = originZ;
            for (int step = 0; step < steps; step++)
            {
                cursorX += gradient.X;
                cursorZ += gradient.Y;
                int targetX = (int)Math.Round(cursorX);
                int targetZ = (int)Math.Round(cursorZ);
                if (targetX < 1 || targetX >= 15 || targetZ < 1 || targetZ >= 15)
                {
                    break;
                }

                if (!TryResolveSurface(chunk, surfaceCache, targetX, targetZ, out int surface))
                {
                    continue;
                }

                if (!TryFindCaveSpan(chunk, surface, targetX, targetZ, out int top, out int bottom))
                {
                    continue;
                }

                int cavityHeight = top - bottom;
                if (cavityHeight < 4)
                {
                    continue;
                }

                double neighborHydrology = Math.Clamp(hydrologyMask[targetX, targetZ], 0.0, 1.0);
                double neighborFlow = Math.Clamp(flowAccumulation[targetX, targetZ] / 6.0, 0.0, 1.0);
                double neighborMoisture = Math.Max(neighborHydrology, neighborFlow);
                if (neighborMoisture < 0.45)
                {
                    continue;
                }

                int localThickness = Math.Clamp(
                    (int)Math.Round(1 + neighborMoisture * 3.0 + pressure),
                    1,
                    Math.Max(2, cavityHeight - 1));
                int localTop = Math.Min(top - 1, bottom + localThickness + 1);
                int localBottom = Math.Max(bottom + 1, localTop - localThickness);
                int floor = Math.Max(localBottom - 1, 1);
                BlockType floorMaterial = fillBlock == BlockType.Water ? BlockType.Clay : BlockType.Cobblestone;

                for (int y = localBottom; y <= localTop && y < 255; y++)
                {
                    chunk.SetBlock(targetX, y, targetZ, fillBlock);
                }

                chunk.SetBlock(targetX, floor, targetZ, floorMaterial);

                if (fillBlock == BlockType.Water)
                {
                    int cap = Math.Min(Math.Max(streamTop, localTop + 1), 254);
                    for (int y = localTop + 1; y <= cap; y++)
                    {
                        if (chunk.GetBlock(targetX, y, targetZ) != BlockType.Air)
                        {
                            chunk.SetBlock(targetX, y, targetZ, BlockType.Air);
                        }
                    }
                }
            }
        }

        private static Vector2 ComputeHydrologyGradientVector(double[,] hydrologyMask, int x, int z)
        {
            int maxX = hydrologyMask.GetLength(0) - 1;
            int maxZ = hydrologyMask.GetLength(1) - 1;
            double gx = hydrologyMask[Math.Min(maxX, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
            double gz = hydrologyMask[x, Math.Min(maxZ, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];
            var gradient = new Vector2((float)gx, (float)gz);
            return gradient.LengthSquared() < 1e-5f ? Vector2.Zero : Vector2.Normalize(gradient);
        }

        private static Vector2 ComputeHydrologyTangent(double[,] hydrologyMask, int x, int z)
        {
            int maxX = hydrologyMask.GetLength(0) - 1;
            int maxZ = hydrologyMask.GetLength(1) - 1;

            double gx = hydrologyMask[Math.Min(maxX, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
            double gz = hydrologyMask[x, Math.Min(maxZ, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];

            var gradient = new Vector2((float)gx, (float)gz);
            if (gradient.LengthSquared() < 1e-5f)
            {
                return Vector2.UnitX;
            }

            var tangent = new Vector2(-gradient.Y, gradient.X);
            return tangent.LengthSquared() < 1e-5f ? Vector2.UnitX : Vector2.Normalize(tangent);
        }

        private static IReadOnlyCollection<(int dx, int dz)> BuildRibbonOffsets(Vector2 tangent, double ribbonWeight, double hydrology)
        {
            var offsets = new HashSet<(int dx, int dz)> { (0, 0) };
            var perpendicular = new Vector2(-tangent.Y, tangent.X);
            if (perpendicular.LengthSquared() < 1e-5f)
            {
                perpendicular = Vector2.UnitY;
            }

            int steps = ribbonWeight > 0.85 ? 3 : 2;
            int halfWidth = hydrology > 0.75 ? 1 : 0;

            for (int step = -steps; step <= steps; step++)
            {
                int baseDx = (int)Math.Round(tangent.X * step);
                int baseDz = (int)Math.Round(tangent.Y * step);

                for (int lateral = -halfWidth; lateral <= halfWidth; lateral++)
                {
                    int offsetX = baseDx + (int)Math.Round(perpendicular.X * lateral);
                    int offsetZ = baseDz + (int)Math.Round(perpendicular.Y * lateral);
                    offsets.Add((offsetX, offsetZ));
                }
            }

            return offsets;
        }

        private bool TryResolveSurface(ChunkData chunk, int[,] surfaceCache, int x, int z, out int surface)
        {
            surface = 0;
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
            {
                return false;
            }

            surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                surface = FindSurfaceLevel(chunk, x, z);
                if (surface <= 0)
                {
                    return false;
                }
                surfaceCache[x, z] = surface;
            }

            return true;
        }

        private static (int dx, int dz) GetAquiferStep(Vector2 direction, int stepIndex)
        {
            int dx = 0;
            int dz = 0;
            if (direction.X > 0.35f)
            {
                dx = 1;
            }
            else if (direction.X < -0.35f)
            {
                dx = -1;
            }

            if (direction.Y > 0.35f)
            {
                dz = 1;
            }
            else if (direction.Y < -0.35f)
            {
                dz = -1;
            }

            if (dx == 0 && dz == 0)
            {
                if (Math.Abs(direction.X) >= Math.Abs(direction.Y))
                {
                    dx = direction.X >= 0 ? 1 : -1;
                }
                else
                {
                    dz = direction.Y >= 0 ? 1 : -1;
                }
            }

            if (stepIndex % 3 == 2 && Math.Abs(direction.X) > 0.15f && Math.Abs(direction.Y) > 0.15f)
            {
                dx = Math.Clamp(dx + (direction.X >= 0 ? 1 : -1), -1, 1);
                dz = Math.Clamp(dz + (direction.Y >= 0 ? 1 : -1), -1, 1);
            }

            return (dx, dz);
        }

        private void CarveAquiferChannelNode(ChunkData chunk, int centerX, int centerY, int centerZ, int radius, bool floodChannel)
        {
            int floor = Math.Max(2, centerY - 2);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = centerX + dx;
                    int nz = centerZ + dz;
                    if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                    {
                        continue;
                    }

                    double falloff = 1.0 - (Math.Abs(dx) + Math.Abs(dz)) * (radius == 1 ? 0.6 : 0.45);
                    if (falloff <= 0.0)
                    {
                        continue;
                    }

                    int roof = Math.Min(254, centerY + (radius > 1 ? 2 : 1));
                    for (int y = roof; y >= floor; y--)
                    {
                        chunk.SetBlock(nx, y, nz, BlockType.Air);
                    }

                    int floorBlock = Math.Max(1, floor - 1);
                    chunk.SetBlock(nx, floorBlock, nz, floodChannel ? BlockType.Clay : BlockType.Cobblestone);

                    if (floodChannel)
                    {
                        for (int y = floor; y <= centerY && y < 255; y++)
                        {
                            chunk.SetBlock(nx, y, nz, BlockType.Water);
                        }
                    }
                }
            }
        }

        private static bool TryFindCaveSpan(ChunkData chunk, int surface, int x, int z, out int top, out int bottom)
        {
            top = -1;
            bottom = -1;

            if (surface <= 0)
            {
                return false;
            }

            int scanStart = Math.Clamp(surface - 2, 8, 220);
            bool insideAir = false;
            for (int y = scanStart; y >= 6; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                bool isAir = block == BlockType.Air || block == BlockType.Water;
                if (isAir)
                {
                    if (!insideAir)
                    {
                        insideAir = true;
                        top = y;
                    }
                }
                else if (insideAir)
                {
                    bottom = y + 1;
                    return true;
                }
            }

            top = -1;
            bottom = -1;
            return false;
        }

        private void PlaceSupportNode(ChunkData chunk, int x, int y, int z, int radius)
        {
            if (y < 1 || y >= 255 || x <= 0 || x >= 15 || z <= 0 || z >= 15)
            {
                return;
            }

            chunk.SetBlock(x, y, z, BlockType.Cobblestone);

            if (radius <= 1)
            {
                return;
            }

            var offsets = new (int dx, int dz)[]
            {
                (1, 0), (-1, 0), (0, 1), (0, -1)
            };

            foreach (var (dx, dz) in offsets)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                {
                    continue;
                }

                var block = chunk.GetBlock(nx, y, nz);
                if (block == BlockType.Air)
                {
                    chunk.SetBlock(nx, y, nz, BlockType.Stone);
                }
            }

            if (y - 1 >= 1 && chunk.GetBlock(x, y - 1, z) == BlockType.Air)
            {
                chunk.SetBlock(x, y - 1, z, BlockType.Stone);
            }
        }

        /// <summary>
        /// 메인 동굴 시스템 생성
        /// </summary>
        private void GenerateMainCaveSystem(TerrainGenerationContext context, ChunkData chunk, Random rand, double[,] caveStabilityField)
        {
            int wormCount = 1 + rand.Next(3); // 1~3개의 메인 웜
            double radiusNoiseSeed = rand.NextDouble() * 1000.0;
            double directionalNoiseSeed = rand.NextDouble() * 500.0;

            if (_worldGenConfig.Caves.UseRegionalMainCaves)
            {
                GenerateRegionalMainCaves(context, chunk, caveStabilityField);
                return;
            }

            for (int w = 0; w < wormCount; w++)
            {
                double x = rand.Next(16);
                double y = rand.Next(15, 55); // 더 깊은 지하부터
                double z = rand.Next(16);
                int steps = 100 + rand.Next(80); // 더 긴 동굴
                double yaw = rand.NextDouble() * Math.PI * 2.0;
                double pitch = (rand.NextDouble() - 0.5) * 0.4;
                double baseRadius = 2.0 + rand.NextDouble() * 1.5; // 기본 반지름

                for (int s = 0; s < steps; s++)
                {
                    // 동적으로 변하는 반지름 (넓어지고 좁아지는 효과)
                    double stability = SampleField(caveStabilityField, x, z);
                    double radiusNoise = SimplexNoise.Generate(x + radiusNoiseSeed, z + radiusNoiseSeed, 0.12, 2, 1.0, 0.55, 55127);
                    double radiusTurbulence = Math.Sin(s * 0.1) * 0.8 + radiusNoise * 0.6;
                    double pressureBias = 0.65 + (1.0 - stability) * 0.45;
                    double currentRadius = (baseRadius + radiusTurbulence) * pressureBias;
                    if (stability > 0.75)
                    {
                        currentRadius *= 1.05 + stability * 0.2;
                    }
                    currentRadius = Math.Clamp(currentRadius, 1.6, baseRadius + 2.1);
                    
                    int cx = (int)Math.Round(x);
                    int cy = (int)Math.Round(y);
                    int cz = (int)Math.Round(z);
                    
                    // 동굴 조각하기
                    CarveSphere(chunk, cx, cy, cz, currentRadius);
                    
                    // 가끔 큰 공간(방) 생성
                    if (s > 20 && rand.NextDouble() < 0.05) // 5% 확률
                    {
                        CarveRoom(chunk, cx, cy, cz, 4 + rand.Next(4));
                        if (rand.NextDouble() < 0.35)
                        {
                            CreateCavePool(chunk, cx, Math.Clamp(cy - rand.Next(1, 3), 2, 120), cz, rand.Next(2, 5));
                        }
                    }

                    // 이동
                    double speed = 0.8 + rand.NextDouble() * 0.4; // 가변 속도
                    x += Math.Cos(yaw) * speed;
                    z += Math.Sin(yaw) * speed;
                    y += Math.Sin(pitch) * 0.3;

                    // 방향 변화 (더 자연스럽게)
                    double directionalNoise = SimplexNoise.Generate(x + directionalNoiseSeed, y + directionalNoiseSeed, 0.05, 2, 1.0, 0.5, 91357);
                    double turnBias = 0.7 - stability * 0.35;
                    yaw += (rand.NextDouble() - 0.5) * 0.3 * turnBias + directionalNoise * 0.35;
                    pitch += (rand.NextDouble() - 0.5) * 0.15 + directionalNoise * 0.18;
                    pitch = Math.Clamp(pitch, -0.7, 0.7);

                    // 범위 체크
                    if (x < 0 || x > 15 || z < 0 || z > 15) break;
                    if (y < 5 || y > 100) break;
                }
            }
        }
        
        /// <summary>
        /// 소형 동굴방들 생성
        /// </summary>
        private static int FloorDiv(int value, int divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be positive.");
            }

            int quotient = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && value < 0)
            {
                quotient--;
            }

            return quotient;
        }

        private void GenerateRegionalMainCaves(TerrainGenerationContext context, ChunkData chunk, double[,] caveStabilityField)
        {
            int regionSizeChunks = Math.Clamp(_worldGenConfig.Caves.RegionalMainCaveRegionSizeChunks, 1, 16);
            int regionX = FloorDiv(context.ChunkX, regionSizeChunks);
            int regionZ = FloorDiv(context.ChunkZ, regionSizeChunks);

            int regionOriginChunkX = regionX * regionSizeChunks;
            int regionOriginChunkZ = regionZ * regionSizeChunks;
            int regionOriginWorldX = regionOriginChunkX * 16;
            int regionOriginWorldZ = regionOriginChunkZ * 16;
            int regionWorldSize = regionSizeChunks * 16;

            int wormCountMin = Math.Max(0, _worldGenConfig.Caves.RegionalMainCaveWormCountMin);
            int wormCountMax = Math.Max(wormCountMin, _worldGenConfig.Caves.RegionalMainCaveWormCountMax);
            int stepsMin = Math.Max(16, _worldGenConfig.Caves.RegionalMainCaveStepsMin);
            int stepsMax = Math.Max(stepsMin, _worldGenConfig.Caves.RegionalMainCaveStepsMax);
            int minY = Math.Clamp(_worldGenConfig.Caves.RegionalMainCaveMinY, 5, 220);
            int maxY = Math.Clamp(_worldGenConfig.Caves.RegionalMainCaveMaxY, minY + 1, 240);
            double radiusMin = Math.Clamp(_worldGenConfig.Caves.RegionalMainCaveRadiusMin, 0.9, 12.0);
            double radiusMax = Math.Clamp(_worldGenConfig.Caves.RegionalMainCaveRadiusMax, radiusMin, 18.0);

            var rand = GetChunkRandom(regionX, regionZ, SaltCaveRegionalMain);
            int wormCount = wormCountMax == wormCountMin ? wormCountMin : rand.Next(wormCountMin, wormCountMax + 1);

            int chunkOriginWorldX = context.ChunkX * 16;
            int chunkOriginWorldZ = context.ChunkZ * 16;

            double radiusNoiseSeed = rand.NextDouble() * 1000.0;
            double directionalNoiseSeed = rand.NextDouble() * 500.0;

            for (int w = 0; w < wormCount; w++)
            {
                double worldX = regionOriginWorldX + rand.NextDouble() * regionWorldSize;
                double worldZ = regionOriginWorldZ + rand.NextDouble() * regionWorldSize;
                double y = rand.Next(minY, maxY);
                int steps = stepsMax == stepsMin ? stepsMin : rand.Next(stepsMin, stepsMax + 1);
                double yaw = rand.NextDouble() * Math.PI * 2.0;
                double pitch = (rand.NextDouble() - 0.5) * 0.35;
                double baseRadius = radiusMin + rand.NextDouble() * Math.Max(0.0, radiusMax - radiusMin);

                for (int s = 0; s < steps; s++)
                {
                    if (worldX < regionOriginWorldX ||
                        worldX >= regionOriginWorldX + regionWorldSize ||
                        worldZ < regionOriginWorldZ ||
                        worldZ >= regionOriginWorldZ + regionWorldSize)
                    {
                        break;
                    }

                    double localX = worldX - chunkOriginWorldX;
                    double localZ = worldZ - chunkOriginWorldZ;
                    double sampleX = Math.Clamp(localX, 0.0, 15.0);
                    double sampleZ = Math.Clamp(localZ, 0.0, 15.0);
                    double stability = SampleField(caveStabilityField, sampleX, sampleZ);

                    double radiusNoise = SimplexNoise.Generate(
                        worldX + radiusNoiseSeed,
                        worldZ + radiusNoiseSeed,
                        0.12,
                        2,
                        1.0,
                        0.55,
                        55127);
                    double radiusTurbulence = Math.Sin(s * 0.1) * 0.75 + radiusNoise * 0.6;
                    double pressureBias = 0.65 + (1.0 - stability) * 0.45;
                    double currentRadius = (baseRadius + radiusTurbulence) * pressureBias;
                    if (stability > 0.75)
                    {
                        currentRadius *= 1.05 + stability * 0.2;
                    }
                    currentRadius = Math.Clamp(currentRadius, 1.4, baseRadius + 2.4);

                    int r = (int)Math.Ceiling(currentRadius);
                    if (localX >= -r && localX <= 15 + r && localZ >= -r && localZ <= 15 + r)
                    {
                        int cx = (int)Math.Round(localX);
                        int cy = (int)Math.Round(y);
                        int cz = (int)Math.Round(localZ);
                        CarveSphere(chunk, cx, cy, cz, currentRadius);
                    }

                    double speed = 0.8 + rand.NextDouble() * 0.4;
                    worldX += Math.Cos(yaw) * speed;
                    worldZ += Math.Sin(yaw) * speed;
                    y += Math.Sin(pitch) * 0.28;

                    double directionalNoise = SimplexNoise.Generate(
                        worldX + directionalNoiseSeed,
                        y + directionalNoiseSeed,
                        0.05,
                        2,
                        1.0,
                        0.5,
                        91357);
                    double turnBias = 0.7 - stability * 0.35;
                    yaw += (rand.NextDouble() - 0.5) * 0.3 * turnBias + directionalNoise * 0.35;
                    pitch += (rand.NextDouble() - 0.5) * 0.12 + directionalNoise * 0.16;
                    pitch = Math.Clamp(pitch, -0.65, 0.65);

                    if (y < 5 || y > 110)
                    {
                        break;
                    }
                }
            }
        }

        private void GenerateSmallCaveRooms(ChunkData chunk, Random rand)
        {
            int roomCount = rand.Next(2, 6); // 2~5개의 소형 방
            
            for (int i = 0; i < roomCount; i++)
            {
                int roomX = rand.Next(3, 13);
                int roomY = rand.Next(10, 60);
                int roomZ = rand.Next(3, 13);
                int roomSize = rand.Next(3, 7);
                
                CarveRoom(chunk, roomX, roomY, roomZ, roomSize);
            }
        }
        
        /// <summary>
        /// 수직 동굴 (갱도) 생성
        /// </summary>
        private void GenerateVerticalShafts(ChunkData chunk, Random rand)
        {
            if (rand.NextDouble() < 0.3) // 30% 확률로 수직갱 생성
            {
                int shaftX = rand.Next(4, 12);
                int shaftZ = rand.Next(4, 12);
                int shaftTop = rand.Next(80, 120);
                int shaftBottom = rand.Next(10, 30);
                double shaftRadius = 1.5 + rand.NextDouble();
                
                for (int y = shaftBottom; y < shaftTop; y++)
                {
                    CarveSphere(chunk, shaftX, y, shaftZ, shaftRadius);
                    
                    // 가끔 측면 통로 생성
                    if (rand.NextDouble() < 0.1)
                    {
                        int sideLength = rand.Next(3, 8);
                        double sideDirection = rand.NextDouble() * Math.PI * 2;
                        
                        for (int i = 0; i < sideLength; i++)
                        {
                            int sideX = shaftX + (int)(Math.Cos(sideDirection) * i);
                            int sideZ = shaftZ + (int)(Math.Sin(sideDirection) * i);
                            CarveSphere(chunk, sideX, y, sideZ, 1.2);
                        }
                    }
                }

                if (rand.NextDouble() < 0.35)
                {
                    int fillStart = Math.Max(shaftBottom, 1);
                    int fillEnd = Math.Min(shaftBottom + 2, shaftTop - 1);
                    for (int y = fillStart; y <= fillEnd; y++)
                    {
                        chunk.SetBlock(shaftX, y, shaftZ, BlockType.Water);
                    }
                }
            }

        }

        /// <summary>
        /// Holds all configurable parameters for noise-based cave generation.
        /// </summary>
        private class CaveGenerationSettings
        {
            public double HorizontalFrequency { get; set; } = 0.0026;
            public double VerticalFrequency { get; set; } = 0.018;
            public double Threshold { get; set; } = 0.42;
            public double LavaThreshold { get; set; } = 0.28;
            public double WaterThreshold { get; set; } = 0.34;
            // Flooded caves are more likely to appear at lower elevations.
            public double FloodedCaveNoiseFrequency { get; set; } = 0.0031;
            public double FloodedCaveProximityToWaterTableWeight { get; set; } = 0.6;
            public double FloodedCaveThreshold { get; set; } = 0.75;
            public double HydrologyStabilityWeight { get; set; } = 0.45;
            public double FlowStabilityWeight { get; set; } = 0.25;
            public double RoughnessStabilityWeight { get; set; } = 0.1;
            public double RiverSuppressionWeight { get; set; } = 0.35;
        }

        private readonly CaveGenerationSettings _caveSettings;

        /// <summary>
        /// 노이즈 기반 동굴층 - 연속된 노이즈 필드를 사용하여 청크 경계를 넘는 동굴을 형성한다.
        /// 개선: 침수 동굴(Flooded Caves) 기능을 추가하여 특정 높이 이하, 그리고 수문학적 요인에 따라 물로 채워진 동굴을 생성합니다.
        /// </summary>
        private void GenerateNoiseCavePass(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] caveStabilityField,
            double[,] erosionRiskField,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            Vector2[,] hydrologyGradientField)
        {
            int baseX = context.ChunkX * 16;
            int baseZ = context.ChunkZ * 16;

            for (int x = 0; x < 16; x++)
            {
                int worldX = baseX + x;
                for (int z = 0; z < 16; z++)
                {
                    int worldZ = baseZ + z;
                    double erosionRisk = SampleField(erosionRiskField, x, z);
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double gradientStrength = Math.Clamp(hydrologyGradientField[x, z].Length(), 0.0, 1.75);
                    double gradientBias = gradientStrength * Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5);
                    double edgeDistance = Math.Min(Math.Min(x, 15 - x), Math.Min(z, 15 - z));
                    double seamRelax = Math.Clamp(1.0 - edgeDistance / 6.0, 0.0, 1.0) * _hydrologyEdgeVarianceClamp;
                    double moistureRetention = Math.Clamp(1.0 - hydrology, 0.0, 1.0);
                    double riverPressureRaw = Math.Abs(SampleRiverField(worldX, worldZ));
                    double riverPressure = Math.Clamp(riverPressureRaw / Math.Max(RiverBankThreshold, 1e-5), 0.0, 1.25);
                    double riparianSuppression = riverPressure * _caveRiverSuppressionWeight;

                    var warp = SimplexNoise.DomainWarp(worldX, worldZ, 0.00095, 0.0015, 22.0, 14.0, 53117);
                    double warpedX = worldX + warp.dx;
                    double warpedZ = worldZ + warp.dz;
                    
                    // 개선: 하드코딩된 값 대신 _caveSettings 사용
                    double horizontalNoise = SimplexNoise.Generate(warpedX, warpedZ, _caveSettings.HorizontalFrequency, 4, 1.0, 0.55, 640371);
                    double secondaryNoise = SimplexNoise.Generate(warpedX * 1.35, warpedZ * 1.35, _caveSettings.HorizontalFrequency * 1.6, 2, 1.0, 0.5, 93217);
                    double ridged = SampleRidgedNoise(warpedX * 0.85, warpedZ * 0.85, _caveSettings.HorizontalFrequency * 1.25, 3, 1.0, 0.5, 91357);
                    double striation = SimplexNoise.Generate(warpedX * 0.9, warpedZ * 0.9, _caveSettings.HorizontalFrequency * 1.1, 2, 1.0, 0.55, 128713) - 0.5;
                    double flowNoise = SimplexNoise.Generate(warpedX * 0.25 + 37.1, warpedZ * 0.25 - 11.4, _caveSettings.HorizontalFrequency * 0.4, 2, 1.0, 0.6, 87121) - 0.5;

                    // 신규: 침수 동굴을 위한 노이즈 값 계산
                    double floodedCaveNoise = NormalizeNoise(SimplexNoise.Generate(warpedX, warpedZ, _caveSettings.FloodedCaveNoiseFrequency, 3, 1.0, 0.5, 488171));

                    for (int y = 8; y < 120; y++)
                    {
                        double verticalNoise = SimplexNoise.Generate(warpedX, y, _caveSettings.VerticalFrequency, 3, 1.0, 0.62, 128947);
                        double density = Math.Abs(horizontalNoise) * 0.5 +
                                         Math.Abs(verticalNoise) * 0.35 +
                                         Math.Abs(secondaryNoise) * 0.2;
                        density = density * (0.65 + ridged * 0.35);
                        density -= Math.Clamp(striation, -0.35, 0.35) * 0.18;
                        density += flowNoise * 0.15;
                        density -= Math.Clamp(erosionRisk * 1.15, 0.0, 1.0) * 0.06;
                        density -= gradientBias * 0.06;
                        density -= seamRelax * 0.01;
                        density -= riparianSuppression * 0.03;

                        double strata = Math.Sin((warpedX + warpedZ + y * 1.5) * 0.012);
                        density -= Math.Clamp(strata * 0.08, -0.08, 0.08);

                        density -= Math.Clamp((y - 24) / 140.0, 0.0, 0.45);

                        int cachedSurface = surfaceCache[x, z];
                        if (cachedSurface > 0)
                        {
                            double ceilingDepth = Math.Clamp((cachedSurface - y) / 48.0, 0.0, 1.0);
                            density += ceilingDepth * 0.08;
                        }

                        double aquifer = SimplexNoise.Generate(worldX, y, 0.0042, 2, 1.0, 0.58, 147113);
                        double liquidity = Math.Clamp((GlobalWaterLevel - y) / 28.0, 0.0, 1.0);
                        double flowBias = Math.Clamp((flowNoise + 0.5) * 0.5 + liquidity * 0.5, 0.0, 1.0);
                        // 개선: 하드코딩된 값 대신 _caveSettings 사용
                        double dynamicThreshold = _caveSettings.Threshold - liquidity * 0.08 + aquifer * 0.02 - flowBias * 0.015;
                        double stability = SampleField(caveStabilityField, x, z);
                        dynamicThreshold -= (stability - 0.5) * 0.08;
                        dynamicThreshold -= Math.Clamp((erosionRisk - 0.35) * 0.08, -0.08, 0.08);
                        double hydrologyPenalty = hydrology * 0.02;
                        double flowSuppression = flow * 0.08;
                        dynamicThreshold += hydrologyPenalty + flowSuppression;
                        dynamicThreshold -= gradientBias * 0.05;
                        dynamicThreshold -= flow * 0.02;
                        dynamicThreshold += (0.5 - hydrology) * 0.02;
                        dynamicThreshold -= seamRelax * 0.03;
                        dynamicThreshold += riparianSuppression * 0.06;
                        double adjustedMoistureRetention = moistureRetention * (1.0 - riparianSuppression * 0.35);
                        dynamicThreshold += adjustedMoistureRetention * _caveMoistureRetentionWeight * 0.01;

                        if (density < dynamicThreshold)
                        {
                            var block = chunk.GetBlock(x, y, z);
                            if (block == BlockType.Air || block == BlockType.Water || block == BlockType.Lava)
                            {
                                continue;
                            }
                            
                            // 신규: 침수 동굴 로직
                            // y좌표가 해수면보다 낮고, 침수 동굴 노이즈 값이 특정 임계값을 넘을 경우 동굴을 물로 채웁니다.
                            double waterTableProximity = Math.Clamp((GlobalWaterLevel - y) / (double)GlobalWaterLevel, 0.0, 1.0);
                            double floodedCheck = floodedCaveNoise * (1.0 - _caveSettings.FloodedCaveProximityToWaterTableWeight) + 
                                                  waterTableProximity * _caveSettings.FloodedCaveProximityToWaterTableWeight +
                                                  erosionRisk * 0.12 +
                                                  gradientBias * 0.12 +
                                                  flow * 0.08 +
                                                  riparianSuppression * 0.35;

                            if (y < GlobalWaterLevel && floodedCheck > _caveSettings.FloodedCaveThreshold)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                            // 개선: 하드코딩된 값 대신 _caveSettings 사용
                            else if (density < Math.Min(_caveSettings.LavaThreshold, dynamicThreshold * 0.55) && y < 18)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Lava);
                            }
                            else if (density < _caveSettings.WaterThreshold + liquidity * 0.05 && y < GlobalWaterLevel - 6)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                            else
                            {
                                chunk.SetBlock(x, y, z, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        private void ApplyCaveHydrologyFeatures(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask)
        {
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltCaveHydro);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 12)
                    {
                        continue;
                    }

                    int y = Math.Min(surface - 4, 110);
                    while (y > 8)
                    {
                        while (y > 8 && chunk.GetBlock(x, y, z) != BlockType.Air)
                        {
                            y--;
                        }

                        if (y <= 8)
                        {
                            break;
                        }

                        int cavityTop = y;
                        while (y > 6 && chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            y--;
                        }

                        int cavityBottom = y + 1;
                        int cavityHeight = cavityTop - cavityBottom + 1;
                        if (cavityHeight < 4)
                        {
                            continue;
                        }

                        double poolChance = Math.Clamp((hydrology - 0.45) * 1.4, 0.0, 1.0);
                        if (poolChance <= 0.0 || rand.NextDouble() > poolChance)
                        {
                            continue;
                        }

                        int poolDepth = Math.Clamp((int)Math.Round(1 + hydrology * 4), 2, Math.Min(6, cavityHeight - 1));
                        int sedimentY = Math.Max(cavityBottom, cavityTop - poolDepth);
                        chunk.SetBlock(x, sedimentY, z, BlockType.Sand);

                        int waterStart = Math.Max(sedimentY + 1, cavityBottom);
                        int waterEnd = Math.Min(cavityTop - 1, sedimentY + poolDepth - 1);
                        for (int fillY = waterStart; fillY <= waterEnd; fillY++)
                        {
                            // 여기서는 침수 동굴과 달리, 얕은 웅덩이만 생성하므로 기존 로직 유지
                            var fillBlock = fillY < GlobalWaterLevel - 4 ? BlockType.Water : BlockType.Air;
                            chunk.SetBlock(x, fillY, z, fillBlock);
                        }

                        if (sedimentY - 1 >= 1 && rand.NextDouble() < 0.35)
                        {
                            chunk.SetBlock(x, sedimentY - 1, z, BlockType.Cobblestone);
                        }

                        break;
                    }
                }
            }
        }

        private void AddCaveDripstoneFeatures(TerrainGenerationContext context, ChunkData chunk)
        {
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltCaveDrip);
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int y = 6;
                    while (y < 120)
                    {
                        while (y < 120 && chunk.GetBlock(x, y, z) != BlockType.Air)
                        {
                            y++;
                        }

                        if (y >= 120)
                        {
                            break;
                        }

                        int cavityStart = y;
                        while (y < 120 && chunk.GetBlock(x, y, z) == BlockType.Air)
                        {
                            y++;
                        }
                        int cavityEnd = y - 1;
                        int cavityHeight = cavityEnd - cavityStart + 1;

                        if (cavityHeight >= 6 && rand.NextDouble() < 0.18)
                        {
                            int topSupportY = Math.Min(cavityEnd + 1, 255);
                            int bottomSupportY = Math.Max(cavityStart - 1, 0);
                            if (topSupportY >= 256 || bottomSupportY <= 0)
                            {
                                y = cavityEnd + 1;
                                continue;
                            }

                            var ceilingBlock = chunk.GetBlock(x, topSupportY, z);
                            var floorBlock = chunk.GetBlock(x, bottomSupportY, z);
                            if (ceilingBlock == BlockType.Air || floorBlock == BlockType.Air)
                            {
                                y = cavityEnd + 1;
                                continue;
                            }

                            int maxFeatureHeight = Math.Min(3, (cavityHeight - 2) / 2);
                            if (maxFeatureHeight > 0)
                            {
                                int stalagmiteHeight = 1 + rand.Next(maxFeatureHeight);
                                int stalactiteHeight = 1 + rand.Next(maxFeatureHeight);
                                if (stalagmiteHeight + stalactiteHeight >= cavityHeight - 1)
                                {
                                    stalagmiteHeight = Math.Max(1, maxFeatureHeight - 1);
                                    stalactiteHeight = Math.Max(1, maxFeatureHeight);
                                }

                                for (int i = 0; i < stalagmiteHeight; i++)
                                {
                                    int py = cavityStart + i;
                                    if (py >= cavityEnd)
                                    {
                                        break;
                                    }
                                    chunk.SetBlock(x, py, z, BlockType.Stone);
                                }

                                for (int i = 0; i < stalactiteHeight; i++)
                                {
                                    int py = cavityEnd - i;
                                    if (py <= cavityStart + 1)
                                    {
                                        break;
                                    }
                                    chunk.SetBlock(x, py, z, BlockType.Stone);
                                }
                            }
                        }

                        y = cavityEnd + 1;
                    }
                }
            }

        }

        /// <summary>
        /// 동굴방 조각하기
        /// </summary>
        private void CarveRoom(ChunkData chunk, int centerX, int centerY, int centerZ, int size)
        {
            for (int dx = -size; dx <= size; dx++)
            {
                for (int dy = -size/2; dy <= size/2; dy++) // 방은 수평적으로 더 넓게
                {
                    for (int dz = -size; dz <= size; dz++)
                    {
                        int x = centerX + dx;
                        int y = centerY + dy;
                        int z = centerZ + dz;
                        
                        if (x >= 0 && x < 16 && z >= 0 && z < 16 && y >= 1 && y < 255)
                        {
                            double dist = Math.Sqrt(dx*dx + dy*dy*1.5 + dz*dz); // 수직 압축
                            if (dist <= size)
                            {
                                var blockType = chunk.GetBlock(x, y, z);
                                if (blockType != BlockType.Air && blockType != BlockType.Water && blockType != BlockType.Lava)
                                {
                                    chunk.SetBlock(x, y, z, BlockType.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void IntegrateKarstInlets(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            RiverFieldCache riverField)
        {
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltCaveKarst);
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.62)
                    {
                        continue;
                    }

                    double catchment = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double riverIntensity = riverField.Intensity[x, z];
                    double riverAffinity = 1.0 - Math.Clamp(riverIntensity / (RiverBankThreshold * 1.15), 0.0, 1.0);
                    double weight = hydrology * 0.5 + catchment * 0.35 + riverAffinity * 0.25;
                    if (weight < 0.65 || rand.NextDouble() > weight * 0.4)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface < 18)
                    {
                        continue;
                    }

                    int shaftTop = Math.Max(8, surface - rand.Next(2, 5));
                    int shaftDepth = Math.Clamp((int)Math.Round(4 + weight * 6 + rand.NextDouble() * 2), 3, 12);
                    int shaftBottom = Math.Max(6, shaftTop - shaftDepth);
                    int radius = weight > 1.05 ? 2 : 1;

                    CarveKarstColumn(chunk, x, z, shaftTop, shaftBottom, radius);

                    if (shaftBottom + 2 < GlobalWaterLevel - 4 && rand.NextDouble() < hydrology)
                    {
                        FillKarstPool(chunk, x, z, shaftBottom, Math.Clamp((int)Math.Round(1 + weight * 3), 2, 5));
                    }

                    var slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    Vector2 direction = slopeDir;
                    if (riverField.Flow[x, z].LengthSquared() > 1e-4f)
                    {
                        direction = Vector2.Normalize(riverField.Flow[x, z]);
                    }
                    else if (direction.LengthSquared() > 1e-4f)
                    {
                        direction = Vector2.Normalize(direction);
                    }
                    else
                    {
                        continue;
                    }

                    CreateKarstTunnel(chunk, x, z, Math.Max(shaftBottom + 1, 6), direction, weight, rand);
                }
            }
        }

        private void CarveKarstColumn(ChunkData chunk, int centerX, int centerZ, int topY, int bottomY, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if ((dx * dx) + (dz * dz) > radius * radius + (radius == 1 ? 0 : 1))
                    {
                        continue;
                    }

                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    for (int y = topY; y >= bottomY && y > 0; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }
        }

        private void FillKarstPool(ChunkData chunk, int centerX, int centerZ, int baseY, int depth)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    chunk.SetBlock(x, baseY, z, BlockType.Sand);
                    for (int y = baseY + 1; y <= baseY + depth && y < GlobalWaterLevel - 2; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }
                }
            }
        }

        private void CreateKarstTunnel(ChunkData chunk, int startX, int startZ, int baseY, Vector2 direction, double weight, Random rand)
        {
            if (direction.LengthSquared() < 1e-4f || baseY <= 2)
            {
                return;
            }

            var dir = Vector2.Normalize(direction);
            double radius = Math.Clamp(1.3 + weight * 0.6, 1.3, 2.4);
            int steps = Math.Clamp((int)Math.Round(3 + weight * 4), 3, 8);
            double x = startX;
            double z = startZ;

            for (int i = 0; i < steps; i++)
            {
                int cx = (int)Math.Round(x);
                int cz = (int)Math.Round(z);
                if (cx < 1 || cx > 14 || cz < 1 || cz > 14)
                {
                    break;
                }

                CarveSphere(chunk, cx, baseY, cz, radius);
                if (rand.NextDouble() < 0.25 && baseY - 1 > 0)
                {
                    chunk.SetBlock(cx, baseY - 1, cz, BlockType.Clay);
                }

                x += dir.X + (rand.NextDouble() - 0.5) * 0.35;
                z += dir.Y + (rand.NextDouble() - 0.5) * 0.35;
            }
        }

        /// <summary>
        /// Carves a spherical pocket of air centered at (cx,cy,cz).
        /// </summary>
        private void CarveSphere(ChunkData chunk, int cx, int cy, int cz, double radius)
        {
            int r = (int)Math.Ceiling(radius);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        int x = cx + dx;
                        int y = cy + dy;
                        int z = cz + dz;
                        if (x < 0 || x >= 16 || z < 0 || z >= 16 || y < 1 || y >= 255) continue;

                        double dist2 = dx * dx + dy * dy + dz * dz;
                        if (dist2 <= radius * radius)
                        {
                            // Only carve solid materials
                            var bt = chunk.GetBlock(x, y, z);
                            if (bt != BlockType.Air && bt != BlockType.Water && bt != BlockType.Lava)
                                chunk.SetBlock(x, y, z, BlockType.Air);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 개선된 던전 생성 시스템 - 더 복잡하고 다양한 구조의 던전
        /// </summary>
        public void GenerateDungeonsInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltDungeon);
            if (rand.NextDouble() > 0.15) return; // 15% 확률로 증가

            // 던전 타입 결정
            DungeonType dungeonType = (DungeonType)rand.Next(3);
            
            switch (dungeonType)
            {
                case DungeonType.SimpleRoom:
                    GenerateSimpleDungeon(chunk, rand);
                    break;
                case DungeonType.MultiRoom:
                    GenerateMultiRoomDungeon(chunk, rand);
                    break;
                case DungeonType.Maze:
                    GenerateMazeDungeon(chunk, rand);
                    break;
            }
        }

        private double SampleRiverField(double worldX, double worldZ)
        {
            double baseFrequency = Math.Clamp(_riverNoiseScale * 0.08, 0.00005, 0.02);
            double warpSimplexFrequency = Math.Clamp(_riverNoiseScale * 0.053, 0.00001, 0.01);
            double warpPerlinFrequency = Math.Clamp(_riverNoiseScale * 0.106, 0.00002, 0.02);

            var warp = SimplexNoise.DomainWarp(worldX, worldZ, warpSimplexFrequency, warpPerlinFrequency, 20.0, 12.0, 91111);
            double sampleX = worldX + warp.dx;
            double sampleZ = worldZ + warp.dz;
            return SimplexNoise.Generate(sampleX, sampleZ, baseFrequency, 5, 1.0, 0.45, 91111);
        }

        private Vector2 ComputeRiverFlowVector(int worldX, int worldZ)
        {
            double gradientStep = Math.Clamp(1.0 / Math.Max(0.0001, _riverNoiseScale * 90.0), 0.35, 1.5);

            double forwardX = SampleRiverField(worldX + gradientStep, worldZ);
            double backwardX = SampleRiverField(worldX - gradientStep, worldZ);
            double forwardZ = SampleRiverField(worldX, worldZ + gradientStep);
            double backwardZ = SampleRiverField(worldX, worldZ - gradientStep);

            var flow = new Vector2((float)(forwardX - backwardX), (float)(forwardZ - backwardZ));
            if (flow.LengthSquared() < 1e-4f)
            {
                flow = new Vector2(
                    (float)(SampleRiverField(worldX + 23.0, worldZ + 7.0) - 0.5),
                    (float)(SampleRiverField(worldX - 19.0, worldZ - 11.0) - 0.5));
            }

            if (flow.LengthSquared() < 1e-6f)
            {
                return Vector2.UnitX;
            }

            return Vector2.Normalize(flow);
        }

        private static void ResolvePerpendicularOffset(Vector2 direction, int step, out int offsetX, out int offsetZ)
        {
            float absX = Math.Abs(direction.X);
            float absZ = Math.Abs(direction.Y);

            offsetX = absX >= 0.35f ? (direction.X >= 0f ? step : -step) : 0;
            offsetZ = absZ >= 0.35f ? (direction.Y >= 0f ? step : -step) : 0;

            if (offsetX == 0 && offsetZ == 0)
            {
                if (absX >= absZ)
                {
                    offsetX = direction.X >= 0f ? step : -step;
                }
                else
                {
                    offsetZ = direction.Y >= 0f ? step : -step;
                }
            }
        }

        private void ExpandRiverChannel(ChunkData chunk, int[,] surfaceCache, int x, int z, int riverSurface, Vector2 flowDir, double channelPressure)
        {
            if (channelPressure < 0.45)
            {
                return;
            }

            var perpendicular = new Vector2(-flowDir.Y, flowDir.X);
            if (perpendicular.LengthSquared() < 1e-6f)
            {
                perpendicular = Vector2.UnitX;
            }
            perpendicular = Vector2.Normalize(perpendicular);

            int reach = channelPressure > 0.9 ? 2 : 1;
            for (int step = 1; step <= reach; step++)
            {
                double floodStrength = Math.Clamp(channelPressure - 0.35 - 0.15 * (step - 1), 0.0, 1.0);
                if (floodStrength <= 0.0)
                {
                    continue;
                }

                ResolvePerpendicularOffset(perpendicular, step, out int offsetX, out int offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, floodStrength, riverSurface, true);

                ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), step, out offsetX, out offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, floodStrength, riverSurface, true);
            }
        }

        private HydrologyFieldCache GetHydrologyField(TerrainGenerationContext context, int[,] surfaceCache)
        {
            return context.GetOrAddMetadata(HydrologyFieldCacheKey, () =>
            {
                var hydrologyMask = BuildHydrologyMask(context.ChunkX, context.ChunkZ, surfaceCache);
                var flowAccumulation = BuildFlowAccumulation(surfaceCache);
                BlendHydrologySeams(context.ChunkX, context.ChunkZ, hydrologyMask, flowAccumulation);
                EnforceHydrologyEdgeConsistency(hydrologyMask, flowAccumulation);
                StabilizeHydrologyVariance(hydrologyMask, flowAccumulation, surfaceCache);
                StabilizeHydrologyGradients(hydrologyMask, flowAccumulation, surfaceCache);
                var initialCurvature = BuildHydrologyCurvature(hydrologyMask, flowAccumulation);
                StabilizeHydrologyWithCurvature(hydrologyMask, flowAccumulation, initialCurvature);
                StabilizeHydrologyWarping(context.ChunkX, context.ChunkZ, hydrologyMask, flowAccumulation);
                ProjectHydrologyEdgeFlux(context, surfaceCache, hydrologyMask, flowAccumulation);
                SmoothHydrologyFields(hydrologyMask, flowAccumulation);
                NormalizeHydrologyPressure(hydrologyMask, flowAccumulation);
                ClampHydrologyToWaterTable(surfaceCache, hydrologyMask, flowAccumulation);
                RelaxHydrologySeams(hydrologyMask, flowAccumulation);
                AnchorHydrologySeamsToSlope(surfaceCache, hydrologyMask, flowAccumulation);
                FeatherHydrologyEdges(hydrologyMask, flowAccumulation);
                var hydrologyCurvature = BuildHydrologyCurvature(hydrologyMask, flowAccumulation);
                var hydrologyGradient = BuildHydrologyGradient(hydrologyMask, flowAccumulation, surfaceCache, hydrologyCurvature);
                var erosionRisk = BuildErosionRiskField(surfaceCache, hydrologyMask, flowAccumulation);

                return new HydrologyFieldCache
                {
                    HydrologyMask = hydrologyMask,
                    FlowAccumulation = flowAccumulation,
                    HydrologyCurvature = hydrologyCurvature,
                    ErosionRisk = erosionRisk,
                    HydrologyGradient = hydrologyGradient
                };
            });
        }

        private RiverFieldCache GetRiverFieldCache(TerrainGenerationContext context)
        {
            var cache = context.GetOrAddMetadata(RiverFieldCacheKey, () => new RiverFieldCache());
            if (!cache.IsInitialized)
            {
                PopulateRiverFieldCache(cache, context);
                FeatherRiverEdgeIntensity(context, cache);
                cache.IsInitialized = true;
            }

            return cache;
        }

        private double[,] GetRiparianSaturation(TerrainGenerationContext context, HydrologyFieldCache hydrologyField, RiverFieldCache? riverField)
        {
            string cacheKey = riverField != null ? RiparianSaturationWithRiverCacheKey : RiparianSaturationCacheKey;
            return context.GetOrAddMetadata(cacheKey, () => BuildRiparianSaturation(hydrologyField, riverField));
        }

        private double[,] BuildRiparianSaturation(HydrologyFieldCache hydrologyField, RiverFieldCache? riverField)
        {
            int width = hydrologyField.HydrologyMask.GetLength(0);
            int depth = hydrologyField.HydrologyMask.GetLength(1);
            var riparian = new double[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double hydrology = hydrologyField.HydrologyMask[x, z];
                    double flow = hydrologyField.FlowAccumulation[x, z];
                    double gradientMag = hydrologyField.HydrologyGradient?[x, z].Length() ?? 0.0;
                    double curvature = hydrologyField.HydrologyCurvature?[x, z] ?? 0.0;
                    double riverPressure = riverField?.Intensity[x, z] ?? 0.0;
                    double erosionRisk = hydrologyField.ErosionRisk[x, z];
                    double riverAlignment = 0.0;
                    if (riverField != null && hydrologyField.HydrologyGradient != null)
                    {
                        Vector2 flowDir = riverField.Flow[x, z];
                        Vector2 gradDir = hydrologyField.HydrologyGradient[x, z];
                        if (flowDir.LengthSquared() > 1e-6 && gradDir.LengthSquared() > 1e-6)
                        {
                            flowDir = Vector2.Normalize(flowDir);
                            gradDir = Vector2.Normalize(gradDir);
                            riverAlignment = Math.Abs(Vector2.Dot(flowDir, gradDir));
                        }
                    }

                    double wetness = hydrology * 0.55 + flow * 0.25 + gradientMag * 0.1 + Math.Max(0.0, curvature) * _hydrologyCurvatureWeight * 0.1;
                    double combined = wetness * 0.65 + riverPressure * 0.45 + riverAlignment * 0.08;
                    combined += _riparianSaturationBoost * Math.Clamp(hydrology + flow + riverPressure, 0.0, 2.0);
                    double erosionPenalty = Math.Clamp(erosionRisk * 0.35, 0.0, 0.8);
                    combined = Math.Clamp(combined * (1.0 - erosionPenalty) + hydrology * 0.05, 0.0, 1.6);
                    riparian[x, z] = combined;
                }
            }

            int riparianIterations = Math.Max(1, _riparianSmoothIterations);
            double riparianBlend = _riparianSmoothBlend > 0.0
                ? Math.Clamp(_riparianSmoothBlend, 0.0, 0.95)
                : Math.Clamp(_hydrologySmoothBlend + 0.05, 0.0, 0.9);
            SmoothScalarField(riparian, riparianIterations, riparianBlend);
            if (_riparianBufferRadius > 0)
            {
                ExpandRiparianBuffer(riparian, _riparianBufferRadius, Math.Clamp(_riparianSaturationBoost + 0.35, 0.0, 1.5));
            }
            return riparian;
        }

        private static void ExpandRiparianBuffer(double[,] riparian, int radius, double strength)
        {
            int width = riparian.GetLength(0);
            int depth = riparian.GetLength(1);
            var buffered = (double[,])riparian.Clone();
            double attenuation = Math.Clamp(strength, 0.0, 2.0);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double maxNeighbor = riparian[x, z];
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > radius + 0.01)
                            {
                                continue;
                            }

                            double candidate = riparian[nx, nz] * Math.Clamp(1.0 - distance / (radius + 0.001), 0.0, 1.0);
                            if (candidate > maxNeighbor)
                            {
                                maxNeighbor = candidate;
                            }
                        }
                    }

                    buffered[x, z] = Math.Max(buffered[x, z], Math.Clamp(maxNeighbor * attenuation, 0.0, 2.0));
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    riparian[x, z] = buffered[x, z];
                }
            }
        }

        private void PopulateRiverFieldCache(RiverFieldCache cache, TerrainGenerationContext context)
        {
            int baseX = context.ChunkX * 16;
            int baseZ = context.ChunkZ * 16;

            for (int x = 0; x < 16; x++)
            {
                int worldX = baseX + x;
                for (int z = 0; z < 16; z++)
                {
                    int worldZ = baseZ + z;
                    double intensity = Math.Abs(SampleRiverField(worldX, worldZ));
                    cache.Intensity[x, z] = intensity;
                    cache.Flow[x, z] = ComputeRiverFlowVector(worldX, worldZ);
                }
            }

        }

        private void FeatherRiverEdgeIntensity(TerrainGenerationContext context, RiverFieldCache cache)
        {
            if (_riverEdgeFeather <= 0.0)
            {
                return;
            }

            double blendBase = Math.Clamp(_riverEdgeFeather, 0.0, 1.0);
            int radius = Math.Max(1, _hydrologyEdgeBlendRadius);
            int originX = context.ChunkX * 16;
            int originZ = context.ChunkZ * 16;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(15 - x, 15 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    int step = Math.Max(1, radius - edgeDistance);
                    int outwardX = x < 8 ? -step : step;
                    int outwardZ = z < 8 ? -step : step;
                    double riverSample = Math.Abs(SampleRiverField(originX + x + outwardX, originZ + z + outwardZ));
                    double riverPressure = Math.Clamp(1.0 - riverSample / Math.Max(RiverBankThreshold, 1e-5), 0.0, 1.0);
                    double blend = Math.Clamp(blendBase * (1.0 - edgeDistance / (double)radius), 0.0, 1.0);
                    double projected = Math.Clamp(cache.Intensity[x, z] * 0.65 + riverPressure * 0.35, 0.0, 1.25);
                    cache.Intensity[x, z] = cache.Intensity[x, z] * (1.0 - blend) + projected * blend;
                }
            }
        }

        private int[,] BuildSurfaceCache(ChunkData chunk)
        {
            var cache = new int[16, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    cache[x, z] = FindSurfaceLevel(chunk, x, z);
                }
            }

            return cache;
        }

        private static Vector2 ComputeTerrainSlopeDirection(int[,] surfaceCache, int x, int z)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);

            int leftIndex = Math.Max(x - 1, 0);
            int rightIndex = Math.Min(x + 1, width - 1);
            int backIndex = Math.Max(z - 1, 0);
            int forwardIndex = Math.Min(z + 1, depth - 1);

            float dx = surfaceCache[rightIndex, z] - surfaceCache[leftIndex, z];
            float dz = surfaceCache[x, forwardIndex] - surfaceCache[x, backIndex];

            var flow = new Vector2(-dx, -dz);
            if (flow.LengthSquared() < 1e-4f)
            {
                return Vector2.Zero;
            }

            return Vector2.Normalize(flow);
        }

        private double[,] BuildHydrologyMask(int chunkX, int chunkZ, int[,] surfaceCache)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);
            var mask = new double[width, depth];

            int minSurface = int.MaxValue;
            int maxSurface = int.MinValue;
            bool hasSurface = false;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    hasSurface = true;
                    minSurface = Math.Min(minSurface, surface);
                    maxSurface = Math.Max(maxSurface, surface);
                }
            }

            if (!hasSurface)
            {
                return mask;
            }

            double invRange = 1.0 / Math.Max(1, maxSurface - minSurface);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        mask[x, z] = 0.0;
                        continue;
                    }

                    double slopeAccum = 0.0;
                    int neighborCount = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            slopeAccum += Math.Abs(surface - neighborSurface);
                            neighborCount++;
                        }
                    }

                    double slope = neighborCount > 0 ? slopeAccum / neighborCount : 0.0;
                    slope = Math.Clamp(slope / 14.0, 0.0, 1.0);

                    double heightNormalized = Math.Clamp((surface - minSurface) * invRange, 0.0, 1.0);
                    double relief = 1.0 - heightNormalized;
                    double valley = Math.Clamp((GlobalWaterLevel - surface) / Math.Max(1.0, _hydrologyShorePush * 1.15), 0.0, 1.0);
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (edgeRadius * 1.2), 0.0, 1.0);

                    int worldX = chunkX * 16 + x;
                    int worldZ = chunkZ * 16 + z;
                    var warp = SimplexNoise.DomainWarp(worldX, worldZ, _hydrologyWarpFrequency, _hydrologyWarpFrequency * 1.4, _hydrologyWarpAmplitude, _hydrologyWarpAmplitude * 0.65, 82119);
                    double warpedX = worldX + warp.dx;
                    double warpedZ = worldZ + warp.dz;
                    double riverSample = Math.Abs(SampleRiverField(worldX, worldZ));
                    double riverPull = Math.Clamp(1.0 - riverSample / Math.Max(RiverBankThreshold, 1e-5), 0.0, 1.0);

                    double humidityFrequency = Math.Clamp(_riverNoiseScale * 0.65, 0.0008, 0.0065);
                    double humidityBase = SimplexNoise.Generate(warpedX + 13.5, warpedZ - 71.5, humidityFrequency, 4, 1.0, 0.58, 71337);
                    double humidityRipples = SimplexNoise.Generate(warpedX - 113.5, warpedZ + 21.5, humidityFrequency * 1.9, 2, 1.0, 0.5, 59113);
                    double humidity = 1.0 - Math.Abs((humidityBase * 0.65 + humidityRipples * 0.35) - 0.5) * (1.35 - 0.25 * _hydrologyFlowPersistence);
                    humidity = Math.Clamp(humidity, 0.0, 1.0);

                    double flowMemory = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
                    double hydrology = slope * (0.32 + 0.18 * flowMemory + 0.08 * edgeFalloff)
                        + valley * (0.34 + 0.12 * edgeFalloff)
                        + relief * 0.12
                        + humidity * (0.22 + 0.08 * flowMemory)
                        + riverPull * (0.06 + 0.08 * _hydrologyFlowGain)
                        + flowMemory * 0.05;

                    mask[x, z] = Math.Clamp(hydrology, 0.0, 1.0);
                }
            }

            return mask;
        }

        private double[,] BuildFlowAccumulation(int[,] surfaceCache)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius * 2);
            double waterRange = Math.Max(1.0, _hydrologyWaterTableClampRange);
            double flowMemory = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
            var raw = new double[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    double contribution = 0.0;
                    double slopeSum = 0.0;
                    int slopeSamples = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            int delta = neighborSurface - surface;
                            if (delta <= 1)
                            {
                                continue;
                            }

                            double slopeNorm = Math.Clamp(delta / (_hydrologySlopePenalty * 1.5), 0.0, 1.0);
                            double weight = 1.0 + Math.Min(6, delta) * 0.15;
                            double continuityWeight = 1.0 + (1.0 - slopeNorm) * 0.25 + flowMemory * 0.1;
                            if (dx != 0 && dz != 0)
                            {
                                weight *= 0.65;
                            }

                            contribution += weight * continuityWeight;
                            slopeSum += delta;
                            slopeSamples++;
                        }
                    }

                    double avgSlope = slopeSamples > 0 ? slopeSum / slopeSamples : 0.0;
                    double slopeFactor = Math.Clamp(avgSlope / Math.Max(1.0, _hydrologySlopePenalty * 1.25), 0.0, 1.0);
                    double slopeAttenuation = 1.0 - slopeFactor * 0.45;
                    double altitudeBias = Math.Clamp(1.0 - Math.Abs(GlobalWaterLevel - surface) / (waterRange * 1.35), 0.3, 1.0);
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    double edgeBoost = (1.0 - Math.Clamp(edgeDistance / (double)edgeRadius, 0.0, 1.0)) * (0.35 + flowMemory * 0.25);
                    double flowSeed = Math.Max(0.0, contribution * altitudeBias * slopeAttenuation);
                    flowSeed = flowSeed * (0.9 + flowMemory * 0.1) + edgeBoost * altitudeBias;

                    raw[x, z] = flowSeed;
                }
            }

            var smoothed = new double[width, depth];
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double total = raw[x, z];
                    double weightSum = 1.0;
                    int surface = surfaceCache[x, z];
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            double neighborSlope = Math.Abs(surfaceCache[nx, nz] - surface);
                            double slopePenalty = Math.Clamp(neighborSlope / Math.Max(1.0, _hydrologySlopePenalty * 1.2), 0.0, 1.0);
                            double smoothingWeight = (dx != 0 && dz != 0 ? 0.35 : 1.0) * (1.0 - slopePenalty * 0.55);
                            smoothingWeight *= 0.85 + flowMemory * 0.25;

                            total += raw[nx, nz] * smoothingWeight;
                            weightSum += smoothingWeight;
                        }
                    }

                    smoothed[x, z] = weightSum > 0.0 ? total / weightSum : raw[x, z];
                }
            }

            return smoothed;
        }

        private double[,] BuildHydrologyCurvature(double[,] hydrologyMask, double[,] flowAccumulation)
        {
            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var curvature = new double[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double center = hydrologyMask[x, z];
                    double variance = 0.0;
                    double neighborSum = 0.0;
                    int count = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            double delta = hydrologyMask[nx, nz] - center;
                            variance += delta * delta;
                            neighborSum += hydrologyMask[nx, nz];
                            count++;
                        }
                    }

                    double sigma = count > 0 ? Math.Sqrt(variance / count) : 0.0;
                    double neighborMean = count > 0 ? neighborSum / count : center;
                    double meanDrift = Math.Abs(neighborMean - center);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 10.0, 0.0, 1.0);
                    double curvatureScore = (sigma * 0.7) + (meanDrift * 0.45);
                    curvatureScore *= 0.9 + flow * 0.45;
                    curvature[x, z] = Math.Clamp(curvatureScore, 0.0, 1.4);
                }
            }

            int smoothIterations = Math.Clamp(_lakeBasinSmoothIterations, 0, 6);
            if (smoothIterations > 0)
            {
                SmoothScalarField(curvature, Math.Max(1, smoothIterations), Math.Clamp(_hydrologySmoothBlend + 0.1, 0.0, 0.95));
            }

            return curvature;
        }

        private Vector2[,] BuildHydrologyGradient(double[,] hydrologyMask, double[,] flowAccumulation, int[,] surfaceCache, double[,]? hydrologyCurvature = null)
        {
            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var gradient = new Vector2[width, depth];
            double slopeWeight = Math.Clamp(_hydrologyGradientSlopeWeight, 0.0, 1.0);
            float maxMagnitude = (float)Math.Clamp(_hydrologyGradientClamp, 0.1, 3.5);
            double curvatureWeight = Math.Clamp(_hydrologyCurvatureWeight, 0.0, 1.5);
            var curvatureField = hydrologyCurvature ?? BuildHydrologyCurvature(hydrologyMask, flowAccumulation);

            for (int x = 0; x < width; x++)
            {
                int left = Math.Max(x - 1, 0);
                int right = Math.Min(x + 1, width - 1);
                for (int z = 0; z < depth; z++)
                {
                    int back = Math.Max(z - 1, 0);
                    int forward = Math.Min(z + 1, depth - 1);
                    double gx = hydrologyMask[right, z] - hydrologyMask[left, z];
                    double gz = hydrologyMask[x, forward] - hydrologyMask[x, back];
                    Vector2 downhill = new((float)(gx * 0.5), (float)(gz * 0.5));
                    double curvature = Math.Clamp(curvatureField[x, z], 0.0, 1.2);
                    float curvatureBias = (float)Math.Clamp(curvature * curvatureWeight, 0.0, 1.2);
                    float curvatureDamping = Math.Clamp(1.0f - curvatureBias * 0.35f, 0.4f, 1.05f);
                    downhill *= curvatureDamping;

                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    double flowStrength = Math.Clamp(flowAccumulation[x, z] / 12.0, 0.0, 1.0);
                    if (slopeDir != Vector2.Zero)
                    {
                        double slopeBlend = slopeWeight * (0.35 + flowStrength * 0.65);
                        double baseMagnitude = downhill.Length();
                        if (baseMagnitude > 1e-6)
                        {
                            Vector2 downhillDir = Vector2.Normalize(downhill);
                            Vector2 blendedDir = Vector2.Normalize(Vector2.Lerp(downhillDir, slopeDir, (float)slopeBlend));
                            float targetMagnitude = (float)Math.Clamp(baseMagnitude * (0.85 + flowStrength * 0.35), 0.05, maxMagnitude);
                            downhill = blendedDir * targetMagnitude;
                        }
                        else
                        {
                            float fallbackMagnitude = (float)Math.Clamp(flowStrength * slopeBlend, 0.0, maxMagnitude);
                            downhill = slopeDir * fallbackMagnitude;
                        }
                    }

                    gradient[x, z] = ClampGradientMagnitude(downhill, maxMagnitude);
                }
            }

            // Gradient jitter at chunk edges leads to diverging river/lake channels between server and client.
            // Apply a short, slope-aware smoothing pass so caves/rivers/lakes see a stable downhill vector.
            var smoothed = new Vector2[width, depth];
            float baseBlend = (float)Math.Clamp(0.25 + _hydrologyGradientWeight * 0.35, 0.05, 0.85);
            float flowMemory = (float)Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
            float blendScale = Math.Clamp(baseBlend + flowMemory * 0.2f, 0.05f, 0.95f);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector2 raw = gradient[x, z];
                    Vector2 accum = raw;
                    float weight = 1.0f;
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    float flowStrength = (float)Math.Clamp(flowAccumulation[x, z] / 12.0, 0.0, 1.0);
                    double curvature = Math.Clamp(curvatureField[x, z], 0.0, 1.2);
                    float curvatureBias = (float)Math.Clamp(curvature * curvatureWeight, 0.0, 1.2);
                    float hydrologyBias = (float)Math.Clamp(0.35 + hydrology * 0.45 + flowMemory * 0.25 + slopeWeight * 0.15 + flowStrength * 0.15 + curvatureBias * 0.2, 0.35, 0.98);

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            Vector2 neighbor = gradient[nx, nz];
                            float alignment = 0.5f;
                            if (raw.LengthSquared() > 1e-6f && neighbor.LengthSquared() > 1e-6f)
                            {
                                Vector2 rawDir = Vector2.Normalize(raw);
                                Vector2 neighborDir = Vector2.Normalize(neighbor);
                                alignment = (Vector2.Dot(rawDir, neighborDir) + 1.0f) * 0.5f;
                            }

                            float w = (0.35f + hydrologyBias * 0.65f) * (0.65f + alignment * 0.35f);
                            w *= 1.0f + curvatureBias * 0.35f;
                            accum += neighbor * w;
                            weight += w;
                        }
                    }

                    Vector2 averaged = weight > 0.0f ? accum / weight : raw;
                    smoothed[x, z] = ClampGradientMagnitude(Vector2.Lerp(raw, averaged, blendScale), maxMagnitude);
                }
            }

            ApplyHydrologyGradientStability(smoothed, maxMagnitude);
            ApplyHydrologyCurvatureRelaxation(smoothed, curvatureField, flowAccumulation, maxMagnitude);
            return smoothed;
        }

        private static Vector2 ClampGradientMagnitude(Vector2 value, float maxMagnitude)
        {
            float length = value.Length();
            if (length > maxMagnitude && length > 1e-6f)
            {
                return value / length * maxMagnitude;
            }

            return value;
        }

        private void ApplyHydrologyGradientStability(Vector2[,] gradient, float maxMagnitude)
        {
            if (_hydrologyGradientStabilityIterations <= 0 || _hydrologyGradientStabilityBlend <= 0.0 || gradient == null)
            {
                return;
            }

            int width = gradient.GetLength(0);
            int depth = gradient.GetLength(1);
            var buffer = new Vector2[width, depth];
            float blend = (float)Math.Clamp(_hydrologyGradientStabilityBlend, 0.0, 1.0);

            for (int iteration = 0; iteration < _hydrologyGradientStabilityIterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Vector2 accum = gradient[x, z];
                        float weight = 1.0f;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                float neighborWeight = dx != 0 && dz != 0 ? 0.35f : 1.0f;
                                accum += gradient[nx, nz] * neighborWeight;
                                weight += neighborWeight;
                            }
                        }

                        Vector2 average = weight > 0.0f ? accum / weight : gradient[x, z];
                        buffer[x, z] = ClampGradientMagnitude(Vector2.Lerp(gradient[x, z], average, blend), maxMagnitude);
                    }
                }

                Array.Copy(buffer, gradient, gradient.Length);
            }
        }

        private void ApplyHydrologyCurvatureRelaxation(Vector2[,] gradient, double[,] curvatureField, double[,] flowAccumulation, float maxMagnitude)
        {
            if (gradient == null || curvatureField == null || _hydrologyCurvatureWeight <= 0.0)
            {
                return;
            }

            int width = gradient.GetLength(0);
            int depth = gradient.GetLength(1);
            var buffer = new Vector2[width, depth];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector2 raw = gradient[x, z];
                    double curvature = Math.Clamp(curvatureField[x, z], 0.0, 1.2);
                    float curvatureBias = (float)Math.Clamp(curvature * _hydrologyCurvatureWeight, 0.0, 1.2);
                    if (curvatureBias <= 0.0f)
                    {
                        buffer[x, z] = raw;
                        continue;
                    }

                    float flow = (float)Math.Clamp(flowAccumulation[x, z] / 10.0, 0.0, 1.0);
                    float blend = Math.Clamp(curvatureBias * (0.35f + flow * 0.45f), 0.0f, 0.8f);
                    Vector2 accum = raw;
                    float weight = 1.0f;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                            {
                                continue;
                            }

                            Vector2 neighbor = gradient[nx, nz];
                            float alignment = 0.5f;
                            if (raw.LengthSquared() > 1e-6f && neighbor.LengthSquared() > 1e-6f)
                            {
                                Vector2 rawDir = Vector2.Normalize(raw);
                                Vector2 neighborDir = Vector2.Normalize(neighbor);
                                alignment = (Vector2.Dot(rawDir, neighborDir) + 1.0f) * 0.5f;
                            }

                            float w = (0.6f + alignment * 0.4f) * (1.0f + curvatureBias * 0.35f) * (0.85f + flow * 0.25f);
                            accum += neighbor * w;
                            weight += w;
                        }
                    }

                    Vector2 averaged = weight > 0.0f ? accum / weight : raw;
                    buffer[x, z] = ClampGradientMagnitude(Vector2.Lerp(raw, averaged, blend), maxMagnitude);
                }
            }

            Array.Copy(buffer, gradient, gradient.Length);
        }

        private void StabilizeHydrologyVariance(double[,] hydrologyMask, double[,] flowAccumulation, int[,] surfaceCache)
        {
            if (_hydrologyVarianceBlend <= 0.0 || hydrologyMask == null || flowAccumulation == null || surfaceCache == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var hydroBuffer = new double[width, depth];
            var flowBuffer = new double[width, depth];
            double baseBlend = Math.Clamp(_hydrologyVarianceBlend, 0.0, 1.0);
            double clampWeight = Math.Clamp(_hydrologyVarianceClamp, 0.0, 2.0);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double flow = flowAccumulation[x, z];
                    double sumHydro = 0.0;
                    double sumHydroSq = 0.0;
                    double sumFlow = 0.0;
                    double sumFlowSq = 0.0;
                    int samples = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int sampleX = Math.Clamp(x + dx, 0, width - 1);
                            int sampleZ = Math.Clamp(z + dz, 0, depth - 1);
                            double sampleHydro = hydrologyMask[sampleX, sampleZ];
                            double sampleFlow = flowAccumulation[sampleX, sampleZ];
                            sumHydro += sampleHydro;
                            sumHydroSq += sampleHydro * sampleHydro;
                            sumFlow += sampleFlow;
                            sumFlowSq += sampleFlow * sampleFlow;
                            samples++;
                        }
                    }

                    if (samples <= 0)
                    {
                        hydroBuffer[x, z] = hydrology;
                        flowBuffer[x, z] = flow;
                        continue;
                    }

                    double meanHydro = sumHydro / samples;
                    double meanFlow = sumFlow / samples;
                    double varianceHydro = Math.Max(0.0, sumHydroSq / samples - meanHydro * meanHydro);
                    double varianceFlow = Math.Max(0.0, sumFlowSq / samples - meanFlow * meanFlow);
                    double stdHydro = Math.Sqrt(varianceHydro);
                    double stdFlow = Math.Sqrt(varianceFlow);

                    double clampHydro = Math.Clamp(1.0 - stdHydro * clampWeight, 0.35, 1.0);
                    double clampFlow = Math.Clamp(1.0 - stdFlow * clampWeight * 0.75, 0.35, 1.0);
                    int surface = surfaceCache[x, z];
                    double slopeWeight = surface > 0 ? Math.Clamp(1.0 - ComputeLocalRelief(surfaceCache, x, z, 2) / 12.0, 0.35, 1.0) : 1.0;
                    double varianceBlend = Math.Clamp(baseBlend * (0.8 + (1.0 - clampHydro) * 0.35) * slopeWeight, 0.0, 0.95);

                    double targetHydro = Math.Clamp(meanHydro + (hydrology - meanHydro) * clampHydro, 0.0, 1.0);
                    double targetFlow = Math.Max(0.0, meanFlow + (flow - meanFlow) * clampFlow);
                    hydroBuffer[x, z] = Math.Clamp(hydrology + (targetHydro - hydrology) * varianceBlend, 0.0, 1.0);
                    flowBuffer[x, z] = Math.Max(0.0, flow + (targetFlow - flow) * varianceBlend);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    hydrologyMask[x, z] = hydroBuffer[x, z];
                    flowAccumulation[x, z] = flowBuffer[x, z];
                }
            }
        }

        private void StabilizeHydrologyGradients(double[,] hydrologyMask, double[,] flowAccumulation, int[,] surfaceCache)
        {
            if (hydrologyMask == null || flowAccumulation == null || surfaceCache == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var hydrologyBuffer = new double[width, depth];
            var flowBuffer = new double[width, depth];
            double flowPersistence = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
            double gradientWeight = Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5);
            double gradientSlopeWeight = Math.Clamp(_hydrologyGradientSlopeWeight, 0.0, 1.0);
            double gradientClamp = Math.Max(1e-4, _hydrologyGradientClamp);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double flow = flowAccumulation[x, z];
                    double blendedHydrology = hydrology;
                    double blendedFlow = flow;
                    double weight = 1.0;
                    double gradX = hydrologyMask[Math.Min(width - 1, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
                    double gradZ = hydrologyMask[x, Math.Min(depth - 1, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];
                    double gradientMagnitude = Math.Sqrt(gradX * gradX + gradZ * gradZ);
                    double clampedGradient = Math.Min(gradientMagnitude, gradientClamp);
                    double gradientNormalized = Math.Clamp(clampedGradient / gradientClamp, 0.0, 1.0);
                    Vector2 gradientDir = gradientMagnitude > 1e-6
                        ? Vector2.Normalize(new Vector2((float)gradX, (float)gradZ))
                        : Vector2.Zero;
                    double gradientAnisotropy = 1.0 + clampedGradient * gradientWeight * 0.35;
                    double gradientDamping = Math.Clamp(1.0 - gradientNormalized * (0.3 + gradientSlopeWeight * 0.25), 0.55, 1.0);

                    int surface = surfaceCache[x, z];
                    double shoreBias = Math.Clamp((GlobalWaterLevel - surface) / _hydrologyShorePush, 0.0, 1.0);
                    shoreBias = Math.Max(0.1, shoreBias * 0.6);

                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                        {
                            if (offsetX == 0 && offsetZ == 0)
                            {
                                continue;
                            }

                            int sampleX = x + offsetX;
                            int sampleZ = z + offsetZ;
                            if (sampleX < 0 || sampleX >= width || sampleZ < 0 || sampleZ >= depth)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[sampleX, sampleZ];
                            double slopePenalty = Math.Clamp(Math.Abs(surface - neighborSurface) / _hydrologySlopePenalty, 0.0, 1.0);
                            double smoothingWeight = (1.0 - slopePenalty * 0.45) * (0.85 + flowPersistence * 0.15);

                            if (gradientDir.LengthSquared() > 1e-5f)
                            {
                                var neighborDir = new Vector2(offsetX, offsetZ);
                                if (neighborDir.LengthSquared() > 1e-5f)
                                {
                                    neighborDir = Vector2.Normalize(neighborDir);
                                    double alignment = Math.Max(0.0, Vector2.Dot(gradientDir, neighborDir));
                                    double alignedWeight = 0.65 + alignment * 0.35;
                                    smoothingWeight *= alignedWeight * gradientAnisotropy;
                                }
                            }

                            blendedHydrology += hydrologyMask[sampleX, sampleZ] * smoothingWeight;
                            blendedFlow += flowAccumulation[sampleX, sampleZ] * smoothingWeight;
                            weight += smoothingWeight;
                        }
                    }

                    double hydrologyBlendBase = Math.Clamp(0.35 + shoreBias * _hydrologyFlowGain + flowPersistence * 0.1, 0.0, 1.0);
                    double flowBlendBase = Math.Clamp(0.25 + shoreBias * _hydrologyFlowGain * 0.65 + flowPersistence * 0.15, 0.0, 1.0);
                    double gradientBlend = gradientNormalized * (0.2 + flowPersistence * 0.15);
                    double hydrologyBlend = Math.Clamp(hydrologyBlendBase * gradientDamping + gradientBlend * 0.5, 0.0, 1.0);
                    double flowBlend = Math.Clamp(flowBlendBase * gradientDamping + gradientBlend, 0.0, 1.0);
                    hydrologyBuffer[x, z] = Math.Clamp(hydrology + (blendedHydrology / weight - hydrology) * hydrologyBlend, 0.0, 1.0);
                    flowBuffer[x, z] = Math.Max(0.0, flow + (blendedFlow / weight - flow) * flowBlend);
                }
            }

            if (_hydrologyDirectionalIterations > 0 && _hydrologyDirectionalBlend > 0.0)
            {
                var directionalHydro = new double[width, depth];
                var directionalFlow = new double[width, depth];
                double directionalBlend = Math.Clamp(_hydrologyDirectionalBlend, 0.0, 1.0);
                double divergenceClamp = Math.Max(1e-5, _hydrologyFlowDivergenceClamp);

                for (int iteration = 0; iteration < _hydrologyDirectionalIterations; iteration++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            double hydrology = hydrologyBuffer[x, z];
                            double flow = flowBuffer[x, z];

                            double gradXDir = hydrologyBuffer[Math.Min(width - 1, x + 1), z] - hydrologyBuffer[Math.Max(0, x - 1), z];
                            double gradZDir = hydrologyBuffer[x, Math.Min(depth - 1, z + 1)] - hydrologyBuffer[x, Math.Max(0, z - 1)];
                            var gradient = new Vector2((float)gradXDir, (float)gradZDir);

                            Vector2 primaryDir = gradient.LengthSquared() > 1e-5f
                                ? Vector2.Normalize(gradient)
                                : ComputeTerrainSlopeDirection(surfaceCache, x, z);
                            if (primaryDir.LengthSquared() < 1e-5f)
                            {
                                primaryDir = Vector2.UnitX;
                            }

                            var perpendicular = new Vector2(-primaryDir.Y, primaryDir.X);
                            if (perpendicular.LengthSquared() < 1e-5f)
                            {
                                perpendicular = Vector2.UnitY;
                            }

                            int mainX = Math.Clamp(x + Math.Sign(primaryDir.X), 0, width - 1);
                            int mainZ = Math.Clamp(z + Math.Sign(primaryDir.Y), 0, depth - 1);
                            if (mainX == x && mainZ == z)
                            {
                                mainX = Math.Min(width - 1, x + 1);
                            }

                            int crossX1 = Math.Clamp(x + Math.Sign(perpendicular.X), 0, width - 1);
                            int crossZ1 = Math.Clamp(z + Math.Sign(perpendicular.Y), 0, depth - 1);
                            int crossX2 = Math.Clamp(x - Math.Sign(perpendicular.X), 0, width - 1);
                            int crossZ2 = Math.Clamp(z - Math.Sign(perpendicular.Y), 0, depth - 1);

                            double primaryHydro = hydrologyBuffer[mainX, mainZ];
                            double primaryFlow = flowBuffer[mainX, mainZ];
                            double lateralHydro = 0.5 * (hydrologyBuffer[crossX1, crossZ1] + hydrologyBuffer[crossX2, crossZ2]);
                            double lateralFlow = 0.5 * (flowBuffer[crossX1, crossZ1] + flowBuffer[crossX2, crossZ2]);

                            double gradientStrength = Math.Min(Math.Sqrt(gradXDir * gradXDir + gradZDir * gradZDir), _hydrologyGradientClamp);
                            double normalizedGradient = Math.Clamp(gradientStrength / Math.Max(1e-4, _hydrologyGradientClamp), 0.0, 1.0);
                            double anisotropy = Math.Clamp(0.65 + normalizedGradient * 0.25 + Math.Clamp(flow * 0.05, 0.0, 0.2), 0.5, 1.3);
                            double divergence = Math.Abs(hydrology - primaryHydro) + Math.Abs(hydrology - lateralHydro);
                            double divergenceWeight = Math.Clamp(1.0 - divergence / divergenceClamp, 0.25, 1.0);

                            double targetHydro = (hydrology + primaryHydro * anisotropy + lateralHydro * (0.85 - normalizedGradient * 0.15)) / (1.0 + anisotropy);
                            double targetFlow = (flow + primaryFlow * anisotropy + lateralFlow * (0.85 - normalizedGradient * 0.15)) / (1.0 + anisotropy);

                            directionalHydro[x, z] = Math.Clamp(hydrology + (targetHydro - hydrology) * directionalBlend * divergenceWeight, 0.0, 1.0);
                            directionalFlow[x, z] = Math.Max(0.0, flow + (targetFlow - flow) * directionalBlend * divergenceWeight);
                        }
                    }

                    Array.Copy(directionalHydro, hydrologyBuffer, hydrologyBuffer.Length);
                    Array.Copy(directionalFlow, flowBuffer, flowBuffer.Length);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    hydrologyMask[x, z] = hydrologyBuffer[x, z];
                    flowAccumulation[x, z] = flowBuffer[x, z];
                }
            }
        }

        private void StabilizeHydrologyWithCurvature(double[,] hydrologyMask, double[,] flowAccumulation, double[,]? hydrologyCurvature)
        {
            if (_hydrologyGradientStabilityIterations <= 0 || _hydrologyGradientStabilityBlend <= 0.0 || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var buffer = new double[width, depth];
            double blend = Math.Clamp(_hydrologyGradientStabilityBlend, 0.0, 1.0);
            double gradientClamp = Math.Max(1e-4, _hydrologyGradientClamp);
            double curvatureWeight = Math.Clamp(_hydrologyCurvatureWeight, 0.0, 1.5);
            double flowPersistence = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);

            for (int iteration = 0; iteration < _hydrologyGradientStabilityIterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        double hydrology = hydrologyMask[x, z];
                        double flow = flowAccumulation[x, z];
                        double curvature = hydrologyCurvature?[x, z] ?? 0.0;

                        double accum = hydrology;
                        double weight = 1.0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int sampleX = Math.Clamp(x + dx, 0, width - 1);
                                int sampleZ = Math.Clamp(z + dz, 0, depth - 1);
                                double neighborHydro = hydrologyMask[sampleX, sampleZ];
                                double neighborFlow = flowAccumulation[sampleX, sampleZ];

                                double gradX = hydrologyMask[Math.Min(width - 1, sampleX + 1), sampleZ] - hydrologyMask[Math.Max(0, sampleX - 1), sampleZ];
                                double gradZ = hydrologyMask[sampleX, Math.Min(depth - 1, sampleZ + 1)] - hydrologyMask[sampleX, Math.Max(0, sampleZ - 1)];
                                double gradientStrength = Math.Min(Math.Sqrt(gradX * gradX + gradZ * gradZ), gradientClamp);
                                double continuity = 1.0 - Math.Clamp(Math.Abs(hydrology - neighborHydro), 0.0, 1.0);
                                double stabilityWeight = 1.0
                                    + gradientStrength * 0.35
                                    + curvature * curvatureWeight * 0.2
                                    + neighborFlow * 0.05
                                    + flowPersistence * 0.1
                                    + flow * 0.05;

                                double neighborWeight = Math.Max(0.05, stabilityWeight * (0.65 + continuity * 0.35));
                                accum += neighborHydro * neighborWeight;
                                weight += neighborWeight;
                            }
                        }

                        double targetHydro = weight > 0.0 ? accum / weight : hydrology;
                        buffer[x, z] = Math.Clamp(hydrology + (targetHydro - hydrology) * blend, 0.0, 1.0);
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        hydrologyMask[x, z] = buffer[x, z];
                    }
                }
            }
        }

        private void ProjectHydrologyEdgeFlux(TerrainGenerationContext context, int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (_hydrologyEdgeFluxBlend <= 0.0)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int radius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double blendBase = Math.Clamp(_hydrologyEdgeFluxBlend, 0.0, 1.0);
            int originX = context.ChunkX * 16;
            int originZ = context.ChunkZ * 16;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    double slopeMagnitude = slopeDir.Length();
                    double slopeWeight = Math.Clamp(slopeMagnitude * (_hydrologyGradientWeight * 0.6 + 0.2), 0.0, 1.0);

                    int outwardX = x < width / 2 ? -1 : 1;
                    int outwardZ = z < depth / 2 ? -1 : 1;
                    int sampleStep = Math.Max(1, radius - edgeDistance);
                    int sampleWorldX = originX + x + (Math.Abs(slopeDir.X) > 1e-4f ? Math.Sign(slopeDir.X) : outwardX) * sampleStep;
                    int sampleWorldZ = originZ + z + (Math.Abs(slopeDir.Y) > 1e-4f ? Math.Sign(slopeDir.Y) : outwardZ) * sampleStep;

                    double riverIntensity = Math.Abs(SampleRiverField(sampleWorldX, sampleWorldZ));
                    double riverPressure = Math.Clamp(1.0 - riverIntensity / Math.Max(RiverBankThreshold, 1e-5), 0.0, 1.0);
                    double wetness = hydrologyMask[x, z] + flowAccumulation[x, z] * 0.1;
                    double projection = Math.Clamp(wetness + riverPressure * (0.35 + slopeWeight * 0.25), 0.0, 1.25);
                    double edgeBlend = Math.Clamp(blendBase * (1.0 - edgeDistance / (double)radius) * (0.85 + slopeWeight * 0.35), 0.0, 1.0);

                    hydrologyMask[x, z] = hydrologyMask[x, z] * (1.0 - edgeBlend) + projection * edgeBlend;

                    double projectedFlow = flowAccumulation[x, z] * (1.0 + riverPressure * 0.35 + slopeWeight * 0.2);
                    flowAccumulation[x, z] = flowAccumulation[x, z] * (1.0 - edgeBlend * 0.5) + projectedFlow * (edgeBlend * 0.5);
                }
            }
        }

        private void SmoothHydrologyFields(double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (_hydrologySmoothIterations <= 0 || _hydrologySmoothBlend <= 0.0)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            var hydroBuffer = new double[width, depth];
            var flowBuffer = new double[width, depth];
            double baseBlend = Math.Clamp(_hydrologySmoothBlend, 0.0, 1.0);
            double anisotropy = Math.Clamp(0.3 + _hydrologyFlowPersistence * 0.55 + _hydrologyContinuityWeight * 0.25, 0.0, 1.0);

            for (int iteration = 0; iteration < _hydrologySmoothIterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        double hydrology = hydrologyMask[x, z];
                        double flow = flowAccumulation[x, z];
                        double weightedHydrology = hydrology;
                        double weightedFlow = flow;
                        double weightTotal = 1.0;
                        var gradient = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                        Vector2 downhill = gradient.LengthSquared() > 1e-5f ? Vector2.Normalize(-gradient) : Vector2.Zero;
                        double maxAlignment = 0.0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                double neighborHydrology = hydrologyMask[nx, nz];
                                double neighborFlow = flowAccumulation[nx, nz];

                                var neighborDir = new Vector2(dx, dz);
                                if (neighborDir.LengthSquared() > 1e-4f)
                                {
                                    neighborDir = Vector2.Normalize(neighborDir);
                                }

                                double alignment = downhill == Vector2.Zero ? 0.0 : Math.Max(0.0, Vector2.Dot(downhill, neighborDir));
                                maxAlignment = Math.Max(maxAlignment, alignment);

                                double gradientDelta = Math.Abs(hydrology - neighborHydrology);
                                double gradientWeight = Math.Clamp(1.0 - gradientDelta * (0.45 + _hydrologyContinuityWeight * 0.35), 0.25, 1.0);
                                double continuityWeight = 1.0 + _hydrologyContinuityWeight * 0.35;
                                double alignmentWeight = 1.0 + alignment * (0.8 + anisotropy * 0.6);
                                double baseWeight = 1.0 + hydrology * 0.3 + neighborHydrology * 0.35 + flow * 0.1 + neighborFlow * 0.1;
                                double finalWeight = baseWeight * alignmentWeight * gradientWeight * continuityWeight;

                                weightedHydrology += neighborHydrology * finalWeight;
                                weightedFlow += neighborFlow * finalWeight * (1.0 + alignment * 0.5);
                                weightTotal += finalWeight;
                            }
                        }

                        double hydroTarget = weightTotal > 0.0 ? weightedHydrology / weightTotal : hydrology;
                        double flowTarget = weightTotal > 0.0 ? weightedFlow / weightTotal : flow;
                        double blend = Math.Clamp(baseBlend + hydrology * 0.12 + maxAlignment * 0.18 + _hydrologyFlowPersistence * 0.08, 0.0, 0.95);
                        hydroBuffer[x, z] = hydrology * (1.0 - blend) + hydroTarget * blend;
                        flowBuffer[x, z] = Math.Max(0.0, flow * (1.0 - blend) + flowTarget * blend);
                    }
                }

                Array.Copy(hydroBuffer, hydrologyMask, hydrologyMask.Length);
                Array.Copy(flowBuffer, flowAccumulation, flowAccumulation.Length);
            }
        }

        private static void SmoothScalarField(double[,] field, int iterations, double blend)
        {
            int width = field.GetLength(0);
            int depth = field.GetLength(1);
            var scratch = new double[width, depth];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        double weightedSum = 0.0;
                        double weightTotal = 0.0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                double weight = dx == 0 && dz == 0 ? 1.5 : 1.0;
                                weightedSum += field[nx, nz] * weight;
                                weightTotal += weight;
                            }
                        }

                        double average = weightTotal > 0.0 ? weightedSum / weightTotal : field[x, z];
                        scratch[x, z] = field[x, z] * (1.0 - blend) + average * blend;
                    }
                }

                Array.Copy(scratch, field, field.Length);
            }
        }

        private double[,] BuildErosionRiskField(int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);
            var risk = new double[width, depth];
            double surfaceRange = Math.Max(1, MaxSurfaceHeight - MinSurfaceHeight);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        risk[x, z] = 0.0;
                        continue;
                    }

                    double slope = ComputeLocalRelief(surfaceCache, x, z, 3);
                    double slopeNorm = Math.Clamp(slope / 10.0, 0.0, 1.0);
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double altitude = Math.Clamp((surface - MinSurfaceHeight) / surfaceRange, 0.0, 1.0);
                    double valley = Math.Clamp((GlobalWaterLevel - surface) / 16.0, 0.0, 1.0);
                    double exposure = Math.Clamp((1.0 - altitude) * 0.65 + valley * 0.45, 0.0, 1.0);

                    double combined = hydrology * 0.4 + flow * 0.28 + exposure * 0.2 + slopeNorm * 0.15;
                    risk[x, z] = Math.Clamp(combined, 0.0, 1.0);
                }
            }

            int iterations = Math.Max(1, _hydrologySmoothIterations);
            double blend = _hydrologySmoothBlend > 0.0 ? _hydrologySmoothBlend : 0.6;
            SmoothScalarField(risk, iterations, blend);
            return risk;
        }

        private static (double hydro, double flow) ComputeInteriorHydrologyAverages(double[,] hydrologyMask, double[,] flowAccumulation, int edgeRadius)
        {
            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);

            double hydroSum = 0.0;
            double flowSum = 0.0;
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(
                        Math.Min(x, z),
                        Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance <= edgeRadius)
                    {
                        continue;
                    }

                    hydroSum += hydrologyMask[x, z];
                    flowSum += flowAccumulation[x, z];
                    count++;
                }
            }

            if (count == 0)
            {
                return (0.0, 0.0);
            }

            return (hydroSum / count, flowSum / count);
        }

        private void EnforceHydrologyEdgeConsistency(double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius);
            var interior = ComputeInteriorHydrologyAverages(hydrologyMask, flowAccumulation, edgeRadius);
            double flowPersistence = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(
                        Math.Min(x, z),
                        Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - Math.Clamp(edgeDistance / (double)edgeRadius, 0.0, 1.0);
                    double targetHydro = ClampEdgeVariance(hydrologyMask[x, z], interior.hydro, _hydrologyEdgeVarianceClamp);
                    double targetFlow = ClampEdgeVariance(flowAccumulation[x, z], interior.flow, _hydrologyEdgeVarianceClamp * 1.25, 0.05);
                    double blend = Math.Clamp(0.35 + falloff * (0.35 + _hydrologyFlowPersistence * 0.1), 0.0, 1.0);

                    var gradient = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                    double gradientStrength = gradient.Length();
                    if (gradientStrength > 1e-5)
                    {
                        var normalized = Vector2.Normalize(gradient);
                        int gradStep = Math.Max(1, edgeRadius - edgeDistance + 1);
                        int gradX = Math.Clamp(x + (int)Math.Round(normalized.X * gradStep), 0, width - 1);
                        int gradZ = Math.Clamp(z + (int)Math.Round(normalized.Y * gradStep), 0, depth - 1);
                        double directionalHydro = hydrologyMask[gradX, gradZ];
                        double directionalFlow = flowAccumulation[gradX, gradZ];
                        double alignment = Vector2.Dot(normalized, ComputeHydrologyGradientVector(hydrologyMask, gradX, gradZ));
                        double gradientBlend = Math.Clamp(_hydrologyEdgeFlowLockWeight * Math.Max(0.0, alignment) * (0.5 + flowPersistence * 0.4 + _hydrologyGradientWeight * 0.2), 0.0, 1.0);
                        targetHydro = targetHydro * (1.0 - gradientBlend) + directionalHydro * gradientBlend;
                        targetFlow = targetFlow * (1.0 - gradientBlend) + directionalFlow * gradientBlend;
                    }

                    hydrologyMask[x, z] = Math.Clamp(hydrologyMask[x, z] * (1.0 - blend) + targetHydro * blend, 0.0, 1.0);
                    flowAccumulation[x, z] = Math.Max(0.0, flowAccumulation[x, z] * (1.0 - blend) + targetFlow * blend);
                }
            }

            if (_hydrologyEdgeStabilityIterations > 0 && _hydrologyEdgeStabilityWeight > 0.0)
            {
                var hydroBuffer = new double[width, depth];
                var flowBuffer = new double[width, depth];
                double stabilityWeight = Math.Clamp(_hydrologyEdgeStabilityWeight, 0.0, 1.0);

                for (int iteration = 0; iteration < _hydrologyEdgeStabilityIterations; iteration++)
                {
                    var iterationInterior = ComputeInteriorHydrologyAverages(hydrologyMask, flowAccumulation, edgeRadius);

                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            int edgeDistance = Math.Min(
                                Math.Min(x, z),
                                Math.Min(width - 1 - x, depth - 1 - z));
                            if (edgeDistance > edgeRadius)
                            {
                                hydroBuffer[x, z] = hydrologyMask[x, z];
                                flowBuffer[x, z] = flowAccumulation[x, z];
                                continue;
                            }

                            double falloff = 1.0 - Math.Clamp(edgeDistance / (double)edgeRadius, 0.0, 1.0);
                            var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                            bool hasFlowDir = flowDir.LengthSquared() > 1e-5f;
                            int sampleX = x;
                            int sampleZ = z;
                            if (hasFlowDir)
                            {
                                sampleX = Math.Clamp(x + Math.Sign(flowDir.X) * 2, 0, width - 1);
                                sampleZ = Math.Clamp(z + Math.Sign(flowDir.Y) * 2, 0, depth - 1);
                            }

                            double alongFlowHydro = hydrologyMask[sampleX, sampleZ];
                            double alongFlow = flowAccumulation[sampleX, sampleZ];
                            double continuity = Math.Clamp(_hydrologyContinuityWeight + falloff * 0.2, 0.0, 1.0);
                            double targetHydro = (hydrologyMask[x, z] * 0.9 + iterationInterior.hydro * (0.75 + continuity * 0.25) + alongFlowHydro * (0.55 + flowPersistence * 0.25)) / (2.2 + continuity * 0.25 + flowPersistence * 0.25);
                            double targetFlow = (flowAccumulation[x, z] * (0.85 + flowPersistence * 0.2) + iterationInterior.flow * (0.6 + flowPersistence * 0.3) + alongFlow * (0.65 + continuity * 0.2)) / (2.1 + flowPersistence * 0.5 + continuity * 0.2);
                            double blend = Math.Clamp(stabilityWeight * falloff * (0.6 + flowPersistence * 0.35), 0.0, 1.0);

                            hydroBuffer[x, z] = Math.Clamp(hydrologyMask[x, z] * (1.0 - blend) + targetHydro * blend, 0.0, 1.0);
                            flowBuffer[x, z] = Math.Max(0.0, flowAccumulation[x, z] * (1.0 - blend * 0.55) + targetFlow * (blend * 0.55));
                        }
                    }

                    Array.Copy(hydroBuffer, hydrologyMask, hydrologyMask.Length);
                    Array.Copy(flowBuffer, flowAccumulation, flowAccumulation.Length);
                }
            }
        }

        private static double ClampEdgeVariance(double value, double anchor, double clampFraction, double absoluteFloor = 0.02)
        {
            double maxDelta = Math.Max(absoluteFloor, Math.Abs(anchor) * clampFraction);
            double delta = value - anchor;
            if (Math.Abs(delta) <= maxDelta)
            {
                return value;
            }

            double clamped = anchor + Math.Sign(delta) * maxDelta;
            return clamped;
        }

        private void StabilizeHydrologyWarping(int chunkX, int chunkZ, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (_hydrologyWarpAmplitude <= 0.0 || _hydrologyWarpFrequency <= 0.0)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int worldX = chunkX * 16;
            int worldZ = chunkZ * 16;
            double warpScale = Math.Clamp(_hydrologyWarpAmplitude / 48.0, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.02, _hydrologyFlowDivergenceClamp * 0.35);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double warpSample = SimplexNoise.Generate(
                        worldX + x + 17.25,
                        worldZ + z - 9.75,
                        _hydrologyWarpFrequency * 0.9,
                        2,
                        1.0,
                        0.6,
                        0x5EEDC0DE);

                    double gx = hydrologyMask[Math.Min(width - 1, x + 1), z] - hydrologyMask[Math.Max(0, x - 1), z];
                    double gz = hydrologyMask[x, Math.Min(depth - 1, z + 1)] - hydrologyMask[x, Math.Max(0, z - 1)];
                    double gradientMagnitude = Math.Sqrt(gx * gx + gz * gz);
                    double neighborHydro = SampleHydrologyAverage(hydrologyMask, x, z);
                    double neighborFlow = SampleHydrologyAverage(flowAccumulation, x, z);
                    double damping = Math.Clamp(
                        1.0 - Math.Abs(warpSample) * warpScale * 0.65 - gradientMagnitude * _hydrologyCurvatureWeight * 0.15,
                        0.45,
                        1.0);

                    hydrologyMask[x, z] = hydrologyMask[x, z] * damping + neighborHydro * (1.0 - damping);
                    double blendedFlow = flowAccumulation[x, z] * damping + neighborFlow * (1.0 - damping);
                    double divergence = Math.Abs(blendedFlow - neighborFlow);
                    if (divergence > divergenceClamp)
                    {
                        blendedFlow = neighborFlow + Math.Sign(blendedFlow - neighborFlow) * divergenceClamp;
                    }

                    flowAccumulation[x, z] = Math.Max(0.0, blendedFlow);
                }
            }
        }

        private static double SampleHydrologyAverage(double[,] field, int x, int z)
        {
            int width = field.GetLength(0);
            int depth = field.GetLength(1);
            double sum = 0.0;
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                    {
                        continue;
                    }

                    sum += field[nx, nz];
                    count++;
                }
            }

            return count > 0 ? sum / count : field[x, z];
        }

        private void BlendHydrologySeams(int chunkX, int chunkZ, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius);
            int sampleRadius = Math.Max(1, edgeRadius);
            double flowPersistence = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
            var interiorAverages = ComputeInteriorHydrologyAverages(hydrologyMask, flowAccumulation, edgeRadius);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                    bool hasFlowDir = flowDir.LengthSquared() > 1e-5f;

                    int edgeDistance = Math.Min(
                        Math.Min(x, z),
                        Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - Math.Clamp(edgeDistance / (double)edgeRadius, 0.0, 1.0);
                    double continuity = Math.Clamp(_hydrologyContinuityWeight + falloff * 0.25, 0.0, 1.0);
                    double blendWeight = Math.Clamp(continuity * falloff, 0.0, 1.0);
                    double ringBlend = Math.Clamp(blendWeight * (0.85 + falloff * 0.25), 0.0, 1.0);
                    double neighborHydrologySum = 0.0;
                    double neighborFlowSum = 0.0;
                    double neighborWeightTotal = 0.0;

                    for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
                    {
                        for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int neighborX = Math.Clamp(x + dx, 0, width - 1);
                            int neighborZ = Math.Clamp(z + dz, 0, depth - 1);
                            double radialDistance = Math.Sqrt(dx * (double)dx + dz * (double)dz);
                            double ringFalloff = 1.0 - Math.Clamp((radialDistance - 1.0) / Math.Max(1.0, sampleRadius - 0.75), 0.0, 1.0);
                            double manhattan = Math.Abs(dx) + Math.Abs(dz);
                            double neighborWeight = Math.Max(0.0, (1.15 - manhattan * 0.18) * ringFalloff);
                            double continuityBias = Math.Clamp(0.82 + continuity * 0.4, 0.0, 1.25);
                            neighborWeight *= continuityBias * ringFalloff;
                            if (neighborWeight <= 0.0)
                            {
                                continue;
                            }

                            if (hasFlowDir)
                            {
                                var neighborDir = new Vector2(dx, dz);
                                neighborDir = neighborDir.LengthSquared() > 1e-5f ? Vector2.Normalize(neighborDir) : neighborDir;
                                double alignment = Math.Max(0.0, Vector2.Dot(flowDir, neighborDir));
                                double flowWeight = 1.0 + _hydrologyEdgeFlowBias * alignment;
                                neighborWeight *= flowWeight;
                            }

                            double flowBias = 0.88 + flowPersistence * 0.35;
                            neighborHydrologySum += hydrologyMask[neighborX, neighborZ] * neighborWeight;
                            neighborFlowSum += flowAccumulation[neighborX, neighborZ] * neighborWeight * flowBias;
                            neighborWeightTotal += neighborWeight;
                        }
                    }

                    double neighborHydrology = neighborWeightTotal > 0.0
                        ? neighborHydrologySum / neighborWeightTotal
                        : hydrologyMask[x, z];
                    double neighborFlow = neighborWeightTotal > 0.0
                        ? neighborFlowSum / neighborWeightTotal
                        : flowAccumulation[x, z];
                    double anchorNoise = SimplexNoise.Generate(
                        chunkX * 16 + x + 19.5,
                        chunkZ * 16 + z - 11.5,
                        0.0028,
                        3,
                        1.0,
                        0.55,
                        91013);
                    double anchorHydrology = Math.Clamp(0.55 + anchorNoise * 0.45 + falloff * 0.05, 0.0, 1.0);
                    double baseHydrology = (hydrologyMask[x, z] * (2.2 + falloff * 0.4) + neighborHydrology * (1.6 + falloff * 0.5) + anchorHydrology * (0.45 + falloff * 0.15)) / (4.25 + falloff * 1.05);
                    double blendedHydrology = hydrologyMask[x, z] * (1.0 - ringBlend) + baseHydrology * ringBlend;
                    blendedHydrology = ClampEdgeVariance(blendedHydrology, interiorAverages.hydro, _hydrologyEdgeVarianceClamp);
                    hydrologyMask[x, z] = Math.Clamp(blendedHydrology, 0.0, 1.0);

                    double anchorFlow = Math.Clamp(anchorHydrology * 0.9 + Math.Abs(anchorNoise) * 0.65, 0.0, 8.0);
                    double baseFlow = (flowAccumulation[x, z] * (1.1 + 0.45 * flowPersistence) + neighborFlow * (0.85 + 0.15 * flowPersistence) + anchorFlow * (0.45 + falloff * 0.1)) / (2.4 + 0.35 * flowPersistence + falloff * 0.1);
                    double blendedFlow = flowAccumulation[x, z] * (1.0 - ringBlend) + baseFlow * ringBlend;
                    blendedFlow = ClampEdgeVariance(blendedFlow, interiorAverages.flow, _hydrologyEdgeVarianceClamp * 1.25, 0.05);
                    flowAccumulation[x, z] = Math.Clamp(blendedFlow, 0.0, 8.0);
                }
            }
        }

        private void NormalizeHydrologyPressure(double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            double min = double.MaxValue;
            double max = double.MinValue;
            double sum = 0.0;
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double value = hydrologyMask[x, z];
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                    sum += value;
                    count++;
                }
            }

            if (count == 0 || max <= min + double.Epsilon)
            {
                return;
            }

            double avg = sum / count;
            double invRange = 1.0 / Math.Max(1e-5, max - min);
            double avgNorm = Math.Clamp((avg - min) * invRange, 0.0, 1.0);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double normalized = Math.Clamp((hydrologyMask[x, z] - min) * invRange, 0.0, 1.0);
                    double flowNorm = Math.Clamp(flowAccumulation[x, z] / (6.0 + 6.0 * _hydrologyFlowPersistence), 0.0, 1.0);
                    int edgeDistance = Math.Min(
                        Math.Min(x, z),
                        Math.Min(width - 1 - x, depth - 1 - z));
                    double edgeBlend = 1.0 - Math.Clamp(edgeDistance / (double)edgeRadius, 0.0, 1.0);
                    double continuity = Math.Clamp(_hydrologyContinuityWeight + edgeBlend * 0.2, 0.0, 1.0);
                    double baseline = normalized * (1.0 - continuity) + avgNorm * continuity;
                    double flowBias = flowNorm * (0.35 + 0.3 * edgeBlend) * _hydrologyFlowPersistence;
                    hydrologyMask[x, z] = Math.Clamp(baseline + flowBias, 0.0, 1.0);
                }
            }
        }

        private void ClampHydrologyToWaterTable(int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (_hydrologyWaterTableClampWeight <= 0.0 || _hydrologyWaterTableClampRange <= 0 || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            double weight = Math.Clamp(_hydrologyWaterTableClampWeight, 0.0, 1.0);
            double invRange = 1.0 / Math.Max(1, _hydrologyWaterTableClampRange);
            double flowBlendScale = 0.65;
            double slopeWeight = Math.Clamp(_hydrologyWaterTableSlopeWeight, 0.0, 1.0);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    double delta = Math.Abs(GlobalWaterLevel - surface);
                    double proximity = 1.0 - Math.Clamp(delta * invRange, 0.0, 1.0);
                    if (proximity <= 0.0)
                    {
                        continue;
                    }

                    double slopeFactor = ComputeWaterTableSlopeFactor(surfaceCache, x, z);
                    double slopeAttenuation = Math.Clamp(1.0 - slopeFactor * slopeWeight, 0.25, 1.0);
                    double blend = weight * proximity * slopeAttenuation;
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    double valleyBias = Math.Clamp((GlobalWaterLevel - surface) / Math.Max(1.0, _hydrologyShorePush * 1.15), -1.0, 1.0);
                    double hydroBoost = Math.Max(0.05, 0.25 - slopeFactor * slopeWeight * 0.12);
                    double targetHydro = Math.Clamp(
                        hydrologyMask[x, z]
                        + hydroBoost * proximity
                        + Math.Max(0.0, valleyBias) * (0.18 * slopeAttenuation),
                        0.0,
                        1.0);

                    double flowBoost = Math.Max(0.05, 0.35 - slopeFactor * slopeWeight * 0.2);
                    double targetFlow = Math.Clamp(flowAccumulation[x, z] + flowBoost * proximity, 0.0, 1.0);
                    double flowBlend = flowBlendScale * (0.55 + slopeAttenuation * 0.45);

                    hydrologyMask[x, z] = Math.Clamp(hydrologyMask[x, z] * (1.0 - blend) + targetHydro * blend, 0.0, 1.0);
                    flowAccumulation[x, z] = Math.Max(0.0, flowAccumulation[x, z] * (1.0 - blend * flowBlend) + targetFlow * (blend * flowBlend));
                }
            }
        }

        private double ComputeWaterTableSlopeFactor(int[,] surfaceCache, int x, int z)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);

            int left = surfaceCache[Math.Max(x - 1, 0), z];
            int right = surfaceCache[Math.Min(x + 1, width - 1), z];
            int back = surfaceCache[x, Math.Max(z - 1, 0)];
            int forward = surfaceCache[x, Math.Min(z + 1, depth - 1)];

            double gradientX = Math.Abs(right - left) * 0.5;
            double gradientZ = Math.Abs(forward - back) * 0.5;
            double slope = Math.Sqrt(gradientX * gradientX + gradientZ * gradientZ);

            return Math.Clamp(slope / Math.Max(1.0, _hydrologyShorePush * 0.9), 0.0, 1.0);
        }

        private void RelaxHydrologySeams(double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (_hydrologySeamRelaxIterations <= 0 || _hydrologySeamRelaxBlend <= 0.0 || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double flowPersistence = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
            double baseBlend = Math.Clamp(_hydrologySeamRelaxBlend, 0.0, 1.0);
            double stabilityWeightBase = Math.Clamp(_hydrologyEdgeStabilityWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(_hydrologyEdgeVarianceClamp, 0.0, 1.0);
            var hydroBuffer = new double[width, depth];
            var flowBuffer = new double[width, depth];

            for (int iteration = 0; iteration < _hydrologySeamRelaxIterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        var flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                        bool hasFlowDir = flowDir.LengthSquared() > 1e-5f;

                        int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            hydroBuffer[x, z] = hydrologyMask[x, z];
                            flowBuffer[x, z] = flowAccumulation[x, z];
                            continue;
                        }

                        double falloff = 1.0 - Math.Clamp(edgeDistance / (double)edgeRadius, 0.0, 1.0);
                        double blend = baseBlend * falloff;
                        double weightedHydro = hydrologyMask[x, z] * 1.25;
                        double weightedFlow = flowAccumulation[x, z] * (0.85 + flowPersistence * 0.25);
                        double weight = 1.25;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                double distance = Math.Abs(dx) + Math.Abs(dz);
                                double neighborWeight = 1.0 - distance * 0.15;
                                double continuity = Math.Clamp(_hydrologyContinuityWeight + falloff * 0.2, 0.0, 1.2);
                                neighborWeight *= 0.85 + continuity * 0.35;

                                if (hasFlowDir)
                                {
                                    var neighborDir = new Vector2(dx, dz);
                                    neighborDir = neighborDir.LengthSquared() > 1e-5f ? Vector2.Normalize(neighborDir) : neighborDir;
                                    double alignment = Math.Max(0.0, Vector2.Dot(flowDir, neighborDir));
                                    double flowWeight = 1.0 + _hydrologyEdgeFlowBias * alignment;
                                    neighborWeight *= flowWeight;
                                }

                                weightedHydro += hydrologyMask[nx, nz] * neighborWeight;
                                weightedFlow += flowAccumulation[nx, nz] * neighborWeight * (0.8 + flowPersistence * 0.35);
                                weight += neighborWeight;
                            }
                        }

                        double averagedHydro = weight > 0.0 ? weightedHydro / weight : hydrologyMask[x, z];
                        double averagedFlow = weight > 0.0 ? weightedFlow / weight : flowAccumulation[x, z];
                        double stabilityBlend = Math.Clamp(stabilityWeightBase * falloff, 0.0, 1.0);
                        double hydroBlend = Math.Clamp(blend * (0.75 + flowPersistence * 0.25) + stabilityBlend * 0.35, 0.0, 1.0);
                        double flowBlend = Math.Clamp(blend * (0.6 + flowPersistence * 0.35) + stabilityBlend * 0.25, 0.0, 1.0);

                        double targetHydro = hydrologyMask[x, z] * (1.0 - hydroBlend) + averagedHydro * hydroBlend;
                        double targetFlow = flowAccumulation[x, z] * (1.0 - flowBlend) + averagedFlow * flowBlend;
                        double hydroDeltaClamp = Math.Clamp(targetHydro - hydrologyMask[x, z], -varianceClamp, varianceClamp);
                        double flowDeltaClamp = Math.Clamp(targetFlow - flowAccumulation[x, z], -varianceClamp * 1.5, varianceClamp * 1.5);

                        hydroBuffer[x, z] = Math.Clamp(hydrologyMask[x, z] + hydroDeltaClamp, 0.0, 1.0);
                        flowBuffer[x, z] = Math.Max(0.0, flowAccumulation[x, z] + flowDeltaClamp);
                    }
                }

                Array.Copy(hydroBuffer, hydrologyMask, hydrologyMask.Length);
                Array.Copy(flowBuffer, flowAccumulation, flowAccumulation.Length);
            }
        }

        private void AnchorHydrologySeamsToSlope(int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            if (surfaceCache == null || hydrologyMask == null || flowAccumulation == null)
            {
                return;
            }

            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int edgeRadius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double flowPersistence = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    if (slopeDir.LengthSquared() <= 1e-5f)
                    {
                        continue;
                    }

                    int step = Math.Max(1, edgeRadius - edgeDistance + 1);
                    int anchorX = Math.Clamp(x + (int)Math.Round(slopeDir.X * step), 1, width - 2);
                    int anchorZ = Math.Clamp(z + (int)Math.Round(slopeDir.Y * step), 1, depth - 2);
                    double heightDelta = Math.Abs(surfaceCache[x, z] - surfaceCache[anchorX, anchorZ]);
                    double slopeStrength = Math.Clamp(heightDelta / 12.0, 0.0, 1.0);
                    double edgeWeight = 1.0 - edgeDistance / (double)Math.Max(1, edgeRadius);
                    double blend = Math.Clamp(edgeWeight * (0.25 + slopeStrength * 0.35) * (0.7 + flowPersistence * 0.25), 0.0, 0.68);

                    double anchorHydro = hydrologyMask[anchorX, anchorZ];
                    double anchorFlow = flowAccumulation[anchorX, anchorZ];
                    double blendedHydro = hydrologyMask[x, z] * (1.0 - blend) + anchorHydro * blend;
                    double blendedFlow = flowAccumulation[x, z] * (1.0 - blend) + anchorFlow * blend;

                    Vector2 flowDir = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                    if (flowDir.LengthSquared() > 1e-5f && _hydrologyEdgeFlowLockWeight > 0.0)
                    {
                        flowDir = Vector2.Normalize(flowDir);
                        int flowStep = Math.Max(1, edgeRadius - edgeDistance + 1);
                        int flowX = Math.Clamp(x + (int)Math.Round(flowDir.X * flowStep), 1, width - 2);
                        int flowZ = Math.Clamp(z + (int)Math.Round(flowDir.Y * flowStep), 1, depth - 2);
                        double flowHydro = hydrologyMask[flowX, flowZ];
                        double flowFlow = flowAccumulation[flowX, flowZ];
                        double alignment = Vector2.Dot(flowDir, ComputeHydrologyGradientVector(hydrologyMask, flowX, flowZ));
                        double alignmentWeight = Math.Clamp(0.6 + Math.Max(0.0, alignment) * 0.4, 0.0, 1.2);
                        double flowBlend = Math.Clamp(edgeWeight * _hydrologyEdgeFlowLockWeight * alignmentWeight * (0.55 + flowPersistence * 0.35), 0.0, 1.0);
                        double targetHydro = (blendedHydro + flowHydro) * 0.5;
                        double targetFlow = (blendedFlow + flowFlow) * 0.5;
                        blendedHydro = blendedHydro * (1.0 - flowBlend) + targetHydro * flowBlend;
                        blendedFlow = blendedFlow * (1.0 - flowBlend) + targetFlow * flowBlend;
                    }

                    Vector2 tangent = ComputeHydrologyTangent(hydrologyMask, x, z);
                    if (tangent.LengthSquared() > 1e-5f && _hydrologyEdgeTangentWeight > 0.0)
                    {
                        tangent = Vector2.Normalize(tangent);
                        int tangentStepX = tangent.X >= 0 ? 1 : -1;
                        int tangentStepZ = tangent.Y >= 0 ? 1 : -1;
                        int tangentX = Math.Clamp(x + tangentStepX, 1, width - 2);
                        int tangentZ = Math.Clamp(z + tangentStepZ, 1, depth - 2);
                        double tangentHydro = hydrologyMask[tangentX, tangentZ];
                        double tangentFlow = flowAccumulation[tangentX, tangentZ];
                        double tangentBlend = Math.Clamp(edgeWeight * _hydrologyEdgeTangentWeight * (0.55 + slopeStrength * 0.35), 0.0, 1.0);
                        double targetHydro = (blendedHydro + tangentHydro) * 0.5;
                        double targetFlow = (blendedFlow + tangentFlow) * 0.5;
                        blendedHydro = blendedHydro * (1.0 - tangentBlend) + targetHydro * tangentBlend;
                        blendedFlow = blendedFlow * (1.0 - tangentBlend) + targetFlow * tangentBlend;
                    }

                    hydrologyMask[x, z] = Math.Clamp(blendedHydro, 0.0, 1.0);
                    flowAccumulation[x, z] = Math.Clamp(blendedFlow, 0.0, 8.0);
                }
            }
        }

        private void FeatherHydrologyEdges(double[,] hydrologyMask, double[,] flowAccumulation)
        {
            int width = hydrologyMask.GetLength(0);
            int depth = hydrologyMask.GetLength(1);
            int radius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double clampWeight = Math.Clamp(_hydrologyEdgeVarianceClamp, 0.0, 1.0);
            double stabilityWeight = Math.Clamp(_hydrologyEdgeStabilityWeight, 0.0, 1.0);

            if (clampWeight <= 0.0)
            {
                return;
            }

            var originalHydro = (double[,])hydrologyMask.Clone();
            var originalFlow = (double[,])flowAccumulation.Clone();

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    double sumHydro = 0.0;
                    double sumFlow = 0.0;
                    int samples = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int sampleX = x + dx;
                            int sampleZ = z + dz;
                            if (sampleX < 0 || sampleX >= width || sampleZ < 0 || sampleZ >= depth)
                            {
                                continue;
                            }

                            sumHydro += originalHydro[sampleX, sampleZ];
                            sumFlow += originalFlow[sampleX, sampleZ];
                            samples++;
                        }
                    }

                    if (samples == 0)
                    {
                        continue;
                    }

                    double averageHydro = sumHydro / samples;
                    double averageFlow = sumFlow / samples;
                    double edgeBlend = clampWeight * (1.0 - edgeDistance / (double)radius);
                    edgeBlend = Math.Clamp(edgeBlend * (0.65 + stabilityWeight * 0.35), 0.0, 1.0);

                    hydrologyMask[x, z] = Math.Clamp(originalHydro[x, z] * (1.0 - edgeBlend) + averageHydro * edgeBlend, 0.0, 1.0);
                    flowAccumulation[x, z] = Math.Max(0.0, originalFlow[x, z] * (1.0 - edgeBlend) + averageFlow * edgeBlend);
                }
            }
        }

        private double[,] BuildRiparianSaturationMap(int[,] surfaceCache, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);
            var riparian = new double[width, depth];

            int maxHeight = 1;
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    maxHeight = Math.Max(maxHeight, surfaceCache[x, z]);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        riparian[x, z] = 0.0;
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double accumulation = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double relief = ComputeLocalRelief(surfaceCache, x, z, 2);
                    double slopePenalty = Math.Clamp(relief / 10.0, 0.0, 1.0);
                    double altitude = Math.Clamp(surface / (double)maxHeight, 0.0, 1.0);
                    double valleyBias = Math.Clamp((GlobalWaterLevel - surface) / 14.0, 0.0, 1.0);
                    double lowlandBias = 1.0 - altitude;
                    double moisture = hydrology * 0.55 + accumulation * 0.3 + lowlandBias * 0.1 + valleyBias * 0.25;
                    double erosionResilience = Math.Clamp(1.0 - slopePenalty * 0.5, 0.0, 1.0);
                    riparian[x, z] = Math.Clamp(moisture * erosionResilience, 0.0, 1.0);
                }
            }

            return riparian;
        }

        private static double AdjustRiverIntensity(double baseIntensity, double hydrologyBias)
        {
            double clamped = Math.Clamp(hydrologyBias, 0.0, 1.0);
            double scaled = baseIntensity * (1.0 - clamped * 0.55) - clamped * 0.004;
            return Math.Max(0.0, scaled);
        }

        private static double ComputeChannelPressure(double catchmentStrength, double hydrology)
        {
            double baseValue = 0.35 + catchmentStrength * 0.5 + hydrology * 0.3;
            return Math.Clamp(baseValue, 0.0, 1.0);
        }

        private void SmoothRiverIntensity(double[,] riverIntensity, double[,] erosionRiskField, double[,] hydrologyMask, double[,] flowAccumulation)
        {
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);
            int iterations = Math.Clamp(_riverIntensitySmoothIterations, 1, 8);
            double baseBlend = Math.Clamp(_riverIntensitySmoothBlend, 0.0, 1.0);
            var scratch = new double[width, depth];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        double hydrology = hydrologyMask[x, z];
                        double flow = flowAccumulation[x, z];
                        double headwater = Math.Clamp(1.0 - Math.Clamp(flow * 0.5, 0.0, 1.0), 0.0, 1.0);
                        var gradient = ComputeHydrologyGradientVector(hydrologyMask, x, z);
                        Vector2 flowDir = gradient.LengthSquared() > 1e-5f ? Vector2.Normalize(-gradient) : Vector2.Zero;
                        double weightedSum = riverIntensity[x, z];
                        double weightTotal = 1.0;
                        double maxAlignment = 0.0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0)
                                {
                                    continue;
                                }

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                                {
                                    continue;
                                }

                                double neighborHydrology = hydrologyMask[nx, nz];
                                double baseWeight = 1.0 + erosionRiskField[nx, nz] * 0.75 + hydrology * 0.35 + neighborHydrology * 0.25;
                                double neighborFlow = flowAccumulation[nx, nz];
                                var neighborDir = new Vector2(dx, dz);
                                if (neighborDir.LengthSquared() > 1e-4f)
                                {
                                    neighborDir = Vector2.Normalize(neighborDir);
                                }

                                double alignment = flowDir == Vector2.Zero ? 0.0 : Math.Max(0.0, Vector2.Dot(flowDir, neighborDir));
                                maxAlignment = Math.Max(maxAlignment, alignment);

                                double flowWeight = 1.0 + _riverFlowAlignmentWeight * (Math.Min(flow + neighborFlow, 2.5) * 0.45 + alignment * 1.1);
                                double hydrologyDelta = Math.Abs(hydrology - neighborHydrology);
                                double gradientWeight = Math.Clamp(1.0 - _riverGradientPenalty * hydrologyDelta, 0.15, 1.0);
                                double stabilityWeight = 1.0 + _riverHeadwaterStabilityWeight * headwater * (1.0 - hydrology * 0.5);
                                double perpendicular = flowDir == Vector2.Zero ? 0.0 : Math.Abs(Vector2.Dot(new Vector2(-flowDir.Y, flowDir.X), neighborDir));
                                double anisotropy = 1.0 + _riverAnisotropyWeight * (alignment - perpendicular * 0.65);
                                anisotropy = Math.Clamp(anisotropy, 0.35, 1.75);
                                double finalWeight = Math.Clamp(baseWeight * flowWeight * gradientWeight * stabilityWeight * anisotropy, 0.35, 3.5);
                                weightedSum += riverIntensity[nx, nz] * finalWeight;
                                weightTotal += finalWeight;
                            }
                        }

                        double average = weightTotal > 0.0 ? weightedSum / weightTotal : riverIntensity[x, z];
                        double blend = Math.Clamp(baseBlend + hydrology * 0.2 + flow * 0.12 + maxAlignment * 0.2 + headwater * _riverHeadwaterStabilityWeight * 0.35, 0.0, 0.95);
                        scratch[x, z] = riverIntensity[x, z] * (1.0 - blend) + average * blend;
                    }
                }

                Array.Copy(scratch, riverIntensity, riverIntensity.Length);
            }

            FeatherRiverIntensityEdges(riverIntensity);
        }

        private void FeatherRiverIntensityEdges(double[,] riverIntensity)
        {
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);
            int radius = Math.Max(1, _hydrologyEdgeBlendRadius);
            double clampWeight = Math.Clamp(_hydrologyEdgeVarianceClamp, 0.0, 1.0);
            double stabilityWeight = Math.Clamp(_hydrologyEdgeStabilityWeight, 0.0, 1.0);
            if (clampWeight <= 0.0)
            {
                return;
            }

            var original = (double[,])riverIntensity.Clone();
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, depth - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    double sum = 0.0;
                    int samples = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int sampleX = x + dx;
                            int sampleZ = z + dz;
                            if (sampleX < 0 || sampleX >= width || sampleZ < 0 || sampleZ >= depth)
                            {
                                continue;
                            }

                            sum += original[sampleX, sampleZ];
                            samples++;
                        }
                    }

                    if (samples == 0)
                    {
                        continue;
                    }

                    double average = sum / samples;
                    double blend = clampWeight * (1.0 - edgeDistance / (double)radius);
                    blend = Math.Clamp(blend * (0.65 + stabilityWeight * 0.35), 0.0, 1.0);
                    riverIntensity[x, z] = Math.Max(0.0, original[x, z] * (1.0 - blend) + average * blend);
                }
            }
        }

        public void GenerateRiversInternal(TerrainGenerationContext context)

        {

            if (!_enableRivers)
            {
                return;
            }

            var chunk = context.Chunk;

            var riverField = GetRiverFieldCache(context);

            TerrainProfile[,]? profiles = null;

            context.TryGetMetadata(TerrainProfilesKey, out profiles);



            var surfaceCache = BuildSurfaceCache(chunk);

            var hydrologyField = GetHydrologyField(context, surfaceCache);
            var hydrologyMask = hydrologyField.HydrologyMask;
            var flowAccumulation = hydrologyField.FlowAccumulation;
            var hydrologyGradient = hydrologyField.HydrologyGradient;
            var riparianSaturation = BuildRiparianSaturationMap(surfaceCache, hydrologyMask, flowAccumulation);
            var erosionRiskField = hydrologyField.ErosionRisk;

            var riverIntensity = new double[16, 16];

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (profiles != null && profiles[x, z].Biome == BiomeType.Ocean)
                    {
                        continue;
                    }

                    if (IsOceanColumn(chunk, x, z))
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double riparian = riparianSaturation[x, z];
                    double catchment = flowAccumulation[x, z];
                    double catchmentStrength = Math.Clamp(catchment / 6.0, 0.0, 1.0);
                    Vector2 gradient = hydrologyGradient[x, z];
                    double gradientStrength = Math.Clamp(gradient.Length(), 0.0, 1.75);
                    double gradientBias = gradientStrength * Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5);

                    double erosionRisk = erosionRiskField[x, z];

                    double channelPressure = ComputeChannelPressure(catchmentStrength, hydrology);

                    channelPressure = Math.Clamp(channelPressure + riparian * 0.2 + erosionRisk * _riverBankErosionWeight + gradientBias * 0.2, 0.0, 1.25);
                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrology) - catchmentStrength * 0.015 - riparian * 0.0125 - erosionRisk * 0.01;
                    intensity = Math.Max(0.0, intensity);
                    intensity *= 1.0 + gradientBias * 0.1;
                    if (_riverReliefPenaltyWeight > 0.0)
                    {
                        double reliefPenalty = Math.Clamp(ComputeLocalRelief(surfaceCache, x, z, 2) / 8.0, 0.0, 1.0);
                        intensity *= 1.0 - reliefPenalty * _riverReliefPenaltyWeight;
                        intensity = Math.Max(0.0, intensity);
                    }

                    riverIntensity[x, z] = intensity;
                    if (intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    Vector2 flowDir = riverField.Flow[x, z];
                    Vector2 slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    if (slopeDir.LengthSquared() > 1e-4f)
                    {
                        Vector2 blended = Vector2.Lerp(flowDir, slopeDir, 0.65f);
                        flowDir = blended.LengthSquared() > 1e-5f ? Vector2.Normalize(blended) : slopeDir;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int riverSurface = Math.Min(surface, GlobalWaterLevel);

                    if (intensity < RiverCenterThreshold)
                    {
                        double normalized = 1.0 - Math.Clamp(intensity / RiverCenterThreshold, 0.0, 1.0);
                        double enrichedPressure = Math.Clamp(channelPressure + riparian * 0.2 + hydrology * 0.05, 0.0, 1.0);
                        CarveRiverColumn(chunk, surfaceCache, x, z, riverSurface, normalized, enrichedPressure, flowDir);
                    }
                    else
                    {
                        double bankStrength = 1.0 - Math.Clamp((intensity - RiverCenterThreshold) / (RiverBankThreshold - RiverCenterThreshold), 0.0, 1.0);
                        bankStrength *= 0.85 + channelPressure * 0.35 + riparian * 0.35 + erosionRisk * _riverBankErosionWeight;
                        bankStrength = Math.Clamp(bankStrength, 0.0, 1.25);
                        FeatherRiverBank(chunk, surfaceCache, x, z, bankStrength, riverSurface, flowDir);
                    }
                }
            }

            NormalizeRiverIntensity(riverIntensity, hydrologyMask, flowAccumulation, hydrologyGradient);
            ApplyRiverWidthModulation(riverIntensity, flowAccumulation, hydrologyMask, hydrologyGradient);
            SmoothRiverIntensity(riverIntensity, erosionRiskField, hydrologyMask, flowAccumulation);

            StitchTributaryChannels(context, chunk, surfaceCache, hydrologyMask, flowAccumulation, riverField, riverIntensity);
            ApplyRiverbankErosion(chunk, surfaceCache, riverField, hydrologyMask);
            ApplyRiverSedimentPass(context, chunk, surfaceCache, riverField, hydrologyMask);
            ApplyRiverPointBarSediment(context, chunk, surfaceCache, riverField, riverIntensity);
            AddFloodplainWetlands(context, chunk, surfaceCache, hydrologyMask, riverIntensity);
            AddFloodplainSwales(chunk, surfaceCache, hydrologyMask, riverIntensity);
            ApplyRiparianBankStabilization(chunk, surfaceCache, hydrologyMask, riparianSaturation, riverIntensity);
            AddRiverDeltaFans(chunk, surfaceCache, riverField, hydrologyMask, flowAccumulation, riverIntensity);
            ApplyRiverGradientSmoothing(chunk, surfaceCache, hydrologyMask, riverIntensity);
            ApplyRiverMeanderTerraces(chunk, surfaceCache, riverIntensity, hydrologyMask, riverField);
            ApplyRiverHydrologyFeedback(chunk, surfaceCache, riverField, hydrologyMask, flowAccumulation, riverIntensity);
            AddRiverSeepageChannels(chunk, surfaceCache, hydrologyMask, flowAccumulation, riverIntensity, riverField);
            SmoothRiverMouths(context, chunk, surfaceCache, hydrologyMask, riverIntensity);
        }

        private void NormalizeRiverIntensity(
            double[,] riverIntensity,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            Vector2[,] hydrologyGradient)
        {
            double clamp = RiverBankThreshold * 1.35;
            double continuityWeight = Math.Clamp(_hydrologyContinuityWeight, 0.0, 1.0);
            double persistenceWeight = Math.Clamp(_hydrologyFlowPersistence, 0.0, 1.0);
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= 0.0)
                    {
                        riverIntensity[x, z] = 0.0;
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double gradientStrength = Math.Clamp(hydrologyGradient[x, z].Length(), 0.0, 1.75);
                    double gradientBoost = 1.0 + gradientStrength * Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5) * 0.35;
                    double hydrologyTerm = hydrology * (0.55 + continuityWeight * 0.6);
                    double flowTerm = flow * (0.65 + persistenceWeight * 0.6);
                    double weight = Math.Clamp(0.35 + hydrologyTerm + flowTerm, 0.35, 1.75);
                    double stabilized = intensity * weight * gradientBoost;

                    if (stabilized < RiverCenterThreshold * 0.08)
                    {
                        stabilized = 0.0;
                    }

                    riverIntensity[x, z] = Math.Clamp(stabilized, 0.0, clamp);
                }
            }
        }

        private void ApplyRiverWidthModulation(
            double[,] riverIntensity,
            double[,] flowAccumulation,
            double[,] hydrologyMask,
            Vector2[,] hydrologyGradient)
        {
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);
            double edgeClamp = Math.Clamp(_hydrologyEdgeVarianceClamp, 0.0, 1.0);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= 0.0)
                    {
                        continue;
                    }

                    double flow = Math.Clamp(flowAccumulation[x, z] / 8.0, 0.0, 1.0);
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double gradientStrength = Math.Clamp(hydrologyGradient[x, z].Length(), 0.0, _hydrologyGradientClamp);
                    double variance = ComputeLocalVariance(flowAccumulation, x, z, 1);
                    double varianceScale = Math.Clamp(variance * 0.35, 0.0, 0.6);
                    double jitter = 1.0 + Math.Clamp(varianceScale * 0.25 + gradientStrength * 0.08, -0.3, 0.45);
                    double headwater = 1.0 - Math.Clamp(flow * 0.65, 0.0, 1.0);
                    double widthScale = 1.0 + flow * 0.25 + hydrology * 0.1 - headwater * _riverHeadwaterStabilityWeight * 0.2;
                    widthScale *= jitter;
                    widthScale = Math.Clamp(widthScale, 0.65, 1.6);

                    double edgeDistance = Math.Min(Math.Min(x, width - 1 - x), Math.Min(z, depth - 1 - z));
                    double seamBlend = Math.Clamp(1.0 - edgeDistance / 6.0, 0.0, 1.0);
                    double seamClamp = 1.0 - seamBlend * edgeClamp * 0.35;

                    double scaled = intensity * widthScale * seamClamp;
                    scaled = Math.Clamp(scaled, 0.0, RiverBankThreshold * 1.4);

                    if (intensity < RiverCenterThreshold)
                    {
                        double taper = Math.Clamp(1.0 - headwater * 0.35, 0.0, 1.0);
                        scaled *= taper;
                    }

                    riverIntensity[x, z] = scaled;
                }
            }
        }

        private void SmoothRiverMouths(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            if (_riverMouthSmoothRadius <= 0 && _riverDeltaWetlandStrength <= 0.0)
            {
                return;
            }

            int radius = Math.Max(1, _riverMouthSmoothRadius);
            double wetland = Math.Clamp(_riverDeltaWetlandStrength, 0.0, 1.0);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold * 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[x, z] = surface;
                    }

                    double shoreBias = Math.Clamp((GlobalWaterLevel + radius - surface) / Math.Max(1.0, _hydrologyShorePush), 0.0, 1.0);
                    if (shoreBias <= 0.05 && hydrologyMask[x, z] < 0.45)
                    {
                        continue;
                    }

                    double mouthStrength = Math.Clamp(intensity / RiverBankThreshold, 0.0, 1.35);
                    mouthStrength = Math.Clamp(mouthStrength * (0.55 + shoreBias * 0.65) + hydrologyMask[x, z] * 0.25, 0.0, 1.5);

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            double falloff = 1.0 - Math.Clamp(Math.Sqrt(dx * dx + dz * dz) / (radius + 0.5), 0.0, 1.0);
                            if (falloff <= 0.0)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                neighborSurface = FindSurfaceLevel(chunk, nx, nz);
                                if (neighborSurface <= 0)
                                {
                                    continue;
                                }
                                surfaceCache[nx, nz] = neighborSurface;
                            }

                            int riverSurface = Math.Min(neighborSurface, GlobalWaterLevel);
                            int carveDepth = Math.Max(1, (int)Math.Round(_riverDepth * mouthStrength * falloff * 0.6));
                            int target = Math.Max(1, riverSurface - carveDepth);

                            for (int y = riverSurface; y >= target; y--)
                            {
                                chunk.SetBlock(nx, y, nz, BlockType.Water);
                            }

                            if (wetland > 0.0 && falloff > 0.35)
                            {
                                var bankBlock = wetland > 0.5 ? BlockType.Clay : BlockType.Sand;
                                int bankY = Math.Max(target - 1, 1);
                                chunk.SetBlock(nx, bankY, nz, bankBlock);
                                if (riverSurface + 1 < 256)
                                {
                                    chunk.SetBlock(nx, riverSurface + 1, nz, BlockType.Air);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CreateCavePool(ChunkData chunk, int centerX, int centerY, int centerZ, int radius)
        {
            if (centerY <= 1 || centerY >= 255)
                return;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                        continue;

                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance > radius)
                        continue;

                    int floorY = Math.Max(1, centerY - 1);
                    chunk.SetBlock(x, floorY, z, BlockType.Sand);
                    chunk.SetBlock(x, centerY, z, BlockType.Water);

                    if (centerY + 1 < 256)
                    {
                        chunk.SetBlock(x, centerY + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private static double SampleRiverIntensity(double[,] riverIntensity, int centerX, int centerZ, int radius)
        {
            double sum = 0.0;
            int samples = 0;
            int width = riverIntensity.GetLength(0);
            int depth = riverIntensity.GetLength(1);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= width || z < 0 || z >= depth)
                    {
                        continue;
                    }

                    double value = riverIntensity[x, z];
                    if (value < 0.0)
                    {
                        continue;
                    }

                    sum += value;
                    samples++;
                }
            }

            if (samples == 0)
            {
                return 0.0;
            }

            return Math.Clamp(sum / samples, 0.0, 1.0);
        }

        private static double ComputeLocalRelief(int[,] surfaceCache, int centerX, int centerZ, int radius)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            double sum = 0.0;
            double sumSq = 0.0;
            int samples = 0;

            int width = surfaceCache.GetLength(0);
            int depth = surfaceCache.GetLength(1);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= width || z < 0 || z >= depth)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    samples++;
                    double height = surface;
                    sum += height;
                    sumSq += height * height;
                    min = Math.Min(min, height);
                    max = Math.Max(max, height);
                }
            }

            if (samples == 0)
            {
                return 0.0;
            }

            double mean = sum / samples;
            double variance = Math.Max(0.0, (sumSq / samples) - mean * mean);
            double stdDev = Math.Sqrt(variance);
            return (max - min) + stdDev;
        }

        private static double ComputeLocalVariance(double[,] field, int centerX, int centerZ, int radius = 1)
        {
            double sum = 0.0;
            double sumSq = 0.0;
            int samples = 0;
            int width = field.GetLength(0);
            int depth = field.GetLength(1);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= width || z < 0 || z >= depth)
                    {
                        continue;
                    }

                    double value = field[x, z];
                    sum += value;
                    sumSq += value * value;
                    samples++;
                }
            }

            if (samples == 0)
            {
                return 0.0;
            }

            double mean = sum / samples;
            return Math.Max(0.0, (sumSq / samples) - mean * mean);
        }

        private static double ComputeGradientVariance(Vector2[,] gradientField, int centerX, int centerZ, int radius)
        {
            double sum = 0.0;
            double sumSq = 0.0;
            int samples = 0;
            int width = gradientField.GetLength(0);
            int depth = gradientField.GetLength(1);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= width || z < 0 || z >= depth)
                    {
                        continue;
                    }

                    double magnitude = gradientField[x, z].Length();
                    sum += magnitude;
                    sumSq += magnitude * magnitude;
                    samples++;
                }
            }

            if (samples == 0)
            {
                return 0.0;
            }

            double mean = sum / samples;
            return Math.Max(0.0, (sumSq / samples) - mean * mean);
        }

        public void GenerateLakesInternal(TerrainGenerationContext context)
        {
            if (!_enableLakes)
            {
                return;
            }

            var chunk = context.Chunk;
            var riverField = GetRiverFieldCache(context);
            var surfaceCache = BuildSurfaceCache(chunk);
            var hydrologyField = GetHydrologyField(context, surfaceCache);
            var hydrologyMask = hydrologyField.HydrologyMask;
            var flowAccumulation = hydrologyField.FlowAccumulation;
            var hydrologyGradient = hydrologyField.HydrologyGradient;
            var hydrologyCurvature = hydrologyField.HydrologyCurvature;
            var riparianSaturation = BuildRiparianSaturationMap(surfaceCache, hydrologyMask, flowAccumulation);
            var erosionRiskField = hydrologyField.ErosionRisk;
            var warp = SimplexNoise.DomainWarp(context.ChunkX * 16, context.ChunkZ * 16, 0.00045, 0.0009, 14.0, 9.0, 67891);
            double lakeSimplex = SimplexNoise.Generate(context.ChunkX + warp.dx, context.ChunkZ + warp.dz, 0.035, 3, 1.0, 0.55, 67891);
            double lakePerlin = PerlinNoise.Generate(context.ChunkX + warp.dx, context.ChunkZ + warp.dz, 0.028, 2, 1.0, 0.6, 77811);
            double lakeNoise = (lakeSimplex + lakePerlin) * 0.5;
            if (lakeNoise < 0.62)
                return;

            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltLake);
            double chunkWeight = Math.Clamp((lakeNoise - 0.62) * 1.8, 0.0, 1.0);
            var lakeConfig = _worldGenConfig.Lakes;
            int maxRadiusSetting = Math.Clamp(lakeConfig.MaxRadius, 3, 12);
            int minDepthSetting = Math.Clamp(lakeConfig.MinDepth, 2, 16);
            int maxDepthSetting = Math.Clamp(lakeConfig.MaxDepth, minDepthSetting, 16);
            double shorelineBlend = Math.Clamp(lakeConfig.ShorelineBlend, 0.0, 1.0);

            int centerX = rand.Next(4, 12);
            int centerZ = rand.Next(4, 12);
            double hydrology = hydrologyMask[centerX, centerZ];
            double riparian = riparianSaturation[centerX, centerZ];
            double flow = Math.Clamp(flowAccumulation[centerX, centerZ] / 12.0, 0.0, 1.0);
            double curvature = hydrologyCurvature[centerX, centerZ];
            double curvatureBias = Math.Clamp(curvature * _hydrologyCurvatureWeight, 0.0, 1.2);
            double erosionRisk = erosionRiskField[centerX, centerZ];
            double relief = ComputeLocalRelief(surfaceCache, centerX, centerZ, 6);
            double basinStability = 1.0 - Math.Clamp(relief / 10.0, 0.0, 1.0);
            Vector2 gradient = hydrologyGradient[centerX, centerZ];
            double gradientStrength = Math.Clamp(gradient.Length(), 0.0, 1.75);
            double gradientBias = gradientStrength * Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5);
            double gradientVariance = ComputeGradientVariance(hydrologyGradient, centerX, centerZ, 3);
            double gradientVarianceWeight = Math.Clamp(gradientVariance, 0.0, 1.25);
            Vector2 inflowDir = gradient;
            double inflowBlend = Math.Clamp(_lakeInflowBlendWeight + gradientVarianceWeight * 0.25, 0.0, 1.0);
            double hydrologyCoherence = Math.Clamp((hydrology + riparian) * 0.5 + flow * 0.35, 0.0, 1.0);
            double erosionPenalty = Math.Clamp(1.0 - erosionRisk * 0.65, 0.25, 1.0);
            double spawnWeight = Math.Clamp((chunkWeight * 0.6 + hydrology * 0.8) * (0.65 + basinStability * 0.5), 0.0, 1.2);
            spawnWeight = Math.Clamp(spawnWeight + riparian * 0.25 + flow * 0.2 + hydrologyCoherence * 0.2 + gradientBias * 0.12 + gradientVarianceWeight * 0.08 + curvatureBias * 0.2 + lakeConfig.SpawnWeightBias, 0.0, 1.35);
            double riverPressure = SampleRiverIntensity(riverField.Intensity, centerX, centerZ, 2);
            if (_lakeRiverProximitySuppression > 0.0 && riverPressure > 0.0)
            {
                double suppression = Math.Clamp(riverPressure * _lakeRiverProximitySuppression, 0.0, 0.85);
                spawnWeight *= 1.0 - suppression;
            }
            spawnWeight *= erosionPenalty;
            spawnWeight *= Math.Clamp(0.85 + hydrologyCoherence * 0.35 + gradientBias * 0.2, 0.7, 1.5);
            if (spawnWeight < Math.Max(0.2, lakeConfig.SpawnWeightBias) || rand.NextDouble() > spawnWeight || basinStability < 0.3)
                return;

            int radiusX = 3 + rand.Next(4) + (int)Math.Round(hydrology * 2.0) + (int)Math.Round(riparian * 2.0) + (int)Math.Round(flow * 1.5);
            int radiusZ = 3 + rand.Next(4) + (int)Math.Round(hydrology * 2.0) + (int)Math.Round(riparian * 1.5) + (int)Math.Round(flow * 1.25);
            double anisotropy = Math.Clamp(hydrologyCoherence * 0.45 + flow * 0.35 + gradientStrength * 0.25 + gradientVarianceWeight * 0.18, 0.0, 1.0);
            double majorScale = 1.0 + anisotropy * 0.25;
            double minorScale = 1.0 - anisotropy * 0.15;
            radiusX = (int)Math.Round(radiusX * (0.8 + erosionPenalty * 0.45) * majorScale * (1.0 + curvatureBias * 0.15));
            radiusZ = (int)Math.Round(radiusZ * (0.82 + erosionPenalty * 0.4) * minorScale * (1.0 + curvatureBias * 0.12));
            radiusX = Math.Clamp(radiusX, 3, maxRadiusSetting);
            radiusZ = Math.Clamp(radiusZ, 3, maxRadiusSetting);
            int maxDepth = 3 + rand.Next(3) + (int)Math.Round(hydrology * 2.0) + (int)Math.Round(riparian * 1.5) + (int)Math.Round(flow * 1.5);
            maxDepth = (int)Math.Round(maxDepth * (0.75 + erosionPenalty * 0.35 + curvatureBias * 0.25));
            maxDepth = Math.Clamp((int)Math.Round(Math.Clamp(maxDepth * (0.7 + basinStability * 0.6), minDepthSetting, maxDepthSetting)), minDepthSetting, maxDepthSetting);
            int waterLevel = Math.Clamp(
                GlobalWaterLevel + rand.Next(-1, 2) + (int)Math.Round((hydrology - 0.5) * 3.0) + (int)Math.Round((riparian - 0.5) * 3.0) + (int)Math.Round((0.5 - erosionRisk) * 2.0) + (int)Math.Round((flow - 0.5) * 2.0) + (int)Math.Round((curvatureBias - 0.5) * 2.0),
                Math.Max(40, GlobalWaterLevel - 18),
                Math.Min(120, GlobalWaterLevel + 8));

            int sampleSurface = FindSurfaceLevel(chunk, centerX, centerZ);
            if (sampleSurface < waterLevel - 4 || sampleSurface > waterLevel + 8)
                return;

            double rotationNoise = SimplexNoise.Generate(context.ChunkX * 0.37 + warp.dx, context.ChunkZ * 0.37 + warp.dz, 0.12, 2, 1.0, 0.6, 91217);
            double rotation = rotationNoise * Math.PI;
            double rotationBlend = Math.Clamp(inflowBlend + gradientVarianceWeight * 0.35, 0.0, 1.0);
            if (rotationBlend > 0.0 && inflowDir.LengthSquared() > 1e-5f)
            {
                Vector2 noiseDir = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
                Vector2 blendedDir = Vector2.Lerp(noiseDir, Vector2.Normalize(inflowDir), (float)rotationBlend);
                if (blendedDir.LengthSquared() > 1e-5f)
                {
                    rotation = Math.Atan2(blendedDir.Y, blendedDir.X);
                }
            }
            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double radiusXWithPadding = radiusX + 0.75;
            double radiusZWithPadding = radiusZ + 0.75;
            bool lakeCreated = false;

            relief = ComputeLocalRelief(surfaceCache, centerX, centerZ, Math.Max(radiusX, radiusZ) + 4);
            basinStability = 1.0 - Math.Clamp(relief / 12.0, 0.0, 1.0);

            for (int dx = -radiusX - 3; dx <= radiusX + 3; dx++)
            {
                for (int dz = -radiusZ - 3; dz <= radiusZ + 3; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                        continue;

                    double rotX = dx * cos - dz * sin;
                    double rotZ = dx * sin + dz * cos;
                    double ellipse = Math.Sqrt(
                        (rotX * rotX) / (radiusXWithPadding * radiusXWithPadding) +
                        (rotZ * rotZ) / (radiusZWithPadding * radiusZWithPadding));
                    double edgeNoise = SimplexNoise.Generate(
                        context.ChunkX * 16 + x,
                        context.ChunkZ * 16 + z,
                        0.22,
                        2,
                        1.0,
                        0.55,
                        91771);
                    double perturbation = edgeNoise * (0.08 + erosionRiskField[x, z] * 0.06 + gradientVarianceWeight * 0.1);
                    double sdf = ellipse - 1.0 - perturbation;

                    if (sdf <= 0.18)
                    {
                        int surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 0)
                            continue;

                        double bowl = Math.Clamp(1.0 - ellipse, 0.0, 1.0);
                        double depthNoise = SimplexNoise.Generate(
                            context.ChunkX * 16 + x,
                            context.ChunkZ * 16 + z,
                            0.18,
                            2,
                            1.0,
                            0.45,
                            82319);
                        double depthFactor = Math.Clamp(bowl + depthNoise * 0.2, 0.0, 1.0);
                        int columnDepth = Math.Clamp(maxDepth + (int)Math.Round(depthFactor * maxDepth * 0.7), 2, maxDepth + 3);
                        int waterFloor = Math.Max(1, waterLevel - columnDepth);

                        for (int y = surface; y >= waterFloor; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        if (waterFloor - 1 >= 0)
                        {
                            chunk.SetBlock(x, waterFloor - 1, z, BlockType.Sand);
                        }

                        for (int y = waterFloor; y <= waterLevel && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        if (waterLevel < GlobalWaterLevel)
                        {
                            for (int y = waterLevel + 1; y <= GlobalWaterLevel && y < 256; y++)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }

                        lakeCreated = true;

                        int topWater = waterLevel < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(waterLevel, 255);
                        if (topWater + 1 < 256)
                        {
                            chunk.SetBlock(x, topWater + 1, z, BlockType.Air);
                        }
                    }
                    else if (sdf <= 0.45)
                    {
                        double rimStrength = Math.Clamp((0.45 - sdf) / 0.45, 0.0, 1.0);
                        rimStrength *= 1.0 + erosionRiskField[x, z] * _lakeRimErosionWeight;
                        rimStrength = Math.Clamp(rimStrength * (0.65 + shorelineBlend), 0.0, 1.35);
                        SculptLakeBank(chunk, x, z, waterLevel, rimStrength);
                    }
                }
            }

            if (lakeCreated)
            {
                ApplyLakeTerraces(chunk, surfaceCache, centerX, centerZ, waterLevel, radiusX, radiusZ, rotation);
                AddLakeShorelineBenches(chunk, surfaceCache, hydrologyMask, centerX, centerZ, waterLevel, radiusX, radiusZ, rotation, basinStability);
                DepositLakeSedimentRings(chunk, centerX, centerZ, waterLevel, radiusX, radiusZ);
                EnhanceLakeShoreVegetation(chunk, centerX, centerZ, radiusX, radiusZ, rand);
                CreateLakeSeeps(context, chunk, surfaceCache, hydrologyMask, centerX, centerZ, waterLevel, radiusX, radiusZ, rand);
                TryLinkLakeToRiver(context, chunk, riverField, hydrologyMask, flowAccumulation, surfaceCache, centerX, centerZ, waterLevel, radiusX, radiusZ);
                AddLakeWetlandPockets(chunk, surfaceCache, hydrologyMask, centerX, centerZ, waterLevel, radiusX, radiusZ, rand);
                waterLevel = EqualizeLakeWaterTable(chunk, surfaceCache, hydrologyMask, centerX, centerZ, radiusX, radiusZ, waterLevel);
                AddLakeOverflowChannels(chunk, surfaceCache, hydrologyMask, flowAccumulation, hydrologyGradient, centerX, centerZ, waterLevel, radiusX, radiusZ);
                StabilizeLakeCatchments(chunk, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterLevel, radiusX, radiusZ);
                ApplyLakeHydrologyFeedback(chunk, surfaceCache, hydrologyMask, flowAccumulation, centerX, centerZ, waterLevel, radiusX, radiusZ);
            }
        }

        private void SculptLakeBank(ChunkData chunk, int x, int z, int waterSurface, double rimStrength)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
                return;

            int surface = FindSurfaceLevel(chunk, x, z);
            if (surface <= 0)
                return;

            int maxDrop = Math.Max(1, (int)Math.Round(3.0 - 2.0 * rimStrength));

            if (surface > waterSurface + maxDrop)
            {
                int target = Math.Max(waterSurface + maxDrop, 1);
                for (int y = surface; y > target; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
                surface = target;
            }

            if (surface <= waterSurface)
            {
                for (int y = surface; y <= waterSurface && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }

                int topWater = waterSurface < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(waterSurface, 255);
                if (topWater + 1 < 256)
                {
                    chunk.SetBlock(x, topWater + 1, z, BlockType.Air);
                }
            }
            else
            {
                chunk.SetBlock(x, surface, z, BlockType.Sand);
                if (surface + 1 < 256)
                {
                    chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                }
            }

        }

        private int EqualizeLakeWaterTable(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            int waterSurface)
        {
            double rimHydrology = SampleLakeRingHydrology(hydrologyMask, centerX, centerZ, radiusX, radiusZ, 1.05, 1.4);
            double basinHydrology = hydrologyMask[Math.Clamp(centerX, 0, 15), Math.Clamp(centerZ, 0, 15)];
            double pressureDelta = (rimHydrology - basinHydrology) * 3.5;
            int targetLevel = Math.Clamp(waterSurface + (int)Math.Round(pressureDelta), waterSurface - 2, waterSurface + 3);
            targetLevel = Math.Clamp(targetLevel, 45, Math.Min(GlobalWaterLevel + 4, 120));

            if (targetLevel == waterSurface)
            {
                return waterSurface;
            }

            AdjustLakeWaterColumns(chunk, surfaceCache, centerX, centerZ, radiusX, radiusZ, waterSurface, targetLevel);
            return targetLevel;
        }

        private static double SampleLakeRingHydrology(
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            double inner,
            double outer)
        {
            double sum = 0.0;
            int samples = 0;
            int limitX = radiusX + 6;
            int limitZ = radiusZ + 6;

            for (int dx = -limitX; dx <= limitX; dx++)
            {
                for (int dz = -limitZ; dz <= limitZ; dz++)
                {
                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.5, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.5, 2)));

                    if (ellipse < inner || ellipse > outer)
                    {
                        continue;
                    }

                    int sampleX = Math.Clamp(centerX + dx, 0, 15);
                    int sampleZ = Math.Clamp(centerZ + dz, 0, 15);
                    sum += hydrologyMask[sampleX, sampleZ];
                    samples++;
                }
            }

            if (samples == 0)
            {
                return hydrologyMask[Math.Clamp(centerX, 0, 15), Math.Clamp(centerZ, 0, 15)];
            }

            return sum / samples;
        }

        private void AdjustLakeWaterColumns(
            ChunkData chunk,
            int[,] surfaceCache,
            int centerX,
            int centerZ,
            int radiusX,
            int radiusZ,
            int previousSurface,
            int targetSurface)
        {
            int maxX = radiusX + 4;
            int maxZ = radiusZ + 4;
            for (int dx = -maxX; dx <= maxX; dx++)
            {
                for (int dz = -maxZ; dz <= maxZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.35, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.35, 2)));

                    if (ellipse <= 1.05)
                    {
                        int floor = FindLakeFloor(chunk, x, z, targetSurface);
                        floor = Math.Max(1, floor);
                        chunk.SetBlock(x, floor, z, BlockType.Clay);
                        int waterTop = Math.Min(targetSurface, 255);
                        for (int y = floor + 1; y <= waterTop && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        int maxClear = Math.Max(previousSurface, waterTop);
                        for (int y = waterTop + 1; y <= maxClear && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        surfaceCache[x, z] = waterTop;
                    }
                    else if (ellipse <= 1.35)
                    {
                        double rimStrength = Math.Clamp(1.35 - ellipse, 0.0, 0.6);
                        SculptLakeBank(chunk, x, z, targetSurface, rimStrength + 0.2);
                    }
                }
            }
        }

        private static int FindLakeFloor(ChunkData chunk, int x, int z, int searchStart)
        {
            for (int y = Math.Min(searchStart, 254); y >= 1; y--)
            {
                var block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    return y;
                }
            }

            return 1;
        }

        private void ApplyLakeTerraces(
            ChunkData chunk,
            int[,] surfaceCache,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            double rotation)
        {
            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double shallowRadiusX = Math.Max(2.0, radiusX * 0.7);
            double shallowRadiusZ = Math.Max(2.0, radiusZ * 0.7);
            double bankRadiusX = radiusX + 4.0;
            double bankRadiusZ = radiusZ + 4.0;

            int extentX = (int)Math.Ceiling(bankRadiusX);
            int extentZ = (int)Math.Ceiling(bankRadiusZ);

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double rotX = dx * cos - dz * sin;
                    double rotZ = dx * sin + dz * cos;

                    double shallowEllipse = Math.Sqrt(
                        (rotX * rotX) / Math.Max(1.0, shallowRadiusX * shallowRadiusX) +
                        (rotZ * rotZ) / Math.Max(1.0, shallowRadiusZ * shallowRadiusZ));

                    double bankEllipse = Math.Sqrt(
                        (rotX * rotX) / Math.Max(1.0, bankRadiusX * bankRadiusX) +
                        (rotZ * rotZ) / Math.Max(1.0, bankRadiusZ * bankRadiusZ));

                    if (shallowEllipse <= 1.0)
                    {
                        int shelfTop = Math.Max(1, waterSurface - 1);
                        for (int y = shelfTop; y <= waterSurface && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }

                        int floor = Math.Max(1, shelfTop - 1);
                        chunk.SetBlock(x, floor, z, BlockType.Sand);
                        surfaceCache[x, z] = Math.Min(waterSurface, 255);
                    }
                    else if (bankEllipse <= 1.2)
                    {
                        int surface = surfaceCache[x, z];
                        if (surface <= 0)
                        {
                            surface = FindSurfaceLevel(chunk, x, z);
                            if (surface <= 0)
                            {
                                continue;
                            }
                            surfaceCache[x, z] = surface;
                        }

                        chunk.SetBlock(x, surface, z, BlockType.Sand);
                        if (surface + 1 < 256)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                        }
                    }
                }
            }
        }

        private void AddLakeShorelineBenches(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            double rotation,
            double basinStability)
        {
            if (basinStability < 0.2)
            {
                return;
            }

            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);
            double radiusXWithPadding = radiusX + 0.75;
            double radiusZWithPadding = radiusZ + 0.75;
            int extentX = radiusX + 6;
            int extentZ = radiusZ + 6;

            var bands = new (double inner, double outer, bool floodable)[]
            {
                (1.02, 1.18, true),
                (1.18, 1.36, false)
            };

            foreach (var band in bands)
            {
                for (int dx = -extentX; dx <= extentX; dx++)
                {
                    for (int dz = -extentZ; dz <= extentZ; dz++)
                    {
                        int worldX = centerX + dx;
                        int worldZ = centerZ + dz;
                        if (worldX < 0 || worldX >= 16 || worldZ < 0 || worldZ >= 16)
                        {
                            continue;
                        }

                        double rotX = dx * cos - dz * sin;
                        double rotZ = dx * sin + dz * cos;
                        double ellipse = Math.Sqrt(
                            (rotX * rotX) / Math.Max(1.0, radiusXWithPadding * radiusXWithPadding) +
                            (rotZ * rotZ) / Math.Max(1.0, radiusZWithPadding * radiusZWithPadding));

                        if (ellipse < band.inner || ellipse > band.outer)
                        {
                            continue;
                        }

                        if (!TryResolveSurface(chunk, surfaceCache, worldX, worldZ, out int surface))
                        {
                            continue;
                        }

                        int targetSurface = band.floodable
                            ? Math.Max(waterSurface + (basinStability > 0.6 ? 0 : 1), 1)
                            : Math.Max(waterSurface + 2 + (band.inner > 1.18 ? 1 : 0), 1);
                        targetSurface = Math.Min(surface, targetSurface);

                        for (int y = surface; y > targetSurface; y--)
                        {
                            chunk.SetBlock(worldX, y, worldZ, BlockType.Air);
                        }

                        double hydrology = Math.Clamp(
                            hydrologyMask[
                                Math.Clamp(worldX, 0, hydrologyMask.GetLength(0) - 1),
                                Math.Clamp(worldZ, 0, hydrologyMask.GetLength(1) - 1)],
                            0.0,
                            1.0);

                        BlockType material = band.floodable
                            ? (hydrology > 0.62 ? BlockType.Clay : BlockType.Sand)
                            : (basinStability > 0.5 ? BlockType.Grass : BlockType.Dirt);
                        chunk.SetBlock(worldX, targetSurface, worldZ, material);

                        if (band.floodable)
                        {
                            for (int y = targetSurface + 1; y <= waterSurface && y < 256; y++)
                            {
                                chunk.SetBlock(worldX, y, worldZ, BlockType.Water);
                            }
                        }
                        else if (targetSurface + 1 < 256)
                        {
                            chunk.SetBlock(worldX, targetSurface + 1, worldZ, BlockType.Air);
                        }

                        surfaceCache[worldX, worldZ] = targetSurface;
                    }
                }
            }
        }

        private void TryLinkLakeToRiver(TerrainGenerationContext context, ChunkData chunk, RiverFieldCache riverField, double[,] hydrologyMask, double[,] flowAccumulation, int[,] surfaceCache, int centerX, int centerZ, int waterLevel, int radiusX, int radiusZ)
        {
            int searchRadius = Math.Max(radiusX, radiusZ) + 6;
            double bestScore = 0.09;
            int bestX = -1;
            int bestZ = -1;

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dz = -searchRadius; dz <= searchRadius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrologyMask[x, z]);
                    if (intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    double catchment = flowAccumulation[x, z];
                    double catchmentBias = Math.Clamp(catchment / 8.0, 0.0, 1.0);
                    double score = intensity - hydrologyMask[x, z] * 0.02 - catchmentBias * 0.015;
                    double relief = ComputeLocalRelief(surfaceCache, x, z, 2);
                    score += Math.Clamp(relief / 10.0, 0.0, 1.0) * 0.01;
                    double inflowFavor = Math.Clamp(catchment * 0.12 + hydrologyMask[x, z] * 0.15, 0.0, 1.0);
                    score -= inflowFavor * _lakeInflowBlendWeight * 0.04;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            if (bestX < 0 || bestZ < 0)
            {
                return;
            }

            int dxTarget = bestX - centerX;
            int dzTarget = bestZ - centerZ;
            double length = Math.Sqrt(dxTarget * dxTarget + dzTarget * dzTarget);
            int startX = centerX;
            int startZ = centerZ;
            if (length > 1.0)
            {
                double normDx = dxTarget / length;
                double normDz = dzTarget / length;
                int offset = Math.Max(Math.Max(radiusX, radiusZ) - 1, 1);
                startX = centerX + (int)Math.Round(normDx * offset);
                startZ = centerZ + (int)Math.Round(normDz * offset);
            }

            startX = Math.Clamp(startX, 0, 15);
            startZ = Math.Clamp(startZ, 0, 15);

            CarveLakeChannel(chunk, startX, startZ, bestX, bestZ, waterLevel);
        }

        private void CarveLakeChannel(ChunkData chunk, int startX, int startZ, int endX, int endZ, int waterLevel)
        {
            int dx = endX - startX;
            int dz = endZ - startZ;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (steps == 0)
            {
                return;
            }

            for (int step = 0; step <= steps; step++)
            {
                double t = step / (double)steps;
                int x = startX + (int)Math.Round(dx * t);
                int z = startZ + (int)Math.Round(dz * t);
                if (x < 0 || x >= 16 || z < 0 || z >= 16)
                {
                    continue;
                }

                int surface = FindSurfaceLevel(chunk, x, z);
                if (surface <= 0)
                {
                    continue;
                }

                int carvingFloor = Math.Max(1, waterLevel - 2);
                if (surface > waterLevel + 1)
                {
                    for (int y = surface; y > waterLevel + 1; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                    surface = waterLevel + 1;
                }

                for (int y = surface; y >= carvingFloor; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }

                if (carvingFloor - 1 >= 1)
                {
                    chunk.SetBlock(x, carvingFloor - 1, z, BlockType.Sand);
                }

                for (int y = carvingFloor; y <= waterLevel && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }

                if (waterLevel + 1 < 256)
                {
                    chunk.SetBlock(x, waterLevel + 1, z, BlockType.Air);
                }

                // Slight widening for natural look
                for (int widen = -1; widen <= 1; widen++)
                {
                    if (widen == 0)
                    {
                        continue;
                    }

                    int wx = x + widen;
                    if (wx < 0 || wx >= 16)
                    {
                        continue;
                    }

                    int wSurface = FindSurfaceLevel(chunk, wx, z);
                    if (wSurface <= 0)
                    {
                        continue;
                    }

                    if (wSurface > waterLevel)
                    {
                        chunk.SetBlock(wx, wSurface, z, BlockType.Sand);
                    }
                }
            }
        }

        public void GenerateCloudsInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = context.ChunkX * 16 + x;
                    var worldZ = context.ChunkZ * 16 + z;

                    var warp = SimplexNoise.DomainWarp(worldX, worldZ, 0.0005, 0.001, 12.0, 6.0, 44444);
                    double noiseSimplex = SimplexNoise.Generate(worldX + warp.dx, worldZ + warp.dz, 0.0009, 4, 1.0, 0.6, 44444);
                    double noisePerlin = PerlinNoise.Generate(worldX + warp.dx, worldZ + warp.dz, 0.0012, 2, 1.0, 0.55, 55444);
                    double noise = (noiseSimplex * 0.65) + (noisePerlin * 0.35);
                    if (noise <= 0.68)
                        continue;

                    int baseAltitude = CloudBaseAltitude + (int)((noise - 0.68) * 45);
                    baseAltitude = Math.Clamp(baseAltitude, CloudBaseAltitude, 255);

                    if (chunk.GetBlock(x, baseAltitude, z) == BlockType.Air)
                    {
                        chunk.SetBlock(x, baseAltitude, z, BlockType.Cloud);

                        if (noise > 0.78 && baseAltitude + 1 < 256)
                        {
                            chunk.SetBlock(x, baseAltitude + 1, z, BlockType.Cloud);
                        }
                    }
                }
            }
        }

        private void CarveRiverColumn(ChunkData chunk, int[,] surfaceCache, int x, int z, int riverSurface, double normalized, double channelPressure, Vector2 flowDir)
        {
            int surface = surfaceCache[x, z];
            if (surface <= 0)
                return;

            if (surface <= GlobalWaterLevel - 3 && chunk.GetBlock(x, surface, z) == BlockType.Water)
                return;

            double pressureScale = 0.85 + channelPressure * 0.65;
            int baseDepth = Math.Clamp(_riverDepth, 2, 24);
            int channelDepth = Math.Clamp(baseDepth + (int)Math.Round(normalized * baseDepth * 1.3 * pressureScale), baseDepth, baseDepth + 8);
            int waterFloor = Math.Max(1, riverSurface - channelDepth);

            for (int y = surface; y >= waterFloor; y--)
            {
                chunk.SetBlock(x, y, z, BlockType.Air);
            }

            if (waterFloor - 1 >= 0)
            {
                chunk.SetBlock(x, waterFloor - 1, z, BlockType.Sand);
            }

            for (int y = waterFloor; y <= riverSurface && y < 256; y++)
            {
                chunk.SetBlock(x, y, z, BlockType.Water);
            }

            if (riverSurface < GlobalWaterLevel)
            {
                for (int y = riverSurface + 1; y <= GlobalWaterLevel && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }
            }

            int updatedSurface = riverSurface < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(riverSurface, 255);
            surfaceCache[x, z] = updatedSurface;

            ExpandRiverChannel(chunk, surfaceCache, x, z, riverSurface, flowDir, channelPressure);

            int maxRadius = Math.Clamp(2 + (int)Math.Round(normalized * (2.0 + channelPressure * 1.5)), 2, 5);
            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (int dz = -maxRadius; dz <= maxRadius; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance > maxRadius + 0.25)
                        continue;

                    double falloff = 1.0 - Math.Clamp(distance / (maxRadius + 0.001), 0.0, 1.0);
                    bool allowFlood = distance <= 1.5;
                    ShapeRiverBank(chunk, surfaceCache, x + dx, z + dz, falloff, riverSurface, allowFlood);
                }
            }

            Vector2 normalizedFlow = flowDir.LengthSquared() < 1e-6f ? Vector2.UnitX : Vector2.Normalize(flowDir);
            int forwardX = x + (int)Math.Round(normalizedFlow.X);
            int forwardZ = z + (int)Math.Round(normalizedFlow.Y);
            ShapeRiverBank(chunk, surfaceCache, forwardX, forwardZ, 0.35, riverSurface, false);

            int backX = x - (int)Math.Round(normalizedFlow.X);
            int backZ = z - (int)Math.Round(normalizedFlow.Y);
            ShapeRiverBank(chunk, surfaceCache, backX, backZ, 0.35, riverSurface, false);
        }

        private void FeatherRiverBank(ChunkData chunk, int[,] surfaceCache, int x, int z, double strength, int riverSurface, Vector2 flowDir)
        {
            if (strength <= 0.0)
                return;

            double clamped = Math.Clamp(strength, 0.0, 1.0);
            ShapeRiverBank(chunk, surfaceCache, x, z, clamped * 0.65, riverSurface, false);

            Vector2 perpendicular = new(-flowDir.Y, flowDir.X);
            if (perpendicular.LengthSquared() < 1e-6f)
            {
                perpendicular = Vector2.UnitX;
            }
            perpendicular = Vector2.Normalize(perpendicular);

            int reach = Math.Max(1, (int)Math.Round(1.0 + clamped * 2.0));
            for (int step = 1; step <= reach; step++)
            {
                double falloff = Math.Clamp(clamped - step * 0.25, 0.0, 1.0);
                if (falloff <= 0.0)
                {
                    break;
                }

                ResolvePerpendicularOffset(perpendicular, step, out int offsetX, out int offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, falloff, riverSurface, false);

                ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), step, out offsetX, out offsetZ);
                ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, falloff, riverSurface, false);
            }
        }

        private void ApplyRiparianBankStabilization(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riparianSaturation,
            double[,] riverIntensity)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double riparian = riparianSaturation[x, z];
                    if (riparian < 0.6)
                    {
                        continue;
                    }

                    double intensity = riverIntensity[x, z];
                    if (intensity >= RiverBankThreshold || intensity <= RiverCenterThreshold * 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int riverSurface = Math.Min(surface, GlobalWaterLevel);
                    double hydrology = hydrologyMask[x, z];
                    double shelfStrength = Math.Clamp((riparian - 0.55) * 1.4 + hydrology * 0.25, 0.0, 1.0);
                    bool allowFlood = intensity < RiverBankThreshold * 0.65;

                    ShapeRiverBank(chunk, surfaceCache, x, z, shelfStrength * 0.8, riverSurface, allowFlood);
                    ShapeRiverBank(chunk, surfaceCache, x, z, shelfStrength * 0.5, riverSurface, false);
                }
            }
        }

        private void StitchTributaryChannels(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            RiverFieldCache riverField,
            double[,] riverIntensity)
        {
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltRiverTributary);
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.7)
                    {
                        continue;
                    }

                    double catchment = Math.Clamp(flowAccumulation[x, z] / 7.5, 0.0, 1.0);
                    double intensity = riverField.Intensity[x, z];
                    if (intensity >= RiverBankThreshold * 1.1 || intensity <= RiverCenterThreshold * 0.35)
                    {
                        continue;
                    }

                    double riverGap = 1.0 - Math.Clamp(intensity / RiverCenterThreshold, 0.0, 1.0);
                    double weight = hydrology * 0.4 + catchment * 0.4 + riverGap * 0.35;
                    if (weight < 0.55 || rand.NextDouble() > weight * 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    var flowDir = riverField.Flow[x, z];
                    var slopeDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    Vector2 direction = flowDir.LengthSquared() > 1e-4f ? flowDir : slopeDir;
                    if (direction.LengthSquared() < 1e-4f)
                    {
                        continue;
                    }

                    TraceTributaryChannel(chunk, surfaceCache, riverIntensity, x, z, direction, weight, rand);
                }
            }
        }

        private void DepositLakeSedimentRings(ChunkData chunk, int centerX, int centerZ, int waterSurface, int radiusX, int radiusZ)
        {
            double innerRadiusX = Math.Max(1.0, radiusX + 0.5);
            double innerRadiusZ = Math.Max(1.0, radiusZ + 0.5);
            double outerRadiusX = innerRadiusX + 2.0;
            double outerRadiusZ = innerRadiusZ + 2.0;
            int extentX = (int)Math.Ceiling(outerRadiusX + 2.0);
            int extentZ = (int)Math.Ceiling(outerRadiusZ + 2.0);

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, innerRadiusX * innerRadiusX) +
                        (dz * dz) / Math.Max(1.0, innerRadiusZ * innerRadiusZ));
                    if (ellipse > 1.35)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(chunk, x, z);
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int targetY = Math.Max(1, Math.Min(surface, waterSurface) - 1);
                    if (ellipse <= 1.0)
                    {
                        chunk.SetBlock(x, targetY, z, BlockType.Clay);
                    }
                    else if (ellipse <= 1.25)
                    {
                        chunk.SetBlock(x, targetY, z, BlockType.Sand);
                    }
                }
            }
        }

        private void EnhanceLakeShoreVegetation(ChunkData chunk, int centerX, int centerZ, int radiusX, int radiusZ, Random rand)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, radiusX * radiusX) +
                        (dz * dz) / Math.Max(1.0, radiusZ * radiusZ));

                    if (ellipse > 1.45)
                    {
                        continue;
                    }

                    int surface = FindSurfaceLevel(chunk, x, z);
                    if (surface <= 0 || surface + 1 >= 256)
                    {
                        continue;
                    }

                    var topBlock = chunk.GetBlock(x, surface, z);
                    if (topBlock == BlockType.Sand && rand.NextDouble() < 0.45)
                    {
                        if (chunk.GetBlock(x, surface + 1, z) == BlockType.Air)
                        {
                            chunk.SetBlock(x, surface + 1, z, rand.NextDouble() < 0.7 ? BlockType.TallGrass : BlockType.DeadBush);
                        }
                    }
                    else if (topBlock == BlockType.Dirt && ellipse <= 1.1 && rand.NextDouble() < 0.35)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Grass);
                        if (chunk.GetBlock(x, surface + 1, z) == BlockType.Air && rand.NextDouble() < 0.4)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.TallGrass);
                        }
                    }
                }
            }
        }

        private void CreateLakeSeeps(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            Random rand)
        {
            int attempts = 4;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int sampleX = Math.Clamp(centerX + rand.Next(-radiusX - 5, radiusX + 6), 1, 14);
                int sampleZ = Math.Clamp(centerZ + rand.Next(-radiusZ - 5, radiusZ + 6), 1, 14);

                double hydrology = hydrologyMask[sampleX, sampleZ];
                if (hydrology < 0.75)
                {
                    continue;
                }

                int surface = surfaceCache[sampleX, sampleZ];
                if (surface <= waterSurface + 1)
                {
                    continue;
                }

                int trenchDepth = Math.Clamp((int)Math.Round(1 + (hydrology - 0.7) * 4.0), 1, 4);
                int floor = Math.Max(surface - trenchDepth, 1);

                for (int y = surface; y >= floor; y--)
                {
                    chunk.SetBlock(sampleX, y, sampleZ, BlockType.Air);
                }

                for (int y = floor; y <= waterSurface && y < 256; y++)
                {
                    chunk.SetBlock(sampleX, y, sampleZ, BlockType.Water);
                }

                if (floor - 1 >= 1)
                {
                    chunk.SetBlock(sampleX, floor - 1, sampleZ, BlockType.Clay);
                }

                surfaceCache[sampleX, sampleZ] = Math.Max(surfaceCache[sampleX, sampleZ], Math.Min(waterSurface, 255));
            }
        }

        private void AddLakeWetlandPockets(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ,
            Random rand)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;
            int pockets = 0;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 1 || x >= 15 || z < 1 || z >= 15)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.5, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.5, 2)));
                    if (ellipse <= 1.05 || ellipse >= 1.45)
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    double spawnWeight = hydrology * 0.65 +
                                         Math.Clamp((ellipse - 1.05) * 1.4, 0.0, 0.35) +
                                         rand.NextDouble() * 0.2;
                    if (spawnWeight < 0.75)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2 || surface < waterSurface - 4 || surface > waterSurface + 6)
                    {
                        continue;
                    }

                    int pocketDepth = Math.Clamp((int)Math.Round(1 + (hydrology - 0.45) * 3.0), 1, 3);
                    int floor = Math.Max(1, surface - pocketDepth);
                    for (int y = surface; y >= floor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    if (hydrology > 0.65)
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Water);
                        if (floor + 1 < 256)
                        {
                            chunk.SetBlock(x, floor + 1, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Sand);
                    }

                    surfaceCache[x, z] = floor;

                    for (int nx = -1; nx <= 1; nx++)
                    {
                        for (int nz = -1; nz <= 1; nz++)
                        {
                            if (nx == 0 && nz == 0)
                            {
                                continue;
                            }

                            int rimX = x + nx;
                            int rimZ = z + nz;
                            if (rimX < 0 || rimX >= 16 || rimZ < 0 || rimZ >= 16)
                            {
                                continue;
                            }

                            int rimSurface = FindSurfaceLevel(chunk, rimX, rimZ);
                            if (rimSurface <= 0)
                            {
                                continue;
                            }

                            var rimType = hydrology > 0.6 ? BlockType.Clay : BlockType.Grass;
                            chunk.SetBlock(rimX, rimSurface, rimZ, rimType);
                        }
                    }

                    pockets++;
                    if (pockets >= 6)
                    {
                        return;
                    }
                }
            }
        }

        private void AddLakeOverflowChannels(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            Vector2[,] hydrologyGradient,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            (int dx, int dz, int extent)[] directions =
            {
                (1, 0, radiusX + 2),
                (-1, 0, radiusX + 2),
                (0, 1, radiusZ + 2),
                (0, -1, radiusZ + 2)
            };

            (int dx, int dz, double weight) primary = (0, 0, 0.0);
            (int dx, int dz, double weight) secondary = (0, 0, 0.0);
            Vector2 centerGradient = hydrologyGradient[centerX, centerZ];
            double centerGradientStrength = Math.Clamp(centerGradient.Length(), 0.0, 1.75);
            Vector2 centerGradientDir = centerGradientStrength > 1e-5 ? Vector2.Normalize(centerGradient) : Vector2.Zero;
            double gradientWeight = Math.Clamp(_hydrologyGradientWeight, 0.0, 1.5);

            foreach (var dir in directions)
            {
                int edgeX = centerX + dir.dx * dir.extent;
                int edgeZ = centerZ + dir.dz * dir.extent;
                if (edgeX < 1 || edgeX >= 15 || edgeZ < 1 || edgeZ >= 15)
                {
                    continue;
                }

                double hydrology = Math.Clamp(hydrologyMask[edgeX, edgeZ], 0.0, 1.0);
                double accumulation = Math.Clamp(flowAccumulation[edgeX, edgeZ], 0.0, 6.0);
                int neighborSurface = surfaceCache[edgeX, edgeZ];
                if (neighborSurface <= 0)
                {
                    neighborSurface = FindSurfaceLevel(chunk, edgeX, edgeZ);
                    if (neighborSurface <= 0)
                    {
                        continue;
                    }
                    surfaceCache[edgeX, edgeZ] = neighborSurface;
                }

                double slope = Math.Clamp((waterSurface - neighborSurface) / 8.0, -1.0, 1.0);
                double weight = Math.Clamp(hydrology * 0.6 + Math.Clamp(accumulation / 4.0, 0.0, 1.0) * 0.4, 0.0, 1.0);
                weight *= 0.55 + Math.Max(0.0, slope);
                if (gradientWeight > 0.0)
                {
                    Vector2 sampleGradient = hydrologyGradient[edgeX, edgeZ];
                    double gradientStrength = Math.Clamp(sampleGradient.Length(), 0.0, 1.75);
                    double gradientFactor = gradientStrength * gradientWeight;
                    if (centerGradientDir != Vector2.Zero)
                    {
                        var dirVec = Vector2.Normalize(new Vector2(dir.dx, dir.dz));
                        double alignment = Math.Max(0.0, Vector2.Dot(centerGradientDir, dirVec));
                        gradientFactor += alignment * gradientWeight;
                    }

                    weight *= 0.85 + gradientFactor * 0.25;
                }
                if (weight <= 0.35)
                {
                    continue;
                }

                if (weight > primary.weight)
                {
                    secondary = primary;
                    primary = (dir.dx, dir.dz, weight);
                }
                else if (weight > secondary.weight)
                {
                    secondary = (dir.dx, dir.dz, weight);
                }
            }

            if (primary.weight > 0.0)
            {
                CarveLakeOverflowChannel(chunk, surfaceCache, centerX, centerZ, primary.dx, primary.dz, waterSurface, primary.weight);
            }

            if (secondary.weight > 0.0)
            {
                CarveLakeOverflowChannel(chunk, surfaceCache, centerX, centerZ, secondary.dx, secondary.dz, waterSurface, secondary.weight * 0.85);
            }
        }

        private void CarveLakeOverflowChannel(
            ChunkData chunk,
            int[,] surfaceCache,
            int originX,
            int originZ,
            int dirX,
            int dirZ,
            int waterSurface,
            double weight)
        {
            if (dirX == 0 && dirZ == 0)
            {
                return;
            }

            int length = Math.Clamp((int)Math.Round(2 + weight * 3 + _lakeOutflowCarveDepth * 0.35), 2, 12);
            int currentX = originX + dirX * 2;
            int currentZ = originZ + dirZ * 2;
            int maxDrop = Math.Max(1, Math.Min(_lakeOutflowCarveDepth, 12));

            for (int step = 0; step < length; step++)
            {
                if (currentX < 1 || currentX >= 15 || currentZ < 1 || currentZ >= 15)
                {
                    break;
                }

                int surface = surfaceCache[currentX, currentZ];
                if (surface <= 1)
                {
                    surface = FindSurfaceLevel(chunk, currentX, currentZ);
                    if (surface <= 1)
                    {
                        break;
                    }
                    surfaceCache[currentX, currentZ] = surface;
                }

                int depth = Math.Max(1, maxDrop - step / 2);
                int target = Math.Max(1, waterSurface - depth);
                if (surface > target)
                {
                    for (int y = surface; y > target; y--)
                    {
                        chunk.SetBlock(currentX, y, currentZ, BlockType.Air);
                    }
                }

                int sandY = Math.Max(1, target - 1);
                chunk.SetBlock(currentX, sandY, currentZ, BlockType.Clay);
                for (int y = target; y <= Math.Min(waterSurface, 255); y++)
                {
                    chunk.SetBlock(currentX, y, currentZ, BlockType.Water);
                }

                surfaceCache[currentX, currentZ] = target;

                int bankX = currentX + dirZ;
                int bankZ = currentZ - dirX;
                if (bankX >= 0 && bankX < 16 && bankZ >= 0 && bankZ < 16)
                {
                    SculptLakeBank(chunk, bankX, bankZ, waterSurface, 0.6);
                }

                bankX = currentX - dirZ;
                bankZ = currentZ + dirX;
                if (bankX >= 0 && bankX < 16 && bankZ >= 0 && bankZ < 16)
                {
                    SculptLakeBank(chunk, bankX, bankZ, waterSurface, 0.55);
                }

                currentX += dirX;
                currentZ += dirZ;
            }
        }

        private void StabilizeLakeCatchments(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            int extentX = radiusX + 5;
            int extentZ = radiusZ + 5;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.6, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.6, 2)));
                    if (ellipse < 0.9 || ellipse > 1.6)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double pressure = Math.Max(hydrology, flow);
                    if (pressure < 0.55)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1 || surface < waterSurface - 4)
                    {
                        continue;
                    }

                    int erosionDepth = Math.Clamp((int)Math.Round((pressure - 0.45) * 6.0), 1, 4);
                    int floor = Math.Max(surface - erosionDepth, 1);
                    for (int y = surface; y >= floor && y >= 1; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    int fillTop = Math.Min(waterSurface, 254);
                    if (hydrology > 0.65)
                    {
                        for (int y = floor; y <= fillTop && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        var fillMaterial = hydrology > 0.78 ? BlockType.Clay : BlockType.Sand;
                        chunk.SetBlock(x, floor, z, fillMaterial);
                    }

                    surfaceCache[x, z] = floor;
                    double rimStrength = Math.Clamp(pressure * 0.8, 0.2, 0.85);
                    SculptLakeBank(chunk, x, z, waterSurface, rimStrength);
                }
            }
        }

        private void ApplyLakeHydrologyFeedback(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            int centerX,
            int centerZ,
            int waterSurface,
            int radiusX,
            int radiusZ)
        {
            int extentX = radiusX + 6;
            int extentZ = radiusZ + 6;

            for (int dx = -extentX; dx <= extentX; dx++)
            {
                for (int dz = -extentZ; dz <= extentZ; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= 16 || z < 0 || z >= 16)
                    {
                        continue;
                    }

                    double ellipse = Math.Sqrt(
                        (dx * dx) / Math.Max(1.0, Math.Pow(radiusX + 0.75, 2)) +
                        (dz * dz) / Math.Max(1.0, Math.Pow(radiusZ + 0.75, 2)));
                    if (ellipse <= 1.05 || ellipse >= 1.65)
                    {
                        continue;
                    }

                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int surface) || surface <= 0)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double moisture = Math.Max(hydrology, flow);
                    if (moisture < 0.45)
                    {
                        continue;
                    }

                    int drop = Math.Clamp((int)Math.Round(1 + moisture * 3.5), 1, 4);
                    int target = Math.Max(surface - drop, Math.Max(waterSurface - 1, 1));

                    for (int y = surface; y > target; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    if (hydrology > 0.65)
                    {
                        chunk.SetBlock(x, target, z, BlockType.Clay);
                        for (int y = target + 1; y <= waterSurface && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                        surfaceCache[x, z] = Math.Min(waterSurface, 255);
                    }
                    else
                    {
                        chunk.SetBlock(x, target, z, BlockType.Sand);
                        if (target + 1 < 256)
                        {
                            chunk.SetBlock(x, target + 1, z, BlockType.Air);
                        }
                        surfaceCache[x, z] = target;
                    }
                }
            }
        }

        private void TraceTributaryChannel(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] riverIntensity,
            int startX,
            int startZ,
            Vector2 direction,
            double strength,
            Random rand)
        {
            if (direction.LengthSquared() < 1e-4f)
            {
                return;
            }

            var dir = Vector2.Normalize(direction);
            double stepX = dir.X;
            double stepZ = dir.Y;
            int steps = Math.Clamp((int)Math.Round(3 + strength * 5), 3, 8);
            double x = startX;
            double z = startZ;
            double channelPressure = Math.Clamp(0.35 + strength * 0.4, 0.35, 0.85);

            for (int i = 0; i < steps; i++)
            {
                int cx = (int)Math.Round(x);
                int cz = (int)Math.Round(z);
                if (cx < 0 || cx >= 16 || cz < 0 || cz >= 16)
                {
                    break;
                }

                int surface = surfaceCache[cx, cz];
                if (surface <= 0)
                {
                    break;
                }

                int riverSurface = Math.Min(surface, GlobalWaterLevel);
                double normalized = Math.Clamp(0.55 + strength * 0.35 - i * 0.08, 0.2, 0.95);
                CarveRiverColumn(chunk, surfaceCache, cx, cz, riverSurface, normalized, channelPressure, dir);
                riverIntensity[cx, cz] = Math.Min(riverIntensity[cx, cz], RiverCenterThreshold * 0.5);

                x += stepX + (rand.NextDouble() - 0.5) * 0.3;
                z += stepZ + (rand.NextDouble() - 0.5) * 0.3;
            }
        }

        private void ApplyRiverbankErosion(ChunkData chunk, int[,] surfaceCache, RiverFieldCache riverField, double[,] hydrologyMask)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrologyMask[x, z]);
                    if (intensity >= RiverBankThreshold + 0.01)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        surface = FindSurfaceLevel(chunk, x, z);
                        if (surface <= 1)
                        {
                            continue;
                        }
                        surfaceCache[x, z] = surface;
                    }

                    int neighborSum = 0;
                    int neighborCount = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                neighborSurface = FindSurfaceLevel(chunk, nx, nz);
                                if (neighborSurface <= 0)
                                {
                                    continue;
                                }
                                surfaceCache[nx, nz] = neighborSurface;
                            }

                            neighborSum += neighborSurface;
                            neighborCount++;
                        }
                    }

                    if (neighborCount == 0)
                    {
                        continue;
                    }

                    int neighborAverage = neighborSum / neighborCount;
                    if (surface - neighborAverage > 2)
                    {
                        int target = Math.Max(neighborAverage + 1, 1);
                        for (int y = surface; y > target; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }
                        surface = target;
                        surfaceCache[x, z] = surface;
                    }

                    var topBlock = chunk.GetBlock(x, surface, z);
                    if (topBlock == BlockType.Grass || topBlock == BlockType.Dirt)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Sand);
                    }

                    if (intensity < RiverCenterThreshold * 0.6 && surface - 1 >= 1)
                    {
                        var belowBlock = chunk.GetBlock(x, surface - 1, z);
                        if (belowBlock == BlockType.Dirt || belowBlock == BlockType.Grass)
                        {
                            chunk.SetBlock(x, surface - 1, z, BlockType.Sand);
                        }
                    }

                    if (surface + 1 < 256 && chunk.GetBlock(x, surface + 1, z) != BlockType.Air)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.Air);
                    }
                }
            }

        }

        private void ApplyRiverSedimentPass(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] hydrologyMask)
        {
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltRiverSediment);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    double intensity = AdjustRiverIntensity(riverField.Intensity[x, z], hydrology);
                    if (intensity >= RiverBankThreshold + 0.01)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 1)
                    {
                        continue;
                    }

                    if (intensity < RiverCenterThreshold * 0.85)
                    {
                        int bedY = Math.Max(1, surface - 2 - rand.Next(2));
                        chunk.SetBlock(x, bedY, z, rand.NextDouble() < 0.4 ? BlockType.Cobblestone : BlockType.Sand);
                        if (bedY - 1 >= 1 && rand.NextDouble() < 0.25)
                        {
                            chunk.SetBlock(x, bedY - 1, z, BlockType.Cobblestone);
                        }

                        continue;
                    }

                    double floodChance = Math.Clamp((hydrology - 0.45) * 1.4, 0.0, 1.0);
                    if (floodChance <= 0.0 || rand.NextDouble() > floodChance)
                    {
                        continue;
                    }

                    int targetHeight = Math.Max(surface - 1, 1);
                    chunk.SetBlock(x, targetHeight, z, BlockType.Sand);
                    if (targetHeight + 1 < 256)
                    {
                        chunk.SetBlock(x, targetHeight + 1, z, BlockType.Air);
                    }
                }
            }
        }

        private void ApplyRiverPointBarSediment(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] riverIntensity)
        {
            int originX = context.ChunkX * 16;
            int originZ = context.ChunkZ * 16;

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity < RiverCenterThreshold * 0.85 || intensity > RiverBankThreshold * 1.05)
                    {
                        continue;
                    }

                    Vector2 flow = riverField.Flow[x, z];
                    if (flow.LengthSquared() < 1e-4f)
                    {
                        continue;
                    }

                    Vector2 perpendicular = new Vector2(-flow.Y, flow.X);
                    if (perpendicular.LengthSquared() < 1e-5f)
                    {
                        perpendicular = new Vector2(1, 0);
                    }
                    perpendicular = Vector2.Normalize(perpendicular);

                    double noise = SimplexNoise.Generate(originX + x * 0.45, originZ + z * 0.45, 0.08, 2, 1.0, 0.55, 8713);
                    int offsetSign = noise >= 0 ? 1 : -1;
                    int targetX = x + Math.Clamp((int)Math.Round(perpendicular.X) * offsetSign, -1, 1);
                    int targetZ = z + Math.Clamp((int)Math.Round(perpendicular.Y) * offsetSign, -1, 1);
                    if (targetX < 0 || targetX >= 16 || targetZ < 0 || targetZ >= 16)
                    {
                        continue;
                    }

                    int surface = surfaceCache[targetX, targetZ];
                    if (surface <= 0)
                    {
                        surface = FindSurfaceLevel(chunk, targetX, targetZ);
                        if (surface <= 0)
                        {
                            continue;
                        }
                        surfaceCache[targetX, targetZ] = surface;
                    }

                    var topBlock = chunk.GetBlock(targetX, surface, targetZ);
                    if (topBlock == BlockType.Air)
                    {
                        continue;
                    }

                    if (topBlock == BlockType.Grass || topBlock == BlockType.Dirt || topBlock == BlockType.Clay)
                    {
                        chunk.SetBlock(targetX, surface, targetZ, BlockType.Sand);
                        if (surface - 1 >= 1 && chunk.GetBlock(targetX, surface - 1, targetZ) == BlockType.Dirt)
                        {
                            chunk.SetBlock(targetX, surface - 1, targetZ, BlockType.Clay);
                        }
                    }

                    if (intensity < RiverCenterThreshold * 1.05)
                    {
                        int waterFloor = Math.Max(1, surface - 1);
                        chunk.SetBlock(targetX, Math.Max(1, waterFloor - 1), targetZ, BlockType.Sand);
                        chunk.SetBlock(targetX, waterFloor, targetZ, BlockType.Water);
                        if (surface + 1 < 256)
                        {
                            chunk.SetBlock(targetX, surface + 1, targetZ, BlockType.Air);
                        }
                        surfaceCache[targetX, targetZ] = Math.Max(surfaceCache[targetX, targetZ], waterFloor);
                    }
                }
            }
        }

        private void AddFloodplainWetlands(
            TerrainGenerationContext context,
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltRiverWetland);

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.72)
                    {
                        continue;
                    }

                    double channelProximity = 1.0 - Math.Clamp(
                        (riverIntensity[x, z] - RiverCenterThreshold) /
                        (RiverBankThreshold - RiverCenterThreshold + 1e-5),
                        0.0,
                        1.0);

                    double weight = hydrology * 0.6 + channelProximity * 0.4;
                    if (weight < 0.78 || rand.NextDouble() > weight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2 || surface >= 255)
                    {
                        continue;
                    }

                    int basinDepth = Math.Clamp((int)Math.Round(1 + weight * 3.0), 1, 4);
                    int floor = Math.Max(surface - basinDepth, 1);
                    for (int y = surface; y >= floor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    int supportY = Math.Max(1, floor - 1);
                    chunk.SetBlock(x, supportY, z, BlockType.Clay);
                    int waterTop = Math.Min(floor + 1, surface);
                    for (int y = floor; y <= waterTop; y++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Water);
                    }

                    if (surface + 1 < 256 && rand.NextDouble() < 0.35)
                    {
                        chunk.SetBlock(x, surface + 1, z, BlockType.TallGrass);
                    }

                    surfaceCache[x, z] = Math.Max(surfaceCache[x, z], waterTop);
                }
            }
        }

        private void AddFloodplainSwales(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold || intensity >= RiverBankThreshold)
                    {
                        continue;
                    }

                    double hydrology = hydrologyMask[x, z];
                    if (hydrology < 0.45)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    double wetness = Math.Clamp((hydrology - 0.4) * 0.9 + (RiverBankThreshold - intensity) * 2.2, 0.0, 1.0);
                    if (wetness <= 0.05)
                    {
                        continue;
                    }

                    int swaleDepth = Math.Clamp((int)Math.Round(1 + wetness * 3.5), 1, 4);
                    int floor = Math.Max(1, surface - swaleDepth);
                    for (int y = surface; y >= floor; y--)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }

                    surfaceCache[x, z] = floor;
                    if (wetness > 0.6)
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Water);
                        if (floor + 1 < 256)
                        {
                            chunk.SetBlock(x, floor + 1, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        chunk.SetBlock(x, floor, z, BlockType.Sand);
                    }

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 1 || nx >= 15 || nz < 1 || nz >= 15)
                            {
                                continue;
                            }

                            int neighborSurface = FindSurfaceLevel(chunk, nx, nz);
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            var rimType = wetness > 0.62 ? BlockType.Sand : BlockType.Grass;
                            chunk.SetBlock(nx, neighborSurface, nz, rimType);

                            if (wetness > 0.62 && neighborSurface + 1 < 256)
                            {
                                chunk.SetBlock(nx, neighborSurface + 1, nz, BlockType.Air);
                            }
                        }
                    }
                }
            }
        }

        private void AddRiverDeltaFans(
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            double[,] riverIntensity)
        {
            for (int x = 2; x < 14; x++)
            {
                for (int z = 2; z < 14; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold * 0.55 || intensity >= RiverBankThreshold * 1.05)
                    {
                        continue;
                    }

                    double accumulation = flowAccumulation[x, z];
                    if (accumulation < 2.2)
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double spawnWeight = Math.Clamp((accumulation - 2.0) * 0.18 + hydrology * 0.55, 0.0, 1.0);
                    spawnWeight *= 1.0 - Math.Clamp(
                        (intensity - RiverCenterThreshold) / (RiverBankThreshold - RiverCenterThreshold + 1e-5),
                        0.0,
                        1.0);
                    double selector = SampleDeterministicNoise(x, z, 257);
                    if (spawnWeight < 0.3 || selector > spawnWeight)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 2)
                    {
                        continue;
                    }

                    Vector2 flow = riverField.Flow[x, z];
                    if (flow.LengthSquared() < 1e-4f)
                    {
                        flow = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    }

                    if (flow.LengthSquared() < 1e-4f)
                    {
                        continue;
                    }

                    flow = Vector2.Normalize(flow);
                    Vector2 perpendicular = new(-flow.Y, flow.X);
                    bool braidRight = SampleDeterministicNoise(x + surface, z + surface, 311) > 0.5;
                    int fanReach = Math.Clamp((int)Math.Round(1 + accumulation * 0.35), 1, 4);

                    for (int step = 1; step <= fanReach; step++)
                    {
                        int targetX = Math.Clamp(x + (int)Math.Round(flow.X * step), 1, 14);
                        int targetZ = Math.Clamp(z + (int)Math.Round(flow.Y * step), 1, 14);

                        int columnSurface = surfaceCache[targetX, targetZ];
                        if (columnSurface <= 1)
                        {
                            columnSurface = FindSurfaceLevel(chunk, targetX, targetZ);
                            if (columnSurface <= 1)
                            {
                                continue;
                            }
                            surfaceCache[targetX, targetZ] = columnSurface;
                        }

                        int riverSurface = Math.Min(columnSurface, Math.Min(254, GlobalWaterLevel));
                        double falloff = Math.Clamp(1.0 - step / (fanReach + 1.0), 0.0, 1.0);
                        ShapeRiverBank(chunk, surfaceCache, targetX, targetZ, 0.45 + falloff * 0.4, riverSurface, allowFlood: true);

                        Vector2 offset = braidRight ? perpendicular : -perpendicular;
                        int barX = Math.Clamp(targetX + (int)Math.Round(offset.X), 0, 15);
                        int barZ = Math.Clamp(targetZ + (int)Math.Round(offset.Y), 0, 15);

                        int barSurface = surfaceCache[barX, barZ];
                        if (barSurface <= 1)
                        {
                            barSurface = FindSurfaceLevel(chunk, barX, barZ);
                            if (barSurface <= 1)
                            {
                                continue;
                            }
                            surfaceCache[barX, barZ] = barSurface;
                        }

                        chunk.SetBlock(barX, barSurface, barZ, BlockType.Sand);
                    }
                }
            }
        }

        private void ApplyRiverGradientSmoothing(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] riverIntensity)
        {
            var adjustments = new int[16, 16];
            bool hasAdjustment = false;

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity > RiverBankThreshold * 1.15)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    if (surface <= 0)
                    {
                        continue;
                    }

                    int neighborSum = 0;
                    int neighborCount = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nx >= 16 || nz < 0 || nz >= 16)
                            {
                                continue;
                            }

                            if (riverIntensity[nx, nz] > RiverBankThreshold * 1.2)
                            {
                                continue;
                            }

                            int neighborSurface = surfaceCache[nx, nz];
                            if (neighborSurface <= 0)
                            {
                                continue;
                            }

                            neighborSum += neighborSurface;
                            neighborCount++;
                        }
                    }

                    if (neighborCount < 3)
                    {
                        continue;
                    }

                    double averageSurface = neighborSum / (double)neighborCount;
                    double hydrologyBias = (hydrologyMask[x, z] - 0.5) * 2.0;
                    int targetSurface = (int)Math.Round(averageSurface - hydrologyBias);
                    int delta = targetSurface - surface;
                    if (Math.Abs(delta) <= 2)
                    {
                        continue;
                    }

                    adjustments[x, z] = Math.Clamp(delta, -4, 3);
                    hasAdjustment = true;
                }
            }

            if (!hasAdjustment)
            {
                return;
            }

            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    int delta = adjustments[x, z];
                    if (delta == 0)
                    {
                        continue;
                    }

                    int surface = surfaceCache[x, z];
                    int targetSurface = Math.Clamp(surface + delta, 2, 254);
                    if (delta < 0)
                    {
                        for (int y = surface; y > targetSurface && y >= 1; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        int waterTop = Math.Min(GlobalWaterLevel, Math.Min(255, surface));
                        for (int y = targetSurface; y <= waterTop && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Water);
                        }
                    }
                    else
                    {
                        for (int y = surface + 1; y <= targetSurface && y < 256; y++)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Sand);
                        }

                        if (targetSurface < GlobalWaterLevel)
                        {
                            for (int y = targetSurface + 1; y <= Math.Min(GlobalWaterLevel, surface) && y < 256; y++)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }
                    }

                    if (targetSurface + 1 < 256)
                    {
                        chunk.SetBlock(x, targetSurface + 1, z, BlockType.Air);
                    }

                    surfaceCache[x, z] = targetSurface;
                }
            }
        }

        private void ApplyRiverMeanderTerraces(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] riverIntensity,
            double[,] hydrologyMask,
            RiverFieldCache riverField)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double intensity = riverIntensity[x, z];
                    if (intensity >= RiverBankThreshold || intensity <= RiverCenterThreshold * 0.35)
                    {
                        continue;
                    }

                    var flowDir = riverField.Flow[x, z];
                    if (flowDir.LengthSquared() < 1e-5f)
                    {
                        continue;
                    }

                    flowDir = Vector2.Normalize(flowDir);
                    var perpendicular = new Vector2(-flowDir.Y, flowDir.X);
                    if (perpendicular.LengthSquared() < 1e-5f)
                    {
                        perpendicular = Vector2.UnitX;
                    }
                    perpendicular = Vector2.Normalize(perpendicular);

                    ResolvePerpendicularOffset(perpendicular, 1, out int posOffsetX, out int posOffsetZ);
                    ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), 1, out int negOffsetX, out int negOffsetZ);

                    if (!TrySampleField(riverIntensity, x + posOffsetX, z + posOffsetZ, out double posIntensity) ||
                        !TrySampleField(riverIntensity, x + negOffsetX, z + negOffsetZ, out double negIntensity))
                    {
                        continue;
                    }

                    bool posIsInner = posIntensity <= negIntensity;
                    var innerOffset = posIsInner ? (posOffsetX, posOffsetZ) : (negOffsetX, negOffsetZ);
                    var outerOffset = posIsInner ? (negOffsetX, negOffsetZ) : (posOffsetX, posOffsetZ);

                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int baseSurface))
                    {
                        continue;
                    }

                    if (!TryResolveSurface(chunk, surfaceCache, x + innerOffset.Item1, z + innerOffset.Item2, out int innerSurface))
                    {
                        continue;
                    }

                    int riverSurface = Math.Min(baseSurface, GlobalWaterLevel);
                    double normalized = Math.Clamp(1.0 - intensity / RiverCenterThreshold, 0.0, 1.0);
                    int shelfDepth = Math.Clamp((int)Math.Round(1 + normalized * 3.0), 1, 4);
                    int shelfFloor = Math.Max(riverSurface - shelfDepth, 1);

                    for (int y = innerSurface; y > shelfFloor; y--)
                    {
                        chunk.SetBlock(x + innerOffset.Item1, y, z + innerOffset.Item2, BlockType.Air);
                    }

                    double bankHydrology = Math.Clamp(
                        hydrologyMask[
                            Math.Clamp(x + innerOffset.Item1, 0, hydrologyMask.GetLength(0) - 1),
                            Math.Clamp(z + innerOffset.Item2, 0, hydrologyMask.GetLength(1) - 1)],
                        0.0,
                        1.0);
                    var shelfMaterial = bankHydrology > 0.62 ? BlockType.Clay : BlockType.Sand;
                    chunk.SetBlock(x + innerOffset.Item1, shelfFloor, z + innerOffset.Item2, shelfMaterial);

                    for (int y = shelfFloor + 1; y <= riverSurface && y < 256; y++)
                    {
                        chunk.SetBlock(x + innerOffset.Item1, y, z + innerOffset.Item2, BlockType.Water);
                    }

                    surfaceCache[x + innerOffset.Item1, z + innerOffset.Item2] = shelfFloor;

                    int outerX = x + outerOffset.Item1;
                    int outerZ = z + outerOffset.Item2;
                    if (!TryResolveSurface(chunk, surfaceCache, outerX, outerZ, out _))
                    {
                        continue;
                    }

                    double gradient = Math.Clamp(Math.Abs(posIntensity - negIntensity) * 38.0, 0.2, 0.85);
                    ShapeRiverBank(chunk, surfaceCache, outerX, outerZ, gradient * 0.5 + 0.2, riverSurface, allowFlood: false);
                }
            }
        }

        private void ApplyRiverHydrologyFeedback(
            ChunkData chunk,
            int[,] surfaceCache,
            RiverFieldCache riverField,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            double[,] riverIntensity)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int surface))
                    {
                        continue;
                    }

                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    if (hydrology < 0.45 && flow < 0.45)
                    {
                        continue;
                    }

                    double intensity = riverIntensity[x, z];
                    int riverSurface = Math.Min(surface, Math.Min(254, GlobalWaterLevel));

                    var flowDir = riverField.Flow[x, z];
                    if (flowDir.LengthSquared() < 1e-5f)
                    {
                        flowDir = ComputeTerrainSlopeDirection(surfaceCache, x, z);
                    }

                    if (flowDir.LengthSquared() < 1e-5f)
                    {
                        flowDir = Vector2.UnitX;
                    }

                    flowDir = Vector2.Normalize(flowDir);

                    if (intensity < RiverCenterThreshold * 0.95)
                    {
                        int infiltration = Math.Clamp((int)Math.Round(1 + (hydrology + flow) * 3.5), 1, 6);
                        int floor = Math.Max(riverSurface - infiltration, 1);
                        for (int y = surface; y >= floor && y >= 1; y--)
                        {
                            chunk.SetBlock(x, y, z, BlockType.Air);
                        }

                        if (hydrology > 0.62)
                        {
                            int fillTop = Math.Min(riverSurface, 254);
                            for (int y = floor; y <= fillTop && y < 256; y++)
                            {
                                chunk.SetBlock(x, y, z, BlockType.Water);
                            }
                        }

                        int supportY = Math.Max(floor - 1, 0);
                        chunk.SetBlock(x, supportY, z, BlockType.Sand);
                        surfaceCache[x, z] = floor;
                        continue;
                    }

                    if (intensity >= RiverBankThreshold)
                    {
                        chunk.SetBlock(x, surface, z, BlockType.Sand);
                        if (surface + 1 < 256)
                        {
                            chunk.SetBlock(x, surface + 1, z, BlockType.Grass);
                        }
                        continue;
                    }

                    double bankStrength = Math.Clamp((hydrology + flow) * 0.5, 0.0, 1.0);
                    var perpendicular = new Vector2(-flowDir.Y, flowDir.X);
                    if (perpendicular.LengthSquared() < 1e-5f)
                    {
                        perpendicular = Vector2.UnitY;
                    }

                    perpendicular = Vector2.Normalize(perpendicular);
                    ResolvePerpendicularOffset(perpendicular, 1, out int offsetX, out int offsetZ);
                    ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, bankStrength * 0.45 + 0.2, riverSurface, true);

                    ResolvePerpendicularOffset(new Vector2(-perpendicular.X, -perpendicular.Y), 1, out offsetX, out offsetZ);
                    ShapeRiverBank(chunk, surfaceCache, x + offsetX, z + offsetZ, bankStrength * 0.35 + 0.15, riverSurface, false);
                }
            }
        }

        private void AddRiverSeepageChannels(
            ChunkData chunk,
            int[,] surfaceCache,
            double[,] hydrologyMask,
            double[,] flowAccumulation,
            double[,] riverIntensity,
            RiverFieldCache riverField)
        {
            for (int x = 1; x < 15; x++)
            {
                for (int z = 1; z < 15; z++)
                {
                    double hydrology = Math.Clamp(hydrologyMask[x, z], 0.0, 1.0);
                    if (hydrology < 0.58)
                    {
                        continue;
                    }

                    var flowVector = riverField.Flow[x, z];
                    if (flowVector.LengthSquared() < 1e-5f)
                    {
                        continue;
                    }

                    double intensity = riverIntensity[x, z];
                    if (intensity <= RiverCenterThreshold || intensity >= RiverBankThreshold + 0.1)
                    {
                        continue;
                    }

                    if (!TryFindDownstreamRiverCell(riverIntensity, x, z, out int targetX, out int targetZ, out double targetIntensity))
                    {
                        continue;
                    }

                    if (targetIntensity >= intensity || targetIntensity > RiverCenterThreshold * 1.05)
                    {
                        continue;
                    }

                    if (!TryResolveSurface(chunk, surfaceCache, x, z, out int surface))
                    {
                        continue;
                    }

                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    int riverSurface = Math.Min(surface, GlobalWaterLevel);
                    int depth = Math.Clamp((int)Math.Round(1 + (hydrology + flow) * 2.5), 1, 4);
                    bool flood = hydrology > 0.7 || flow > 0.6;

                    CarveRiverSeepagePath(chunk, surfaceCache, x, z, targetX, targetZ, riverSurface, depth, flood);
                }
            }
        }

        private static bool TryFindDownstreamRiverCell(
            double[,] riverIntensity,
            int x,
            int z,
            out int targetX,
            out int targetZ,
            out double targetIntensity)
        {
            var offsets = new (int dx, int dz)[]
            {
                (1, 0),
                (-1, 0),
                (0, 1),
                (0, -1)
            };

            targetX = -1;
            targetZ = -1;
            targetIntensity = double.MaxValue;

            foreach (var (dx, dz) in offsets)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= riverIntensity.GetLength(0) || nz < 0 || nz >= riverIntensity.GetLength(1))
                {
                    continue;
                }

                double sample = riverIntensity[nx, nz];
                if (sample < targetIntensity)
                {
                    targetIntensity = sample;
                    targetX = nx;
                    targetZ = nz;
                }
            }

            return targetX >= 0;
        }

        private void CarveRiverSeepagePath(
            ChunkData chunk,
            int[,] surfaceCache,
            int startX,
            int startZ,
            int endX,
            int endZ,
            int riverSurface,
            int depth,
            bool flood)
        {
            int steps = Math.Max(Math.Abs(endX - startX), Math.Abs(endZ - startZ));
            steps = Math.Clamp(steps, 1, 4);
            double stepX = (endX - startX) / (double)steps;
            double stepZ = (endZ - startZ) / (double)steps;
            double cursorX = startX;
            double cursorZ = startZ;

            for (int i = 0; i <= steps; i++)
            {
                int cx = Math.Clamp((int)Math.Round(cursorX), 0, 15);
                int cz = Math.Clamp((int)Math.Round(cursorZ), 0, 15);

                if (!TryResolveSurface(chunk, surfaceCache, cx, cz, out int surface))
                {
                    cursorX += stepX;
                    cursorZ += stepZ;
                    continue;
                }

                int floor = Math.Max(surface - depth, 1);
                for (int y = surface; y > floor; y--)
                {
                    chunk.SetBlock(cx, y, cz, BlockType.Air);
                }

                if (flood)
                {
                    chunk.SetBlock(cx, floor, cz, BlockType.Clay);
                    for (int y = floor + 1; y <= riverSurface && y < 256; y++)
                    {
                        chunk.SetBlock(cx, y, cz, BlockType.Water);
                    }
                    surfaceCache[cx, cz] = Math.Min(riverSurface, 255);
                }
                else
                {
                    chunk.SetBlock(cx, floor, cz, BlockType.Sand);
                    if (floor + 1 < 256)
                    {
                        chunk.SetBlock(cx, floor + 1, cz, BlockType.Air);
                    }
                    surfaceCache[cx, cz] = floor;
                }

                cursorX += stepX;
                cursorZ += stepZ;
            }
        }

        private static bool TrySampleField(double[,] field, int x, int z, out double value)
        {
            if (x < 0 || x >= field.GetLength(0) || z < 0 || z >= field.GetLength(1))
            {
                value = 0;
                return false;
            }

            value = field[x, z];
            return true;
        }

        private void ShapeRiverBank(ChunkData chunk, int[,] surfaceCache, int x, int z, double falloff, int riverSurface, bool allowFlood)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
                return;

            int surface = surfaceCache[x, z];
            if (surface <= 0)
            {
                surface = FindSurfaceLevel(chunk, x, z);
                if (surface <= 0)
                    return;
                surfaceCache[x, z] = surface;
            }

            int maxDrop = Math.Max(1, (int)Math.Round(3.0 - 2.0 * falloff));

            if (surface > riverSurface + maxDrop)
            {
                int target = Math.Max(riverSurface + maxDrop, 1);
                for (int y = surface; y > target; y--)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
                surface = target;
                surfaceCache[x, z] = target;
            }

            if (allowFlood && surface <= riverSurface)
            {
                for (int y = Math.Max(surface, 1); y <= riverSurface && y < 256; y++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Water);
                }

                int updatedSurface = riverSurface < GlobalWaterLevel ? Math.Min(GlobalWaterLevel, 255) : Math.Min(riverSurface, 255);
                surfaceCache[x, z] = updatedSurface;
                surface = updatedSurface;
            }
            else
            {
                chunk.SetBlock(x, surface, z, BlockType.Sand);
            }

            if (surface + 1 < 256)
            {
                chunk.SetBlock(x, surface + 1, z, BlockType.Air);
            }
        }

        private bool IsOceanColumn(ChunkData chunk, int x, int z)
        {
            var biome = chunk.GetBiome(x, z);
            if (biome == BiomeType.Ocean)
                return true;

            var waterAtLevel = GlobalWaterLevel >= 0 && GlobalWaterLevel < 256 && chunk.GetBlock(x, GlobalWaterLevel, z) == BlockType.Water;
            var waterBelow = GlobalWaterLevel - 1 >= 0 && chunk.GetBlock(x, GlobalWaterLevel - 1, z) == BlockType.Water;
            return waterAtLevel && waterBelow;
        }

        
        /// <summary>
        /// 던전 타입 열거형
        /// </summary>
        private enum DungeonType
        {
            SimpleRoom,
            MultiRoom,
            Maze
        }
        
        /// <summary>
        /// 단순한 방 형태 던전
        /// </summary>
        private void GenerateSimpleDungeon(ChunkData chunk, Random rand)
        {
            int roomWidth = 6 + rand.Next(4);   // 6..9
            int roomHeight = 4 + rand.Next(2);  // 4..5
            int roomDepth = 6 + rand.Next(4);

            int ox = rand.Next(2, 16 - roomWidth - 2);
            int oy = rand.Next(15, 40); // 더 깊은 지하
            int oz = rand.Next(2, 16 - roomDepth - 2);

            BuildDungeonRoom(chunk, rand, ox, oy, oz, roomWidth, roomHeight, roomDepth);
            
            // 보물 상자 위치 (중앙)
            int treasureX = ox + roomWidth / 2;
            int treasureZ = oz + roomDepth / 2;
            // TODO: 보물 상자 블록 추가 시 사용
            // chunk.SetBlock(treasureX, oy + 1, treasureZ, BlockType.Chest);
        }
        
        /// <summary>
        /// 다중 방 던전
        /// </summary>
        private void GenerateMultiRoomDungeon(ChunkData chunk, Random rand)
        {
            int roomCount = 2 + rand.Next(3); // 2~4개 방
            
            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth = 5 + rand.Next(3);
                int roomHeight = 3 + rand.Next(2);
                int roomDepth = 5 + rand.Next(3);

                int ox = rand.Next(1, 16 - roomWidth - 1);
                int oy = rand.Next(15, 35);
                int oz = rand.Next(1, 16 - roomDepth - 1);
                
                BuildDungeonRoom(chunk, rand, ox, oy, oz, roomWidth, roomHeight, roomDepth);
                
                // 방들 사이에 복도 연결 (간단한 버전)
                if (i > 0)
                {
                    ConnectRooms(chunk, ox + roomWidth/2, oy + 1, oz + roomDepth/2, rand);
                }
            }
        }
        
        /// <summary>
        /// 미로 형태 던전
        /// </summary>
        private void GenerateMazeDungeon(ChunkData chunk, Random rand)
        {
            int startX = rand.Next(2, 6);
            int startZ = rand.Next(2, 6);
            int mazeY = rand.Next(20, 35);
            int mazeSize = 8; // 8x8 미로
            
            // 간단한 미로 생성 (더 복잡한 알고리즘으로 확장 가능)
            for (int x = 0; x < mazeSize; x++)
            {
                for (int z = 0; z < mazeSize; z++)
                {
                    int worldX = startX + x;
                    int worldZ = startZ + z;
                    
                    if (worldX < 16 && worldZ < 16)
                    {
                        // 체스판 패턴으로 벽과 통로 생성
                        if ((x + z) % 2 == 0 || rand.NextDouble() < 0.3)
                        {
                            // 통로
                            chunk.SetBlock(worldX, mazeY, worldZ, BlockType.Air);
                            chunk.SetBlock(worldX, mazeY + 1, worldZ, BlockType.Air);
                            chunk.SetBlock(worldX, mazeY + 2, worldZ, BlockType.Air);
                        }
                        else
                        {
                            // 벽
                            for (int y = 0; y < 4; y++)
                            {
                                chunk.SetBlock(worldX, mazeY + y, worldZ, BlockType.Cobblestone);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 던전 방 건설
        /// </summary>
        private void BuildDungeonRoom(ChunkData chunk, Random rand, int ox, int oy, int oz, int width, int height, int depth)
        {
            // 내부 비우기
            for (int x = ox + 1; x < ox + width - 1; x++)
            {
                for (int y = oy + 1; y < oy + height - 1; y++)
                {
                    for (int z = oz + 1; z < oz + depth - 1; z++)
                    {
                        chunk.SetBlock(x, y, z, BlockType.Air);
                    }
                }
            }

            // 벽, 바닥, 천장 건설
            for (int x = ox; x < ox + width; x++)
            {
                for (int y = oy; y < oy + height; y++)
                {
                    for (int z = oz; z < oz + depth; z++)
                    {
                        bool isWall = (x == ox || x == ox + width - 1 || 
                                      z == oz || z == oz + depth - 1 || 
                                      y == oy || y == oy + height - 1);
                        if (isWall)
                        {
                            // 다양한 재료 사용
                            BlockType wallMaterial = GetDungeonWallMaterial(rand);
                            chunk.SetBlock(x, y, z, wallMaterial);
                        }
                    }
                }
            }

            // 입구 생성 (더 자연스럽게)
            CreateDungeonEntrance(chunk, ox, oy, oz, width, depth);

            DecorateDungeonInterior(chunk, ox, oy, oz, width, height, depth, rand);
        }
        
        /// <summary>
        /// 던전 벽 재료 결정
        /// </summary>
        private BlockType GetDungeonWallMaterial(Random rand)
        {
            var materials = new[] { BlockType.Cobblestone, BlockType.Stone, BlockType.Stone };
            return materials[rand.Next(materials.Length)];
        }
        
        /// <summary>
        /// 던전 입구 생성
        /// </summary>
        private void CreateDungeonEntrance(ChunkData chunk, int ox, int oy, int oz, int width, int depth)
        {
            // 정면에 2x2 입구 생성
            for (int y = oy + 1; y < oy + 3; y++)
            {
                for (int x = ox + width/2 - 1; x <= ox + width/2; x++)
                {
                    chunk.SetBlock(x, y, oz, BlockType.Air);
                }
            }
        }

        private void DecorateDungeonInterior(ChunkData chunk, int ox, int oy, int oz, int width, int height, int depth, Random rand)
        {
            if (width > 4 && depth > 4)
            {
                var supports = new (int x, int z)[]
                {
                    (ox + 1, oz + 1),
                    (ox + width - 2, oz + 1),
                    (ox + 1, oz + depth - 2),
                    (ox + width - 2, oz + depth - 2)
                };

                int supportTop = Math.Max(oy + 1, Math.Min(oy + height - 2, 255));
                foreach (var (sx, sz) in supports)
                {
                    if (sx <= ox || sx >= ox + width - 1 || sz <= oz || sz >= oz + depth - 1)
                    {
                        continue;
                    }

                    for (int y = oy + 1; y <= supportTop; y++)
                    {
                        if (chunk.GetBlock(sx, y, sz) == BlockType.Air)
                        {
                            chunk.SetBlock(sx, y, sz, BlockType.Cobblestone);
                        }
                    }
                }
            }

            if (width > 6 && depth > 6 && rand.NextDouble() < 0.35)
            {
                int poolX = rand.Next(ox + 2, ox + width - 2);
                int poolZ = rand.Next(oz + 2, oz + depth - 2);
                var fluid = rand.NextDouble() < 0.55 ? BlockType.Water : BlockType.Lava;
                chunk.SetBlock(poolX, oy, poolZ, fluid);
            }

            if (width > 3 && depth > 3)
            {
                int treasureCount = rand.Next(1, 1 + Math.Max(1, (width * depth) / 24));
                for (int i = 0; i < treasureCount; i++)
                {
                    int lootX = rand.Next(ox + 1, ox + width - 1);
                    int lootZ = rand.Next(oz + 1, oz + depth - 1);

                    if (chunk.GetBlock(lootX, oy + 1, lootZ) == BlockType.Air)
                    {
                        chunk.SetBlock(lootX, oy, lootZ, BlockType.Cobblestone);
                        var lootBlock = rand.NextDouble() < 0.7 ? BlockType.GoldOre : BlockType.DiamondOre;
                        chunk.SetBlock(lootX, oy + 1, lootZ, lootBlock);
                    }
                }
            }
        }

        /// <summary>
        /// 방들을 복도로 연결
        /// </summary>
        private void ConnectRooms(ChunkData chunk, int x, int y, int z, Random rand)
        {
            // 간단한 직선 복도 (더 복잡한 연결 로직으로 확장 가능)
            int corridorLength = rand.Next(3, 8);
            int direction = rand.Next(4); // 0:북, 1:동, 2:남, 3:서
            
            int[] dx = {0, 1, 0, -1};
            int[] dz = {-1, 0, 1, 0};
            
            for (int i = 0; i < corridorLength; i++)
            {
                int newX = x + dx[direction] * i;
                int newZ = z + dz[direction] * i;
                
                if (newX >= 0 && newX < 16 && newZ >= 0 && newZ < 16)
                {
                    chunk.SetBlock(newX, y, newZ, BlockType.Air);
                    chunk.SetBlock(newX, y + 1, newZ, BlockType.Air);
                    chunk.SetBlock(newX, y + 2, newZ, BlockType.Air);
                }
            }
        }

        private int GenerateHeight(int worldX, int worldZ)
        {
            var profile = CalculateTerrainProfile(worldX, worldZ);
            var columnTop = profile.HasWater
                ? Math.Max(profile.SurfaceHeight, profile.WaterLevel)
                : profile.SurfaceHeight;
            return Math.Clamp(columnTop, 0, 255);
        }

        private BiomeType GenerateBiome(int worldX, int worldZ)
        {
            var profile = CalculateTerrainProfile(worldX, worldZ);
            return profile.Biome;
        }

        public BiomeType SampleBiome(int worldX, int worldZ)
        {
            return CalculateTerrainProfile(worldX, worldZ).Biome;
        }

        /// <summary>
        /// 개선된 광물 생성 시스템 - 더 현실적이고 균형 잡힌 분배
        /// </summary>
        public void GenerateOresInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltOre);

            // 각 광물별로 사실적인 깊이와 희귀성 설정
            GenerateOreType(chunk, rand, BlockType.CoalOre, 5, 50, 12, 6);      // 석탄: 언제나, 여러 층에서
            GenerateOreType(chunk, rand, BlockType.IronOre, 1, 40, 8, 4);       // 철: 중간 깊이
            GenerateOreType(chunk, rand, BlockType.GoldOre, 1, 25, 4, 3);       // 금: 깊은 곳
            GenerateOreType(chunk, rand, BlockType.DiamondOre, 1, 16, 2, 2);    // 다이아몬드: 가장 깊은 곳
        }
        
        /// <summary>
        /// 특정 광물 종류를 생성
        /// </summary>
        private void GenerateOreType(ChunkData chunk, Random rand, BlockType oreType, 
            int minY, int maxY, int maxVeins, int maxVeinSize)
        {
            int veinCount = rand.Next(1, maxVeins + 1);
            
            for (int vein = 0; vein < veinCount; vein++)
            {
                int centerX = rand.Next(16);
                int centerY = rand.Next(minY, maxY + 1);
                int centerZ = rand.Next(16);
                
                // 광맥 크기 결정
                int veinSize = rand.Next(1, maxVeinSize + 1);
                
                // 광맥 모양 생성 (구형이 아닌 불규칙한 형태)
                GenerateOreVein(chunk, rand, oreType, centerX, centerY, centerZ, veinSize);
            }
        }
        
        /// <summary>
        /// 광맥을 불규칙한 형태로 생성
        /// </summary>
        private void GenerateOreVein(ChunkData chunk, Random rand, BlockType oreType, 
            int centerX, int centerY, int centerZ, int size)
        {
            var oreBlocks = new List<(int x, int y, int z)>();
            
            // 시작점 추가
            oreBlocks.Add((centerX, centerY, centerZ));
            
            // 주변으로 확산
            for (int i = 0; i < size - 1; i++)
            {
                if (oreBlocks.Count == 0) break;
                
                // 기존 광물 블록 중 무작위로 하나 선택
                var baseBlock = oreBlocks[rand.Next(oreBlocks.Count)];
                
                // 6방향 중 무작위로 확산
                var directions = new (int dx, int dy, int dz)[] 
                {
                    (1, 0, 0), (-1, 0, 0), (0, 1, 0), 
                    (0, -1, 0), (0, 0, 1), (0, 0, -1)
                };
                
                var direction = directions[rand.Next(directions.Length)];
                int newX = baseBlock.x + direction.dx;
                int newY = baseBlock.y + direction.dy;
                int newZ = baseBlock.z + direction.dz;
                
                // 범위 체크 및 중복 방지
                if (newX >= 0 && newX < 16 && newY >= 0 && newY < 256 && newZ >= 0 && newZ < 16)
                {
                    if (!oreBlocks.Contains((newX, newY, newZ)))
                    {
                        oreBlocks.Add((newX, newY, newZ));
                    }
                }
            }
            
            // 실제로 광물 블록 배치
            foreach (var (x, y, z) in oreBlocks)
            {
                if (chunk.GetBlock(x, y, z) == BlockType.Stone)
                {
                    chunk.SetBlock(x, y, z, oreType);
                }
            }
        }

        public void GenerateVegetationInternal(TerrainGenerationContext context)
        {
            var chunk = context.Chunk;
            var rand = GetChunkRandom(context.ChunkX, context.ChunkZ, SaltVegetation);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var biome = chunk.GetBiome(x, z);
                    var surfaceY = FindSurfaceLevel(chunk, x, z);
                    var globalX = context.ChunkX * 16 + x;
                    var globalZ = context.ChunkZ * 16 + z;
                    var vegetationWarp = SimplexNoise.DomainWarp(globalX, globalZ, 0.0015, 0.0028, 6.0, 3.0, 9127);
                    double patchiness = NormalizeNoise(PerlinNoise.Generate(globalX + vegetationWarp.dx, globalZ + vegetationWarp.dz, 0.008, 2, 1.0, 0.5, 9127));
                    double density = GetVegetationDensity(biome) * (0.5 + patchiness * 0.5);

                    if (surfaceY > 0 && chunk.GetBlock(x, surfaceY, z) == BlockType.Grass)
                    {
                        if (rand.NextDouble() < density)
                        {
                            if (surfaceY + 1 < 256)
                            {
                                var vegType = GetVegetationType(biome, rand);
                                chunk.SetBlock(x, surfaceY + 1, z, vegType);
                                
                                if (vegType == BlockType.Wood && surfaceY + 5 < 256)
                                {
                                    for (int i = 1; i <= 4; i++)
                                        chunk.SetBlock(x, surfaceY + i, z, BlockType.Wood);
                                    
                                    for (int dx = -2; dx <= 2; dx++)
                                    {
                                        for (int dz = -2; dz <= 2; dz++)
                                        {
                                            if (x + dx >= 0 && x + dx < 16 && z + dz >= 0 && z + dz < 16)
                                            {
                                                for (int dy = 3; dy <= 5; dy++)
                                                {
                                                    if (surfaceY + dy < 256 && 
                                                        chunk.GetBlock(x + dx, surfaceY + dy, z + dz) == BlockType.Air)
                                                    {
                                                        chunk.SetBlock(x + dx, surfaceY + dy, z + dz, BlockType.Leaves);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private int FindSurfaceLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                if (chunk.GetBlock(x, y, z) != BlockType.Air)
                    return y;
            }
            return -1;
        }

        private double GetVegetationDensity(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => 0.8,
                BiomeType.Plains => 0.3,
                BiomeType.Hills => 0.25,
                BiomeType.Mountains => 0.1,
                BiomeType.Desert => 0.05,
                BiomeType.Tundra => 0.12,
                BiomeType.Cliffs => 0.02,
                BiomeType.Beach => 0.03,
                BiomeType.Ocean => 0.0,
                _ => 0.2
            };
        }

        private BlockType GetVegetationType(BiomeType biome, Random rand)
        {
            return biome switch
            {
                BiomeType.Forest => rand.NextDouble() < 0.3 ? BlockType.Wood : BlockType.TallGrass,
                BiomeType.Desert => BlockType.DeadBush,
                BiomeType.Beach => BlockType.DeadBush,
                BiomeType.Cliffs => BlockType.DeadBush,
                _ => BlockType.TallGrass
            };
        }

        private sealed class BaseTerrainStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public BaseTerrainStage(WorldManager owner) => _owner = owner;
            public string Name => "base-terrain";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateBaseTerrainInternal(context);
        }

        private sealed class OreGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public OreGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "ores";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateOresInternal(context);
        }

        private sealed class CaveGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public CaveGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "caves";
            public void Execute(TerrainGenerationContext context)
            {
                if (_owner._useImprovedCaves)
                {
                    _owner.GenerateImprovedCavesInternal(context);
                    return;
                }

                _owner.GenerateCavesInternal(context);
            }
        }

        private sealed class DungeonGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public DungeonGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "dungeons";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateDungeonsInternal(context);
        }

        private sealed class RiverGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public RiverGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "rivers";
            public void Execute(TerrainGenerationContext context)
            {
                if (_owner._useImprovedRivers)
                {
                    _owner.GenerateImprovedRiversInternal(context);
                    return;
                }

                _owner.GenerateRiversInternal(context);
            }
        }

        private sealed class LakeGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public LakeGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "lakes";
            public void Execute(TerrainGenerationContext context)
            {
                if (_owner._useImprovedLakes)
                {
                    _owner.GenerateImprovedLakesInternal(context);
                    return;
                }

                _owner.GenerateLakesInternal(context);
            }
        }

        private sealed class VegetationGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public VegetationGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "vegetation";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateVegetationInternal(context);
        }

        private sealed class CloudGenerationStage : ITerrainGenerationStage
        {
            private readonly WorldManager _owner;
            public CloudGenerationStage(WorldManager owner) => _owner = owner;
            public string Name => "clouds";
            public void Execute(TerrainGenerationContext context) => _owner.GenerateCloudsInternal(context);
        }

        private string GetChunkKey(int x, int z) => $"{x},{z}";
        
        private (int x, int z) ParseChunkKey(string key)
        {
            var parts = key.Split(',');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }

        // === 마인크래프트 핸들러용 추가 메서드들 ===

        /// <summary>
        /// 특정 블록 정보 가져오기
        /// </summary>
        public async Task<Models.BlockData?> GetBlockAsync(int x, int y, int z)
        {
            int chunkX = (int)Math.Floor(x / 16.0);
            int chunkZ = (int)Math.Floor(z / 16.0);
            int localX = x - chunkX * 16;
            int localZ = z - chunkZ * 16;

            var chunk = await GetChunkAsync(chunkX, chunkZ);
            if (chunk != null)
            {
                var blockType = chunk.GetBlock(localX, y, localZ);
                return new Models.BlockData(x, y, z, (int)blockType);
            }
            
            return null;
        }

        /// <summary>
        /// 블록 설정하기
        /// </summary>
        public async Task SetBlockAsync(Models.BlockData blockData)
        {
            int chunkX = (int)Math.Floor(blockData.X / 16.0);
            int chunkZ = (int)Math.Floor(blockData.Z / 16.0);
            int localX = blockData.X - chunkX * 16;
            int localZ = blockData.Z - chunkZ * 16;

            var chunk = await GetChunkAsync(chunkX, chunkZ);
            if (chunk != null)
            {
                chunk.SetBlock(localX, blockData.Y, localZ, (BlockType)blockData.BlockId);
                
                // 청크를 수정됨으로 표시
                var chunkKey = GetChunkKey(chunkX, chunkZ);
                if (_loadedChunks.TryGetValue(chunkKey, out var loadedChunk))
                {
                    loadedChunk.IsModified = true;
                }
            }
        }

        /// <summary>
        /// 블록 제거하기
        /// </summary>
        public async Task RemoveBlockAsync(int x, int y, int z)
        {
            var airBlock = new Models.BlockData(x, y, z, 0); // 0 = Air
            await SetBlockAsync(airBlock);
        }

        /// <summary>
        /// 청크 내 엔티티들 가져오기
        /// </summary>
        public async Task<List<Models.Entity>> GetEntitiesInChunk(int chunkX, int chunkZ)
        {
            // TODO: 실제 구현에서는 데이터베이스에서 엔티티 조회
            // 현재는 빈 리스트 반환
            return new List<Models.Entity>();
        }
    }

    public static class SimplexNoise
    {
        public static (double dx, double dz) DomainWarp(double x, double z, double simplexFrequency, double perlinFrequency, double simplexAmplitude, double perlinAmplitude, int seed)
        {
            double simplexOffsetX = Generate(x, z, simplexFrequency, 3, 1.0, 0.5, seed) * simplexAmplitude;
            double simplexOffsetZ = Generate(x + 37.0, z + 53.0, simplexFrequency, 3, 1.0, 0.5, seed ^ 0x5F5F5F5F) * simplexAmplitude;

            double perlinOffsetX = PerlinNoise.Generate(x, z, perlinFrequency, 2, 1.0, 0.55, seed ^ 0x00FF00FF) * perlinAmplitude;
            double perlinOffsetZ = PerlinNoise.Generate(x + 17.0, z + 23.0, perlinFrequency, 2, 1.0, 0.55, seed ^ 0x7F00EF00) * perlinAmplitude;

            return (simplexOffsetX + perlinOffsetX, simplexOffsetZ + perlinOffsetZ);
        }

        public static double Generate(double x, double y, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var random = new Random(seed);
            double total = 0;
            double maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += GenerateOctave(x * frequency, y * frequency, random) * amplitude;
                maxValue += amplitude;
                
                frequency *= 2;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        private static double GenerateOctave(double x, double y, Random random)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            
            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);
            
            double u = Fade(xf);
            double v = Fade(yf);
            
            var p = new int[512];
            for (int i = 0; i < 256; i++)
                p[i] = p[i + 256] = random.Next(256);
            
            int aa = p[p[xi] + yi];
            int ab = p[p[xi] + yi + 1];
            int ba = p[p[xi + 1] + yi];
            int bb = p[p[xi + 1] + yi + 1];
            
            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
            
            return Lerp(x1, x2, v);
        }
        
        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + t * (b - a);
        private static double Grad(int hash, double x, double y) => ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    public static class PerlinNoise
    {
        public static double Generate(double x, double y, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var random = new Random(seed);
            var permutation = BuildPermutation(random);

            double total = 0.0;
            double maxValue = 0.0;
            double currentFrequency = frequency;
            double currentAmplitude = amplitude;

            for (int i = 0; i < octaves; i++)
            {
                total += Perlin(permutation, x * currentFrequency, y * currentFrequency) * currentAmplitude;
                maxValue += currentAmplitude;
                currentFrequency *= 2.0;
                currentAmplitude *= persistence;
            }

            return maxValue == 0 ? 0 : total / maxValue;
        }

        private static double Perlin(int[] permutation, double x, double y)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            double u = Fade(xf);
            double v = Fade(yf);

            int aa = permutation[permutation[xi] + yi];
            int ab = permutation[permutation[xi] + yi + 1];
            int ba = permutation[permutation[xi + 1] + yi];
            int bb = permutation[permutation[xi + 1] + yi + 1];

            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        private static int[] BuildPermutation(Random random)
        {
            var baseArray = new int[256];
            for (int i = 0; i < 256; i++)
            {
                baseArray[i] = i;
            }

            for (int i = 255; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                var temp = baseArray[i];
                baseArray[i] = baseArray[swapIndex];
                baseArray[swapIndex] = temp;
            }

            var permutation = new int[512];
            for (int i = 0; i < 512; i++)
            {
                permutation[i] = baseArray[i & 255];
            }

            return permutation;
        }


        // ==================== Utility Methods ====================

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + t * (b - a);

        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 7;
            double u = h < 4 ? x : y;
            double v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
