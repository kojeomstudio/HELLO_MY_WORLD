# Session 122 SharedProtocol Architecture Design

- Date: 2026-02-25
- Session: 122
- Status: Design Complete

## Overview

This document presents the improved architecture design for the SharedProtocol DLL, addressing the weaknesses identified in the analysis and providing a comprehensive solution for sharing common enums, constants, and code between client and server.

## Design Goals

1. **Single Source of Truth:** Eliminate enum duplication by using protobuf-generated enums as the source of truth
2. **Comprehensive Coverage:** Add missing terrain generation, world map control, and hydrology constants and enums
3. **Extensibility:** Design for easy addition of new constants, enums, and utilities
4. **Maintainability:** Clear organization and documentation for all shared code
5. **Performance:** Optimize for both client and server usage
6. **Compatibility:** Maintain backward compatibility with existing code

## Architecture Overview

```
SharedProtocol/
├── Common/
│   ├── Constants/
│   │   ├── GameConstants.cs (existing)
│   │   ├── NetworkConstants.cs (existing)
│   │   ├── WorldConstants.cs (existing)
│   │   ├── TerrainGenerationConstants.cs (NEW)
│   │   └── WorldMapControlConstants.cs (NEW)
│   ├── Enums/
│   │   ├── BiomeEnums.cs (existing - to be refactored)
│   │   ├── CombatEnums.cs (existing)
│   │   ├── CoreEnums.cs (existing)
│   │   ├── GameEnums.cs (existing - to be refactored)
│   │   ├── ItemEnums.cs (existing)
│   │   ├── WorldEnums.cs (existing)
│   │   ├── TerrainGenerationEnums.cs (NEW)
│   │   ├── ExpandedBiomeEnums.cs (NEW)
│   │   └── ExpandedEntityEnums.cs (NEW)
│   ├── Utilities/
│   │   ├── MathUtilities.cs (NEW)
│   │   ├── SerializationUtilities.cs (NEW)
│   │   ├── ValidationUtilities.cs (NEW)
│   │   ├── CompressionUtilities.cs (NEW)
│   │   └── LoggingUtilities.cs (NEW)
│   └── Interfaces/
│       └── ISharedProtocol.cs (existing)
├── Messages/
│   ├── BaseMessages.cs (refactored from Messages.cs)
│   ├── MinecraftMessages.cs (existing)
│   ├── ContainerMessages.cs (refactored from MinecraftContainerMessages.cs)
│   ├── WorldSyncMessages.cs (existing)
│   ├── TerrainGenerationMessages.cs (NEW)
│   ├── WorldMapControlMessages.cs (NEW)
│   └── HydrologyMessages.cs (NEW)
├── Dispatchers/
│   ├── BaseMessageDispatcher.cs (refactored from MessageDispatcher.cs)
│   ├── MinecraftMessageDispatcher.cs (existing - to be completed)
│   └── MessageHandlerRegistry.cs (NEW)
├── EnhancedMinecraft/
│   ├── ChunkPayloadBuilder.cs (existing)
│   ├── ProtocolRegistry.cs (existing)
│   ├── ProtocolStandardization.cs (existing)
│   ├── ProtocolValidator.cs (existing)
│   ├── ProtoDiagnostics.cs (existing)
│   ├── ProtoFingerprint.cs (existing)
│   ├── ProtoRuntime.cs (existing)
│   └── UnifiedMessageHandler.cs (existing)
├── Proto/
│   ├── common.proto (existing)
│   ├── game_auth.proto (existing)
│   ├── game_chat.proto (existing)
│   ├── game_core.proto (existing)
│   ├── game_diag.proto (existing)
│   ├── game_move.proto (existing)
│   ├── game_world.proto (existing)
│   ├── enhanced_minecraft_game.proto (existing)
│   ├── terrain_generation.proto (NEW)
│   ├── world_map_control.proto (NEW)
│   └── hydrology.proto (NEW)
├── Session.cs (existing)
├── GameProtocol.cs (existing)
└── SharedProtocol.csproj (to be updated)
```

## New Components

### 1. Terrain Generation Constants

#### File: SharedProtocol/Common/Constants/TerrainGenerationConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// Terrain generation constants shared between client and server
/// </summary>
public static class TerrainGenerationConstants
{
    #region Cave Generation
    
    /// <summary>
    /// Threshold for cave generation (0.0 - 1.0)
    /// </summary>
    public const double CaveThreshold = 0.5;
    
    /// <summary>
    /// Horizontal frequency for cave noise
    /// </summary>
    public const double CaveHorizontalFrequency = 0.05;
    
    /// <summary>
    /// Vertical frequency for cave noise
    /// </summary>
    public const double CaveVerticalFrequency = 0.1;
    
    /// <summary>
    /// Minimum height for cave generation
    /// </summary>
    public const int CaveMinHeight = 10;
    
    /// <summary>
    /// Maximum height for cave generation
    /// </summary>
    public const int CaveMaxHeight = 50;
    
    /// <summary>
    /// Maximum cave radius
    /// </summary>
    public const int CaveMaxRadius = 8;
    
    /// <summary>
    /// Minimum cave radius
    /// </summary>
    public const int CaveMinRadius = 2;
    
    #endregion
    
    #region River Generation
    
    /// <summary>
    /// Threshold for river bank generation
    /// </summary>
    public const double RiverBankThreshold = 0.6;
    
    /// <summary>
    /// Noise scale for river generation
    /// </summary>
    public const double RiverNoiseScale = 0.02;
    
    /// <summary>
    /// Minimum river width in blocks
    /// </summary>
    public const int RiverMinWidth = 3;
    
    /// <summary>
    /// Maximum river width in blocks
    /// </summary>
    public const int RiverMaxWidth = 8;
    
    /// <summary>
    /// River depth in blocks
    /// </summary>
    public const int RiverDepth = 3;
    
    #endregion
    
    #region Lake Generation
    
    /// <summary>
    /// Threshold for wetland/lake generation
    /// </summary>
    public const double LakeWetlandThreshold = 0.7;
    
    /// <summary>
    /// Bias for lake spawn weight
    /// </summary>
    public const double LakeSpawnWeightBias = 1.2;
    
    /// <summary>
    /// Minimum lake radius in blocks
    /// </summary>
    public const int LakeMinRadius = 5;
    
    /// <summary>
    /// Maximum lake radius in blocks
    /// </summary>
    public const int LakeMaxRadius = 15;
    
    /// <summary>
    /// Lake depth in blocks
    /// </summary>
    public const int LakeDepth = 5;
    
    #endregion
    
    #region Hydrology
    
    /// <summary>
    /// Threshold for hydrology flow calculation
    /// </summary>
    public const double HydrologyFlowThreshold = 0.3;
    
    /// <summary>
    /// Threshold for erosion risk calculation
    /// </summary>
    public const double HydrologyErosionThreshold = 0.5;
    
    /// <summary>
    /// Sample radius for hydrology calculations
    /// </summary>
    public const int HydrologySampleRadius = 8;
    
    /// <summary>
    /// Maximum flow accumulation value
    /// </summary>
    public const double MaxFlowAccumulation = 1000.0;
    
    #endregion
    
    #region Noise
    
    /// <summary>
    /// Seed offset for noise generation
    /// </summary>
    public const int NoiseSeedOffset = 12345;
    
    /// <summary>
    /// Base scale for noise generation
    /// </summary>
    public const double NoiseScale = 0.01;
    
    /// <summary>
    /// Number of octaves for noise generation
    /// </summary>
    public const int NoiseOctaves = 4;
    
    /// <summary>
    /// Persistence for noise generation
    /// </summary>
    public const double NoisePersistence = 0.5;
    
    /// <summary>
    /// Lacunarity for noise generation
    /// </summary>
    public const double NoiseLacunarity = 2.0;
    
