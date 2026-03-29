# World Map Control Architecture Improvements Design Document

**Date:** 2026-01-17  
**Project:** Enhanced Minecraft Game  
**Status:** Design document for world map control client-side integration and improvements

---

## Overview

This document outlines improvements to the world map control system, specifically focusing on client-side integration, real-time updates, and enhanced user experience. The server-side implementation is already complete with [`WorldMapControlManager`](GameServer/World/WorldMapControlManager.cs), but the client-side integration needs to be completed.

---

## Current Implementation Status

### Server-Side Components

| Component | File | Status | Description |
|-----------|------|---------|-------------|
| WorldMapControlManager | `GameServer/World/WorldMapControlManager.cs` | ✅ Complete | Lightweight world map control service with caching |
| WorldMapController | `GameServer/World/WorldMapController.cs` | ✅ Complete | World map control request handler |
| WorldMapControlProfile | Configuration | ✅ Complete | Profile-based map preferences |

### Client-Side Components

| Component | File | Status | Description |
|-----------|------|---------|-------------|
| EnhancedWorldMapController | `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs` | ⚠️ Partial | Client-side world map controller exists |
| WorldMapControlSystem | `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs` | ⚠️ Partial | World map control system exists |
| WorldMapControlUI | - | ❌ Not Started | UI for world map display |
| MiniMapDisplay | - | ❌ Not Started | Mini-map overlay display |

### Configuration Files

| File | Status | Description |
|------|---------|-------------|
| `config/enhanced_world_map_control_server.json` | ✅ Complete | Server-side map control configuration |
| `config/enhanced_world_map_control_client.json` | ✅ Complete | Client-side map control configuration |

---

## Proposed Improvements

### 1. Complete Client-Side Integration

#### 1.1 WorldMapControlClient Class

**Current Limitation**: Client-side world map control is incomplete and not fully integrated with server.

**Improvement**: Implement complete [`WorldMapControlClient`](Assets/Scripts/Minecraft/World/WorldMapControlClient.cs) class for client-side map control.

```csharp
using UnityEngine;
using Game.Move;
using Game.World;

namespace Minecraft.World
{
    /// <summary>
    /// Client-side world map control system for real-time map updates.
    /// </summary>
    public class WorldMapControlClient : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WorldMapControlConfig _config;
        
        private WorldMapControlProfile _currentProfile;
        private Texture2D _mapTexture;
        private Color[] _mapPixels;
        private bool _isMapVisible = true;
        private Vector2Int _lastPlayerChunk;
        
        public bool IsMapVisible => _isMapVisible;
        public WorldMapControlProfile CurrentProfile => _currentProfile;
        
        private void Start()
        {
            LoadProfile();
            InitializeMapTexture();
            SubscribeToEvents();
        }
        
        private void LoadProfile()
        {
            // Load profile from configuration
            var profileData = Resources.Load<TextAsset>("Config/world_map_control_profile");
            if (profileData != null)
            {
                _currentProfile = JsonUtility.FromJson<WorldMapControlProfile>(profileData.text);
            }
            else
            {
                _currentProfile = WorldMapControlProfile.GetDefault();
            }
        }
        
        private void InitializeMapTexture()
        {
            int textureSize = _config.renderDistance * 2 + 1;
            _mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            _mapPixels = new Color[textureSize * textureSize];
            _mapTexture.filterMode = FilterMode.Point;
            _mapTexture.wrapMode = TextureWrapMode.Clamp;
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to world update events
            MinecraftNetworkClient.Instance.OnChunkDataReceived += OnChunkDataReceived;
            MinecraftNetworkClient.Instance.OnPlayerPositionChanged += OnPlayerPositionChanged;
        }
        
        private void OnChunkDataReceived(ChunkDataResponse chunkData)
        {
            UpdateMapForChunk(chunkData);
        }
        
        private void OnPlayerPositionChanged(Vector3 position)
        {
            Vector2Int playerChunk = new Vector2Int(
                Mathf.FloorToInt(position.x / 16f),
                Mathf.FloorToInt(position.z / 16f)
            );
            
            if (playerChunk != _lastPlayerChunk)
            {
                _lastPlayerChunk = playerChunk;
                RequestMapUpdate(playerChunk);
            }
        }
        
        private void UpdateMapForChunk(ChunkDataResponse chunkData)
        {
            // Update map texture with chunk data
            Vector2Int chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkZ);
            UpdateMapPixels(chunkPos, chunkData);
            _mapTexture.Apply();
        }
        
        private void UpdateMapPixels(Vector2Int chunkPos, ChunkDataResponse chunkData)
        {
            int textureSize = _config.renderDistance * 2 + 1;
            int centerOffset = _config.renderDistance;
            
            // Calculate texture position
            int textureX = centerOffset + (chunkPos.x - _lastPlayerChunk.x);
            int textureZ = centerOffset + (chunkPos.z - _lastPlayerChunk.z);
            
            // Check bounds
            if (textureX < 0 || textureX >= textureSize || 
                textureZ < 0 || textureZ >= textureSize)
            {
                return;
            }
            
            // Process chunk data and update pixels
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int pixelX = textureX + x;
                    int pixelZ = textureZ + z;
                    int pixelIndex = pixelZ * textureSize + pixelX;
                    
                    // Get block type from chunk data
                    int blockType = GetBlockType(chunkData, x, z);
                    Color mapColor = GetMapColor(blockType);
                    
                    _mapPixels[pixelIndex] = mapColor;
                }
            }
        }
        
        private int GetBlockType(ChunkDataResponse chunkData, int x, int z)
        {
            // Extract block type from chunk data
            // This depends on the actual chunk data format
            return chunkData.Blocks[z * 16 + x];
        }
        
        private Color GetMapColor(int blockType)
        {
            // Map block type to map color
            // Can be configured in profile
            return _currentProfile.GetBlockColor(blockType);
        }
        
        private void RequestMapUpdate(Vector2Int playerChunk)
        {
            // Send request to server for map data around player
            var request = new WorldMapDataRequest
            {
                CenterChunkX = playerChunk.x,
                CenterChunkZ = playerChunk.z,
                RenderDistance = _config.renderDistance,
                ProfileHash = _currentProfile.GetHash()
            };
            
            MinecraftNetworkClient.Instance.SendWorldMapRequest(request);
        }
        
        public void ToggleMapVisibility()
        {
            _isMapVisible = !_isMapVisible;
        }
        
        public void SetProfile(WorldMapControlProfile profile)
        {
            _currentProfile = profile;
            RefreshMap();
        }
        
        private void RefreshMap()
        {
            // Clear and regenerate map
            Array.Clear(_mapPixels, 0, _mapPixels.Length);
            _mapTexture.Apply();
            
            // Request fresh data from server
            RequestMapUpdate(_lastPlayerChunk);
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void UnsubscribeFromEvents()
        {
            if (MinecraftNetworkClient.Instance != null)
            {
                MinecraftNetworkClient.Instance.OnChunkDataReceived -= OnChunkDataReceived;
                MinecraftNetworkClient.Instance.OnPlayerPositionChanged -= OnPlayerPositionChanged;
            }
        }
        
        public Texture2D GetMapTexture()
        {
            return _mapTexture;
        }
    }
}
```

