using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ValidateWorldMapQueuePolicyParity();
            ValidateWorldMapProfileParity();
            
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
                string profilePath = ResolveRepoPath(worldGenConfig.MapControlProfilePath);
                var profile = ServerWorldMapControlProfileUtility.Load(profilePath);
                Console.WriteLine(
                    $"Generated world map control profile at '{profilePath}' " +
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
                var manifestCandidates = DiscoverFeatureManifestCandidates().ToList();
                if (manifestCandidates.Count == 0)
                {
                    manifestCandidates.Add(ResolveRepoPath(Path.Combine("config", "minecraft_feature_client_server_core_content_util_2026-02-28-session-134.json")));
                    manifestCandidates.Add(ResolveRepoPath(Path.Combine("config", "minecraft_feature_core_content_util_2026-02-04.json")));
                }

                FeatureManifest? manifest = null;
                string manifestPath = string.Empty;

                foreach (var candidate in manifestCandidates)
                {
                    var resolved = Path.IsPathRooted(candidate) ? candidate : ResolveRepoPath(candidate);
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

        private static IEnumerable<string> DiscoverFeatureManifestCandidates()
        {
            string configDirectory = ResolveRepoPath("config");
            if (!Directory.Exists(configDirectory))
            {
                return Array.Empty<string>();
            }

            var patterns = new[]
            {
                "minecraft_feature_client_server_core_content_util_*.json",
                "minecraft_feature_core_content_util_*.json"
            };

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pattern in patterns)
            {
                foreach (var path in Directory.GetFiles(configDirectory, pattern, SearchOption.TopDirectoryOnly))
                {
                    candidates.Add(path);
                }
            }

            return candidates
                .OrderByDescending(path => TryExtractSessionNumber(Path.GetFileNameWithoutExtension(path)))
                .ThenByDescending(File.GetLastWriteTimeUtc)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static int TryExtractSessionNumber(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return -1;
            }

            int marker = fileName.LastIndexOf("session-", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                return -1;
            }

            int start = marker + "session-".Length;
            int end = start;
            while (end < fileName.Length && char.IsDigit(fileName[end]))
            {
                end++;
            }

            if (end <= start)
            {
                return -1;
            }

            return int.TryParse(fileName.Substring(start, end - start), out int value) ? value : -1;
        }

        private static void ValidateWorldMapQueuePolicyParity()
        {
            try
            {
                string serverPolicyPath = ResolveRepoPath(Path.Combine("config", "world_map_control_queue_policy.json"));
                string clientPolicyPath = ResolveRepoPath(Path.Combine("Assets", "StreamingAssets", "world_map_control_queue_policy.json"));

                if (!File.Exists(serverPolicyPath) || !File.Exists(clientPolicyPath))
                {
                    Console.WriteLine(
                        $"[WorldMapQueuePolicy][WARN] Skipped parity check (server='{serverPolicyPath}', client='{clientPolicyPath}').");
                    return;
                }

                string serverJson = File.ReadAllText(serverPolicyPath);
                string clientJson = File.ReadAllText(clientPolicyPath);
                int serverVersion = ReadJsonVersion(serverJson);
                int clientVersion = ReadJsonVersion(clientJson);
                string serverHash = ComputeSha256Hex(serverJson);
                string clientHash = ComputeSha256Hex(clientJson);

                if (string.Equals(serverHash, clientHash, StringComparison.OrdinalIgnoreCase) && serverVersion == clientVersion)
                {
                    Console.WriteLine(
                        $"[WorldMapQueuePolicy] Server/client policy parity OK (version={serverVersion}, hash={serverHash[..12]}...).");
                    return;
                }

                Console.WriteLine(
                    $"[WorldMapQueuePolicy][WARN] Policy drift detected: server(v={serverVersion}, hash={serverHash[..12]}...) != client(v={clientVersion}, hash={clientHash[..12]}...). Mirroring server policy to StreamingAssets.");
                string? clientDirectory = Path.GetDirectoryName(clientPolicyPath);
                if (!string.IsNullOrWhiteSpace(clientDirectory))
                {
                    Directory.CreateDirectory(clientDirectory);
                }

                File.Copy(serverPolicyPath, clientPolicyPath, overwrite: true);
                Console.WriteLine($"[WorldMapQueuePolicy] Mirrored policy to {clientPolicyPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapQueuePolicy][WARN] Parity validation failed: {ex.Message}");
            }
        }

        private static void ValidateWorldMapProfileParity()
        {
            try
            {
                string serverProfilePath = ResolveRepoPath(Path.Combine("config", "world_map_control_profile.json"));
                string clientProfilePath = ResolveRepoPath(Path.Combine("Assets", "StreamingAssets", "world-map-control.json"));

                if (!File.Exists(serverProfilePath) || !File.Exists(clientProfilePath))
                {
                    Console.WriteLine(
                        $"[WorldMapControlProfile][WARN] Skipped parity check (server='{serverProfilePath}', client='{clientProfilePath}').");
                    return;
                }

                var serverProfile = SharedWorldMapControlProfileUtility.Load(serverProfilePath);
                var clientProfile = SharedWorldMapControlProfileUtility.Load(clientProfilePath);
                if (serverProfile == null || clientProfile == null)
                {
                    Console.WriteLine("[WorldMapControlProfile][WARN] Unable to read profiles; skipping parity check.");
                    return;
                }

                string computedServerHash = SharedWorldMapControlProfileUtility.ComputeHash(serverProfile);
                string computedClientHash = SharedWorldMapControlProfileUtility.ComputeHash(clientProfile);
                string serverHash = string.IsNullOrWhiteSpace(serverProfile.ProfileHash)
                    ? computedServerHash
                    : serverProfile.ProfileHash;
                string clientHash = string.IsNullOrWhiteSpace(clientProfile.ProfileHash)
                    ? computedClientHash
                    : clientProfile.ProfileHash;
                bool serverHashStale = !string.IsNullOrWhiteSpace(serverProfile.ProfileHash) &&
                                       !string.Equals(serverProfile.ProfileHash, computedServerHash, StringComparison.OrdinalIgnoreCase);
                bool clientHashStale = !string.IsNullOrWhiteSpace(clientProfile.ProfileHash) &&
                                       !string.Equals(clientProfile.ProfileHash, computedClientHash, StringComparison.OrdinalIgnoreCase);

                bool versionMismatch = clientProfile.Version < serverProfile.Version ||
                                       clientProfile.Version < SharedFeatureCatalog.MapControlProfileVersion;
                bool signatureMismatch = !string.Equals(
                    clientProfile.HydrologySignature,
                    serverProfile.HydrologySignature,
                    StringComparison.OrdinalIgnoreCase);
                bool hashMismatch = !string.Equals(clientHash, serverHash, StringComparison.OrdinalIgnoreCase) ||
                                    serverHashStale ||
                                    clientHashStale;

                if (versionMismatch || signatureMismatch || hashMismatch)
                {
                    if (serverHashStale)
                    {
                        serverProfile.ProfileHash = computedServerHash;
                        serverProfile.GeneratedAtUtc = DateTime.UtcNow;
                        SharedWorldMapControlProfileUtility.Save(serverProfile, serverProfilePath);
                    }

                    MirrorProfile(serverProfilePath, clientProfilePath);
                    Console.WriteLine(
                        $"[WorldMapControlProfile] Repaired parity " +
                        $"(versionMismatch={versionMismatch}, signatureMismatch={signatureMismatch}, hashMismatch={hashMismatch}).");
                    return;
                }

                Console.WriteLine(
                    $"[WorldMapControlProfile] Server/client parity verified " +
                    $"(version={serverProfile.Version}, signature={serverProfile.HydrologySignature}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlProfile][WARN] Parity validation failed: {ex.Message}");
            }
        }

        private static int ReadJsonVersion(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("version", out var versionElement) &&
                    versionElement.ValueKind == JsonValueKind.Number &&
                    versionElement.TryGetInt32(out int version))
                {
                    return version;
                }
            }
            catch
            {
            }

            return 0;
        }

        private static string ComputeSha256Hex(string content)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
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
                Console.WriteLine($"[ProtoProbe] Descriptor coverage ratio: {result.DescriptorCoverageRatio:F3}");
                if (result.MissingGeneratedRequiredDescriptors.Count > 0)
                {
                    Console.WriteLine(
                        "[ProtoProbe][WARN] Missing generated required descriptors: " +
                        string.Join(", ", result.MissingGeneratedRequiredDescriptors));
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
            ApplyWorldMapQueuePolicyOverrides(mapSettings);
            var profilePath = ResolveRepoPath(worldGenConfig.MapControlProfilePath);

            var profile = ServerWorldMapControlProfileUtility.Create(worldGenConfig, config.World);
            profile.HydrologySignature = SharedFeatureCatalog.HydrologySignature;
            profile.Version = Math.Max(worldGenConfig.MapControlProfileVersion, profile.Version);
            profile.RenderDistance = Math.Max(profile.RenderDistance, mapSettings.DefaultRenderDistance);
            profile.SimulationDistance = Math.Max(profile.SimulationDistance, mapSettings.DefaultUnloadDistance);
            profile.ProfileHash = ServerWorldMapControlProfileUtility.ComputeHash(profile);
            ServerWorldMapControlProfileUtility.Save(profile, profilePath);
            TryMirrorProfileToRootConfig(profilePath, worldGenConfig.MapControlProfilePath);
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

                worldGenConfig.MapControlProfileVersion = Math.Max(worldGenConfig.MapControlProfileVersion, SharedFeatureCatalog.MapControlProfileVersion);

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

                    if (section.TerrainGeneration.UpdateBatchSize > 0)
                    {
                        mapSettings.MaxQueuedChunkRequests = Math.Clamp(section.TerrainGeneration.UpdateBatchSize.Value * 32, 128, 16384);
                    }

                    if (section.TerrainGeneration.MaxQueuedChunkRequests > 0)
                    {
                        mapSettings.MaxQueuedChunkRequests = Math.Clamp(section.TerrainGeneration.MaxQueuedChunkRequests.Value, 128, 16384);
                    }

                    if (section.TerrainGeneration.QueuePressureFactor > 0)
                    {
                        mapSettings.QueuePressureFactor = Math.Clamp(section.TerrainGeneration.QueuePressureFactor.Value, 1, 8);
                    }

                    if (section.TerrainGeneration.QueueSlackRatio is > 0)
                    {
                        mapSettings.QueueSlackRatio = Math.Clamp(section.TerrainGeneration.QueueSlackRatio.Value, 1.1, 6.0);
                    }

                    if (section.TerrainGeneration.QueueBurstSlackMultiplier is > 0)
                    {
                        mapSettings.QueueBurstSlackMultiplier = Math.Clamp(section.TerrainGeneration.QueueBurstSlackMultiplier.Value, 1.0, 3.0);
                    }

                    if (section.TerrainGeneration.QueueLoadSheddingThreshold is > 0)
                    {
                        mapSettings.QueueLoadSheddingThreshold = Math.Clamp(section.TerrainGeneration.QueueLoadSheddingThreshold.Value, 0.5, 0.98);
                    }

                    if (section.TerrainGeneration.QueueEmergencyBrakeThreshold is > 0)
                    {
                        mapSettings.QueueEmergencyBrakeThreshold = Math.Clamp(section.TerrainGeneration.QueueEmergencyBrakeThreshold.Value, 0.75, 4.0);
                    }

                    if (section.TerrainGeneration.QueueLoadEmaBlend is > 0)
                    {
                        mapSettings.QueueLoadEmaBlend = WorldMapQueuePolicy.ClampEmaBlend(section.TerrainGeneration.QueueLoadEmaBlend.Value, mapSettings.QueueLoadEmaBlend);
                    }

                    if (section.TerrainGeneration.QueueEmergencyReleaseRatio is > 0)
                    {
                        mapSettings.QueueEmergencyReleaseRatio = WorldMapQueuePolicy.ClampEmergencyReleaseRatio(section.TerrainGeneration.QueueEmergencyReleaseRatio.Value, mapSettings.QueueEmergencyReleaseRatio);
                    }

                    if (section.TerrainGeneration.QueueTrendBoostWeight is > 0)
                    {
                        mapSettings.QueueTrendBoostWeight = WorldMapQueuePolicy.ClampTrendBoostWeight(section.TerrainGeneration.QueueTrendBoostWeight.Value, mapSettings.QueueTrendBoostWeight);
                    }

                    if (section.TerrainGeneration.QueueShockAbsorberWeight is > 0)
                    {
                        mapSettings.QueueShockAbsorberWeight = WorldMapQueuePolicy.ClampShockAbsorberWeight(section.TerrainGeneration.QueueShockAbsorberWeight.Value, mapSettings.QueueShockAbsorberWeight);
                    }

                    if (section.TerrainGeneration.QueueOverloadDrainFactor > 0)
                    {
                        mapSettings.QueueOverloadDrainFactor = Math.Clamp(section.TerrainGeneration.QueueOverloadDrainFactor.Value, 1, 16);
                    }

                    if (section.TerrainGeneration.QueueBackoffDelayMs > 0)
                    {
                        mapSettings.QueueBackoffDelayMs = Math.Clamp(section.TerrainGeneration.QueueBackoffDelayMs.Value, 1, 200);
                    }

                    if (section.TerrainGeneration.QueueEmergencyHoldTicks > 0)
                    {
                        mapSettings.QueueEmergencyHoldTicks = Math.Clamp(section.TerrainGeneration.QueueEmergencyHoldTicks.Value, 1, 128);
                    }

                    if (section.TerrainGeneration.QueueRecoveryRampTicks > 0)
                    {
                        mapSettings.QueueRecoveryRampTicks = Math.Clamp(section.TerrainGeneration.QueueRecoveryRampTicks.Value, 1, 256);
                    }

                    if (section.TerrainGeneration.QueueNearChunkKeepCount > 0)
                    {
                        mapSettings.QueueNearChunkKeepCount = Math.Clamp(section.TerrainGeneration.QueueNearChunkKeepCount.Value, 8, 512);
                    }

                    if (section.TerrainGeneration.QueueHotspotBias is > 0)
                    {
                        mapSettings.QueueHotspotBias = WorldMapQueuePolicy.ClampHotspotBias(section.TerrainGeneration.QueueHotspotBias.Value, mapSettings.QueueHotspotBias);
                    }

                    if (section.TerrainGeneration.QueueHotspotEmergencyPenalty is > 0)
                    {
                        mapSettings.QueueHotspotEmergencyPenalty = WorldMapQueuePolicy.ClampHotspotEmergencyPenalty(section.TerrainGeneration.QueueHotspotEmergencyPenalty.Value, mapSettings.QueueHotspotEmergencyPenalty);
                    }

                    if (section.TerrainGeneration.QueueHotspotRetentionSeconds > 0)
                    {
                        mapSettings.QueueHotspotRetentionSeconds = Math.Clamp(section.TerrainGeneration.QueueHotspotRetentionSeconds.Value, 5, 300);
                    }

                    if (section.TerrainGeneration.InflightChunkTimeoutSeconds > 0)
                    {
                        mapSettings.InflightChunkTimeoutSeconds = Math.Clamp(section.TerrainGeneration.InflightChunkTimeoutSeconds.Value, 5, 600);
                    }

                    if (section.TerrainGeneration.InflightPruneIntervalSeconds > 0)
                    {
                        mapSettings.InflightPruneIntervalSeconds = Math.Clamp(section.TerrainGeneration.InflightPruneIntervalSeconds.Value, 1, 120);
                    }

                    if (section.TerrainGeneration.QueueStalePruneMax > 0)
                    {
                        mapSettings.QueueStalePruneMax = Math.Clamp(section.TerrainGeneration.QueueStalePruneMax.Value, 8, 256);
                    }

                    if (section.TerrainGeneration.QueueStalePruneEmergencyMultiplier is > 0)
                    {
                        mapSettings.QueueStalePruneEmergencyMultiplier = Math.Clamp(section.TerrainGeneration.QueueStalePruneEmergencyMultiplier.Value, 1.0, 3.0);
                    }
                }

                if (section.Cache?.MaxCachedChunks > 0)
                {
                    mapSettings.MaxCachedChunks = Math.Max(64, section.Cache.MaxCachedChunks.Value);
                    int unloadFromCacheBudget = Math.Max(2, (int)Math.Ceiling(Math.Sqrt(section.Cache.MaxCachedChunks.Value)));
                    mapSettings.DefaultUnloadDistance = Math.Max(mapSettings.DefaultUnloadDistance, unloadFromCacheBudget);
                    mapSettings.MaxQueuedChunkRequests = Math.Max(mapSettings.MaxQueuedChunkRequests, Math.Clamp(section.Cache.MaxCachedChunks.Value * 3, 128, 16384));
                }

                if (section.Cache?.MaxQueuedChunkRequests > 0)
                {
                    mapSettings.MaxQueuedChunkRequests = Math.Clamp(section.Cache.MaxQueuedChunkRequests.Value, 128, 16384);
                }

                if (section.Cache?.QueuePressureFactor > 0)
                {
                    mapSettings.QueuePressureFactor = Math.Clamp(section.Cache.QueuePressureFactor.Value, 1, 8);
                }

                if (section.Cache?.QueueSlackRatio is > 0)
                {
                    mapSettings.QueueSlackRatio = Math.Clamp(section.Cache.QueueSlackRatio.Value, 1.1, 6.0);
                }

                if (section.Cache?.QueueBurstSlackMultiplier is > 0)
                {
                    mapSettings.QueueBurstSlackMultiplier = Math.Clamp(section.Cache.QueueBurstSlackMultiplier.Value, 1.0, 3.0);
                }

                if (section.Cache?.QueueLoadSheddingThreshold is > 0)
                {
                    mapSettings.QueueLoadSheddingThreshold = Math.Clamp(section.Cache.QueueLoadSheddingThreshold.Value, 0.5, 0.98);
                }

                if (section.Cache?.QueueEmergencyBrakeThreshold is > 0)
                {
                    mapSettings.QueueEmergencyBrakeThreshold = Math.Clamp(section.Cache.QueueEmergencyBrakeThreshold.Value, 0.75, 4.0);
                }

                if (section.Cache?.QueueLoadEmaBlend is > 0)
                {
                    mapSettings.QueueLoadEmaBlend = WorldMapQueuePolicy.ClampEmaBlend(section.Cache.QueueLoadEmaBlend.Value, mapSettings.QueueLoadEmaBlend);
                }

                if (section.Cache?.QueueEmergencyReleaseRatio is > 0)
                {
                    mapSettings.QueueEmergencyReleaseRatio = WorldMapQueuePolicy.ClampEmergencyReleaseRatio(section.Cache.QueueEmergencyReleaseRatio.Value, mapSettings.QueueEmergencyReleaseRatio);
                }

                if (section.Cache?.QueueTrendBoostWeight is > 0)
                {
                    mapSettings.QueueTrendBoostWeight = WorldMapQueuePolicy.ClampTrendBoostWeight(section.Cache.QueueTrendBoostWeight.Value, mapSettings.QueueTrendBoostWeight);
                }

                if (section.Cache?.QueueShockAbsorberWeight is > 0)
                {
                    mapSettings.QueueShockAbsorberWeight = WorldMapQueuePolicy.ClampShockAbsorberWeight(section.Cache.QueueShockAbsorberWeight.Value, mapSettings.QueueShockAbsorberWeight);
                }

                if (section.Cache?.QueueOverloadDrainFactor > 0)
                {
                    mapSettings.QueueOverloadDrainFactor = Math.Clamp(section.Cache.QueueOverloadDrainFactor.Value, 1, 16);
                }

                if (section.Cache?.QueueBackoffDelayMs > 0)
                {
                    mapSettings.QueueBackoffDelayMs = Math.Clamp(section.Cache.QueueBackoffDelayMs.Value, 1, 200);
                }

                if (section.Cache?.QueueEmergencyHoldTicks > 0)
                {
                    mapSettings.QueueEmergencyHoldTicks = Math.Clamp(section.Cache.QueueEmergencyHoldTicks.Value, 1, 128);
                }

                if (section.Cache?.QueueRecoveryRampTicks > 0)
                {
                    mapSettings.QueueRecoveryRampTicks = Math.Clamp(section.Cache.QueueRecoveryRampTicks.Value, 1, 256);
                }

                if (section.Cache?.QueueNearChunkKeepCount > 0)
                {
                    mapSettings.QueueNearChunkKeepCount = Math.Clamp(section.Cache.QueueNearChunkKeepCount.Value, 8, 512);
                }

                if (section.Cache?.QueueHotspotBias is > 0)
                {
                    mapSettings.QueueHotspotBias = WorldMapQueuePolicy.ClampHotspotBias(section.Cache.QueueHotspotBias.Value, mapSettings.QueueHotspotBias);
                }

                if (section.Cache?.QueueHotspotEmergencyPenalty is > 0)
                {
                    mapSettings.QueueHotspotEmergencyPenalty = WorldMapQueuePolicy.ClampHotspotEmergencyPenalty(section.Cache.QueueHotspotEmergencyPenalty.Value, mapSettings.QueueHotspotEmergencyPenalty);
                }

                if (section.Cache?.QueueHotspotRetentionSeconds > 0)
                {
                    mapSettings.QueueHotspotRetentionSeconds = Math.Clamp(section.Cache.QueueHotspotRetentionSeconds.Value, 5, 300);
                }

                if (section.Cache?.InflightChunkTimeoutSeconds > 0)
                {
                    mapSettings.InflightChunkTimeoutSeconds = Math.Clamp(section.Cache.InflightChunkTimeoutSeconds.Value, 5, 600);
                }

                if (section.Cache?.InflightPruneIntervalSeconds > 0)
                {
                    mapSettings.InflightPruneIntervalSeconds = Math.Clamp(section.Cache.InflightPruneIntervalSeconds.Value, 1, 120);
                }

                if (section.Cache?.QueueStalePruneMax > 0)
                {
                    mapSettings.QueueStalePruneMax = Math.Clamp(section.Cache.QueueStalePruneMax.Value, 8, 256);
                }

                if (section.Cache?.QueueStalePruneEmergencyMultiplier is > 0)
                {
                    mapSettings.QueueStalePruneEmergencyMultiplier = Math.Clamp(section.Cache.QueueStalePruneEmergencyMultiplier.Value, 1.0, 3.0);
                }

                mapSettings.QueuePressureFactor = mapSettings.MaxCachedChunks > 0 && mapSettings.MaxQueuedChunkRequests > mapSettings.MaxCachedChunks * 2
                    ? Math.Max(3, mapSettings.QueuePressureFactor)
                    : Math.Max(2, mapSettings.QueuePressureFactor);

                mapSettings.QueueSlackRatio = mapSettings.MaxCachedChunks > 0 && mapSettings.MaxQueuedChunkRequests > mapSettings.MaxCachedChunks * 2
                    ? Math.Max(2.2, mapSettings.QueueSlackRatio)
                    : Math.Max(1.8, mapSettings.QueueSlackRatio);
                mapSettings.QueueLoadSheddingThreshold = Math.Clamp(mapSettings.QueueLoadSheddingThreshold, 0.5, 0.98);
                mapSettings.QueueEmergencyBrakeThreshold = Math.Clamp(mapSettings.QueueEmergencyBrakeThreshold, 0.75, 4.0);

                mapSettings.DefaultUnloadDistance = Math.Max(mapSettings.DefaultUnloadDistance, mapSettings.DefaultRenderDistance + 2);
                Console.WriteLine(
                    $"[WorldMapControlRuntime] Applied server runtime settings from {runtimePath} " +
                    $"(render={mapSettings.DefaultRenderDistance}, unload={mapSettings.DefaultUnloadDistance}, cache={mapSettings.MaxCachedChunks}, queueLimit={mapSettings.MaxQueuedChunkRequests}, queuePressure={mapSettings.QueuePressureFactor}, queueSlack={mapSettings.QueueSlackRatio:F2}, burstSlack={mapSettings.QueueBurstSlackMultiplier:F2}, shed={mapSettings.QueueLoadSheddingThreshold:F2}, emergencyBrake={mapSettings.QueueEmergencyBrakeThreshold:F2}, emaBlend={mapSettings.QueueLoadEmaBlend:F2}, releaseRatio={mapSettings.QueueEmergencyReleaseRatio:F2}, trend={mapSettings.QueueTrendBoostWeight:F2}, shock={mapSettings.QueueShockAbsorberWeight:F2}, hotspotBias={mapSettings.QueueHotspotBias:F2}, hotspotEmergencyPenalty={mapSettings.QueueHotspotEmergencyPenalty:F2}, hotspotRetentionSec={mapSettings.QueueHotspotRetentionSeconds}, nearKeep={mapSettings.QueueNearChunkKeepCount}, drain={mapSettings.QueueOverloadDrainFactor}, backoffMs={mapSettings.QueueBackoffDelayMs}, holdTicks={mapSettings.QueueEmergencyHoldTicks}, recoveryRampTicks={mapSettings.QueueRecoveryRampTicks}, inflightTimeoutSec={mapSettings.InflightChunkTimeoutSeconds}, inflightPruneSec={mapSettings.InflightPruneIntervalSeconds}, stalePruneMax={mapSettings.QueueStalePruneMax}, stalePruneEmergencyMultiplier={mapSettings.QueueStalePruneEmergencyMultiplier:F2}, profileVersion={worldGenConfig.MapControlProfileVersion}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlRuntime][WARN] Failed to apply '{runtimePath}': {ex.Message}");
            }
        }

        private static void ApplyWorldMapQueuePolicyOverrides(WorldMapControlSettings mapSettings)
        {
            string queuePolicyPath = ResolveRepoPath(Path.Combine("config", "world_map_control_queue_policy.json"));
            if (!File.Exists(queuePolicyPath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(queuePolicyPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var queuePolicy = JsonSerializer.Deserialize<WorldMapQueuePolicyConfig>(json, options);
                if (queuePolicy?.Server == null)
                {
                    return;
                }

                var server = queuePolicy.Server;
                if (server.MaxQueuedChunkRequests > 0)
                {
                    mapSettings.MaxQueuedChunkRequests = Math.Clamp(server.MaxQueuedChunkRequests.Value, 128, 16384);
                }

                if (server.QueuePressureFactor > 0)
                {
                    mapSettings.QueuePressureFactor = Math.Clamp(server.QueuePressureFactor.Value, 1, 8);
                }

                if (server.MaxConcurrentChunkGenerations > 0)
                {
                    mapSettings.MaxConcurrentChunkGenerations = Math.Clamp(server.MaxConcurrentChunkGenerations.Value, 1, 64);
                }

                if (server.UpdateBatchSize > 0)
                {
                    mapSettings.UpdateBatchSize = Math.Clamp(server.UpdateBatchSize.Value, 1, 1024);
                }

                if (server.UpdateIntervalMs > 0)
                {
                    mapSettings.UpdateIntervalMs = Math.Clamp(server.UpdateIntervalMs.Value, 16, 60000);
                }

                if (server.QueueSlackRatio is > 0)
                {
                    mapSettings.QueueSlackRatio = Math.Clamp(server.QueueSlackRatio.Value, 1.1, 6.0);
                }

                if (server.QueueBurstSlackMultiplier is > 0)
                {
                    mapSettings.QueueBurstSlackMultiplier = Math.Clamp(server.QueueBurstSlackMultiplier.Value, 1.0, 3.0);
                }

                if (server.QueueLoadSheddingThreshold is > 0)
                {
                    mapSettings.QueueLoadSheddingThreshold = Math.Clamp(server.QueueLoadSheddingThreshold.Value, 0.5, 0.98);
                }

                if (server.QueueEmergencyBrakeThreshold is > 0)
                {
                    mapSettings.QueueEmergencyBrakeThreshold = Math.Clamp(server.QueueEmergencyBrakeThreshold.Value, 0.75, 4.0);
                }

                if (server.QueueLoadEmaBlend is > 0)
                {
                    mapSettings.QueueLoadEmaBlend = WorldMapQueuePolicy.ClampEmaBlend(server.QueueLoadEmaBlend.Value, mapSettings.QueueLoadEmaBlend);
                }

                if (server.QueueEmergencyReleaseRatio is > 0)
                {
                    mapSettings.QueueEmergencyReleaseRatio = WorldMapQueuePolicy.ClampEmergencyReleaseRatio(server.QueueEmergencyReleaseRatio.Value, mapSettings.QueueEmergencyReleaseRatio);
                }

                if (server.QueueTrendBoostWeight is > 0)
                {
                    mapSettings.QueueTrendBoostWeight = WorldMapQueuePolicy.ClampTrendBoostWeight(server.QueueTrendBoostWeight.Value, mapSettings.QueueTrendBoostWeight);
                }

                if (server.QueueShockAbsorberWeight is > 0)
                {
                    mapSettings.QueueShockAbsorberWeight = WorldMapQueuePolicy.ClampShockAbsorberWeight(server.QueueShockAbsorberWeight.Value, mapSettings.QueueShockAbsorberWeight);
                }

                if (server.QueueOverloadDrainFactor > 0)
                {
                    mapSettings.QueueOverloadDrainFactor = Math.Clamp(server.QueueOverloadDrainFactor.Value, 1, 16);
                }

                if (server.QueueBackoffDelayMs > 0)
                {
                    mapSettings.QueueBackoffDelayMs = Math.Clamp(server.QueueBackoffDelayMs.Value, 1, 200);
                }

                if (server.QueueEmergencyHoldTicks > 0)
                {
                    mapSettings.QueueEmergencyHoldTicks = Math.Clamp(server.QueueEmergencyHoldTicks.Value, 1, 128);
                }

                if (server.QueueRecoveryRampTicks > 0)
                {
                    mapSettings.QueueRecoveryRampTicks = Math.Clamp(server.QueueRecoveryRampTicks.Value, 1, 256);
                }

                if (server.QueueNearChunkKeepCount > 0)
                {
                    mapSettings.QueueNearChunkKeepCount = Math.Clamp(server.QueueNearChunkKeepCount.Value, 8, 512);
                }

                if (server.QueueHotspotBias is > 0)
                {
                    mapSettings.QueueHotspotBias = WorldMapQueuePolicy.ClampHotspotBias(server.QueueHotspotBias.Value, mapSettings.QueueHotspotBias);
                }

                if (server.QueueHotspotEmergencyPenalty is > 0)
                {
                    mapSettings.QueueHotspotEmergencyPenalty = WorldMapQueuePolicy.ClampHotspotEmergencyPenalty(server.QueueHotspotEmergencyPenalty.Value, mapSettings.QueueHotspotEmergencyPenalty);
                }

                if (server.QueueHotspotRetentionSeconds > 0)
                {
                    mapSettings.QueueHotspotRetentionSeconds = Math.Clamp(server.QueueHotspotRetentionSeconds.Value, 5, 300);
                }

                if (server.InflightChunkTimeoutSeconds > 0)
                {
                    mapSettings.InflightChunkTimeoutSeconds = Math.Clamp(server.InflightChunkTimeoutSeconds.Value, 5, 600);
                }

                if (server.InflightPruneIntervalSeconds > 0)
                {
                    mapSettings.InflightPruneIntervalSeconds = Math.Clamp(server.InflightPruneIntervalSeconds.Value, 1, 120);
                }

                if (server.QueueStalePruneMax > 0)
                {
                    mapSettings.QueueStalePruneMax = Math.Clamp(server.QueueStalePruneMax.Value, 8, 256);
                }

                if (server.QueueStalePruneEmergencyMultiplier is > 0)
                {
                    mapSettings.QueueStalePruneEmergencyMultiplier = Math.Clamp(server.QueueStalePruneEmergencyMultiplier.Value, 1.0, 3.0);
                }

                Console.WriteLine(
                    $"[WorldMapQueuePolicy] Applied queue settings from {queuePolicyPath} " +
                    $"(queueLimit={mapSettings.MaxQueuedChunkRequests}, queuePressure={mapSettings.QueuePressureFactor}, " +
                    $"queueSlack={mapSettings.QueueSlackRatio:F2}, burstSlack={mapSettings.QueueBurstSlackMultiplier:F2}, shed={mapSettings.QueueLoadSheddingThreshold:F2}, emergencyBrake={mapSettings.QueueEmergencyBrakeThreshold:F2}, emaBlend={mapSettings.QueueLoadEmaBlend:F2}, releaseRatio={mapSettings.QueueEmergencyReleaseRatio:F2}, trend={mapSettings.QueueTrendBoostWeight:F2}, shock={mapSettings.QueueShockAbsorberWeight:F2}, hotspotBias={mapSettings.QueueHotspotBias:F2}, hotspotEmergencyPenalty={mapSettings.QueueHotspotEmergencyPenalty:F2}, hotspotRetentionSec={mapSettings.QueueHotspotRetentionSeconds}, nearKeep={mapSettings.QueueNearChunkKeepCount}, drain={mapSettings.QueueOverloadDrainFactor}, backoffMs={mapSettings.QueueBackoffDelayMs}, holdTicks={mapSettings.QueueEmergencyHoldTicks}, recoveryRampTicks={mapSettings.QueueRecoveryRampTicks}, inflightTimeoutSec={mapSettings.InflightChunkTimeoutSeconds}, inflightPruneSec={mapSettings.InflightPruneIntervalSeconds}, stalePruneMax={mapSettings.QueueStalePruneMax}, stalePruneEmergencyMultiplier={mapSettings.QueueStalePruneEmergencyMultiplier:F2}, " +
                    $"maxConcurrent={mapSettings.MaxConcurrentChunkGenerations}, batch={mapSettings.UpdateBatchSize}, intervalMs={mapSettings.UpdateIntervalMs}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapQueuePolicy][WARN] Failed to apply '{queuePolicyPath}': {ex.Message}");
            }
        }

        private static void TryMirrorProfileToStreamingAssets(string profilePath)
        {
            try
            {
                string repoTargetPath = ResolveRepoPath(Path.Combine("Assets", "StreamingAssets", "world-map-control.json"));
                MirrorProfile(profilePath, repoTargetPath);

                string rootTargetPath = Path.GetFullPath(Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets",
                    "StreamingAssets",
                    "world-map-control.json"));
                if (!string.Equals(repoTargetPath, rootTargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    MirrorProfile(profilePath, rootTargetPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorldMapControlProfile][WARN] Failed to mirror profile to StreamingAssets: {ex.Message}");
            }
        }

        private static void MirrorProfile(string sourcePath, string targetPath)
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourcePath, targetPath, overwrite: true);
            Console.WriteLine($"[WorldMapControlProfile] Mirrored profile to {targetPath}");
        }

        private static void TryMirrorProfileToRootConfig(string profilePath, string configuredRelativePath)
        {
            try
            {
                string targetPath = Path.IsPathRooted(configuredRelativePath)
                    ? configuredRelativePath
                    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredRelativePath));

                if (string.Equals(profilePath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

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
                Console.WriteLine($"[WorldMapControlProfile][WARN] Failed to mirror profile to root config: {ex.Message}");
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

            [JsonPropertyName("maxQueuedChunkRequests")]
            public int? MaxQueuedChunkRequests { get; set; }

            [JsonPropertyName("queuePressureFactor")]
            public int? QueuePressureFactor { get; set; }

            [JsonPropertyName("queueSlackRatio")]
            public double? QueueSlackRatio { get; set; }

            [JsonPropertyName("queueBurstSlackMultiplier")]
            public double? QueueBurstSlackMultiplier { get; set; }

            [JsonPropertyName("queueLoadSheddingThreshold")]
            public double? QueueLoadSheddingThreshold { get; set; }

            [JsonPropertyName("queueEmergencyBrakeThreshold")]
            public double? QueueEmergencyBrakeThreshold { get; set; }

            [JsonPropertyName("queueLoadEmaBlend")]
            public double? QueueLoadEmaBlend { get; set; }

            [JsonPropertyName("queueEmergencyReleaseRatio")]
            public double? QueueEmergencyReleaseRatio { get; set; }

            [JsonPropertyName("queueTrendBoostWeight")]
            public double? QueueTrendBoostWeight { get; set; }

            [JsonPropertyName("queueShockAbsorberWeight")]
            public double? QueueShockAbsorberWeight { get; set; }

            [JsonPropertyName("queueOverloadDrainFactor")]
            public int? QueueOverloadDrainFactor { get; set; }

            [JsonPropertyName("queueBackoffDelayMs")]
            public int? QueueBackoffDelayMs { get; set; }

            [JsonPropertyName("queueEmergencyHoldTicks")]
            public int? QueueEmergencyHoldTicks { get; set; }

            [JsonPropertyName("queueRecoveryRampTicks")]
            public int? QueueRecoveryRampTicks { get; set; }

            [JsonPropertyName("queueNearChunkKeepCount")]
            public int? QueueNearChunkKeepCount { get; set; }

            [JsonPropertyName("queueHotspotBias")]
            public double? QueueHotspotBias { get; set; }

            [JsonPropertyName("queueHotspotEmergencyPenalty")]
            public double? QueueHotspotEmergencyPenalty { get; set; }

            [JsonPropertyName("queueHotspotRetentionSeconds")]
            public int? QueueHotspotRetentionSeconds { get; set; }

            [JsonPropertyName("inflightChunkTimeoutSeconds")]
            public int? InflightChunkTimeoutSeconds { get; set; }

            [JsonPropertyName("inflightPruneIntervalSeconds")]
            public int? InflightPruneIntervalSeconds { get; set; }

            [JsonPropertyName("queueStalePruneMax")]
            public int? QueueStalePruneMax { get; set; }

            [JsonPropertyName("queueStalePruneEmergencyMultiplier")]
            public double? QueueStalePruneEmergencyMultiplier { get; set; }
        }

        private sealed class WorldMapRuntimeTerrainGeneration
        {
            [JsonPropertyName("maxConcurrentChunkGenerations")]
            public int? MaxConcurrentChunkGenerations { get; set; }

            [JsonPropertyName("updateBatchSize")]
            public int? UpdateBatchSize { get; set; }

            [JsonPropertyName("updateIntervalMs")]
            public int? UpdateIntervalMs { get; set; }

            [JsonPropertyName("maxQueuedChunkRequests")]
            public int? MaxQueuedChunkRequests { get; set; }

            [JsonPropertyName("queuePressureFactor")]
            public int? QueuePressureFactor { get; set; }

            [JsonPropertyName("queueSlackRatio")]
            public double? QueueSlackRatio { get; set; }

            [JsonPropertyName("queueBurstSlackMultiplier")]
            public double? QueueBurstSlackMultiplier { get; set; }

            [JsonPropertyName("queueLoadSheddingThreshold")]
            public double? QueueLoadSheddingThreshold { get; set; }

            [JsonPropertyName("queueEmergencyBrakeThreshold")]
            public double? QueueEmergencyBrakeThreshold { get; set; }

            [JsonPropertyName("queueLoadEmaBlend")]
            public double? QueueLoadEmaBlend { get; set; }

            [JsonPropertyName("queueEmergencyReleaseRatio")]
            public double? QueueEmergencyReleaseRatio { get; set; }

            [JsonPropertyName("queueTrendBoostWeight")]
            public double? QueueTrendBoostWeight { get; set; }

            [JsonPropertyName("queueShockAbsorberWeight")]
            public double? QueueShockAbsorberWeight { get; set; }

            [JsonPropertyName("queueOverloadDrainFactor")]
            public int? QueueOverloadDrainFactor { get; set; }

            [JsonPropertyName("queueBackoffDelayMs")]
            public int? QueueBackoffDelayMs { get; set; }

            [JsonPropertyName("queueEmergencyHoldTicks")]
            public int? QueueEmergencyHoldTicks { get; set; }

            [JsonPropertyName("queueRecoveryRampTicks")]
            public int? QueueRecoveryRampTicks { get; set; }

            [JsonPropertyName("queueNearChunkKeepCount")]
            public int? QueueNearChunkKeepCount { get; set; }

            [JsonPropertyName("queueHotspotBias")]
            public double? QueueHotspotBias { get; set; }

            [JsonPropertyName("queueHotspotEmergencyPenalty")]
            public double? QueueHotspotEmergencyPenalty { get; set; }

            [JsonPropertyName("queueHotspotRetentionSeconds")]
            public int? QueueHotspotRetentionSeconds { get; set; }

            [JsonPropertyName("inflightChunkTimeoutSeconds")]
            public int? InflightChunkTimeoutSeconds { get; set; }

            [JsonPropertyName("inflightPruneIntervalSeconds")]
            public int? InflightPruneIntervalSeconds { get; set; }

            [JsonPropertyName("queueStalePruneMax")]
            public int? QueueStalePruneMax { get; set; }

            [JsonPropertyName("queueStalePruneEmergencyMultiplier")]
            public double? QueueStalePruneEmergencyMultiplier { get; set; }
        }

        private sealed class WorldMapQueuePolicyConfig
        {
            [JsonPropertyName("version")]
            public int Version { get; set; }

            [JsonPropertyName("server")]
            public WorldMapQueuePolicySection? Server { get; set; }
        }

        private sealed class WorldMapQueuePolicySection
        {
            [JsonPropertyName("maxQueuedChunkRequests")]
            public int? MaxQueuedChunkRequests { get; set; }

            [JsonPropertyName("queuePressureFactor")]
            public int? QueuePressureFactor { get; set; }

            [JsonPropertyName("maxConcurrentChunkGenerations")]
            public int? MaxConcurrentChunkGenerations { get; set; }

            [JsonPropertyName("updateBatchSize")]
            public int? UpdateBatchSize { get; set; }

            [JsonPropertyName("updateIntervalMs")]
            public int? UpdateIntervalMs { get; set; }

            [JsonPropertyName("queueSlackRatio")]
            public double? QueueSlackRatio { get; set; }

            [JsonPropertyName("queueBurstSlackMultiplier")]
            public double? QueueBurstSlackMultiplier { get; set; }

            [JsonPropertyName("queueLoadSheddingThreshold")]
            public double? QueueLoadSheddingThreshold { get; set; }

            [JsonPropertyName("queueEmergencyBrakeThreshold")]
            public double? QueueEmergencyBrakeThreshold { get; set; }

            [JsonPropertyName("queueLoadEmaBlend")]
            public double? QueueLoadEmaBlend { get; set; }

            [JsonPropertyName("queueEmergencyReleaseRatio")]
            public double? QueueEmergencyReleaseRatio { get; set; }

            [JsonPropertyName("queueTrendBoostWeight")]
            public double? QueueTrendBoostWeight { get; set; }

            [JsonPropertyName("queueShockAbsorberWeight")]
            public double? QueueShockAbsorberWeight { get; set; }

            [JsonPropertyName("queueOverloadDrainFactor")]
            public int? QueueOverloadDrainFactor { get; set; }

            [JsonPropertyName("queueBackoffDelayMs")]
            public int? QueueBackoffDelayMs { get; set; }

            [JsonPropertyName("queueEmergencyHoldTicks")]
            public int? QueueEmergencyHoldTicks { get; set; }

            [JsonPropertyName("queueRecoveryRampTicks")]
            public int? QueueRecoveryRampTicks { get; set; }

            [JsonPropertyName("queueNearChunkKeepCount")]
            public int? QueueNearChunkKeepCount { get; set; }

            [JsonPropertyName("queueHotspotBias")]
            public double? QueueHotspotBias { get; set; }

            [JsonPropertyName("queueHotspotEmergencyPenalty")]
            public double? QueueHotspotEmergencyPenalty { get; set; }

            [JsonPropertyName("queueHotspotRetentionSeconds")]
            public int? QueueHotspotRetentionSeconds { get; set; }

            [JsonPropertyName("inflightChunkTimeoutSeconds")]
            public int? InflightChunkTimeoutSeconds { get; set; }

            [JsonPropertyName("inflightPruneIntervalSeconds")]
            public int? InflightPruneIntervalSeconds { get; set; }

            [JsonPropertyName("queueStalePruneMax")]
            public int? QueueStalePruneMax { get; set; }

            [JsonPropertyName("queueStalePruneEmergencyMultiplier")]
            public double? QueueStalePruneEmergencyMultiplier { get; set; }
        }
    }
}
