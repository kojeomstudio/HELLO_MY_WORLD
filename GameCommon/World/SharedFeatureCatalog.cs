using System.Collections.Generic;

namespace GameCommon.World
{
    /// <summary>
    /// Shared feature identifiers consumed by both server and Unity via GameCommon.dll.
    /// </summary>
    public enum FeatureCategory
    {
        Core,
        Content,
        Utility
    }

    public enum FeatureLayer
    {
        Shared,
        Server,
        Client
    }

    public sealed class SharedFeatureDescriptor
    {
        public SharedFeatureDescriptor(
            string id,
            string name,
            FeatureCategory category,
            FeatureLayer layer,
            string[] owners,
            string[] artifacts,
            string status,
            string priority)
        {
            Id = id;
            Name = name;
            Category = category;
            Layer = layer;
            Owners = owners;
            Artifacts = artifacts;
            Status = status;
            Priority = priority;
        }

        public string Id { get; }
        public string Name { get; }
        public FeatureCategory Category { get; }
        public FeatureLayer Layer { get; }
        public string[] Owners { get; }
        public string[] Artifacts { get; }
        public string Status { get; }
        public string Priority { get; }
    }

    public static class SharedFeatureCatalog
    {
        /// <summary>
        /// Signature for hydrology-aware terrain and map-control alignment on 2026-02-24
        /// (floodplain-coupled cave shielding + hotspot-retention queue guard + map-control v55).
        /// </summary>
        public const string HydrologySignature = "2026-02-24-hydrology-riverlake-cave-v51";

        /// <summary>
        /// Descriptor list used by diagnostics and tooling to align client/server feature coverage.
        /// </summary>
        public static IReadOnlyList<SharedFeatureDescriptor> Features => features;

        public static bool TryGetFeature(string id, out SharedFeatureDescriptor descriptor)
        {
            foreach (var feature in features)
            {
                if (feature.Id == id)
                {
                    descriptor = feature;
                    return true;
                }
            }

            descriptor = default!;
            return false;
        }

        private static readonly List<SharedFeatureDescriptor> features = new()
        {
            new SharedFeatureDescriptor(
                "S20-CORE-01",
                "Hydrology WorldGen v51",
                FeatureCategory.Core,
                FeatureLayer.Shared,
                new[]
                {
                    "GameCommon/World/WorldMapControlProfile.cs",
                    "GameCommon/World/WorldMapControlProfileUtility.cs",
                    "GameServer/World/WorldMapControlProfile.cs",
                    "Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs",
                    "config/world_map_control_profile.json",
                    "config/minecraft_feature_client_server_core_content_util_2026-02-23-session-113.json"
                },
                new[]
                {
                    HydrologySignature,
                    "config/world.json",
                    "Assets/StreamingAssets/world-config.json",
                    "MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs"
                },
                "implemented",
                "high"),
            new SharedFeatureDescriptor(
                "S20-CORE-02",
                "Shared DLL + Proto Contracts",
                FeatureCategory.Core,
                FeatureLayer.Shared,
                new[]
                {
                    "GameCommon/GameCommon.csproj",
                    "SharedProtocol/SharedProtocol.csproj",
                    "Assets/Plugins/GameCommon.dll"
                },
                new[] { "SharedProtocol", "GameCommon" },
                "implemented",
                "high"),
            new SharedFeatureDescriptor(
                "S20-CONTENT-01",
                "Hydrology-Aware Caves",
                FeatureCategory.Content,
                FeatureLayer.Server,
                new[]
                {
                    "GameServer/World/Generation/ImprovedCaveGenerator.cs",
                    "GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs",
                    "MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs"
                },
                new[] { HydrologySignature, "config/world.json", "config/enhanced_terrain_generation.json" },
                "implemented",
                "high"),
            new SharedFeatureDescriptor(
                "S20-CONTENT-02",
                "River Curvature + Oxbow Cutoff Continuity",
                FeatureCategory.Content,
                FeatureLayer.Shared,
                new[]
                {
                    "GameServer/World/Generation/ImprovedRiverGenerator.cs",
                    "GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs",
                    "Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs"
                },
                new[] { HydrologySignature, "config/world.json" },
                "implemented",
                "high"),
            new SharedFeatureDescriptor(
                "S20-CONTENT-03",
                "Lake Shoreline + Alluvial Backwater Link",
                FeatureCategory.Content,
                FeatureLayer.Shared,
                new[]
                {
                    "GameServer/World/Generation/ImprovedLakeGenerator.cs",
                    "GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs",
                    "Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs"
                },
                new[] { HydrologySignature, "config/world.json" },
                "implemented",
                "medium"),
            new SharedFeatureDescriptor(
                "S20-UTIL-01",
                "Proto Registry + Fingerprint Validation",
                FeatureCategory.Utility,
                FeatureLayer.Shared,
                new[]
                {
                    "SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs",
                    "SharedProtocol/EnhancedMinecraft/ProtoDiagnostics.cs",
                    "GameServer/World/WorldMapControlManager.cs"
                },
                new[] { HydrologySignature },
                "implemented",
                "high"),
                new SharedFeatureDescriptor(
                "S20-UTIL-02",
                "Data-Driven Config Parity",
                FeatureCategory.Utility,
                FeatureLayer.Shared,
                new[]
                {
                    "config/world.json",
                    "config/world_map_control_profile.json",
                    "config/minecraft_feature_client_server_core_content_util_2026-02-23-session-113.json"
                },
                new[] { HydrologySignature },
                "implemented",
                "high"),
            new SharedFeatureDescriptor(
                "S20-UTIL-03",
                "Dummy Protocol Client Round-Trip",
                FeatureCategory.Utility,
                FeatureLayer.Server,
                new[]
                {
                    "GameServer/Testing/DummyProtocolClient.cs",
                    "config/protocol_dummy_client.json"
                },
                new[] { "SharedProtocol", "EnhancedMinecraftProtocol" },
                "implemented",
                "medium")
        };
    }
}
