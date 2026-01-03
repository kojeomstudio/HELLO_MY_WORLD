using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced world map control system that manages world generation parameters,
    /// terrain features, and client-server synchronization
    /// </summary>
    public class WorldMapControlSystem : MonoBehaviour
    {
        [Header("World Map Control Configuration")]
        [SerializeField] private string configFileName = "world-map-control.json";
        [SerializeField] private bool loadConfigOnStart = true;
        [SerializeField] private bool autoSaveConfig = true;
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showControlPanel = false;
        
        // World map control profile
        private WorldMapControlProfile controlProfile;
        
        // Configuration file path
        private string configFilePath;
        
        // Events
        public event Action<WorldMapControlProfile> OnConfigurationLoaded;
        public event Action<WorldMapControlProfile> OnConfigurationChanged;
        
        // Singleton instance
        private static WorldMapControlSystem _instance;
        public static WorldMapControlSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldMapControlSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("WorldMapControlSystem");
                        _instance = go.AddComponent<WorldMapControlSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeSystem();
        }
        
        private void InitializeSystem()
        {
            // Set config file path
            configFilePath = Path.Combine(Application.streamingAssetsPath, configFileName);
            
            // Initialize default profile
            controlProfile = CreateDefaultProfile();
            
            // Load configuration if enabled
            if (loadConfigOnStart)
            {
                LoadConfiguration();
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapControlSystem] Initialized with config: {configFilePath}");
            }
        }
        
        private void Start()
        {
            // Notify listeners of initial configuration
            OnConfigurationLoaded?.Invoke(controlProfile);
        }
        
        private WorldMapControlProfile CreateDefaultProfile()
        {
            return new WorldMapControlProfile
            {
                Version = 1,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 12,
                GlobalWaterLevel = 62,
                
                // Hydrology settings
                HydrologyGradientStabilityIterations = 1,
                HydrologyGradientStabilityBlend = 0.45,
                HydrologyCurvatureWeight = 0.32,
                HydrologyEdgeBlendRadius = 3,
                HydrologyVarianceBlend = 0.55,
                HydrologyVarianceClamp = 0.65,
                HydrologySeamRelaxIterations = 2,
                HydrologySeamRelaxBlend = 0.5,
                HydrologyEdgeFluxBlend = 0.55,
                HydrologyEdgeVarianceClamp = 0.32,
                HydrologySmoothBlend = 0.6,
                HydrologySmoothIterations = 2,
                HydrologyShorePush = 5,
                HydrologySlopePenalty = 6,
                HydrologyFlowGain = 0.5,
                HydrologyContinuityWeight = 0.35,
                HydrologyEdgeFlowBias = 0.35,
                HydrologyEdgeTangentWeight = 0.45,
                HydrologyEdgeFlowLockWeight = 0.38,
                HydrologyEdgeStabilityIterations = 1,
                HydrologyEdgeStabilityWeight = 0.32,
                HydrologyWaterTableClampWeight = 0.42,
                HydrologyWaterTableClampRange = 18,
                HydrologyWaterTableSlopeWeight = 0.55,
                HydrologyFlowPersistence = 0.68,
                HydrologyGradientWeight = 0.35,
                HydrologyGradientSlopeWeight = 0.42,
                HydrologyGradientClamp = 1.65,
                HydrologyDirectionalIterations = 1,
                HydrologyDirectionalBlend = 0.42,
                HydrologyFlowDivergenceClamp = 0.55,
                HydrologyWarpFrequency = 0.0009,
                HydrologyWarpAmplitude = 9,
                
                // Riparian settings
                RiparianSmoothIterations = 2,
                RiparianSmoothBlend = 0.6,
                RiparianSaturationBoost = 0.18,
                RiparianBufferRadius = 1,
                
                // River settings
                RiverCenterThreshold = 0.0125,
                RiverBankThreshold = 0.028,
                RiverDepth = 6,
                RiverNoiseScale = 0.015,
                RiverIntensitySmoothIterations = 3,
                RiverIntensitySmoothBlend = 0.58,
                RiverConfluenceBoost = 0.35,
                RiverFlowAlignmentWeight = 0.28,
                RiverGradientPenalty = 0.42,
                RiverHeadwaterStabilityWeight = 0.35,
                RiverAnisotropyWeight = 0.32,
                RiverReliefPenaltyWeight = 0.25,
                RiverEdgeFeather = 0.45,
                RiverMouthSmoothRadius = 3,
                RiverDeltaWetlandStrength = 0.45,
                RiverSeamFillStrength = 0.5,
                RiverBankErosionWeight = 0.18,
                
                // Lake settings
                LakeSpawnWeightBias = 0.3,
                LakeShorelineBlend = 0.66,
                LakeWetlandSaturationThreshold = 0.55,
                LakeOutflowCarveDepth = 2,
                LakeBasinSmoothIterations = 2,
                LakeShelfDepth = 2,
                LakeMaxRadius = 9,
                LakeWetlandBufferRadius = 2,
                LakeRiverProximitySuppression = 0.35,
                LakeInflowBlendWeight = 0.42,
                LakeRimErosionWeight = 0.3,
                
                // Cave settings
                CaveEdgeSealStrength = 0.45,
                SupportPillarChance = 0.28,
                CaveStabilitySmoothIterations = 1,
                CaveStabilitySmoothBlend = 0.55,
                CaveSupportDensity = 0.6,
                CaveSupportHydrationBias = 0.42,
                CaveSupportFlowBias = 0.2,
                CaveMoistureRetentionWeight = 0.35,
                CaveRiparianPlugDepth = 2,
                CaveCeilingStabilityWeight = 0.35,
                CaveHydrologyWeight = 0.45,
                CaveFlowWeight = 0.25,
                CaveRoughnessWeight = 0.1,
                CaveDepthWeight = 0.2,
                CaveRiverSuppressionWeight = 0.35,
                
                // Feature toggles
                EnableRivers = true,
                EnableLakes = true,
                EnableCaves = true,
                UseImprovedCaves = true,
                UseImprovedRivers = true,
                UseImprovedLakes = true,
                
                // Client-specific settings
                MaxConcurrentChunkGenerations = 4,
                UpdateBatchSize = 24,
                UpdateIntervalMs = 100,
                DefaultRenderDistance = 10,
                DefaultMapScale = 1,
                DefaultShowCoordinates = true,
                DefaultShowBiomeInfo = true,
                DefaultTerrainQuality = 2,
                DefaultWaterQuality = 2,
                DefaultVegetationQuality = 2,
                DefaultFogEnabled = true,
                DefaultShadowEnabled = true,
                DefaultMaxChunkUpdatesPerFrame = 12,
                DefaultChunkLOD = 2,
                DefaultUnloadDistance = 12,
                
                // Performance settings
                TargetFrameRate = 60,
                VSyncEnabled = true,
                MaxChunkLoadTimeMs = 50,
                ChunkUnloadDelaySeconds = 30,
                
                // Network settings
                NetworkCompressionEnabled = true,
                NetworkCompressionLevel = 6,
                ChunkRequestTimeoutMs = 5000,
                MaxConcurrentChunkRequests = 8,
                
                // Metadata
                GeneratedAtUtc = DateTime.UtcNow.ToString("o"),
                ProfileHash = ComputeProfileHash()
            };
        }
        
        /// <summary>
        /// Load configuration from file
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    var loadedProfile = JsonConvert.DeserializeObject<WorldMapControlProfile>(json);
                    
                    if (loadedProfile != null)
                    {
                        controlProfile = loadedProfile;
                        controlProfile.ProfileHash = ComputeProfileHash();
                        
                        if (enableDebugLogging)
                        {
                            Debug.Log($"[WorldMapControlSystem] Configuration loaded from {configFilePath}");
                        }
                        
                        OnConfigurationLoaded?.Invoke(controlProfile);
                    }
                }
                else
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"[WorldMapControlSystem] Config file not found: {configFilePath}, using defaults");
                    }
                    
                    // Save default configuration
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapControlSystem] Failed to load configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void SaveConfiguration()
        {
            if (!autoSaveConfig) return;
            
            try
            {
                // Update metadata
                controlProfile.GeneratedAtUtc = DateTime.UtcNow.ToString("o");
                controlProfile.ProfileHash = ComputeProfileHash();
                
                // Serialize to JSON
                string json = JsonConvert.SerializeObject(controlProfile, Formatting.Indented);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write to file
                File.WriteAllText(configFilePath, json);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapControlSystem] Configuration saved to {configFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapControlSystem] Failed to save configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update configuration and notify listeners
        /// </summary>
        public void UpdateConfiguration(WorldMapControlProfile newProfile)
        {
            if (newProfile == null) return;
            
            controlProfile = newProfile;
            controlProfile.ProfileHash = ComputeProfileHash();
            
            OnConfigurationChanged?.Invoke(controlProfile);
            
            if (autoSaveConfig)
            {
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// Get current configuration profile
        /// </summary>
        public WorldMapControlProfile GetConfiguration()
        {
            return controlProfile;
        }
        
        /// <summary>
        /// Reset to default configuration
        /// </summary>
        public void ResetToDefaults()
        {
            controlProfile = CreateDefaultProfile();
            OnConfigurationChanged?.Invoke(controlProfile);
            
            if (autoSaveConfig)
            {
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// Apply configuration to terrain generator
        /// </summary>
        public void ApplyToTerrainGenerator(EnhancedTerrainGenerator generator)
        {
            if (generator == null) return;
            
            // Apply terrain generation parameters
            generator.SetTerrainParameters(new TerrainParameters
            {
                ChunkSize = controlProfile.ChunkSize,
                WorldHeight = 256,
                SeaLevel = controlProfile.GlobalWaterLevel,
                EnableCaves = controlProfile.EnableCaves,
                EnableRivers = controlProfile.EnableRivers,
                EnableLakes = controlProfile.EnableLakes,
                UseImprovedCaves = controlProfile.UseImprovedCaves,
                UseImprovedRivers = controlProfile.UseImprovedRivers,
                UseImprovedLakes = controlProfile.UseImprovedLakes
            });
        }
        
        /// <summary>
        /// Apply configuration to client world controller
        /// </summary>
        public void ApplyToClientController(EnhancedClientWorldController controller)
        {
            if (controller == null) return;
            
            // Apply client-specific settings
            controller.SetViewDistance(controlProfile.DefaultRenderDistance);
            controller.SetChunkUpdateInterval(controlProfile.UpdateIntervalMs / 1000f);
            controller.SetMaxChunksPerFrame(controlProfile.DefaultMaxChunkUpdatesPerFrame);
        }
        
        /// <summary>
        /// Get client-specific configuration
        /// </summary>
        public ClientWorldConfig GetClientConfig()
        {
            return new ClientWorldConfig
            {
                ViewDistance = controlProfile.DefaultRenderDistance,
                ChunkSize = controlProfile.ChunkSize,
                WorldHeight = 256,
                SeaLevel = controlProfile.GlobalWaterLevel,
                MaxConcurrentChunkGenerations = controlProfile.MaxConcurrentChunkGenerations,
                UpdateBatchSize = controlProfile.UpdateBatchSize,
                UpdateIntervalMs = controlProfile.UpdateIntervalMs,
                MapScale = controlProfile.DefaultMapScale,
                ShowCoordinates = controlProfile.DefaultShowCoordinates,
                ShowBiomeInfo = controlProfile.DefaultShowBiomeInfo,
                TerrainQuality = controlProfile.DefaultTerrainQuality,
                WaterQuality = controlProfile.DefaultWaterQuality,
                VegetationQuality = controlProfile.DefaultVegetationQuality,
                FogEnabled = controlProfile.DefaultFogEnabled,
                ShadowEnabled = controlProfile.DefaultShadowEnabled,
                MaxChunkUpdatesPerFrame = controlProfile.DefaultMaxChunkUpdatesPerFrame,
                ChunkLOD = controlProfile.DefaultChunkLOD,
                UnloadDistance = controlProfile.DefaultUnloadDistance,
                TargetFrameRate = controlProfile.TargetFrameRate,
                VSyncEnabled = controlProfile.VSyncEnabled,
                MaxChunkLoadTimeMs = controlProfile.MaxChunkLoadTimeMs,
                ChunkUnloadDelaySeconds = controlProfile.ChunkUnloadDelaySeconds
            };
        }
        
        /// <summary>
        /// Get server-specific configuration
        /// </summary>
        public ServerWorldConfig GetServerConfig()
        {
            return new ServerWorldConfig
            {
                ChunkSize = controlProfile.ChunkSize,
                WorldHeight = 256,
                SeaLevel = controlProfile.GlobalWaterLevel,
                RenderDistance = controlProfile.RenderDistance,
                SimulationDistance = controlProfile.SimulationDistance,
                NetworkCompressionEnabled = controlProfile.NetworkCompressionEnabled,
                NetworkCompressionLevel = controlProfile.NetworkCompressionLevel,
                ChunkRequestTimeoutMs = controlProfile.ChunkRequestTimeoutMs,
                MaxConcurrentChunkRequests = controlProfile.MaxConcurrentChunkRequests,
                
                // Terrain generation settings
                EnableCaves = controlProfile.EnableCaves,
                EnableRivers = controlProfile.EnableRivers,
                EnableLakes = controlProfile.EnableLakes,
                UseImprovedCaves = controlProfile.UseImprovedCaves,
                UseImprovedRivers = controlProfile.UseImprovedRivers,
                UseImprovedLakes = controlProfile.UseImprovedLakes,
                
                // Hydrology settings
                HydrologyGradientStabilityIterations = controlProfile.HydrologyGradientStabilityIterations,
                HydrologyGradientStabilityBlend = controlProfile.HydrologyGradientStabilityBlend,
                HydrologyCurvatureWeight = controlProfile.HydrologyCurvatureWeight,
                HydrologyEdgeBlendRadius = controlProfile.HydrologyEdgeBlendRadius,
                HydrologyVarianceBlend = controlProfile.HydrologyVarianceBlend,
                HydrologyVarianceClamp = controlProfile.HydrologyVarianceClamp,
                HydrologySeamRelaxIterations = controlProfile.HydrologySeamRelaxIterations,
                HydrologySeamRelaxBlend = controlProfile.HydrologySeamRelaxBlend,
                HydrologyEdgeFluxBlend = controlProfile.HydrologyEdgeFluxBlend,
                HydrologyEdgeVarianceClamp = controlProfile.HydrologyEdgeVarianceClamp,
                HydrologySmoothBlend = controlProfile.HydrologySmoothBlend,
                HydrologySmoothIterations = controlProfile.HydrologySmoothIterations,
                HydrologyShorePush = controlProfile.HydrologyShorePush,
                HydrologySlopePenalty = controlProfile.HydrologySlopePenalty,
                HydrologyFlowGain = controlProfile.HydrologyFlowGain,
                HydrologyContinuityWeight = controlProfile.HydrologyContinuityWeight,
                HydrologyEdgeFlowBias = controlProfile.HydrologyEdgeFlowBias,
                HydrologyEdgeTangentWeight = controlProfile.HydrologyEdgeTangentWeight,
                HydrologyEdgeFlowLockWeight = controlProfile.HydrologyEdgeFlowLockWeight,
                HydrologyEdgeStabilityIterations = controlProfile.HydrologyEdgeStabilityIterations,
                HydrologyEdgeStabilityWeight = controlProfile.HydrologyEdgeStabilityWeight,
                HydrologyWaterTableClampWeight = controlProfile.HydrologyWaterTableClampWeight,
                HydrologyWaterTableClampRange = controlProfile.HydrologyWaterTableClampRange,
                HydrologyWaterTableSlopeWeight = controlProfile.HydrologyWaterTableSlopeWeight,
                HydrologyFlowPersistence = controlProfile.HydrologyFlowPersistence,
                HydrologyGradientWeight = controlProfile.HydrologyGradientWeight,
                HydrologyGradientSlopeWeight = controlProfile.HydrologyGradientSlopeWeight,
                HydrologyGradientClamp = controlProfile.HydrologyGradientClamp,
                HydrologyDirectionalIterations = controlProfile.HydrologyDirectionalIterations,
                HydrologyDirectionalBlend = controlProfile.HydrologyDirectionalBlend,
                HydrologyFlowDivergenceClamp = controlProfile.HydrologyFlowDivergenceClamp,
                HydrologyWarpFrequency = controlProfile.HydrologyWarpFrequency,
                HydrologyWarpAmplitude = controlProfile.HydrologyWarpAmplitude,
                
                // Riparian settings
                RiparianSmoothIterations = controlProfile.RiparianSmoothIterations,
                RiparianSmoothBlend = controlProfile.RiparianSmoothBlend,
                RiparianSaturationBoost = controlProfile.RiparianSaturationBoost,
                RiparianBufferRadius = controlProfile.RiparianBufferRadius,
                
                // River settings
                RiverCenterThreshold = controlProfile.RiverCenterThreshold,
                RiverBankThreshold = controlProfile.RiverBankThreshold,
                RiverDepth = controlProfile.RiverDepth,
                RiverNoiseScale = controlProfile.RiverNoiseScale,
                RiverIntensitySmoothIterations = controlProfile.RiverIntensitySmoothIterations,
                RiverIntensitySmoothBlend = controlProfile.RiverIntensitySmoothBlend,
                RiverConfluenceBoost = controlProfile.RiverConfluenceBoost,
                RiverFlowAlignmentWeight = controlProfile.RiverFlowAlignmentWeight,
                RiverGradientPenalty = controlProfile.RiverGradientPenalty,
                RiverHeadwaterStabilityWeight = controlProfile.RiverHeadwaterStabilityWeight,
                RiverAnisotropyWeight = controlProfile.RiverAnisotropyWeight,
                RiverReliefPenaltyWeight = controlProfile.RiverReliefPenaltyWeight,
                RiverEdgeFeather = controlProfile.RiverEdgeFeather,
                RiverMouthSmoothRadius = controlProfile.RiverMouthSmoothRadius,
                RiverDeltaWetlandStrength = controlProfile.RiverDeltaWetlandStrength,
                RiverSeamFillStrength = controlProfile.RiverSeamFillStrength,
                RiverBankErosionWeight = controlProfile.RiverBankErosionWeight,
                
                // Lake settings
                LakeSpawnWeightBias = controlProfile.LakeSpawnWeightBias,
                LakeShorelineBlend = controlProfile.LakeShorelineBlend,
                LakeWetlandSaturationThreshold = controlProfile.LakeWetlandSaturationThreshold,
                LakeOutflowCarveDepth = controlProfile.LakeOutflowCarveDepth,
                LakeBasinSmoothIterations = controlProfile.LakeBasinSmoothIterations,
                LakeShelfDepth = controlProfile.LakeShelfDepth,
                LakeMaxRadius = controlProfile.LakeMaxRadius,
                LakeWetlandBufferRadius = controlProfile.LakeWetlandBufferRadius,
                LakeRiverProximitySuppression = controlProfile.LakeRiverProximitySuppression,
                LakeInflowBlendWeight = controlProfile.LakeInflowBlendWeight,
                LakeRimErosionWeight = controlProfile.LakeRimErosionWeight,
                
                // Cave settings
                CaveEdgeSealStrength = controlProfile.CaveEdgeSealStrength,
                SupportPillarChance = controlProfile.SupportPillarChance,
                CaveStabilitySmoothIterations = controlProfile.CaveStabilitySmoothIterations,
                CaveStabilitySmoothBlend = controlProfile.CaveStabilitySmoothBlend,
                CaveSupportDensity = controlProfile.CaveSupportDensity,
                CaveSupportHydrationBias = controlProfile.CaveSupportHydrationBias,
                CaveSupportFlowBias = controlProfile.CaveSupportFlowBias,
                CaveMoistureRetentionWeight = controlProfile.CaveMoistureRetentionWeight,
                CaveRiparianPlugDepth = controlProfile.CaveRiparianPlugDepth,
                CaveCeilingStabilityWeight = controlProfile.CaveCeilingStabilityWeight,
                CaveHydrologyWeight = controlProfile.CaveHydrologyWeight,
                CaveFlowWeight = controlProfile.CaveFlowWeight,
                CaveRoughnessWeight = controlProfile.CaveRoughnessWeight,
                CaveDepthWeight = controlProfile.CaveDepthWeight,
                CaveRiverSuppressionWeight = controlProfile.CaveRiverSuppressionWeight
            };
        }
        
        /// <summary>
        /// Compute hash for profile validation
        /// </summary>
        private string ComputeProfileHash()
        {
            // Simple hash implementation - in production, use proper cryptographic hash
            var hashSource = $"{controlProfile.ChunkSize}_{controlProfile.RenderDistance}_{controlProfile.GlobalWaterLevel}_{controlProfile.EnableRivers}_{controlProfile.EnableLakes}_{controlProfile.EnableCaves}";
            return hashSource.GetHashCode().ToString("X");
        }
        
        private void OnGUI()
        {
            if (!showControlPanel) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 600));
            GUILayout.BeginVertical("box");
            GUILayout.Label("World Map Control System", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Load Configuration"))
            {
                LoadConfiguration();
            }
            
            if (GUILayout.Button("Save Configuration"))
            {
                SaveConfiguration();
            }
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Profile Hash: {controlProfile.ProfileHash}");
            GUILayout.Label($"Generated: {controlProfile.GeneratedAtUtc}");
            
            GUILayout.Space(10);
            GUILayout.Label("Terrain Features:", EditorStyles.boldLabel);
            GUILayout.Label($"Rivers: {(controlProfile.EnableRivers ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Lakes: {(controlProfile.EnableLakes ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Caves: {(controlProfile.EnableCaves ? "Enabled" : "Disabled")}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnDestroy()
        {
            if (autoSaveConfig)
            {
                SaveConfiguration();
            }
        }
    }
    
    /// <summary>
    /// World map control profile data structure
    /// </summary>
    [Serializable]
    public class WorldMapControlProfile
    {
        public int Version { get; set; }
        public string ProfileHash { get; set; }
        public string GeneratedAtUtc { get; set; }
        
        // Basic world settings
        public int ChunkSize { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public int GlobalWaterLevel { get; set; }
        
        // Hydrology settings
        public int HydrologyGradientStabilityIterations { get; set; }
        public double HydrologyGradientStabilityBlend { get; set; }
        public double HydrologyCurvatureWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public double HydrologyVarianceBlend { get; set; }
        public double HydrologyVarianceClamp { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public double HydrologySeamRelaxBlend { get; set; }
        public double HydrologyEdgeFluxBlend { get; set; }
        public double HydrologyEdgeVarianceClamp { get; set; }
        public double HydrologySmoothBlend { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public double HydrologyShorePush { get; set; }
        public double HydrologySlopePenalty { get; set; }
        public double HydrologyFlowGain { get; set; }
        public double HydrologyContinuityWeight { get; set; }
        public double HydrologyEdgeFlowBias { get; set; }
        public double HydrologyEdgeTangentWeight { get; set; }
        public double HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public double HydrologyEdgeStabilityWeight { get; set; }
        public double HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public double HydrologyWaterTableSlopeWeight { get; set; }
        public double HydrologyFlowPersistence { get; set; }
        public double HydrologyGradientWeight { get; set; }
        public double HydrologyGradientSlopeWeight { get; set; }
        public double HydrologyGradientClamp { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public double HydrologyDirectionalBlend { get; set; }
        public double HydrologyFlowDivergenceClamp { get; set; }
        public double HydrologyWarpFrequency { get; set; }
        public double HydrologyWarpAmplitude { get; set; }
        
        // Riparian settings
        public int RiparianSmoothIterations { get; set; }
        public double RiparianSmoothBlend { get; set; }
        public double RiparianSaturationBoost { get; set; }
        public int RiparianBufferRadius { get; set; }
        
        // River settings
        public double RiverCenterThreshold { get; set; }
        public double RiverBankThreshold { get; set; }
        public int RiverDepth { get; set; }
        public double RiverNoiseScale { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public double RiverIntensitySmoothBlend { get; set; }
        public double RiverConfluenceBoost { get; set; }
        public double RiverFlowAlignmentWeight { get; set; }
        public double RiverGradientPenalty { get; set; }
        public double RiverHeadwaterStabilityWeight { get; set; }
        public double RiverAnisotropyWeight { get; set; }
        public double RiverReliefPenaltyWeight { get; set; }
        public double RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public double RiverDeltaWetlandStrength { get; set; }
        public double RiverSeamFillStrength { get; set; }
        public double RiverBankErosionWeight { get; set; }
        
        // Lake settings
        public double LakeSpawnWeightBias { get; set; }
        public double LakeShorelineBlend { get; set; }
        public double LakeWetlandSaturationThreshold { get; set; }
        public int LakeOutflowCarveDepth { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int LakeShelfDepth { get; set; }
        public int LakeMaxRadius { get; set; }
        public int LakeWetlandBufferRadius { get; set; }
        public double LakeRiverProximitySuppression { get; set; }
        public double LakeInflowBlendWeight { get; set; }
        public double LakeRimErosionWeight { get; set; }
        
        // Cave settings
        public double CaveEdgeSealStrength { get; set; }
        public double SupportPillarChance { get; set; }
        public int CaveStabilitySmoothIterations { get; set; }
        public double CaveStabilitySmoothBlend { get; set; }
        public double CaveSupportDensity { get; set; }
        public double CaveSupportHydrationBias { get; set; }
        public double CaveSupportFlowBias { get; set; }
        public double CaveMoistureRetentionWeight { get; set; }
        public int CaveRiparianPlugDepth { get; set; }
        public double CaveCeilingStabilityWeight { get; set; }
        public double CaveHydrologyWeight { get; set; }
        public double CaveFlowWeight { get; set; }
        public double CaveRoughnessWeight { get; set; }
        public double CaveDepthWeight { get; set; }
        public double CaveRiverSuppressionWeight { get; set; }
        
        // Feature toggles
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
        
        // Client-specific settings
        public int MaxConcurrentChunkGenerations { get; set; }
        public int UpdateBatchSize { get; set; }
        public int UpdateIntervalMs { get; set; }
        public int DefaultRenderDistance { get; set; }
        public int DefaultMapScale { get; set; }
        public bool DefaultShowCoordinates { get; set; }
        public bool DefaultShowBiomeInfo { get; set; }
        public int DefaultTerrainQuality { get; set; }
        public int DefaultWaterQuality { get; set; }
        public int DefaultVegetationQuality { get; set; }
        public bool DefaultFogEnabled { get; set; }
        public bool DefaultShadowEnabled { get; set; }
        public int DefaultMaxChunkUpdatesPerFrame { get; set; }
        public int DefaultChunkLOD { get; set; }
        public int DefaultUnloadDistance { get; set; }
        
        // Performance settings
        public int TargetFrameRate { get; set; }
        public bool VSyncEnabled { get; set; }
        public int MaxChunkLoadTimeMs { get; set; }
        public int ChunkUnloadDelaySeconds { get; set; }
        
        // Network settings
        public bool NetworkCompressionEnabled { get; set; }
        public int NetworkCompressionLevel { get; set; }
        public int ChunkRequestTimeoutMs { get; set; }
        public int MaxConcurrentChunkRequests { get; set; }
    }
    
    /// <summary>
    /// Client-specific world configuration
    /// </summary>
    [Serializable]
    public class ClientWorldConfig
    {
        public int ViewDistance { get; set; }
        public int ChunkSize { get; set; }
        public int WorldHeight { get; set; }
        public int SeaLevel { get; set; }
        public int MaxConcurrentChunkGenerations { get; set; }
        public int UpdateBatchSize { get; set; }
        public int UpdateIntervalMs { get; set; }
        public int MapScale { get; set; }
        public bool ShowCoordinates { get; set; }
        public bool ShowBiomeInfo { get; set; }
        public int TerrainQuality { get; set; }
        public int WaterQuality { get; set; }
        public int VegetationQuality { get; set; }
        public bool FogEnabled { get; set; }
        public bool ShadowEnabled { get; set; }
        public int MaxChunkUpdatesPerFrame { get; set; }
        public int ChunkLOD { get; set; }
        public int UnloadDistance { get; set; }
        public int TargetFrameRate { get; set; }
        public bool VSyncEnabled { get; set; }
        public int MaxChunkLoadTimeMs { get; set; }
        public int ChunkUnloadDelaySeconds { get; set; }
    }
    
    /// <summary>
    /// Server-specific world configuration
    /// </summary>
    [Serializable]
    public class ServerWorldConfig
    {
        public int ChunkSize { get; set; }
        public int WorldHeight { get; set; }
        public int SeaLevel { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public bool NetworkCompressionEnabled { get; set; }
        public int NetworkCompressionLevel { get; set; }
        public int ChunkRequestTimeoutMs { get; set; }
        public int MaxConcurrentChunkRequests { get; set; }
        
        // Terrain generation settings
        public bool EnableCaves { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
        
        // Hydrology settings
        public int HydrologyGradientStabilityIterations { get; set; }
        public double HydrologyGradientStabilityBlend { get; set; }
        public double HydrologyCurvatureWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public double HydrologyVarianceBlend { get; set; }
        public double HydrologyVarianceClamp { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public double HydrologySeamRelaxBlend { get; set; }
        public double HydrologyEdgeFluxBlend { get; set; }
        public double HydrologyEdgeVarianceClamp { get; set; }
        public double HydrologySmoothBlend { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public double HydrologyShorePush { get; set; }
        public double HydrologySlopePenalty { get; set; }
        public double HydrologyFlowGain { get; set; }
        public double HydrologyContinuityWeight { get; set; }
        public double HydrologyEdgeFlowBias { get; set; }
        public double HydrologyEdgeTangentWeight { get; set; }
        public double HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public double HydrologyEdgeStabilityWeight { get; set; }
        public double HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public double HydrologyWaterTableSlopeWeight { get; set; }
        public double HydrologyFlowPersistence { get; set; }
        public double HydrologyGradientWeight { get; set; }
        public double HydrologyGradientSlopeWeight { get; set; }
        public double HydrologyGradientClamp { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public double HydrologyDirectionalBlend { get; set; }
        public double HydrologyFlowDivergenceClamp { get; set; }
        public double HydrologyWarpFrequency { get; set; }
        public double HydrologyWarpAmplitude { get; set; }
        
        // Riparian settings
        public int RiparianSmoothIterations { get; set; }
        public double RiparianSmoothBlend { get; set; }
        public double RiparianSaturationBoost { get; set; }
        public int RiparianBufferRadius { get; set; }
        
        // River settings
        public double RiverCenterThreshold { get; set; }
        public double RiverBankThreshold { get; set; }
        public int RiverDepth { get; set; }
        public double RiverNoiseScale { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public double RiverIntensitySmoothBlend { get; set; }
        public double RiverConfluenceBoost { get; set; }
        public double RiverFlowAlignmentWeight { get; set; }
        public double RiverGradientPenalty { get; set; }
        public double RiverHeadwaterStabilityWeight { get; set; }
        public double RiverAnisotropyWeight { get; set; }
        public double RiverReliefPenaltyWeight { get; set; }
        public double RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public double RiverDeltaWetlandStrength { get; set; }
        public double RiverSeamFillStrength { get; set; }
        public double RiverBankErosionWeight { get; set; }
        
        // Lake settings
        public double LakeSpawnWeightBias { get; set; }
        public double LakeShorelineBlend { get; set; }
        public double LakeWetlandSaturationThreshold { get; set; }
        public int LakeOutflowCarveDepth { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int LakeShelfDepth { get; set; }
        public int LakeMaxRadius { get; set; }
        public int LakeWetlandBufferRadius { get; set; }
        public double LakeRiverProximitySuppression { get; set; }
        public double LakeInflowBlendWeight { get; set; }
        public double LakeRimErosionWeight { get; set; }
        
        // Cave settings
        public double CaveEdgeSealStrength { get; set; }
        public double SupportPillarChance { get; set; }
        public int CaveStabilitySmoothIterations { get; set; }
        public double CaveStabilitySmoothBlend { get; set; }
        public double CaveSupportDensity { get; set; }
        public double CaveSupportHydrationBias { get; set; }
        public double CaveSupportFlowBias { get; set; }
        public double CaveMoistureRetentionWeight { get; set; }
        public int CaveRiparianPlugDepth { get; set; }
        public double CaveCeilingStabilityWeight { get; set; }
        public double CaveHydrologyWeight { get; set; }
        public double CaveFlowWeight { get; set; }
        public double CaveRoughnessWeight { get; set; }
        public double CaveDepthWeight { get; set; }
        public double CaveRiverSuppressionWeight { get; set; }
    }
    
    /// <summary>
    /// Terrain generation parameters
    /// </summary>
    [Serializable]
    public class TerrainParameters
    {
        public int ChunkSize { get; set; }
        public int WorldHeight { get; set; }
        public int SeaLevel { get; set; }
        public bool EnableCaves { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
    }
}using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Minecraft.World
{
    /// <summary>
    /// Enhanced world map control system that manages world generation parameters,
    /// terrain features, and client-server synchronization
    /// </summary>
    public class WorldMapControlSystem : MonoBehaviour
    {
        [Header("World Map Control Configuration")]
        [SerializeField] private string configFileName = "world-map-control.json";
        [SerializeField] private bool loadConfigOnStart = true;
        [SerializeField] private bool autoSaveConfig = true;
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showControlPanel = false;
        
        // World map control profile
        private WorldMapControlProfile controlProfile;
        
        // Configuration file path
        private string configFilePath;
        
        // Events
        public event Action<WorldMapControlProfile> OnConfigurationLoaded;
        public event Action<WorldMapControlProfile> OnConfigurationChanged;
        
        // Singleton instance
        private static WorldMapControlSystem _instance;
        public static WorldMapControlSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldMapControlSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("WorldMapControlSystem");
                        _instance = go.AddComponent<WorldMapControlSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeSystem();
        }
        
        private void InitializeSystem()
        {
            // Set config file path
            configFilePath = Path.Combine(Application.streamingAssetsPath, configFileName);
            
            // Initialize default profile
            controlProfile = CreateDefaultProfile();
            
            // Load configuration if enabled
            if (loadConfigOnStart)
            {
                LoadConfiguration();
            }
            
            if (enableDebugLogging)
            {
                Debug.Log($"[WorldMapControlSystem] Initialized with config: {configFilePath}");
            }
        }
        
        private void Start()
        {
            // Notify listeners of initial configuration
            OnConfigurationLoaded?.Invoke(controlProfile);
        }
        
        private WorldMapControlProfile CreateDefaultProfile()
        {
            return new WorldMapControlProfile
            {
                Version = 1,
                ChunkSize = 16,
                RenderDistance = 10,
                SimulationDistance = 12,
                GlobalWaterLevel = 62,
                
                // Hydrology settings
                HydrologyGradientStabilityIterations = 1,
                HydrologyGradientStabilityBlend = 0.45,
                HydrologyCurvatureWeight = 0.32,
                HydrologyEdgeBlendRadius = 3,
                HydrologyVarianceBlend = 0.55,
                HydrologyVarianceClamp = 0.65,
                HydrologySeamRelaxIterations = 2,
                HydrologySeamRelaxBlend = 0.5,
                HydrologyEdgeFluxBlend = 0.55,
                HydrologyEdgeVarianceClamp = 0.32,
                HydrologySmoothBlend = 0.6,
                HydrologySmoothIterations = 2,
                HydrologyShorePush = 5,
                HydrologySlopePenalty = 6,
                HydrologyFlowGain = 0.5,
                HydrologyContinuityWeight = 0.35,
                HydrologyEdgeFlowBias = 0.35,
                HydrologyEdgeTangentWeight = 0.45,
                HydrologyEdgeFlowLockWeight = 0.38,
                HydrologyEdgeStabilityIterations = 1,
                HydrologyEdgeStabilityWeight = 0.32,
                HydrologyWaterTableClampWeight = 0.42,
                HydrologyWaterTableClampRange = 18,
                HydrologyWaterTableSlopeWeight = 0.55,
                HydrologyFlowPersistence = 0.68,
                HydrologyGradientWeight = 0.35,
                HydrologyGradientSlopeWeight = 0.42,
                HydrologyGradientClamp = 1.65,
                HydrologyDirectionalIterations = 1,
                HydrologyDirectionalBlend = 0.42,
                HydrologyFlowDivergenceClamp = 0.55,
                HydrologyWarpFrequency = 0.0009,
                HydrologyWarpAmplitude = 9,
                
                // Riparian settings
                RiparianSmoothIterations = 2,
                RiparianSmoothBlend = 0.6,
                RiparianSaturationBoost = 0.18,
                RiparianBufferRadius = 1,
                
                // River settings
                RiverCenterThreshold = 0.0125,
                RiverBankThreshold = 0.028,
                RiverDepth = 6,
                RiverNoiseScale = 0.015,
                RiverIntensitySmoothIterations = 3,
                RiverIntensitySmoothBlend = 0.58,
                RiverConfluenceBoost = 0.35,
                RiverFlowAlignmentWeight = 0.28,
                RiverGradientPenalty = 0.42,
                RiverHeadwaterStabilityWeight = 0.35,
                RiverAnisotropyWeight = 0.32,
                RiverReliefPenaltyWeight = 0.25,
                RiverEdgeFeather = 0.45,
                RiverMouthSmoothRadius = 3,
                RiverDeltaWetlandStrength = 0.45,
                RiverSeamFillStrength = 0.5,
                RiverBankErosionWeight = 0.18,
                
                // Lake settings
                LakeSpawnWeightBias = 0.3,
                LakeShorelineBlend = 0.66,
                LakeWetlandSaturationThreshold = 0.55,
                LakeOutflowCarveDepth = 2,
                LakeBasinSmoothIterations = 2,
                LakeShelfDepth = 2,
                LakeMaxRadius = 9,
                LakeWetlandBufferRadius = 2,
                LakeRiverProximitySuppression = 0.35,
                LakeInflowBlendWeight = 0.42,
                LakeRimErosionWeight = 0.3,
                
                // Cave settings
                CaveEdgeSealStrength = 0.45,
                SupportPillarChance = 0.28,
                CaveStabilitySmoothIterations = 1,
                CaveStabilitySmoothBlend = 0.55,
                CaveSupportDensity = 0.6,
                CaveSupportHydrationBias = 0.42,
                CaveSupportFlowBias = 0.2,
                CaveMoistureRetentionWeight = 0.35,
                CaveRiparianPlugDepth = 2,
                CaveCeilingStabilityWeight = 0.35,
                CaveHydrologyWeight = 0.45,
                CaveFlowWeight = 0.25,
                CaveRoughnessWeight = 0.1,
                CaveDepthWeight = 0.2,
                CaveRiverSuppressionWeight = 0.35,
                
                // Feature toggles
                EnableRivers = true,
                EnableLakes = true,
                EnableCaves = true,
                UseImprovedCaves = true,
                UseImprovedRivers = true,
                UseImprovedLakes = true,
                
                // Client-specific settings
                MaxConcurrentChunkGenerations = 4,
                UpdateBatchSize = 24,
                UpdateIntervalMs = 100,
                DefaultRenderDistance = 10,
                DefaultMapScale = 1,
                DefaultShowCoordinates = true,
                DefaultShowBiomeInfo = true,
                DefaultTerrainQuality = 2,
                DefaultWaterQuality = 2,
                DefaultVegetationQuality = 2,
                DefaultFogEnabled = true,
                DefaultShadowEnabled = true,
                DefaultMaxChunkUpdatesPerFrame = 12,
                DefaultChunkLOD = 2,
                DefaultUnloadDistance = 12,
                
                // Performance settings
                TargetFrameRate = 60,
                VSyncEnabled = true,
                MaxChunkLoadTimeMs = 50,
                ChunkUnloadDelaySeconds = 30,
                
                // Network settings
                NetworkCompressionEnabled = true,
                NetworkCompressionLevel = 6,
                ChunkRequestTimeoutMs = 5000,
                MaxConcurrentChunkRequests = 8,
                
                // Metadata
                GeneratedAtUtc = DateTime.UtcNow.ToString("o"),
                ProfileHash = ComputeProfileHash()
            };
        }
        
        /// <summary>
        /// Load configuration from file
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    var loadedProfile = JsonConvert.DeserializeObject<WorldMapControlProfile>(json);
                    
                    if (loadedProfile != null)
                    {
                        controlProfile = loadedProfile;
                        controlProfile.ProfileHash = ComputeProfileHash();
                        
                        if (enableDebugLogging)
                        {
                            Debug.Log($"[WorldMapControlSystem] Configuration loaded from {configFilePath}");
                        }
                        
                        OnConfigurationLoaded?.Invoke(controlProfile);
                    }
                }
                else
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"[WorldMapControlSystem] Config file not found: {configFilePath}, using defaults");
                    }
                    
                    // Save default configuration
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapControlSystem] Failed to load configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void SaveConfiguration()
        {
            if (!autoSaveConfig) return;
            
            try
            {
                // Update metadata
                controlProfile.GeneratedAtUtc = DateTime.UtcNow.ToString("o");
                controlProfile.ProfileHash = ComputeProfileHash();
                
                // Serialize to JSON
                string json = JsonConvert.SerializeObject(controlProfile, Formatting.Indented);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write to file
                File.WriteAllText(configFilePath, json);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[WorldMapControlSystem] Configuration saved to {configFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldMapControlSystem] Failed to save configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update configuration and notify listeners
        /// </summary>
        public void UpdateConfiguration(WorldMapControlProfile newProfile)
        {
            if (newProfile == null) return;
            
            controlProfile = newProfile;
            controlProfile.ProfileHash = ComputeProfileHash();
            
            OnConfigurationChanged?.Invoke(controlProfile);
            
            if (autoSaveConfig)
            {
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// Get current configuration profile
        /// </summary>
        public WorldMapControlProfile GetConfiguration()
        {
            return controlProfile;
        }
        
        /// <summary>
        /// Reset to default configuration
        /// </summary>
        public void ResetToDefaults()
        {
            controlProfile = CreateDefaultProfile();
            OnConfigurationChanged?.Invoke(controlProfile);
            
            if (autoSaveConfig)
            {
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// Apply configuration to terrain generator
        /// </summary>
        public void ApplyToTerrainGenerator(EnhancedTerrainGenerator generator)
        {
            if (generator == null) return;
            
            // Apply terrain generation parameters
            generator.SetTerrainParameters(new TerrainParameters
            {
                ChunkSize = controlProfile.ChunkSize,
                WorldHeight = 256,
                SeaLevel = controlProfile.GlobalWaterLevel,
                EnableCaves = controlProfile.EnableCaves,
                EnableRivers = controlProfile.EnableRivers,
                EnableLakes = controlProfile.EnableLakes,
                UseImprovedCaves = controlProfile.UseImprovedCaves,
                UseImprovedRivers = controlProfile.UseImprovedRivers,
                UseImprovedLakes = controlProfile.UseImprovedLakes
            });
        }
        
        /// <summary>
        /// Apply configuration to client world controller
        /// </summary>
        public void ApplyToClientController(EnhancedClientWorldController controller)
        {
            if (controller == null) return;
            
            // Apply client-specific settings
            controller.SetViewDistance(controlProfile.DefaultRenderDistance);
            controller.SetChunkUpdateInterval(controlProfile.UpdateIntervalMs / 1000f);
            controller.SetMaxChunksPerFrame(controlProfile.DefaultMaxChunkUpdatesPerFrame);
        }
        
        /// <summary>
        /// Get client-specific configuration
        /// </summary>
        public ClientWorldConfig GetClientConfig()
        {
            return new ClientWorldConfig
            {
                ViewDistance = controlProfile.DefaultRenderDistance,
                ChunkSize = controlProfile.ChunkSize,
                WorldHeight = 256,
                SeaLevel = controlProfile.GlobalWaterLevel,
                MaxConcurrentChunkGenerations = controlProfile.MaxConcurrentChunkGenerations,
                UpdateBatchSize = controlProfile.UpdateBatchSize,
                UpdateIntervalMs = controlProfile.UpdateIntervalMs,
                MapScale = controlProfile.DefaultMapScale,
                ShowCoordinates = controlProfile.DefaultShowCoordinates,
                ShowBiomeInfo = controlProfile.DefaultShowBiomeInfo,
                TerrainQuality = controlProfile.DefaultTerrainQuality,
                WaterQuality = controlProfile.DefaultWaterQuality,
                VegetationQuality = controlProfile.DefaultVegetationQuality,
                FogEnabled = controlProfile.DefaultFogEnabled,
                ShadowEnabled = controlProfile.DefaultShadowEnabled,
                MaxChunkUpdatesPerFrame = controlProfile.DefaultMaxChunkUpdatesPerFrame,
                ChunkLOD = controlProfile.DefaultChunkLOD,
                UnloadDistance = controlProfile.DefaultUnloadDistance,
                TargetFrameRate = controlProfile.TargetFrameRate,
                VSyncEnabled = controlProfile.VSyncEnabled,
                MaxChunkLoadTimeMs = controlProfile.MaxChunkLoadTimeMs,
                ChunkUnloadDelaySeconds = controlProfile.ChunkUnloadDelaySeconds
            };
        }
        
        /// <summary>
        /// Get server-specific configuration
        /// </summary>
        public ServerWorldConfig GetServerConfig()
        {
            return new ServerWorldConfig
            {
                ChunkSize = controlProfile.ChunkSize,
                WorldHeight = 256,
                SeaLevel = controlProfile.GlobalWaterLevel,
                RenderDistance = controlProfile.RenderDistance,
                SimulationDistance = controlProfile.SimulationDistance,
                NetworkCompressionEnabled = controlProfile.NetworkCompressionEnabled,
                NetworkCompressionLevel = controlProfile.NetworkCompressionLevel,
                ChunkRequestTimeoutMs = controlProfile.ChunkRequestTimeoutMs,
                MaxConcurrentChunkRequests = controlProfile.MaxConcurrentChunkRequests,
                
                // Terrain generation settings
                EnableCaves = controlProfile.EnableCaves,
                EnableRivers = controlProfile.EnableRivers,
                EnableLakes = controlProfile.EnableLakes,
                UseImprovedCaves = controlProfile.UseImprovedCaves,
                UseImprovedRivers = controlProfile.UseImprovedRivers,
                UseImprovedLakes = controlProfile.UseImprovedLakes,
                
                // Hydrology settings
                HydrologyGradientStabilityIterations = controlProfile.HydrologyGradientStabilityIterations,
                HydrologyGradientStabilityBlend = controlProfile.HydrologyGradientStabilityBlend,
                HydrologyCurvatureWeight = controlProfile.HydrologyCurvatureWeight,
                HydrologyEdgeBlendRadius = controlProfile.HydrologyEdgeBlendRadius,
                HydrologyVarianceBlend = controlProfile.HydrologyVarianceBlend,
                HydrologyVarianceClamp = controlProfile.HydrologyVarianceClamp,
                HydrologySeamRelaxIterations = controlProfile.HydrologySeamRelaxIterations,
                HydrologySeamRelaxBlend = controlProfile.HydrologySeamRelaxBlend,
                HydrologyEdgeFluxBlend = controlProfile.HydrologyEdgeFluxBlend,
                HydrologyEdgeVarianceClamp = controlProfile.HydrologyEdgeVarianceClamp,
                HydrologySmoothBlend = controlProfile.HydrologySmoothBlend,
                HydrologySmoothIterations = controlProfile.HydrologySmoothIterations,
                HydrologyShorePush = controlProfile.HydrologyShorePush,
                HydrologySlopePenalty = controlProfile.HydrologySlopePenalty,
                HydrologyFlowGain = controlProfile.HydrologyFlowGain,
                HydrologyContinuityWeight = controlProfile.HydrologyContinuityWeight,
                HydrologyEdgeFlowBias = controlProfile.HydrologyEdgeFlowBias,
                HydrologyEdgeTangentWeight = controlProfile.HydrologyEdgeTangentWeight,
                HydrologyEdgeFlowLockWeight = controlProfile.HydrologyEdgeFlowLockWeight,
                HydrologyEdgeStabilityIterations = controlProfile.HydrologyEdgeStabilityIterations,
                HydrologyEdgeStabilityWeight = controlProfile.HydrologyEdgeStabilityWeight,
                HydrologyWaterTableClampWeight = controlProfile.HydrologyWaterTableClampWeight,
                HydrologyWaterTableClampRange = controlProfile.HydrologyWaterTableClampRange,
                HydrologyWaterTableSlopeWeight = controlProfile.HydrologyWaterTableSlopeWeight,
                HydrologyFlowPersistence = controlProfile.HydrologyFlowPersistence,
                HydrologyGradientWeight = controlProfile.HydrologyGradientWeight,
                HydrologyGradientSlopeWeight = controlProfile.HydrologyGradientSlopeWeight,
                HydrologyGradientClamp = controlProfile.HydrologyGradientClamp,
                HydrologyDirectionalIterations = controlProfile.HydrologyDirectionalIterations,
                HydrologyDirectionalBlend = controlProfile.HydrologyDirectionalBlend,
                HydrologyFlowDivergenceClamp = controlProfile.HydrologyFlowDivergenceClamp,
                HydrologyWarpFrequency = controlProfile.HydrologyWarpFrequency,
                HydrologyWarpAmplitude = controlProfile.HydrologyWarpAmplitude,
                
                // Riparian settings
                RiparianSmoothIterations = controlProfile.RiparianSmoothIterations,
                RiparianSmoothBlend = controlProfile.RiparianSmoothBlend,
                RiparianSaturationBoost = controlProfile.RiparianSaturationBoost,
                RiparianBufferRadius = controlProfile.RiparianBufferRadius,
                
                // River settings
                RiverCenterThreshold = controlProfile.RiverCenterThreshold,
                RiverBankThreshold = controlProfile.RiverBankThreshold,
                RiverDepth = controlProfile.RiverDepth,
                RiverNoiseScale = controlProfile.RiverNoiseScale,
                RiverIntensitySmoothIterations = controlProfile.RiverIntensitySmoothIterations,
                RiverIntensitySmoothBlend = controlProfile.RiverIntensitySmoothBlend,
                RiverConfluenceBoost = controlProfile.RiverConfluenceBoost,
                RiverFlowAlignmentWeight = controlProfile.RiverFlowAlignmentWeight,
                RiverGradientPenalty = controlProfile.RiverGradientPenalty,
                RiverHeadwaterStabilityWeight = controlProfile.RiverHeadwaterStabilityWeight,
                RiverAnisotropyWeight = controlProfile.RiverAnisotropyWeight,
                RiverReliefPenaltyWeight = controlProfile.RiverReliefPenaltyWeight,
                RiverEdgeFeather = controlProfile.RiverEdgeFeather,
                RiverMouthSmoothRadius = controlProfile.RiverMouthSmoothRadius,
                RiverDeltaWetlandStrength = controlProfile.RiverDeltaWetlandStrength,
                RiverSeamFillStrength = controlProfile.RiverSeamFillStrength,
                RiverBankErosionWeight = controlProfile.RiverBankErosionWeight,
                
                // Lake settings
                LakeSpawnWeightBias = controlProfile.LakeSpawnWeightBias,
                LakeShorelineBlend = controlProfile.LakeShorelineBlend,
                LakeWetlandSaturationThreshold = controlProfile.LakeWetlandSaturationThreshold,
                LakeOutflowCarveDepth = controlProfile.LakeOutflowCarveDepth,
                LakeBasinSmoothIterations = controlProfile.LakeBasinSmoothIterations,
                LakeShelfDepth = controlProfile.LakeShelfDepth,
                LakeMaxRadius = controlProfile.LakeMaxRadius,
                LakeWetlandBufferRadius = controlProfile.LakeWetlandBufferRadius,
                LakeRiverProximitySuppression = controlProfile.LakeRiverProximitySuppression,
                LakeInflowBlendWeight = controlProfile.LakeInflowBlendWeight,
                LakeRimErosionWeight = controlProfile.LakeRimErosionWeight,
                
                // Cave settings
                CaveEdgeSealStrength = controlProfile.CaveEdgeSealStrength,
                SupportPillarChance = controlProfile.SupportPillarChance,
                CaveStabilitySmoothIterations = controlProfile.CaveStabilitySmoothIterations,
                CaveStabilitySmoothBlend = controlProfile.CaveStabilitySmoothBlend,
                CaveSupportDensity = controlProfile.CaveSupportDensity,
                CaveSupportHydrationBias = controlProfile.CaveSupportHydrationBias,
                CaveSupportFlowBias = controlProfile.CaveSupportFlowBias,
                CaveMoistureRetentionWeight = controlProfile.CaveMoistureRetentionWeight,
                CaveRiparianPlugDepth = controlProfile.CaveRiparianPlugDepth,
                CaveCeilingStabilityWeight = controlProfile.CaveCeilingStabilityWeight,
                CaveHydrologyWeight = controlProfile.CaveHydrologyWeight,
                CaveFlowWeight = controlProfile.CaveFlowWeight,
                CaveRoughnessWeight = controlProfile.CaveRoughnessWeight,
                CaveDepthWeight = controlProfile.CaveDepthWeight,
                CaveRiverSuppressionWeight = controlProfile.CaveRiverSuppressionWeight
            };
        }
        
        /// <summary>
        /// Compute hash for profile validation
        /// </summary>
        private string ComputeProfileHash()
        {
            // Simple hash implementation - in production, use proper cryptographic hash
            var hashSource = $"{controlProfile.ChunkSize}_{controlProfile.RenderDistance}_{controlProfile.GlobalWaterLevel}_{controlProfile.EnableRivers}_{controlProfile.EnableLakes}_{controlProfile.EnableCaves}";
            return hashSource.GetHashCode().ToString("X");
        }
        
        private void OnGUI()
        {
            if (!showControlPanel) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 600));
            GUILayout.BeginVertical("box");
            GUILayout.Label("World Map Control System", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Load Configuration"))
            {
                LoadConfiguration();
            }
            
            if (GUILayout.Button("Save Configuration"))
            {
                SaveConfiguration();
            }
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Profile Hash: {controlProfile.ProfileHash}");
            GUILayout.Label($"Generated: {controlProfile.GeneratedAtUtc}");
            
            GUILayout.Space(10);
            GUILayout.Label("Terrain Features:", EditorStyles.boldLabel);
            GUILayout.Label($"Rivers: {(controlProfile.EnableRivers ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Lakes: {(controlProfile.EnableLakes ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Caves: {(controlProfile.EnableCaves ? "Enabled" : "Disabled")}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnDestroy()
        {
            if (autoSaveConfig)
            {
                SaveConfiguration();
            }
        }
    }
    
    /// <summary>
    /// World map control profile data structure
    /// </summary>
    [Serializable]
    public class WorldMapControlProfile
    {
        public int Version { get; set; }
        public string ProfileHash { get; set; }
        public string GeneratedAtUtc { get; set; }
        
        // Basic world settings
        public int ChunkSize { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public int GlobalWaterLevel { get; set; }
        
        // Hydrology settings
        public int HydrologyGradientStabilityIterations { get; set; }
        public double HydrologyGradientStabilityBlend { get; set; }
        public double HydrologyCurvatureWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public double HydrologyVarianceBlend { get; set; }
        public double HydrologyVarianceClamp { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public double HydrologySeamRelaxBlend { get; set; }
        public double HydrologyEdgeFluxBlend { get; set; }
        public double HydrologyEdgeVarianceClamp { get; set; }
        public double HydrologySmoothBlend { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public double HydrologyShorePush { get; set; }
        public double HydrologySlopePenalty { get; set; }
        public double HydrologyFlowGain { get; set; }
        public double HydrologyContinuityWeight { get; set; }
        public double HydrologyEdgeFlowBias { get; set; }
        public double HydrologyEdgeTangentWeight { get; set; }
        public double HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public double HydrologyEdgeStabilityWeight { get; set; }
        public double HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public double HydrologyWaterTableSlopeWeight { get; set; }
        public double HydrologyFlowPersistence { get; set; }
        public double HydrologyGradientWeight { get; set; }
        public double HydrologyGradientSlopeWeight { get; set; }
        public double HydrologyGradientClamp { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public double HydrologyDirectionalBlend { get; set; }
        public double HydrologyFlowDivergenceClamp { get; set; }
        public double HydrologyWarpFrequency { get; set; }
        public double HydrologyWarpAmplitude { get; set; }
        
        // Riparian settings
        public int RiparianSmoothIterations { get; set; }
        public double RiparianSmoothBlend { get; set; }
        public double RiparianSaturationBoost { get; set; }
        public int RiparianBufferRadius { get; set; }
        
        // River settings
        public double RiverCenterThreshold { get; set; }
        public double RiverBankThreshold { get; set; }
        public int RiverDepth { get; set; }
        public double RiverNoiseScale { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public double RiverIntensitySmoothBlend { get; set; }
        public double RiverConfluenceBoost { get; set; }
        public double RiverFlowAlignmentWeight { get; set; }
        public double RiverGradientPenalty { get; set; }
        public double RiverHeadwaterStabilityWeight { get; set; }
        public double RiverAnisotropyWeight { get; set; }
        public double RiverReliefPenaltyWeight { get; set; }
        public double RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public double RiverDeltaWetlandStrength { get; set; }
        public double RiverSeamFillStrength { get; set; }
        public double RiverBankErosionWeight { get; set; }
        
        // Lake settings
        public double LakeSpawnWeightBias { get; set; }
        public double LakeShorelineBlend { get; set; }
        public double LakeWetlandSaturationThreshold { get; set; }
        public int LakeOutflowCarveDepth { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int LakeShelfDepth { get; set; }
        public int LakeMaxRadius { get; set; }
        public int LakeWetlandBufferRadius { get; set; }
        public double LakeRiverProximitySuppression { get; set; }
        public double LakeInflowBlendWeight { get; set; }
        public double LakeRimErosionWeight { get; set; }
        
        // Cave settings
        public double CaveEdgeSealStrength { get; set; }
        public double SupportPillarChance { get; set; }
        public int CaveStabilitySmoothIterations { get; set; }
        public double CaveStabilitySmoothBlend { get; set; }
        public double CaveSupportDensity { get; set; }
        public double CaveSupportHydrationBias { get; set; }
        public double CaveSupportFlowBias { get; set; }
        public double CaveMoistureRetentionWeight { get; set; }
        public int CaveRiparianPlugDepth { get; set; }
        public double CaveCeilingStabilityWeight { get; set; }
        public double CaveHydrologyWeight { get; set; }
        public double CaveFlowWeight { get; set; }
        public double CaveRoughnessWeight { get; set; }
        public double CaveDepthWeight { get; set; }
        public double CaveRiverSuppressionWeight { get; set; }
        
        // Feature toggles
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool EnableCaves { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
        
        // Client-specific settings
        public int MaxConcurrentChunkGenerations { get; set; }
        public int UpdateBatchSize { get; set; }
        public int UpdateIntervalMs { get; set; }
        public int DefaultRenderDistance { get; set; }
        public int DefaultMapScale { get; set; }
        public bool DefaultShowCoordinates { get; set; }
        public bool DefaultShowBiomeInfo { get; set; }
        public int DefaultTerrainQuality { get; set; }
        public int DefaultWaterQuality { get; set; }
        public int DefaultVegetationQuality { get; set; }
        public bool DefaultFogEnabled { get; set; }
        public bool DefaultShadowEnabled { get; set; }
        public int DefaultMaxChunkUpdatesPerFrame { get; set; }
        public int DefaultChunkLOD { get; set; }
        public int DefaultUnloadDistance { get; set; }
        
        // Performance settings
        public int TargetFrameRate { get; set; }
        public bool VSyncEnabled { get; set; }
        public int MaxChunkLoadTimeMs { get; set; }
        public int ChunkUnloadDelaySeconds { get; set; }
        
        // Network settings
        public bool NetworkCompressionEnabled { get; set; }
        public int NetworkCompressionLevel { get; set; }
        public int ChunkRequestTimeoutMs { get; set; }
        public int MaxConcurrentChunkRequests { get; set; }
    }
    
    /// <summary>
    /// Client-specific world configuration
    /// </summary>
    [Serializable]
    public class ClientWorldConfig
    {
        public int ViewDistance { get; set; }
        public int ChunkSize { get; set; }
        public int WorldHeight { get; set; }
        public int SeaLevel { get; set; }
        public int MaxConcurrentChunkGenerations { get; set; }
        public int UpdateBatchSize { get; set; }
        public int UpdateIntervalMs { get; set; }
        public int MapScale { get; set; }
        public bool ShowCoordinates { get; set; }
        public bool ShowBiomeInfo { get; set; }
        public int TerrainQuality { get; set; }
        public int WaterQuality { get; set; }
        public int VegetationQuality { get; set; }
        public bool FogEnabled { get; set; }
        public bool ShadowEnabled { get; set; }
        public int MaxChunkUpdatesPerFrame { get; set; }
        public int ChunkLOD { get; set; }
        public int UnloadDistance { get; set; }
        public int TargetFrameRate { get; set; }
        public bool VSyncEnabled { get; set; }
        public int MaxChunkLoadTimeMs { get; set; }
        public int ChunkUnloadDelaySeconds { get; set; }
    }
    
    /// <summary>
    /// Server-specific world configuration
    /// </summary>
    [Serializable]
    public class ServerWorldConfig
    {
        public int ChunkSize { get; set; }
        public int WorldHeight { get; set; }
        public int SeaLevel { get; set; }
        public int RenderDistance { get; set; }
        public int SimulationDistance { get; set; }
        public bool NetworkCompressionEnabled { get; set; }
        public int NetworkCompressionLevel { get; set; }
        public int ChunkRequestTimeoutMs { get; set; }
        public int MaxConcurrentChunkRequests { get; set; }
        
        // Terrain generation settings
        public bool EnableCaves { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
        
        // Hydrology settings
        public int HydrologyGradientStabilityIterations { get; set; }
        public double HydrologyGradientStabilityBlend { get; set; }
        public double HydrologyCurvatureWeight { get; set; }
        public int HydrologyEdgeBlendRadius { get; set; }
        public double HydrologyVarianceBlend { get; set; }
        public double HydrologyVarianceClamp { get; set; }
        public int HydrologySeamRelaxIterations { get; set; }
        public double HydrologySeamRelaxBlend { get; set; }
        public double HydrologyEdgeFluxBlend { get; set; }
        public double HydrologyEdgeVarianceClamp { get; set; }
        public double HydrologySmoothBlend { get; set; }
        public int HydrologySmoothIterations { get; set; }
        public double HydrologyShorePush { get; set; }
        public double HydrologySlopePenalty { get; set; }
        public double HydrologyFlowGain { get; set; }
        public double HydrologyContinuityWeight { get; set; }
        public double HydrologyEdgeFlowBias { get; set; }
        public double HydrologyEdgeTangentWeight { get; set; }
        public double HydrologyEdgeFlowLockWeight { get; set; }
        public int HydrologyEdgeStabilityIterations { get; set; }
        public double HydrologyEdgeStabilityWeight { get; set; }
        public double HydrologyWaterTableClampWeight { get; set; }
        public int HydrologyWaterTableClampRange { get; set; }
        public double HydrologyWaterTableSlopeWeight { get; set; }
        public double HydrologyFlowPersistence { get; set; }
        public double HydrologyGradientWeight { get; set; }
        public double HydrologyGradientSlopeWeight { get; set; }
        public double HydrologyGradientClamp { get; set; }
        public int HydrologyDirectionalIterations { get; set; }
        public double HydrologyDirectionalBlend { get; set; }
        public double HydrologyFlowDivergenceClamp { get; set; }
        public double HydrologyWarpFrequency { get; set; }
        public double HydrologyWarpAmplitude { get; set; }
        
        // Riparian settings
        public int RiparianSmoothIterations { get; set; }
        public double RiparianSmoothBlend { get; set; }
        public double RiparianSaturationBoost { get; set; }
        public int RiparianBufferRadius { get; set; }
        
        // River settings
        public double RiverCenterThreshold { get; set; }
        public double RiverBankThreshold { get; set; }
        public int RiverDepth { get; set; }
        public double RiverNoiseScale { get; set; }
        public int RiverIntensitySmoothIterations { get; set; }
        public double RiverIntensitySmoothBlend { get; set; }
        public double RiverConfluenceBoost { get; set; }
        public double RiverFlowAlignmentWeight { get; set; }
        public double RiverGradientPenalty { get; set; }
        public double RiverHeadwaterStabilityWeight { get; set; }
        public double RiverAnisotropyWeight { get; set; }
        public double RiverReliefPenaltyWeight { get; set; }
        public double RiverEdgeFeather { get; set; }
        public int RiverMouthSmoothRadius { get; set; }
        public double RiverDeltaWetlandStrength { get; set; }
        public double RiverSeamFillStrength { get; set; }
        public double RiverBankErosionWeight { get; set; }
        
        // Lake settings
        public double LakeSpawnWeightBias { get; set; }
        public double LakeShorelineBlend { get; set; }
        public double LakeWetlandSaturationThreshold { get; set; }
        public int LakeOutflowCarveDepth { get; set; }
        public int LakeBasinSmoothIterations { get; set; }
        public int LakeShelfDepth { get; set; }
        public int LakeMaxRadius { get; set; }
        public int LakeWetlandBufferRadius { get; set; }
        public double LakeRiverProximitySuppression { get; set; }
        public double LakeInflowBlendWeight { get; set; }
        public double LakeRimErosionWeight { get; set; }
        
        // Cave settings
        public double CaveEdgeSealStrength { get; set; }
        public double SupportPillarChance { get; set; }
        public int CaveStabilitySmoothIterations { get; set; }
        public double CaveStabilitySmoothBlend { get; set; }
        public double CaveSupportDensity { get; set; }
        public double CaveSupportHydrationBias { get; set; }
        public double CaveSupportFlowBias { get; set; }
        public double CaveMoistureRetentionWeight { get; set; }
        public int CaveRiparianPlugDepth { get; set; }
        public double CaveCeilingStabilityWeight { get; set; }
        public double CaveHydrologyWeight { get; set; }
        public double CaveFlowWeight { get; set; }
        public double CaveRoughnessWeight { get; set; }
        public double CaveDepthWeight { get; set; }
        public double CaveRiverSuppressionWeight { get; set; }
    }
    
    /// <summary>
    /// Terrain generation parameters
    /// </summary>
    [Serializable]
    public class TerrainParameters
    {
        public int ChunkSize { get; set; }
        public int WorldHeight { get; set; }
        public int SeaLevel { get; set; }
        public bool EnableCaves { get; set; }
        public bool EnableRivers { get; set; }
        public bool EnableLakes { get; set; }
        public bool UseImprovedCaves { get; set; }
        public bool UseImprovedRivers { get; set; }
        public bool UseImprovedLakes { get; set; }
    }
}
}