#### 1.2 WorldMapControlUI Class

**Current Limitation**: No UI exists for world map display and interaction.

**Improvement**: Implement [`WorldMapControlUI`](Assets/Scripts/Minecraft/UI/WorldMapControlUI.cs) for map display.

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    /// <summary>
    /// UI for world map control display and interaction.
    /// </summary>
    public class WorldMapControlUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _mapDisplay;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Slider _zoomSlider;
        [SerializeField] private Toggle _showBiomesToggle;
        [SerializeField] private Toggle _showHeightToggle;
        
        private WorldMapControlClient _mapClient;
        private float _currentZoom = 1.0f;
        private bool _showBiomes = true;
        private bool _showHeight = false;
        
        private void Start()
        {
            _mapClient = FindObjectOfType<WorldMapControlClient>();
            InitializeUI();
            SubscribeToEvents();
        }
        
        private void InitializeUI()
        {
            // Set up button listeners
            _toggleButton.onClick.AddListener(OnToggleClicked);
            _refreshButton.onClick.AddListener(OnRefreshClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            
            // Set up slider listener
            _zoomSlider.onValueChanged.AddListener(OnZoomChanged);
            
            // Set up toggle listeners
            _showBiomesToggle.onValueChanged.AddListener(OnShowBiomesChanged);
            _showHeightToggle.onValueChanged.AddListener(OnShowHeightChanged);
            
            // Set initial values
            _zoomSlider.value = _currentZoom;
            _showBiomesToggle.isOn = _showBiomes;
            _showHeightToggle.isOn = _showHeight;
            
            // Update map display
            UpdateMapDisplay();
        }
        
        private void SubscribeToEvents()
        {
            if (_mapClient != null)
            {
                // Subscribe to map updates
                _mapClient.OnMapUpdated += OnMapUpdated;
            }
        }
        
        private void OnToggleClicked()
        {
            _mapClient.ToggleMapVisibility();
            UpdateMapDisplay();
        }
        
        private void OnRefreshClicked()
        {
            _mapClient.RefreshMap();
        }
        
        private void OnSettingsClicked()
        {
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }
        
        private void OnZoomChanged(float value)
        {
            _currentZoom = value;
            UpdateMapDisplay();
        }
        
        private void OnShowBiomesChanged(bool value)
        {
            _showBiomes = value;
            UpdateMapDisplay();
        }
        
        private void OnShowHeightChanged(bool value)
        {
            _showHeight = value;
            UpdateMapDisplay();
        }
        
        private void OnMapUpdated(Texture2D mapTexture)
        {
            // Update map display texture
            _mapDisplay.texture = mapTexture;
            UpdateMapDisplay();
        }
        
        private void UpdateMapDisplay()
        {
            // Update map visibility
            _mapDisplay.gameObject.SetActive(_mapClient.IsMapVisible);
            
            // Update zoom
            _mapDisplay.rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1.0f);
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_mapClient != null)
            {
                _mapClient.OnMapUpdated -= OnMapUpdated;
            }
        }
    }
}
```

### 2. Real-Time Map Updates

#### 2.1 Chunk-Based Updates

**Current Limitation**: Map updates are infrequent and not real-time.

**Improvement**: Implement real-time chunk-based map updates as chunks are loaded.

```csharp
// Enhanced method in WorldMapControlClient
private void OnChunkDataReceived(ChunkDataResponse chunkData)
{
    // Update map for each chunk as it's received
    UpdateMapForChunk(chunkData);
    
    // Trigger map updated event
    OnMapUpdated?.Invoke(_mapTexture);
}
```

#### 2.2 Player Position Tracking

**Current Limitation**: Player position tracking is basic.

**Improvement**: Implement smooth player position tracking with interpolation.

```csharp
// Enhanced method in WorldMapControlClient
private void OnPlayerPositionChanged(Vector3 position)
{
    Vector2Int playerChunk = new Vector2Int(
        Mathf.FloorToInt(position.x / 16f),
        Mathf.FloorToInt(position.z / 16f)
    );
    
    // Interpolate map center for smooth movement
    StartCoroutine(InterpolateMapCenter(_lastPlayerChunk, playerChunk));
    
    _lastPlayerChunk = playerChunk;
}

