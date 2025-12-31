using System;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Improved cave generation with enhanced algorithms
    /// </summary>
    public class ImprovedCaveGenerator
    {
        private readonly CaveConfig _config;
        private readonly Random _random;

        public ImprovedCaveGenerator(CaveConfig config, long worldSeed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = new Random((int)worldSeed ^ 0x5A3C7B01);
        }

        /// <summary>
        /// Build cave mask for specified area
        /// </summary>
        public bool[,,] BuildMask(int width, int depth, int height, int[,] heightMap, int seaLevel)
        {
            var mask = new bool[width, depth, height];
            
            // Generate primary cave tunnels
            GeneratePrimaryTunnels(mask, width, depth, height, heightMap, seaLevel);
            
            // Generate secondary cave branches
            GenerateSecondaryBranches(mask, width, depth, height, heightMap, seaLevel);
            
            // Generate caves and caverns
            GenerateCaverns(mask, width, depth, height, heightMap, seaLevel);
            
            // Apply smoothing to cave edges
            SmoothCaveEdges(mask, width, depth, height);
            
            return mask;
        }

        /// <summary>
        /// Generate primary cave tunnels
        /// </summary>
        private void GeneratePrimaryTunnels(bool[,,] mask, int width, int depth, int height, int[,] heightMap, int seaLevel)
        {
            int tunnelCount = (int)(_config.TunnelDensity * width * depth / 1000.0);
            
            for (int i = 0; i < tunnelCount; i++)
            {
                // Random starting point
                int startX = _random.Next(width);
                int startZ = _random.Next(depth);
                int startY = _random.Next(seaLevel, height - 10);
                
                // Generate tunnel path
                GenerateTunnelPath(mask, width, depth, height, startX, startY, startZ, _config.TunnelLength, _config.TunnelRadius);
            }
        }

        /// <summary>
        /// Generate a single tunnel path
        /// </summary>
        private void GenerateTunnelPath(bool[,,] mask, int width, int depth, int height, int startX, int startY, int startZ, int length, int radius)
        {
            int x = startX;
            int y = startY;
            int z = startZ;
            
            // Random direction
            double dx = (_random.NextDouble() - 0.5) * 2;
            double dy = (_random.NextDouble() - 0.5) * 0.5;
            double dz = (_random.NextDouble() - 0.5) * 2;
            
            // Normalize direction
            double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            dx /= length;
            dy /= length;
            dz /= length;
            
            for (int step = 0; step < length; step++)
            {
                // Carve out tunnel at current position
                CarveTunnelSection(mask, width, depth, height, x, y, z, radius);
                
                // Update position
                x += (int)Math.Round(dx);
                y += (int)Math.Round(dy);
                z += (int)Math.Round(dz);
                
                // Add some randomness to direction
                dx += (_random.NextDouble() - 0.5) * 0.2;
                dy += (_random.NextDouble() - 0.5) * 0.1;
                dz += (_random.NextDouble() - 0.5) * 0.2;
                
                // Renormalize direction
                length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                dx /= length;
                dy /= length;
                dz /= length;
                
                // Check bounds
                if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= depth)
                    break;
            }
        }

        /// <summary>
        /// Carve out a tunnel section
        /// </summary>
        private void CarveTunnelSection(bool[,,] mask, int width, int depth, int height, int centerX, int centerY, int centerZ, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int px = centerX + x;
                        int py = centerY + y;
                        int pz = centerZ + z;
                        
                        if (px >= 0 && px < width && py >= 0 && py < height && pz >= 0 && pz < depth)
                        {
                            double distance = Math.Sqrt(x * x + y * y + z * z);
                            if (distance <= radius)
                            {
                                mask[px, py, pz] = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate secondary cave branches
        /// </summary>
        private void GenerateSecondaryBranches(bool[,,] mask, int width, int depth, int height, int[,] heightMap, int seaLevel)
        {
            int branchCount = (int)(_config.BranchDensity * width * depth / 2000.0);
            
            for (int i = 0; i < branchCount; i++)
            {
                // Random starting point
                int startX = _random.Next(width);
                int startZ = _random.Next(depth);
                int startY = _random.Next(seaLevel / 2, height - 5);
                
                // Generate branch path
                GenerateTunnelPath(mask, width, depth, height, startX, startY, startZ, 
                    _config.BranchLength, _config.BranchRadius);
            }
        }

        /// <summary>
        /// Generate caves and caverns
        /// </summary>
        private void GenerateCaverns(bool[,,] mask, int width, int depth, int height, int[,] heightMap, int seaLevel)
        {
            int cavernCount = (int)(_config.CavernDensity * width * depth / 5000.0);
            
            for (int i = 0; i < cavernCount; i++)
            {
                // Random center point
                int centerX = _random.Next(width);
                int centerZ = _random.Next(depth);
                int centerY = _random.Next(seaLevel / 2, height - 10);
                
                // Generate cavern
                GenerateCavern(mask, width, depth, height, centerX, centerY, centerZ, 
                    _config.CavernRadius);
            }
        }

        /// <summary>
        /// Generate a single cavern
        /// </summary>
        private void GenerateCavern(bool[,,] mask, int width, int depth, int height, int centerX, int centerY, int centerZ, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int px = centerX + x;
                        int py = centerY + y;
                        int pz = centerZ + z;
                        
                        if (px >= 0 && px < width && py >= 0 && py < height && pz >= 0 && pz < depth)
                        {
                            double distance = Math.Sqrt(x * x + y * y + z * z);
                            if (distance <= radius)
                            {
                                mask[px, py, pz] = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply smoothing to cave edges
        /// </summary>
        private void SmoothCaveEdges(bool[,,] mask, int width, int depth, int height)
        {
            var smoothedMask = new bool[width, depth, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int solidCount = 0;
                        int totalCount = 0;
                        
                        // Check neighboring cells
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dz = -1; dz <= 1; dz++)
                                {
                                    int nx = x + dx;
                                    int ny = y + dy;
                                    int nz = z + dz;
                                    
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && nz >= 0 && nz < depth)
                                    {
                                        totalCount++;
                                        if (!mask[nx, ny, nz])
                                            solidCount++;
                                    }
                                }
                            }
                        }
                        
                        // Apply smoothing threshold
                        smoothedMask[x, y, z] = (solidCount / (double)totalCount) > 0.7;
                    }
                }
            }
            
            // Copy smoothed values back
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        mask[x, y, z] = smoothedMask[x, y, z];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configuration for cave generation
    /// </summary>
    public class CaveConfig
    {
        public double TunnelDensity { get; set; } = 0.5;
        public int TunnelLength { get; set; } = 50;
        public int TunnelRadius { get; set; } = 3;
        public double BranchDensity { get; set; } = 0.3;
        public int BranchLength { get; set; } = 20;
        public int BranchRadius { get; set; } = 2;
        public double CavernDensity { get; set; } = 0.1;
        public int CavernRadius { get; set; } = 8;
    }
}
                        if (px >= 0 && px < width && py >= 0 && py < height && pz >= 0 && pz < depth)
                        {
                            double distance = Math.Sqrt(x * x + y * y + z * z);
                            if (distance <= radius)
                            {
                                mask[px, py, pz] = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply smoothing to cave edges
        /// </summary>
        private void SmoothCaveEdges(bool[,,] mask, int width, int depth, int height)
        {
            var smoothedMask = new bool[width, depth, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int solidCount = 0;
                        int totalCount = 0;
                        
                        // Check neighboring cells
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dz = -1; dz <= 1; dz++)
                                {
                                    int nx = x + dx;
                                    int ny = y + dy;
                                    int nz = z + dz;
                                    
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && nz >= 0 && nz < depth)
                                    {
                                        totalCount++;
                                        if (!mask[nx, ny, nz])
                                            solidCount++;
                                    }
                                }
                            }
                        }
                        
                        // Apply smoothing threshold
                        smoothedMask[x, y, z] = (solidCount / (double)totalCount) > 0.7;
                    }
                }
            }
            
            // Copy smoothed values back
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        mask[x, y, z] = smoothedMask[x, y, z];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configuration for cave generation
    /// </summary>
    public class CaveConfig
    {
        public double TunnelDensity { get; set; } = 0.5;
        public int TunnelLength { get; set; } = 50;
        public int TunnelRadius { get; set; } = 3;
        public double BranchDensity { get; set; } = 0.3;
        public int BranchLength { get; set; } = 20;
        public int BranchRadius { get; set; } = 2;
        public double CavernDensity { get; set; } = 0.1;
        public int CavernRadius { get; set; } = 8;
    }
}
}
                double t = steps > 0 ? (double)i / steps : 0;
                int x = (int)Math.Round(x1 + (x2 - x1) * t);
                int y = (int)Math.Round(y1 + (y2 - y1) * t);
                int z = (int)Math.Round(z1 + (z2 - z1) * t);
                
                CarveCave(mask, width, height, depth, x, y, z, radius);
            }
        }
    }
}
}
                                Math.Pow(x - startX, 2) + 
                                Math.Pow(y - startY, 2) + 
                                Math.Pow(z - startZ, 2));
                            
                            if (distance < minDistance && distance > 3)
                            {
                                minDistance = distance;
                                nearestX = x;
                                nearestY = y;
                                nearestZ = z;
                            }
                        }
                    }
                }
            }
            
            // Create tunnel to nearest air space
            if (nearestX != -1)
            {
                CreateTunnel(mask, width, height, depth, startX, startY, startZ, nearestX, nearestY, nearestZ, 1.0);
            }
        }

        /// <summary>
        /// Create a tunnel between two points
        /// </summary>
        private void CreateTunnel(bool[,,] mask, int width, int height, int depth, int x1, int y1, int z1, int x2, int y2, int z2, double radius)
        {
            double distance = Math.Sqrt(
                Math.Pow(x2 - x1, 2) + 
                Math.Pow(y2 - y1, 2) + 
                Math.Pow(z2 - z1, 2));
            
            int steps = (int)Math.Ceiling(distance);
            
            for (int i = 0; i <= steps; i++)
            {
                double t = steps > 0 ? (double)i / steps : 0;
                int x = (int)Math.Round(x1 + (x2 - x1) * t);
                int y = (int)Math.Round(y1 + (y2 - y1) * t);
                int z = (int)Math.Round(z1 + (z2 - z1) * t);
                
                CarveCave(mask, width, height, depth, x, y, z, radius);
            }
        }
    }
}
}
                {
                    for (int y = 5; y < 100; y++)
                    {
                        if (chunk.GetBlock(x, y, z) == BlockType.Air && (x != startX || y != startY || z != startZ))
                        {
                            double distance = Math.Sqrt(
                                Math.Pow(x - startX, 2) + 
                                Math.Pow(y - startY, 2) + 
                                Math.Pow(z - startZ, 2));
                            
                            if (distance < minDistance && distance > 3)
                            {
                                minDistance = distance;
                                nearestX = x;
                                nearestY = y;
                                nearestZ = z;
                            }
                        }
                    }
                }
            }
            
            // Create tunnel to nearest air space
            if (nearestX != -1)
            {
                CreateTunnel(chunk, startX, startY, startZ, nearestX, nearestY, nearestZ, 1.0);
            }
        }

        /// <summary>
        /// Create a tunnel between two points
        /// </summary>
        private void CreateTunnel(ChunkData chunk, int x1, int y1, int z1, int x2, int y2, int z2, double radius)
        {
            double distance = Math.Sqrt(
                Math.Pow(x2 - x1, 2) + 
                Math.Pow(y2 - y1, 2) + 
                Math.Pow(z2 - z1, 2));
            
            int steps = (int)Math.Ceiling(distance);
            
            for (int i = 0; i <= steps; i++)
            {
                double t = steps > 0 ? (double)i / steps : 0;
                int x = (int)Math.Round(x1 + (x2 - x1) * t);
                int y = (int)Math.Round(y1 + (y2 - y1) * t);
                int z = (int)Math.Round(z1 + (z2 - z1) * t);
                
                CarveCave(chunk, x, y, z, radius);
            }
        }

        /// <summary>
        /// Create a stalactite hanging from the ceiling
        /// </summary>
        private void CreateStalactite(ChunkData chunk, int x, int y, int z, Random rand)
        {
            int length = 1 + rand.Next(3);
            
            for (int i = 0; i < length && y - i >= 0; i++)
            {
                if (chunk.GetBlock(x, y - i, z) == BlockType.Air)
                {
                    chunk.SetBlock(x, y - i, z, BlockType.Stone);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Create a stalagmite rising from the floor
        /// </summary>
        private void CreateStalagmite(ChunkData chunk, int x, int y, int z, Random rand)
        {
            int length = 1 + rand.Next(3);
            
            for (int i = 0; i < length && y + i < 256; i++)
            {
                if (chunk.GetBlock(x, y + i, z) == BlockType.Air)
                {
                    chunk.SetBlock(x, y + i, z, BlockType.Stone);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
                }
            }
        }

        /// <summary>
        /// Check if a connection should be created at this position
        /// </summary>
        private bool ShouldCreateConnection(ChunkData chunk, int x, int y, int z, Random rand)
        {
            // Count adjacent air blocks
            int airCount = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;
                        
                        if (nx >= 0 && nx < 16 && ny >= 0 && ny < 256 && nz >= 0 && nz < 16)
                        {
                            if (chunk.GetBlock(nx, ny, nz) == BlockType.Air)
                                airCount++;
                        }
                    }
                }
            }
            
            // Create connection if this is an isolated air pocket
            return airCount < 5 && rand.NextDouble() < 0.3;
        }

        /// <summary>
        /// Create a connection tunnel
        /// </summary>
        private void CreateConnection(ChunkData chunk, int startX, int startY, int startZ, Random rand)
        {
            // Find nearest air space to connect to
            int nearestX = -1, nearestY = -1, nearestZ = -1;
            double minDistance = double.MaxValue;
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 5; y < 100; y++)
                    {
                        if (chunk.GetBlock(x, y, z) == BlockType.Air && (x != startX || y != startY || z != startZ))
                        {
                            double distance = Math.Sqrt(
                                Math.Pow(x - startX, 2) + 
                                Math.Pow(y - startY, 2) + 
                                Math.Pow(z - startZ, 2));
                            
                            if (distance < minDistance && distance > 3)
                            {
                                minDistance = distance;
                                nearestX = x;
                                nearestY = y;
                                nearestZ = z;
                            }
                        }
                    }
                }
            }
            
            // Create tunnel to nearest air space
            if (nearestX != -1)
            {
                CreateTunnel(chunk, startX, startY, startZ, nearestX, nearestY, nearestZ, 1.0);
            }
        }

        /// <summary>
        /// Create a tunnel between two points
        /// </summary>
        private void CreateTunnel(ChunkData chunk, int x1, int y1, int z1, int x2, int y2, int z2, double radius)
        {
            double distance = Math.Sqrt(
                Math.Pow(x2 - x1, 2) + 
                Math.Pow(y2 - y1, 2) + 
                Math.Pow(z2 - z1, 2));
            
            int steps = (int)Math.Ceiling(distance);
            
            for (int i = 0; i <= steps; i++)
            {
                double t = steps > 0 ? (double)i / steps : 0;
                int x = (int)Math.Round(x1 + (x2 - x1) * t);
                int y = (int)Math.Round(y1 + (y2 - y1) * t);
                int z = (int)Math.Round(z1 + (z2 - z1) * t);
                
                CarveCave(chunk, x, y, z, radius);
            }
        }

        /// <summary>
        /// Create a stalactite hanging from the ceiling
        /// </summary>
        private void CreateStalactite(ChunkData chunk, int x, int y, int z, Random rand)
        {
            int length = 1 + rand.Next(3);
            
            for (int i = 0; i < length && y - i >= 0; i++)
            {
                if (chunk.GetBlock(x, y - i, z) == BlockType.Air)
                {
                    chunk.SetBlock(x, y - i, z, BlockType.Stone);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Create a stalagmite rising from the floor
        /// </summary>
        private void CreateStalagmite(ChunkData chunk, int x, int y, int z, Random rand)
        {
            int length = 1 + rand.Next(3);
            
            for (int i = 0; i < length && y + i < 256; i++)
            {
                if (chunk.GetBlock(x, y + i, z) == BlockType.Air)
                {
                    chunk.SetBlock(x, y + i, z, BlockType.Stone);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
}
