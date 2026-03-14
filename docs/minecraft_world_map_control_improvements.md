# World Map Control Architecture Improvements

## Server-Side Improvements

### Current Implementation Status

The server-side world map control system is already well-implemented with the following features:

#### WorldMapControlManager.cs
- **Profile Management**: Player-specific map preferences with hot-reload support
- **Chunk Caching**: Efficient chunk caching with budget management
- **Enhanced Terrain Pipeline Integration**: Uses ImprovedTerrainCoordinator for hydrology-aware terrain generation
- **Generation Signature**: Cache invalidation based on world generation parameters
- **Profile Persistence**: JSON-based profile storage with hash verification

#### WorldMapController.cs
- **Async Chunk Generation**: Non-blocking chunk generation pipeline
- **Cleanup Timer**: Automatic cache cleanup
- **Profile Hot-Reload**: Automatic reload when config changes detected

### Recommended Improvements

#### 1. Client-Side Integration
```csharp
// Add to client protocol handler
public class WorldMapControlClient
{
    private WorldMapControlProfile localProfile;
    private Dictionary<int, ChunkData> chunkCache;
    private int renderDistance = 4;
    
    public void UpdateProfile(WorldMapProfile profile)
    {
        localProfile = profile;
        RequestProfileUpdate(profile);
    }
    
    public void SetRenderDistance(int distance)
    {
        renderDistance = Math.Clamp(distance, 2, 8);
        UpdateVisibleChunks();
    }
    
    private void UpdateVisibleChunks()
    {
        // Calculate visible chunks based on player position and render distance
        // Request new chunks, unload distant chunks
    }
}
```

#### 2. Real-Time Map Updates
```csharp
// Add to WorldMapControlManager
public async Task BroadcastMapUpdateAsync(int worldId, int chunkX, int chunkZ, ChunkData chunkData)
{
    var playersInChunk = SessionManager.GetPlayersInChunk(worldId, chunkX, chunkZ);
    if (playersInChunk.Any())
    {
        var update = new EnhancedMinecraftProtocol.ChunkData
        {
            ChunkX = chunkX,
            ChunkZ = chunkZ,
            BlockData = chunkData.BlockData,
            BiomeData = chunkData.BiomeData,
            LightData = chunkData.LightData,
            Entities = chunkData.Entities,
            TileEntities = chunkData.TileEntities,
            GenerationTimestamp = chunkData.GenerationTimestamp
        };
        
        await SessionManager.BroadcastMinecraftDualAsync(
            MinecraftMessageType.ChunkDataResponse,
            null,
            new EnhancedMinecraftProtocol.ChunkLoadResponse
            {
                Chunks = { update },
                TotalRequested = 1,
                TotalSent = 1
            });
    }
}
```

#### 3. Biome Information Display
```csharp
// Add biome data to chunk responses
public class BiomeData
{
    public int BiomeId { get; set; }
    public string BiomeName { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
    public string[] BlockTypes { get; set; }
}

// Enhanced chunk data with biome info
public class EnhancedChunkData : ChunkData
{
    public BiomeData[] BiomeData { get; set; }
    public Dictionary<int, int> BiomeMap { get; set; } // x,z -> biomeId
}
```

## Client-Side Improvements