private IEnumerator InterpolateMapCenter(Vector2Int fromChunk, Vector2Int toChunk)
{
    float duration = 0.5f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        // Interpolate chunk position
        Vector2Int currentChunk = Vector2Int.Lerp(fromChunk, toChunk, t);
        
        // Update map center
        UpdateMapCenter(currentChunk);
        
        yield return null;
    }
    
    // Ensure final position
    UpdateMapCenter(toChunk);
}
```

### 3. Biome Information Display

#### 3.1 Biome Overlay

**Current Limitation**: No biome information is displayed on the map.

**Improvement**: Implement biome overlay with color coding and labels.

```csharp
// Enhanced method in WorldMapControlClient
private Color GetMapColor(int blockType, int biomeType = -1)
{
    // Get base block color
    Color baseColor = _currentProfile.GetBlockColor(blockType);
    
    // Apply biome overlay if enabled
    if (_showBiomes && biomeType >= 0)
    {
        Color biomeColor = _currentProfile.GetBiomeColor(biomeType);
        return Color.Lerp(baseColor, biomeColor, _config.biomeOverlayOpacity);
    }
    
    return baseColor;
}
```

#### 3.2 Biome Labels

**Current Limitation**: No biome labels are displayed on the map.

**Improvement**: Implement biome labels with hover information.

```csharp
// New method in WorldMapControlUI
public void OnMapPointerEnter(PointerEventData eventData)
{
    // Get pointer position on map
    Vector2 localPoint;
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
        eventData.position, 
        _mapDisplay.rectTransform, 
        eventData.pressEventCamera, 
        out localPoint))
    {
        return;
    }
    
    // Convert to chunk coordinates
    Vector2Int mapPos = GetMapPositionFromLocalPoint(localPoint);
    Vector2Int chunkPos = GetChunkPositionFromMapPosition(mapPos);
    
    // Get biome at position
    int biomeType = GetBiomeAtChunk(chunkPos);
    
    // Show biome tooltip
    ShowBiomeTooltip(biomeType, eventData.position);
}