    #endregion
    
    #region Terrain Quality
    
    /// <summary>
    /// Default terrain generation quality
    /// </summary>
    public const TerrainQualityLevel DefaultQuality = TerrainQualityLevel.Medium;
    
    /// <summary>
    /// Default terrain generation mode
    /// </summary>
    public const TerrainGenerationMode DefaultMode = TerrainGenerationMode.Standard;
    
    #endregion
}
```

### 2. World Map Control Constants

#### File: SharedProtocol/Common/Constants/WorldMapControlConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// World map control constants shared between client and server
/// </summary>
public static class WorldMapControlConstants
{
    /// <summary>
    /// World map resolution in pixels
    /// </summary>
    public const int WorldMapResolution = 256;
    
    /// <summary>
    /// Size of each map region in chunks
    /// </summary>
    public const int WorldMapRegionSize = 32;
    
    /// <summary>
    /// Update interval for world map in milliseconds
    /// </summary>
    public const int WorldMapUpdateIntervalMs = 1000;
    
    /// <summary>
    /// Maximum number of cached map regions
    /// </summary>
    public const int WorldMapCacheSize = 100;
    
    /// <summary>
    /// Maximum number of map regions
    /// </summary>
    public const int WorldMapMaxRegions = 1000;
    
    /// <summary>
    /// Compression ratio for map data
    /// </summary>
    public const float WorldMapCompressionRatio = 0.5f;
    
    /// <summary>
    /// Default map detail level
    /// </summary>
    public const WorldMapDetailLevel DefaultDetailLevel = WorldMapDetailLevel.Detailed;
}
```

### 3. Terrain Generation Enums

#### File: SharedProtocol/Common/Enums/TerrainGenerationEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Terrain generation enumeration types
/// </summary>
public static class TerrainGenerationEnums
{
    /// <summary>
    /// Types of terrain features
    /// </summary>
    public enum TerrainFeatureType
    {
        CaveEntrance = 0,
        RiverSource = 1,
        LakeOutlet = 2,
        Waterfall = 3,
        Geyser = 4,
        HotSpring = 5,
        Ravine = 6,
        Canyon = 7,
        Arch = 8,
        Overhang = 9
    }
    
    /// <summary>
    /// Types of caves
    /// </summary>
    public enum CaveType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Massive = 3,
        Ravine = 4,
        WaterCave = 5,
        LavaCave = 6
    }
    
    /// <summary>
    /// Types of rivers
    /// </summary>
    public enum RiverType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Underground = 3,
        Surface = 4,
        Frozen = 5
    }
    
    /// <summary>
    /// Types of lakes
    /// </summary>
    public enum LakeType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Deep = 3,
        Underground = 4,
        Surface = 5,
        Frozen = 6
    }
    
    /// <summary>
    /// Types of hydrology data
    /// </summary>
    public enum HydrologyDataType
    {
        FullHydrology = 0,
        FlowAccumulation = 1,
        ErosionRisk = 2,
        TerrainFeatures = 3
    }
    
    /// <summary>
    /// Terrain generation modes
    /// </summary>
    public enum TerrainGenerationMode
    {
        Standard = 0,
        Fast = 1,
        HighQuality = 2,
        Ultra = 3
    }
    
    /// <summary>
    /// Terrain quality levels
    /// </summary>
    public enum TerrainQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }
    
    /// <summary>
    /// Hydrology update types
    /// </summary>
    public enum HydrologyUpdateType
    {
        FlowChange = 0,
        ErosionUpdate = 1,
        WaterLevelChange = 2,
        SeasonalChange = 3
    }
}
```

### 4. Expanded Biome Enums

#### File: SharedProtocol/Common/Enums/ExpandedBiomeEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Expanded biome enumeration types (Minecraft standard)
/// </summary>
public static class ExpandedBiomeEnums
{
    /// <summary>
    /// Extended biome types matching Minecraft standard
    /// </summary>
    public enum ExtendedBiomeType
    {
        // Plains variants
        Plains = 0,
        SunflowerPlains = 1,
        
        // Forest variants
        Forest = 2,
        FlowerForest = 3,
        BirchForest = 4,
        BirchForestHills = 5,
        DarkForest = 6,
        DarkForestHills = 7,
        Taiga = 8,
        TaigaHills = 9,
        TaigaMountains = 10,
        GiantTreeTaiga = 11,
        GiantTreeTaigaHills = 12,
        SnowyTaiga = 13,
        SnowyTaigaHills = 14,
        SnowyTaigaMountains = 15,
        
        // Desert variants
        Desert = 16,
        DesertHills = 17,
        DesertLakes = 18,
        
        // Mountain variants
        Mountains = 19,
        WoodedMountains = 20,
        GravellyMountains = 21,
        MountainEdge = 22,
        SnowyMountains = 23,
        SnowyTundra = 24,
        SnowyMountains = 25,
        
        // Ocean variants
        Ocean = 26,
        DeepOcean = 27,
        FrozenOcean = 28,
        DeepFrozenOcean = 29,
        ColdOcean = 30,
        DeepColdOcean = 31,
        LukewarmOcean = 32,
        DeepLukewarmOcean = 33,
        WarmOcean = 34,
        DeepWarmOcean = 35,
        
        // Swamp variants
        Swamp = 36,
        SwampHills = 37,
        
        // Jungle variants
        Jungle = 38,
        JungleHills = 39,
        JungleEdge = 40,
        ModifiedJungle = 41,
        ModifiedJungleEdge = 42,
        BambooJungle = 43,
        BambooJungleHills = 44,
        
        // Mesa variants
        Badlands = 45,
        BadlandsPlateau = 46,
        ModifiedBadlandsPlateau = 47,
        WoodedBadlandsPlateau = 48,
        ModifiedWoodedBadlandsPlateau = 49,
        ErodedBadlands = 50,
        
        // Savanna variants
        Savanna = 51,
        SavannaPlateau = 52,
        ShatteredSavanna = 53,
        ShatteredSavannaPlateau = 54,
        
        // Ice plains variants
        SnowyPlains = 55,
        SnowyBeach = 56,
        
        // Mushroom island variants
        MushroomFields = 57,
        MushroomFieldShore = 58,
        
        // Beach variants
        Beach = 59,
        StonyShore = 60,
        StonyBeach = 61,
        
        // River variants
        River = 62,
        FrozenRiver = 63,
        
        // Extreme hills variants
        ExtremeHills = 64,
        ExtremeHillsEdge = 65,
        ExtremeHillsPlus = 66,
        ExtremeHillsPlusM = 67,
        
        // Other
        TheVoid = 68,
        TheEnd = 69,
        SmallEndIslands = 70,
        EndMidlands = 71,
        EndHighlands = 72,
        EndBarrens = 73,
        NetherWastes = 74,
        CrimsonForest = 75,
        WarpedForest = 76,
        SoulSandValley = 77,
        BasaltDeltas = 78
    }
}
```

### 5. Expanded Entity Enums

#### File: SharedProtocol/Common/Enums/ExpandedEntityEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Expanded entity enumeration types (Minecraft standard)
/// </summary>
public static class ExpandedEntityEnums
{
    /// <summary>
    /// Extended entity types matching Minecraft standard
    /// </summary>
    public enum ExtendedEntityType
    {
        // Players
        Player = 0,
        
        // Hostile mobs
        Zombie = 10,
        Husk = 11,
        Drowned = 12,
        ZombieVillager = 13,
        Skeleton = 14,
        Stray = 15,
        WitherSkeleton = 16,
        Spider = 17,
        CaveSpider = 18,
        Creeper = 19,
        Enderman = 20,
        Witch = 21,
        Slime = 22,
        MagmaCube = 23,
        Blaze = 24,
        Ghast = 25,
        Guardian = 26,
        ElderGuardian = 27,
        Shulker = 28,
        Silverfish = 29,
        Endermite = 30,
        Vindicator = 31,
        Evoker = 32,
        Vex = 33,
        Illusioner = 34,
        Ravager = 35,
        Pillager = 36,
        Phantom = 37,
        