### 1. World Map Control UI
```csharp
// Unity UI component
public class WorldMapControlUI : MonoBehaviour
{
    [SerializeField] private Slider renderDistanceSlider;
    [SerializeField] private Toggle showCoordinatesToggle;
    [SerializeField] private Toggle showBiomeInfoToggle;
    [SerializeField] private Dropdown terrainQualityDropdown;
    [SerializeField] private Dropdown waterQualityDropdown;
    [SerializeField] private Dropdown vegetationQualityDropdown;
    [SerializeField] private Button resetToDefaultsButton;
    
    private WorldMapControlProfile currentProfile;
    
    private void Start()
    {
        LoadCurrentProfile();
        SetupUIListeners();
    }
    
    private void LoadCurrentProfile()
    {
        // Fetch current profile from server
        NetworkManager.SendRequest(new WorldMapRequest
        {
            Type = WorldMapRequestType.GetPlayerProfile,
            PlayerId = NetworkManager.PlayerId
        }, OnProfileReceived);
    }
    
    private void OnProfileReceived(WorldMapResponse response)
    {
        if (response.Success && response.PlayerProfile != null)
        {
            currentProfile = response.PlayerProfile;
            UpdateUIFromProfile(currentProfile);
        }
    }
    
    private void UpdateUIFromProfile(WorldMapProfile profile)
    {
        renderDistanceSlider.value = profile.RenderDistance;
        showCoordinatesToggle.isOn = profile.ShowCoordinates;
        showBiomeInfoToggle.isOn = profile.ShowBiomeInfo;
        terrainQualityDropdown.value = profile.TerrainQuality;
        waterQualityDropdown.value = profile.WaterQuality;
        vegetationQualityDropdown.value = profile.VegetationQuality;
    }
    
    private void SetupUIListeners()
    {
        renderDistanceSlider.onValueChanged.AddListener(OnRenderDistanceChanged);
        showCoordinatesToggle.onValueChanged.AddListener(OnShowCoordinatesChanged);
        showBiomeInfoToggle.onValueChanged.AddListener(OnShowBiomeInfoChanged);
        terrainQualityDropdown.onValueChanged.AddListener(OnTerrainQualityChanged);
        waterQualityDropdown.onValueChanged.AddListener(OnWaterQualityChanged);
        vegetationQualityDropdown.onValueChanged.AddListener(OnVegetationQualityChanged);
        resetToDefaultsButton.onClick.AddListener(OnResetToDefaults);
    }
    
    private void OnRenderDistanceChanged(float value)
    {
        SendProfileUpdate(ProfileUpdateType.RenderDistance, (int)value);
    }
    
    private void OnShowCoordinatesChanged(bool value)
    {
        SendProfileUpdate(ProfileUpdateType.ShowCoordinates, value);
    }
    
    private void OnShowBiomeInfoChanged(bool value)
    {
        SendProfileUpdate(ProfileUpdateType.ShowBiomeInfo, value);
    }
    
    private void OnTerrainQualityChanged(int value)
    {
        SendProfileUpdate(ProfileUpdateType.TerrainQuality, value);
    }
    
    private void OnWaterQualityChanged(int value)
    {
        SendProfileUpdate(ProfileUpdateType.WaterQuality, value);
    }
    
    private void OnVegetationQualityChanged(int value)
    {
        SendProfileUpdate(ProfileUpdateType.VegetationQuality, value);
    }
    
    private void OnResetToDefaults()
    {
        var defaultProfile = new WorldMapProfile
        {
            RenderDistance = 4,
            MapScale = 1.0,
            ShowCoordinates = false,
            ShowBiomeInfo = false,
            TerrainQuality = 2,
            WaterQuality = 2,
            VegetationQuality = 2
        };
        
        SendFullProfileUpdate(defaultProfile);
    }
    
    private void SendProfileUpdate(ProfileUpdateType type, object value)
    {
        var update = new ProfileUpdate
        {
            Type = type,
            Value = value
        };
        
        NetworkManager.SendRequest(new WorldMapRequest
        {
            Type = WorldMapRequestType.UpdatePlayerProfile,
            PlayerId = NetworkManager.PlayerId,
            ProfileUpdates = new List<ProfileUpdate> { update }
        });
    }
    
    private void SendFullProfileUpdate(WorldMapProfile profile)
    {
        var updates = new List<ProfileUpdate>
        {
            new ProfileUpdate { Type = ProfileUpdateType.RenderDistance, Value = profile.RenderDistance },
            new ProfileUpdate { Type = ProfileUpdateType.MapScale, Number = profile.MapScale },
            new ProfileUpdate { Type = ProfileUpdateType.ShowCoordinates, Flag = profile.ShowCoordinates },
            new ProfileUpdate { Type = ProfileUpdateType.ShowBiomeInfo, Flag = profile.ShowBiomeInfo },
            new ProfileUpdate { Type = ProfileUpdateType.TerrainQuality, Value = profile.TerrainQuality },
            new ProfileUpdate { Type = ProfileUpdateType.WaterQuality, Value = profile.WaterQuality },
            new ProfileUpdate { Type = ProfileUpdateType.VegetationQuality, Value = profile.VegetationQuality }
        };
        
        NetworkManager.SendRequest(new WorldMapRequest
        {
            Type = WorldMapRequestType.UpdatePlayerProfile,
            PlayerId = NetworkManager.PlayerId,
            ProfileUpdates = updates
        });
    }
}
```