private void ShowBiomeTooltip(int biomeType, Vector2 screenPosition)
{
    string biomeName = _currentProfile.GetBiomeName(biomeType);
    
    // Show tooltip at screen position
    _tooltipText.text = biomeName;
    _tooltipPanel.transform.position = screenPosition;
    _tooltipPanel.SetActive(true);
}
```

### 4. Enhanced Map Features

#### 4.1 Height Visualization

**Current Limitation**: No height information is visualized on the map.

**Improvement**: Implement height-based shading for terrain visualization.

```csharp
// Enhanced method in WorldMapControlClient
private Color GetMapColor(int blockType, int biomeType = -1, int height = 0)
{
    // Get base block color
    Color baseColor = _currentProfile.GetBlockColor(blockType);
    
    // Apply biome overlay if enabled
    if (_showBiomes && biomeType >= 0)
    {
        Color biomeColor = _currentProfile.GetBiomeColor(biomeType);
        baseColor = Color.Lerp(baseColor, biomeColor, _config.biomeOverlayOpacity);
    }
    
    // Apply height shading if enabled
    if (_showHeight)
    {
        float heightFactor = (float)height / WorldHeight;
        Color heightColor = Color.Lerp(Color.black, Color.white, heightFactor);
        baseColor = Color.Lerp(baseColor, heightColor, _config.heightShadeOpacity);
    }
    
    return baseColor;
}
```

#### 4.2 Mini-Map Display

**Current Limitation**: No mini-map overlay exists.

**Improvement**: Implement mini-map display in corner of screen.

```csharp
// New class: MiniMapDisplay.cs
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    /// <summary>
    /// Mini-map display overlay for world navigation.
    /// </summary>
    public class MiniMapDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _miniMapDisplay;
        [SerializeField] private Image _playerIndicator;
        [SerializeField] private Image _directionIndicator;
        
        [Header("Configuration")]
        [SerializeField] private int _miniMapSize = 128;
        [SerializeField] private float _updateInterval = 0.5f;
        
        private WorldMapControlClient _mapClient;
        private float _lastUpdateTime;
        
        private void Start()
        {
            _mapClient = FindObjectOfType<WorldMapControlClient>();
            InitializeMiniMap();
        }
        
        private void InitializeMiniMap()
        {
            // Create mini-map texture
            Texture2D miniMapTexture = new Texture2D(_miniMapSize, _miniMapSize, TextureFormat.RGB24, false);
            miniMapTexture.filterMode = FilterMode.Point;
            _miniMapDisplay.texture = miniMapTexture;
        }
        
        private void Update()
        {
            // Update mini-map at interval
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateMiniMap();
                _lastUpdateTime = Time.time;
            }
            
            // Update player indicator
            UpdatePlayerIndicator();
        }
        
        private void UpdateMiniMap()
        {
            // Get current map texture
            Texture2D mapTexture = _mapClient.GetMapTexture();
            
            // Crop center region for mini-map
            int cropSize = _miniMapSize;
            int cropX = (mapTexture.width - cropSize) / 2;
            int cropY = (mapTexture.height - cropSize) / 2;
            
            Color[] pixels = mapTexture.GetPixels(cropX, cropY, cropSize, cropSize);
            
            // Apply to mini-map texture
            _miniMapDisplay.texture.SetPixels(pixels);
            _miniMapDisplay.texture.Apply();
        }
        
        private void UpdatePlayerIndicator()
        {
            // Center player indicator
            _playerIndicator.rectTransform.anchoredPosition = Vector2.zero;
            
            // Update direction indicator based on player rotation
            if (MinecraftPlayerController.Instance != null)
            {
                float playerRotation = MinecraftPlayerController.Instance.transform.rotation.eulerAngles.y;
                _directionIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, -playerRotation);
            }
        }
    }
}
```

### 5. Configuration Improvements

#### 5.1 Enhanced Client Configuration

```json
{
  "worldMapControl": {
    "enabled": true,
    "renderDistance": 8,
    "updateInterval": 0.5,
    "zoomLevel": 1.0,
    "minZoom": 0.5,
    "maxZoom": 2.0,
    "showBiomes": true,
    "showHeight": false,
    "biomeOverlayOpacity": 0.3,
    "heightShadeOpacity": 0.2,
    "blockColors": {
      "0": "#7F7F7F",
      "1": "#8B8B8B",
      "2": "#A0522D",
      "3": "#7CFC00",
      "4": "#FFFFFF",
      "5": "#FFD700",
      "6": "#C0C0C0",
      "7": "#0000FF",
      "8": "#FFA500",
      "9": "#808080",
      "10": "#000000",
      "11": "#00FF00"
    },
    "biomeColors": {
      "plains": "#90C050",
      "forest": "#228B22",
      "desert": "#F4A460",
      "mountains": "#808080",
      "taiga": "#2E8B57",
      "swamp": "#4B3621",
      "river": "#0000FF",
      "ocean": "#000080",
      "beach": "#F4A460"
    },
    "miniMap": {
      "enabled": true,
      "size": 128,
      "position": "bottomRight",
      "opacity": 0.8
    },
    "profiles": [
      {
        "name": "Default",
        "renderDistance": 8,
        "showBiomes": true,
        "showHeight": false
      },
      {
        "name": "Detailed",
        "renderDistance": 12,
        "showBiomes": true,
        "showHeight": true
      },
      {
        "name": "Performance",
        "renderDistance": 6,
        "showBiomes": false,
        "showHeight": false
      }
    ]
  }
}
```

---

## Implementation Plan

### Phase 1: Client-Side Integration (High Priority)
1. Implement WorldMapControlClient class
2. Implement WorldMapControlUI class
3. Add protobuf message types for map requests
4. Implement server-side map data handler
5. Test client-server map synchronization

### Phase 2: Real-Time Updates (High Priority)
1. Implement chunk-based map updates
2. Add player position tracking
3. Implement smooth map center interpolation
4. Optimize update frequency

### Phase 3: Enhanced Features (Medium Priority)
1. Implement biome overlay
2. Add biome labels with tooltips
3. Implement height visualization
4. Add mini-map display

### Phase 4: Configuration and Polish (Medium Priority)
1. Update client configuration file
2. Add profile management
3. Implement settings persistence
4. Add UI polish and animations

---

## Testing Strategy

### Unit Tests
- Test WorldMapControlClient initialization
- Test map texture updates
- Test profile loading and switching
- Test color mapping functions

### Integration Tests
- Test client-server map request/response
- Test real-time chunk updates
- Test player position tracking
- Test biome information display

### Performance Tests
- Measure map update frequency
- Profile memory usage
- Test with large render distances
- Optimize texture updates

---

## Notes

- Client-side integration should maintain compatibility with existing server implementation
- Real-time updates should be optimized to avoid performance issues
- UI should be responsive and intuitive
- Configuration changes should be hot-reloadable
- Mini-map should be optional and configurable

**Date:** 2026-01-17  
**Project:** Enhanced Minecraft Game  
**Status:** Design document for world map control client-side integration and improvements

---

## Overview

This document outlines improvements to the world map control system, specifically focusing on client-side integration, real-time updates, and enhanced user experience. The server-side implementation is already complete with [`WorldMapControlManager`](GameServer/World/WorldMapControlManager.cs), but the client-side integration needs to be completed.

---

## Current Implementation Status

### Server-Side Components

| Component | File | Status | Description |
|-----------|------|---------|-------------|
| WorldMapControlManager | `GameServer/World/WorldMapControlManager.cs` | ✅ Complete | Lightweight world map control service with caching |
| WorldMapController | `GameServer/World/WorldMapController.cs` | ✅ Complete | World map control request handler |
| WorldMapControlProfile | Configuration | ✅ Complete | Profile-based map preferences |

### Client-Side Components

| Component | File | Status | Description |
|-----------|------|---------|-------------|
| EnhancedWorldMapController | `Assets/Scripts/Minecraft/World/EnhancedWorldMapController.cs` | ⚠️ Partial | Client-side world map controller exists |
| WorldMapControlSystem | `Assets/Scripts/Minecraft/World/WorldMapControlSystem.cs` | ⚠️ Partial | World map control system exists |
| WorldMapControlUI | - | ❌ Not Started | UI for world map display |
| MiniMapDisplay | - | ❌ Not Started | Mini-map overlay display |

### Configuration Files

| File | Status | Description |
|------|---------|-------------|
| `config/enhanced_world_map_control_server.json` | ✅ Complete | Server-side map control configuration |
| `config/enhanced_world_map_control_client.json` | ✅ Complete | Client-side map control configuration |

---

## Proposed Improvements

### 1. Complete Client-Side Integration

#### 1.1 WorldMapControlClient Class

**Current Limitation**: Client-side world map control is incomplete and not fully integrated with server.

**Improvement**: Implement complete [`WorldMapControlClient`](Assets/Scripts/Minecraft/World/WorldMapControlClient.cs) class for client-side map control.

```csharp
using UnityEngine;
using Game.Move;
using Game.World;