        // Passive mobs
        Pig = 40,
        Cow = 41,
        Sheep = 42,
        Chicken = 43,
        Rabbit = 44,
        Horse = 45,
        Donkey = 46,
        Mule = 47,
        Llama = 48,
        TraderLlama = 49,
        Fox = 50,
        Panda = 51,
        Turtle = 52,
        Bee = 53,
        Cat = 54,
        Wolf = 55,
        Ocelot = 56,
        Parrot = 57,
        Bat = 58,
        Squid = 59,
        GlowSquid = 60,
        Cod = 61,
        Salmon = 62,
        Pufferfish = 63,
        TropicalFish = 64,
        Axolotl = 65,
        Goat = 66,
        Frog = 67,
        Tadpole = 68,
        Allay = 69,
        
        // Neutral mobs
        IronGolem = 80,
        SnowGolem = 81,
        Villager = 82,
        WanderingTrader = 83,
        PolarBear = 84,
        Dolphin = 85,
        
        // Boss entities
        EnderDragon = 100,
        Wither = 101,
        Warden = 102,
        
        // Projectiles
        Arrow = 120,
        SpectralArrow = 121,
        TippedArrow = 122,
        Trident = 123,
        Snowball = 124,
        Egg = 125,
        Fireball = 126,
        SmallFireball = 127,
        DragonFireball = 128,
        WitherSkull = 129,
        WitherSkullDangerous = 130,
        ShulkerBullet = 131,
        LlamaSpit = 132,
        ExperienceBottle = 133,
        EnderPearl = 134,
        EyeOfEnder = 135,
        Potion = 136,
        FireworkRocket = 137,
        
        // Vehicles
        Boat = 140,
        Minecart = 141,
        ChestMinecart = 142,
        FurnaceMinecart = 143,
        TntMinecart = 144,
        HopperMinecart = 145,
        CommandBlockMinecart = 146,
        SpawnerMinecart = 147,
        
        // Hanging entities
        ItemFrame = 150,
        GlowItemFrame = 151,
        Painting = 152,
        LeashKnot = 153,
        
        // Item entities
        DroppedItem = 160,
        ExperienceOrb = 161,
        ArmorStand = 162,
        
        // Other
        AreaEffectCloud = 170,
        EndCrystal = 171,
        EvokerFangs = 172,
        FallingBlock = 173,
        FireworkRocketEntity = 174,
        LightningBolt = 175,
        Marker = 176
    }
}
```

### 6. Common Utilities

#### File: SharedProtocol/Common/Utilities/MathUtilities.cs

```csharp
namespace SharedProtocol.Common.Utilities;

/// <summary>
/// Common math utilities shared between client and server
/// </summary>
public static class MathUtilities
{
    /// <summary>
    /// Clamps a value between minimum and maximum
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return value < min ? min : value > max ? max : value;
    }
    
    /// <summary>
    /// Clamps a value between minimum and maximum
    /// </summary>
    public static double Clamp(double value, double min, double max)
    {
        return value < min ? min : value > max ? max : value;
    }
    
    /// <summary>
    /// Clamps a value between minimum and maximum
    /// </summary>
    public static int Clamp(int value, int min, int max)
    {
        return value < min ? min : value > max ? max : value;
    }
    
    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    
    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    public static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
    
    /// <summary>
    /// Maps a value from one range to another
    /// </summary>
    public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
    }
    
    /// <summary>
    /// Maps a value from one range to another
    /// </summary>
    public static double Map(double value, double inMin, double inMax, double outMin, double outMax)
    {
        return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
    }
}
```

#### File: SharedProtocol/Common/Utilities/SerializationUtilities.cs

```csharp
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SharedProtocol.Common.Utilities;

/// <summary>
/// Common serialization utilities shared between client and server
/// </summary>
public static class SerializationUtilities
{
    /// <summary>
    /// Compresses byte array using GZip
    /// </summary>
    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
    
    /// <summary>
    /// Decompresses byte array using GZip
    /// </summary>
    public static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
    
    /// <summary>
    /// Serializes string to UTF-8 bytes
    /// </summary>
    public static byte[] StringToBytes(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
    
    /// <summary>
    /// Deserializes UTF-8 bytes to string
    /// </summary>
    public static string BytesToString(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
}
```

#### File: SharedProtocol/Common/Utilities/ValidationUtilities.cs

```csharp
namespace SharedProtocol.Common.Utilities;

/// <summary>
/// Common validation utilities shared between client and server
/// </summary>
public static class ValidationUtilities
{
    /// <summary>
    /// Validates that a value is within range
    /// </summary>
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }
    
    /// <summary>
    /// Validates that a value is within range
    /// </summary>
    public static bool IsInRange(float value, float min, float max)
    {
        return value >= min && value <= max;
    }
    
    /// <summary>
    /// Validates that a value is within range
    /// </summary>
    public static bool IsInRange(double value, double min, double max)
    {
        return value >= min && value <= max;
    }
    
    /// <summary>
    /// Validates that a string is not null or empty
    /// </summary>
    public static bool IsValidString(string? str)
    {
        return !string.IsNullOrEmpty(str);
    }
    
    /// <summary>
    /// Validates that a string is not null or whitespace
    /// </summary>
    public static bool IsValidStringNoWhitespace(string? str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }
}
```

### 7. New Protocol Messages

#### File: SharedProtocol/Messages/TerrainGenerationMessages.cs

```csharp
using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// Terrain generation protocol messages
/// </summary>
public static class TerrainGenerationMessages
{
    [ProtoContract]
    public class TerrainGenerationRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public int WorldHeight { get; set; }
        [ProtoMember(5)] public long WorldSeed { get; set; }
        [ProtoMember(6)] public TerrainGenerationOptions Options { get; set; } = new();
    }
    
    [ProtoContract]
    public class TerrainGenerationOptions
    {
        [ProtoMember(1)] public bool GenerateCaves { get; set; }
        [ProtoMember(2)] public bool GenerateRivers { get; set; }
        [ProtoMember(3)] public bool GenerateLakes { get; set; }
        [ProtoMember(4)] public CaveGenerationOptions CaveOptions { get; set; } = new();
        [ProtoMember(5)] public RiverGenerationOptions RiverOptions { get; set; } = new();
        [ProtoMember(6)] public LakeGenerationOptions LakeOptions { get; set; } = new();
    }
    
    [ProtoContract]
    public class CaveGenerationOptions
    {
        [ProtoMember(1)] public double Threshold { get; set; }
        [ProtoMember(2)] public double HorizontalFrequency { get; set; }
        [ProtoMember(3)] public double VerticalFrequency { get; set; }
    }
    
    [ProtoContract]
    public class RiverGenerationOptions
    {
        [ProtoMember(1)] public double BankThreshold { get; set; }
        [ProtoMember(2)] public double NoiseScale { get; set; }
    }
    
    [ProtoContract]
    public class LakeGenerationOptions
    {
        [ProtoMember(1)] public double WetlandThreshold { get; set; }
        [ProtoMember(2)] public double SpawnWeightBias { get; set; }
    }
    
