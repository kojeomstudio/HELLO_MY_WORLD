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
