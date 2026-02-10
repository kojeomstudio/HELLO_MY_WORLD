using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GameCommon.DataDriven;
using GameCommon.World;
using GameServerApp.Configuration;
using GameServerApp.Testing;
using SharedProtocol.EnhancedMinecraft;
using ServerWorldMapControlProfileUtility = GameServerApp.World.WorldMapControlProfileUtility;
using SharedWorldMapControlProfileUtility = GameCommon.World.WorldMapControlProfileUtility;
using GameServerApp.World;

namespace GameServerApp
{
    /// <summary>
    /// Enhanced Minecraft-style game server with complete client-server architecture.
    /// Replaces P2P networking with centralized server authority and protobuf communication.
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Display server architecture information
            DisplayServerInfo();
            ProtoRuntime.EnsureInitialized();
            ProtocolValidator.ValidateEnhancedContracts();
            ProtocolStandardization.ValidateProtocolImplementation();
            ProtoDiagnostics.LogSummary();
            ProtoDiagnostics.AssertRegistryClean();
            EmitProtoReport();
            LoadFeatureManifest();
            
            // Check if we should run in server-only mode
            if (args.Contains("--server"))
            {
                return await ServerLauncher.RunAsync(args);
            }

            // Self test: start server, run test client, shutdown
            if (args.Contains("--selftest") || args.Contains("--test-client"))
            {
                try
                {
                    var config = ServerConfig.LoadFromFile();
                    EnsureWorldMapProfile(config);
                    var server = new GameServer(config.Network.Port, config.Database.DatabaseFile, config);

                    var cts = new CancellationTokenSource();
                    var serverTask = server.StartAsync();

                    // Wait a moment for the server to start listening
                    await Task.Delay(300);

                    await TestClient.RunTestSuiteAsync();
                    await RunDummyProtocolProbeAsync(args.Contains("--proto-probe"), CancellationToken.None);

                    server.Stop();
                    await Task.Delay(200);
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Self test failed: {ex.Message}");
                    return 1;
                }
            }

            if (args.Contains("--generate-map-profile"))
            {
                var config = ServerConfig.LoadFromFile();
                EnsureWorldMapProfile(config);
                var worldGenConfig = WorldGenerationConfig.Load(config.World.WorldConfigPath);
                var profile = ServerWorldMapControlProfileUtility.Load(worldGenConfig.MapControlProfilePath);
                Console.WriteLine(
                    $"Generated world map control profile at '{worldGenConfig.MapControlProfilePath}' " +
                    $"(hash: {profile?.ProfileHash ?? "unknown"}, version: {profile?.Version}, signature: {profile?.HydrologySignature}).");
                return 0;
            }

            if (args.Contains("--proto-probe"))
            {
                await RunDummyProtocolProbeAsync(probeNetwork: false, CancellationToken.None);
                return 0;
            }
            
            Console.WriteLine("\nChoose an option:");
            Console.WriteLine("1. Start Enhanced Minecraft Server");
            Console.WriteLine("2. Run Test Client");
            Console.WriteLine("3. Server Configuration");
            Console.WriteLine("4. Exit");
            
            while (true)
            {
                Console.Write("\nEnter your choice (1-4): ");
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        await RunEnhancedServerAsync();
                        break;
                        
                    case "2":
                        await TestClient.RunTestSuiteAsync();
                        break;
                        
                    case "3":
                        DisplayConfigurationMenu();
                        break;
                        
                    case "4":
                        Console.WriteLine("Goodbye!");
                        return 0;
                        
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                        continue;
                }
                