### 2. Mini-Map Display
```csharp
// Unity mini-map component
public class MiniMapDisplay : MonoBehaviour
{
    [SerializeField] private RawImage miniMapImage;
    [SerializeField] private int textureSize = 256;
    [SerializeField] private Color32[] biomeColors;
    
    private Texture2D miniMapTexture;
    private Color32[] textureData;
    private Dictionary<(int x, int z), ChunkData> visibleChunks;
    
    private void Start()
    {
        miniMapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        textureData = new Color32[textureSize * textureSize];
        miniMapImage.texture = miniMapTexture;
        
        NetworkManager.RegisterChunkDataHandler(OnChunkDataReceived);
    }
    
    private void OnChunkDataReceived(ChunkData chunkData)
    {
        visibleChunks[(chunkData.ChunkX, chunkData.ChunkZ)] = chunkData;
        UpdateMiniMap();
    }
    
    private void UpdateMiniMap()
    {
        // Clear texture
        Array.Clear(textureData, 0, textureData.Length);
        
        // Draw visible chunks
        foreach (var kvp in visibleChunks)
        {
            var (chunkX, chunkZ) = kvp.Key;
            var chunk = kvp.Value;
            
            if (chunk == null) continue;
            
            // Calculate position on mini-map
            int mapX = textureSize / 2 + (chunkX - PlayerChunkX) * 16;
            int mapZ = textureSize / 2 + (chunkZ - PlayerChunkZ) * 16;
            
            // Draw chunk based on biome
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int biomeId = GetBiomeAt(chunk, x, y);
                    Color32 color = biomeColors[biomeId % biomeColors.Length];
                    
                    int texX = mapX + x;
                    int texZ = mapZ + y;
                    
                    if (texX >= 0 && texX < textureSize && texZ >= 0 && texZ < textureSize)
                    {
                        textureData[texZ * textureSize + texX] = color;
                    }
                }
            }
        }
        
        miniMapTexture.SetPixels32(textureData);
        miniMapTexture.Apply();
    }
    
    private int GetBiomeAt(ChunkData chunk, int localX, int localY)
    {
        // Extract biome information from chunk data
        if (chunk.BiomeData != null && chunk.BiomeData.Length > 0)
        {
            int index = localY * 16 + localX;
            if (index < chunk.BiomeData.Length)
            {
                return chunk.BiomeData[index].BiomeId;
            }
        }
        return 0; // Default biome
    }
}
```

## Configuration File Improvements

### Enhanced Server Configuration
```json
{
  "worldMapControl": {
    "enabled": true,
    "profilePath": "config/world_map_control_profiles.json",
    "profileVersion": 1,
    "defaults": {
      "renderDistance": 4,
      "mapScale": 1.0,
      "showCoordinates": false,
      "showBiomeInfo": false,
      "terrainQuality": 2,
      "waterQuality": 2,
      "vegetationQuality": 2
    },
    "cache": {
      "maxCachedChunks": 256,
      "cleanupIntervalSeconds": 60,
      "enableChunkCache": true
    },
    "realTimeUpdates": {
      "enabled": true,
      "updateIntervalMs": 1000,
      "broadcastToChunkOnly": true
    }
  }
}
```

### Enhanced Client Configuration
```json
{
  "worldMapControl": {
    "ui": {
      "showMiniMap": true,
      "miniMapPosition": "bottom-right",
      "miniMapSize": 256,
      "miniMapOpacity": 0.9,
      "showPlayerMarker": true,
      "showChunkBorders": true
    },
    "display": {
      "showCoordinates": false,
      "showBiomeInfo": false,
      "showFps": true,
      "showPing": true
    },
    "performance": {
      "chunkUpdateThrottleMs": 50,
      "maxConcurrentChunkRequests": 8,
      "enableChunkPrediction": true
    }
  }
}
```

## Data-Driven Biome System