    [ProtoContract]
    public class TerrainGenerationResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public TerrainData TerrainData { get; set; } = new();
        [ProtoMember(4)] public long GenerationTimeMs { get; set; }
    }
    
    [ProtoContract]
    public class TerrainData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] CaveMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] RiverMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] LakeMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class TerrainFeatureData
    {
        [ProtoMember(1)] public Common.Enums.TerrainFeatureType FeatureType { get; set; }
        [ProtoMember(2)] public Vector3Int Position { get; set; } = new();
        [ProtoMember(3)] public int FeatureId { get; set; }
        [ProtoMember(4)] public string FeatureData { get; set; } = string.Empty;
    }
}
```

#### File: SharedProtocol/Messages/WorldMapControlMessages.cs

```csharp
using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// World map control protocol messages
/// </summary>
public static class WorldMapControlMessages
{
    [ProtoContract]
    public class WorldMapLoadRequest
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public int RegionSize { get; set; }
        [ProtoMember(4)] public WorldMapDetailLevel DetailLevel { get; set; }
    }
    
    public enum WorldMapDetailLevel
    {
        Overview = 0,
        Detailed = 1,
        Full = 2
    }
    
    [ProtoContract]
    public class WorldMapLoadResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public WorldMapData MapData { get; set; } = new();
    }
    
    [ProtoContract]
    public class WorldMapData
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public byte[] BiomeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] HeightMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] WaterMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] FeatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public System.Collections.Generic.List<WorldMapRegion> Regions { get; set; } = new();
    }
    
    [ProtoContract]
    public class WorldMapRegion
    {
        [ProtoMember(1)] public int X { get; set; }
        [ProtoMember(2)] public int Z { get; set; }
        [ProtoMember(3)] public int Width { get; set; }
        [ProtoMember(4)] public int Height { get; set; }
        [ProtoMember(5)] public Common.Enums.BiomeType PrimaryBiome { get; set; }
        [ProtoMember(6)] public float WaterCoverage { get; set; }
        [ProtoMember(7)] public float CaveDensity { get; set; }
    }
    
    [ProtoContract]
    public class WorldMapUpdateBroadcast
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public MapUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
    
    public enum MapUpdateType
    {
        BiomeChange = 0,
        TerrainModification = 1,
        WaterLevelChange = 2,
        FeatureAddition = 3,
        FeatureRemoval = 4
    }
}
```

#### File: SharedProtocol/Messages/HydrologyMessages.cs

```csharp
using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// Hydrology protocol messages
/// </summary>
public static class HydrologyMessages
{
    [ProtoContract]
    public class HydrologyDataRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public Common.Enums.HydrologyDataType DataType { get; set; }
    }
    
    [ProtoContract]
    public class HydrologyDataResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public HydrologyData Data { get; set; } = new();
    }
    
    [ProtoContract]
    public class HydrologyData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] SlopeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] CurvatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ReliefMap { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class HydrologyUpdateBroadcast
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public Common.Enums.HydrologyUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
}
```

### 8. Message Handler Registry

#### File: SharedProtocol/Dispatchers/MessageHandlerRegistry.cs

```csharp
using System;
using System.Collections.Generic;

namespace SharedProtocol.Dispatchers;

/// <summary>
/// Central registry for all message handlers
/// </summary>
public class MessageHandlerRegistry
{
    private readonly Dictionary<Type, object> _handlers = new();
    private readonly Dictionary<Enum, Type> _messageTypeToContract = new();
    
    /// <summary>
    /// Registers a message handler
    /// </summary>
    public void RegisterHandler<TMessage, THandler>(Enum messageType, THandler handler)
        where TMessage : class
        where THandler : class
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
        
        var messageContractType = typeof(TMessage);
        _handlers[messageContractType] = handler;
        _messageTypeToContract[messageType] = messageContractType;
    }
    
    /// <summary>
    /// Gets a registered handler for a message type
    /// </summary>
    public bool TryGetHandler<TMessage>(out object? handler)
    {
        return _handlers.TryGetValue(typeof(TMessage), out handler);
    }
    
    /// <summary>
    /// Gets the contract type for a message type enum
    /// </summary>
    public bool TryGetContractType(Enum messageType, out Type? contractType)
    {
        return _messageTypeToContract.TryGetValue(messageType, out contractType);
    }
    
    /// <summary>
    /// Gets all registered handler types
    /// </summary>
    public IReadOnlyCollection<Type> GetRegisteredHandlerTypes()
    {
        return _handlers.Keys;
    }
    
    /// <summary>
    /// Clears all registered handlers
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
        _messageTypeToContract.Clear();
    }
}
```

## Refactoring Plan

### Phase 1: Add New Constants and Enums
- [ ] Create TerrainGenerationConstants.cs
- [ ] Create WorldMapControlConstants.cs
- [ ] Create TerrainGenerationEnums.cs
- [ ] Create ExpandedBiomeEnums.cs
- [ ] Create ExpandedEntityEnums.cs

### Phase 2: Add Utilities
- [ ] Create MathUtilities.cs
- [ ] Create SerializationUtilities.cs
- [ ] Create ValidationUtilities.cs

### Phase 3: Add New Protocol Messages
- [ ] Create TerrainGenerationMessages.cs
- [ ] Create WorldMapControlMessages.cs
- [ ] Create HydrologyMessages.cs

### Phase 4: Refactor Existing Code
- [ ] Refactor Messages.cs to BaseMessages.cs
- [ ] Refactor MinecraftContainerMessages.cs to ContainerMessages.cs
- [ ] Refactor MessageDispatcher.cs to BaseMessageDispatcher.cs
- [ ] Complete TODO items in MinecraftMessageDispatcher.cs

### Phase 5: Update Project File
- [ ] Update SharedProtocol.csproj with new files
- [ ] Update protobuf references

### Phase 6: Update Protobuf Definitions
- [ ] Create terrain_generation.proto
- [ ] Create world_map_control.proto
- [ ] Create hydrology.proto

### Phase 7: Testing
- [ ] Unit tests for new constants
- [ ] Unit tests for new enums
- [ ] Unit tests for new utilities
- [ ] Unit tests for new messages
- [ ] Integration tests

## Migration Strategy

### Backward Compatibility

1. **Keep Existing Files:** Do not delete existing files until migration is complete
2. **Use Aliases:** Create using aliases for moved types
3. **Deprecation Warnings:** Add Obsolete attributes to deprecated code
4. **Gradual Migration:** Migrate one component at a time

### Example Migration Code

```csharp
// Old code (still works)
using SharedProtocol;
var messageType = MessageType.LoginRequest;