                Console.WriteLine("\nPress any key to return to menu...");
                Console.ReadKey();
                Console.Clear();
                DisplayServerInfo();
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1. Start Enhanced Minecraft Server");
                Console.WriteLine("2. Run Test Client");
                Console.WriteLine("3. Server Configuration");
                Console.WriteLine("4. Exit");
            }
        }

        private static void EmitProtoReport()
        {
            try
            {
                string reportPath = ResolveRepoPath(Path.Combine("config", "proto_reference_report.json"));
                ProtoDiagnostics.WriteReportToFile(reportPath);
                Console.WriteLine($"[Proto] Reference report written to {reportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Proto][WARN] Unable to write proto reference report: {ex.Message}");
            }
        }

        private static void LoadFeatureManifest()
        {
            try
            {
                string[] manifestCandidates =
                {
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-10-session-65.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-10-session-64.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-10-session-63.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-09-session-61.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-09-session-59.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-08-session-58.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-08-session-57.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-08-session-55.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-07-session-53.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-07-session-51.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-06-session-49.json"),
                    Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-06-session-47.json"),
                    Path.Combine("config", "minecraft_feature_core_content_util_2026-02-04.json"),
                    Path.Combine("config", "minecraft_feature_core_content_util_2026-02-03-session-40.json"),
                    Path.Combine("config", "minecraft_feature_core_content_util_2026-02-03.json"),
                    Path.Combine("config", "minecraft_feature_core_content_util_2026-02-02-session-38.json"),
                    Path.Combine("config", "minecraft_feature_core_content_util_2026-02-01.json")
                };

                FeatureManifest? manifest = null;
                string manifestPath = string.Empty;

                foreach (var candidate in manifestCandidates)
                {
                    var resolved = ResolveRepoPath(candidate);
                    manifest = FeatureManifest.TryLoad(resolved);
                    if (manifest != null)
                    {
                        manifestPath = resolved;
                        break;
                    }
                }

                if (manifest == null)
                {
                    Console.WriteLine("[FeatureManifest][WARN] Manifest not found; skipping shared feature load.");
                    return;
                }

                var issues = manifest.Validate();
                Console.WriteLine($"[FeatureManifest] Loaded {manifest.Features.Count} entries (v{manifest.Version}) from {manifestPath}.");
                if (issues.Count > 0)
                {
                    Console.WriteLine("[FeatureManifest][WARN] " + string.Join("; ", issues));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeatureManifest][WARN] Failed to load feature manifest: {ex.Message}");
            }
        }

        private static async Task RunDummyProtocolProbeAsync(bool probeNetwork, CancellationToken cancellationToken)
        {
            try
            {
                string settingsPath = ResolveRepoPath(Path.Combine("config", "protocol_dummy_client.json"));
                var client = DummyProtocolClient.CreateFromConfig(settingsPath);
                bool useNetwork = probeNetwork || client.Settings.ProbeNetwork;
                var result = await client.RunAsync(useNetwork, cancellationToken);
                Console.WriteLine($"[ProtoProbe] RoundTrip={result.RoundTripOk} Descriptor={result.DescriptorName} Validated={string.Join(",", result.ValidatedPackets)}");
                if (result.MissingRequiredPackets.Count > 0)
                {
                    Console.WriteLine("[ProtoProbe][WARN] Missing required bindings: " + string.Join(", ", result.MissingRequiredPackets));
                }
                if (result.MissingPrototypePackets.Count > 0)
                {
                    Console.WriteLine("[ProtoProbe][WARN] Missing prototype bindings: " + string.Join(", ", result.MissingPrototypePackets));
                }
                if (result.OptionalUnregistered.Count > 0)
                {
                    Console.WriteLine("[ProtoProbe] Optional packets without bindings: " + string.Join(", ", result.OptionalUnregistered));
                }
                if (!string.IsNullOrWhiteSpace(result.ReportPath))
                {
                    Console.WriteLine($"[ProtoProbe] Report written to {result.ReportPath}");
                }
                if (!string.IsNullOrWhiteSpace(result.ReferenceReportPath))
                {
                    Console.WriteLine($"[ProtoProbe] Reference report written to {result.ReferenceReportPath}");
                }
                if (result.NetworkProbeAttempted)
                {
                    if (result.NetworkProbeOk)
                    {
                        Console.WriteLine("[ProtoProbe] Network probe succeeded.");
                    }
                    else
                    {
                        Console.WriteLine($"[ProtoProbe][WARN] Network probe failed: {result.NetworkError}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProtoProbe][WARN] Dummy client failed: {ex.Message}");
            }
        }

        private static string ResolveRepoPath(string relativePath)
        {
            string rootCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            string combined = Path.Combine(rootCandidate, relativePath);
            if (File.Exists(combined) || Directory.Exists(combined))
            {
                return combined;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static void DisplayServerInfo()
        {
            Console.WriteLine("===== Minecraft-Like Game Server Architecture =====");
            Console.WriteLine("• Client-Server Architecture (P2P removed)");
            Console.WriteLine("• Google Protocol Buffers for communication");
            Console.WriteLine("• Enhanced SQLite database with full game state");
            Console.WriteLine("• Real-time chunk generation and synchronization");
            Console.WriteLine("• Session management with player persistence");
            Console.WriteLine("• Anti-cheat and server-side validation");
            Console.WriteLine("=============================================");
        }
        
        /// <summary>
        /// Runs the enhanced Minecraft-style server with full client-server architecture.
        /// </summary>
        private static async Task RunEnhancedServerAsync()
        {
            try
            {
                Console.WriteLine("\n=== Starting Enhanced Minecraft Server ===");
                
                var config = ServerConfig.LoadFromFile();
                EnsureWorldMapProfile(config);
                var server = new GameServer(config.Network.Port, config.Database.DatabaseFile, config);
                
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("\n=== Shutdown Signal Received ===");
                    server.Stop();
                };
                
                var serverTask = server.StartAsync();
                
                Console.WriteLine("\n=== Server Commands ===");
                Console.WriteLine("Type 'help' for available commands");
                Console.WriteLine("Type 'stop' or press Ctrl+C to shutdown");
                Console.WriteLine("========================");
                
                // Server command loop
                _ = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var input = Console.ReadLine();
                            if (string.IsNullOrEmpty(input)) continue;
                            
                            await ProcessServerCommand(input.Trim().ToLower(), server);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Command error: {ex.Message}");
                        }
                    }
                });
                
                try
                {
                    await Task.Delay(-1, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected cancellation
                }
                
                Console.WriteLine("Server shutting down gracefully...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private static async Task ProcessServerCommand(string command, GameServer server)
        {
            switch (command)
            {
                case "help":
                    Console.WriteLine("\nAvailable Commands:");
                    Console.WriteLine("  help     - Show this help message");
                    Console.WriteLine("  stop     - Stop the server");
                    Console.WriteLine("  status   - Show server status");
                    Console.WriteLine("  players  - List online players");
                    Console.WriteLine("  config   - Show current configuration");
                    break;
                    
                case "stop":
                    server.Stop();
                    break;
                    
                case "status":
                    Console.WriteLine($"Server Status: Running");
                    Console.WriteLine($"Architecture: Client-Server (was P2P)");
                    Console.WriteLine($"Protocol: Google Protocol Buffers");
                    Console.WriteLine($"Database: SQLite with enhanced schema");
                    break;
                    
                case "config":
                    var config = ServerConfig.LoadFromFile();
                    Console.WriteLine($"\nCurrent Configuration:");
                    Console.WriteLine($"  Network Port: {config.Network.Port}");
                    Console.WriteLine($"  Max Connections: {config.Network.MaxConnections}");
                    Console.WriteLine($"  Database File: {config.Database.DatabaseFile}");
                    Console.WriteLine($"  World Seed: {config.World.WorldSeed}");
                    break;
                    
                default:
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }

        private static void EnsureWorldMapProfile(ServerConfig config)
        {
            var configManager = new DataDrivenConfigManager("config");
            configManager.ValidateConfigurations();

            var worldGenConfig = WorldGenerationConfig.Load(config.World.WorldConfigPath);
            var mapSettings = configManager.GetConfiguration<WorldMapControlSettings>();
            ApplyWorldMapRuntimeOverrides(worldGenConfig, mapSettings);
            var profilePath = ResolveRepoPath(worldGenConfig.MapControlProfilePath);

            var profile = ServerWorldMapControlProfileUtility.Create(worldGenConfig, config.World);
            profile.HydrologySignature = SharedFeatureCatalog.HydrologySignature;
            profile.Version = Math.Max(worldGenConfig.MapControlProfileVersion, profile.Version);
            profile.RenderDistance = Math.Max(profile.RenderDistance, mapSettings.DefaultRenderDistance);
            profile.SimulationDistance = Math.Max(profile.SimulationDistance, mapSettings.DefaultUnloadDistance);
            ServerWorldMapControlProfileUtility.Save(profile, profilePath);
            TryMirrorProfileToStreamingAssets(profilePath);
        }

        private static void ApplyWorldMapRuntimeOverrides(WorldGenerationConfig worldGenConfig, WorldMapControlSettings mapSettings)
        {
            string runtimePath = ResolveRepoPath(Path.Combine("config", "enhanced_world_map_control_server.json"));
            if (!File.Exists(runtimePath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(runtimePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var runtime = JsonSerializer.Deserialize<WorldMapRuntimeServerConfig>(json, options);
                var section = runtime?.WorldMapControl;
                if (section == null || !section.Enabled)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(section.ProfilePath))
                {
                    worldGenConfig.MapControlProfilePath = section.ProfilePath!;
                }

                if (section.ProfileVersion > 0)
                {
                    worldGenConfig.MapControlProfileVersion = Math.Max(section.ProfileVersion, worldGenConfig.MapControlProfileVersion);
                }

                if (section.Defaults != null)
                {
                    if (section.Defaults.RenderDistance > 0)
                    {
                        mapSettings.DefaultRenderDistance = Math.Clamp(section.Defaults.RenderDistance.Value, 2, 32);
                    }

                    if (section.Defaults.MapScale > 0)
                    {
                        mapSettings.DefaultMapScale = Math.Clamp(section.Defaults.MapScale.Value, 0.25, 8.0);
                    }

                    if (section.Defaults.TerrainQuality > 0)
                    {
                        mapSettings.DefaultTerrainQuality = Math.Clamp(section.Defaults.TerrainQuality.Value, 1, 5);
                    }

                    if (section.Defaults.WaterQuality > 0)
                    {
                        mapSettings.DefaultWaterQuality = Math.Clamp(section.Defaults.WaterQuality.Value, 1, 5);
                    }

                    if (section.Defaults.VegetationQuality > 0)
                    {
                        mapSettings.DefaultVegetationQuality = Math.Clamp(section.Defaults.VegetationQuality.Value, 1, 5);
                    }

                    if (section.Defaults.ShowCoordinates.HasValue)
                    {
                        mapSettings.DefaultShowCoordinates = section.Defaults.ShowCoordinates.Value;
                    }

                    if (section.Defaults.ShowBiomeInfo.HasValue)
                    {
                        mapSettings.DefaultShowBiomeInfo = section.Defaults.ShowBiomeInfo.Value;
                    }
                }

                if (section.TerrainGeneration != null)
                {
                    if (section.TerrainGeneration.MaxConcurrentChunkGenerations > 0)
                    {
                        mapSettings.MaxConcurrentChunkGenerations = Math.Clamp(section.TerrainGeneration.MaxConcurrentChunkGenerations.Value, 1, 64);
                    }

                    if (section.TerrainGeneration.UpdateBatchSize > 0)
                    {
                        mapSettings.UpdateBatchSize = Math.Clamp(section.TerrainGeneration.UpdateBatchSize.Value, 1, 1024);
                    }

                    if (section.TerrainGeneration.UpdateIntervalMs > 0)
                    {
                        mapSettings.UpdateIntervalMs = Math.Clamp(section.TerrainGeneration.UpdateIntervalMs.Value, 16, 60_000);
                    }
                }

                if (section.Cache?.MaxCachedChunks > 0)
                {
                    mapSettings.MaxCachedChunks = Math.Max(64, section.Cache.MaxCachedChunks.Value);
                    int unloadFromCacheBudget = Math.Max(2, (int)Math.Ceiling(Math.Sqrt(section.Cache.MaxCachedChunks.Value)));
                    mapSettings.DefaultUnloadDistance = Math.Max(mapSettings.DefaultUnloadDistance, unloadFromCacheBudget);
                }

                mapSettings.DefaultUnloadDistance = Math.Max(mapSettings.DefaultUnloadDistance, mapSettings.DefaultRenderDistance + 2);
                Console.WriteLine(
                    $"[WorldMapControlRuntime] Applied server runtime settings from {runtimePath} " +
                    $"(render={mapSettings.DefaultRenderDistance}, unload={mapSettings.DefaultUnloadDistance}, cache={mapSettings.MaxCachedChunks}, profileVersion={worldGenConfig.MapControlProfileVersion}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlRuntime][WARN] Failed to apply '{runtimePath}': {ex.Message}");
            }
        }

        private static void TryMirrorProfileToStreamingAssets(string profilePath)
        {
            try
            {
                string targetPath = ResolveRepoPath(Path.Combine("Assets", "StreamingAssets", "world-map-control.json"));
                var directory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(profilePath, targetPath, overwrite: true);
                Console.WriteLine($"[WorldMapControlProfile] Mirrored profile to {targetPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlProfile][WARN] Failed to mirror profile to StreamingAssets: {ex.Message}");
            }
        }

        private static void DisplayConfigurationMenu()
        {
            Console.WriteLine("\n=== Server Configuration ===");
            var config = ServerConfig.LoadFromFile();
            
            Console.WriteLine($"Network Settings:");
            Console.WriteLine($"  Port: {config.Network.Port}");
            Console.WriteLine($"  Max Connections: {config.Network.MaxConnections}");
            Console.WriteLine($"  Timeout: {config.Network.ConnectionTimeoutMinutes} minutes");
            
            Console.WriteLine($"\nWorld Settings:");
            Console.WriteLine($"  Default World: {config.World.DefaultWorldName}");
            Console.WriteLine($"  World Seed: {config.World.WorldSeed}");
            Console.WriteLine($"  Chunk Load Radius: {config.World.ChunkLoadRadius}");
            
            Console.WriteLine($"\nGameplay Settings:");
            Console.WriteLine($"  Max Players: {config.Gameplay.MaxPlayersPerWorld}");
            Console.WriteLine($"  PvP Enabled: {config.Gameplay.EnablePvP}");
            Console.WriteLine($"  Flying Enabled: {config.Gameplay.EnableFlying}");
            
            Console.WriteLine($"\nSecurity Settings:");
            Console.WriteLine($"  Authentication Required: {config.Security.RequireAuthentication}");
            Console.WriteLine($"  Session Timeout: {config.Security.SessionTimeoutHours} hours");
            Console.WriteLine($"  Anti-cheat: {config.Security.EnableAntiCheat}");
            
            Console.WriteLine("\nConfiguration file: server-config.json");
            Console.WriteLine("Edit the file and restart the server to apply changes.");
        }

        private sealed class WorldMapRuntimeServerConfig
        {
            [JsonPropertyName("worldMapControl")]
            public WorldMapControlRuntimeSection? WorldMapControl { get; set; }
        }

        private sealed class WorldMapControlRuntimeSection
        {
            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; } = true;

            [JsonPropertyName("profilePath")]
            public string? ProfilePath { get; set; }

            [JsonPropertyName("profileVersion")]
            public int ProfileVersion { get; set; }

            [JsonPropertyName("defaults")]
            public WorldMapRuntimeDefaults? Defaults { get; set; }

            [JsonPropertyName("cache")]
            public WorldMapRuntimeCache? Cache { get; set; }

            [JsonPropertyName("terrainGeneration")]
            public WorldMapRuntimeTerrainGeneration? TerrainGeneration { get; set; }
        }

        private sealed class WorldMapRuntimeDefaults
        {
            [JsonPropertyName("renderDistance")]
            public int? RenderDistance { get; set; }

            [JsonPropertyName("mapScale")]
            public double? MapScale { get; set; }

            [JsonPropertyName("showCoordinates")]
            public bool? ShowCoordinates { get; set; }

            [JsonPropertyName("showBiomeInfo")]
            public bool? ShowBiomeInfo { get; set; }

            [JsonPropertyName("terrainQuality")]
            public int? TerrainQuality { get; set; }

            [JsonPropertyName("waterQuality")]
            public int? WaterQuality { get; set; }

            [JsonPropertyName("vegetationQuality")]
            public int? VegetationQuality { get; set; }
        }

        private sealed class WorldMapRuntimeCache
        {
            [JsonPropertyName("maxCachedChunks")]
            public int? MaxCachedChunks { get; set; }
        }

        private sealed class WorldMapRuntimeTerrainGeneration
        {
            [JsonPropertyName("maxConcurrentChunkGenerations")]
            public int? MaxConcurrentChunkGenerations { get; set; }

            [JsonPropertyName("updateBatchSize")]
            public int? UpdateBatchSize { get; set; }

            [JsonPropertyName("updateIntervalMs")]
            public int? UpdateIntervalMs { get; set; }
        }
    }
}
