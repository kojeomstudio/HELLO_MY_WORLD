using System;
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced cave generation system with multi-layered cave networks,
    /// improved cave connectivity, and better integration with terrain features.
    /// </summary>
    public class ImprovedCaveGenerator
    {
        private readonly CaveGenerationSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, List<CaveSystem>> _caveSystems;
        private readonly Dictionary<int, List<CaveTunnel>> _caveTunnels;
        private readonly Dictionary<int, List<CaveChamber>> _caveChambers;
        
        public ImprovedCaveGenerator(CaveGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _caveSystems = new Dictionary<int, List<CaveSystem>>();
            _caveTunnels = new Dictionary<int, List<CaveTunnel>>();
            _caveChambers = new Dictionary<int, List<CaveChamber>>();
        }
        
        /// <summary>
        /// Generate caves for a chunk
        /// </summary>
        public void GenerateCaves(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Initialize collections for this chunk
            _caveSystems[chunkKey] = new List<CaveSystem>();
            _caveTunnels[chunkKey] = new List<CaveTunnel>();
            _caveChambers[chunkKey] = new List<CaveChamber>();
            
            // Generate cave systems for this chunk
            GenerateCaveSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate cave tunnels
            GenerateCaveTunnels(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate cave chambers
            GenerateCaveChambers(chunkX, chunkZ, heightMap, blockTypes);
            
            // Connect cave systems
            ConnectCaveSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add water features to caves
            AddCaveWaterFeatures(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add cave decorations
            AddCaveDecorations(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate cave systems
        /// </summary>
        private void GenerateCaveSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var caveCount = _random.Next(_settings.MinCavesPerChunk, _settings.MaxCavesPerChunk + 1);
            
            for (int i = 0; i < caveCount; i++)
            {
                var caveSystem = new CaveSystem
                {
                    Id = _random.Next(),
                    CenterX = chunkX * 16 + _random.Next(16),
                    CenterY = _random.Next(_settings.MinCaveDepth, _settings.MaxCaveDepth),
                    CenterZ = chunkZ * 16 + _random.Next(16),
                    Radius = _random.Next(_settings.MinCaveRadius, _settings.MaxCaveRadius),
                    Complexity = _random.NextDouble() * _settings.CaveComplexityFactor,
                    Type = (CaveType)_random.Next(Enum.GetValues(typeof(CaveType)).Length)
                };
                
                _caveSystems[chunkKey].Add(caveSystem);
                
                // Generate cave system
                GenerateCaveSystem(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a single cave system
        /// </summary>
        private void GenerateCaveSystem(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate cave system based on type
            switch (caveSystem.Type)
            {
                case CaveType.Simple:
                    GenerateSimpleCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case CaveType.Branching:
                    GenerateBranchingCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case CaveType.Network:
                    GenerateNetworkCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case CaveType.Ravine:
                    GenerateRavineCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
            }
        }
        
        /// <summary>
        /// Generate a simple cave
        /// </summary>
        private void GenerateSimpleCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate a simple tunnel from the center
            var tunnel = new CaveTunnel
            {
                Id = _random.Next(),
                CaveSystemId = caveSystem.Id,
                StartX = caveSystem.CenterX,
                StartY = caveSystem.CenterY,
                StartZ = caveSystem.CenterZ,
                EndX = caveSystem.CenterX + _random.Next(-20, 21),
                EndY = caveSystem.CenterY + _random.Next(-10, 11),
                EndZ = caveSystem.CenterZ + _random.Next(-20, 21),
                Radius = caveSystem.Radius * 0.7f,
                Type = TunnelType.Main
            };
            
            _caveTunnels[chunkKey].Add(tunnel);
            
            // Carve the tunnel
            CarveTunnel(tunnel, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate a branching cave
        /// </summary>
        private void GenerateBranchingCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate main tunnel
            var mainTunnel = new CaveTunnel
            {
                Id = _random.Next(),
                CaveSystemId = caveSystem.Id,
                StartX = caveSystem.CenterX,
                StartY = caveSystem.CenterY,
                StartZ = caveSystem.CenterZ,
                EndX = caveSystem.CenterX + _random.Next(-30, 31),
                EndY = caveSystem.CenterY + _random.Next(-15, 16),
                EndZ = caveSystem.CenterZ + _random.Next(-30, 31),
                Radius = caveSystem.Radius * 0.8f,
                Type = TunnelType.Main
            };
            
            _caveTunnels[chunkKey].Add(mainTunnel);
            
            // Carve the main tunnel
            CarveTunnel(mainTunnel, chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate branches
            var branchCount = (int)(caveSystem.Complexity * 5);
            for (int i = 0; i < branchCount; i++)
            {
                var branchTunnel = new CaveTunnel
                {
                    Id = _random.Next(),
                    CaveSystemId = caveSystem.Id,
                    StartX = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).X,
                    StartY = GetRandomPointOnLine(mainTunnel.StartY, mainTunnel.StartY, mainTunnel.EndY, mainTunnel.EndY).Y,
                    StartZ = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).Z,
                    EndX = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).X + _random.Next(-15, 16),
                    EndY = GetRandomPointOnLine(mainTunnel.StartY, mainTunnel.StartY, mainTunnel.EndY, mainTunnel.EndY).Y + _random.Next(-10, 11),
                    EndZ = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).Z + _random.Next(-15, 16),
                    Radius = caveSystem.Radius * 0.5f,
                    Type = TunnelType.Branch
                };
                
                _caveTunnels[chunkKey].Add(branchTunnel);
                
                // Carve the branch tunnel
                CarveTunnel(branchTunnel, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a network cave
        /// </summary>
        private void GenerateNetworkCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate multiple interconnected tunnels
            var tunnelCount = (int)(caveSystem.Complexity * 8);
            var tunnelPositions = new List<(int x, int y, int z)>();
            
            // Generate initial positions
            for (int i = 0; i < tunnelCount; i++)
            {
                tunnelPositions.Add((
                    caveSystem.CenterX + _random.Next(-40, 41),
                    caveSystem.CenterY + _random.Next(-20, 21),
                    caveSystem.CenterZ + _random.Next(-40, 41)
                ));
            }
            
            // Connect positions with tunnels
            for (int i = 0; i < tunnelPositions.Count; i++)
            {
                for (int j = i + 1; j < tunnelPositions.Count; j++)
                {
                    // Connect some positions based on distance and complexity
                    var distance = Math.Sqrt(
                        Math.Pow(tunnelPositions[i].x - tunnelPositions[j].x, 2) +
                        Math.Pow(tunnelPositions[i].y - tunnelPositions[j].y, 2) +
                        Math.Pow(tunnelPositions[i].z - tunnelPositions[j].z, 2)
                    );
                    
                    if (distance < 30 * caveSystem.Complexity && _random.NextDouble() < 0.3)
                    {
                        var tunnel = new CaveTunnel
                        {
                            Id = _random.Next(),
                            CaveSystemId = caveSystem.Id,
                            StartX = tunnelPositions[i].x,
                            StartY = tunnelPositions[i].y,
                            StartZ = tunnelPositions[i].z,
                            EndX = tunnelPositions[j].x,
                            EndY = tunnelPositions[j].y,
                            EndZ = tunnelPositions[j].z,
                            Radius = caveSystem.Radius * 0.6f,
                            Type = TunnelType.Network
                        };
                        
                        _caveTunnels[chunkKey].Add(tunnel);
                        
                        // Carve the tunnel
                        CarveTunnel(tunnel, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a ravine cave
        /// </summary>
        private void GenerateRavineCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate a long, deep ravine
            var ravineLength = _random.Next(40, 80);
            var ravineDirection = _random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < ravineLength; i++)
            {
                var x = caveSystem.CenterX + (int)(Math.Cos(ravineDirection) * i);
                var z = caveSystem.CenterZ + (int)(Math.Sin(ravineDirection) * i);
                var y = caveSystem.CenterY + _random.Next(-5, 6);
                
                // Create a vertical shaft at this position
                var shaft = new CaveTunnel
                {
                    Id = _random.Next(),
                    CaveSystemId = caveSystem.Id,
                    StartX = x,
                    StartY = Math.Max(0, y - 20),
                    StartZ = z,
                    EndX = x,
                    EndY = Math.Min(255, y + 20),
                    EndZ = z,
                    Radius = caveSystem.Radius * 0.4f,
                    Type = TunnelType.Ravine
                };
                
                _caveTunnels[chunkKey].Add(shaft);
                
                // Carve the shaft
                CarveTunnel(shaft, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate cave tunnels
        /// </summary>
        private void GenerateCaveTunnels(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Additional tunnel generation is handled in GenerateCaveSystems
        }
        
        /// <summary>
        /// Generate cave chambers
        /// </summary>
        private void GenerateCaveChambers(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var chamberCount = _random.Next(_settings.MinChambersPerChunk, _settings.MaxChambersPerChunk + 1);
            
            for (int i = 0; i < chamberCount; i++)
            {
                var chamber = new CaveChamber
                {
                    Id = _random.Next(),
                    CenterX = chunkX * 16 + _random.Next(16),
                    CenterY = _random.Next(_settings.MinCaveDepth, _settings.MaxCaveDepth),
                    CenterZ = chunkZ * 16 + _random.Next(16),
                    RadiusX = _random.Next(_settings.MinChamberRadius, _settings.MaxChamberRadius),
                    RadiusY = _random.Next(_settings.MinChamberRadius, _settings.MaxChamberRadius),
                    RadiusZ = _random.Next(_settings.MinChamberRadius, _settings.MaxChamberRadius),
                    Type = (ChamberType)_random.Next(Enum.GetValues(typeof(ChamberType)).Length)
                };
                
                _caveChambers[chunkKey].Add(chamber);
                
                // Carve the chamber
                CarveChamber(chamber, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Connect cave systems
        /// </summary>
        private void ConnectCaveSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Connect nearby cave systems
            for (int i = 0; i < _caveSystems[chunkKey].Count; i++)
            {
                for (int j = i + 1; j < _caveSystems[chunkKey].Count; j++)
                {
                    var system1 = _caveSystems[chunkKey][i];
                    var system2 = _caveSystems[chunkKey][j];
                    
                    var distance = Math.Sqrt(
                        Math.Pow(system1.CenterX - system2.CenterX, 2) +
                        Math.Pow(system1.CenterY - system2.CenterY, 2) +
                        Math.Pow(system1.CenterZ - system2.CenterZ, 2)
                    );
                    
                    // Connect systems if they're close enough
                    if (distance < _settings.CaveConnectionDistance && _random.NextDouble() < _settings.CaveConnectionProbability)
                    {
                        var connection = new CaveTunnel
                        {
                            Id = _random.Next(),
                            CaveSystemId = system1.Id,
                            StartX = system1.CenterX,
                            StartY = system1.CenterY,
                            StartZ = system1.CenterZ,
                            EndX = system2.CenterX,
                            EndY = system2.CenterY,
                            EndZ = system2.CenterZ,
                            Radius = Math.Min(system1.Radius, system2.Radius) * 0.5f,
                            Type = TunnelType.Connection
                        };
                        
                        _caveTunnels[chunkKey].Add(connection);
                        
                        // Carve the connection
                        CarveTunnel(connection, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add water features to caves
        /// </summary>
        private void AddCaveWaterFeatures(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add water pools to chambers
            foreach (var chamber in _caveChambers[chunkKey])
            {
                if (chamber.Type == ChamberType.Flooded || (chamber.Type == ChamberType.Lake && _random.NextDouble() < 0.7))
                {
                    // Fill the bottom of the chamber with water
                    var waterLevel = chamber.CenterY - chamber.RadiusY / 2;
                    
                    for (int x = 0; x < 16; x++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            var worldX = chunkX * 16 + x;
                            var worldZ = chunkZ * 16 + z;
                            
                            // Check if this position is inside the chamber
                            var dx = (worldX - chamber.CenterX) / (double)chamber.RadiusX;
                            var dy = 0; // We're filling from the bottom
                            var dz = (worldZ - chamber.CenterZ) / (double)chamber.RadiusZ;
                            
                            if (dx * dx + dy * dy + dz * dz <= 1.0)
                            {
                                for (int y = 0; y < 256; y++)
                                {
                                    var index = y * 16 * 16 + z * 16 + x;
                                    
                                    // Check if this position is inside the chamber
                                    var dy3d = (y - chamber.CenterY) / (double)chamber.RadiusY;
                                    var dx3d = (worldX - chamber.CenterX) / (double)chamber.RadiusX;
                                    var dz3d = (worldZ - chamber.CenterZ) / (double)chamber.RadiusZ;
                                    
                                    if (dx3d * dx3d + dy3d * dy3d + dz3d * dz3d <= 1.0 && y <= waterLevel)
                                    {
                                        blockTypes[index] = (int)BlockType.Water;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Add water streams to tunnels
            foreach (var tunnel in _caveTunnels[chunkKey])
            {
                if (tunnel.Type == TunnelType.Ravine && _random.NextDouble() < 0.3)
                {
                    // Add a water stream at the bottom of the ravine
                    var steps = 20;
                    
                    for (int i = 0; i <= steps; i++)
                    {
                        var t = i / (double)steps;
                        var x = (int)(tunnel.StartX + t * (tunnel.EndX - tunnel.StartX));
                        var y = (int)(tunnel.StartY + t * (tunnel.EndY - tunnel.StartY));
                        var z = (int)(tunnel.StartZ + t * (tunnel.EndZ - tunnel.StartZ));
                        
                        // Find the bottom of the tunnel at this position
                        var bottomY = FindTunnelBottom(x, y, z, tunnel, chunkX, chunkZ, heightMap, blockTypes);
                        
                        if (bottomY >= 0)
                        {
                            var index = bottomY * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                blockTypes[index] = (int)BlockType.Water;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add cave decorations
        /// </summary>
        private void AddCaveDecorations(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add decorations to chambers
            foreach (var chamber in _caveChambers[chunkKey])
            {
                switch (chamber.Type)
                {
                    case ChamberType.Stalactite:
                        AddStalactites(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                    case ChamberType.Stalagmite:
                        AddStalagmites(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                    case ChamberType.Crystal:
                        AddCrystals(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                    case ChamberType.Mushroom:
                        AddMushrooms(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                }
            }
            
            // Add decorations to tunnels
            foreach (var tunnel in _caveTunnels[chunkKey])
            {
                if (_random.NextDouble() < _settings.CaveDecorationDensity)
                {
                    AddTunnelDecorations(tunnel, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Carve a tunnel
        /// </summary>
        private void CarveTunnel(CaveTunnel tunnel, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(tunnel.EndX - tunnel.StartX, 2) +
                Math.Pow(tunnel.EndY - tunnel.StartY, 2) +
                Math.Pow(tunnel.EndZ - tunnel.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = tunnel.StartX + t * (tunnel.EndX - tunnel.StartX);
                var y = tunnel.StartY + t * (tunnel.EndY - tunnel.StartY);
                var z = tunnel.StartZ + t * (tunnel.EndZ - tunnel.StartZ);
                
                // Carve a sphere at this position
                CarveSphere(x, y, z, tunnel.Radius, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Carve a chamber
        /// </summary>
        private void CarveChamber(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    for (int y = 0; y < 256; y++)
                    {
                        var dx = (worldX - chamber.CenterX) / (double)chamber.RadiusX;
                        var dy = (y - chamber.CenterY) / (double)chamber.RadiusY;
                        var dz = (worldZ - chamber.CenterZ) / (double)chamber.RadiusZ;
                        
                        // Check if this position is inside the ellipsoid
                        if (dx * dx + dy * dy + dz * dz <= 1.0)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Don't carve water blocks
                                if (blockTypes[index] != (int)BlockType.Water)
                                {
                                    blockTypes[index] = 0; // Air
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Carve a sphere
        /// </summary>
        private void CarveSphere(double centerX, double centerY, double centerZ, double radius, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var minX = Math.Max(0, (int)Math.Floor(centerX - radius) - chunkX * 16);
            var maxX = Math.Min(15, (int)Math.Ceiling(centerX + radius) - chunkX * 16);
            var minY = Math.Max(0, (int)Math.Floor(centerY - radius));
            var maxY = Math.Min(255, (int)Math.Ceiling(centerY + radius));
            var minZ = Math.Max(0, (int)Math.Floor(centerZ - radius) - chunkZ * 16);
            var maxZ = Math.Min(15, (int)Math.Ceiling(centerZ + radius) - chunkZ * 16);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var dx = (chunkX * 16 + x - centerX);
                        var dy = (y - centerY);
                        var dz = (chunkZ * 16 + z - centerZ);
                        
                        // Check if this position is inside the sphere
                        if (dx * dx + dy * dy + dz * dz <= radius * radius)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Don't carve water blocks
                                if (blockTypes[index] != (int)BlockType.Water)
                                {
                                    blockTypes[index] = 0; // Air
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add stalactites to a chamber
        /// </summary>
        private void AddStalactites(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var stalactiteCount = _random.Next(5, 15);
            
            for (int i = 0; i < stalactiteCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dz * dz <= 0.8) // Only place near the edges
                {
                    // Find the ceiling of the chamber at this position
                    var ceilingY = FindChamberCeiling(x, chamber.CenterY, z, chamber, chunkX, chunkZ, heightMap, blockTypes);
                    
                    if (ceilingY >= 0)
                    {
                        // Grow a stalactite from the ceiling
                        var length = _random.Next(3, 8);
                        
                        for (int j = 0; j < length; j++)
                        {
                            var y = ceilingY - j;
                            var index = y * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                            
                            if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                            {
                                blockTypes[index] = (int)BlockType.Stone;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add stalagmites to a chamber
        /// </summary>
        private void AddStalagmites(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var stalagmiteCount = _random.Next(5, 15);
            
            for (int i = 0; i < stalagmiteCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dz * dz <= 0.8) // Only place near the edges
                {
                    // Find the floor of the chamber at this position
                    var floorY = FindChamberFloor(x, chamber.CenterY, z, chamber, chunkX, chunkZ, heightMap, blockTypes);
                    
                    if (floorY >= 0)
                    {
                        // Grow a stalagmite from the floor
                        var length = _random.Next(3, 8);
                        
                        for (int j = 0; j < length; j++)
                        {
                            var y = floorY + j;
                            var index = y * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                            
                            if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                            {
                                blockTypes[index] = (int)BlockType.Stone;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add crystals to a chamber
        /// </summary>
        private void AddCrystals(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var crystalCount = _random.Next(10, 30);
            
            for (int i = 0; i < crystalCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var y = chamber.CenterY + _random.Next(-chamber.RadiusY, chamber.RadiusY + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dy = (y - chamber.CenterY) / (double)chamber.RadiusY;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dy * dy + dz * dz <= 0.9)
                {
                    var index = y * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                    
                    if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                    {
                        blockTypes[index] = (int)BlockType.Diamond; // Use diamond as crystal placeholder
                    }
                }
            }
        }
        
        /// <summary>
        /// Add mushrooms to a chamber
        /// </summary>
        private void AddMushrooms(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var mushroomCount = _random.Next(10, 25);
            
            for (int i = 0; i < mushroomCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dz * dz <= 0.9)
                {
                    // Find the floor of the chamber at this position
                    var floorY = FindChamberFloor(x, chamber.CenterY, z, chamber, chunkX, chunkZ, heightMap, blockTypes);
                    
                    if (floorY >= 0)
                    {
                        var index = (floorY + 1) * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                        
                        if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                        {
                            blockTypes[index] = (int)BlockType.Mushroom;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add decorations to tunnels
        /// </summary>
        private void AddTunnelDecorations(CaveTunnel tunnel, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(tunnel.EndX - tunnel.StartX, 2) +
                Math.Pow(tunnel.EndY - tunnel.StartY, 2) +
                Math.Pow(tunnel.EndZ - tunnel.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i += 5) // Place decorations every 5 steps
            {
                var t = i / (double)steps;
                var x = tunnel.StartX + t * (tunnel.EndX - tunnel.StartX);
                var y = tunnel.StartY + t * (tunnel.EndY - tunnel.StartY);
                var z = tunnel.StartZ + t * (tunnel.EndZ - tunnel.StartZ);
                
                // Add ore veins
                if (_random.NextDouble() < 0.1)
                {
                    AddOreVein(x, y, z, tunnel.Radius * 0.3, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Add cobwebs
                if (_random.NextDouble() < 0.05)
                {
                    AddCobwebs(x, y, z, tunnel.Radius * 0.5, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Add an ore vein
        /// </summary>
        private void AddOreVein(double centerX, double centerY, double centerZ, double radius, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var oreType = _random.Next(1, 6); // Different ore types
            var oreCount = _random.Next(3, 8);
            
            for (int i = 0; i < oreCount; i++)
            {
                var x = centerX + _random.Next(-3, 4);
                var y = centerY + _random.Next(-3, 4);
                var z = centerZ + _random.Next(-3, 4);
                
                var index = (int)y * 16 * 16 + ((int)z - chunkZ * 16) * 16 + ((int)x - chunkX * 16);
                
                if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                {
                    blockTypes[index] = oreType;
                }
            }
        }
        
        /// <summary>
        /// Add cobwebs
        /// </summary>
        private void AddCobwebs(double centerX, double centerY, double centerZ, double radius, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var webCount = _random.Next(2, 5);
            
            for (int i = 0; i < webCount; i++)
            {
                var x = centerX + _random.Next(-2, 3);
                var y = centerY + _random.Next(-2, 3);
                var z = centerZ + _random.Next(-2, 3);
                
                var index = (int)y * 16 * 16 + ((int)z - chunkZ * 16) * 16 + ((int)x - chunkX * 16);
                
                if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                {
                    blockTypes[index] = (int)BlockType.Cobweb;
                }
            }
        }
        
        /// <summary>
        /// Find the bottom of a tunnel at a position
        /// </summary>
        private int FindTunnelBottom(int x, int y, int z, CaveTunnel tunnel, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int i = y; i >= 0; i--)
            {
                var index = i * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                
                if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                {
                    return i + 1;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Find the ceiling of a chamber at a position
        /// </summary>
        private int FindChamberCeiling(int x, int y, int z, CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int i = y + chamber.RadiusY; i >= y; i--)
            {
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dy = (i - chamber.CenterY) / (double)chamber.RadiusY;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dy * dy + dz * dz <= 1.0)
                {
                    var index = i * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                    
                    if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                    {
                        return i - 1;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Find the floor of a chamber at a position
        /// </summary>
        private int FindChamberFloor(int x, int y, int z, CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int i = y - chamber.RadiusY; i <= y; i++)
            {
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dy = (i - chamber.CenterY) / (double)chamber.RadiusY;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dy * dy + dz * dz <= 1.0)
                {
                    var index = i * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                    
                    if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                    {
                        return i + 1;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Get a random point on a line
        /// </summary>
        private (int X, int Y, int Z) GetRandomPointOnLine(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            var t = _random.NextDouble();
            return (
                (int)(x1 + t * (x2 - x1)),
                (int)(y1 + t * (y2 - y1)),
                (int)(z1 + t * (z2 - z1))
            );
        }
        
        /// <summary>
        /// Get a unique key for a chunk
        /// </summary>
        private int GetChunkKey(int chunkX, int chunkZ)
        {
            return chunkX * 1000000 + chunkZ;
        }
    }
    
    #region Supporting Classes and Enums
    
    /// <summary>
    /// Cave system information
    /// </summary>
    public class CaveSystem
    {
        public int Id { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterZ { get; set; }
        public int Radius { get; set; }
        public double Complexity { get; set; }
        public CaveType Type { get; set; }
    }
    
    /// <summary>
    /// Cave tunnel information
    /// </summary>
    public class CaveTunnel
    {
        public int Id { get; set; }
        public int CaveSystemId { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int EndZ { get; set; }
        public float Radius { get; set; }
        public TunnelType Type { get; set; }
    }
    
    /// <summary>
    /// Cave chamber information
    /// </summary>
    public class CaveChamber
    {
        public int Id { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterZ { get; set; }
        public int RadiusX { get; set; }
        public int RadiusY { get; set; }
        public int RadiusZ { get; set; }
        public ChamberType Type { get; set; }
    }
    
    /// <summary>
    /// Cave types
    /// </summary>
    public enum CaveType
    {
        Simple,
        Branching,
        Network,
        Ravine
    }
    
    /// <summary>
    /// Tunnel types
    /// </summary>
    public enum TunnelType
    {
        Main,
        Branch,
        Network,
        Connection,
        Ravine
    }
    
    /// <summary>
    /// Chamber types
    /// </summary>
    public enum ChamberType
    {
        Empty,
        Stalactite,
        Stalagmite,
        Crystal,
        Mushroom,
        Lake,
        Flooded
    }
    
    /// <summary>
    /// Block types
    /// </summary>
    public enum BlockType
    {
        Air = 0,
        Stone = 1,
        Dirt = 2,
        Grass = 3,
        Water = 4,
        Sand = 5,
        Gravel = 6,
        Wood = 7,
        Leaves = 8,
        Coal = 9,
        Iron = 10,
        Gold = 11,
        Diamond = 12,
        Mushroom = 13,
        Cobweb = 14
    }
    
    #endregion
}
using System.Collections.Generic;
using System.Linq;
using GameServerApp.Configuration;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced cave generation system with multi-layered cave networks,
    /// improved cave connectivity, and better integration with terrain features.
    /// </summary>
    public class ImprovedCaveGenerator
    {
        private readonly CaveGenerationSettings _settings;
        private readonly Random _random;
        private readonly Dictionary<int, List<CaveSystem>> _caveSystems;
        private readonly Dictionary<int, List<CaveTunnel>> _caveTunnels;
        private readonly Dictionary<int, List<CaveChamber>> _caveChambers;
        
        public ImprovedCaveGenerator(CaveGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(_settings.Seed);
            _caveSystems = new Dictionary<int, List<CaveSystem>>();
            _caveTunnels = new Dictionary<int, List<CaveTunnel>>();
            _caveChambers = new Dictionary<int, List<CaveChamber>>();
        }
        
        /// <summary>
        /// Generate caves for a chunk
        /// </summary>
        public void GenerateCaves(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Initialize collections for this chunk
            _caveSystems[chunkKey] = new List<CaveSystem>();
            _caveTunnels[chunkKey] = new List<CaveTunnel>();
            _caveChambers[chunkKey] = new List<CaveChamber>();
            
            // Generate cave systems for this chunk
            GenerateCaveSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate cave tunnels
            GenerateCaveTunnels(chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate cave chambers
            GenerateCaveChambers(chunkX, chunkZ, heightMap, blockTypes);
            
            // Connect cave systems
            ConnectCaveSystems(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add water features to caves
            AddCaveWaterFeatures(chunkX, chunkZ, heightMap, blockTypes);
            
            // Add cave decorations
            AddCaveDecorations(chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate cave systems
        /// </summary>
        private void GenerateCaveSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var caveCount = _random.Next(_settings.MinCavesPerChunk, _settings.MaxCavesPerChunk + 1);
            
            for (int i = 0; i < caveCount; i++)
            {
                var caveSystem = new CaveSystem
                {
                    Id = _random.Next(),
                    CenterX = chunkX * 16 + _random.Next(16),
                    CenterY = _random.Next(_settings.MinCaveDepth, _settings.MaxCaveDepth),
                    CenterZ = chunkZ * 16 + _random.Next(16),
                    Radius = _random.Next(_settings.MinCaveRadius, _settings.MaxCaveRadius),
                    Complexity = _random.NextDouble() * _settings.CaveComplexityFactor,
                    Type = (CaveType)_random.Next(Enum.GetValues(typeof(CaveType)).Length)
                };
                
                _caveSystems[chunkKey].Add(caveSystem);
                
                // Generate cave system
                GenerateCaveSystem(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a single cave system
        /// </summary>
        private void GenerateCaveSystem(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate cave system based on type
            switch (caveSystem.Type)
            {
                case CaveType.Simple:
                    GenerateSimpleCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case CaveType.Branching:
                    GenerateBranchingCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case CaveType.Network:
                    GenerateNetworkCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
                case CaveType.Ravine:
                    GenerateRavineCave(caveSystem, chunkX, chunkZ, heightMap, blockTypes);
                    break;
            }
        }
        
        /// <summary>
        /// Generate a simple cave
        /// </summary>
        private void GenerateSimpleCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate a simple tunnel from the center
            var tunnel = new CaveTunnel
            {
                Id = _random.Next(),
                CaveSystemId = caveSystem.Id,
                StartX = caveSystem.CenterX,
                StartY = caveSystem.CenterY,
                StartZ = caveSystem.CenterZ,
                EndX = caveSystem.CenterX + _random.Next(-20, 21),
                EndY = caveSystem.CenterY + _random.Next(-10, 11),
                EndZ = caveSystem.CenterZ + _random.Next(-20, 21),
                Radius = caveSystem.Radius * 0.7f,
                Type = TunnelType.Main
            };
            
            _caveTunnels[chunkKey].Add(tunnel);
            
            // Carve the tunnel
            CarveTunnel(tunnel, chunkX, chunkZ, heightMap, blockTypes);
        }
        
        /// <summary>
        /// Generate a branching cave
        /// </summary>
        private void GenerateBranchingCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate main tunnel
            var mainTunnel = new CaveTunnel
            {
                Id = _random.Next(),
                CaveSystemId = caveSystem.Id,
                StartX = caveSystem.CenterX,
                StartY = caveSystem.CenterY,
                StartZ = caveSystem.CenterZ,
                EndX = caveSystem.CenterX + _random.Next(-30, 31),
                EndY = caveSystem.CenterY + _random.Next(-15, 16),
                EndZ = caveSystem.CenterZ + _random.Next(-30, 31),
                Radius = caveSystem.Radius * 0.8f,
                Type = TunnelType.Main
            };
            
            _caveTunnels[chunkKey].Add(mainTunnel);
            
            // Carve the main tunnel
            CarveTunnel(mainTunnel, chunkX, chunkZ, heightMap, blockTypes);
            
            // Generate branches
            var branchCount = (int)(caveSystem.Complexity * 5);
            for (int i = 0; i < branchCount; i++)
            {
                var branchTunnel = new CaveTunnel
                {
                    Id = _random.Next(),
                    CaveSystemId = caveSystem.Id,
                    StartX = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).X,
                    StartY = GetRandomPointOnLine(mainTunnel.StartY, mainTunnel.StartY, mainTunnel.EndY, mainTunnel.EndY).Y,
                    StartZ = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).Z,
                    EndX = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).X + _random.Next(-15, 16),
                    EndY = GetRandomPointOnLine(mainTunnel.StartY, mainTunnel.StartY, mainTunnel.EndY, mainTunnel.EndY).Y + _random.Next(-10, 11),
                    EndZ = GetRandomPointOnLine(mainTunnel.StartX, mainTunnel.StartZ, mainTunnel.EndX, mainTunnel.EndZ).Z + _random.Next(-15, 16),
                    Radius = caveSystem.Radius * 0.5f,
                    Type = TunnelType.Branch
                };
                
                _caveTunnels[chunkKey].Add(branchTunnel);
                
                // Carve the branch tunnel
                CarveTunnel(branchTunnel, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate a network cave
        /// </summary>
        private void GenerateNetworkCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate multiple interconnected tunnels
            var tunnelCount = (int)(caveSystem.Complexity * 8);
            var tunnelPositions = new List<(int x, int y, int z)>();
            
            // Generate initial positions
            for (int i = 0; i < tunnelCount; i++)
            {
                tunnelPositions.Add((
                    caveSystem.CenterX + _random.Next(-40, 41),
                    caveSystem.CenterY + _random.Next(-20, 21),
                    caveSystem.CenterZ + _random.Next(-40, 41)
                ));
            }
            
            // Connect positions with tunnels
            for (int i = 0; i < tunnelPositions.Count; i++)
            {
                for (int j = i + 1; j < tunnelPositions.Count; j++)
                {
                    // Connect some positions based on distance and complexity
                    var distance = Math.Sqrt(
                        Math.Pow(tunnelPositions[i].x - tunnelPositions[j].x, 2) +
                        Math.Pow(tunnelPositions[i].y - tunnelPositions[j].y, 2) +
                        Math.Pow(tunnelPositions[i].z - tunnelPositions[j].z, 2)
                    );
                    
                    if (distance < 30 * caveSystem.Complexity && _random.NextDouble() < 0.3)
                    {
                        var tunnel = new CaveTunnel
                        {
                            Id = _random.Next(),
                            CaveSystemId = caveSystem.Id,
                            StartX = tunnelPositions[i].x,
                            StartY = tunnelPositions[i].y,
                            StartZ = tunnelPositions[i].z,
                            EndX = tunnelPositions[j].x,
                            EndY = tunnelPositions[j].y,
                            EndZ = tunnelPositions[j].z,
                            Radius = caveSystem.Radius * 0.6f,
                            Type = TunnelType.Network
                        };
                        
                        _caveTunnels[chunkKey].Add(tunnel);
                        
                        // Carve the tunnel
                        CarveTunnel(tunnel, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a ravine cave
        /// </summary>
        private void GenerateRavineCave(CaveSystem caveSystem, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Generate a long, deep ravine
            var ravineLength = _random.Next(40, 80);
            var ravineDirection = _random.NextDouble() * Math.PI * 2;
            
            for (int i = 0; i < ravineLength; i++)
            {
                var x = caveSystem.CenterX + (int)(Math.Cos(ravineDirection) * i);
                var z = caveSystem.CenterZ + (int)(Math.Sin(ravineDirection) * i);
                var y = caveSystem.CenterY + _random.Next(-5, 6);
                
                // Create a vertical shaft at this position
                var shaft = new CaveTunnel
                {
                    Id = _random.Next(),
                    CaveSystemId = caveSystem.Id,
                    StartX = x,
                    StartY = Math.Max(0, y - 20),
                    StartZ = z,
                    EndX = x,
                    EndY = Math.Min(255, y + 20),
                    EndZ = z,
                    Radius = caveSystem.Radius * 0.4f,
                    Type = TunnelType.Ravine
                };
                
                _caveTunnels[chunkKey].Add(shaft);
                
                // Carve the shaft
                CarveTunnel(shaft, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Generate cave tunnels
        /// </summary>
        private void GenerateCaveTunnels(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            // Additional tunnel generation is handled in GenerateCaveSystems
        }
        
        /// <summary>
        /// Generate cave chambers
        /// </summary>
        private void GenerateCaveChambers(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            var chamberCount = _random.Next(_settings.MinChambersPerChunk, _settings.MaxChambersPerChunk + 1);
            
            for (int i = 0; i < chamberCount; i++)
            {
                var chamber = new CaveChamber
                {
                    Id = _random.Next(),
                    CenterX = chunkX * 16 + _random.Next(16),
                    CenterY = _random.Next(_settings.MinCaveDepth, _settings.MaxCaveDepth),
                    CenterZ = chunkZ * 16 + _random.Next(16),
                    RadiusX = _random.Next(_settings.MinChamberRadius, _settings.MaxChamberRadius),
                    RadiusY = _random.Next(_settings.MinChamberRadius, _settings.MaxChamberRadius),
                    RadiusZ = _random.Next(_settings.MinChamberRadius, _settings.MaxChamberRadius),
                    Type = (ChamberType)_random.Next(Enum.GetValues(typeof(ChamberType)).Length)
                };
                
                _caveChambers[chunkKey].Add(chamber);
                
                // Carve the chamber
                CarveChamber(chamber, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Connect cave systems
        /// </summary>
        private void ConnectCaveSystems(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Connect nearby cave systems
            for (int i = 0; i < _caveSystems[chunkKey].Count; i++)
            {
                for (int j = i + 1; j < _caveSystems[chunkKey].Count; j++)
                {
                    var system1 = _caveSystems[chunkKey][i];
                    var system2 = _caveSystems[chunkKey][j];
                    
                    var distance = Math.Sqrt(
                        Math.Pow(system1.CenterX - system2.CenterX, 2) +
                        Math.Pow(system1.CenterY - system2.CenterY, 2) +
                        Math.Pow(system1.CenterZ - system2.CenterZ, 2)
                    );
                    
                    // Connect systems if they're close enough
                    if (distance < _settings.CaveConnectionDistance && _random.NextDouble() < _settings.CaveConnectionProbability)
                    {
                        var connection = new CaveTunnel
                        {
                            Id = _random.Next(),
                            CaveSystemId = system1.Id,
                            StartX = system1.CenterX,
                            StartY = system1.CenterY,
                            StartZ = system1.CenterZ,
                            EndX = system2.CenterX,
                            EndY = system2.CenterY,
                            EndZ = system2.CenterZ,
                            Radius = Math.Min(system1.Radius, system2.Radius) * 0.5f,
                            Type = TunnelType.Connection
                        };
                        
                        _caveTunnels[chunkKey].Add(connection);
                        
                        // Carve the connection
                        CarveTunnel(connection, chunkX, chunkZ, heightMap, blockTypes);
                    }
                }
            }
        }
        
        /// <summary>
        /// Add water features to caves
        /// </summary>
        private void AddCaveWaterFeatures(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add water pools to chambers
            foreach (var chamber in _caveChambers[chunkKey])
            {
                if (chamber.Type == ChamberType.Flooded || (chamber.Type == ChamberType.Lake && _random.NextDouble() < 0.7))
                {
                    // Fill the bottom of the chamber with water
                    var waterLevel = chamber.CenterY - chamber.RadiusY / 2;
                    
                    for (int x = 0; x < 16; x++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            var worldX = chunkX * 16 + x;
                            var worldZ = chunkZ * 16 + z;
                            
                            // Check if this position is inside the chamber
                            var dx = (worldX - chamber.CenterX) / (double)chamber.RadiusX;
                            var dy = 0; // We're filling from the bottom
                            var dz = (worldZ - chamber.CenterZ) / (double)chamber.RadiusZ;
                            
                            if (dx * dx + dy * dy + dz * dz <= 1.0)
                            {
                                for (int y = 0; y < 256; y++)
                                {
                                    var index = y * 16 * 16 + z * 16 + x;
                                    
                                    // Check if this position is inside the chamber
                                    var dy3d = (y - chamber.CenterY) / (double)chamber.RadiusY;
                                    var dx3d = (worldX - chamber.CenterX) / (double)chamber.RadiusX;
                                    var dz3d = (worldZ - chamber.CenterZ) / (double)chamber.RadiusZ;
                                    
                                    if (dx3d * dx3d + dy3d * dy3d + dz3d * dz3d <= 1.0 && y <= waterLevel)
                                    {
                                        blockTypes[index] = (int)BlockType.Water;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Add water streams to tunnels
            foreach (var tunnel in _caveTunnels[chunkKey])
            {
                if (tunnel.Type == TunnelType.Ravine && _random.NextDouble() < 0.3)
                {
                    // Add a water stream at the bottom of the ravine
                    var steps = 20;
                    
                    for (int i = 0; i <= steps; i++)
                    {
                        var t = i / (double)steps;
                        var x = (int)(tunnel.StartX + t * (tunnel.EndX - tunnel.StartX));
                        var y = (int)(tunnel.StartY + t * (tunnel.EndY - tunnel.StartY));
                        var z = (int)(tunnel.StartZ + t * (tunnel.EndZ - tunnel.StartZ));
                        
                        // Find the bottom of the tunnel at this position
                        var bottomY = FindTunnelBottom(x, y, z, tunnel, chunkX, chunkZ, heightMap, blockTypes);
                        
                        if (bottomY >= 0)
                        {
                            var index = bottomY * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                blockTypes[index] = (int)BlockType.Water;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add cave decorations
        /// </summary>
        private void AddCaveDecorations(int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var chunkKey = GetChunkKey(chunkX, chunkZ);
            
            // Add decorations to chambers
            foreach (var chamber in _caveChambers[chunkKey])
            {
                switch (chamber.Type)
                {
                    case ChamberType.Stalactite:
                        AddStalactites(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                    case ChamberType.Stalagmite:
                        AddStalagmites(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                    case ChamberType.Crystal:
                        AddCrystals(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                    case ChamberType.Mushroom:
                        AddMushrooms(chamber, chunkX, chunkZ, heightMap, blockTypes);
                        break;
                }
            }
            
            // Add decorations to tunnels
            foreach (var tunnel in _caveTunnels[chunkKey])
            {
                if (_random.NextDouble() < _settings.CaveDecorationDensity)
                {
                    AddTunnelDecorations(tunnel, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Carve a tunnel
        /// </summary>
        private void CarveTunnel(CaveTunnel tunnel, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(tunnel.EndX - tunnel.StartX, 2) +
                Math.Pow(tunnel.EndY - tunnel.StartY, 2) +
                Math.Pow(tunnel.EndZ - tunnel.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = tunnel.StartX + t * (tunnel.EndX - tunnel.StartX);
                var y = tunnel.StartY + t * (tunnel.EndY - tunnel.StartY);
                var z = tunnel.StartZ + t * (tunnel.EndZ - tunnel.StartZ);
                
                // Carve a sphere at this position
                CarveSphere(x, y, z, tunnel.Radius, chunkX, chunkZ, heightMap, blockTypes);
            }
        }
        
        /// <summary>
        /// Carve a chamber
        /// </summary>
        private void CarveChamber(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    var worldX = chunkX * 16 + x;
                    var worldZ = chunkZ * 16 + z;
                    
                    for (int y = 0; y < 256; y++)
                    {
                        var dx = (worldX - chamber.CenterX) / (double)chamber.RadiusX;
                        var dy = (y - chamber.CenterY) / (double)chamber.RadiusY;
                        var dz = (worldZ - chamber.CenterZ) / (double)chamber.RadiusZ;
                        
                        // Check if this position is inside the ellipsoid
                        if (dx * dx + dy * dy + dz * dz <= 1.0)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Don't carve water blocks
                                if (blockTypes[index] != (int)BlockType.Water)
                                {
                                    blockTypes[index] = 0; // Air
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Carve a sphere
        /// </summary>
        private void CarveSphere(double centerX, double centerY, double centerZ, double radius, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var minX = Math.Max(0, (int)Math.Floor(centerX - radius) - chunkX * 16);
            var maxX = Math.Min(15, (int)Math.Ceiling(centerX + radius) - chunkX * 16);
            var minY = Math.Max(0, (int)Math.Floor(centerY - radius));
            var maxY = Math.Min(255, (int)Math.Ceiling(centerY + radius));
            var minZ = Math.Max(0, (int)Math.Floor(centerZ - radius) - chunkZ * 16);
            var maxZ = Math.Min(15, (int)Math.Ceiling(centerZ + radius) - chunkZ * 16);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var dx = (chunkX * 16 + x - centerX);
                        var dy = (y - centerY);
                        var dz = (chunkZ * 16 + z - centerZ);
                        
                        // Check if this position is inside the sphere
                        if (dx * dx + dy * dy + dz * dz <= radius * radius)
                        {
                            var index = y * 16 * 16 + z * 16 + x;
                            
                            if (index >= 0 && index < blockTypes.Length)
                            {
                                // Don't carve water blocks
                                if (blockTypes[index] != (int)BlockType.Water)
                                {
                                    blockTypes[index] = 0; // Air
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add stalactites to a chamber
        /// </summary>
        private void AddStalactites(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var stalactiteCount = _random.Next(5, 15);
            
            for (int i = 0; i < stalactiteCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dz * dz <= 0.8) // Only place near the edges
                {
                    // Find the ceiling of the chamber at this position
                    var ceilingY = FindChamberCeiling(x, chamber.CenterY, z, chamber, chunkX, chunkZ, heightMap, blockTypes);
                    
                    if (ceilingY >= 0)
                    {
                        // Grow a stalactite from the ceiling
                        var length = _random.Next(3, 8);
                        
                        for (int j = 0; j < length; j++)
                        {
                            var y = ceilingY - j;
                            var index = y * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                            
                            if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                            {
                                blockTypes[index] = (int)BlockType.Stone;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add stalagmites to a chamber
        /// </summary>
        private void AddStalagmites(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var stalagmiteCount = _random.Next(5, 15);
            
            for (int i = 0; i < stalagmiteCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dz * dz <= 0.8) // Only place near the edges
                {
                    // Find the floor of the chamber at this position
                    var floorY = FindChamberFloor(x, chamber.CenterY, z, chamber, chunkX, chunkZ, heightMap, blockTypes);
                    
                    if (floorY >= 0)
                    {
                        // Grow a stalagmite from the floor
                        var length = _random.Next(3, 8);
                        
                        for (int j = 0; j < length; j++)
                        {
                            var y = floorY + j;
                            var index = y * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                            
                            if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                            {
                                blockTypes[index] = (int)BlockType.Stone;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add crystals to a chamber
        /// </summary>
        private void AddCrystals(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var crystalCount = _random.Next(10, 30);
            
            for (int i = 0; i < crystalCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var y = chamber.CenterY + _random.Next(-chamber.RadiusY, chamber.RadiusY + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dy = (y - chamber.CenterY) / (double)chamber.RadiusY;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dy * dy + dz * dz <= 0.9)
                {
                    var index = y * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                    
                    if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                    {
                        blockTypes[index] = (int)BlockType.Diamond; // Use diamond as crystal placeholder
                    }
                }
            }
        }
        
        /// <summary>
        /// Add mushrooms to a chamber
        /// </summary>
        private void AddMushrooms(CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var mushroomCount = _random.Next(10, 25);
            
            for (int i = 0; i < mushroomCount; i++)
            {
                var x = chamber.CenterX + _random.Next(-chamber.RadiusX, chamber.RadiusX + 1);
                var z = chamber.CenterZ + _random.Next(-chamber.RadiusZ, chamber.RadiusZ + 1);
                
                // Check if this position is inside the chamber
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dz * dz <= 0.9)
                {
                    // Find the floor of the chamber at this position
                    var floorY = FindChamberFloor(x, chamber.CenterY, z, chamber, chunkX, chunkZ, heightMap, blockTypes);
                    
                    if (floorY >= 0)
                    {
                        var index = (floorY + 1) * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                        
                        if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                        {
                            blockTypes[index] = (int)BlockType.Mushroom;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add decorations to tunnels
        /// </summary>
        private void AddTunnelDecorations(CaveTunnel tunnel, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var steps = (int)Math.Sqrt(
                Math.Pow(tunnel.EndX - tunnel.StartX, 2) +
                Math.Pow(tunnel.EndY - tunnel.StartY, 2) +
                Math.Pow(tunnel.EndZ - tunnel.StartZ, 2)
            );
            
            for (int i = 0; i <= steps; i += 5) // Place decorations every 5 steps
            {
                var t = i / (double)steps;
                var x = tunnel.StartX + t * (tunnel.EndX - tunnel.StartX);
                var y = tunnel.StartY + t * (tunnel.EndY - tunnel.StartY);
                var z = tunnel.StartZ + t * (tunnel.EndZ - tunnel.StartZ);
                
                // Add ore veins
                if (_random.NextDouble() < 0.1)
                {
                    AddOreVein(x, y, z, tunnel.Radius * 0.3, chunkX, chunkZ, heightMap, blockTypes);
                }
                
                // Add cobwebs
                if (_random.NextDouble() < 0.05)
                {
                    AddCobwebs(x, y, z, tunnel.Radius * 0.5, chunkX, chunkZ, heightMap, blockTypes);
                }
            }
        }
        
        /// <summary>
        /// Add an ore vein
        /// </summary>
        private void AddOreVein(double centerX, double centerY, double centerZ, double radius, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var oreType = _random.Next(1, 6); // Different ore types
            var oreCount = _random.Next(3, 8);
            
            for (int i = 0; i < oreCount; i++)
            {
                var x = centerX + _random.Next(-3, 4);
                var y = centerY + _random.Next(-3, 4);
                var z = centerZ + _random.Next(-3, 4);
                
                var index = (int)y * 16 * 16 + ((int)z - chunkZ * 16) * 16 + ((int)x - chunkX * 16);
                
                if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                {
                    blockTypes[index] = oreType;
                }
            }
        }
        
        /// <summary>
        /// Add cobwebs
        /// </summary>
        private void AddCobwebs(double centerX, double centerY, double centerZ, double radius, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            var webCount = _random.Next(2, 5);
            
            for (int i = 0; i < webCount; i++)
            {
                var x = centerX + _random.Next(-2, 3);
                var y = centerY + _random.Next(-2, 3);
                var z = centerZ + _random.Next(-2, 3);
                
                var index = (int)y * 16 * 16 + ((int)z - chunkZ * 16) * 16 + ((int)x - chunkX * 16);
                
                if (index >= 0 && index < blockTypes.Length && blockTypes[index] == 0)
                {
                    blockTypes[index] = (int)BlockType.Cobweb;
                }
            }
        }
        
        /// <summary>
        /// Find the bottom of a tunnel at a position
        /// </summary>
        private int FindTunnelBottom(int x, int y, int z, CaveTunnel tunnel, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int i = y; i >= 0; i--)
            {
                var index = i * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                
                if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                {
                    return i + 1;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Find the ceiling of a chamber at a position
        /// </summary>
        private int FindChamberCeiling(int x, int y, int z, CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int i = y + chamber.RadiusY; i >= y; i--)
            {
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dy = (i - chamber.CenterY) / (double)chamber.RadiusY;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dy * dy + dz * dz <= 1.0)
                {
                    var index = i * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                    
                    if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                    {
                        return i - 1;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Find the floor of a chamber at a position
        /// </summary>
        private int FindChamberFloor(int x, int y, int z, CaveChamber chamber, int chunkX, int chunkZ, int[] heightMap, int[] blockTypes)
        {
            for (int i = y - chamber.RadiusY; i <= y; i++)
            {
                var dx = (x - chamber.CenterX) / (double)chamber.RadiusX;
                var dy = (i - chamber.CenterY) / (double)chamber.RadiusY;
                var dz = (z - chamber.CenterZ) / (double)chamber.RadiusZ;
                
                if (dx * dx + dy * dy + dz * dz <= 1.0)
                {
                    var index = i * 16 * 16 + (z - chunkZ * 16) * 16 + (x - chunkX * 16);
                    
                    if (index >= 0 && index < blockTypes.Length && blockTypes[index] != 0)
                    {
                        return i + 1;
                    }
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Get a random point on a line
        /// </summary>
        private (int X, int Y, int Z) GetRandomPointOnLine(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            var t = _random.NextDouble();
            return (
                (int)(x1 + t * (x2 - x1)),
                (int)(y1 + t * (y2 - y1)),
                (int)(z1 + t * (z2 - z1))
            );
        }
        
        /// <summary>
        /// Get a unique key for a chunk
        /// </summary>
        private int GetChunkKey(int chunkX, int chunkZ)
        {
            return chunkX * 1000000 + chunkZ;
        }
    }
    
    #region Supporting Classes and Enums
    
    /// <summary>
    /// Cave system information
    /// </summary>
    public class CaveSystem
    {
        public int Id { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterZ { get; set; }
        public int Radius { get; set; }
        public double Complexity { get; set; }
        public CaveType Type { get; set; }
    }
    
    /// <summary>
    /// Cave tunnel information
    /// </summary>
    public class CaveTunnel
    {
        public int Id { get; set; }
        public int CaveSystemId { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int EndZ { get; set; }
        public float Radius { get; set; }
        public TunnelType Type { get; set; }
    }
    
    /// <summary>
    /// Cave chamber information
    /// </summary>
    public class CaveChamber
    {
        public int Id { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int CenterZ { get; set; }
        public int RadiusX { get; set; }
        public int RadiusY { get; set; }
        public int RadiusZ { get; set; }
        public ChamberType Type { get; set; }
    }
    
    /// <summary>
    /// Cave types
    /// </summary>
    public enum CaveType
    {
        Simple,
        Branching,
        Network,
        Ravine
    }
    
    /// <summary>
    /// Tunnel types
    /// </summary>
    public enum TunnelType
    {
        Main,
        Branch,
        Network,
        Connection,
        Ravine
    }
    
    /// <summary>
    /// Chamber types
    /// </summary>
    public enum ChamberType
    {
        Empty,
        Stalactite,
        Stalagmite,
        Crystal,
        Mushroom,
        Lake,
        Flooded
    }
    
    /// <summary>
    /// Block types
    /// </summary>
    public enum BlockType
    {
        Air = 0,
        Stone = 1,
        Dirt = 2,
        Grass = 3,
        Water = 4,
        Sand = 5,
        Gravel = 6,
        Wood = 7,
        Leaves = 8,
        Coal = 9,
        Iron = 10,
        Gold = 11,
        Diamond = 12,
        Mushroom = 13,
        Cobweb = 14
    }
    
    #endregion
}
}
                {
                    var distSq = x * x + z * z;
                    if (distSq <= radius * radius)
                    {
                        var worldX = centerX + x;
                        var worldZ = centerZ + z;
                        
                        for (int y = chamber.StartY; y < chamber.StartY + chamber.Height; y++)
                        {
                            if (IsInChunkBounds(worldX, y, worldZ))
                            {
                                chunk.CaveMask[worldX, y, worldZ] = true;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Carve a tunnel into the terrain
        /// </summary>
        private void CarveTunnel(ChunkData chunk, CaveTunnel tunnel)
        {
            foreach (var point in tunnel.Path)
            {
                if (!IsInChunkBounds(point.X, point.Y, point.Z))
                    continue;
                    
                var radius = (int)(tunnel.Width / 2.0);
                
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        var distSq = dx * dx + dz * dz;
                        if (distSq <= radius * radius)
                        {
                            var worldX = point.X + dx;
                            var worldZ = point.Z + dz;
                            
                            for (int y = point.Y - 1; y <= point.Y + 1; y++)
                            {
                                if (IsInChunkBounds(worldX, y, worldZ))
                                {
                                    chunk.CaveMask[worldX, y, worldZ] = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add water features to caves
        /// </summary>
        private void AddCaveWaterFeatures(ChunkData chunk, CaveSystem system)
        {
            // Add underground lakes and water streams
            var waterRandom = _worldManager.GetChunkRandom(0, 0, system.GetHashCode());
            
            foreach (var chamber in system.Chambers)
            {
                if (waterRandom.NextDouble() < 0.3) // 30% chance of water in chamber
                {
                    var waterLevel = _worldManager.GlobalWaterLevel - 2;
                    var centerX = (chamber.StartX + chamber.EndX) / 2;
                    var centerZ = (chamber.StartZ + chamber.EndZ) / 2;
                    
                    // Fill lower part of chamber with water
                    for (int x = chamber.StartX; x < chamber.EndX; x++)
                    {
                        for (int z = chamber.StartZ; z < chamber.EndZ; z++)
                        {
                            var distFromCenter = Math.Sqrt(
                                Math.Pow(x - centerX, 2) + Math.Pow(z - centerZ, 2));
                                
                            if (distFromCenter <= chamber.Radius * 0.7)
                            {
                                for (int y = chamber.StartY; y < waterLevel; y++)
                                {
                                    if (IsInChunkBounds(x, y, z))
                                    {
                                        chunk.SetBlock(x, y, z, BlockType.Water);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Add water streams in tunnels
            foreach (var tunnel in system.Tunnels)
            {
                if (waterRandom.NextDouble() < 0.15) // 15% chance of stream
                {
                    AddWaterStream(chunk, tunnel, waterRandom);
                }
            }
        }
        
        /// <summary>
        /// Add a water stream following tunnel path
        /// </summary>
        private void AddWaterStream(ChunkData chunk, CaveTunnel tunnel, Random random)
        {
            var streamWidth = 1;
            var streamDepth = 1;
            
            for (int i = 0; i < tunnel.Path.Count; i += 5)
            {
                var point = tunnel.Path[Math.Min(i, tunnel.Path.Count - 1)];
                
                for (int dx = -streamWidth; dx <= streamWidth; dx++)
                {
                    for (int dz = -streamWidth; dz <= streamWidth; dz++)
                    {
                        var worldX = point.X + dx;
                        var worldZ = point.Z + dz;
                        var worldY = point.Y - streamDepth;
                        
                        if (IsInChunkBounds(worldX, worldY, worldZ))
                        {
                            chunk.SetBlock(worldX, worldY, worldZ, BlockType.Water);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if world coordinates are within chunk bounds
        /// </summary>
        private bool IsInChunkBounds(int worldX, int worldY, int worldZ)
        {
            return worldX >= 0 && worldX < 16 && 
                   worldY >= 0 && worldY < 256 && 
                   worldZ >= 0 && worldZ < 16;
        }
        
        /// <summary>
        /// Simple Perlin noise implementation
        /// </summary>
        private static double PerlinNoise.Generate(double x, double y, int octaves, double persistence, double scale, int seed)
        {
            var total = 0.0;
            var amplitude = 1.0;
            var maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                var freq = Math.Pow(2, i);
                var perlinValue = Noise2D(x * freq / scale, y * freq / scale, seed + i);
                total += perlinValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// 2D Simplex noise implementation
        /// </summary>
        private static double SimplexNoise.Generate(double x, double y, int octaves, double persistence, double scale, int seed)
        {
            // Simplified Simplex noise for cave generation
            var total = 0.0;
            var amplitude = 1.0;
            var maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                var freq = Math.Pow(2, i);
                var simplexValue = SimplexNoise2D(x * freq / scale, y * freq / scale, seed + i);
                total += simplexValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// 2D Simplex noise calculation
        /// </summary>
        private static double SimplexNoise2D(double x, double y, int seed)
        {
            // Simplified 2D Simplex noise
            var F2 = 0.5 * (Math.Sqrt(3.0) - 1.0);
            var G2 = (3.0 - Math.Sqrt(3.0)) / 6.0;
            var H2 = (Math.Sqrt(3.0) - 1.0) / 3.0;
            
            var s = (seed & 0xFF);
            var i = (s & 15) >> 1;
            var j = (s & 7);
            var xi = x + i + (s & 8);
            var yi = y + i + (s & 8);
            
            var n = xi + yi * 37;
            var a = n - (n << 1);
            var b = n - (n << 2);
            var c = n - (n << 3);
            var t = 0.6 - x * x - y * y;
            
            var t0 = (a | b | c) * t;
            var t1 = (a | b | c) * (t - 3.0);
            var t2 = (a | b | c) * (t - 6.0);
            var t3 = (a | b | c) * (t - 9.0);
            var t4 = (a | b | c) * (t - 12.0);
            
            return t0 + t1 + t2 + t3 + t4;
        }
        
        /// <summary>
        /// 2D noise function
        /// </summary>
        private static double Noise2D(int x, int y, int seed)
        {
            var n = x + y * 57 + seed * 131;
            n = (n << 13) ^ n;
            return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff)) / 1073741824.0);
        }
    }
    
    /// <summary>
    /// Represents a complete cave system with chambers and tunnels
    /// </summary>
    public class CaveSystem
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int Size { get; set; }
        public double Complexity { get; set; }
        public bool HasWater { get; set; }
        public List<CaveChamber> Chambers { get; set; } = new();
        public List<CaveTunnel> Tunnels { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a cave chamber
    /// </summary>
    public class CaveChamber
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int EndZ { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public int Radius { get; set; }
        public int ShapeType { get; set; }
    }
    
    /// <summary>
    /// Represents a cave tunnel
    /// </summary>
    public class CaveTunnel
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int EndZ { get; set; }
        public double Width { get; set; }
        public int Length { get; set; }
        public List<TunnelPoint> Path { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point in a tunnel path
    /// </summary>
    public class TunnelPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}
}

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Enhanced cave generation system with multi-layered cave networks,
    /// improved cave connectivity, and better integration with terrain features.
    /// </summary>
    public class ImprovedCaveGenerator
    {
        private readonly WorldManager _worldManager;
        private readonly Random _random;
        private readonly CaveGenerationSettings _settings;
        
        // Cave generation parameters
        private const int CaveSystemMinSize = 50;
        private const int CaveSystemMaxSize = 200;
        private const double CaveTunnelMinWidth = 2.0;
        private const double CaveTunnelMaxWidth = 8.0;
        private const double CaveChamberMinRadius = 4.0;
        private const double CaveChamberMaxRadius = 12.0;
        private const double CaveVerticalVariation = 0.3;
        private const double CaveHorizontalVariation = 0.4;
        private const int CaveMaxDepth = 80;
        private const double CaveRoughness = 0.15;
        
        public ImprovedCaveGenerator(WorldManager worldManager)
        {
            _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
            _random = worldManager.GetChunkRandom(0, 0, 0);
            _settings = worldManager._caveSettings;
        }
        
        /// <summary>
        /// Generate enhanced cave system for a chunk
        /// </summary>
        public void GenerateCaves(ChunkData chunk, int chunkX, int chunkZ)
        {
            if (!_worldManager._enableCaves || !_worldManager._useImprovedCaves)
                return;
                
            var caveSystems = GenerateCaveSystems(chunkX, chunkZ);
            
            foreach (var caveSystem in caveSystems)
            {
                GenerateCaveSystem(chunk, caveSystem);
            }
        }
        
        /// <summary>
        /// Generate multiple cave systems with improved connectivity and variety
        /// </summary>
        private List<CaveSystem> GenerateCaveSystems(int chunkX, int chunkZ)
        {
            var systems = new List<CaveSystem>();
            var worldSeed = _worldManager.GetWorldSeed();
            
            // Determine number of cave systems based on terrain characteristics
            int systemCount = DetermineCaveSystemCount(chunkX, chunkZ);
            
            for (int i = 0; i < systemCount; i++)
            {
                var system = GenerateSingleCaveSystem(chunkX, chunkZ, i, systemCount, worldSeed);
                if (system != null)
                {
                    systems.Add(system);
                }
            }
            
            return systems;
        }
        
        /// <summary>
        /// Determine appropriate number of cave systems for this chunk
        /// </summary>
        private int DetermineCaveSystemCount(int chunkX, int chunkZ)
        {
            // Use terrain characteristics to influence cave density
            var baseRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, 123);
            var terrainFactor = GetTerrainCaveFactor(chunkX, chunkZ);
            
            int baseCount = (int)(baseRandom.NextDouble() * 3.0 * terrainFactor);
            return Math.Clamp(baseCount, 0, 3);
        }
        
        /// <summary>
        /// Get terrain factor that influences cave generation
        /// </summary>
        private double GetTerrainCaveFactor(int chunkX, int chunkZ)
        {
            // Sample terrain height and moisture to determine cave suitability
            var sampleX = chunkX * 16 + 8;
            var sampleZ = chunkZ * 16 + 8;
            
            var continentalness = SimplexNoise.Generate(sampleX * 0.003f, sampleZ * 0.003f, 0, 5, 1.0, 934113);
            var moisture = SimplexNoise.Generate(sampleX * 0.007f + 100, sampleZ * 0.007f + 100, 0, 3, 1.0, 512773);
            
            // Caves prefer areas with moderate elevation and higher moisture
            var elevationFactor = Math.Max(0.3, 1.0 - Math.Abs(continentalness - 0.5) * 2.0);
            var moistureFactor = Math.Min(1.5, moisture + 0.5);
            
            return elevationFactor * moistureFactor;
        }
        
        /// <summary>
        /// Generate a single cave system with multiple connected chambers
        /// </summary>
        private CaveSystem? GenerateSingleCaveSystem(int chunkX, int chunkZ, int systemIndex, int totalSystems, WorldSeedConfig worldSeed)
        {
            var systemRandom = _worldManager.GetChunkRandom(chunkX, chunkZ, systemIndex * 100 + totalSystems * 1000);
            
            // Determine cave system parameters
            var systemSize = systemRandom.Next(CaveSystemMinSize, CaveSystemMaxSize);
            var complexity = systemRandom.NextDouble() * 0.7 + 0.3; // 0.3 to 1.0 complexity
            var hasWater = systemRandom.NextDouble() < 0.4; // 40% chance of water features
            
            // Generate starting point
            var startX = systemRandom.Next(2, 14);
            var startZ = systemRandom.Next(2, 14);
            var startY = systemRandom.Next(20, 120);
            
            var system = new CaveSystem
            {
                StartX = startX,
                StartY = startY,
                StartZ = startZ,
                Size = systemSize,
                Complexity = complexity,
                HasWater = hasWater,
                Chambers = new List<CaveChamber>(),
                Tunnels = new List<CaveTunnel>()
            };
            
            // Generate cave structure
            GenerateCaveStructure(system, systemRandom);
            
            return system;
        }
        
        /// <summary>
        /// Generate the internal structure of a cave system
        /// </summary>
        private void GenerateCaveStructure(CaveSystem system, Random systemRandom)
        {
            var remainingSize = system.Size;
            var currentX = system.StartX;
            var currentY = system.StartY;
            var currentZ = system.StartZ;
            
            // Generate main chamber
            var chamberSize = (int)(system.Size * system.Complexity * 0.4);
            if (chamberSize > 10)
            {
                var mainChamber = GenerateChamber(currentX, currentY, currentZ, chamberSize, systemRandom, true);
                system.Chambers.Add(mainChamber);
                
                currentX = mainChamber.EndX;
                currentY = mainChamber.EndY;
                currentZ = mainChamber.EndZ;
                remainingSize -= chamberSize;
            }
            
            // Generate connecting tunnels
            var tunnelCount = Math.Max(1, (int)(system.Complexity * 2.5));
            for (int i = 0; i < tunnelCount && remainingSize > 5; i++)
            {
                var tunnelLength = systemRandom.Next(remainingSize / tunnelCount, remainingSize / 2);
                var tunnel = GenerateTunnel(currentX, currentY, currentZ, tunnelLength, systemRandom);
                
                if (tunnel != null)
                {
                    system.Tunnels.Add(tunnel);
                    currentX = tunnel.EndX;
                    currentY = tunnel.EndY;
                    currentZ = tunnel.EndZ;
                    remainingSize -= tunnelLength;
                }
            }
            
            // Generate secondary chambers
            while (remainingSize > 15 && systemRandom.NextDouble() < system.Complexity)
            {
                var secondarySize = systemRandom.Next(8, Math.Min(20, remainingSize / 2));
                var secondaryChamber = GenerateChamber(currentX, currentY, currentZ, secondarySize, systemRandom, false);
                
                if (secondaryChamber != null)
                {
                    system.Chambers.Add(secondaryChamber);
                    
                    // Connect with tunnel
                    var connectTunnel = GenerateTunnel(
                        secondaryChamber.EndX, secondaryChamber.EndY, secondaryChamber.EndZ,
                        systemRandom.Next(3, 8), systemRandom);
                    
                    if (connectTunnel != null)
                    {
                        system.Tunnels.Add(connectTunnel);
                        currentX = connectTunnel.EndX;
                        currentY = connectTunnel.EndY;
                        currentZ = connectTunnel.EndZ;
                        remainingSize -= secondarySize + connectTunnel.Length;
                    }
                }
            }
        }
        
        /// <summary>
        /// Generate a cave chamber with improved geometry
        /// </summary>
        private CaveChamber? GenerateChamber(int startX, int startY, int startZ, int size, Random random, bool isMain)
        {
            var radius = isMain 
                ? random.NextDouble() * (CaveChamberMaxRadius - CaveChamberMinRadius) + CaveChamberMinRadius
                : random.NextDouble() * (CaveChamberMaxRadius - CaveChamberMinRadius) * 0.7 + CaveChamberMinRadius;
            
            var height = (int)(radius * random.NextDouble() * 1.5 + 1.0);
            var width = (int)(radius * 2.0);
            var depth = (int)(radius * random.NextDouble() * 1.2 + 1.0);
            
            // Generate chamber shape
            var shapeType = random.Next(0, 3);
            var endX = startX;
            var endY = startY;
            var endZ = startZ;
            
            switch (shapeType)
            {
                case 0: // Elliptical
                    endX = startX + (int)(width * random.NextDouble() * 0.8 + 0.2);
                    endZ = startZ + (int)(depth * random.NextDouble() * 0.8 + 0.2);
                    break;
                    
                case 1: // Circular
                    var angle = random.NextDouble() * Math.PI * 2.0;
                    endX = startX + (int)(Math.Cos(angle) * width * 0.5);
                    endZ = startZ + (int)(Math.Sin(angle) * depth * 0.5);
                    break;
                    
                case 2: // Irregular
                    endX = startX + random.Next(-width/2, width/2);
                    endZ = startZ + random.Next(-depth/2, depth/2);
                    endY = startY + random.Next(-height/4, height/4);
                    break;
            }
            
            return new CaveChamber
            {
                StartX = startX,
                StartY = startY,
                StartZ = startZ,
                EndX = endX,
                EndY = endY,
                EndZ = endZ,
                Width = width,
                Height = height,
                Depth = depth,
                Radius = (int)radius,
                ShapeType = shapeType
            };
        }
        
        /// <summary>
        /// Generate a tunnel with improved pathing
        /// </summary>
        private CaveTunnel? GenerateTunnel(int startX, int startY, int startZ, int length, Random random)
        {
            if (length < 3)
                return null;
                
            var width = random.NextDouble() * (CaveTunnelMaxWidth - CaveTunnelMinWidth) + CaveTunnelMinWidth;
            var verticalVariation = (int)(CaveVerticalVariation * length);
            var horizontalVariation = (int)(CaveHorizontalVariation * length);
            
            var path = GenerateTunnelPath(startX, startZ, length, width, verticalVariation, horizontalVariation, random);
            if (path == null)
                return null;
                
            return new CaveTunnel
            {
                StartX = startX,
                StartY = startY,
                StartZ = startZ,
                EndX = path[^1].X,
                EndY = startY,
                EndZ = path[^1].Z,
                Width = (int)width,
                Length = length,
                Path = path
            };
        }
        
        /// <summary>
        /// Generate tunnel path with natural curves
        /// </summary>
        private List<TunnelPoint>? GenerateTunnelPath(int startX, int startZ, int length, double width, 
            int verticalVariation, int horizontalVariation, Random random)
        {
            var path = new List<TunnelPoint>();
            var currentX = startX;
            var currentZ = startZ;
            var currentY = startY;
            
            for (int i = 0; i <= length; i++)
            {
                // Add natural curves using Perlin noise
                var noiseX = (i / (double)length) * 4.0;
                var noiseZ = (i / (double)length) * 4.0;
                var curveX = PerlinNoise.Generate(noiseX, noiseZ, 0, 2, 1.0, 934113);
                var curveZ = PerlinNoise.Generate(noiseX + 100, noiseZ + 100, 0, 2, 1.0, 512773);
                
                var offsetX = (curveX * horizontalVariation) + (random.NextDouble() - 0.5) * CaveRoughness;
                var offsetZ = (curveZ * horizontalVariation) + (random.NextDouble() - 0.5) * CaveRoughness;
                var offsetY = (int)(Math.Sin(i * 0.1) * verticalVariation + (random.NextDouble() - 0.5) * CaveRoughness);
                
                currentX = startX + (int)(i * width * 0.5 + offsetX);
                currentZ = startZ + (int)(i * width * 0.5 + offsetZ);
                currentY += offsetY;
                
                path.Add(new TunnelPoint
                {
                    X = currentX,
                    Y = currentY,
                    Z = currentZ
                });
            }
            
            return path;
        }
        
        /// <summary>
        /// Apply cave system to chunk data
        /// </summary>
        private void GenerateCaveSystem(ChunkData chunk, CaveSystem system)
        {
            foreach (var chamber in system.Chambers)
            {
                CarveChamber(chunk, chamber);
            }
            
            foreach (var tunnel in system.Tunnels)
            {
                CarveTunnel(chunk, tunnel);
            }
            
            // Add water features if enabled
            if (system.HasWater)
            {
                AddCaveWaterFeatures(chunk, system);
            }
        }
        
        /// <summary>
        /// Carve a chamber into the terrain
        /// </summary>
        private void CarveChamber(ChunkData chunk, CaveChamber chamber)
        {
            var centerX = (chamber.StartX + chamber.EndX) / 2;
            var centerZ = (chamber.StartZ + chamber.EndZ) / 2;
            var radius = chamber.Radius;
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    var distSq = x * x + z * z;
                    if (distSq <= radius * radius)
                    {
                        var worldX = centerX + x;
                        var worldZ = centerZ + z;
                        
                        for (int y = chamber.StartY; y < chamber.StartY + chamber.Height; y++)
                        {
                            if (IsInChunkBounds(worldX, y, worldZ))
                            {
                                chunk.CaveMask[worldX, y, worldZ] = true;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Carve a tunnel into the terrain
        /// </summary>
        private void CarveTunnel(ChunkData chunk, CaveTunnel tunnel)
        {
            foreach (var point in tunnel.Path)
            {
                if (!IsInChunkBounds(point.X, point.Y, point.Z))
                    continue;
                    
                var radius = (int)(tunnel.Width / 2.0);
                
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        var distSq = dx * dx + dz * dz;
                        if (distSq <= radius * radius)
                        {
                            var worldX = point.X + dx;
                            var worldZ = point.Z + dz;
                            
                            for (int y = point.Y - 1; y <= point.Y + 1; y++)
                            {
                                if (IsInChunkBounds(worldX, y, worldZ))
                                {
                                    chunk.CaveMask[worldX, y, worldZ] = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Add water features to caves
        /// </summary>
        private void AddCaveWaterFeatures(ChunkData chunk, CaveSystem system)
        {
            // Add underground lakes and water streams
            var waterRandom = _worldManager.GetChunkRandom(0, 0, system.GetHashCode());
            
            foreach (var chamber in system.Chambers)
            {
                if (waterRandom.NextDouble() < 0.3) // 30% chance of water in chamber
                {
                    var waterLevel = _worldManager.GlobalWaterLevel - 2;
                    var centerX = (chamber.StartX + chamber.EndX) / 2;
                    var centerZ = (chamber.StartZ + chamber.EndZ) / 2;
                    
                    // Fill lower part of chamber with water
                    for (int x = chamber.StartX; x < chamber.EndX; x++)
                    {
                        for (int z = chamber.StartZ; z < chamber.EndZ; z++)
                        {
                            var distFromCenter = Math.Sqrt(
                                Math.Pow(x - centerX, 2) + Math.Pow(z - centerZ, 2));
                                
                            if (distFromCenter <= chamber.Radius * 0.7)
                            {
                                for (int y = chamber.StartY; y < waterLevel; y++)
                                {
                                    if (IsInChunkBounds(x, y, z))
                                    {
                                        chunk.SetBlock(x, y, z, BlockType.Water);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Add water streams in tunnels
            foreach (var tunnel in system.Tunnels)
            {
                if (waterRandom.NextDouble() < 0.15) // 15% chance of stream
                {
                    AddWaterStream(chunk, tunnel, waterRandom);
                }
            }
        }
        
        /// <summary>
        /// Add a water stream following tunnel path
        /// </summary>
        private void AddWaterStream(ChunkData chunk, CaveTunnel tunnel, Random random)
        {
            var streamWidth = 1;
            var streamDepth = 1;
            
            for (int i = 0; i < tunnel.Path.Count; i += 5)
            {
                var point = tunnel.Path[Math.Min(i, tunnel.Path.Count - 1)];
                
                for (int dx = -streamWidth; dx <= streamWidth; dx++)
                {
                    for (int dz = -streamWidth; dz <= streamWidth; dz++)
                    {
                        var worldX = point.X + dx;
                        var worldZ = point.Z + dz;
                        var worldY = point.Y - streamDepth;
                        
                        if (IsInChunkBounds(worldX, worldY, worldZ))
                        {
                            chunk.SetBlock(worldX, worldY, worldZ, BlockType.Water);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if world coordinates are within chunk bounds
        /// </summary>
        private bool IsInChunkBounds(int worldX, int worldY, int worldZ)
        {
            return worldX >= 0 && worldX < 16 && 
                   worldY >= 0 && worldY < 256 && 
                   worldZ >= 0 && worldZ < 16;
        }
        
        /// <summary>
        /// Simple Perlin noise implementation
        /// </summary>
        private static double PerlinNoise.Generate(double x, double y, int octaves, double persistence, double scale, int seed)
        {
            var total = 0.0;
            var amplitude = 1.0;
            var maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                var freq = Math.Pow(2, i);
                var perlinValue = Noise2D(x * freq / scale, y * freq / scale, seed + i);
                total += perlinValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// 2D Simplex noise implementation
        /// </summary>
        private static double SimplexNoise.Generate(double x, double y, int octaves, double persistence, double scale, int seed)
        {
            // Simplified Simplex noise for cave generation
            var total = 0.0;
            var amplitude = 1.0;
            var maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                var freq = Math.Pow(2, i);
                var simplexValue = SimplexNoise2D(x * freq / scale, y * freq / scale, seed + i);
                total += simplexValue * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// 2D Simplex noise calculation
        /// </summary>
        private static double SimplexNoise2D(double x, double y, int seed)
        {
            // Simplified 2D Simplex noise
            var F2 = 0.5 * (Math.Sqrt(3.0) - 1.0);
            var G2 = (3.0 - Math.Sqrt(3.0)) / 6.0;
            var H2 = (Math.Sqrt(3.0) - 1.0) / 3.0;
            
            var s = (seed & 0xFF);
            var i = (s & 15) >> 1;
            var j = (s & 7);
            var xi = x + i + (s & 8);
            var yi = y + i + (s & 8);
            
            var n = xi + yi * 37;
            var a = n - (n << 1);
            var b = n - (n << 2);
            var c = n - (n << 3);
            var t = 0.6 - x * x - y * y;
            
            var t0 = (a | b | c) * t;
            var t1 = (a | b | c) * (t - 3.0);
            var t2 = (a | b | c) * (t - 6.0);
            var t3 = (a | b | c) * (t - 9.0);
            var t4 = (a | b | c) * (t - 12.0);
            
            return t0 + t1 + t2 + t3 + t4;
        }
        
        /// <summary>
        /// 2D noise function
        /// </summary>
        private static double Noise2D(int x, int y, int seed)
        {
            var n = x + y * 57 + seed * 131;
            n = (n << 13) ^ n;
            return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff)) / 1073741824.0);
        }
    }
    
    /// <summary>
    /// Represents a complete cave system with chambers and tunnels
    /// </summary>
    public class CaveSystem
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int Size { get; set; }
        public double Complexity { get; set; }
        public bool HasWater { get; set; }
        public List<CaveChamber> Chambers { get; set; } = new();
        public List<CaveTunnel> Tunnels { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a cave chamber
    /// </summary>
    public class CaveChamber
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int EndZ { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public int Radius { get; set; }
        public int ShapeType { get; set; }
    }
    
    /// <summary>
    /// Represents a cave tunnel
    /// </summary>
    public class CaveTunnel
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StartZ { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
        public int EndZ { get; set; }
        public double Width { get; set; }
        public int Length { get; set; }
        public List<TunnelPoint> Path { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a point in a tunnel path
    /// </summary>
    public class TunnelPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}