namespace Minecraft.World
{
    /// <summary>
    /// Client-side world map control system for real-time map updates.
    /// </summary>
    public class WorldMapControlClient : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WorldMapControlConfig _config;
        
        private WorldMapControlProfile _currentProfile;
        private Texture2D _mapTexture;
        private Color[] _mapPixels;
        private bool _isMapVisible = true;
        private Vector2Int _lastPlayerChunk;
        
        public bool IsMapVisible => _isMapVisible;
        public WorldMapControlProfile CurrentProfile => _currentProfile;
        
        private void Start()
        {
            LoadProfile();
            InitializeMapTexture();
            SubscribeToEvents();
        }
        
        private void LoadProfile()
        {
            // Load profile from configuration
            var profileData = Resources.Load<TextAsset>("Config/world_map_control_profile");
            if (profileData != null)
            {
                _currentProfile = JsonUtility.FromJson<WorldMapControlProfile>(profileData.text);
            }
            else
            {
                _currentProfile = WorldMapControlProfile.GetDefault();
            }
        }
        
        private void InitializeMapTexture()
        {
            int textureSize = _config.renderDistance * 2 + 1;
            _mapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            _mapPixels = new Color[textureSize * textureSize];
            _mapTexture.filterMode = FilterMode.Point;
            _mapTexture.wrapMode = TextureWrapMode.Clamp;
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to world update events
            MinecraftNetworkClient.Instance.OnChunkDataReceived += OnChunkDataReceived;
            MinecraftNetworkClient.Instance.OnPlayerPositionChanged += OnPlayerPositionChanged;
        }
        
        private void OnChunkDataReceived(ChunkDataResponse chunkData)
        {
            UpdateMapForChunk(chunkData);
        }
        
        private void OnPlayerPositionChanged(Vector3 position)
        {
            Vector2Int playerChunk = new Vector2Int(
                Mathf.FloorToInt(position.x / 16f),
                Mathf.FloorToInt(position.z / 16f)
            );
            
            if (playerChunk != _lastPlayerChunk)
            {
                _lastPlayerChunk = playerChunk;
                RequestMapUpdate(playerChunk);
            }
        }
        