// New code (preferred)
using SharedProtocol.Messages;
using SharedProtocol.Common.Enums;
var messageType = BaseMessageType.LoginRequest;
```

## Implementation Priority

### High Priority (Session 122)
1. Add terrain generation constants
2. Add terrain generation enums
3. Add world map control constants
4. Create new protocol messages

### Medium Priority (Session 123)
5. Add expanded biome enums
6. Add expanded entity enums
7. Add common utilities
8. Complete TODO items in dispatcher

### Low Priority (Session 124)
9. Refactor existing code
10. Resolve enum duplication
11. Improve documentation

## Next Steps

1. **Review and approve** this architecture design
2. **Create implementation tasks** for each phase
3. **Implement Phase 1** (New constants and enums)
4. **Test thoroughly** before proceeding
5. **Continue through all phases**
6. **Document and deploy**

## References

- SharedProtocol DLL analysis
- Terrain generation algorithms
- World map control architecture
- Protobuf protocol analysis
- Minecraft standard biomes and entities

- Date: 2026-02-25
- Session: 122
- Status: Design Complete

## Overview

This document presents the improved architecture design for the SharedProtocol DLL, addressing the weaknesses identified in the analysis and providing a comprehensive solution for sharing common enums, constants, and code between client and server.

## Design Goals

1. **Single Source of Truth:** Eliminate enum duplication by using protobuf-generated enums as the source of truth
2. **Comprehensive Coverage:** Add missing terrain generation, world map control, and hydrology constants and enums
3. **Extensibility:** Design for easy addition of new constants, enums, and utilities
4. **Maintainability:** Clear organization and documentation for all shared code
5. **Performance:** Optimize for both client and server usage
6. **Compatibility:** Maintain backward compatibility with existing code

## Architecture Overview

```
SharedProtocol/
├── Common/
│   ├── Constants/
│   │   ├── GameConstants.cs (existing)
│   │   ├── NetworkConstants.cs (existing)
│   │   ├── WorldConstants.cs (existing)
│   │   ├── TerrainGenerationConstants.cs (NEW)
│   │   └── WorldMapControlConstants.cs (NEW)
│   ├── Enums/
│   │   ├── BiomeEnums.cs (existing - to be refactored)
│   │   ├── CombatEnums.cs (existing)
│   │   ├── CoreEnums.cs (existing)
│   │   ├── GameEnums.cs (existing - to be refactored)
│   │   ├── ItemEnums.cs (existing)
│   │   ├── WorldEnums.cs (existing)
│   │   ├── TerrainGenerationEnums.cs (NEW)
│   │   ├── ExpandedBiomeEnums.cs (NEW)
│   │   └── ExpandedEntityEnums.cs (NEW)
│   ├── Utilities/
│   │   ├── MathUtilities.cs (NEW)
│   │   ├── SerializationUtilities.cs (NEW)
│   │   ├── ValidationUtilities.cs (NEW)
│   │   ├── CompressionUtilities.cs (NEW)
│   │   └── LoggingUtilities.cs (NEW)
│   └── Interfaces/
│       └── ISharedProtocol.cs (existing)
├── Messages/
│   ├── BaseMessages.cs (refactored from Messages.cs)
│   ├── MinecraftMessages.cs (existing)
│   ├── ContainerMessages.cs (refactored from MinecraftContainerMessages.cs)
│   ├── WorldSyncMessages.cs (existing)
│   ├── TerrainGenerationMessages.cs (NEW)
│   ├── WorldMapControlMessages.cs (NEW)
│   └── HydrologyMessages.cs (NEW)
├── Dispatchers/
│   ├── BaseMessageDispatcher.cs (refactored from MessageDispatcher.cs)
│   ├── MinecraftMessageDispatcher.cs (existing - to be completed)
│   └── MessageHandlerRegistry.cs (NEW)
├── EnhancedMinecraft/
│   ├── ChunkPayloadBuilder.cs (existing)
│   ├── ProtocolRegistry.cs (existing)
│   ├── ProtocolStandardization.cs (existing)
│   ├── ProtocolValidator.cs (existing)
│   ├── ProtoDiagnostics.cs (existing)
│   ├── ProtoFingerprint.cs (existing)
│   ├── ProtoRuntime.cs (existing)
│   └── UnifiedMessageHandler.cs (existing)
├── Proto/
│   ├── common.proto (existing)
│   ├── game_auth.proto (existing)
│   ├── game_chat.proto (existing)
│   ├── game_core.proto (existing)
│   ├── game_diag.proto (existing)
│   ├── game_move.proto (existing)
│   ├── game_world.proto (existing)
│   ├── enhanced_minecraft_game.proto (existing)
│   ├── terrain_generation.proto (NEW)
│   ├── world_map_control.proto (NEW)
│   └── hydrology.proto (NEW)
├── Session.cs (existing)
├── GameProtocol.cs (existing)
└── SharedProtocol.csproj (to be updated)
```

## New Components

### 1. Terrain Generation Constants

#### File: SharedProtocol/Common/Constants/TerrainGenerationConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// Terrain generation constants shared between client and server
/// </summary>
public static class TerrainGenerationConstants
{
    #region Cave Generation
    
    /// <summary>
    /// Threshold for cave generation (0.0 - 1.0)
    /// </summary>
    public const double CaveThreshold = 0.5;
    
    /// <summary>
    /// Horizontal frequency for cave noise
    /// </summary>
    public const double CaveHorizontalFrequency = 0.05;
    
    /// <summary>
    /// Vertical frequency for cave noise
    /// </summary>
    public const double CaveVerticalFrequency = 0.1;
    
    /// <summary>
    /// Minimum height for cave generation
    /// </summary>
    public const int CaveMinHeight = 10;
    
    /// <summary>
    /// Maximum height for cave generation
    /// </summary>
    public const int CaveMaxHeight = 50;
    
    /// <summary>
    /// Maximum cave radius
    /// </summary>
    public const int CaveMaxRadius = 8;
    
    /// <summary>
    /// Minimum cave radius
    /// </summary>
    public const int CaveMinRadius = 2;
    
    #endregion
    
    #region River Generation
    
    /// <summary>
    /// Threshold for river bank generation
    /// </summary>
    public const double RiverBankThreshold = 0.6;
    
    /// <summary>
    /// Noise scale for river generation
    /// </summary>
    public const double RiverNoiseScale = 0.02;
    
    /// <summary>
    /// Minimum river width in blocks
    /// </summary>
    public const int RiverMinWidth = 3;
    
    /// <summary>
    /// Maximum river width in blocks
    /// </summary>
    public const int RiverMaxWidth = 8;
    
    /// <summary>
    /// River depth in blocks
    /// </summary>
    public const int RiverDepth = 3;
    
    #endregion
    
    #region Lake Generation
    
    /// <summary>
    /// Threshold for wetland/lake generation
    /// </summary>
    public const double LakeWetlandThreshold = 0.7;
    
    /// <summary>
    /// Bias for lake spawn weight
    /// </summary>
    public const double LakeSpawnWeightBias = 1.2;
    
    /// <summary>
    /// Minimum lake radius in blocks
    /// </summary>
    public const int LakeMinRadius = 5;
    
    /// <summary>
    /// Maximum lake radius in blocks
    /// </summary>
    public const int LakeMaxRadius = 15;
    
    /// <summary>
    /// Lake depth in blocks
    /// </summary>
    public const int LakeDepth = 5;
    
    #endregion
    
    #region Hydrology
    
    /// <summary>
    /// Threshold for hydrology flow calculation
    /// </summary>
    public const double HydrologyFlowThreshold = 0.3;
    
    /// <summary>
    /// Threshold for erosion risk calculation
    /// </summary>
    public const double HydrologyErosionThreshold = 0.5;
    
    /// <summary>
    /// Sample radius for hydrology calculations
    /// </summary>
    public const int HydrologySampleRadius = 8;
    
    /// <summary>
    /// Maximum flow accumulation value
    /// </summary>
    public const double MaxFlowAccumulation = 1000.0;
    
    #endregion
    
    #region Noise
    
    /// <summary>
    /// Seed offset for noise generation
    /// </summary>
    public const int NoiseSeedOffset = 12345;
    
    /// <summary>
    /// Base scale for noise generation
    /// </summary>
    public const double NoiseScale = 0.01;
    
    /// <summary>
    /// Number of octaves for noise generation
    /// </summary>
    public const int NoiseOctaves = 4;
    
    /// <summary>
    /// Persistence for noise generation
    /// </summary>
    public const double NoisePersistence = 0.5;
    
    /// <summary>
    /// Lacunarity for noise generation
    /// </summary>
    public const double NoiseLacunarity = 2.0;
    
    #endregion
    
    #region Terrain Quality
    
    /// <summary>
    /// Default terrain generation quality
    /// </summary>
    public const TerrainQualityLevel DefaultQuality = TerrainQualityLevel.Medium;
    
    /// <summary>
    /// Default terrain generation mode
    /// </summary>
    public const TerrainGenerationMode DefaultMode = TerrainGenerationMode.Standard;
    
    #endregion
}
```

### 2. World Map Control Constants

#### File: SharedProtocol/Common/Constants/WorldMapControlConstants.cs