### Biome Configuration File
```json
{
  "biomes": [
    {
      "id": 0,
      "name": "Plains",
      "temperature": 0.5,
      "humidity": 0.5,
      "color": "#90A14D",
      "surfaceBlocks": [1, 2, 3],
      "undergroundBlocks": [3, 4, 5],
      "treeTypes": ["oak", "birch"],
      "grassTypes": ["tall_grass"],
      "flowerTypes": ["dandelion", "poppy"]
    },
    {
      "id": 1,
      "name": "Forest",
      "temperature": 0.7,
      "humidity": 0.6,
      "color": "#056621",
      "surfaceBlocks": [2, 3, 12],
      "undergroundBlocks": [3, 4],
      "treeTypes": ["oak", "dark_oak"],
      "grassTypes": ["tall_grass", "fern"],
      "flowerTypes": ["rose", "lily_of_the_valley"]
    },
    {
      "id": 2,
      "name": "Desert",
      "temperature": 2.0,
      "humidity": 0.0,
      "color": "#E8C758",
      "surfaceBlocks": [12, 24],
      "undergroundBlocks": [12, 13],
      "treeTypes": [],
      "grassTypes": ["dead_bush"],
      "flowerTypes": ["cactus"]
    },
    {
      "id": 3,
      "name": "Taiga",
      "temperature": 0.25,
      "humidity": 0.8,
      "color": "#307030",
      "surfaceBlocks": [7, 8, 9],
      "undergroundBlocks": [7, 8],
      "treeTypes": ["spruce", "pine"],
      "grassTypes": ["grass", "sweet_berry_bush"],
      "flowerTypes": ["large_fern"]
    },
    {
      "id": 4,
      "name": "Swamp",
      "temperature": 0.8,
      "humidity": 0.9,
      "color": "#2A7B99",
      "surfaceBlocks": [9, 10, 11],
      "undergroundBlocks": [9, 10],
      "treeTypes": ["oak"],
      "grassTypes": ["grass", "red_mushroom", "brown_mushroom"],
      "flowerTypes": ["lily_pad"],
      "waterColor": "#2A7B99"
    },
    {
      "id": 5,
      "name": "Ocean",
      "temperature": 0.5,
      "humidity": 1.0,
      "color": "#1E4B8C",
      "surfaceBlocks": [8, 9],
      "undergroundBlocks": [9, 10],
      "treeTypes": [],
      "grassTypes": [],
      "flowerTypes": [],
      "waterColor": "#1E4B8C"
    },
    {
      "id": 6,
      "name": "River",
      "temperature": 0.5,
      "humidity": 0.7,
      "color": "#3F76E4",
      "surfaceBlocks": [8, 9],
      "undergroundBlocks": [9, 10],
      "treeTypes": [],
      "grassTypes": ["grass"],
      "flowerTypes": [],
      "waterColor": "#3F76E4"
    },
    {
      "id": 7,
      "name": "Beach",
      "temperature": 0.8,
      "humidity": 0.4,
      "color": "#F2D299",
      "surfaceBlocks": [12, 7],
      "undergroundBlocks": [7],
      "treeTypes": [],
      "grassTypes": [],
      "flowerTypes": [],
      "waterColor": "#1E4B8C"
    },
    {
      "id": 8,
      "name": "Mountains",
      "temperature": 0.0,
      "humidity": 0.5,
      "color": "#808080",
      "surfaceBlocks": [1, 2, 3],
      "undergroundBlocks": [1, 2, 3],
      "treeTypes": ["spruce"],
      "grassTypes": ["grass", "stone"],
      "flowerTypes": [],
      "snowColor": "#FFFFFF"
    },
    {
      "id": 9,
      "name": "Snowy Tundra",
      "temperature": 0.0,
      "humidity": 0.0,
      "color": "#FFFFFF",
      "surfaceBlocks": [7, 8, 80],
      "undergroundBlocks": [7, 8],
      "treeTypes": ["spruce"],
      "grassTypes": ["grass"],
      "flowerTypes": [],
      "snowColor": "#FFFFFF"
    }
  ]
}
```

## Implementation Priority

### Phase 1: Core Improvements (High Priority)
1. ✅ Server-side world map control with profile management
2. ✅ Enhanced terrain generation with hydrology awareness
3. ✅ Protobuf protocol with dual support (legacy + enhanced)
4. ⏳ Client-side world map control UI
5. ⏳ Real-time chunk update broadcasting
6. ⏳ Biome information system

### Phase 2: Content Improvements (Medium Priority)
1. ⏳ Mini-map display component
2. ⏳ Biome-based terrain coloring
3. ⏳ Dynamic terrain quality adjustment
4. ⏳ Water and vegetation quality settings

### Phase 3: Utility Improvements (Low Priority)
1. ⏳ Performance monitoring and optimization
2. ⏳ Chunk prediction system
3. ⏳ Advanced caching strategies
4. ⏳ Debug and diagnostic tools

## Summary

The world map control system is well-architected with:
- **Server**: Robust profile management, chunk caching, and enhanced terrain generation
- **Protocol**: Dual protobuf support (protobuf-net + Google.Protobuf) for backward compatibility
- **Configuration**: JSON-based with hot-reload support
- **Extensibility**: Clean interfaces for client-side integration

**Next Steps**:
1. Implement client-side UI components
2. Add biome data to chunk responses
3. Create data-driven biome configuration
4. Implement real-time map update broadcasting
5. Add mini-map display component

