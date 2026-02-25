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
    public const Enums.TerrainQualityLevel DefaultQuality = Enums.TerrainQualityLevel.Medium;
    
    /// <summary>
    /// Default terrain generation mode
    /// </summary>
    public const Enums.TerrainGenerationMode DefaultMode = Enums.TerrainGenerationMode.Standard;
    
    #endregion
}

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
    public const Enums.TerrainQualityLevel DefaultQuality = Enums.TerrainQualityLevel.Medium;
    
    /// <summary>
    /// Default terrain generation mode
    /// </summary>
    public const Enums.TerrainGenerationMode DefaultMode = Enums.TerrainGenerationMode.Standard;
    
    #endregion
}

