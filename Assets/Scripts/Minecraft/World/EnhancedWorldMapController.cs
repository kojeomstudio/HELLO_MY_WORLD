using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Minecraft.Core;
using EnhancedMinecraftProtocol;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced world map control system with improved architecture
    /// Provides better synchronization between server and client
    /// </summary>
    public class EnhancedWorldMapController : MonoBehaviour
    {
        [Header("World Map Control")]
        [SerializeField] private Material mapMaterial;
        [SerializeField] private GameObject mapMarkerPrefab;
        [SerializeField] private Camera mapCamera;
        
        [Header("UI References")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private UnityEngine.UI.Text coordinatesText;
        [SerializeField] private UnityEngine.UI.Text biomeText;
        [SerializeField] private UnityEngine.UI.Toggle showPlayersToggle;
        [SerializeField] private UnityEngine.UI.Toggle showCavesToggle;
        [SerializeField] private UnityEngine.UI.Toggle showRiversToggle;
        [SerializeField] private UnityEngine.UI.Toggle showLakesToggle;
        
        // World data
        private WorldConfig _worldConfig;
        private WorldMapControlProfile _mapControlProfile;
        private readonly Dictionary<Vector2Int, ChunkData> _loadedChunks = new();
        private readonly Dictionary<string, PlayerMapMarker> _playerMarkers = new();
        private string _profileHash = string.Empty;
        private bool _showPlayers = true;
        private bool _showCaves = true;
        private bool _showRivers = true;
        private bool _showLakes = true;
        private string _profilePath = string.Empty;
        private string _worldConfigPath = string.Empty;
        private DateTime _profileWriteTime;
        private DateTime _worldConfigWriteTime;
        
        // Map rendering
        private RenderTexture _mapRenderTexture;
        private Texture2D _mapTexture;
        private Dictionary<Vector2Int, Color> _biomeColors = new();
        
        // Performance optimization
        private readonly Queue<Vector2Int> _chunksToUpdate = new();
        private float _lastMapUpdate = 0f;
        private const float MAP_UPDATE_INTERVAL = 0.5f;
        
        // Events
        public event Action<Vector2Int> ChunkDataUpdated;
        public event Action<string> PlayerMarkerAdded;
        public event Action<string> PlayerMarkerRemoved;
        
        private void Start()
        {
            InitializeConfiguration();
            InitializeBiomeColors();
            InitializeMapRendering();
            SetupEventHandlers();
            
            Debug.Log("EnhancedWorldMapController initialized");
        }
        
        private void InitializeConfiguration()
        {
            _worldConfig = WorldConfig.Instance;
            _worldConfigPath = Path.Combine(Application.streamingAssetsPath, "world-config.json");
            if (!File.Exists(_worldConfigPath))
            {
                _worldConfigPath = string.Empty;
            }
            else
            {
                _worldConfigWriteTime = File.GetLastWriteTimeUtc(_worldConfigPath);
            }

            _profilePath = ResolveProfilePath();
            _mapControlProfile = WorldMapControlProfile.LoadFromFile(_profilePath, _worldConfig);
            if (_mapControlProfile.Version < _worldConfig.MapControlProfileVersion)
            {
                Debug.LogWarning($"[WorldMap] Map-control profile v{_mapControlProfile.Version} is older than config v{_worldConfig.MapControlProfileVersion}. Regenerating from config.");
                _mapControlProfile = WorldMapControlProfile.FromConfig(_worldConfig);
                ResetMapCache();
            }

            if (File.Exists(_profilePath))
            {
                _profileWriteTime = File.GetLastWriteTimeUtc(_profilePath);
            }

            _profileHash = _mapControlProfile.ProfileHash;
            _showPlayers = true;
            _showCaves = _mapControlProfile.EnableCaves;
            _showRivers = _mapControlProfile.EnableRivers;
            _showLakes = _mapControlProfile.EnableLakes;
            ApplyToggleDefaults();
            ValidateProfileHash();
        }

        private string ResolveProfilePath()
        {
            string defaultPath = Path.Combine(Application.streamingAssetsPath, "world-map-control.json");
            string configuredPath = _worldConfig != null ? _worldConfig.MapControlProfilePath : string.Empty;

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(Application.streamingAssetsPath, configuredPath);
            }

            return defaultPath;
        }

        private void ApplyToggleDefaults()
        {
            if (showPlayersToggle != null)
            {
                showPlayersToggle.isOn = _showPlayers;
            }

            if (showCavesToggle != null)
            {
                showCavesToggle.isOn = _showCaves;
            }

            if (showRiversToggle != null)
            {
                showRiversToggle.isOn = _showRivers;
            }

            if (showLakesToggle != null)
            {
                showLakesToggle.isOn = _showLakes;
            }
        }

        private void ResetMapCache()
        {
            _loadedChunks.Clear();
            _chunksToUpdate.Clear();
            _lastMapUpdate = 0f;
        }
        
        private void InitializeBiomeColors()
        {
            _biomeColors = new Dictionary<Vector2Int, Color>
            {
                { new Vector2Int(0, 0), new Color(0.5f, 0.8f, 0.2f) }, // Plains - green
                { new Vector2Int(1, 0), new Color(0.6f, 0.5f, 0.3f) }, // Mountains - gray
                { new Vector2Int(2, 0), new Color(0.2f, 0.6f, 0.1f) }, // Forest - dark green
                { new Vector2Int(3, 0), new Color(0.9f, 0.8f, 0.4f) }, // Desert - yellow
                { new Vector2Int(4, 0), new Color(0.4f, 0.7f, 0.3f) }, // Hills - light green
                { new Vector2Int(5, 0), new Color(0.3f, 0.5f, 0.2f) }, // Swamp - brown
                { new Vector2Int(6, 0), new Color(0.7f, 0.8f, 0.9f) }, // Taiga - light blue
                { new Vector2Int(7, 0), new Color(0.2f, 0.3f, 0.5f) }, // Ocean - blue
                { new Vector2Int(8, 0), new Color(0.9f, 0.9f, 0.8f) }  // Beach - sand
            };
        }
        
        private void InitializeMapRendering()
        {
            // Create render texture for map
            int chunkSize = Mathf.Max(1, _mapControlProfile.ChunkSize);
            int mapSize = Mathf.Max(chunkSize * 2, _mapControlProfile.RenderDistance * 2 * chunkSize);

            if (_mapRenderTexture != null)
            {
                _mapRenderTexture.Release();
                DestroyImmediate(_mapRenderTexture);
            }

            if (_mapTexture != null)
            {
                DestroyImmediate(_mapTexture);
            }

            _mapRenderTexture = new RenderTexture(mapSize, mapSize, 24)
            {
                name = "WorldMapRenderTexture"
            };
            
            // Create texture for map display
            _mapTexture = new Texture2D(mapSize, mapSize, TextureFormat.RGB24, false)
            {
                name = "WorldMapTexture"
            };
            
            // Setup camera for map rendering
            if (mapCamera != null)
            {
                mapCamera.targetTexture = _mapRenderTexture;
                mapCamera.orthographic = true;
                mapCamera.orthographicSize = _mapControlProfile.RenderDistance * chunkSize;
                mapCamera.cullingMask = LayerMask.GetMask("MapLayer");
            }
        }
        
        private void SetupEventHandlers()
        {
            // Setup toggle listeners
            if (showPlayersToggle != null)
                showPlayersToggle.onValueChanged.AddListener(OnShowPlayersToggled);
            
            if (showCavesToggle != null)
                showCavesToggle.onValueChanged.AddListener(OnShowCavesToggled);
            
            if (showRiversToggle != null)
                showRiversToggle.onValueChanged.AddListener(OnShowRiversToggled);
            
            if (showLakesToggle != null)
                showLakesToggle.onValueChanged.AddListener(OnShowLakesToggled);
        }
        
        private void Update()
        {
            MaybeReloadProfile();
            // Update map at intervals
            if (Time.time - _lastMapUpdate > MAP_UPDATE_INTERVAL)
            {
                UpdateMap();
                _lastMapUpdate = Time.time;
            }
            
            // Process chunk updates
            while (_chunksToUpdate.Count > 0)
            {
                var chunkPos = _chunksToUpdate.Dequeue();
                UpdateChunkOnMap(chunkPos);
            }
        }
        
        /// <summary>
        /// Updates the world map with current chunk data
        /// </summary>
        public void UpdateMap()
        {
            if (_mapRenderTexture == null) return;
            
            // Set camera to center on player position
            var playerPos = transform.position;
            mapCamera.transform.position = new Vector3(playerPos.x, mapCamera.transform.position.y, playerPos.z);
            
            // Render map to texture
            RenderTexture.active = _mapRenderTexture;
            mapCamera.Render();
            
            // Copy to texture
            RenderTexture.active = null;
            
            // Update UI texture if needed
            UpdateMapUI();
        }
        
        /// <summary>
        /// Adds or updates chunk data for map rendering
        /// </summary>
        public void UpdateChunkData(Vector2Int chunkPos, ChunkData chunkData)
        {
            _loadedChunks[chunkPos] = chunkData;
            _chunksToUpdate.Enqueue(chunkPos);
            
            ChunkDataUpdated?.Invoke(chunkPos);
        }
        
        /// <summary>
        /// Adds a player marker to the map
        /// </summary>
        public void AddPlayerMarker(string playerId, Vector3 worldPosition, string playerName)
        {
            if (_playerMarkers.ContainsKey(playerId))
            {
                UpdatePlayerMarker(playerId, worldPosition);
                return;
            }

            var markerObject = CreateMapMarkerObject(worldPosition, playerName);
            if (markerObject != null)
            {
                markerObject.SetActive(_showPlayers);
            }

            var marker = new PlayerMapMarker
            {
                PlayerId = playerId,
                PlayerName = playerName,
                WorldPosition = worldPosition,
                MarkerObject = markerObject
            };
            
            _playerMarkers[playerId] = marker;
            PlayerMarkerAdded?.Invoke(playerId);
        }
        
        /// <summary>
        /// Updates an existing player marker position
        /// </summary>
        public void UpdatePlayerMarker(string playerId, Vector3 worldPosition)
        {
            if (!_playerMarkers.TryGetValue(playerId, out var marker)) return;
            
            marker.WorldPosition = worldPosition;
            
            if (marker.MarkerObject != null)
            {
                // Convert world position to map position
                var mapPos = WorldToMapPosition(worldPosition);
                marker.MarkerObject.transform.localPosition = mapPos;
                marker.MarkerObject.SetActive(_showPlayers);
            }
        }
        
        /// <summary>
        /// Removes a player marker from the map
        /// </summary>
        public void RemovePlayerMarker(string playerId)
        {
            if (!_playerMarkers.TryGetValue(playerId, out var marker)) return;
            
            if (marker.MarkerObject != null)
            {
                DestroyImmediate(marker.MarkerObject);
            }
            
            _playerMarkers.Remove(playerId);
            PlayerMarkerRemoved?.Invoke(playerId);
        }
        
        private void UpdateChunkOnMap(Vector2Int chunkPos)
        {
            if (!_loadedChunks.TryGetValue(chunkPos, out var chunkData)) return;
            
            // Convert chunk position to map position
            var mapPos = ChunkToMapPosition(chunkPos);
            
            // Update map texture at chunk position
            UpdateMapTexture(mapPos, chunkData);
        }
        
        private void UpdateMapTexture(Vector2Int mapPos, ChunkData chunkData)
        {
            bool overlayHydrology = _showRivers || _showLakes;
            bool overlayCaves = _showCaves;
            if (!overlayHydrology && !overlayCaves)
            {
                return;
            }

            // This would update the map texture with chunk data
            // Implementation depends on how the map is rendered
            // For now, we'll just mark it as needing update
            // In a full implementation, this would draw the chunk on the map texture
        }
        
        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            // Convert world coordinates to map coordinates
            float mapX = (worldPos.x - transform.position.x) + _mapRenderTexture.width / 2f;
            float mapY = (worldPos.z - transform.position.z) + _mapRenderTexture.height / 2f;
            
            return new Vector2(mapX, mapY);
        }
        
        private Vector2Int ChunkToMapPosition(Vector2Int chunkPos)
        {
            // Convert chunk coordinates to map coordinates
            int mapX = chunkPos.x * 16 + _mapRenderTexture.width / 2;
            int mapY = chunkPos.y * 16 + _mapRenderTexture.height / 2;
            
            return new Vector2Int(mapX, mapY);
        }
        
        private GameObject CreateMapMarkerObject(Vector3 worldPosition, string playerName)
        {
            if (mapMarkerPrefab == null) return null;
            
            var marker = Instantiate(mapMarkerPrefab, mapContainer);
            var mapPos = WorldToMapPosition(worldPosition);
            
            marker.transform.localPosition = mapPos;
            
            // Set player name if available
            var textComponent = marker.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = playerName;
            }
            
            return marker;
        }
        
        private void UpdateMapUI()
        {
            // Update coordinates text
            if (coordinatesText != null)
            {
                var playerPos = transform.position;
                coordinatesText.text = $"X: {Mathf.FloorToInt(playerPos.x)}, Z: {Mathf.FloorToInt(playerPos.z)}";
            }
            
            // Update biome text
            if (biomeText != null)
            {
                var chunkPos = new Vector2Int(
                    Mathf.FloorToInt(transform.position.x / 16f),
                    Mathf.FloorToInt(transform.position.z / 16f)
                );
                
                if (_loadedChunks.TryGetValue(chunkPos, out var chunkData))
                {
                    biomeText.text = $"Biome: {GetBiomeName(chunkData.BiomeType)}";
                }
                else
                {
                    biomeText.text = "Biome: Unknown";
                }
            }
        }
        
        private string GetBiomeName(int biomeType)
        {
            return biomeType switch
            {
                0 => "Plains",
                1 => "Mountains",
                2 => "Forest",
                3 => "Desert",
                4 => "Hills",
                5 => "Swamp",
                6 => "Taiga",
                7 => "Ocean",
                8 => "Beach",
                _ => "Unknown"
            };
        }
        
        private WorldMapControlProfile CreateDefaultProfile()
        {
            return WorldMapControlProfile.FromConfig(_worldConfig);
        }

        public void ApplyServerProfile(WorldMapControlProfile profile, string serverHash = "")
        {
            if (profile == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(serverHash) &&
                !string.Equals(profile.ProfileHash, serverHash, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[WorldMap] Server profile hash {serverHash} differs from local {_profileHash}. Rebinding to server profile.");
            }

            _mapControlProfile = profile;
            _profileHash = profile.ProfileHash;
            _showCaves = profile.EnableCaves;
            _showRivers = profile.EnableRivers;
            _showLakes = profile.EnableLakes;
            ApplyToggleDefaults();
            ResetMapCache();
            InitializeMapRendering();
            UpdateMap();
        }

        private void ValidateProfileHash()
        {
            if (string.IsNullOrWhiteSpace(_profileHash))
            {
                Debug.LogWarning("[WorldMap] Map-control profile hash missing; verify server exported config/world_map_control_profile.json.");
            }

            if (_worldConfig != null)
            {
                var expectedProfile = WorldMapControlProfile.FromConfig(_worldConfig);
                if (!string.Equals(expectedProfile.ProfileHash, _profileHash, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[WorldMap] Map-control profile hash drift detected. Current={_profileHash} Expected={expectedProfile.ProfileHash}. Regenerate the profile from the server config to keep hydrology overlays aligned.");
                }
            }
        }

        private void MaybeReloadProfile()
        {
            try
            {
                bool configReloaded = false;
                if (!string.IsNullOrEmpty(_worldConfigPath))
                {
                    var write = File.GetLastWriteTimeUtc(_worldConfigPath);
                    if (write > _worldConfigWriteTime)
                    {
                        _worldConfigWriteTime = write;
                        WorldConfig.ForceReload();
                        _worldConfig = WorldConfig.Instance;
                        configReloaded = true;
                    }
                }

                if (!string.IsNullOrEmpty(_profilePath) && File.Exists(_profilePath))
                {
                    var profileWrite = File.GetLastWriteTimeUtc(_profilePath);
                    if (profileWrite > _profileWriteTime || configReloaded)
                    {
                        var profile = WorldMapControlProfile.LoadFromFile(_profilePath, _worldConfig);
                        if (profile.Version < _worldConfig.MapControlProfileVersion)
                        {
                            profile = WorldMapControlProfile.FromConfig(_worldConfig);
                        }

                        if (!string.Equals(profile.ProfileHash, _profileHash, StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyServerProfile(profile);
                        }

                        _profileWriteTime = profileWrite;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WorldMap] Failed to reload map-control profile: {ex.Message}");
            }
        }
        
        // Toggle event handlers
        private void OnShowPlayersToggled(bool isOn)
        {
            _showPlayers = isOn;
            SetPlayerMarkersVisible(isOn);
        }
        
        private void OnShowCavesToggled(bool isOn)
        {
            // Update map to show/hide caves
            if (!_mapControlProfile.EnableCaves)
            {
                _showCaves = false;
                if (showCavesToggle != null) showCavesToggle.isOn = false;
                return;
            }

            _showCaves = isOn;
            UpdateMap();
        }
        
        private void OnShowRiversToggled(bool isOn)
        {
            // Update map to show/hide rivers
            if (!_mapControlProfile.EnableRivers)
            {
                _showRivers = false;
                if (showRiversToggle != null) showRiversToggle.isOn = false;
                return;
            }

            _showRivers = isOn;
            UpdateMap();
        }
        
        private void OnShowLakesToggled(bool isOn)
        {
            // Update map to show/hide lakes
            if (!_mapControlProfile.EnableLakes)
            {
                _showLakes = false;
                if (showLakesToggle != null) showLakesToggle.isOn = false;
                return;
            }

            _showLakes = isOn;
            UpdateMap();
        }
        
        private void SetPlayerMarkersVisible(bool isVisible)
        {
            foreach (var marker in _playerMarkers.Values)
            {
                if (marker.MarkerObject != null)
                {
                    marker.MarkerObject.SetActive(isVisible);
                }
            }
        }
        
        private void OnDestroy()
        {
            // Clean up resources
            if (_mapRenderTexture != null)
            {
                _mapRenderTexture.Release();
                DestroyImmediate(_mapRenderTexture);
            }
            
            if (_mapTexture != null)
            {
                DestroyImmediate(_mapTexture);
            }
            
            // Clean up player markers
            foreach (var marker in _playerMarkers.Values)
            {
                if (marker.MarkerObject != null)
                {
                    DestroyImmediate(marker.MarkerObject);
                }
            }
            
            _playerMarkers.Clear();
            _loadedChunks.Clear();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw loaded chunks
            Gizmos.color = Color.green;
            foreach (var chunkPos in _loadedChunks.Keys)
            {
                var worldPos = new Vector3(chunkPos.x * 16, 0, chunkPos.y * 16);
                Gizmos.DrawWireCube(worldPos + new Vector3(8, 128, 8), new Vector3(16, 256, 16));
            }
            
            // Draw player positions
            Gizmos.color = Color.red;
            foreach (var marker in _playerMarkers.Values)
            {
                Gizmos.DrawSphere(marker.WorldPosition, 2f);
            }
        }
    }
    /// <summary>
    /// Player marker data for map display
    /// </summary>
    public class PlayerMapMarker
    {
        public string PlayerId;
        public string PlayerName;
        public Vector3 WorldPosition;
        public GameObject MarkerObject;
    }
}