## Server-Side Improvements

### Current Implementation Status

The server-side world map control system is already well-implemented with the following features:

#### WorldMapControlManager.cs
- **Profile Management**: Player-specific map preferences with hot-reload support
- **Chunk Caching**: Efficient chunk caching with budget management
- **Enhanced Terrain Pipeline Integration**: Uses ImprovedTerrainCoordinator for hydrology-aware terrain generation
- **Generation Signature**: Cache invalidation based on world generation parameters
- **Profile Persistence**: JSON-based profile storage with hash verification

#### WorldMapController.cs
- **Async Chunk Generation**: Non-blocking chunk generation pipeline
- **Cleanup Timer**: Automatic cache cleanup
- **Profile Hot-Reload**: Automatic reload when config changes detected

### Recommended Improvements

#### 1. Client-Side Integration
```csharp
// Add to client protocol handler
public class WorldMapControlClient
{
    private WorldMapControlProfile localProfile;
    private Dictionary<int, ChunkData> chunkCache;
    private int renderDistance = 4;
    
    public void UpdateProfile(WorldMapProfile profile)
    {
        localProfile = profile;
        RequestProfileUpdate(profile);
    }
    
    public void SetRenderDistance(int distance)
    {
        renderDistance = Math.Clamp(distance, 2, 8);
        UpdateVisibleChunks();
    }
    
    private void UpdateVisibleChunks()
    {
        // Calculate visible chunks based on player position and render distance
        // Request new chunks, unload distant chunks
    }
}
```

#### 2. Real-Time Map Updates
```csharp
// Add to WorldMapControlManager
public async Task BroadcastMapUpdateAsync(int worldId, int chunkX, int chunkZ, ChunkData chunkData)
{
    var playersInChunk = SessionManager.GetPlayersInChunk(worldId, chunkX, chunkZ);
    if (playersInChunk.Any())
    {
        var update = new EnhancedMinecraftProtocol.ChunkData
        {
            ChunkX = chunkX,
            ChunkZ = chunkZ,
            BlockData = chunkData.BlockData,
            BiomeData = chunkData.BiomeData,
            LightData = chunkData.LightData,
            Entities = chunkData.Entities,
            TileEntities = chunkData.TileEntities,
            GenerationTimestamp = chunkData.GenerationTimestamp
        };
        
        await SessionManager.BroadcastMinecraftDualAsync(
            MinecraftMessageType.ChunkDataResponse,
            null,
            new EnhancedMinecraftProtocol.ChunkLoadResponse
            {
                Chunks = { update },
                TotalRequested = 1,
                TotalSent = 1
            });
    }
}
```

#### 3. Biome Information Display
```csharp
// Add biome data to chunk responses
public class BiomeData
{
    public int BiomeId { get; set; }
    public string BiomeName { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
    public string[] BlockTypes { get; set; }
}

// Enhanced chunk data with biome info
public class EnhancedChunkData : ChunkData
{
    public BiomeData[] BiomeData { get; set; }
    public Dictionary<int, int> BiomeMap { get; set; } // x,z -> biomeId
}
```

## Client-Side Improvements

