using System;
using GameServerApp.Utils;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Improved river generation with enhanced hydrology
    /// </summary>
    public class ImprovedRiverGenerator
    {
        private readonly RiverConfig _config;
        private readonly Random _random;

        public ImprovedRiverGenerator(RiverConfig config, long worldSeed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = new Random((int)worldSeed ^ 0x7B3C9A01);
        }

        /// <summary>
        /// Build river mask for specified area
        /// </summary>
        public float[,] BuildMask(int chunkX, int chunkZ, int chunkSize, int[,] heightMap, int seaLevel)
        {
            var mask = new float[chunkSize, chunkSize];
            
            // Generate river paths
            GenerateRiverPaths(mask, chunkX, chunkZ, chunkSize, heightMap, seaLevel);
            
            // Apply river width variations
            ApplyWidthVariations(mask, chunkSize);
            
            // Smooth river edges
            SmoothRiverEdges(mask, chunkSize);
            
            return mask;
        }

        /// <summary>
        /// Generate river paths
        /// </summary>
        private void GenerateRiverPaths(float[,] mask, int chunkX, int chunkZ, int chunkSize, int[,] heightMap, int seaLevel)
        {
            int riverCount = (int)(_config.RiverDensity * chunkSize * chunkSize / 10000.0);
            
            for (int i = 0; i < riverCount; i++)
            {
                // Find starting point (prefer higher elevations)
                var startPoint = FindRiverStart(heightMap, chunkSize, seaLevel);
                if (startPoint == null)
                    continue;
                
                // Generate river path
                GenerateRiverPath(mask, chunkX, chunkZ, chunkSize, heightMap, seaLevel, startPoint.Value);
            }
        }

        /// <summary>
        /// Find suitable river starting point
        /// </summary>
        private (int x, int z)? FindRiverStart(int[,] heightMap, int chunkSize, int seaLevel)
        {
            var candidates = new System.Collections.Generic.List<(int x, int z)>();
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (heightMap[x, z] > seaLevel + 5)
                    {
                        candidates.Add((x, z));
                    }
                }
            }
            
            if (candidates.Count == 0)
                return null;
            
            // Prefer higher elevations
            candidates.Sort((a, b) => heightMap[b.x, b.z].CompareTo(heightMap[a.x, a.z]));
            
            // Select from top 20% of candidates
            int topCount = Math.Max(1, candidates.Count / 5);
            int index = _random.Next(topCount);
            return candidates[index];
        }

        /// <summary>
        /// Generate a single river path
        /// </summary>
        private void GenerateRiverPath(float[,] mask, int chunkX, int chunkZ, int chunkSize, int[,] heightMap, int seaLevel, (int x, int z) start)
        {
            int x = start.x;
            int z = start.z;
            int currentHeight = heightMap[x, z];
            
            // River flow direction (initially random with downward bias)
            double flowX = (_random.NextDouble() - 0.5) * 2;
            double flowZ = (_random.NextDouble() - 0.5) * 2;
            
            for (int step = 0; step < _config.RiverLength; step++)
            {
                // Mark river at current position
                MarkRiverAt(mask, chunkSize, x, z, _config.RiverWidth);
                
                // Find next position based on gradient
                var nextPos = FindNextRiverPosition(heightMap, chunkSize, x, z, flowX, flowZ);
                if (nextPos == null)
                    break;
                
                // Update flow direction
                flowX = nextPos.Value.x - x;
                flowZ = nextPos.Value.z - z;
                double length = Math.Sqrt(flowX * flowX + flowZ * flowZ);
                if (length > 0)
                {
                    flowX /= length;
                    flowZ /= length;
                }
                
                // Update position
                x = nextPos.Value.x;
                z = nextPos.Value.z;
                currentHeight = heightMap[x, z];
                
                // Check if reached sea level
                if (currentHeight <= seaLevel)
                    break;
            }
        }

        /// <summary>
        /// Find next river position based on gradient
        /// </summary>
        private (int x, int z)? FindNextRiverPosition(int[,] heightMap, int chunkSize, int currentX, int currentZ, double flowX, double flowZ)
        {
            int bestX = currentX;
            int bestZ = currentZ;
            int bestHeight = heightMap[currentX, currentZ];
            
            // Check neighboring positions
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;
                    
                    int newX = currentX + dx;
                    int newZ = currentZ + dz;
                    
                    if (newX >= 0 && newX < chunkSize && newZ >= 0 && newZ < chunkSize)
                    {
                        int newHeight = heightMap[newX, newZ];
                        
                        // Prefer lower elevations and flow direction
                        double heightDiff = bestHeight - newHeight;
                        double flowAlignment = dx * flowX + dz * flowZ;
                        double score = heightDiff * 2 + flowAlignment;
                        
                        if (score > 0)
                        {
                            bestX = newX;
                            bestZ = newZ;
                            bestHeight = newHeight;
                        }
                    }
                }
            }
            
            // Return null if no better position found
            if (bestX == currentX && bestZ == currentZ)
                return null;
            
            return (bestX, bestZ);
        }

        /// <summary>
        /// Mark river at specific position
        /// </summary>
        private void MarkRiverAt(float[,] mask, int chunkSize, int x, int z, int width)
        {
            int radius = width / 2;
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int px = x + dx;
                    int pz = z + dz;
                    
                    if (px >= 0 && px < chunkSize && pz >= 0 && pz < chunkSize)
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz);
                        if (distance <= radius)
                        {
                            // Stronger influence at center
                            float influence = 1.0f - (float)(distance / radius);
                            mask[px, pz] = Math.Max(mask[px, pz], influence);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply width variations to rivers
        /// </summary>
        private void ApplyWidthVariations(float[,] mask, int chunkSize)
        {
            var noise = new float[chunkSize, chunkSize];
            
            // Generate width variation noise
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    noise[x, z] = (float)SimplexNoise.Generate(
                        x * 0.05, z * 0.05, 1.0, 2, 1.0, 0.5, _random.Next());
                }
            }
            
            // Apply variations
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (mask[x, z] > 0)
                    {
                        float variation = (noise[x, z] + 1.0f) * 0.5f; // Normalize to 0-1
                        float adjustedWidth = _config.RiverWidth * (0.7f + variation * 0.6f); // 70% to 130% of base width
                        
                        // Apply adjusted width
                        ApplyAdjustedWidth(mask, chunkSize, x, z, (int)adjustedWidth);
                    }
                }
            }
        }

        /// <summary>
        /// Apply adjusted width at specific position
        /// </summary>
        private void ApplyAdjustedWidth(float[,] mask, int chunkSize, int centerX, int centerZ, int width)
        {
            int radius = width / 2;
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int px = centerX + dx;
                    int pz = centerZ + dz;
                    
                    if (px >= 0 && px < chunkSize && pz >= 0 && pz < chunkSize)
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz);
                        if (distance <= radius)
                        {
                            float influence = 1.0f - (float)(distance / radius);
                            mask[px, pz] = Math.Max(mask[px, pz], influence * 0.8f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Smooth river edges
        /// </summary>
        private void SmoothRiverEdges(float[,] mask, int chunkSize)
        {
            var smoothedMask = new float[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float sum = 0;
                    int count = 0;
                    
                    // Check neighboring cells
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                            {
                                sum += mask[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMask[x, z] = sum / count;
                }
            }
            
            // Blend with original
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    mask[x, z] = mask[x, z] * 0.7f + smoothedMask[x, z] * 0.3f;
                }
            }
        }
    }

    /// <summary>
    /// Configuration for river generation
    /// </summary>
    public class RiverConfig
    {
        public double RiverDensity { get; set; } = 0.3;
        public int RiverLength { get; set; } = 100;
        public int RiverWidth { get; set; } = 3;
    }
}
                    {
                        double distance = Math.Sqrt(dx * dx + dz * dz);
                        if (distance <= radius)
                        {
                            float influence = 1.0f - (float)(distance / radius);
                            mask[px, pz] = Math.Max(mask[px, pz], influence * 0.8f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Smooth river edges
        /// </summary>
        private void SmoothRiverEdges(float[,] mask, int chunkSize)
        {
            var smoothedMask = new float[chunkSize, chunkSize];
            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float sum = 0;
                    int count = 0;
                    
                    // Check neighboring cells
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            
                            if (nx >= 0 && nx < chunkSize && nz >= 0 && nz < chunkSize)
                            {
                                sum += mask[nx, nz];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMask[x, z] = sum / count;
                }
            }
            
            // Blend with original
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    mask[x, z] = mask[x, z] * 0.7f + smoothedMask[x, z] * 0.3f;
                }
            }
        }
    }

    /// <summary>
    /// Configuration for river generation
    /// </summary>
    public class RiverConfig
    {
        public double RiverDensity { get; set; } = 0.3;
        public int RiverLength { get; set; } = 100;
        public int RiverWidth { get; set; } = 3;
    }
}
}