```csharp
namespace SharedProtocol.Common.Constants;

/// <summary>
/// World map control constants shared between client and server
/// </summary>
public static class WorldMapControlConstants
{
    /// <summary>
    /// World map resolution in pixels
    /// </summary>
    public const int WorldMapResolution = 256;
    
    /// <summary>
    /// Size of each map region in chunks
    /// </summary>
    public const int WorldMapRegionSize = 32;
    
    /// <summary>
    /// Update interval for world map in milliseconds
    /// </summary>
    public const int WorldMapUpdateIntervalMs = 1000;
    
    /// <summary>
    /// Maximum number of cached map regions
    /// </summary>
    public const int WorldMapCacheSize = 100;
    
    /// <summary>
    /// Maximum number of map regions
    /// </summary>
    public const int WorldMapMaxRegions = 1000;
    
    /// <summary>
    /// Compression ratio for map data
    /// </summary>
    public const float WorldMapCompressionRatio = 0.5f;
    
    /// <summary>
    /// Default map detail level
    /// </summary>
    public const WorldMapDetailLevel DefaultDetailLevel = WorldMapDetailLevel.Detailed;
}
```

### 3. Terrain Generation Enums

#### File: SharedProtocol/Common/Enums/TerrainGenerationEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Terrain generation enumeration types
/// </summary>
public static class TerrainGenerationEnums
{
    /// <summary>
    /// Types of terrain features
    /// </summary>
    public enum TerrainFeatureType
    {
        CaveEntrance = 0,
        RiverSource = 1,
        LakeOutlet = 2,
        Waterfall = 3,
        Geyser = 4,
        HotSpring = 5,
        Ravine = 6,
        Canyon = 7,
        Arch = 8,
        Overhang = 9
    }
    
    /// <summary>
    /// Types of caves
    /// </summary>
    public enum CaveType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Massive = 3,
        Ravine = 4,
        WaterCave = 5,
        LavaCave = 6
    }
    
    /// <summary>
    /// Types of rivers
    /// </summary>
    public enum RiverType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Underground = 3,
        Surface = 4,
        Frozen = 5
    }
    
    /// <summary>
    /// Types of lakes
    /// </summary>
    public enum LakeType
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Deep = 3,
        Underground = 4,
        Surface = 5,
        Frozen = 6
    }
    
    /// <summary>
    /// Types of hydrology data
    /// </summary>
    public enum HydrologyDataType
    {
        FullHydrology = 0,
        FlowAccumulation = 1,
        ErosionRisk = 2,
        TerrainFeatures = 3
    }
    
    /// <summary>
    /// Terrain generation modes
    /// </summary>
    public enum TerrainGenerationMode
    {
        Standard = 0,
        Fast = 1,
        HighQuality = 2,
        Ultra = 3
    }
    
    /// <summary>
    /// Terrain quality levels
    /// </summary>
    public enum TerrainQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }
    
    /// <summary>
    /// Hydrology update types
    /// </summary>
    public enum HydrologyUpdateType
    {
        FlowChange = 0,
        ErosionUpdate = 1,
        WaterLevelChange = 2,
        SeasonalChange = 3
    }
}
```

### 4. Expanded Biome Enums

#### File: SharedProtocol/Common/Enums/ExpandedBiomeEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Expanded biome enumeration types (Minecraft standard)
/// </summary>
public static class ExpandedBiomeEnums
{
    /// <summary>
    /// Extended biome types matching Minecraft standard
    /// </summary>
    public enum ExtendedBiomeType
    {
        // Plains variants
        Plains = 0,
        SunflowerPlains = 1,
        
        // Forest variants
        Forest = 2,
        FlowerForest = 3,
        BirchForest = 4,
        BirchForestHills = 5,
        DarkForest = 6,
        DarkForestHills = 7,
        Taiga = 8,
        TaigaHills = 9,
        TaigaMountains = 10,
        GiantTreeTaiga = 11,
        GiantTreeTaigaHills = 12,
        SnowyTaiga = 13,
        SnowyTaigaHills = 14,
        SnowyTaigaMountains = 15,
        
        // Desert variants
        Desert = 16,
        DesertHills = 17,
        DesertLakes = 18,
        
        // Mountain variants
        Mountains = 19,
        WoodedMountains = 20,
        GravellyMountains = 21,
        MountainEdge = 22,
        SnowyMountains = 23,
        SnowyTundra = 24,
        SnowyMountains = 25,
        
        // Ocean variants
        Ocean = 26,
        DeepOcean = 27,
        FrozenOcean = 28,
        DeepFrozenOcean = 29,
        ColdOcean = 30,
        DeepColdOcean = 31,
        LukewarmOcean = 32,
        DeepLukewarmOcean = 33,
        WarmOcean = 34,
        DeepWarmOcean = 35,
        
        // Swamp variants
        Swamp = 36,
        SwampHills = 37,
        
        // Jungle variants
        Jungle = 38,
        JungleHills = 39,
        JungleEdge = 40,
        ModifiedJungle = 41,
        ModifiedJungleEdge = 42,
        BambooJungle = 43,
        BambooJungleHills = 44,
        
        // Mesa variants
        Badlands = 45,
        BadlandsPlateau = 46,
        ModifiedBadlandsPlateau = 47,
        WoodedBadlandsPlateau = 48,
        ModifiedWoodedBadlandsPlateau = 49,
        ErodedBadlands = 50,
        
        // Savanna variants
        Savanna = 51,
        SavannaPlateau = 52,
        ShatteredSavanna = 53,
        ShatteredSavannaPlateau = 54,
        
        // Ice plains variants
        SnowyPlains = 55,
        SnowyBeach = 56,
        
        // Mushroom island variants
        MushroomFields = 57,
        MushroomFieldShore = 58,
        
        // Beach variants
        Beach = 59,
        StonyShore = 60,
        StonyBeach = 61,
        
        // River variants
        River = 62,
        FrozenRiver = 63,
        
        // Extreme hills variants
        ExtremeHills = 64,
        ExtremeHillsEdge = 65,
        ExtremeHillsPlus = 66,
        ExtremeHillsPlusM = 67,
        
        // Other
        TheVoid = 68,
        TheEnd = 69,
        SmallEndIslands = 70,
        EndMidlands = 71,
        EndHighlands = 72,
        EndBarrens = 73,
        NetherWastes = 74,
        CrimsonForest = 75,
        WarpedForest = 76,
        SoulSandValley = 77,
        BasaltDeltas = 78
    }
}
```

### 5. Expanded Entity Enums

#### File: SharedProtocol/Common/Enums/ExpandedEntityEnums.cs