### 1. World Map Control UI
```csharp
// Unity UI component
public class WorldMapControlUI : MonoBehaviour
{
    [SerializeField] private Slider renderDistanceSlider;
    [SerializeField] private Toggle showCoordinatesToggle;
    [SerializeField] private Toggle showBiomeInfoToggle;
    [SerializeField] private Dropdown terrainQualityDropdown;
    [SerializeField] private Dropdown waterQualityDropdown;
    [SerializeField] private Dropdown vegetationQualityDropdown;
    [SerializeField] private Button resetToDefaultsButton;
    
    private WorldMapControlProfile currentProfile;
    
    private void Start()
    {
        LoadCurrentProfile();
        SetupUIListeners();
    }
    
    private void LoadCurrentProfile()
    {
        // Fetch current profile from server
        NetworkManager.SendRequest(new WorldMapRequest
        {
            Type = WorldMapRequestType.GetPlayerProfile,
            PlayerId = NetworkManager.PlayerId
        }, OnProfileReceived);
    }
    
    private void OnProfileReceived(WorldMapResponse response)
    {
        if (response.Success && response.PlayerProfile != null)
        {
            currentProfile = response.PlayerProfile;
            UpdateUIFromProfile(currentProfile);
        }
    }
    
    private void UpdateUIFromProfile(WorldMapProfile profile)
    {
        renderDistanceSlider.value = profile.RenderDistance;
        showCoordinatesToggle.isOn = profile.ShowCoordinates;
        showBiomeInfoToggle.isOn = profile.ShowBiomeInfo;
        terrainQualityDropdown.value = profile.TerrainQuality;
        waterQualityDropdown.value = profile.WaterQuality;
        vegetationQualityDropdown.value = profile.VegetationQuality;
    }
    
    private void SetupUIListeners()
    {
        renderDistanceSlider.onValueChanged.AddListener(OnRenderDistanceChanged);
        showCoordinatesToggle.onValueChanged.AddListener(OnShowCoordinatesChanged);
        showBiomeInfoToggle.onValueChanged.AddListener(OnShowBiomeInfoChanged);
        terrainQualityDropdown.onValueChanged.AddListener(OnTerrainQualityChanged);
        waterQualityDropdown.onValueChanged.AddListener(OnWaterQualityChanged);
        vegetationQualityDropdown.onValueChanged.AddListener(OnVegetationQualityChanged);
        resetToDefaultsButton.onClick.AddListener(OnResetToDefaults);
    }
    
    private void OnRenderDistanceChanged(float value)
    {
        SendProfileUpdate(ProfileUpdateType.RenderDistance, (int)value);
    }
    
    private void OnShowCoordinatesChanged(bool value)
    {
        SendProfileUpdate(ProfileUpdateType.ShowCoordinates, value);
    }
    
    private void OnShowBiomeInfoChanged(bool value)
    {
        SendProfileUpdate(ProfileUpdateType.ShowBiomeInfo, value);
    }
    
    private void OnTerrainQualityChanged(int value)
    {
        SendProfileUpdate(ProfileUpdateType.TerrainQuality, value);
    }
    
    private void OnWaterQualityChanged(int value)
    {
        SendProfileUpdate(ProfileUpdateType.WaterQuality, value);
    }
    
    private void OnVegetationQualityChanged(int value)
    {
        SendProfileUpdate(ProfileUpdateType.VegetationQuality, value);
    }
    
    private void OnResetToDefaults()
    {
        var defaultProfile = new WorldMapProfile
        {
            RenderDistance = 4,
            MapScale = 1.0,
            ShowCoordinates = false,
            ShowBiomeInfo = false,
            TerrainQuality = 2,
            WaterQuality = 2,
            VegetationQuality = 2
        };
        
        SendFullProfileUpdate(defaultProfile);
    }
    
    private void SendProfileUpdate(ProfileUpdateType type, object value)
    {
        var update = new ProfileUpdate
        {
            Type = type,
            Value = value
        };
        
        NetworkManager.SendRequest(new WorldMapRequest
        {
            Type = WorldMapRequestType.UpdatePlayerProfile,
            PlayerId = NetworkManager.PlayerId,
            ProfileUpdates = new List<ProfileUpdate> { update }
        });
    }
    
    private void SendFullProfileUpdate(WorldMapProfile profile)
    {
        var updates = new List<ProfileUpdate>
        {
            new ProfileUpdate { Type = ProfileUpdateType.RenderDistance, Value = profile.RenderDistance },
            new ProfileUpdate { Type = ProfileUpdateType.MapScale, Number = profile.MapScale },
            new ProfileUpdate { Type = ProfileUpdateType.ShowCoordinates, Flag = profile.ShowCoordinates },
            new ProfileUpdate { Type = ProfileUpdateType.ShowBiomeInfo, Flag = profile.ShowBiomeInfo },
            new ProfileUpdate { Type = ProfileUpdateType.TerrainQuality, Value = profile.TerrainQuality },
            new ProfileUpdate { Type = ProfileUpdateType.WaterQuality, Value = profile.WaterQuality },
            new ProfileUpdate { Type = ProfileUpdateType.VegetationQuality, Value = profile.VegetationQuality }
        };
        
        NetworkManager.SendRequest(new WorldMapRequest
        {
            Type = WorldMapRequestType.UpdatePlayerProfile,
            PlayerId = NetworkManager.PlayerId,
            ProfileUpdates = updates
        });
    }
}
```