        private void UpdateMapForChunk(ChunkDataResponse chunkData)
        {
            // Update map texture with chunk data
            Vector2Int chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkZ);
            UpdateMapPixels(chunkPos, chunkData);
            _mapTexture.Apply();
        }
        
        private void UpdateMapPixels(Vector2Int chunkPos, ChunkDataResponse chunkData)
        {
            int textureSize = _config.renderDistance * 2 + 1;
            int centerOffset = _config.renderDistance;
            
            // Calculate texture position
            int textureX = centerOffset + (chunkPos.x - _lastPlayerChunk.x);
            int textureZ = centerOffset + (chunkPos.z - _lastPlayerChunk.z);
            
            // Check bounds
            if (textureX < 0 || textureX >= textureSize || 
                textureZ < 0 || textureZ >= textureSize)
            {
                return;
            }
            
            // Process chunk data and update pixels
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int pixelX = textureX + x;
                    int pixelZ = textureZ + z;
                    int pixelIndex = pixelZ * textureSize + pixelX;
                    
                    // Get block type from chunk data
                    int blockType = GetBlockType(chunkData, x, z);
                    Color mapColor = GetMapColor(blockType);
                    
                    _mapPixels[pixelIndex] = mapColor;
                }
            }
        }
        
        private int GetBlockType(ChunkDataResponse chunkData, int x, int z)
        {
            // Extract block type from chunk data
            // This depends on the actual chunk data format
            return chunkData.Blocks[z * 16 + x];
        }
        
        private Color GetMapColor(int blockType)
        {
            // Map block type to map color
            // Can be configured in profile
            return _currentProfile.GetBlockColor(blockType);
        }
        
        private void RequestMapUpdate(Vector2Int playerChunk)
        {
            // Send request to server for map data around player
            var request = new WorldMapDataRequest
            {
                CenterChunkX = playerChunk.x,
                CenterChunkZ = playerChunk.z,
                RenderDistance = _config.renderDistance,
                ProfileHash = _currentProfile.GetHash()
            };
            
            MinecraftNetworkClient.Instance.SendWorldMapRequest(request);
        }
        
        public void ToggleMapVisibility()
        {
            _isMapVisible = !_isMapVisible;
        }
        
        public void SetProfile(WorldMapControlProfile profile)
        {
            _currentProfile = profile;
            RefreshMap();
        }
        
        private void RefreshMap()
        {
            // Clear and regenerate map
            Array.Clear(_mapPixels, 0, _mapPixels.Length);
            _mapTexture.Apply();
            
            // Request fresh data from server
            RequestMapUpdate(_lastPlayerChunk);
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void UnsubscribeFromEvents()
        {
            if (MinecraftNetworkClient.Instance != null)
            {
                MinecraftNetworkClient.Instance.OnChunkDataReceived -= OnChunkDataReceived;
                MinecraftNetworkClient.Instance.OnPlayerPositionChanged -= OnPlayerPositionChanged;
            }
        }
        
        public Texture2D GetMapTexture()
        {
            return _mapTexture;
        }
    }
}
```

#### 1.2 WorldMapControlUI Class

**Current Limitation**: No UI exists for world map display and interaction.

**Improvement**: Implement [`WorldMapControlUI`](Assets/Scripts/Minecraft/UI/WorldMapControlUI.cs) for map display.

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    /// <summary>
    /// UI for world map control display and interaction.
    /// </summary>
    public class WorldMapControlUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _mapDisplay;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Slider _zoomSlider;
        [SerializeField] private Toggle _showBiomesToggle;
        [SerializeField] private Toggle _showHeightToggle;
        
        private WorldMapControlClient _mapClient;
        private float _currentZoom = 1.0f;
        private bool _showBiomes = true;
        private bool _showHeight = false;
        
        private void Start()
        {
            _mapClient = FindObjectOfType<WorldMapControlClient>();
            InitializeUI();
            SubscribeToEvents();
        }
        
        private void InitializeUI()
        {
            // Set up button listeners
            _toggleButton.onClick.AddListener(OnToggleClicked);
            _refreshButton.onClick.AddListener(OnRefreshClicked);
            _settingsButton.onClick.AddListener(OnSettingsClicked);
            
            // Set up slider listener
            _zoomSlider.onValueChanged.AddListener(OnZoomChanged);
            
            // Set up toggle listeners
            _showBiomesToggle.onValueChanged.AddListener(OnShowBiomesChanged);
            _showHeightToggle.onValueChanged.AddListener(OnShowHeightChanged);
            
            // Set initial values
            _zoomSlider.value = _currentZoom;
            _showBiomesToggle.isOn = _showBiomes;
            _showHeightToggle.isOn = _showHeight;
            
            // Update map display
            UpdateMapDisplay();
        }
        
        private void SubscribeToEvents()
        {
            if (_mapClient != null)
            {
                // Subscribe to map updates
                _mapClient.OnMapUpdated += OnMapUpdated;
            }
        }
        
        private void OnToggleClicked()
        {
            _mapClient.ToggleMapVisibility();
            UpdateMapDisplay();
        }
        
        private void OnRefreshClicked()
        {
            _mapClient.RefreshMap();
        }
        
        private void OnSettingsClicked()
        {
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }
        
        private void OnZoomChanged(float value)
        {
            _currentZoom = value;
            UpdateMapDisplay();
        }
        
        private void OnShowBiomesChanged(bool value)
        {
            _showBiomes = value;
            UpdateMapDisplay();
        }
        
        private void OnShowHeightChanged(bool value)
        {
            _showHeight = value;
            UpdateMapDisplay();
        }
        
        private void OnMapUpdated(Texture2D mapTexture)
        {
            // Update map display texture
            _mapDisplay.texture = mapTexture;
            UpdateMapDisplay();
        }
        
        private void UpdateMapDisplay()
        {
            // Update map visibility
            _mapDisplay.gameObject.SetActive(_mapClient.IsMapVisible);
            
            // Update zoom
            _mapDisplay.rectTransform.localScale = new Vector3(_currentZoom, _currentZoom, 1.0f);
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_mapClient != null)
            {
                _mapClient.OnMapUpdated -= OnMapUpdated;
            }
        }
    }
}
```