```csharp
namespace SharedProtocol.Common.Enums;

/// <summary>
/// Expanded entity enumeration types (Minecraft standard)
/// </summary>
public static class ExpandedEntityEnums
{
    /// <summary>
    /// Extended entity types matching Minecraft standard
    /// </summary>
    public enum ExtendedEntityType
    {
        // Players
        Player = 0,
        
        // Hostile mobs
        Zombie = 10,
        Husk = 11,
        Drowned = 12,
        ZombieVillager = 13,
        Skeleton = 14,
        Stray = 15,
        WitherSkeleton = 16,
        Spider = 17,
        CaveSpider = 18,
        Creeper = 19,
        Enderman = 20,
        Witch = 21,
        Slime = 22,
        MagmaCube = 23,
        Blaze = 24,
        Ghast = 25,
        Guardian = 26,
        ElderGuardian = 27,
        Shulker = 28,
        Silverfish = 29,
        Endermite = 30,
        Vindicator = 31,
        Evoker = 32,
        Vex = 33,
        Illusioner = 34,
        Ravager = 35,
        Pillager = 36,
        Phantom = 37,
        
        // Passive mobs
        Pig = 40,
        Cow = 41,
        Sheep = 42,
        Chicken = 43,
        Rabbit = 44,
        Horse = 45,
        Donkey = 46,
        Mule = 47,
        Llama = 48,
        TraderLlama = 49,
        Fox = 50,
        Panda = 51,
        Turtle = 52,
        Bee = 53,
        Cat = 54,
        Wolf = 55,
        Ocelot = 56,
        Parrot = 57,
        Bat = 58,
        Squid = 59,
        GlowSquid = 60,
        Cod = 61,
        Salmon = 62,
        Pufferfish = 63,
        TropicalFish = 64,
        Axolotl = 65,
        Goat = 66,
        Frog = 67,
        Tadpole = 68,
        Allay = 69,
        
        // Neutral mobs
        IronGolem = 80,
        SnowGolem = 81,
        Villager = 82,
        WanderingTrader = 83,
        PolarBear = 84,
        Dolphin = 85,
        
        // Boss entities
        EnderDragon = 100,
        Wither = 101,
        Warden = 102,
        
        // Projectiles
        Arrow = 120,
        SpectralArrow = 121,
        TippedArrow = 122,
        Trident = 123,
        Snowball = 124,
        Egg = 125,
        Fireball = 126,
        SmallFireball = 127,
        DragonFireball = 128,
        WitherSkull = 129,
        WitherSkullDangerous = 130,
        ShulkerBullet = 131,
        LlamaSpit = 132,
        ExperienceBottle = 133,
        EnderPearl = 134,
        EyeOfEnder = 135,
        Potion = 136,
        FireworkRocket = 137,
        
        // Vehicles
        Boat = 140,
        Minecart = 141,
        ChestMinecart = 142,
        FurnaceMinecart = 143,
        TntMinecart = 144,
        HopperMinecart = 145,
        CommandBlockMinecart = 146,
        SpawnerMinecart = 147,
        
        // Hanging entities
        ItemFrame = 150,
        GlowItemFrame = 151,
        Painting = 152,
        LeashKnot = 153,
        
        // Item entities
        DroppedItem = 160,
        ExperienceOrb = 161,
        ArmorStand = 162,
        
        // Other
        AreaEffectCloud = 170,
        EndCrystal = 171,
        EvokerFangs = 172,
        FallingBlock = 173,
        FireworkRocketEntity = 174,
        LightningBolt = 175,
        Marker = 176
    }
}
```

### 6. Common Utilities

#### File: SharedProtocol/Common/Utilities/MathUtilities.cs

```csharp
namespace SharedProtocol.Common.Utilities;

/// <summary>
/// Common math utilities shared between client and server
/// </summary>
public static class MathUtilities
{
    /// <summary>
    /// Clamps a value between minimum and maximum
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return value < min ? min : value > max ? max : value;
    }
    
    /// <summary>
    /// Clamps a value between minimum and maximum
    /// </summary>
    public static double Clamp(double value, double min, double max)
    {
        return value < min ? min : value > max ? max : value;
    }
    
    /// <summary>
    /// Clamps a value between minimum and maximum
    /// </summary>
    public static int Clamp(int value, int min, int max)
    {
        return value < min ? min : value > max ? max : value;
    }
    
    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    
    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    public static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
    
    /// <summary>
    /// Maps a value from one range to another
    /// </summary>
    public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
    }
    
    /// <summary>
    /// Maps a value from one range to another
    /// </summary>
    public static double Map(double value, double inMin, double inMax, double outMin, double outMax)
    {
        return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
    }
}
```

#### File: SharedProtocol/Common/Utilities/SerializationUtilities.cs

```csharp
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SharedProtocol.Common.Utilities;

/// <summary>
/// Common serialization utilities shared between client and server
/// </summary>
public static class SerializationUtilities
{
    /// <summary>
    /// Compresses byte array using GZip
    /// </summary>
    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
    
    /// <summary>
    /// Decompresses byte array using GZip
    /// </summary>
    public static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
    
    /// <summary>
    /// Serializes string to UTF-8 bytes
    /// </summary>
    public static byte[] StringToBytes(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
    
    /// <summary>
    /// Deserializes UTF-8 bytes to string
    /// </summary>
    public static string BytesToString(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
}
```

#### File: SharedProtocol/Common/Utilities/ValidationUtilities.cs

```csharp
namespace SharedProtocol.Common.Utilities;

/// <summary>
/// Common validation utilities shared between client and server
/// </summary>
public static class ValidationUtilities
{
    /// <summary>
    /// Validates that a value is within range
    /// </summary>
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }
    
    /// <summary>
    /// Validates that a value is within range
    /// </summary>
    public static bool IsInRange(float value, float min, float max)
    {
        return value >= min && value <= max;
    }
    
    /// <summary>
    /// Validates that a value is within range
    /// </summary>
    public static bool IsInRange(double value, double min, double max)
    {
        return value >= min && value <= max;
    }
    
    /// <summary>
    /// Validates that a string is not null or empty
    /// </summary>
    public static bool IsValidString(string? str)
    {
        return !string.IsNullOrEmpty(str);
    }
    
    /// <summary>
    /// Validates that a string is not null or whitespace
    /// </summary>
    public static bool IsValidStringNoWhitespace(string? str)
    {
        return !string.IsNullOrWhiteSpace(str);
    }
}
```

### 7. New Protocol Messages

#### File: SharedProtocol/Messages/TerrainGenerationMessages.cs

```csharp
using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// Terrain generation protocol messages
/// </summary>
public static class TerrainGenerationMessages
{
    [ProtoContract]
    public class TerrainGenerationRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public int WorldHeight { get; set; }
        [ProtoMember(5)] public long WorldSeed { get; set; }
        [ProtoMember(6)] public TerrainGenerationOptions Options { get; set; } = new();
    }
    
    [ProtoContract]
    public class TerrainGenerationOptions
    {
        [ProtoMember(1)] public bool GenerateCaves { get; set; }
        [ProtoMember(2)] public bool GenerateRivers { get; set; }
        [ProtoMember(3)] public bool GenerateLakes { get; set; }
        [ProtoMember(4)] public CaveGenerationOptions CaveOptions { get; set; } = new();
        [ProtoMember(5)] public RiverGenerationOptions RiverOptions { get; set; } = new();
        [ProtoMember(6)] public LakeGenerationOptions LakeOptions { get; set; } = new();
    }
    
    [ProtoContract]
    public class CaveGenerationOptions
    {
        [ProtoMember(1)] public double Threshold { get; set; }
        [ProtoMember(2)] public double HorizontalFrequency { get; set; }
        [ProtoMember(3)] public double VerticalFrequency { get; set; }
    }
    
    [ProtoContract]
    public class RiverGenerationOptions
    {
        [ProtoMember(1)] public double BankThreshold { get; set; }
        [ProtoMember(2)] public double NoiseScale { get; set; }
    }
    
    [ProtoContract]
    public class LakeGenerationOptions
    {
        [ProtoMember(1)] public double WetlandThreshold { get; set; }
        [ProtoMember(2)] public double SpawnWeightBias { get; set; }
    }
    
    [ProtoContract]
    public class TerrainGenerationResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public TerrainData TerrainData { get; set; } = new();
        [ProtoMember(4)] public long GenerationTimeMs { get; set; }
    }
    
    [ProtoContract]
    public class TerrainData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] CaveMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] RiverMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] LakeMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class TerrainFeatureData
    {
        [ProtoMember(1)] public Common.Enums.TerrainFeatureType FeatureType { get; set; }
        [ProtoMember(2)] public Vector3Int Position { get; set; } = new();
        [ProtoMember(3)] public int FeatureId { get; set; }
        [ProtoMember(4)] public string FeatureData { get; set; } = string.Empty;
    }
}
```

#### File: SharedProtocol/Messages/WorldMapControlMessages.cs