### 2. Mini-Map Display
```csharp
// Unity mini-map component
public class MiniMapDisplay : MonoBehaviour
{
    [SerializeField] private RawImage miniMapImage;
    [SerializeField] private int textureSize = 256;
    [SerializeField] private Color32[] biomeColors;
    
    private Texture2D miniMapTexture;
    private Color32[] textureData;
    private Dictionary<(int x, int z), ChunkData> visibleChunks;
    
    private void Start()
    {
        miniMapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        textureData = new Color32[textureSize * textureSize];
        miniMapImage.texture = miniMapTexture;
        
        NetworkManager.RegisterChunkDataHandler(OnChunkDataReceived);
    }
    
    private void OnChunkDataReceived(ChunkData chunkData)
    {
        visibleChunks[(chunkData.ChunkX, chunkData.ChunkZ)] = chunkData;
        UpdateMiniMap();
    }
    
    private void UpdateMiniMap()
    {
        // Clear texture
        Array.Clear(textureData, 0, textureData.Length);
        
        // Draw visible chunks
        foreach (var kvp in visibleChunks)
        {
            var (chunkX, chunkZ) = kvp.Key;
            var chunk = kvp.Value;
            
            if (chunk == null) continue;
            
            // Calculate position on mini-map
            int mapX = textureSize / 2 + (chunkX - PlayerChunkX) * 16;
            int mapZ = textureSize / 2 + (chunkZ - PlayerChunkZ) * 16;
            
            // Draw chunk based on biome
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int biomeId = GetBiomeAt(chunk, x, y);
                    Color32 color = biomeColors[biomeId % biomeColors.Length];
                    
                    int texX = mapX + x;
                    int texZ = mapZ + y;
                    
                    if (texX >= 0 && texX < textureSize && texZ >= 0 && texZ < textureSize)
                    {
                        textureData[texZ * textureSize + texX] = color;
                    }
                }
            }
        }
        
        miniMapTexture.SetPixels32(textureData);
        miniMapTexture.Apply();
    }
    
    private int GetBiomeAt(ChunkData chunk, int localX, int localY)
    {
        // Extract biome information from chunk data
        if (chunk.BiomeData != null && chunk.BiomeData.Length > 0)
        {
            int index = localY * 16 + localX;
            if (index < chunk.BiomeData.Length)
            {
                return chunk.BiomeData[index].BiomeId;
            }
        }
        return 0; // Default biome
    }
}
```

## Configuration File Improvements

### Enhanced Server Configuration
```json
{
  "worldMapControl": {
    "enabled": true,
    "profilePath": "config/world_map_control_profiles.json",
    "profileVersion": 1,
    "defaults": {
      "renderDistance": 4,
      "mapScale": 1.0,
      "showCoordinates": false,
      "showBiomeInfo": false,
      "terrainQuality": 2,
      "waterQuality": 2,
      "vegetationQuality": 2
    },
    "cache": {
      "maxCachedChunks": 256,
      "cleanupIntervalSeconds": 60,
      "enableChunkCache": true
    },
    "realTimeUpdates": {
      "enabled": true,
      "updateIntervalMs": 1000,
      "broadcastToChunkOnly": true
    }
  }
}
```

### Enhanced Client Configuration
```json
{
  "worldMapControl": {
    "ui": {
      "showMiniMap": true,
      "miniMapPosition": "bottom-right",
      "miniMapSize": 256,
      "miniMapOpacity": 0.9,
      "showPlayerMarker": true,
      "showChunkBorders": true
    },
    "display": {
      "showCoordinates": false,
      "showBiomeInfo": false,
      "showFps": true,
      "showPing": true
    },
    "performance": {
      "chunkUpdateThrottleMs": 50,
      "maxConcurrentChunkRequests": 8,
      "enableChunkPrediction": true
    }
  }
}
```

## Data-Driven Biome System

