namespace SharedProtocol.Common.Enums;

/// <summary>
/// World-related enumeration types
/// </summary>
public static class WorldEnums
{
    /// <summary>
    /// World generation types
    /// </summary>
    public enum WorldType
    {
        Normal = 0,
        Flat = 1,
        LargeBiomes = 2,
        Amplified = 3,
        Debug = 4,
        Custom = 5
    }
    
    /// <summary>
    /// Weather types
    /// </summary>
    public enum WeatherType
    {
        Clear = 0,
        Rain = 1,
        Storm = 2,
        Snow = 3
    }
    
    /// <summary>
    /// Chunk unload reasons
    /// </summary>
    public enum ChunkUnloadReason
    {
        UnloadViewDistance = 0,
        UnloadManual = 1,
        UnloadWorldTransfer = 2,
        UnloadShutdown = 3
    }
    
    /// <summary>
    /// Entity spawn reasons
    /// </summary>
    public enum SpawnReason
    {
        Natural = 0,
        Spawner = 1,
        Breeding = 2,
        Command = 3,
        ItemDrop = 4,
        Projectile = 5
    }
    
    /// <summary>
    /// Entity despawn reasons
    /// </summary>
    public enum DespawnReason
    {
        Natural = 0,
        Death = 1,
        Pickup = 2,
        ChunkUnload = 3,
        Command = 4
    }

    /// <summary>
    /// Detail level for world map region payloads.
    /// </summary>
    public enum WorldMapDetailLevel
    {
        Minimal = 0,
        Standard = 1,
        Detailed = 2,
        Full = 3
    }

    /// <summary>
    /// Update categories for world map broadcast payloads.
    /// </summary>
    public enum MapUpdateType
    {
        Unknown = 0,
        TerrainChanged = 1,
        WaterChanged = 2,
        BiomeChanged = 3,
        StructuresChanged = 4,
        RegionInvalidated = 5
    }
}