```csharp
using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// World map control protocol messages
/// </summary>
public static class WorldMapControlMessages
{
    [ProtoContract]
    public class WorldMapLoadRequest
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public int RegionSize { get; set; }
        [ProtoMember(4)] public WorldMapDetailLevel DetailLevel { get; set; }
    }
    
    public enum WorldMapDetailLevel
    {
        Overview = 0,
        Detailed = 1,
        Full = 2
    }
    
    [ProtoContract]
    public class WorldMapLoadResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public WorldMapData MapData { get; set; } = new();
    }
    
    [ProtoContract]
    public class WorldMapData
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public byte[] BiomeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] HeightMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] WaterMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] FeatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public System.Collections.Generic.List<WorldMapRegion> Regions { get; set; } = new();
    }
    
    [ProtoContract]
    public class WorldMapRegion
    {
        [ProtoMember(1)] public int X { get; set; }
        [ProtoMember(2)] public int Z { get; set; }
        [ProtoMember(3)] public int Width { get; set; }
        [ProtoMember(4)] public int Height { get; set; }
        [ProtoMember(5)] public Common.Enums.BiomeType PrimaryBiome { get; set; }
        [ProtoMember(6)] public float WaterCoverage { get; set; }
        [ProtoMember(7)] public float CaveDensity { get; set; }
    }
    
    [ProtoContract]
    public class WorldMapUpdateBroadcast
    {
        [ProtoMember(1)] public int RegionX { get; set; }
        [ProtoMember(2)] public int RegionZ { get; set; }
        [ProtoMember(3)] public MapUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
    
    public enum MapUpdateType
    {
        BiomeChange = 0,
        TerrainModification = 1,
        WaterLevelChange = 2,
        FeatureAddition = 3,
        FeatureRemoval = 4
    }
}
```

#### File: SharedProtocol/Messages/HydrologyMessages.cs

```csharp
using ProtoBuf;

namespace SharedProtocol.Messages;

/// <summary>
/// Hydrology protocol messages
/// </summary>
public static class HydrologyMessages
{
    [ProtoContract]
    public class HydrologyDataRequest
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public int ChunkSize { get; set; }
        [ProtoMember(4)] public Common.Enums.HydrologyDataType DataType { get; set; }
    }
    
    [ProtoContract]
    public class HydrologyDataResponse
    {
        [ProtoMember(1)] public bool Success { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public HydrologyData Data { get; set; } = new();
    }
    
    [ProtoContract]
    public class HydrologyData
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public byte[] HydrologyMask { get; set; } = Array.Empty<byte>();
        [ProtoMember(4)] public byte[] FlowAccumulation { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public byte[] ErosionRisk { get; set; } = Array.Empty<byte>();
        [ProtoMember(6)] public byte[] SlopeMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(7)] public byte[] CurvatureMap { get; set; } = Array.Empty<byte>();
        [ProtoMember(8)] public byte[] ReliefMap { get; set; } = Array.Empty<byte>();
    }
    
    [ProtoContract]
    public class HydrologyUpdateBroadcast
    {
        [ProtoMember(1)] public int ChunkX { get; set; }
        [ProtoMember(2)] public int ChunkZ { get; set; }
        [ProtoMember(3)] public Common.Enums.HydrologyUpdateType UpdateType { get; set; }
        [ProtoMember(4)] public byte[] UpdatedData { get; set; } = Array.Empty<byte>();
        [ProtoMember(5)] public long Timestamp { get; set; }
    }
}
```

### 8. Message Handler Registry

#### File: SharedProtocol/Dispatchers/MessageHandlerRegistry.cs

```csharp
using System;
using System.Collections.Generic;

namespace SharedProtocol.Dispatchers;

/// <summary>
/// Central registry for all message handlers
/// </summary>
public class MessageHandlerRegistry
{
    private readonly Dictionary<Type, object> _handlers = new();
    private readonly Dictionary<Enum, Type> _messageTypeToContract = new();
    
    /// <summary>
    /// Registers a message handler
    /// </summary>
    public void RegisterHandler<TMessage, THandler>(Enum messageType, THandler handler)
        where TMessage : class
        where THandler : class
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }
        
        var messageContractType = typeof(TMessage);
        _handlers[messageContractType] = handler;
        _messageTypeToContract[messageType] = messageContractType;
    }
    
    /// <summary>
    /// Gets a registered handler for a message type
    /// </summary>
    public bool TryGetHandler<TMessage>(out object? handler)
    {
        return _handlers.TryGetValue(typeof(TMessage), out handler);
    }
    
    /// <summary>
    /// Gets the contract type for a message type enum
    /// </summary>
    public bool TryGetContractType(Enum messageType, out Type? contractType)
    {
        return _messageTypeToContract.TryGetValue(messageType, out contractType);
    }
    
    /// <summary>
    /// Gets all registered handler types
    /// </summary>
    public IReadOnlyCollection<Type> GetRegisteredHandlerTypes()
    {
        return _handlers.Keys;
    }
    
    /// <summary>
    /// Clears all registered handlers
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
        _messageTypeToContract.Clear();
    }
}
```

## Refactoring Plan

### Phase 1: Add New Constants and Enums
- [ ] Create TerrainGenerationConstants.cs
- [ ] Create WorldMapControlConstants.cs
- [ ] Create TerrainGenerationEnums.cs
- [ ] Create ExpandedBiomeEnums.cs
- [ ] Create ExpandedEntityEnums.cs

### Phase 2: Add Utilities
- [ ] Create MathUtilities.cs
- [ ] Create SerializationUtilities.cs
- [ ] Create ValidationUtilities.cs

### Phase 3: Add New Protocol Messages
- [ ] Create TerrainGenerationMessages.cs
- [ ] Create WorldMapControlMessages.cs
- [ ] Create HydrologyMessages.cs

### Phase 4: Refactor Existing Code
- [ ] Refactor Messages.cs to BaseMessages.cs
- [ ] Refactor MinecraftContainerMessages.cs to ContainerMessages.cs
- [ ] Refactor MessageDispatcher.cs to BaseMessageDispatcher.cs
- [ ] Complete TODO items in MinecraftMessageDispatcher.cs

### Phase 5: Update Project File
- [ ] Update SharedProtocol.csproj with new files
- [ ] Update protobuf references

### Phase 6: Update Protobuf Definitions
- [ ] Create terrain_generation.proto
- [ ] Create world_map_control.proto
- [ ] Create hydrology.proto

### Phase 7: Testing
- [ ] Unit tests for new constants
- [ ] Unit tests for new enums
- [ ] Unit tests for new utilities
- [ ] Unit tests for new messages
- [ ] Integration tests

## Migration Strategy

### Backward Compatibility

1. **Keep Existing Files:** Do not delete existing files until migration is complete
2. **Use Aliases:** Create using aliases for moved types
3. **Deprecation Warnings:** Add Obsolete attributes to deprecated code
4. **Gradual Migration:** Migrate one component at a time

### Example Migration Code

```csharp
// Old code (still works)
using SharedProtocol;
var messageType = MessageType.LoginRequest;

// New code (preferred)
using SharedProtocol.Messages;
using SharedProtocol.Common.Enums;
var messageType = BaseMessageType.LoginRequest;
```

## Implementation Priority

### High Priority (Session 122)
1. Add terrain generation constants
2. Add terrain generation enums
3. Add world map control constants
4. Create new protocol messages

### Medium Priority (Session 123)
5. Add expanded biome enums
6. Add expanded entity enums
7. Add common utilities
8. Complete TODO items in dispatcher

### Low Priority (Session 124)
9. Refactor existing code
10. Resolve enum duplication
11. Improve documentation

## Next Steps

1. **Review and approve** this architecture design
2. **Create implementation tasks** for each phase
3. **Implement Phase 1** (New constants and enums)
4. **Test thoroughly** before proceeding
5. **Continue through all phases**
6. **Document and deploy**

## References

- SharedProtocol DLL analysis
- Terrain generation algorithms
- World map control architecture
- Protobuf protocol analysis
- Minecraft standard biomes and entities

