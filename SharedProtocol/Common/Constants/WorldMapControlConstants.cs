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
    public const SharedProtocol.Common.Enums.WorldEnums.WorldMapDetailLevel DefaultDetailLevel =
        SharedProtocol.Common.Enums.WorldEnums.WorldMapDetailLevel.Detailed;
}
