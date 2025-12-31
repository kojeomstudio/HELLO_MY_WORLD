using System;
using GameServerApp.Utils;
using GameServerApp.World;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Lake basin mask generator with river suppression and shoreline blending.
    /// </summary>
    public sealed class ImprovedLakeGenerator
    {
        private readonly LakeConfig config;
        private readonly Random random;

        public ImprovedLakeGenerator(LakeConfig config, long worldSeed)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            random = new Random((int)worldSeed ^ 0x1A2E0001);
        }

        public float[,] BuildMask(int chunkX, int chunkZ, int chunkSize, int[,] heightMap, float[,]? riverMask, int seaLevel)
        {
            var lakes = new float[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int worldX = chunkX * chunkSize + x;
                    int worldZ = chunkZ * chunkSize + z;

                    double primary = SimplexNoise.Generate(worldX * 0.008, worldZ * 0.008, 1.0, 3, 1.0, 0.55, random.Next());
                    double basin = SimplexNoise.Generate(worldX * 0.004 + 31, worldZ * 0.004 + 17, 1.0, 2, 1.0, 0.6, random.Next());
                    double inflow = riverMask != null ? riverMask[x, z] * config.ShorelineBlend : 0.0;
                    double slope = ComputeSlope(heightMap, x, z, chunkSize);
                    double rimPenalty = slope * config.ShorelineBlend * 0.05;
                    double elevationBias = Math.Max(0, seaLevel - heightMap[x, z]) * 0.0015;
                    double weight = (primary * 0.6) + (basin * 0.4) + config.SpawnWeightBias + inflow + elevationBias - rimPenalty;

                    if (riverMask != null)
                    {
                        weight -= riverMask[x, z] * config.RiverProximitySuppression * 0.5;
                    }

                    double wetlandThreshold = config.WetlandSaturationThreshold - (inflow * 0.15);
                    if (weight > wetlandThreshold && heightMap[x, z] > seaLevel - config.MaxDepth)
                    {
                        lakes[x, z] = (float)Math.Clamp(weight, 0.0, 1.0);
                    }
                }
            }

            Smooth(lakes, config.LakeBasinSmoothIterations, 0.55);
            ApplyWetlandBuffer(lakes, config.WetlandBufferRadius, config.ShorelineBlend);
            return lakes;
        }

        private static double ComputeSlope(int[,] heightMap, int x, int z, int chunkSize)
        {
            int left = heightMap[Math.Max(0, x - 1), z];
            int right = heightMap[Math.Min(chunkSize - 1, x + 1), z];
            int down = heightMap[x, Math.Max(0, z - 1)];
            int up = heightMap[x, Math.Min(chunkSize - 1, z + 1)];

            double dx = right - left;
            double dz = up - down;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        private static void Smooth(float[,] field, int iterations, double blend)
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

                                sum += field[nx, nz];
                                samples++;
                            }
                        }

                        float average = sum / samples;
                        buffer[x, z] = (float)(field[x, z] * (1.0 - blend) + average * blend);
                    }
                }

                Array.Copy(buffer, field, buffer.Length);
            }
        }

        private static void ApplyWetlandBuffer(float[,] field, int radius, double shorelineBlend)
        {
            radius = Math.Max(0, radius);
            shorelineBlend = Math.Clamp(shorelineBlend, 0.0, 1.0);
            if (radius == 0)
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
                    float center = field[x, z];
                    if (center <= 0f)
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

                            float distanceFalloff = 1f - (Math.Abs(dx) + Math.Abs(dz)) / (float)(radius + 1);
                            float influence = Math.Clamp(center * (float)shorelineBlend * distanceFalloff, 0f, 1f);
                            buffer[nx, nz] = Math.Max(buffer[nx, nz], influence);
                        }
                    }
                }
            }

            Array.Copy(buffer, field, buffer.Length);
        }
    }

    /// <summary>
    /// Configuration for lake generation
    /// </summary>
    public class LakeConfig
    {
        public double SpawnWeightBias { get; set; } = 0.0;
        public double ShorelineBlend { get; set; } = 0.5;
        public double RiverProximitySuppression { get; set; } = 0.8;
        public double WetlandSaturationThreshold { get; set; } = 0.6;
        public double MaxDepth { get; set; } = 10.0;
        public int LakeBasinSmoothIterations { get; set; } = 2;
        public int WetlandBufferRadius { get; set; } = 3;
    }
}
    /// Configuration for lake generation
    /// </summary>
    public class LakeConfig
    {
        public double SpawnWeightBias { get; set; } = 0.0;
        public double ShorelineBlend { get; set; } = 0.5;
        public double RiverProximitySuppression { get; set; } = 0.8;
        public double WetlandSaturationThreshold { get; set; } = 0.6;
        public double MaxDepth { get; set; } = 10.0;
        public int LakeBasinSmoothIterations { get; set; } = 2;
        public int WetlandBufferRadius { get; set; } = 3;
    }
}
                    intensityMap[x, z] = intensityMap[x, z] * 0.4 + smoothedMap[x, z] * 0.6;
                }
            }
        }

        /// <summary>
        /// Find the surface level at a position
        /// </summary>
        private int FindSurfaceLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                BlockType block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    return y;
                }
            }
            return -1;
        }

        /// <summary>
        /// Find the water level at a position
        /// </summary>
        private int FindWaterLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                BlockType block = chunk.GetBlock(x, y, z);
                if (block == BlockType.Water)
                {
                    return y;
                }
            }
            return -1;
        }
    }
}
                    }
                    
                    // Add reeds occasionally
                    if (_random.NextDouble() < 0.03 * intensity)
                    {
                        AddReeds(chunk, x, z);
                    }
                }
            }
        }

        /// <summary>
        /// Add lake shores
        /// </summary>
        private void AddLakeShores(ChunkData chunk, int x, int z, int surfaceY, double intensity)
        {
            // Calculate shore width based on intensity
            int shoreWidth = (int)(1 + intensity * 2);
            
            for (int i = -shoreWidth; i <= shoreWidth; i++)
            {
                for (int j = -shoreWidth; j <= shoreWidth; j++)
                {
                    // Calculate distance from lake center
                    double distance = Math.Sqrt(i * i + j * j);
                    
                    // Only place shores at certain distances
                    if (distance > shoreWidth * 0.7 && distance <= shoreWidth)
                    {
                        int shoreX = x + i;
                        int shoreZ = z + j;
                        
                        if (shoreX >= 0 && shoreX < 16 && shoreZ >= 0 && shoreZ < 16)
                        {
                            // Choose shore material based on intensity
                            BlockType shoreMaterial = intensity > 0.6 ? BlockType.Sand : BlockType.Dirt;
                            
                            // Place shore material
                            int shoreY = FindSurfaceLevel(chunk, shoreX, shoreZ);
                            if (shoreY >= 0 && shoreY < 256)
                            {
                                chunk.SetBlock(shoreX, shoreY, shoreZ, shoreMaterial);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a lily pad
        /// </summary>
        private void AddLilyPad(ChunkData chunk, int x, int z)
        {
            int waterY = FindWaterLevel(chunk, x, z);
            if (waterY < 0)
                return;
            
            // Place lily pad on water surface
            if (waterY + 1 < 256)
            {
                chunk.SetBlock(x, waterY + 1, z, BlockType.LilyPad);
            }
        }

        /// <summary>
        /// Add reeds
        /// </summary>
        private void AddReeds(ChunkData chunk, int x, int z)
        {
            int surfaceY = FindSurfaceLevel(chunk, x, z);
            if (surfaceY < 0)
                return;
            
            // Check if this is a water block or near water
            bool nearWater = false;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                    {
                        int waterY = FindWaterLevel(chunk, nx, nz);
                        if (waterY >= 0 && Math.Abs(waterY - surfaceY) <= 1)
                        {
                            nearWater = true;
                            break;
                        }
                    }
                }
                if (nearWater) break;
            }
            
            if (nearWater)
            {
                // Place reeds
                int reedHeight = 1 + _random.Next(2);
                for (int i = 0; i < reedHeight && surfaceY + i < 256; i++)
                {
                    chunk.SetBlock(x, surfaceY + i, z, BlockType.Reeds);
                }
            }
        }

        /// <summary>
        /// Smooth the intensity map
        /// </summary>
        private void SmoothIntensityMap(double[,] intensityMap)
        {
            var smoothedMap = new double[16, 16];
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    double sum = 0;
                    int count = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < 16 && nz >= 0 && nz < 16)
                            {
                                sum += intensityMap[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, z] = sum / count;
                }
            }
            
            // Copy smoothed values back
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    intensityMap[x, z] = intensityMap[x, z] * 0.4 + smoothedMap[x, z] * 0.6;
                }
            }
        }

        /// <summary>
        /// Find the surface level at a position
        /// </summary>
        private int FindSurfaceLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                BlockType block = chunk.GetBlock(x, y, z);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    return y;
                }
            }
            return -1;
        }

        /// <summary>
        /// Find the water level at a position
        /// </summary>
        private int FindWaterLevel(ChunkData chunk, int x, int z)
        {
            for (int y = 255; y >= 0; y--)
            {
                BlockType block = chunk.GetBlock(x, y, z);
                if (block == BlockType.Water)
                {
                    return y;
                }
            }
            return -1;
        }
    }
}
}
