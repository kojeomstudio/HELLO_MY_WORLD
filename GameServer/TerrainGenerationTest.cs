using System;
using System.IO;
using GameServerApp.World.Generation;

namespace GameServerApp
{
    /// <summary>
    /// Simple test program for terrain generation
    /// </summary>
    public class TerrainGenerationTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting terrain generation test...");
            
            try
            {
                // Load configuration
                var configPath = "World/Generation/WorldGenerationConfig.json";
                var config = WorldGenerationConfig.LoadConfig(configPath);
                
                Console.WriteLine($"Loaded configuration from {configPath}");
                Console.WriteLine($"Biome types: {config.BiomeConfig.Biomes.Count}");
                Console.WriteLine($"Ore types: {config.OreDistributionConfig.OreTypes.Count}");
                Console.WriteLine($"Structure types: {config.StructureGenerationConfig.StructureTypes.Count}");
                Console.WriteLine($"Entity types: {config.EntitySpawnConfig.EntityTypes.Count}");
                
                // Create world generation manager
                var manager = new WorldGenerationManager(config);
                
                // Generate a test chunk
                var context = manager.GenerateChunk(0, 0, 12345);
                
                Console.WriteLine($"Generated chunk (0,0) with seed 12345");
                Console.WriteLine($"Height map size: {context.HeightMap.GetLength(0)}x{context.HeightMap.GetLength(1)}");
                Console.WriteLine($"Biome map size: {context.BiomeData.GetLength(0)}x{context.BiomeData.GetLength(1)}");
                Console.WriteLine($"Block types size: {context.BlockTypes.GetLength(0)}x{context.BlockTypes.GetLength(1)}x{context.BlockTypes.GetLength(2)}");
                
                // Test biome generation
                var biomeCount = new int[Enum.GetNames(typeof(BiomeType)).Length];
                for (int x = 0; x < context.ChunkSize; x++)
                {
                    for (int z = 0; z < context.ChunkSize; z++)
                    {
                        var biome = context.GetBiome(x, z);
                        biomeCount[(int)biome]++;
                    }
                }
                
                Console.WriteLine("Biome distribution:");
                foreach (BiomeType biome in Enum.GetValues(typeof(BiomeType)))
                {
                    Console.WriteLine($"  {biome}: {biomeCount[(int)biome]}");
                }
                
                Console.WriteLine("Terrain generation test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during terrain generation test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}using System.IO;
using GameServerApp.World.Generation;

namespace GameServerApp
{
    /// <summary>
    /// Simple test program for terrain generation
    /// </summary>
    public class TerrainGenerationTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting terrain generation test...");
            
            try
            {
                // Load configuration
                var configPath = "World/Generation/WorldGenerationConfig.json";
                var config = WorldGenerationConfig.LoadConfig(configPath);
                
                Console.WriteLine($"Loaded configuration from {configPath}");
                Console.WriteLine($"Biome types: {config.BiomeConfig.Biomes.Count}");
                Console.WriteLine($"Ore types: {config.OreDistributionConfig.OreTypes.Count}");
                Console.WriteLine($"Structure types: {config.StructureGenerationConfig.StructureTypes.Count}");
                Console.WriteLine($"Entity types: {config.EntitySpawnConfig.EntityTypes.Count}");
                
                // Create world generation manager
                var manager = new WorldGenerationManager(config);
                
                // Generate a test chunk
                var context = manager.GenerateChunk(0, 0, 12345);
                
                Console.WriteLine($"Generated chunk (0,0) with seed 12345");
                Console.WriteLine($"Height map size: {context.HeightMap.GetLength(0)}x{context.HeightMap.GetLength(1)}");
                Console.WriteLine($"Biome map size: {context.BiomeData.GetLength(0)}x{context.BiomeData.GetLength(1)}");
                Console.WriteLine($"Block types size: {context.BlockTypes.GetLength(0)}x{context.BlockTypes.GetLength(1)}x{context.BlockTypes.GetLength(2)}");
                
                // Test biome generation
                var biomeCount = new int[Enum.GetNames(typeof(BiomeType)).Length];
                for (int x = 0; x < context.ChunkSize; x++)
                {
                    for (int z = 0; z < context.ChunkSize; z++)
                    {
                        var biome = context.GetBiome(x, z);
                        biomeCount[(int)biome]++;
                    }
                }
                
                Console.WriteLine("Biome distribution:");
                foreach (BiomeType biome in Enum.GetValues(typeof(BiomeType)))
                {
                    Console.WriteLine($"  {biome}: {biomeCount[(int)biome]}");
                }
                
                Console.WriteLine("Terrain generation test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during terrain generation test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
