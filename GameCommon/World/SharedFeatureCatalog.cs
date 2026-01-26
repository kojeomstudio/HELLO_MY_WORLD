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
        /// Signature for hydrology-aware terrain and map-control alignment on 2026-01-26.
        /// </summary>
        public const string HydrologySignature = "2026-01-26-hydrology-shield";

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
                "SHARED-WORLD-CORE-01",
                "Hydrology Shielded WorldGen",
                FeatureCategory.Core,
                FeatureLayer.Shared,
                new[] { "GameServer/World/Generation/ImprovedTerrainCoordinator.cs", "Assets/MyAssets/Scripts/GameWorld/WorldMapController.cs" },
                new[] { HydrologySignature },
                "in-progress",
                "high"),
            new SharedFeatureDescriptor(
                "SHARED-PROTO-UTIL-01",
                "EnhancedMinecraft Dummy Client",
                FeatureCategory.Utility,
                FeatureLayer.Server,
                new[] { "GameServer/Testing/DummyProtocolClient.cs" },
                new[] { "SharedProtocol", "EnhancedMinecraftProtocol" },
                "in-progress",
                "medium"),
            new SharedFeatureDescriptor(
                "SHARED-WORLD-UTIL-02",
                "World Map Control Signature",
                FeatureCategory.Utility,
                FeatureLayer.Shared,
                new[] { "GameServer/World/WorldMapControlProfile.cs", "Assets/MyAssets/Scripts/GameWorld/WorldMapControlProfile.cs" },
                new[] { HydrologySignature },
                "in-progress",
                "high")
        };
    }
}
