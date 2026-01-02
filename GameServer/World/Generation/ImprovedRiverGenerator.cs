using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Hydrology-driven river mask builder with seam feathering and flow-aware width modulation.
    /// </summary>
    public sealed class ImprovedRiverGenerator
    {
        private readonly WaterConfig config;
        private readonly Random random;

        public ImprovedRiverGenerator(WaterConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)(worldSeed ^ 0x7B3C9A01));
        }

        public float[,] BuildMask(
            int chunkX,
            int chunkZ,
            int chunkSize,
            int[,] heightMap,
            float[,] hydrologyMask,
            float[,] flowAccumulation,
            int seaLevel)
        {
            var mask = new float[chunkSize, chunkSize];
            double noiseScale = Math.Max(0.0001, config.RiverNoiseScale);
            double reliefPenalty = Math.Clamp(config.RiverReliefPenaltyWeight, 0.0, 1.0);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int height = heightMap[x, z];
                    double worldX = chunkX * chunkSize + x;
                    double worldZ = chunkZ * chunkSize + z;
                    double baseNoise = Math.Abs(SimplexNoise.Generate(
                        worldX * noiseScale,
                        worldZ * noiseScale,
                        1.0,
                        2,
                        1.0,
                        0.55,
                        random.Next()));

                    double hydrology = hydrologyMask[x, z];
                    double flow = Math.Clamp(flowAccumulation[x, z] / 6.0, 0.0, 1.0);
                    double gradient = TerrainMaskUtility.ComputeSlope(heightMap, x, z);
                    double relief = Math.Max(0, heightMap[x, z] - seaLevel) / Math.Max(1, seaLevel);

                    double riverMask = config.RiverBankThreshold - baseNoise;
                    double pressure = Math.Max(0.0, riverMask);
                    pressure *= 1.0 + hydrology * config.HydrologyContinuityWeight;
                    pressure *= 1.0 + flow * config.RiverFlowAlignmentWeight;
                    pressure *= 1.0 - Math.Clamp(gradient * config.RiverGradientPenalty * 0.08, 0.0, 0.45);
                    pressure *= 1.0 - Math.Clamp(relief * reliefPenalty, 0.0, 0.35);

                    // Headwater stability slightly broadens shallow channels to avoid seams.
                    double headwater = 1.0 - Math.Clamp(flow * config.RiverHeadwaterStabilityWeight, 0.0, 0.65);
                    pressure *= 1.0 + headwater * 0.1;
                    double deltaBlend = 1.0 - Math.Clamp(Math.Abs(height - seaLevel) / Math.Max(1.0, config.RiverMouthSmoothRadius * 2.0), 0.0, 1.0);
                    pressure *= 1.0 + deltaBlend * config.RiverDeltaWetlandStrength * 0.5;

                    mask[x, z] = (float)Math.Clamp(pressure, 0.0, 1.35);
                }
            }

            TerrainMaskUtility.Smooth2D(mask, config.RiverIntensitySmoothIterations, config.RiverIntensitySmoothBlend);
            FeatherEdges(mask, config.RiverEdgeFeather, config.RiverSeamFillStrength);
            return mask;
        }

        private static void FeatherEdges(float[,] mask, double feather, double seamFill)
        {
            feather = Math.Clamp(feather, 0.0, 1.0);
            seamFill = Math.Clamp(seamFill, 0.0, 1.0);
            if (feather <= 0.0 && seamFill <= 0.0)
            {
                return;
            }

            int sizeX = mask.GetLength(0);
            int sizeZ = mask.GetLength(1);
            var buffer = (float[,])mask.Clone();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    bool isEdge = x == 0 || z == 0 || x == sizeX - 1 || z == sizeZ - 1;
                    if (!isEdge)
                    {
                        continue;
                    }

                    float centre = mask[x, z];
                    float neighbour = TerrainMaskUtility.Clamp01(SampleInterior(mask, x, z));
                    float blended = (float)(centre * (1.0 - feather) + neighbour * feather);
                    buffer[x, z] = Math.Max(blended, centre * (float)(1.0 - seamFill));
                }
            }

            Array.Copy(buffer, mask, buffer.Length);
        }

        private static float SampleInterior(float[,] field, int x, int z)
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
    }
}