### Biome Configuration File
```json
{
  "biomes": [
    {
      "id": 0,
      "name": "Plains",
      "temperature": 0.5,
      "humidity": 0.5,
      "color": "#90A14D",
      "surfaceBlocks": [1, 2, 3],
      "undergroundBlocks": [3, 4, 5],
      "treeTypes": ["oak", "birch"],
      "grassTypes": ["tall_grass"],
      "flowerTypes": ["dandelion", "poppy"]
    },
    {
      "id": 1,
      "name": "Forest",
      "temperature": 0.7,
      "humidity": 0.6,
      "color": "#056621",
      "surfaceBlocks": [2, 3, 12],
      "undergroundBlocks": [3, 4],
      "treeTypes": ["oak", "dark_oak"],
      "grassTypes": ["tall_grass", "fern"],
      "flowerTypes": ["rose", "lily_of_the_valley"]
    },
    {
      "id": 2,
      "name": "Desert",
      "temperature": 2.0,
      "humidity": 0.0,
      "color": "#E8C758",
      "surfaceBlocks": [12, 24],
      "undergroundBlocks": [12, 13],
      "treeTypes": [],
      "grassTypes": ["dead_bush"],
      "flowerTypes": ["cactus"]
    },
    {
      "id": 3,
      "name": "Taiga",
      "temperature": 0.25,
      "humidity": 0.8,
      "color": "#307030",
      "surfaceBlocks": [7, 8, 9],
      "undergroundBlocks": [7, 8],
      "treeTypes": ["spruce", "pine"],
      "grassTypes": ["grass", "sweet_berry_bush"],
      "flowerTypes": ["large_fern"]
    },
    {
      "id": 4,
      "name": "Swamp",
      "temperature": 0.8,
      "humidity": 0.9,
      "color": "#2A7B99",
      "surfaceBlocks": [9, 10, 11],
      "undergroundBlocks": [9, 10],
      "treeTypes": ["oak"],
      "grassTypes": ["grass", "red_mushroom", "brown_mushroom"],
      "flowerTypes": ["lily_pad"],
      "waterColor": "#2A7B99"
    },
    {
      "id": 5,
      "name": "Ocean",
      "temperature": 0.5,
      "humidity": 1.0,
      "color": "#1E4B8C",
      "surfaceBlocks": [8, 9],
      "undergroundBlocks": [9, 10],
      "treeTypes": [],
      "grassTypes": [],
      "flowerTypes": [],
      "waterColor": "#1E4B8C"
    },
    {
      "id": 6,
      "name": "River",
      "temperature": 0.5,
      "humidity": 0.7,
      "color": "#3F76E4",
      "surfaceBlocks": [8, 9],
      "undergroundBlocks": [9, 10],
      "treeTypes": [],
      "grassTypes": ["grass"],
      "flowerTypes": [],
      "waterColor": "#3F76E4"
    },
    {
      "id": 7,
      "name": "Beach",
      "temperature": 0.8,
      "humidity": 0.4,
      "color": "#F2D299",
      "surfaceBlocks": [12, 7],
      "undergroundBlocks": [7],
      "treeTypes": [],
      "grassTypes": [],
      "flowerTypes": [],
      "waterColor": "#1E4B8C"
    },
    {
      "id": 8,
      "name": "Mountains",
      "temperature": 0.0,
      "humidity": 0.5,
      "color": "#808080",
      "surfaceBlocks": [1, 2, 3],
      "undergroundBlocks": [1, 2, 3],
      "treeTypes": ["spruce"],
      "grassTypes": ["grass", "stone"],
      "flowerTypes": [],
      "snowColor": "#FFFFFF"
    },
    {
      "id": 9,
      "name": "Snowy Tundra",
      "temperature": 0.0,
      "humidity": 0.0,
      "color": "#FFFFFF",
      "surfaceBlocks": [7, 8, 80],
      "undergroundBlocks": [7, 8],
      "treeTypes": ["spruce"],
      "grassTypes": ["grass"],
      "flowerTypes": [],
      "snowColor": "#FFFFFF"
    }
  ]
}
```

## Implementation Priority

### Phase 1: Core Improvements (High Priority)
1. ✅ Server-side world map control with profile management
2. ✅ Enhanced terrain generation with hydrology awareness
3. ✅ Protobuf protocol with dual support (legacy + enhanced)
4. ⏳ Client-side world map control UI
5. ⏳ Real-time chunk update broadcasting
6. ⏳ Biome information system

### Phase 2: Content Improvements (Medium Priority)
1. ⏳ Mini-map display component
2. ⏳ Biome-based terrain coloring
3. ⏳ Dynamic terrain quality adjustment
4. ⏳ Water and vegetation quality settings

### Phase 3: Utility Improvements (Low Priority)
1. ⏳ Performance monitoring and optimization
2. ⏳ Chunk prediction system
3. ⏳ Advanced caching strategies
4. ⏳ Debug and diagnostic tools

## Summary

The world map control system is well-architected with:
- **Server**: Robust profile management, chunk caching, and enhanced terrain generation
- **Protocol**: Dual protobuf support (protobuf-net + Google.Protobuf) for backward compatibility
- **Configuration**: JSON-based with hot-reload support
- **Extensibility**: Clean interfaces for client-side integration

**Next Steps**:
1. Implement client-side UI components
2. Add biome data to chunk responses
3. Create data-driven biome configuration
4. Implement real-time map update broadcasting
5. Add mini-map display component