### 2. Real-Time Map Updates

#### 2.1 Chunk-Based Updates

**Current Limitation**: Map updates are infrequent and not real-time.

**Improvement**: Implement real-time chunk-based map updates as chunks are loaded.

```csharp
// Enhanced method in WorldMapControlClient
private void OnChunkDataReceived(ChunkDataResponse chunkData)
{
    // Update map for each chunk as it's received
    UpdateMapForChunk(chunkData);
    
    // Trigger map updated event
    OnMapUpdated?.Invoke(_mapTexture);
}
```

#### 2.2 Player Position Tracking

**Current Limitation**: Player position tracking is basic.

**Improvement**: Implement smooth player position tracking with interpolation.

```csharp
// Enhanced method in WorldMapControlClient
private void OnPlayerPositionChanged(Vector3 position)
{
    Vector2Int playerChunk = new Vector2Int(
        Mathf.FloorToInt(position.x / 16f),
        Mathf.FloorToInt(position.z / 16f)
    );
    
    // Interpolate map center for smooth movement
    StartCoroutine(InterpolateMapCenter(_lastPlayerChunk, playerChunk));
    
    _lastPlayerChunk = playerChunk;
}

private IEnumerator InterpolateMapCenter(Vector2Int fromChunk, Vector2Int toChunk)
{
    float duration = 0.5f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        // Interpolate chunk position
        Vector2Int currentChunk = Vector2Int.Lerp(fromChunk, toChunk, t);
        
        // Update map center
        UpdateMapCenter(currentChunk);
        
        yield return null;
    }
    
    // Ensure final position
    UpdateMapCenter(toChunk);
}
```

### 3. Biome Information Display

#### 3.1 Biome Overlay

**Current Limitation**: No biome information is displayed on the map.

**Improvement**: Implement biome overlay with color coding and labels.

```csharp
// Enhanced method in WorldMapControlClient
private Color GetMapColor(int blockType, int biomeType = -1)
{
    // Get base block color
    Color baseColor = _currentProfile.GetBlockColor(blockType);
    
    // Apply biome overlay if enabled
    if (_showBiomes && biomeType >= 0)
    {
        Color biomeColor = _currentProfile.GetBiomeColor(biomeType);
        return Color.Lerp(baseColor, biomeColor, _config.biomeOverlayOpacity);
    }
    
    return baseColor;
}
```

#### 3.2 Biome Labels

**Current Limitation**: No biome labels are displayed on the map.

**Improvement**: Implement biome labels with hover information.

```csharp
// New method in WorldMapControlUI
public void OnMapPointerEnter(PointerEventData eventData)
{
    // Get pointer position on map
    Vector2 localPoint;
    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
        eventData.position, 
        _mapDisplay.rectTransform, 
        eventData.pressEventCamera, 
        out localPoint))
    {
        return;
    }
    
    // Convert to chunk coordinates
    Vector2Int mapPos = GetMapPositionFromLocalPoint(localPoint);
    Vector2Int chunkPos = GetChunkPositionFromMapPosition(mapPos);
    
    // Get biome at position
    int biomeType = GetBiomeAtChunk(chunkPos);
    
    // Show biome tooltip
    ShowBiomeTooltip(biomeType, eventData.position);
}

private void ShowBiomeTooltip(int biomeType, Vector2 screenPosition)
{
    string biomeName = _currentProfile.GetBiomeName(biomeType);
    
    // Show tooltip at screen position
    _tooltipText.text = biomeName;
    _tooltipPanel.transform.position = screenPosition;
    _tooltipPanel.SetActive(true);
}
```

### 4. Enhanced Map Features

#### 4.1 Height Visualization

**Current Limitation**: No height information is visualized on the map.

**Improvement**: Implement height-based shading for terrain visualization.

```csharp
// Enhanced method in WorldMapControlClient
private Color GetMapColor(int blockType, int biomeType = -1, int height = 0)
{
    // Get base block color
    Color baseColor = _currentProfile.GetBlockColor(blockType);
    
    // Apply biome overlay if enabled
    if (_showBiomes && biomeType >= 0)
    {
        Color biomeColor = _currentProfile.GetBiomeColor(biomeType);
        baseColor = Color.Lerp(baseColor, biomeColor, _config.biomeOverlayOpacity);
    }
    
    // Apply height shading if enabled
    if (_showHeight)
    {
        float heightFactor = (float)height / WorldHeight;
        Color heightColor = Color.Lerp(Color.black, Color.white, heightFactor);
        baseColor = Color.Lerp(baseColor, heightColor, _config.heightShadeOpacity);
    }
    
    return baseColor;
}
```

#### 4.2 Mini-Map Display

**Current Limitation**: No mini-map overlay exists.

**Improvement**: Implement mini-map display in corner of screen.

