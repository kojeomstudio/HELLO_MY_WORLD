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
        /// Signature for hydrology-aware terrain and map-control alignment on 2026-01-26 (session 18).
        /// </summary>
        public const string HydrologySignature = "2026-01-26-hydrology-shield-v2";

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
                "S18-CORE-01",
                "Hydrology Shielded WorldGen v2",
                FeatureCategory.Core,
                FeatureLayer.Shared,
                new[]
                {
                    "GameServer/World/WorldMapControlProfile.cs",
                    "Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs",
                    "config/world_map_control_profile.json"
                },
                new[] { HydrologySignature, "config/world.json" },
                "in-progress",
                "high"),
            new SharedFeatureDescriptor(
                "S18-CORE-02",
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
                "planned",
                "high"),
            new SharedFeatureDescriptor(
                "S18-CONTENT-01",
                "Hydrology-Aware Caves",
                FeatureCategory.Content,
                FeatureLayer.Server,
                new[]
                {
                    "GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs",
                    "MapGeneratorLib/MapGeneratorLib/Sources/Algorithms/WorldGenAlgorithms.cs"
                },
                new[] { HydrologySignature, "config/world.json" },
                "planned",
                "high"),
            new SharedFeatureDescriptor(
                "S18-CONTENT-02",
                "River Curvature + Hydrology Warp",
                FeatureCategory.Content,
                FeatureLayer.Shared,
                new[]
                {
                    "GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs",
                    "Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs"
                },
                new[] { HydrologySignature, "config/world.json" },
                "planned",
                "high"),
            new SharedFeatureDescriptor(
                "S18-CONTENT-03",
                "Lake Shoreline + Outflow Harmonization",
                FeatureCategory.Content,
                FeatureLayer.Shared,
                new[]
                {
                    "GameServer/World/Generation/EnhancedTerrainGenerationPipeline.cs",
                    "Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs"
                },
                new[] { HydrologySignature, "config/world.json" },
                "planned",
                "medium"),
            new SharedFeatureDescriptor(
                "S18-UTIL-01",
                "Dummy Protocol Client Round-Trip",
                FeatureCategory.Utility,
                FeatureLayer.Server,
                new[] { "GameServer/Testing/DummyProtocolClient.cs" },
                new[] { "SharedProtocol", "EnhancedMinecraftProtocol" },
                "planned",
                "medium"),
            new SharedFeatureDescriptor(
                "S18-UTIL-02",
                "Proto Registry + Fingerprint Validation",
                FeatureCategory.Utility,
                FeatureLayer.Shared,
                new[]
                {
                    "SharedProtocol/EnhancedMinecraft/ProtocolRegistry.cs",
                    "GameServer/World/WorldMapControlManager.cs"
                },
                new[] { HydrologySignature },
                "planned",
                "high"),
            new SharedFeatureDescriptor(
                "S18-UTIL-03",
                "Data-Driven Config Parity",
                FeatureCategory.Utility,
                FeatureLayer.Shared,
                new[]
                {
                    "config/world.json",
                    "config/world_map_control_profile.json"
                },
                new[] { HydrologySignature },
                "in-progress",
                "high")
        };
    }
}
