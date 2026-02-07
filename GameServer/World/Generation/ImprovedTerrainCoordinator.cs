using System;
using GameServerApp;
using GameServerApp.World;
using GameServerApp.Utils;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Aggregates improved cave/river/lake mask generation using the data-driven world config.
    /// </summary>
    public sealed record TerrainMaskResult
    {
        public bool[,,]? Caves { get; init; }
        public float[,]? Rivers { get; init; }
        public float[,]? Lakes { get; init; }
        public float[,]? ErosionRisk { get; init; }
        public float[,] Hydrology { get; init; } = default!;
        public float[,] FlowAccumulation { get; init; } = default!;
    }

    public sealed class ImprovedTerrainCoordinator
    {
        private readonly WorldGenerationConfig config;
        private readonly int chunkSize;
        private readonly int worldHeight;
        private readonly int seaLevel;
        private readonly long worldSeed;
        private readonly ImprovedCaveGenerator caveGenerator;
        private readonly ImprovedRiverGenerator riverGenerator;
        private readonly ImprovedLakeGenerator lakeGenerator;

        public ImprovedTerrainCoordinator(WorldGenerationConfig config, WorldSettings worldSettings)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (worldSettings == null) throw new ArgumentNullException(nameof(worldSettings));

            chunkSize = Math.Max(1, config.ChunkSize);
            worldHeight = Math.Max(1, config.WorldHeight);
            seaLevel = Math.Clamp(
                config.TerrainGeneration.SeaLevel <= 0 ? config.Water.GlobalWaterLevel : config.TerrainGeneration.SeaLevel,
                4,
                worldHeight - 4);

            worldSeed = worldSettings.WorldSeed != 0 ? worldSettings.WorldSeed : config.Seed;
            caveGenerator = new ImprovedCaveGenerator(config.Caves, worldSeed);
            riverGenerator = new ImprovedRiverGenerator(config.Water, worldSeed);
            lakeGenerator = new ImprovedLakeGenerator(config.Lakes, config.Water, worldSeed);
        }

        public TerrainMaskResult GenerateMasks(int chunkX, int chunkZ, int[,] heightMap, int sizeOverride)
        {
            int size = Math.Min(Math.Max(1, sizeOverride), chunkSize);
            var hydrology = BuildHydrologyMask(heightMap, size);
            var flow = BuildFlowAccumulation(heightMap, hydrology, size);
            ApplyFlowMemory(heightMap, hydrology, flow);
            BlendHydrologyWithFlow(heightMap, hydrology, flow);
            ApplyCurvatureHydrologyGuide(heightMap, hydrology, flow);
            ApplyHydrologyContinuityEnvelope(heightMap, hydrology, flow);
            NormalizeHydrologyFlowEdges(hydrology, flow);
            DiffuseHydrologyEdges(hydrology, flow);
            ApplyWaterTableEnvelope(heightMap, hydrology, flow);
            ApplyHydrologyEdgeEnvelope(hydrology, flow);
            ApplyCrossChunkHydrologyStitch(hydrology, flow);
            ApplyHydrologyEdgeCohesion(heightMap, hydrology, flow);
            HarmonizeHydrologyWithSurface(heightMap, hydrology, flow);
            ApplyHydrologyReservoirSmoothing(heightMap, hydrology, flow);
            TerrainMaskUtility.BalanceHydrologyPressure(
                hydrology,
                flow,
                config.Water.HydrologyPressureBlend,
                config.Water.HydrologyPressureGradientClamp);
            ApplyHydrologyGradientCoupling(hydrology, flow);
            var erosionRisk = BuildErosionRiskField(heightMap, hydrology, flow, size);
            ApplyRiparianEdgeFeather(hydrology, flow, erosionRisk);
            ApplyErosionAwareDamping(hydrology, flow, erosionRisk);
            ApplyHydrologyMomentum(heightMap, hydrology, flow, erosionRisk);
            ApplyConfluenceMemoryField(heightMap, hydrology, flow, erosionRisk);
            ApplySubterraneanHydrologyShield(heightMap, hydrology, flow, erosionRisk);
            ApplyRiparianFlowBridge(heightMap, hydrology, flow, erosionRisk);

            float[,]? riverMask = config.Water.EnableRivers
                ? riverGenerator.BuildMask(chunkX, chunkZ, size, heightMap, hydrology, flow, erosionRisk, seaLevel)
                : null;

            float[,]? lakeMask = config.Water.EnableLakes
                ? lakeGenerator.BuildMask(chunkX, chunkZ, size, heightMap, hydrology, flow, riverMask, erosionRisk, seaLevel)
                : null;

            if (lakeMask != null)
            {
                ApplyLakeHydrologySeepage(heightMap, hydrology, flow, lakeMask, riverMask);
            }

            if (riverMask != null || lakeMask != null)
            {
                ApplyRiverLakeHydrologyFeedback(heightMap, hydrology, flow, riverMask, lakeMask, erosionRisk);
                ApplyAquiferSuppression(hydrology, flow, riverMask, lakeMask);
                ApplyRiparianCaveBuffer(erosionRisk, hydrology, flow, riverMask, lakeMask);
            }

            bool[,,]? caveMask = config.Caves.EnableCaves
                ? caveGenerator.BuildMask(chunkX, chunkZ, size, worldHeight, heightMap, hydrology, flow, riverMask, erosionRisk, seaLevel)
                : null;

            return new TerrainMaskResult
            {
                Caves = caveMask,
                Rivers = riverMask,
                Lakes = lakeMask,
                ErosionRisk = erosionRisk,
                Hydrology = hydrology,
                FlowAccumulation = flow
            };
        }

        private void ApplyLakeHydrologySeepage(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] lakeMask,
            float[,]? riverMask)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double seepageWeight = Math.Clamp(config.Lakes.FlowSeepageWeight, 0.0, 1.0);
            double inflowBlend = Math.Clamp(config.Water.LakeInflowBlendWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(config.Water.HydrologyEdgeVarianceClamp, 0.0, 1.0);
            double slopePenalty = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double continuity = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double edgeSeal = Math.Clamp(config.Water.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            double outflowStability = Math.Clamp(config.Lakes.OutflowStabilityWeight, 0.0, 1.0);
            double edgeLock = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double outflowTaper = Math.Clamp(config.Lakes.LakeOutflowTaper, 0.0, 1.0);
            double edgeTangentWeight = Math.Clamp(config.Water.HydrologyEdgeTangentWeight, 0.0, 1.0);
            double directionalBlend = Math.Clamp(config.Water.HydrologyDirectionalBlend, 0.0, 1.0);
            double riverContinuityWeight = Math.Clamp(config.Water.RiverEdgeContinuityWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double spillwayDepthBias = Math.Clamp((config.Lakes.OutflowCarveDepth + config.Lakes.ShelfDepth) / 24.0, 0.05, 0.55);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float lake = lakeMask[x, z];
                    if (lake <= 0.01f)
                    {
                        continue;
                    }

                    float river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0f;
                    double flowMemory = TerrainMaskUtility.SampleInterior(flowCopy, x, z) * (0.5 + inflowBlend * 0.35);
                    double hydroBase = hydroCopy[x, z];
                    double hydrologyGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydroCopy, x, z) - hydroBase);
                    double flowGradient = Math.Abs(TerrainMaskUtility.SampleInterior(flowCopy, x, z) - flowCopy[x, z]);
                    double infiltration = lake * (seepageWeight * 0.65 + inflowBlend * 0.35);
                    double slopeGuard = 1.0 - Math.Clamp(TerrainMaskUtility.ComputeSlope(heightMap, x, z) * slopePenalty / 18.0, 0.0, 0.6);
                    double riverGuard = 1.0 - river * 0.35;
                    double continuityBrake = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * continuity * 0.35, 0.0, 0.35);
                    double edgeSealBlend = 1.0 - Math.Clamp(lake * edgeSeal * 0.25, 0.0, 0.25);
                    double shorelineGuard = 1.0 - Math.Clamp(outflowStability * lake * 0.5, 0.0, 0.4);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    int tangentX = Math.Clamp(x - downhill.Z, 0, sizeX - 1);
                    int tangentZ = Math.Clamp(z + downhill.X, 0, sizeZ - 1);
                    double downHydro = hydroCopy[downX, downZ];
                    double downFlow = flowCopy[downX, downZ];
                    double tangentHydro = hydroCopy[tangentX, tangentZ];
                    double tangentFlow = flowCopy[tangentX, tangentZ];
                    double spillwayPressure =
                        Math.Max(0.0, hydroBase - downHydro) +
                        Math.Max(0.0, flowCopy[x, z] - downFlow) * 0.35;
                    double spillwayBlend = Math.Clamp(
                        lake * outflowTaper * (0.45 + riverContinuityWeight * 0.35) +
                        spillwayPressure * 0.2 +
                        spillwayDepthBias,
                        0.0,
                        1.25);
                    double directionalHydro = downHydro * (0.45 + outflowStability * 0.2) + tangentHydro * edgeTangentWeight * 0.15;
                    double directionalFlow = downFlow * (0.55 + directionalBlend * 0.25) + tangentFlow * edgeTangentWeight * 0.2;
                    double hydroTarget = hydroBase + infiltration * slopeGuard * riverGuard;
                    hydroTarget =
                        hydroTarget * continuityBrake * edgeSealBlend * shorelineGuard * (1.0 - spillwayBlend * 0.35) +
                        directionalHydro * spillwayBlend * 0.35 +
                        flowMemory * inflowBlend * 0.25;
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.25));

                    double flowTarget = flowCopy[x, z] * (1.0 - lake * 0.25);
                    flowTarget += hydrology[x, z] * (seepageWeight * 0.35 + inflowBlend * 0.2);
                    flowTarget += directionalFlow * spillwayBlend * (0.25 + riverContinuityWeight * 0.35);
                    flowTarget += spillwayPressure * flowPersistence * (0.08 + outflowTaper * 0.1);
                    flowTarget += flowMemory * edgeLock * 0.15;
                    flowTarget *= continuityBrake * shorelineGuard;
                    flowTarget *= 1.0 - Math.Clamp(spillwayDepthBias * lake * 0.18, 0.0, 0.15);
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(flowTarget + lake * 0.05, 0.0, 1.2));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend * 0.85, varianceClamp * 1.35);
        }

        private void ApplyAquiferSuppression(float[,] hydrology, float[,] flow, float[,]? riverMask, float[,]? lakeMask)
        {
            if (riverMask == null && lakeMask == null)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double riverSuppression = Math.Clamp(config.Caves.RiverSuppressionWeight, 0.0, 1.0);
            double moistureRetention = Math.Clamp(config.Caves.MoistureRetentionWeight, 0.0, 1.0);
            double flowMemoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double edgeLock = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double seepageWeight = Math.Clamp(config.Lakes.FlowSeepageWeight, 0.0, 1.0);
            double outflowStability = Math.Clamp(config.Lakes.OutflowStabilityWeight, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? TerrainMaskUtility.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = river * riverSuppression + lake * (seepageWeight * 0.5 + outflowStability * 0.35);
                    if (wetness <= 0.01)
                    {
                        continue;
                    }

                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double flowMemory = TerrainMaskUtility.SampleInterior(flowCopy, x, z) * flowMemoryWeight;
                    double damp = 1.0 - Math.Clamp(wetness * (moistureRetention * 0.5 + edgeLock * 0.35), 0.0, 0.85);
                    double sealedHydro = hydro * damp + wetness * moistureRetention * 0.25;
                    double sealedFlow = flowValue * (1.0 - wetness * 0.45) + flowMemory * (wetness * 0.35);

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(sealedHydro, 0.0, varianceClamp + 1.0));
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(sealedFlow, 0.0, 1.25));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend * 0.85, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend * 0.65, varianceClamp * 1.35);
        }

        private void ApplyHydrologyMomentum(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk)
        {
            double momentumWeight = Math.Clamp(config.Water.HydrologyFlowGain, 0.0, 1.0);
            double persistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, config.Water.HydrologyFlowDivergenceClamp);
            double erosionBrake = Math.Clamp(config.Water.RiverReliefPenaltyWeight, 0.0, 1.0);
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int dx = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int dz = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);

                    double baseHydro = hydroCopy[x, z];
                    double baseFlow = flowCopy[x, z];
                    double downhillHydro = hydroCopy[dx, dz];
                    double downhillFlow = flowCopy[dx, dz];
                    double pressure = baseHydro + baseFlow * 0.25;
                    double downhillPressure = downhillHydro + downhillFlow * 0.25;
                    double gradient = Math.Abs(downhillPressure - pressure);
                    double divergence = Math.Min(1.0, gradient / divergenceClamp);
                    double erosion = erosionRisk[x, z] * erosionBrake;
                    double momentum = (downhillPressure - pressure) * momentumWeight;
                    double blendedHydro = baseHydro * (1.0 - momentumWeight) + downhillHydro * momentumWeight + momentum * 0.25;
                    blendedHydro = blendedHydro * (1.0 - erosion * 0.25) + baseHydro * erosion * 0.25;
                    double blendedFlow = baseFlow * (1.0 - persistence) + (downhillFlow + momentum) * persistence;
                    blendedFlow *= 1.0 - divergence * 0.35;
                    blendedFlow = Math.Clamp(blendedFlow, 0.0, Math.Max(1.35, baseFlow + Math.Abs(momentum) * 0.5));

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(blendedHydro, 0.0, 1.25));
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)blendedFlow);
                }
            }
        }

        private void ApplyConfluenceMemoryField(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk)
        {
            double confluenceBoost = Math.Clamp(config.Water.RiverConfluenceBoost, 0.0, 2.0);
            if (confluenceBoost <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double flowMemory = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double continuityWeight = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double divergenceClamp = Math.Max(0.0001, config.Water.HydrologyFlowDivergenceClamp);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int dx = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int dz = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double baseHydro = hydroCopy[x, z];
                    double baseFlow = flowCopy[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    double downFlow = flowCopy[dx, dz];
                    double downHydro = hydroCopy[dx, dz];
                    double flowSeed = Math.Max(baseFlow, Math.Max(seamFlow, downFlow));
                    if (flowSeed <= 0.01)
                    {
                        continue;
                    }

                    double hydroGradient = Math.Abs(seamHydro - baseHydro);
                    double flowGradient = Math.Abs(seamFlow - baseFlow);
                    double divergence = Math.Min(1.0, Math.Abs(downFlow - baseFlow) / divergenceClamp);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double memory = flowSeed * (0.4 + flowMemory * 0.35 + continuityWeight * 0.25);
                    double stabilizer = 1.0 - Math.Clamp(divergence * 0.35 + erosion * 0.25, 0.0, 0.65);
                    double continuityBrake = 1.0 - Math.Clamp((hydroGradient + flowGradient) * continuityWeight * 0.35, 0.0, 0.35);
                    double confluence = memory * confluenceBoost * (0.12 + edgeFalloff * 0.08);
                    double hydroTarget = baseHydro + confluence * stabilizer * continuityBrake + downHydro * 0.05;
                    double flowTarget = baseFlow * (1.0 - erosion * 0.2) +
                        (confluence + downFlow * 0.1 + seamFlow * 0.08) * flowPersistence * stabilizer;

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(hydroTarget, 0.0, varianceClamp + 1.0));
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(flowTarget, 0.0, Math.Max(1.35, baseFlow + confluence * 0.5)));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend * 0.7, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, config.Water.HydrologyEdgeNormalizationBlend * 0.55, varianceClamp * 1.2);
        }

        private void ApplySubterraneanHydrologyShield(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk)
        {
            double sealStrength = Math.Clamp(config.Caves.EdgeSealStrength, 0.0, 1.0);
            double moistureRetention = Math.Clamp(config.Caves.MoistureRetentionWeight, 0.0, 1.0);
            double flowMemory = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double slopePenalty = Math.Max(0.001, config.Water.HydrologySlopePenalty);
            double entranceDampening = Math.Clamp(config.Caves.CaveEntranceFlowDampening, 0.0, 1.0);
            double ceilingMoistureClamp = Math.Clamp(config.Caves.CeilingMoistureClamp, 0.0, 1.0);
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z));
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f);
                    double seal = Math.Clamp(sealStrength * (0.25 + slope / (slopePenalty * 8.0) + curvature * 0.12), 0.0, 0.65);
                    double retention = 1.0 - Math.Clamp(erosion * moistureRetention * 0.5, 0.0, 0.55);
                    double entranceGuard = 1.0 - Math.Clamp(flowCopy[x, z] * entranceDampening * 0.25, 0.0, 0.35);
                    double aquiferGuard = 1.0 - Math.Clamp(hydroCopy[x, z] * ceilingMoistureClamp * 0.2, 0.0, 0.25);
                    double hydroTarget = hydroCopy[x, z] * (1.0 - seal) + flowCopy[x, z] * flowMemory * 0.25;
                    hydroTarget = Math.Clamp(hydroTarget * retention * aquiferGuard + hydroCopy[x, z] * (1.0 - aquiferGuard) * 0.15, 0.0, 1.3);

                    double flowTarget = flowCopy[x, z] * (1.0 - seal * 0.35) + hydroCopy[x, z] * 0.15;
                    flowTarget *= (1.0 - erosion * 0.25) * entranceGuard;
                    flowTarget = Math.Clamp(flowTarget, 0.0, 1.1);

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)hydroTarget);
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)flowTarget);
                }
            }
        }

        private void ApplyRiparianFlowBridge(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            double continuity = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double flowLock = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double flowBias = Math.Clamp(config.Water.HydrologyEdgeFlowBias, 0.0, 1.0);
            double tangentWeight = Math.Clamp(config.Water.HydrologyEdgeTangentWeight, 0.0, 1.5);
            double directionalBlend = Math.Clamp(config.Water.HydrologyDirectionalBlend, 0.0, 1.0);
            double edgeBlend = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double erosionBrake = Math.Clamp(config.Water.RiverReliefPenaltyWeight, 0.0, 1.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double downhillHydro = hydroCopy[downX, downZ];
                    double downhillFlow = flowCopy[downX, downZ];
                    double gradient = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double erosion = Math.Clamp(erosionRisk[x, z], 0.0f, 1.0f) * erosionBrake;
                    double corridorHydro = (hydro + seamHydro + downhillHydro) / 3.0;
                    double corridorFlow = (flowValue + seamFlow + downhillFlow) / 3.0;
                    double tangent = (Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * 0.5;
                    double tangentAssist = 1.0 + tangent * tangentWeight * 0.1;
                    double edgeFalloff = 1.0 - Math.Clamp(Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z)) / (double)(edgeRadius + 1), 0.0, 1.0);
                    double bridge = Math.Clamp(continuity * 0.35 + flowLock * 0.25 + edgeBlend * edgeFalloff * 0.35, 0.08, 0.85);
                    double erosionDamp = 1.0 - erosion * 0.35;
                    double gradientBrake = 1.0 - Math.Clamp(gradient * config.Water.HydrologyGradientWeight * 0.05, 0.0, 0.35);

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(
                        hydro * (1.0 - bridge) + corridorHydro * bridge * tangentAssist * gradientBrake,
                        0.0,
                        varianceClamp + 1.0));

                    double directional = 1.0 + tangent * directionalBlend * 0.25;
                    double edgeBias = 1.0 + edgeFalloff * flowBias * 0.25;
                    double flowTarget = flowValue * (1.0 - bridge) + corridorFlow * bridge * directional * edgeBias;
                    flowTarget = flowTarget * erosionDamp + flowValue * (1.0 - erosionDamp);
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(
                        flowTarget,
                        0.0,
                        Math.Max(1.5, flowValue + corridorFlow * 0.5)));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, edgeBlend * 0.75, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, Math.Max(0.05, edgeBlend * 0.55), varianceClamp * 1.35);
        }

        private void ApplyWaterTableEnvelope(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double clampRange = Math.Max(1.0, config.Water.HydrologyWaterTableClampRange + 6.0);
            double envelopeWeight = Math.Clamp(config.Water.HydrologyWaterTableClampWeight + 0.08, 0.0, 1.0);
            double seamBlend = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int surface = heightMap[x, z];
                    double waterBias = 1.0 - Math.Clamp(Math.Abs(surface - seaLevel) / clampRange, 0.0, 1.0);
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double seamWeight = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double blend = envelopeWeight * (0.6 * waterBias + 0.4 * seamWeight);
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    double hydro = hydrology[x, z];
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double stability = 1.0 - Math.Clamp(Math.Abs(surface - seaLevel) / (clampRange * 1.25), 0.0, 0.65);

                    double targetHydro = hydro * (1.0 - blend) + (hydro + neighbourHydro * (1.0 + seamWeight * seamBlend)) * 0.5 * blend;
                    targetHydro *= 1.0 + waterBias * 0.12;
                    targetHydro *= stability;
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(targetHydro, 0.0, varianceClamp + 0.75));

                    double flowValue = flow[x, z];
                    double targetFlow = flowValue * (1.0 + waterBias * 0.1) + neighbourFlow * (0.15 + seamWeight * seamBlend * 0.25);
                    double flowBlend = Math.Clamp(blend + seamBlend * 0.15, 0.0, 1.0);
                    double blendedFlow = flowValue + (targetFlow - flowValue) * flowBlend;
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(blendedFlow, 0.0, flowClamp + 2.0));
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, seamBlend * 0.85, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, seamBlend * 0.65, varianceClamp * 1.25);
        }

        private void ApplyRiverLakeHydrologyFeedback(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            float[,]? riverMask,
            float[,]? lakeMask,
            float[,] erosionRisk)
        {
            if (riverMask == null && lakeMask == null)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();
            double edgeLock = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight, 0.0, 1.0);
            double tangentWeight = Math.Clamp(config.Water.HydrologyEdgeTangentWeight, 0.0, 1.0);
            double anisotropy = Math.Clamp(config.Water.RiverAnisotropyWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double gradientPenalty = Math.Clamp(config.Water.RiverGradientPenalty, 0.0, 1.5);
            double reliefPenalty = Math.Clamp(config.Water.RiverReliefPenaltyWeight, 0.0, 1.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? TerrainMaskUtility.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = river * 0.65 + lake * 0.55;
                    if (wetness < 0.01 && erosionRisk[x, z] < 0.01f)
                    {
                        continue;
                    }

                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double slopeGuard = 1.0 - Math.Clamp(slope * gradientPenalty / 64.0, 0.0, 0.55);
                    double erosionGuard = 1.0 - Math.Clamp(erosionRisk[x, z] * reliefPenalty, 0.0, 0.45);
                    double baseHydro = hydroCopy[x, z];
                    double baseFlow = flowCopy[x, z];
                    double lockedHydro = baseHydro * (1.0 - edgeLock) + wetness * edgeLock;
                    double tangentialBoost = (river + lake) * tangentWeight * 0.25;
                    double flowTarget = baseFlow * (1.0 - wetness * 0.35) + wetness * (flowPersistence * 0.35 + anisotropy * 0.25 + tangentialBoost);
                    double hydroTarget = lockedHydro * slopeGuard * erosionGuard + flowTarget * 0.1;

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.35));
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(flowTarget, 0.0, 1.2));
                }
            }
        }

        private void ApplyRiparianCaveBuffer(float[,] erosionRisk, float[,] hydrology, float[,] flow, float[,]? riverMask, float[,]? lakeMask)
        {
            if (riverMask == null && lakeMask == null)
            {
                return;
            }

            int sizeX = erosionRisk.GetLength(0);
            int sizeZ = erosionRisk.GetLength(1);
            var copy = (float[,])erosionRisk.Clone();
            double riverSuppression = Math.Clamp(config.Caves.RiverSuppressionWeight, 0.0, 1.0);
            double rimErosion = Math.Clamp(config.Water.LakeRimErosionWeight, 0.0, 1.0);
            double guardWeight = Math.Clamp(config.Caves.RiparianCaveGuardWeight, 0.0, 1.0);
            int bufferRadius = Math.Max(1, config.Water.RiparianBufferRadius + config.Caves.RiparianPlugDepth);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double river = riverMask != null ? TerrainMaskUtility.Clamp01(riverMask[x, z]) : 0.0;
                    double lake = lakeMask != null ? TerrainMaskUtility.Clamp01(lakeMask[x, z]) : 0.0;
                    double wetness = Math.Max(Math.Max(river, lake), Math.Max(hydrology[x, z], flow[x, z]));
                    if (wetness <= 0.01)
                    {
                        continue;
                    }

                    double variance = TerrainMaskUtility.SampleVariance(copy, x, z, bufferRadius);
                    double hydrologyGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrology, x, z) - hydrology[x, z]);
                    double flowGradient = Math.Abs(TerrainMaskUtility.SampleInterior(flow, x, z) - flow[x, z]);
                    double moistureGuard = Math.Clamp(wetness + hydrologyGradient * 0.5 + flowGradient * 0.35, 0.0, 2.0) * guardWeight;
                    double wetBuffer = wetness * (riverSuppression * 0.65 + rimErosion * 0.25) + moistureGuard;
                    double stability = 1.0 + variance * 0.2;
                    erosionRisk[x, z] = TerrainMaskUtility.Clamp01((float)Math.Min(1.0, copy[x, z] + wetBuffer * stability));
                }
            }

            TerrainMaskUtility.Smooth2D(
                erosionRisk,
                Math.Max(bufferRadius, config.Caves.StabilitySmoothIterations),
                Math.Clamp(config.Caves.StabilitySmoothBlend * 0.35 + guardWeight * 0.15, 0.0, 1.0));
            TerrainMaskUtility.NormalizeEdges(
                erosionRisk,
                bufferRadius,
                Math.Max(1, config.Caves.StabilitySmoothIterations / 2),
                Math.Clamp(config.Caves.StabilitySmoothBlend * 0.25, 0.0, 1.0));
        }

        private float[,] BuildHydrologyMask(int[,] heightMap, int size)
        {
            var hydrology = new float[size, size];
            double clampRange = Math.Max(1, config.Water.HydrologyWaterTableClampRange);
            double clampWeight = Math.Clamp(config.Water.HydrologyWaterTableClampWeight, 0.0, 1.0);
            double slopeWeight = Math.Clamp(config.Water.HydrologyWaterTableSlopeWeight, 0.0, 1.0);
            double slopePenaltyWeight = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double curvatureWeight = Math.Clamp(config.Water.HydrologyCurvatureWeight, 0.0, 1.5);
            double varianceClamp = Math.Clamp(config.Water.HydrologyVarianceClamp, 0.0, 2.0);
            double shorePush = Math.Max(0.1, config.Water.HydrologyShorePush);

            double varianceBlend = Math.Clamp(config.Water.HydrologyVarianceBlend, 0.0, 1.0);

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    int surface = heightMap[x, z];
                    double distance = Math.Max(0, surface - seaLevel);
                    double waterBias = 1.0 - Math.Clamp(distance / clampRange, 0.0, 1.0);
                    double shoreBoost = Math.Exp(-distance / shorePush);
                    double slopePenalty = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double stability = 1.0 - Math.Clamp(slopePenalty * (slopeWeight + slopePenaltyWeight * 0.1) / 6.0, 0.0, 0.7);
                    double gradientDamp = 1.0 - Math.Clamp(slopePenalty * gradientWeight / Math.Max(1.0, config.Water.HydrologyGradientClamp * 8.0), 0.0, 0.35);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * curvatureWeight * 0.08;
                    double warpedNoise = SimplexNoise.Generate(
                        (x + 17) * config.Water.HydrologyWarpFrequency,
                        (z + 31) * config.Water.HydrologyWarpFrequency,
                        1.0,
                        2,
                        config.Water.HydrologyWarpAmplitude * 0.15,
                        0.6,
                        (int)(worldSeed ^ 0x6611));
                    double baseline = Math.Clamp(waterBias * clampWeight * stability * gradientDamp, 0.0, 1.0);
                    baseline = Math.Clamp(baseline + warpedNoise * 0.05 + shoreBoost * 0.05 - curvature, 0.0, 1.2);
                    hydrology[x, z] = (float)baseline;
                }
            }

            if (varianceBlend > 0.0)
            {
                TerrainMaskUtility.BlendInterior(hydrology, varianceBlend);
            }

            TerrainMaskUtility.Smooth2D(hydrology, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            TerrainMaskUtility.DirectionalSmooth(heightMap, hydrology, config.Water.HydrologyDirectionalIterations, config.Water.HydrologyDirectionalBlend);
            TerrainMaskUtility.ApplyRiparianBuffer(hydrology, config.Water.RiparianBufferRadius, config.Water.RiparianSaturationBoost);
            TerrainMaskUtility.StabilizeEdges(
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeStabilityIterations,
                config.Water.HydrologyEdgeStabilityWeight,
                config.Water.HydrologyEdgeFluxBlend);
            TerrainMaskUtility.ApplyEdgeFlowLocks(
                heightMap,
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeFlowLockWeight,
                config.Water.HydrologyEdgeFlowBias,
                config.Water.HydrologyEdgeTangentWeight);
            TerrainMaskUtility.ApplyGradientStability(
                hydrology,
                config.Water.HydrologyGradientStabilityIterations,
                config.Water.HydrologyGradientStabilityBlend,
                config.Water.HydrologyGradientClamp);
            TerrainMaskUtility.FillBasins(
                hydrology,
                Math.Max(0.05, config.Water.HydrologyEdgeStabilityWeight * 0.5),
                Math.Max(1, config.Water.HydrologySeamRelaxIterations));
            TerrainMaskUtility.StitchEdges(hydrology, config.Water.HydrologySeamRelaxBlend * 0.65);
            TerrainMaskUtility.ClampVariance(hydrology, varianceClamp);
            TerrainMaskUtility.RelaxEdges(hydrology, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            TerrainMaskUtility.NormalizeEdgeBands(
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                Math.Max(0.05, config.Water.HydrologySeamRelaxBlend * 0.5),
                config.Water.HydrologyEdgeVarianceClamp);
            TerrainMaskUtility.NormalizeEdges(
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeNormalizationIterations,
                config.Water.HydrologyEdgeNormalizationBlend);
            return hydrology;
        }

        private float[,] BuildFlowAccumulation(int[,] heightMap, float[,] hydrology, int size)
        {
            var flow = new float[size, size];
            double persistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double divergenceClamp = Math.Clamp(config.Water.HydrologyFlowDivergenceClamp, 0.1, 1.5);

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    double accumulation = 0.0;
                    double current = heightMap[x, z];
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    double gradientMagnitude = Math.Sqrt(downhill.X * downhill.X + downhill.Z * downhill.Z);

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= size || nz >= size) continue;

                            double neighbor = heightMap[nx, nz];
                            if (neighbor < current)
                            {
                                accumulation += (current - neighbor) * 0.25;
                            }
                        }
                    }

                    double hydrologyBoost = hydrology[x, z] * config.Water.HydrologyFlowGain;
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * config.Water.HydrologyCurvatureWeight * 0.1;
                    double continuity = 1.0 + hydrology[x, z] * config.Water.HydrologyContinuityWeight;
                    double scaled = ((accumulation * (1.0 - persistence)) + hydrologyBoost) * continuity;
                    double meanderNoise = Math.Abs(SimplexNoise.Generate(
                        (x + 17 + worldSeed % 997) * config.Water.HydrologyWarpFrequency * 12.0,
                        (z - 31 + worldSeed % 883) * config.Water.HydrologyWarpFrequency * 12.0,
                        1.0,
                        2,
                        Math.Max(0.1, config.Water.HydrologyWarpAmplitude * 0.02),
                        0.55,
                        (int)(worldSeed ^ 0x5FFF)));
                    scaled *= 1.0 + meanderNoise * Math.Clamp(config.Water.HydrologyVarianceBlend, 0.0, 1.0) * 0.15;
                    scaled *= 1.0 - Math.Clamp(curvature, 0.0, 0.6);
                    scaled *= 1.0 - Math.Clamp(gradientMagnitude * config.Water.HydrologyGradientSlopeWeight * 0.05, 0.0, 0.35);
                    double clampMax = Math.Max(2.5, divergenceClamp * 12.0);
                    flow[x, z] = (float)Math.Clamp(scaled, 0.0, clampMax);
                }
            }

            TerrainMaskUtility.Smooth2D(flow, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            TerrainMaskUtility.DirectionalSmooth(heightMap, flow, config.Water.HydrologyDirectionalIterations, config.Water.HydrologyDirectionalBlend);
            TerrainMaskUtility.StabilizeEdges(
                flow,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeStabilityIterations,
                config.Water.HydrologyEdgeStabilityWeight,
                config.Water.HydrologyEdgeFluxBlend);
            TerrainMaskUtility.ApplyGradientStability(
                flow,
                config.Water.HydrologyGradientStabilityIterations,
                config.Water.HydrologyGradientStabilityBlend,
                config.Water.HydrologyGradientClamp);
            TerrainMaskUtility.ApplyEdgeFlowLocks(
                heightMap,
                flow,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeFlowLockWeight,
                config.Water.HydrologyEdgeFlowBias,
                config.Water.HydrologyEdgeTangentWeight);
            TerrainMaskUtility.FillBasins(
                flow,
                Math.Max(0.05, config.Water.HydrologyEdgeStabilityWeight * 0.35),
                Math.Max(1, config.Water.HydrologySeamRelaxIterations));
            TerrainMaskUtility.StitchEdges(flow, config.Water.HydrologySeamRelaxBlend * 0.65);
            TerrainMaskUtility.RelaxEdges(flow, config.Water.HydrologySeamRelaxIterations, config.Water.HydrologySeamRelaxBlend);
            TerrainMaskUtility.NormalizeEdgeBands(
                flow,
                config.Water.HydrologyEdgeBlendRadius,
                Math.Max(0.05, config.Water.HydrologySeamRelaxBlend * 0.5),
                config.Water.HydrologyEdgeVarianceClamp);
            TerrainMaskUtility.NormalizeEdges(
                flow,
                config.Water.HydrologyEdgeBlendRadius,
                config.Water.HydrologyEdgeNormalizationIterations,
                config.Water.HydrologyEdgeNormalizationBlend);
            return flow;
        }

        private void ApplyFlowMemory(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = flow.GetLength(0);
            int sizeZ = flow.GetLength(1);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double watershedBlend = Math.Clamp(config.Water.HydrologyWatershedStitchWeight, 0.0, 1.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            int watershedRadius = Math.Max(1, config.Water.HydrologyWatershedStitchRadius);
            if (memoryWeight <= 0.0 && watershedBlend <= 0.0 && flowShadowWeight <= 0.0)
            {
                return;
            }

            var buffer = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float flowValue = flow[x, z];
                    float hydro = hydrology[x, z];
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    float downhillFlow = flow[downX, downZ];
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);

                    double continuity = 1.0 + hydro * config.Water.HydrologyContinuityWeight + neighbourHydro * 0.25;
                    double memory = flowValue * (1.0 - memoryWeight);
                    memory += (downhillFlow + flowValue) * (memoryWeight * 0.25);
                    memory += neighbourFlow * (memoryWeight * 0.35);
                    memory += hydro * memoryWeight * 0.25;
                    memory *= continuity;
                    memory *= 1.0 - Math.Clamp(hydrologyGradient * flowShadowWeight * 0.25, 0.0, 0.3);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(watershedRadius + 1), 0.0, 1.0);
                    double edgeRepair = watershedBlend * edgeFalloff;
                    if (edgeRepair > 0.0)
                    {
                        double seamAnchor = neighbourHydro * 0.35 + hydro * 0.35 + neighbourFlow * 0.3;
                        memory = memory * (1.0 - edgeRepair * 0.55) + seamAnchor * edgeRepair;
                    }

                    buffer[x, z] = (float)Math.Clamp(
                        memory,
                        0.0,
                        Math.Max(flowValue + 1.5, config.Water.HydrologyFlowDivergenceClamp * 12.0));
                }
            }

            Array.Copy(buffer, flow, buffer.Length);
        }

        private void BlendHydrologyWithFlow(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double flowBlend = Math.Clamp(config.Water.HydrologyContinuityWeight * 0.35, 0.05, 0.45);
            double edgeBlend = Math.Clamp(config.Water.HydrologyEdgeFlowLockWeight * 0.5, 0.0, 0.45);
            int edgeRadius = Math.Max(
                1,
                Math.Max(config.Water.HydrologyEdgeBlendRadius, config.Water.HydrologyWatershedStitchRadius));
            int watershedRadius = Math.Max(1, config.Water.HydrologyWatershedStitchRadius);
            double confluenceBoost = Math.Clamp(config.Water.RiverConfluenceBoost, 0.0, 2.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.Water.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double directionalBias = Math.Clamp(config.Water.HydrologyDirectionalBlend * 0.5, 0.0, 0.5);
            double watershedBlend = Math.Clamp(config.Water.HydrologyWatershedStitchWeight, 0.0, 1.0);

            var buffer = (float[,])hydrology.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    double normalizedFlow = Math.Clamp(flowValue / Math.Max(1.0, config.Water.RiverDepth), 0.0, 1.0);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z) / Math.Max(1.0, config.Water.RiverDepth);
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);

                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFalloff = Math.Clamp(1.0 - edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double edgeFactor = edgeBlend * edgeFalloff + watershedBlend * edgeFalloff * 0.5;
                    double blend = Math.Clamp(flowBlend + edgeFactor, 0.0, 0.9);

                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double directionalHydro = hydrology[downX, downZ];
                    double directionalFlow = Math.Clamp(flow[downX, downZ] / Math.Max(1.0, config.Water.RiverDepth), 0.0, 1.0);
                    double directionalWeight = Math.Clamp((Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * directionalBias + directionalFlow * 0.2, 0.0, 0.45);

                    double confluence = confluenceBoost > 0.0
                        ? (neighbourFlow * 0.5 + neighbourHydro * 0.25 + hydrologyGradient * 0.15) * confluenceBoost
                        : 0.0;

                    double flowShadow = Math.Clamp(
                        (normalizedFlow + neighbourFlow) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5 +
                        directionalFlow * flowShadowWeight * 0.15,
                        0.0,
                        0.7);

                    double blended = hydro * (1.0 - blend) + normalizedFlow * blend;
                    blended = blended * (1.0 - flowShadow * 0.35) + neighbourHydro * flowShadow * 0.35;
                    blended = blended * (1.0 - directionalWeight) + directionalHydro * directionalWeight;
                    blended *= 1.0 + confluence;
                    buffer[x, z] = (float)Math.Clamp(blended, 0.0, 1.25);
                }
            }

            Array.Copy(buffer, hydrology, buffer.Length);
            TerrainMaskUtility.BlendWatershedEdges(
                heightMap,
                hydrology,
                flow,
                watershedRadius,
                watershedBlend,
                flowShadowWeight);
            TerrainMaskUtility.ClampVariance(hydrology, config.Water.HydrologyVarianceClamp);
            TerrainMaskUtility.ApplyFlowShadow(hydrology, flow, flowShadowWeight, flowShadowSlopeWeight);
            TerrainMaskUtility.StitchEdges(hydrology, Math.Min(0.65, config.Water.HydrologySeamRelaxBlend * 0.85));
            TerrainMaskUtility.NormalizeEdgeBands(
                hydrology,
                config.Water.HydrologyEdgeBlendRadius,
                Math.Max(0.05, config.Water.HydrologySeamRelaxBlend * 0.4),
                config.Water.HydrologyEdgeVarianceClamp);
        }

        private void ApplyCurvatureHydrologyGuide(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            double curvatureWeight = Math.Clamp(config.Water.HydrologyCurvatureWeight, 0.0, 1.5);
            if (curvatureWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double slopePenalty = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double flowClamp = Math.Max(1.0, config.Water.HydrologyFlowDivergenceClamp * 12.0);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double seamHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double seamFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    double curvature = SampleCurvature(heightMap, x, z);
                    double basinAssist = Math.Clamp(curvature * curvatureWeight * 0.35, -0.65, 0.65);
                    double ridgePenalty = Math.Max(0.0, -basinAssist);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double slopeBrake = 1.0 - Math.Clamp(slope * slopePenalty * 0.02, 0.0, 0.45);
                    double gradient = Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flowValue) * 0.35;
                    double stability = 1.0 - Math.Clamp(gradient * gradientWeight * 0.35 + ridgePenalty * 0.35, 0.0, 0.75);

                    double hydroAnchor = hydro * 0.55 + seamHydro * 0.3 + seamFlow * 0.15;
                    double targetHydro = hydroAnchor + basinAssist * 0.35;
                    targetHydro *= slopeBrake * stability;
                    double clampDelta = varianceClamp * 0.35;
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(targetHydro, hydro - clampDelta, hydro + clampDelta));

                    double flowAnchor = flowValue * 0.6 + seamFlow * 0.25 + seamHydro * 0.15;
                    double targetFlow = flowAnchor + Math.Max(0.0, basinAssist) * 0.25;
                    targetFlow *= slopeBrake;
                    targetFlow *= 1.0 - Math.Clamp(ridgePenalty * 0.25 + gradient * gradientWeight * 0.25, 0.0, 0.55);
                    flow[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(targetFlow, 0.0, flowClamp));
                }
            }
        }

        private void ApplyHydrologyContinuityEnvelope(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double envelope = Math.Clamp(config.Water.HydrologyVarianceBlend * 0.5 + config.Water.HydrologyFlowMemoryWeight * 0.35, 0.05, 0.9);
            double flowMemoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double slopePenalty = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double stabilityWeight = Math.Clamp(config.Water.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(config.Water.HydrologyVarianceClamp, 0.0, 2.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.Water.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double flowClamp = Math.Max(config.Water.HydrologyFlowDivergenceClamp * 12.0, 2.5);
            double edgeBlendBase = Math.Clamp(config.Water.HydrologyEdgeBlendRadius / (double)Math.Max(1, chunkSize), 0.0, 0.35);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);
                    double flowGradient = Math.Abs(neighbourFlow - flowValue);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double directionalHydro = hydrology[downX, downZ];
                    double directionalFlow = flow[downX, downZ];

                    double edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFactor = edgeBlendBase * (1.0 - Math.Clamp(edgeDistance / (config.Water.HydrologyEdgeBlendRadius + 1.0), 0.0, 1.0));
                    double stability = 1.0 - Math.Clamp(
                        (hydrologyGradient + flowGradient) * stabilityWeight * 0.5 +
                        slope * slopePenalty * 0.02,
                        0.0,
                        0.85);
                    double flowShadow = Math.Clamp(
                        (flowValue / Math.Max(1.0, config.Water.RiverDepth)) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5,
                        0.0,
                        0.8);
                    double anchor = hydro * 0.55 + neighbourHydro * 0.25 + directionalHydro * 0.2;
                    double directionalBias = (Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * 0.25;
                    double blend = Math.Clamp(envelope * stability + edgeFactor + directionalBias * 0.15, 0.0, 0.9);
                    double harmonizedHydro = hydro * (1.0 - blend) + anchor * blend;
                    harmonizedHydro *= 1.0 - flowShadow * 0.15;
                    harmonizedHydro = Math.Clamp(harmonizedHydro, 0.0, varianceClamp);
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)harmonizedHydro);

                    double flowAnchor = flowValue * (0.6 + flowMemoryWeight * 0.25) + neighbourFlow * 0.25 + directionalFlow * 0.15 + hydrologyGradient * flowMemoryWeight * 0.1;
                    double blendedFlow = flowValue * (1.0 - blend * 0.35) + flowAnchor * blend * 0.35;
                    blendedFlow = Math.Clamp(blendedFlow, 0.0, flowClamp * (1.0 - flowShadow * 0.1));
                    flow[x, z] = (float)blendedFlow;
                }
            }
        }

        private void ApplyHydrologyEdgeEnvelope(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double continuityWeight = Math.Clamp(config.Water.HydrologyContinuityWeight, 0.0, 1.0);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyVarianceClamp);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
            double normalization = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double stabilityBoost = 1.0 + Math.Clamp(config.Water.HydrologyEdgeStabilityIterations * 0.05, 0.0, 0.3);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance >= edgeRadius)
                    {
                        continue;
                    }

                    double edgeWeight = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double hydro = hydrology[x, z];
                    double flowValue = flow[x, z];
                    double interiorHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double interiorFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double seamMemory = (hydro + interiorHydro + flowValue + interiorFlow) * 0.25;
                    double gradient = Math.Abs(interiorHydro - hydro) + Math.Abs(interiorFlow - flowValue) * 0.35;
                    double stability = 1.0 - Math.Clamp(gradient * config.Water.HydrologyEdgeVarianceClamp * 0.5, 0.0, 0.85);
                    double seamAnchor = (hydro + interiorHydro + flowValue * 0.5 + interiorFlow * 0.5) / 3.0;
                    double targetHydro = hydro * (1.0 - edgeWeight * 0.25) + seamAnchor * edgeWeight * (0.65 + continuityWeight * 0.35);
                    targetHydro += interiorFlow * memoryWeight * 0.05;
                    targetHydro = targetHydro * (1.0 - normalization * 0.25) + seamMemory * normalization * 0.25;
                    hydrology[x, z] = (float)Math.Clamp(targetHydro * stability * stabilityBoost, 0.0, varianceClamp);

                    double targetFlow = flowValue * (1.0 - edgeWeight * 0.25) + Math.Max(flowValue, interiorFlow) * edgeWeight;
                    targetFlow += seamAnchor * memoryWeight * 0.1;
                    targetFlow = targetFlow * (1.0 - normalization * 0.25) + (seamMemory + interiorFlow) * normalization * 0.25;
                    flow[x, z] = (float)Math.Clamp(targetFlow * stability * stabilityBoost, 0.0, flowClamp + 2.0);
                }
            }
        }

        private void NormalizeHydrologyFlowEdges(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, Math.Max(config.Water.HydrologyEdgeBlendRadius, config.Water.HydrologyWatershedStitchRadius));
            int iterations = Math.Max(1, config.Water.HydrologyEdgeNormalizationIterations);
            double blendBase = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend, 0.0, 1.0);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double watershedBlend = Math.Clamp(config.Water.HydrologyWatershedStitchWeight, 0.0, 1.0);
            double varianceClamp = Math.Clamp(config.Water.HydrologyEdgeVarianceClamp, 0.0, 2.0);

            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            continue;
                        }

                        double edgeFalloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                        double blend = blendBase * edgeFalloff;
                        if (blend <= 0.0)
                        {
                            continue;
                        }

                        float hydro = hydrology[x, z];
                        float flowValue = flow[x, z];
                        double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                        double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                        double seamAnchor = (neighbourHydro + hydro) * 0.5 + neighbourFlow * memoryWeight * 0.25;

                        double edgeRepair = watershedBlend * edgeFalloff;
                        double targetHydro = (neighbourHydro * (1.0 + memoryWeight * 0.35) + hydro * 0.65 + flowValue * memoryWeight * 0.15) / (1.8 + memoryWeight * 0.35);
                        targetHydro = (targetHydro + seamAnchor * (0.25 + edgeRepair * 0.35)) / (1.25 + edgeRepair * 0.35);
                        double candidateHydro = hydro + (targetHydro - hydro) * blend;
                        if (varianceClamp > 0.0)
                        {
                            double clampRange = varianceClamp * 0.35;
                            double min = hydro - clampRange;
                            double max = hydro + clampRange;
                            candidateHydro = Math.Clamp(candidateHydro, min, max);
                        }
                        hydroBuffer[x, z] = (float)Math.Clamp(candidateHydro, 0.0, 1.05);

                        double targetFlow = (neighbourFlow * (1.0 + memoryWeight) + flowValue + hydro * memoryWeight * 0.35) / (2.0 + memoryWeight);
                        targetFlow = (targetFlow + seamAnchor * (0.2 + edgeRepair * 0.35)) / (1.2 + edgeRepair * 0.35);
                        double clampMax = Math.Max(flowValue + 1.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
                        flowBuffer[x, z] = (float)Math.Clamp(targetFlow, 0.0, clampMax);
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
                Array.Copy(flowBuffer, flow, flowBuffer.Length);
            }
        }

        private void DiffuseHydrologyEdges(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            if (sizeX < 4 || sizeZ < 4)
            {
                return;
            }

            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            int iterations = Math.Max(1, Math.Min(3, config.Water.HydrologyEdgeStabilityIterations / 2));
            double baseBlend = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend * 0.5 + config.Water.HydrologyContinuityWeight * 0.35, 0.0, 0.95);
            double varianceClamp = Math.Max(0.001, config.Water.HydrologyEdgeVarianceClamp);
            double fluxBlend = Math.Clamp(config.Water.HydrologyEdgeFluxBlend, 0.0, 1.0);
            double flowClamp = Math.Max(0.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);

            if (baseBlend <= 0.0)
            {
                return;
            }

            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > edgeRadius)
                        {
                            hydroBuffer[x, z] = hydrology[x, z];
                            flowBuffer[x, z] = flow[x, z];
                            continue;
                        }

                        double tension = 1.0 - Math.Clamp(edgeDistance / (double)Math.Max(1, edgeRadius), 0.0, 1.0);
                        double blend = baseBlend * (0.65 + tension * 0.35);
                        double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                        double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                        double hydroVariance = TerrainMaskUtility.SampleVariance(hydrology, x, z);
                        double flowVariance = TerrainMaskUtility.SampleVariance(flow, x, z);

                        double targetHydro = hydrology[x, z] * (1.0 - blend) + neighbourHydro * blend;
                        targetHydro -= hydroVariance * varianceClamp * 0.5;
                        targetHydro = Math.Clamp(targetHydro, 0.0, 1.25);

                        double targetFlow = flow[x, z] * (1.0 - blend) + neighbourFlow * blend;
                        targetFlow -= flowVariance * varianceClamp * 0.35;
                        targetFlow += targetHydro * fluxBlend * 0.1;
                        targetFlow = Math.Clamp(targetFlow, 0.0, Math.Max(flow[x, z] + 1.0, flowClamp));

                        hydroBuffer[x, z] = TerrainMaskUtility.Clamp01((float)targetHydro);
                        flowBuffer[x, z] = TerrainMaskUtility.Clamp01((float)targetFlow);
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
                Array.Copy(flowBuffer, flow, flowBuffer.Length);
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, baseBlend, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, baseBlend * 0.85, varianceClamp * 1.35);
        }

        private void ApplyCrossChunkHydrologyStitch(float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double blendBase = Math.Clamp(config.Water.HydrologySeamRelaxBlend + config.Water.HydrologyEdgeFluxBlend * 0.25, 0.05, 0.95);
            double flowBlend = Math.Clamp(config.Water.HydrologyEdgeNormalizationBlend + config.Water.HydrologyFlowMemoryWeight * 0.25, 0.0, 1.0);
            double varianceClamp = Math.Max(0.0, config.Water.HydrologyEdgeVarianceClamp);

            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0);
                    double interiorHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double interiorFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    double hydroTarget = hydroCopy[x, z] * (1.0 - blendBase * falloff * 0.5) + interiorHydro * blendBase * falloff * 0.5;
                    hydroTarget += interiorFlow * flowBlend * 0.05;
                    if (varianceClamp > 0.0)
                    {
                        double clampRange = varianceClamp * falloff * 0.35;
                        hydroTarget = Math.Clamp(hydroTarget, hydroCopy[x, z] - clampRange, hydroCopy[x, z] + clampRange);
                    }

                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)hydroTarget);

                    double flowTarget = flowCopy[x, z] * (1.0 - flowBlend * falloff) + interiorFlow * flowBlend * falloff;
                    flow[x, z] = (float)Math.Clamp(flowTarget, 0.0, Math.Max(flowCopy[x, z] + 1.0, config.Water.HydrologyFlowDivergenceClamp * 12.0));
                }
            }
        }

        private void ApplyHydrologyEdgeCohesion(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double seamBlend = Math.Clamp(config.Water.HydrologySeamRelaxBlend + config.Water.HydrologyEdgeStabilityWeight * 0.35, 0.05, 0.95);
            double memoryWeight = Math.Clamp(config.Water.HydrologyFlowMemoryWeight, 0.0, 1.0);
            double riparianBoost = Math.Clamp(config.Water.RiparianSaturationBoost, 0.0, 1.0);
            double edgeFlux = Math.Clamp(config.Water.HydrologyEdgeFluxBlend, 0.0, 1.0);
            double varianceClamp = Math.Max(0.0, config.Water.HydrologyEdgeVarianceClamp);
            double slopePenalty = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double flowClamp = Math.Max(2.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(edgeRadius + 1);
                    double blend = seamBlend * falloff * (1.0 + riparianBoost * falloff * 0.35);
                    double hydro = hydroCopy[x, z];
                    double flowValue = flowCopy[x, z];
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    double seamMemory = (hydro + neighbourHydro + flowValue + neighbourFlow) * 0.25;
                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double directionalHydro = hydroCopy[downX, downZ];
                    double directionalFlow = flowCopy[downX, downZ];
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double hydroGradient = Math.Abs(neighbourHydro - hydro);
                    double flowGradient = Math.Abs(neighbourFlow - flowValue);
                    double stability = 1.0 - Math.Clamp(
                        (hydroGradient + flowGradient) * config.Water.HydrologyEdgeStabilityWeight * 0.35 +
                        slope * slopePenalty * 0.02,
                        0.0,
                        0.85);

                    double seamAnchor = hydro * 0.3 + neighbourHydro * 0.3 + seamMemory * 0.4;
                    double riparianCohesion = Math.Clamp(seamAnchor * riparianBoost + hydroGradient * edgeFlux * 0.5, 0.0, 1.0);
                    double anchorHydro = hydro * (0.55 + memoryWeight * 0.25) + neighbourHydro * 0.25 + directionalHydro * 0.15 + seamMemory * edgeFlux * 0.1;
                    double directionalBias = (Math.Abs(downhill.X) + Math.Abs(downhill.Z)) * 0.15;
                    double edgeAnchor = hydro * (1.0 - varianceClamp * falloff * 0.35) + neighbourHydro * varianceClamp * falloff * 0.35;
                    double harmonized = hydro * (1.0 - blend) + anchorHydro * blend;
                    harmonized = harmonized * stability + edgeAnchor * (1.0 - stability) * 0.25;
                    harmonized = harmonized * (1.0 - riparianCohesion * 0.35) + seamAnchor * riparianCohesion * 0.35;
                    harmonized *= 1.0 - Math.Clamp(hydroGradient * gradientWeight * 0.15 + directionalBias, 0.0, 0.4);
                    double clampDelta = varianceClamp * falloff;
                    hydrology[x, z] = (float)Math.Clamp(harmonized, hydro - clampDelta, hydro + clampDelta);

                    double flowAnchor = flowValue * (0.55 + memoryWeight * 0.25) + neighbourFlow * 0.25 + directionalFlow * 0.15 + hydroGradient * memoryWeight * 0.1;
                    flowAnchor += seamMemory * riparianBoost * 0.15;
                    double blendedFlow = flowValue * (1.0 - blend * 0.35) + flowAnchor * blend * 0.35;
                    blendedFlow = Math.Clamp(blendedFlow, 0.0, flowClamp * (1.0 + varianceClamp * 0.15));
                    flow[x, z] = (float)blendedFlow;
                }
            }

            TerrainMaskUtility.NormalizeEdgeBands(hydrology, edgeRadius, seamBlend * 0.85, varianceClamp);
            TerrainMaskUtility.NormalizeEdgeBands(flow, edgeRadius, seamBlend * 0.65, varianceClamp * 1.25);
        }

        private void ApplyRiparianEdgeFeather(float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius + config.Water.RiparianBufferRadius);
            double feather = Math.Clamp(config.Water.HydrologySeamRelaxBlend * 0.5 + config.Water.RiparianSaturationBoost * 0.25, 0.05, 0.9);
            double clampRange = Math.Max(0.001, config.Water.HydrologyEdgeVarianceClamp);
            double stability = Math.Clamp(config.Water.HydrologyEdgeStabilityWeight + config.Caves.RiparianCaveGuardWeight * 0.5, 0.0, 1.5);
            double flowClamp = Math.Max(2.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > edgeRadius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(edgeRadius + 1);
                    double interiorHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double interiorFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    double hydroGradient = Math.Abs(interiorHydro - hydroCopy[x, z]);
                    double flowGradient = Math.Abs(interiorFlow - flowCopy[x, z]);
                    double blend = feather * falloff;
                    double guard = Math.Clamp((hydroGradient + flowGradient) * stability * 0.35, 0.0, 0.65);

                    double hydroTarget = hydroCopy[x, z] * (1.0 - blend) + interiorHydro * blend;
                    hydroTarget = Math.Clamp(hydroTarget * (1.0 - guard), hydroCopy[x, z] - clampRange, hydroCopy[x, z] + clampRange);
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)hydroTarget);

                    double flowTarget = flowCopy[x, z] * (1.0 - blend * 0.75) + interiorFlow * blend * 0.75;
                    flowTarget = Math.Clamp(flowTarget * (1.0 - guard * 0.85), 0.0, Math.Max(flowCopy[x, z] + 1.0, flowClamp));
                    flow[x, z] = (float)flowTarget;

                    double erosionPull = Math.Clamp((hydroGradient + flowGradient) * stability * 0.5, 0.0, 0.65);
                    erosionRisk[x, z] = TerrainMaskUtility.Clamp01((float)Math.Max(
                        erosionRisk[x, z] * (1.0 - blend * 0.5),
                        erosionPull * 0.35));
                }
            }

            TerrainMaskUtility.NormalizeEdges(
                hydrology,
                edgeRadius,
                Math.Max(1, config.Water.HydrologySeamRelaxIterations + 1),
                feather * 0.85);
            TerrainMaskUtility.NormalizeEdges(
                flow,
                edgeRadius,
                Math.Max(1, config.Water.HydrologySeamRelaxIterations + 1),
                feather * 0.65);
        }

        private void HarmonizeHydrologyWithSurface(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            double edgeClamp = Math.Clamp(config.Water.HydrologyEdgeVarianceClamp, 0.0, 1.0);
            double gradientWeight = Math.Clamp(config.Water.HydrologyGradientWeight, 0.0, 1.0);
            double stabilityWeight = Math.Clamp(config.Water.HydrologyEdgeStabilityWeight, 0.0, 1.0);
            double flowPersistence = Math.Clamp(config.Water.HydrologyFlowPersistence, 0.0, 1.0);
            double slopePenalty = Math.Max(0.0, config.Water.HydrologySlopePenalty);
            double curvatureWeight = Math.Clamp(config.Water.HydrologyCurvatureWeight, 0.0, 1.0);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);
            double clampMax = Math.Max(2.5, config.Water.HydrologyFlowDivergenceClamp * 12.0);
            double flowShadowWeight = Math.Clamp(config.Water.HydrologyFlowShadowWeight, 0.0, 1.0);
            double flowShadowSlopeWeight = Math.Clamp(config.Water.HydrologyFlowShadowSlopeWeight, 0.0, 1.0);
            double flowSeepageWeight = Math.Clamp(config.Lakes.FlowSeepageWeight, 0.0, 1.0);

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                    double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                    double hydrologyGradient = Math.Abs(neighbourHydro - hydro);
                    double flowGradient = Math.Abs(neighbourFlow - flowValue);
                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double curvature = Math.Abs(SampleCurvature(heightMap, x, z)) * curvatureWeight * 0.05;
                    double flowShadow = Math.Clamp(
                        (flowValue / Math.Max(1.0, config.Water.RiverDepth)) * flowShadowWeight +
                        hydrologyGradient * flowShadowSlopeWeight * 0.5,
                        0.0,
                        0.75);

                    var downhill = TerrainMaskUtility.ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    double downhillHydro = hydrology[downX, downZ];
                    double downhillFlow = flow[downX, downZ];

                    double edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeBlend = 1.0 - Math.Clamp(edgeDistance / (edgeRadius + 1.0), 0.0, 1.0);

                    double stability = 1.0 - Math.Clamp((hydrologyGradient + flowGradient) * stabilityWeight, 0.0, 0.55);
                    stability *= 1.0 - Math.Clamp(slope / Math.Max(1.0, slopePenalty * 1.1), 0.0, 0.55);
                    stability *= 1.0 - flowShadow * 0.15;

                    double anchorHydro = hydro * (0.6 + flowPersistence * 0.25) + neighbourHydro * 0.25 + neighbourFlow * 0.15;
                    double directionalAnchor = downhillHydro * 0.25 + downhillFlow * 0.15;
                    double seepage = (flowValue + neighbourFlow + hydrologyGradient) * flowSeepageWeight * 0.15;
                    double blend = Math.Clamp(
                        hydrologyGradient * (0.35 + gradientWeight * 0.35) +
                        flowGradient * 0.15 +
                        edgeBlend * 0.35 +
                        curvature, 0.0, 0.85);

                    double harmonized = (anchorHydro + directionalAnchor + seepage) * stability;
                    double anchoredHydro = hydro * (1.0 - blend) + harmonized * blend;
                    double edgeAnchor = hydro * (1.0 - edgeBlend * edgeClamp) + neighbourHydro * edgeBlend * edgeClamp;
                    double dampenedHydro = anchoredHydro * (1.0 - edgeBlend * 0.35) + edgeAnchor * edgeBlend * 0.35;
                    dampenedHydro *= 1.0 - flowShadow * 0.15;
                    hydrology[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(dampenedHydro, 0.0, 1.25));

                    double flowAnchor = hydrology[x, z] * 0.5 + flowValue * (0.5 + flowPersistence * 0.2) + seepage * 0.25;
                    double flowClamp = clampMax * (1.0 - flowShadow * 0.15);
                    flow[x, z] = (float)Math.Clamp(
                        flowValue * (1.0 - blend * 0.35) + flowAnchor * blend * 0.35,
                        0.0,
                        Math.Max(0.5, flowClamp));
                }
            }
        }

        private void ApplyHydrologyReservoirSmoothing(int[,] heightMap, float[,] hydrology, float[,] flow)
        {
            int size = hydrology.GetLength(0);
            int depth = hydrology.GetLength(1);
            int iterations = Math.Max(0, config.Water.HydrologyReservoirIterations);
            double blend = Math.Clamp(config.Water.HydrologyReservoirBlend, 0.0, 1.0);
            if (iterations <= 0 || blend <= 0.0 || flow == null)
            {
                return;
            }

            var hydroBuffer = new float[size, depth];
            var flowBuffer = new float[size, depth];
            double clampRange = Math.Max(1.0, config.Water.HydrologyWaterTableClampRange);
            int edgeRadius = Math.Max(1, config.Water.HydrologyEdgeBlendRadius);

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int surface = heightMap[x, z];
                        float hydro = hydrology[x, z];
                        float flowValue = flow[x, z];
                        double neighbourHydro = TerrainMaskUtility.SampleInterior(hydrology, x, z);
                        double neighbourFlow = TerrainMaskUtility.SampleInterior(flow, x, z);
                        double edgeDistance = Math.Min(Math.Min(x, size - 1 - x), Math.Min(z, depth - 1 - z));
                        double edgeAttenuation = 1.0 - Math.Clamp(edgeDistance / (edgeRadius * 1.5), 0.0, 1.0);
                        double waterDepth = Math.Max(0, config.Water.GlobalWaterLevel - surface);
                        double waterClamp = Math.Clamp(waterDepth / clampRange, 0.0, 1.0);
                        double reservoirBlend = blend * (0.65 + edgeAttenuation * 0.35) * (0.65 + waterClamp * 0.35);
                        double hydroTarget = hydro * (1.0 - reservoirBlend) + neighbourHydro * reservoirBlend;
                        double flowTarget = flowValue * (1.0 - reservoirBlend * 0.65) + neighbourFlow * reservoirBlend * 0.65;
                        hydroBuffer[x, z] = TerrainMaskUtility.Clamp01((float)Math.Clamp(hydroTarget, 0.0, 1.35));
                        flowBuffer[x, z] = TerrainMaskUtility.Clamp01(
                            (float)Math.Clamp(flowTarget, 0.0, Math.Max(flowValue + 1.0, config.Water.HydrologyFlowDivergenceClamp * 12.0)));
                    }
                }

                Array.Copy(hydroBuffer, hydrology, hydrology.Length);
                Array.Copy(flowBuffer, flow, flow.Length);
            }
        }

        private void ApplyHydrologyGradientCoupling(float[,] hydrology, float[,] flow)
        {
            double clamp = Math.Max(0.05, config.Water.HydrologyGradientClamp * 0.25);
            double blend = Math.Clamp(
                config.Water.HydrologyEdgeNormalizationBlend * 0.5 +
                config.Water.HydrologyContinuityWeight * 0.35,
                0.0,
                0.85);
            TerrainMaskUtility.ClampGradientCoupling(hydrology, flow, clamp, blend);
        }

        private void ApplyErosionAwareDamping(float[,] hydrology, float[,] flow, float[,] erosionRisk)
        {
            double hydroWeight = Math.Clamp(config.Water.HydrologyEdgeStabilityWeight + config.Water.RiverBankErosionWeight, 0.0, 2.0) * 0.35;
            double flowWeight = Math.Clamp(config.Water.RiverBankErosionWeight + config.Water.LakeRimErosionWeight, 0.0, 2.0) * 0.35;
            if (hydroWeight <= 0.0 && flowWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double risk = Math.Clamp(erosionRisk[x, z], 0.0, 1.0);
                    if (risk <= 0.0)
                    {
                        continue;
                    }

                    double interiorHydro = TerrainMaskUtility.SampleInterior(hydroCopy, x, z);
                    double interiorFlow = TerrainMaskUtility.SampleInterior(flowCopy, x, z);
                    double damp = Math.Clamp(1.0 - risk * hydroWeight, 0.35, 1.0);
                    double flowDamp = Math.Clamp(1.0 - risk * flowWeight, 0.35, 1.0);
                    double smoothing = Math.Clamp(risk * config.Water.HydrologyVarianceBlend * 0.5, 0.0, 0.45);

                    double anchoredHydro = hydroCopy[x, z] * damp + interiorHydro * (1.0 - damp) * 0.5;
                    anchoredHydro = anchoredHydro * (1.0 - smoothing) + interiorHydro * smoothing;
                    double varianceClamp = Math.Max(0.0, config.Water.HydrologyVarianceClamp);
                    hydrology[x, z] = TerrainMaskUtility.Clamp01(Math.Clamp(anchoredHydro, 0.0, 1.0 + varianceClamp * 0.25));

                    double flowAnchor = flowCopy[x, z] * flowDamp + interiorFlow * (1.0 - flowDamp) * 0.35 + hydrology[x, z] * 0.15;
                    double flowClamp = Math.Max(config.Water.HydrologyFlowDivergenceClamp * 12.0, flowCopy[x, z] + 2.0);
                    flow[x, z] = (float)Math.Clamp(flowAnchor, 0.0, flowClamp);

                    erosionRisk[x, z] = TerrainMaskUtility.Clamp01(
                        risk * 0.65 +
                        hydrology[x, z] * 0.2 +
                        Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0) * 0.15);
                }
            }
        }

        private float[,] BuildErosionRiskField(int[,] heightMap, float[,] hydrology, float[,] flow, int size)
        {
            var risk = new float[size, size];
            double surfaceRange = Math.Max(1, worldHeight);

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    int surface = heightMap[x, z];
                    if (surface <= 0)
                    {
                        risk[x, z] = 0f;
                        continue;
                    }

                    double slope = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double slopeNorm = Math.Clamp(slope / 10.0, 0.0, 1.0);
                    double hydro = Math.Clamp(hydrology[x, z], 0.0f, 1.0f);
                    double flowNorm = Math.Clamp(flow[x, z] / 6.0, 0.0, 1.0);
                    double hydroGradient = Math.Abs(TerrainMaskUtility.SampleInterior(hydrology, x, z) - hydro);
                    double flowGradient = Math.Abs(TerrainMaskUtility.SampleInterior(flow, x, z) / 6.0 - flowNorm);
                    double altitude = Math.Clamp(surface / surfaceRange, 0.0, 1.0);
                    double valley = Math.Clamp((config.Water.GlobalWaterLevel - surface) / 16.0, 0.0, 1.0);
                    double exposure = Math.Clamp((1.0 - altitude) * 0.65 + valley * 0.45, 0.0, 1.0);
                    double continuityPenalty = Math.Clamp((hydroGradient + flowGradient) * 0.5, 0.0, 1.0);
                    double combined = hydro * 0.4 + flowNorm * 0.28 + exposure * 0.2 + slopeNorm * 0.15 + continuityPenalty * 0.12;

                    risk[x, z] = (float)Math.Clamp(combined, 0.0, 1.0);
                }
            }

            TerrainMaskUtility.Smooth2D(risk, config.Water.HydrologySmoothIterations, config.Water.HydrologySmoothBlend);
            return risk;
        }

        private static double SampleCurvature(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int forward = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            int back = heightMap[x, Math.Max(0, z - 1)];
            double laplacian = (left + right + forward + back - 4 * center) / 4.0;
            return laplacian;
        }
    }

    internal static class TerrainMaskUtility
    {
        public static float Clamp01(double value) => (float)Math.Clamp(value, 0.0, 1.0);

        public static double ComputeSlope(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int east = heightMap[Math.Min(sizeX - 1, x + 1), z];
            int north = heightMap[x, Math.Min(sizeZ - 1, z + 1)];
            double dx = center - east;
            double dz = center - north;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public static void Smooth2D(float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float sum = field[x, z];
                        int samples = 1;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ) continue;
                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        float average = sum / Math.Max(1, samples);
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + average * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void DirectionalSmooth(int[,] heightMap, float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (iterations == 0 || blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        var downhill = ComputeDownhillVector(heightMap, x, z);
                        int nx = Math.Clamp(x + Math.Sign(downhill.X), 0, sizeX - 1);
                        int nz = Math.Clamp(z + Math.Sign(downhill.Z), 0, sizeZ - 1);
                        float neighbour = field[nx, nz];
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + neighbour * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void StabilizeEdges(float[,] field, int radius, int iterations, double weight, double fluxBlend)
        {
            radius = Math.Max(1, radius);
            iterations = Math.Max(0, iterations);
            weight = Math.Clamp(weight, 0.0, 1.0);
            fluxBlend = Math.Clamp(fluxBlend, 0.0, 1.0);
            if (iterations == 0 || weight <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        bool isEdge = x < radius || z < radius || x >= sizeX - radius || z >= sizeZ - radius;
                        if (!isEdge)
                        {
                            buffer[x, z] = field[x, z];
                            continue;
                        }

                        float interior = SampleInterior(field, x, z);
                        double blend = weight * (1.0 - Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z)) / (double)radius);
                        double stabilised = field[x, z] * (1.0 - blend) + interior * blend;
                        buffer[x, z] = (float)(stabilised * (1.0 - fluxBlend) + interior * fluxBlend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void ApplyRiparianBuffer(float[,] field, int radius, double saturationBoost)
        {
            radius = Math.Max(0, radius);
            saturationBoost = Math.Clamp(saturationBoost, 0.0, 2.0);
            if (radius == 0 || saturationBoost <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float centre = field[x, z];
                    if (centre <= 0f)
                    {
                        continue;
                    }

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dz = -radius; dz <= radius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                            {
                                continue;
                            }

                            double distance = Math.Sqrt(dx * dx + dz * dz);
                            if (distance > radius + 0.001)
                            {
                                continue;
                            }

                            float influence = Clamp01(centre * saturationBoost * (1.0 - distance / (radius + 0.001)));
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void ApplyEdgeFlowLocks(int[,] heightMap, float[,] field, int radius, double lockWeight, double flowBias, double tangentWeight)
        {
            radius = Math.Max(1, radius);
            lockWeight = Math.Clamp(lockWeight, 0.0, 1.0);
            flowBias = Math.Clamp(flowBias, 0.0, 1.0);
            tangentWeight = Math.Clamp(tangentWeight, 0.0, 1.0);
            if (lockWeight <= 0.0 && flowBias <= 0.0 && tangentWeight <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance >= radius)
                    {
                        continue;
                    }

                    double blend = lockWeight * (1.0 - edgeDistance / (double)radius);
                    if (blend <= 0.0)
                    {
                        continue;
                    }

                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int nx = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int nz = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    float downhillValue = field[nx, nz];

                    int tx = Math.Clamp(x - downhill.Z, 0, sizeX - 1);
                    int tz = Math.Clamp(z + downhill.X, 0, sizeZ - 1);
                    float tangentValue = field[tx, tz];

                    float interior = SampleInterior(field, x, z);
                    double flowAligned = field[x, z] * (1.0 - flowBias) + downhillValue * flowBias;
                    double tangentAligned = field[x, z] * (1.0 - tangentWeight) + tangentValue * tangentWeight;
                    double locked = (flowAligned * 0.6) + (tangentAligned * 0.4);
                    double blended = field[x, z] * (1.0 - blend) + interior * (blend * 0.3) + locked * (blend * 0.7);

                    buffer[x, z] = (float)Math.Clamp(blended, 0.0, Math.Max(1.5, field[x, z] + 0.35));
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void ClampVariance(float[,] field, double clamp)
        {
            clamp = Math.Clamp(clamp, 0.0, 2.0);
            if (clamp <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float centre = field[x, z];
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = Clamp01(centre * (float)(1.0 - clamp * 0.5) + interior * (float)(clamp * 0.5));
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void RelaxEdges(float[,] field, int iterations, double blend)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (x > 0 && x < sizeX - 1 && z > 0 && z < sizeZ - 1)
                        {
                            continue;
                        }

                        float neighbour = SampleInterior(field, x, z);
                        field[x, z] = (float)(field[x, z] * (1.0 - blend) + neighbour * blend);
                    }
                }
            }
        }

        public static void StitchEdges(float[,] field, double blend)
        {
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + interior * blend);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void FillBasins(float[,] field, double strength, int iterations)
        {
            strength = Math.Clamp(strength, 0.0, 1.0);
            iterations = Math.Max(0, iterations);
            if (strength <= 0.0 || iterations == 0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float value = field[x, z];
                        float neighbour = SampleInterior(field, x, z);
                        if (value >= neighbour)
                        {
                            buffer[x, z] = value;
                            continue;
                        }

                        double delta = (neighbour - value) * strength * 0.5;
                        buffer[x, z] = Clamp01(value + (float)delta);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void ApplyFlowShadow(float[,] hydrology, float[,] flow, double weight, double slopeWeight)
        {
            weight = Math.Clamp(weight, 0.0, 1.0);
            slopeWeight = Math.Clamp(slopeWeight, 0.0, 1.0);
            if (weight <= 0.0 && slopeWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroBuffer = (float[,])hydrology.Clone();
            var flowBuffer = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydrology[x, z];
                    float flowValue = flow[x, z];
                    float neighbourFlow = SampleInterior(flow, x, z);
                    double flowShadow = Math.Clamp((flowValue + neighbourFlow) * 0.5 * weight, 0.0, 0.6);

                    float neighbourHydro = SampleInterior(hydrology, x, z);
                    double slopeShadow = Math.Clamp(Math.Abs(hydro - neighbourHydro) * slopeWeight, 0.0, 0.35);

                    double dampenedHydro = hydro * (1.0 - flowShadow * 0.35 - slopeShadow * 0.35) + neighbourHydro * (flowShadow * 0.2);
                    double flowDamp = flowValue * (1.0 - flowShadow * 0.25 - slopeShadow * 0.2);
                    flowDamp += neighbourFlow * (flowShadow * 0.25 + slopeShadow * 0.15);

                    hydroBuffer[x, z] = Clamp01(dampenedHydro);
                    flowBuffer[x, z] = (float)Math.Clamp(flowDamp, 0.0, Math.Max(flowValue + 0.75, neighbourFlow + 0.75));
                }
            }

            Array.Copy(hydroBuffer, hydrology, hydroBuffer.Length);
            Array.Copy(flowBuffer, flow, flowBuffer.Length);
        }

        public static float SampleInterior(float[,] field, int x, int z)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            int cx = Math.Clamp(x, 1, sizeX - 2);
            int cz = Math.Clamp(z, 1, sizeZ - 2);
            float sum = 0f;
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = Math.Clamp(cx + dx, 1, sizeX - 2);
                    int nz = Math.Clamp(cz + dz, 1, sizeZ - 2);
                    sum += field[nx, nz];
                    count++;
                }
            }

            return count == 0 ? field[cx, cz] : sum / count;
        }

        public static void BlendInterior(float[,] field, double blend)
        {
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float interior = SampleInterior(field, x, z);
                    buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + interior * blend);
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }

        public static void ApplyGradientStability(float[,] field, int iterations, double blend, double gradientClamp)
        {
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            gradientClamp = Math.Max(0.0001, gradientClamp);
            if (iterations == 0 || blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float centre = field[x, z];
                        float interior = SampleInterior(field, x, z);
                        double gradient = Math.Abs(centre - interior);
                        double weight = Math.Clamp(gradient / gradientClamp, 0.0, 1.0) * blend;
                        if (weight <= 0.0)
                        {
                            buffer[x, z] = centre;
                            continue;
                        }

                        double stabilised = centre * (1.0 - weight) + interior * weight;
                        double clampMax = Math.Max(Math.Max(centre, interior) + gradientClamp * 0.5, 1.0);
                        buffer[x, z] = (float)Math.Clamp(stabilised, 0.0, clampMax);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        public static void BlendWatershedEdges(
            int[,] heightMap,
            float[,] hydrology,
            float[,] flow,
            int radius,
            double blendWeight,
            double flowAnchorWeight)
        {
            radius = Math.Max(0, radius);
            blendWeight = Math.Clamp(blendWeight, 0.0, 1.0);
            flowAnchorWeight = Math.Clamp(flowAnchorWeight, 0.0, 1.0);
            if (radius <= 0 || blendWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > radius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(radius + 1);
                    double blend = blendWeight * falloff;

                    float interiorHydro = SampleInterior(hydroCopy, x, z);
                    float interiorFlow = SampleInterior(flowCopy, x, z);
                    var downhill = ComputeDownhillVector(heightMap, x, z);
                    int downX = Math.Clamp(x + downhill.X, 0, sizeX - 1);
                    int downZ = Math.Clamp(z + downhill.Z, 0, sizeZ - 1);
                    float downhillHydro = hydroCopy[downX, downZ];
                    float downhillFlow = flowCopy[downX, downZ];

                    double flowAnchor = Math.Clamp((flowCopy[x, z] + interiorFlow + downhillFlow) / 3.0, 0.0, 8.0);
                    flowAnchor = Math.Clamp(flowAnchor * flowAnchorWeight, 0.0, 4.0);

                    double targetHydro = interiorHydro * 0.55 + downhillHydro * 0.25 + flowAnchor * 0.1 + hydroCopy[x, z] * 0.1;
                    double targetFlow = interiorFlow * 0.5 + downhillFlow * 0.25 + flowAnchor * 0.25;

                    hydrology[x, z] = Clamp01(hydroCopy[x, z] * (1.0 - blend) + targetHydro * blend);
                    flow[x, z] = (float)Math.Clamp(flowCopy[x, z] * (1.0 - blend * 0.5) + targetFlow * blend, 0.0, Math.Max(2.5, targetFlow * 1.5 + 0.5));
                }
            }
        }

        public static void BalanceHydrologyPressure(float[,] hydrology, float[,] flow, double blendWeight, double gradientClamp, double flowWeight = 0.5)
        {
            if (hydrology == null || flow == null)
            {
                return;
            }

            blendWeight = Math.Clamp(blendWeight, 0.0, 1.0);
            gradientClamp = Math.Max(0.0, gradientClamp);
            flowWeight = Math.Clamp(flowWeight, 0.0, 1.0);
            if (blendWeight <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    double pressure = hydroCopy[x, z] + flowCopy[x, z] * flowWeight;
                    double neighborSum = 0.0;
                    double neighborWeight = 0.0;

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
                            if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                            {
                                continue;
                            }

                            double neighborPressure = hydroCopy[nx, nz] + flowCopy[nx, nz] * flowWeight;
                            double weight = dx == 0 || dz == 0 ? 1.0 : 0.65;
                            neighborSum += neighborPressure * weight;
                            neighborWeight += weight;
                        }
                    }

                    if (neighborWeight <= 0.0)
                    {
                        continue;
                    }

                    double averagePressure = neighborSum / neighborWeight;
                    double delta = Math.Clamp(averagePressure - pressure, -gradientClamp, gradientClamp);
                    double variance = SampleVariance(hydroCopy, x, z);
                    double varianceDamp = 1.0 - Math.Clamp(variance * 0.5, 0.0, 0.35);
                    double blend = blendWeight * varianceDamp;

                    double targetPressure = pressure + delta;
                    double targetHydro = hydroCopy[x, z] + delta * 0.65;
                    double targetFlow = flowCopy[x, z] + (targetPressure - targetHydro) * 0.5 / Math.Max(0.001, flowWeight);

                    hydrology[x, z] = Clamp01(hydroCopy[x, z] * (1.0 - blend) + Clamp01(targetHydro) * blend);
                    flow[x, z] = (float)Math.Clamp(
                        flowCopy[x, z] * (1.0 - blend * 0.5) + targetFlow * (blend * 0.5),
                        0.0,
                        Math.Max(8.0, flowCopy[x, z] + Math.Abs(delta) * 6.0));
                }
            }
        }

        public static void ClampGradientCoupling(float[,] hydrology, float[,] flow, double clamp, double blend)
        {
            clamp = Math.Max(0.0, clamp);
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (blend <= 0.0)
            {
                return;
            }

            int sizeX = hydrology.GetLength(0);
            int sizeZ = hydrology.GetLength(1);
            var hydroCopy = (float[,])hydrology.Clone();
            var flowCopy = (float[,])flow.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float hydro = hydroCopy[x, z];
                    float neighbourHydro = SampleInterior(hydroCopy, x, z);
                    float flowValue = flowCopy[x, z];
                    float neighbourFlow = SampleInterior(flowCopy, x, z);
                    double hydroDelta = Math.Clamp(neighbourHydro - hydro, -clamp, clamp);
                    double flowDelta = Math.Clamp(neighbourFlow - flowValue, -clamp * 6.0, clamp * 6.0);
                    double targetHydro = hydro + hydroDelta * blend;
                    double targetFlow = flowValue + flowDelta * blend * 0.5;
                    hydrology[x, z] = Clamp01(targetHydro);
                    flow[x, z] = (float)Math.Clamp(
                        targetFlow,
                        0.0,
                        Math.Max(flowValue + Math.Abs(flowDelta) * blend * 2.0, neighbourFlow + Math.Abs(flowDelta) + 1.0));
                }
            }
        }

        public static void ApplyHydrologyContinuity(
            float[,] mask,
            float[,] hydrology,
            float[,] flow,
            int edgeRadius,
            double continuityWeight)
        {
            continuityWeight = Math.Clamp(continuityWeight, 0.0, 1.0);
            if (continuityWeight <= 0.0)
            {
                return;
            }

            edgeRadius = Math.Max(0, edgeRadius);
            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var copy = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    double edgeFactor = edgeRadius > 0
                        ? 1.0 - Math.Clamp(edgeDistance / (double)(edgeRadius + 1), 0.0, 1.0)
                        : 0.0;
                    float hydro = Clamp01(hydrology[x, z]);
                    float seamHydro = Clamp01(SampleInterior(hydrology, x, z));
                    float flowNorm = Clamp01(flow[x, z] / 6.0f);
                    float seamFlow = Clamp01(SampleInterior(flow, x, z) / 6.0f);
                    double gradient = Math.Abs(seamHydro - hydro) + Math.Abs(seamFlow - flowNorm);
                    double blend = continuityWeight * (0.25 + edgeFactor * 0.35);
                    blend += Math.Clamp(gradient * continuityWeight * 0.35, 0.0, 0.65);
                    double target = copy[x, z] * (1.0 - blend);
                    double continuityAnchor = (hydro + flowNorm + seamHydro + seamFlow) * 0.25;
                    target += (copy[x, z] * (1.0 - Math.Clamp(gradient * 0.2, 0.0, 0.25)) + continuityAnchor) * blend;
                    mask[x, z] = Clamp01(target);
                }
            }
        }

        public static void NormalizeEdgeBands(float[,] field, int radius, double interiorBlend, double clampRange)
        {
            radius = Math.Max(1, radius);
            interiorBlend = Math.Clamp(interiorBlend, 0.0, 1.0);
            clampRange = Math.Max(0.0, clampRange);
            if (interiorBlend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var copy = (float[,])field.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                    if (edgeDistance > radius)
                    {
                        continue;
                    }

                    double falloff = 1.0 - edgeDistance / (double)(radius + 1);
                    double blend = interiorBlend * falloff;
                    float interior = SampleInterior(copy, x, z);
                    double target = copy[x, z] * (1.0 - blend) + interior * blend;

                    if (clampRange > 0.0)
                    {
                        double deltaClamp = clampRange * falloff;
                        double min = copy[x, z] - deltaClamp;
                        double max = copy[x, z] + deltaClamp;
                        target = Math.Clamp(target, min, max);
                    }

                    field[x, z] = Clamp01(target);
                }
            }
        }

        public static void NormalizeEdges(float[,] field, int radius, int iterations, double blend)
        {
            radius = Math.Max(0, radius);
            iterations = Math.Max(0, iterations);
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (iterations <= 0 || blend <= 0.0)
            {
                return;
            }

            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            var buffer = new float[sizeX, sizeZ];

            for (int iter = 0; iter < iterations; iter++)
            {
                Array.Copy(field, buffer, field.Length);

                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int edgeDistance = Math.Min(Math.Min(x, sizeX - 1 - x), Math.Min(z, sizeZ - 1 - z));
                        if (edgeDistance > radius)
                        {
                            continue;
                        }

                        float interior = SampleInterior(buffer, x, z);
                        float current = buffer[x, z];
                        double edgeFalloff = 1.0 - edgeDistance / (double)(radius + 1);
                        double lerp = blend * edgeFalloff;
                        field[x, z] = Clamp01((float)(current * (1.0 - lerp) + interior * lerp));
                    }
                }
            }
        }

        public static double SampleVariance(float[,] field, int x, int z, int radius = 1)
        {
            int sizeX = field.GetLength(0);
            int sizeZ = field.GetLength(1);
            double sum = 0.0;
            double sumSq = 0.0;
            int count = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= sizeX || nz >= sizeZ)
                    {
                        continue;
                    }

                    double value = field[nx, nz];
                    sum += value;
                    sumSq += value * value;
                    count++;
                }
            }

            if (count == 0)
            {
                return 0.0;
            }

            double mean = sum / count;
            double variance = Math.Max(0.0, sumSq / count - mean * mean);
            return Math.Clamp(variance, 0.0, 1.0);
        }

        public static (int X, int Z) ComputeDownhillVector(int[,] heightMap, int x, int z)
        {
            int sizeX = heightMap.GetLength(0);
            int sizeZ = heightMap.GetLength(1);
            int center = heightMap[x, z];
            int bestDrop = 0;
            int bestX = 0;
            int bestZ = 0;

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
                    if (nx < 0 || nx >= sizeX || nz < 0 || nz >= sizeZ)
                    {
                        continue;
                    }

                    int drop = center - heightMap[nx, nz];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestX = dx;
                        bestZ = dz;
                    }
                }
            }

            return (bestX, bestZ);
        }
    }
}