```csharp
// New class: MiniMapDisplay.cs
using UnityEngine;
using UnityEngine.UI;

namespace Minecraft.UI
{
    /// <summary>
    /// Mini-map display overlay for world navigation.
    /// </summary>
    public class MiniMapDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage _miniMapDisplay;
        [SerializeField] private Image _playerIndicator;
        [SerializeField] private Image _directionIndicator;
        
        [Header("Configuration")]
        [SerializeField] private int _miniMapSize = 128;
        [SerializeField] private float _updateInterval = 0.5f;
        
        private WorldMapControlClient _mapClient;
        private float _lastUpdateTime;
        
        private void Start()
        {
            _mapClient = FindObjectOfType<WorldMapControlClient>();
            InitializeMiniMap();
        }
        
        private void InitializeMiniMap()
        {
            // Create mini-map texture
            Texture2D miniMapTexture = new Texture2D(_miniMapSize, _miniMapSize, TextureFormat.RGB24, false);
            miniMapTexture.filterMode = FilterMode.Point;
            _miniMapDisplay.texture = miniMapTexture;
        }
        
        private void Update()
        {
            // Update mini-map at interval
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateMiniMap();
                _lastUpdateTime = Time.time;
            }
            
            // Update player indicator
            UpdatePlayerIndicator();
        }
        
        private void UpdateMiniMap()
        {
            // Get current map texture
            Texture2D mapTexture = _mapClient.GetMapTexture();
            
            // Crop center region for mini-map
            int cropSize = _miniMapSize;
            int cropX = (mapTexture.width - cropSize) / 2;
            int cropY = (mapTexture.height - cropSize) / 2;
            
            Color[] pixels = mapTexture.GetPixels(cropX, cropY, cropSize, cropSize);
            
            // Apply to mini-map texture
            _miniMapDisplay.texture.SetPixels(pixels);
            _miniMapDisplay.texture.Apply();
        }
        
        private void UpdatePlayerIndicator()
        {
            // Center player indicator
            _playerIndicator.rectTransform.anchoredPosition = Vector2.zero;
            
            // Update direction indicator based on player rotation
            if (MinecraftPlayerController.Instance != null)
            {
                float playerRotation = MinecraftPlayerController.Instance.transform.rotation.eulerAngles.y;
                _directionIndicator.rectTransform.rotation = Quaternion.Euler(0, 0, -playerRotation);
            }
        }
    }
}
```

### 5. Configuration Improvements

#### 5.1 Enhanced Client Configuration

```json
{
  "worldMapControl": {
    "enabled": true,
    "renderDistance": 8,
    "updateInterval": 0.5,
    "zoomLevel": 1.0,
    "minZoom": 0.5,
    "maxZoom": 2.0,
    "showBiomes": true,
    "showHeight": false,
    "biomeOverlayOpacity": 0.3,
    "heightShadeOpacity": 0.2,
    "blockColors": {
      "0": "#7F7F7F",
      "1": "#8B8B8B",
      "2": "#A0522D",
      "3": "#7CFC00",
      "4": "#FFFFFF",
      "5": "#FFD700",
      "6": "#C0C0C0",
      "7": "#0000FF",
      "8": "#FFA500",
      "9": "#808080",
      "10": "#000000",
      "11": "#00FF00"
    },
    "biomeColors": {
      "plains": "#90C050",
      "forest": "#228B22",
      "desert": "#F4A460",
      "mountains": "#808080",
      "taiga": "#2E8B57",
      "swamp": "#4B3621",
      "river": "#0000FF",
      "ocean": "#000080",
      "beach": "#F4A460"
    },
    "miniMap": {
      "enabled": true,
      "size": 128,
      "position": "bottomRight",
      "opacity": 0.8
    },
    "profiles": [
      {
        "name": "Default",
        "renderDistance": 8,
        "showBiomes": true,
        "showHeight": false
      },
      {
        "name": "Detailed",
        "renderDistance": 12,
        "showBiomes": true,
        "showHeight": true
      },
      {
        "name": "Performance",
        "renderDistance": 6,
        "showBiomes": false,
        "showHeight": false
      }
    ]
  }
}
```

---

## Implementation Plan

### Phase 1: Client-Side Integration (High Priority)
1. Implement WorldMapControlClient class
2. Implement WorldMapControlUI class
3. Add protobuf message types for map requests
4. Implement server-side map data handler
5. Test client-server map synchronization

### Phase 2: Real-Time Updates (High Priority)
1. Implement chunk-based map updates
2. Add player position tracking
3. Implement smooth map center interpolation
4. Optimize update frequency

### Phase 3: Enhanced Features (Medium Priority)
1. Implement biome overlay
2. Add biome labels with tooltips
3. Implement height visualization
4. Add mini-map display

### Phase 4: Configuration and Polish (Medium Priority)
1. Update client configuration file
2. Add profile management
3. Implement settings persistence
4. Add UI polish and animations

---

## Testing Strategy

### Unit Tests
- Test WorldMapControlClient initialization
- Test map texture updates
- Test profile loading and switching
- Test color mapping functions

### Integration Tests
- Test client-server map request/response
- Test real-time chunk updates
- Test player position tracking
- Test biome information display

### Performance Tests
- Measure map update frequency
- Profile memory usage
- Test with large render distances
- Optimize texture updates

---

## Notes

- Client-side integration should maintain compatibility with existing server implementation
- Real-time updates should be optimized to avoid performance issues
- UI should be responsive and intuitive
- Configuration changes should be hot-reloadable
- Mini-map should be optional and configurable

